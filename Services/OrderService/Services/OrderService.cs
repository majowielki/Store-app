using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Observability;
using Store.Contracts.Orders.V1;
using Store.OrderService.Clients;
using Store.OrderService.Data;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Store.OrderService.Services;

public class OrderService : IOrderService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly OrderDbContext _context;
    private readonly ICartClient _cart;
    private readonly ICatalogClient _catalog;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly PricingOptions _pricing;
    private readonly StoreMetrics _metrics;
    private readonly TimeProvider _time;
    private readonly DeliveryEstimator _delivery;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrderDbContext context,
        ICartClient cart,
        ICatalogClient catalog,
        IPublishEndpoint publishEndpoint,
        IOptions<PricingOptions> pricing,
        StoreMetrics metrics,
        TimeProvider time,
        DeliveryEstimator delivery,
        ILogger<OrderService> logger)
    {
        _context = context;
        _cart = cart;
        _catalog = catalog;
        _publishEndpoint = publishEndpoint;
        _pricing = pricing.Value;
        _metrics = metrics;
        _time = time;
        _delivery = delivery;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateOrderFromCartAsync(CreateOrderFromCartRequest request, string? idempotencyKey = null)
    {
        var requestHash = idempotencyKey is null ? null : HashRequest(request);
        if (idempotencyKey is not null)
        {
            // A retry of an answered checkout gets the same order back, before any work is done
            var replayed = await FindAnsweredAsync(idempotencyKey, request.UserId, requestHash!);
            if (replayed is not null)
            {
                return replayed;
            }
        }

        var cart = await _cart.GetSnapshotAsync(request.UserId);
        if (cart is null || cart.Lines.Count == 0)
        {
            throw new DomainValidationException("The cart is empty");
        }

        // Price the lines from the catalogue as it is now; the cart's prices may be stale
        var lines = new List<OrderLine>(cart.Lines.Count);
        var repricedLines = 0;
        foreach (var line in cart.Lines)
        {
            var product = await _catalog.GetSnapshotAsync(line.ProductId);
            if (product is null || !product.IsActive)
            {
                throw new ConflictException($"\"{line.Title}\" is no longer available. Remove it from the cart to continue.");
            }

            if (product.EffectivePrice != line.UnitPrice) repricedLines++;
            lines.Add(new OrderLine
            {
                ProductId = product.Id,
                ProductTitle = product.Title,
                ProductImage = product.Image,
                Company = product.Company,
                Color = line.Color,
                UnitPrice = product.EffectivePrice,
                Quantity = line.Quantity
            });
        }

        if (repricedLines > 0) _metrics.PriceMismatch(repricedLines, "checkout");

        // The audit service records the order from the OrderPlaced event
        return await PlaceOrderAsync(request, lines, idempotencyKey, requestHash);
    }

    /// <summary>
    /// Writes the order, the idempotency key and the order-placed event in one transaction.
    /// The customer row is locked first, so concurrent checkouts of the same customer are
    /// serialised: only one of them can be the first order, and a duplicate that waited on the
    /// lock finds the key its twin stored and returns that order instead of creating another.
    /// </summary>
    private async Task<OrderResponse> PlaceOrderAsync(
        CreateOrderFromCartRequest request, List<OrderLine> lines, string? idempotencyKey, string? requestHash)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""INSERT INTO "Customers" ("UserId", "OrdersPlaced") VALUES ({request.UserId}, 0) ON CONFLICT ("UserId") DO NOTHING""");
        var customer = await _context.Customers
            .FromSqlInterpolated($"""SELECT * FROM "Customers" WHERE "UserId" = {request.UserId} FOR UPDATE""")
            .SingleAsync();

        if (idempotencyKey is not null)
        {
            var replayed = await FindAnsweredAsync(idempotencyKey, request.UserId, requestHash!);
            if (replayed is not null)
            {
                await transaction.RollbackAsync();
                return replayed;
            }
        }

        var subtotal = lines.Sum(l => l.LineTotal);
        var code = await LockDiscountCodeAsync(request.DiscountCode, subtotal, now);
        var totals = PricingPolicy.Calculate(subtotal, isFirstOrder: customer.OrdersPlaced == 0, _pricing, code?.Discount ?? 0m);
        var delivery = _delivery.EstimateNow();

        var order = new Order
        {
            UserId = request.UserId,
            UserEmail = request.UserEmail,
            DeliveryAddress = request.DeliveryAddress,
            CustomerName = request.CustomerName,
            Notes = request.Notes,
            Lines = lines,
            Subtotal = totals.Subtotal,
            DiscountAmount = totals.DiscountAmount,
            DiscountReason = totals.DiscountReason,
            DeliveryFee = totals.DeliveryFee,
            Total = totals.Total,
            Status = OrderStatus.Placed,
            StatusHistory = [new OrderStatusChange { Status = OrderStatus.Placed, ChangedAt = now, ChangedBy = request.UserId }],
            DeliveryFrom = delivery.From,
            DeliveryTo = delivery.To,
            CreatedAt = now
        };

        // The code counts as used only when it, not the first-order discount, took the money off
        if (code is { } applied && totals.DiscountReason == PricingPolicy.CodeDiscountReason)
        {
            applied.Entry.TimesUsed++;
            order.DiscountCode = applied.Entry.Code;
        }

        customer.OrdersPlaced++;
        customer.FirstOrderAt ??= now;
        customer.LastOrderAt = now;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        if (idempotencyKey is not null)
        {
            _context.IdempotencyKeys.Add(new IdempotencyKey
            {
                Key = idempotencyKey,
                UserId = request.UserId,
                RequestHash = requestHash!,
                OrderId = order.Id,
                CreatedAt = now
            });
        }

        // Goes to the outbox table with this transaction; the cart, identity and audit
        // services receive it once the transaction is committed
        await _publishEndpoint.Publish(new OrderPlaced(
            order.Id,
            order.UserId,
            order.UserEmail,
            order.CustomerName,
            order.DeliveryAddress,
            request.SaveAddress,
            order.Subtotal,
            order.DiscountAmount,
            order.DeliveryFee,
            order.Total,
            order.Lines.Select(l => new OrderPlacedLine(l.ProductId, l.ProductTitle, l.Quantity, l.UnitPrice)).ToList(),
            order.CreatedAt));

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        _metrics.OrderPlaced(order.Total, order.DiscountReason);
        return MapToOrderResponse(order);
    }

    /// <summary>
    /// The discount code the customer typed, locked until the order commits so its usage limit
    /// holds when many checkouts use it at once, with what it takes off the subtotal. A code
    /// that cannot be used refuses the order with the reason, rather than charging more than the
    /// cart promised.
    /// </summary>
    private async Task<(DiscountCode Entry, decimal Discount)?> LockDiscountCodeAsync(string? typed, decimal subtotal, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(typed))
        {
            return null;
        }

        var normalized = DiscountCode.Normalize(typed);
        await _context.Database.ExecuteSqlInterpolatedAsync($"""SELECT 1 FROM "DiscountCodes" WHERE "Code" = {normalized} FOR UPDATE""");
        var entry = await _context.DiscountCodes.FirstOrDefaultAsync(c => c.Code == normalized);
        var check = DiscountCodePolicy.Check(entry, subtotal, now);
        if (!check.IsUsable)
        {
            throw new DomainValidationException(check.Refusal!);
        }

        return (entry!, check.Amount);
    }

    /// <summary>
    /// The order an earlier request with this key received, or null when the key is new. The
    /// same key with a different body or from a different customer is rejected.
    /// </summary>
    private async Task<OrderResponse?> FindAnsweredAsync(string idempotencyKey, string userId, string requestHash)
    {
        var answered = await _context.IdempotencyKeys.AsNoTracking().FirstOrDefaultAsync(k => k.Key == idempotencyKey);
        if (answered is null)
        {
            return null;
        }

        if (answered.UserId != userId || answered.RequestHash != requestHash)
        {
            throw new DomainValidationException("Idempotency-Key was already used for a different request");
        }

        var order = await _context.Orders.AsNoTracking().Include(o => o.Lines).Include(o => o.StatusHistory).SingleAsync(o => o.Id == answered.OrderId);
        _logger.LogInformation("Checkout with Idempotency-Key {Key} replayed order {OrderId}", idempotencyKey, order.Id);
        return MapToOrderResponse(order);
    }

    private static string HashRequest(CreateOrderFromCartRequest request)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            request.UserEmail,
            request.CustomerName,
            request.DeliveryAddress,
            request.Notes,
            request.SaveAddress,
            request.DiscountCode
        }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public async Task<OrderResponse> GetOrderAsync(int orderId, string userId)
    {
        var order = await FindOrderAsync(orderId);

        // Customers only see their own orders; admins go through GetOrderForAdminAsync
        if (order.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to access order {OrderId} belonging to {OrderUserId}",
                userId, orderId, order.UserId);
            throw new ForbiddenException("This order belongs to another customer");
        }

        return MapToOrderResponse(order);
    }

    public async Task<OrderResponse> GetOrderForAdminAsync(int orderId)
        => MapToOrderResponse(await FindOrderAsync(orderId));

    /// <summary>
    /// Moves an order to another status along <see cref="OrderStatusFlow"/>. The order row is
    /// locked for the change, so two administrators pressing buttons at once cannot both move
    /// it from the same status; the change, its history row and the event commit together.
    /// </summary>
    public async Task<OrderResponse> ChangeStatusAsync(int orderId, OrderStatus status, string actorId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        await _context.Database.ExecuteSqlInterpolatedAsync($"""SELECT 1 FROM "Orders" WHERE "Id" = {orderId} FOR UPDATE""");
        var order = await _context.Orders
            .Include(o => o.Lines)
            .Include(o => o.StatusHistory)
            .SingleOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("Order", orderId);

        var previous = order.Status;
        if (!OrderStatusFlow.CanMove(previous, status))
        {
            throw new ConflictException(previous == status
                ? $"The order is already {Describe(status)}."
                : $"An order that is {Describe(previous)} cannot be marked {Describe(status)}.");
        }

        if (status == OrderStatus.Cancelled && order.DiscountCode is { } used)
        {
            await ReleaseDiscountCodeAsync(used);
        }

        var now = _time.GetUtcNow().UtcDateTime;
        order.Status = status;
        order.StatusHistory.Add(new OrderStatusChange { Status = status, ChangedAt = now, ChangedBy = actorId });

        // With the change in the outbox: the audit service records it once the transaction commits
        await _publishEndpoint.Publish(new OrderStatusChanged(order.Id, order.UserId, previous.ToString(), status.ToString(), actorId, now));

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation("Order {OrderId} moved from {Previous} to {Status} by {ActorId}", order.Id, previous, status, actorId);
        return MapToOrderResponse(order);
    }

    /// <summary>
    /// Gives a cancelled order's code its use back, so a code limited to a few orders is not used
    /// up by orders that never went through. The order keeps naming the code it was placed with.
    /// </summary>
    private async Task ReleaseDiscountCodeAsync(string code)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($"""SELECT 1 FROM "DiscountCodes" WHERE "Code" = {code} FOR UPDATE""");
        var entry = await _context.DiscountCodes.FirstOrDefaultAsync(c => c.Code == code);
        if (entry is { TimesUsed: > 0 })
        {
            entry.TimesUsed--;
        }
    }

    private static string Describe(OrderStatus status) => status.ToString().ToLowerInvariant();

    private async Task<Order> FindOrderAsync(int orderId)
        => await _context.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("Order", orderId);

    public Task<PagedResponse<OrderResponse>> GetUserOrdersAsync(string userId, PagedQuery paging)
        => ListOrdersAsync(_context.Orders.Where(o => o.UserId == userId), paging);

    public Task<PagedResponse<OrderResponse>> GetAllOrdersAsync(PagedQuery paging)
        => ListOrdersAsync(_context.Orders, paging);

    private static async Task<PagedResponse<OrderResponse>> ListOrdersAsync(IQueryable<Order> query, PagedQuery paging)
    {
        paging = paging.Normalized();
        var totalCount = await query.CountAsync();

        var orders = await query
            .AsNoTracking()
            .Include(o => o.Lines)
            .Include(o => o.StatusHistory)
            // Two collections: one query each instead of their product; the order must be unique for the pages to agree
            .AsSplitQuery()
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync();

        return new PagedResponse<OrderResponse>(orders.Select(MapToOrderResponse).ToList(), totalCount, paging);
    }

    public Task<int> GetUserOrdersCountAsync(string userId)
        => _context.Orders.CountAsync(o => o.UserId == userId);

    public async Task<OrderStatsResponse> GetOrderStatsAsync(int daysWindow = 30)
    {
        var since = _time.GetUtcNow().UtcDateTime.Date.AddDays(-Math.Abs(daysWindow));
        var window = _context.Orders.AsNoTracking().Where(o => o.CreatedAt >= since);

        // Aggregates run in SQL; only one row per day and per product comes back
        var daily = await window
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new TimeBucketStats { BucketStart = g.Key, Orders = g.Count(), Revenue = g.Sum(o => o.Total) })
            .OrderBy(b => b.BucketStart)
            .ToListAsync();

        var weekly = daily
            .GroupBy(d => WeekStart(d.BucketStart))
            .OrderBy(g => g.Key)
            .Select(g => new TimeBucketStats { BucketStart = g.Key, Orders = g.Sum(d => d.Orders), Revenue = g.Sum(d => d.Revenue) })
            .ToList();

        var topProducts = await _context.OrderLines.AsNoTracking()
            .Where(l => l.Order.CreatedAt >= since)
            .GroupBy(l => new { l.ProductId, l.ProductTitle })
            .Select(g => new TopProductStats
            {
                ProductId = g.Key.ProductId,
                ProductTitle = g.Key.ProductTitle,
                Quantity = g.Sum(l => l.Quantity),
                Revenue = g.Sum(l => l.UnitPrice * l.Quantity)
            })
            .OrderByDescending(p => p.Quantity)
            .ThenByDescending(p => p.Revenue)
            .Take(10)
            .ToListAsync();

        return new OrderStatsResponse
        {
            TotalOrders = daily.Sum(d => d.Orders),
            TotalRevenue = daily.Sum(d => d.Revenue),
            Daily = daily,
            Weekly = weekly,
            TopProducts = topProducts
        };
    }

    /// <summary>Monday of the ISO week the date falls in.</summary>
    private static DateTime WeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private static OrderResponse MapToOrderResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            UserEmail = order.UserEmail,
            DeliveryAddress = order.DeliveryAddress,
            CustomerName = order.CustomerName,
            OrderItems = order.Lines.Select(l => new OrderItemResponse
            {
                Id = l.Id,
                ProductId = l.ProductId,
                ProductTitle = l.ProductTitle,
                ProductImage = l.ProductImage,
                Price = l.UnitPrice,
                Quantity = l.Quantity,
                Color = l.Color,
                Company = l.Company,
                LineTotal = l.LineTotal
            }).ToList(),
            TotalItems = order.TotalItems,
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            DiscountReason = order.DiscountReason,
            DiscountCode = order.DiscountCode,
            DeliveryFee = order.DeliveryFee,
            Total = order.Total,
            Status = order.Status.ToString(),
            StatusHistory = order.StatusHistory
                .OrderBy(c => c.ChangedAt)
                .ThenBy(c => c.Id)
                .Select(c => new OrderStatusChangeResponse { Status = c.Status.ToString(), ChangedAt = c.ChangedAt })
                .ToList(),
            NextStatuses = OrderStatusFlow.NextFrom(order.Status).Select(s => s.ToString()).ToList(),
            DeliveryFrom = order.DeliveryFrom,
            DeliveryTo = order.DeliveryTo,
            CreatedAt = order.CreatedAt,
            Notes = order.Notes
        };
    }
}
