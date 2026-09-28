using MassTransit;
using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.Contracts.Catalog.V1;
using Store.Contracts.Orders.V1;
using Store.ProductService.Data;
using Store.ProductService.Models;

namespace Store.ProductService.Services;

/// <summary>
/// The stock of the catalogue: units on hand, the units orders hold and the visitors waiting for
/// a product that ran out. The order events drive it: a placed order reserves every line or
/// none, a cancelled one gives its units back, a shipped one takes them off the stock.
/// </summary>
public interface IStockLedger
{
    /// <summary>
    /// Holds the units of every line of <paramref name="order"/> and publishes
    /// <see cref="StockReserved"/>, or holds nothing and publishes <see cref="StockUnavailable"/>
    /// when a line lacks units. Runs inside the caller's transaction (a consumer's inbox).
    /// </summary>
    Task ReserveAsync(OrderPlaced order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gives back the units a cancelled order holds and publishes <see cref="StockReleased"/>;
    /// for an order not seen yet it leaves a note, so its late "placed" event reserves nothing.
    /// Runs inside the caller's transaction.
    /// </summary>
    Task ReleaseAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>Takes the units of a shipped order off the stock. Runs inside the caller's transaction.</summary>
    Task ShipAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the units on hand of a product (an administrator counting the warehouse); it cannot
    /// go below the units held for orders. Tells the waiting visitors when the product is back.
    /// </summary>
    Task<Product> SetStockAsync(int productId, int stockQuantity, string? actorId);

    /// <summary>Asks for an e-mail when a product that ran out is back; asking twice changes nothing.</summary>
    Task SubscribeAsync(int productId, string email);
}

public sealed class StockLedger : IStockLedger
{
    /// <summary>First key of the transaction-scoped advisory lock that serialises everything done for one order.</summary>
    private const int OrderLockSpace = 0x53544B; // "STK"

    private readonly ProductDbContext _context;
    private readonly IPublishEndpoint _publish;
    private readonly IAuditTrail _auditTrail;
    private readonly TimeProvider _time;
    private readonly ILogger<StockLedger> _logger;

    public StockLedger(ProductDbContext context, IPublishEndpoint publish, IAuditTrail auditTrail, TimeProvider time, ILogger<StockLedger> logger)
    {
        _context = context;
        _publish = publish;
        _auditTrail = auditTrail;
        _time = time;
        _logger = logger;
    }

    public async Task ReserveAsync(OrderPlaced order, CancellationToken cancellationToken = default)
    {
        await LockOrderAsync(order.OrderId, cancellationToken);
        var known = await _context.StockOrders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == order.OrderId, cancellationToken);
        if (known is not null)
        {
            // Cancelled before it arrived, or handled already
            _logger.LogInformation("Order {OrderId} is already {Status} here; nothing to reserve", order.OrderId, known.Status);
            return;
        }

        var wanted = order.Lines
            .GroupBy(line => line.ProductId)
            .Select(group => (ProductId: group.Key, Quantity: group.Sum(line => line.Quantity), Title: group.First().ProductTitle))
            .OrderBy(line => line.ProductId)
            .ToList();
        var products = await LockProductsAsync(wanted.Select(line => line.ProductId), cancellationToken);

        var shortages = new List<StockShortage>();
        foreach (var line in wanted)
        {
            var available = products.TryGetValue(line.ProductId, out var product) && product.IsActive ? product.AvailableQuantity : 0;
            if (available < line.Quantity)
            {
                shortages.Add(new StockShortage(line.ProductId, product?.Title ?? line.Title, line.Quantity, available));
            }
        }

        var now = _time.GetUtcNow().UtcDateTime;
        var stockOrder = new StockOrder { OrderId = order.OrderId, CreatedAt = now, UpdatedAt = now };
        if (shortages.Count == 0)
        {
            foreach (var line in wanted)
            {
                products[line.ProductId].ReservedQuantity += line.Quantity;
                stockOrder.Lines.Add(new StockOrderLine { ProductId = line.ProductId, Quantity = line.Quantity });
            }

            stockOrder.Status = StockOrderStatus.Reserved;
            await _publish.Publish(new StockReserved(order.OrderId, ToStockLines(stockOrder), now), cancellationToken);
            _logger.LogInformation("Reserved {Units} units for order {OrderId}", wanted.Sum(line => line.Quantity), order.OrderId);
        }
        else
        {
            stockOrder.Status = StockOrderStatus.Unavailable;
            await _publish.Publish(new StockUnavailable(order.OrderId, shortages, now), cancellationToken);
            _logger.LogInformation("Order {OrderId} lacks stock for {Lines} lines; nothing reserved", order.OrderId, shortages.Count);
        }

        _context.StockOrders.Add(stockOrder);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await LockOrderAsync(orderId, cancellationToken);
        var now = _time.GetUtcNow().UtcDateTime;
        var stockOrder = await _context.StockOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

        if (stockOrder is null)
        {
            // The cancellation overtook the order: this note keeps its late arrival from reserving
            _context.StockOrders.Add(new StockOrder { OrderId = orderId, Status = StockOrderStatus.Released, CreatedAt = now, UpdatedAt = now });
            await _publish.Publish(new StockReleased(orderId, [], now), cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (stockOrder.Status is StockOrderStatus.Released or StockOrderStatus.Shipped)
        {
            _logger.LogInformation("Order {OrderId} is already {Status}; nothing to release", orderId, stockOrder.Status);
            return;
        }

        var released = new List<Product>();
        if (stockOrder.Status == StockOrderStatus.Reserved)
        {
            var products = await LockProductsAsync(stockOrder.Lines.Select(line => line.ProductId), cancellationToken);
            foreach (var line in stockOrder.Lines)
            {
                var product = products[line.ProductId];
                product.ReservedQuantity -= line.Quantity;
                released.Add(product);
            }
        }

        stockOrder.Status = StockOrderStatus.Released;
        stockOrder.UpdatedAt = now;
        await _publish.Publish(new StockReleased(orderId, ToStockLines(stockOrder), now), cancellationToken);
        await NotifyWaitingAsync(released, now, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Released the stock of order {OrderId}", orderId);
    }

    public async Task ShipAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await LockOrderAsync(orderId, cancellationToken);
        var stockOrder = await _context.StockOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
        if (stockOrder?.Status != StockOrderStatus.Reserved)
        {
            // Only a paid order ships, and it got its stock; anything else is a message to look at
            _logger.LogWarning("Order {OrderId} shipped, but its stock is {Status}; the stock is left as it is", orderId, stockOrder?.Status.ToString() ?? "unknown");
            return;
        }

        var products = await LockProductsAsync(stockOrder.Lines.Select(line => line.ProductId), cancellationToken);
        foreach (var line in stockOrder.Lines)
        {
            var product = products[line.ProductId];
            product.StockQuantity -= line.Quantity;
            product.ReservedQuantity -= line.Quantity;
        }

        stockOrder.Status = StockOrderStatus.Shipped;
        stockOrder.UpdatedAt = _time.GetUtcNow().UtcDateTime;
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Order {OrderId} shipped: its units left the stock", orderId);
    }

    public async Task<Product> SetStockAsync(int productId, int stockQuantity, string? actorId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var products = await LockProductsAsync([productId], CancellationToken.None);
        var product = products.GetValueOrDefault(productId) ?? throw new NotFoundException("Product", productId);
        if (stockQuantity < product.ReservedQuantity)
        {
            throw new DomainValidationException(
                $"{product.ReservedQuantity} units are held for orders that have not shipped yet; the stock cannot go below that.");
        }

        var previous = product.StockQuantity;
        var now = _time.GetUtcNow().UtcDateTime;
        product.StockQuantity = stockQuantity;
        product.UpdatedAt = now;
        await NotifyWaitingAsync([product], now, CancellationToken.None);
        await _context.SaveChangesAsync();

        await _auditTrail.RecordAsync("PRODUCT_STOCK_UPDATED", nameof(Product), productId.ToString(), actorId,
            oldValues: new { stockQuantity = previous }, newValues: new { stockQuantity, product.ReservedQuantity });
        await transaction.CommitAsync();

        _logger.LogInformation("Stock of product {ProductId} set from {Previous} to {Stock}", productId, previous, stockQuantity);
        return product;
    }

    public async Task SubscribeAsync(int productId, string email)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId && p.IsActive)
            ?? throw new NotFoundException("Product", productId);
        if (product.AvailableQuantity > 0)
        {
            throw new ConflictException($"\"{product.Title}\" is in stock - it can be added to the bag now.");
        }

        var normalized = email.Trim().ToLowerInvariant();
        var now = _time.GetUtcNow().UtcDateTime;
        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "StockAlerts" ("ProductId", "Email", "CreatedAt") VALUES ({productId}, {normalized}, {now})
            ON CONFLICT ("ProductId", "Email") DO NOTHING
            """);
    }

    /// <summary>
    /// One event per visitor waiting for a product that has units again, then their alert goes:
    /// the address is kept only as long as it is needed.
    /// </summary>
    private async Task NotifyWaitingAsync(IEnumerable<Product> products, DateTime now, CancellationToken cancellationToken)
    {
        foreach (var product in products.Where(p => p.IsActive && p.AvailableQuantity > 0).DistinctBy(p => p.Id))
        {
            var alerts = await _context.StockAlerts.Where(a => a.ProductId == product.Id).ToListAsync(cancellationToken);
            foreach (var alert in alerts)
            {
                await _publish.Publish(new ProductBackInStock(
                    product.Id, product.Title, product.Slug, product.Image, product.EffectivePrice, alert.Email, now), cancellationToken);
            }

            _context.StockAlerts.RemoveRange(alerts);
            if (alerts.Count > 0)
            {
                _logger.LogInformation("Product {ProductId} is back: {Count} waiting visitors notified", product.Id, alerts.Count);
            }
        }
    }

    /// <summary>
    /// Everything done for one order waits for whatever is being done for it already, even
    /// before the order has a row here (a cancellation racing the order it cancels).
    /// </summary>
    private async Task LockOrderAsync(int orderId, CancellationToken cancellationToken)
        => await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({OrderLockSpace}, {orderId})", cancellationToken);

    /// <summary>
    /// The products, locked until the transaction ends. Always in id order, so two orders sharing
    /// products cannot each hold one the other waits for.
    /// </summary>
    private async Task<Dictionary<int, Product>> LockProductsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken)
    {
        var ids = productIds.Distinct().Order().ToArray();
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM "Products" WHERE "Id" = ANY({ids}) ORDER BY "Id" FOR UPDATE""", cancellationToken);
        return await _context.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
    }

    private static List<StockLine> ToStockLines(StockOrder order)
        => order.Lines.Select(line => new StockLine(line.ProductId, line.Quantity)).ToList();
}
