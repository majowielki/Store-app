using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Observability;
using Store.BuildingBlocks.Persistence;
using Store.OrderService.Clients;
using Store.OrderService.Data;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Models;
using Store.OrderService.Saga;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Store.OrderService.Services;

/// <summary>Checkout repricing, discounts and idempotent transactional order placement.</summary>
public sealed class OrderCheckout
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
    private readonly ILogger<OrderCheckout> _logger;

    public OrderCheckout(
        OrderDbContext context,
        ICartClient cart,
        ICatalogClient catalog,
        IPublishEndpoint publishEndpoint,
        IOptions<PricingOptions> pricing,
        StoreMetrics metrics,
        TimeProvider time,
        DeliveryEstimator delivery,
        ILogger<OrderCheckout> logger)
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

    public async Task<OrderResponse> CreateOrderFromCartAsync(CreateOrderFromCartRequest request, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        var requestHash = idempotencyKey is null ? null : HashRequest(request);
        if (idempotencyKey is not null)
        {
            // A retry of an answered checkout gets the same order back, before any work is done
            var replayed = await FindAnsweredAsync(idempotencyKey, request.UserId, requestHash!, cancellationToken);
            if (replayed is not null)
            {
                return replayed;
            }
        }

        var cart = await _cart.GetSnapshotAsync(request.UserId, cancellationToken);
        if (cart is null || cart.Lines.Count == 0)
        {
            throw new DomainValidationException("The cart is empty");
        }

        // Price the lines from the catalogue as it is now; the cart's prices may be stale
        var lines = new List<OrderLine>(cart.Lines.Count);
        var available = new Dictionary<int, int>();
        var repricedLines = 0;
        var productIds = cart.Lines.Select(l => l.ProductId).Distinct().ToArray();
        var snapshots = await Task.WhenAll(productIds.Select(id => _catalog.GetSnapshotAsync(id, cancellationToken)));
        var products = productIds.Zip(snapshots).ToDictionary(pair => pair.First, pair => pair.Second);
        foreach (var line in cart.Lines)
        {
            var product = products[line.ProductId];
            if (product is null || !product.IsActive)
            {
                throw new ConflictException($"\"{line.Title}\" is no longer available. Remove it from the cart to continue.");
            }

            available[product.Id] = product.AvailableQuantity;
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

        // The reservation after checkout is what holds the units; this only spares the customer an
        // order that is sure to be cancelled. A product can be in the cart in two colours.
        foreach (var product in lines.GroupBy(l => l.ProductId))
        {
            var wanted = product.Sum(l => l.Quantity);
            var left = available[product.Key];
            if (wanted > left)
            {
                var title = product.First().ProductTitle;
                throw new ConflictException(left == 0
                    ? $"\"{title}\" has sold out. Remove it from the cart to continue."
                    : $"Only {left} of \"{title}\" left. Lower the quantity to continue.");
            }
        }

        if (repricedLines > 0) _metrics.PriceMismatch(repricedLines, "checkout");

        // The audit service records the order from the OrderPlaced event
        return await PlaceOrderAsync(request, lines, cart, idempotencyKey, requestHash, cancellationToken);
    }

    /// <summary>
    /// Writes the order, the idempotency key and the order-placed event in one transaction.
    /// The customer row is locked first, so concurrent checkouts of the same customer are
    /// serialised: only one of them can be the first order, and a duplicate that waited on the
    /// lock finds the key its twin stored and returns that order instead of creating another.
    /// </summary>
    private async Task<OrderResponse> PlaceOrderAsync(
        CreateOrderFromCartRequest request, List<OrderLine> lines, Store.Contracts.Cart.CartSnapshot cart, string? idempotencyKey, string? requestHash, CancellationToken cancellationToken = default)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var customer = await LockCustomerAsync(request.UserId, cancellationToken);

        if (idempotencyKey is not null)
        {
            var replayed = await FindAnsweredAsync(idempotencyKey, request.UserId, requestHash!, cancellationToken);
            if (replayed is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return replayed;
            }
        }

        var subtotal = lines.Sum(l => l.LineTotal);
        var code = await LockDiscountCodeAsync(request.DiscountCode, subtotal, now, cancellationToken);
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
        await _context.SaveChangesAsync(cancellationToken);

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

        // The saga of the order starts with it, waiting for the stock
        _context.OrderStates.Add(OrderState.StartFor(order.Id, now));

        // Goes to the outbox table with this transaction; the cart, identity and audit
        // services receive it once the transaction is committed
        await _publishEndpoint.Publish(OrderEvents.Placed(order, request.SaveAddress) with { Cart = cart }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _metrics.OrderPlaced(order.Total, order.DiscountReason);
        return OrderService.MapToOrderResponse(order);
    }

    /// <summary>
    /// The customer's row, made on their first checkout and locked until the order commits. An
    /// insert that finds the row already there does nothing, so two first checkouts at once both
    /// end up waiting on the same row.
    /// </summary>
    private async Task<Customer> LockCustomerAsync(string userId, CancellationToken cancellationToken = default)
    {
        var customers = _context.Sql<Customer>();
        var key = customers.Column(c => c.UserId);
        await _context.Database.ExecuteSqlAsync(FormattableStringFactory.Create(
            $"INSERT INTO {customers.Table} ({key}, {customers.Column(c => c.OrdersPlaced)}) VALUES ({{0}}, 0) ON CONFLICT ({key}) DO NOTHING", userId), cancellationToken);
        await _context.LockForUpdateAsync<Customer, string>(c => c.UserId, userId, cancellationToken);
        return await _context.Customers.SingleAsync(c => c.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// The discount code the customer typed, locked until the order commits so its usage limit
    /// holds when many checkouts use it at once, with what it takes off the subtotal. A code
    /// that cannot be used refuses the order with the reason, rather than charging more than the
    /// cart promised.
    /// </summary>
    private async Task<(DiscountCode Entry, decimal Discount)?> LockDiscountCodeAsync(string? typed, decimal subtotal, DateTime now, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(typed))
        {
            return null;
        }

        var normalized = DiscountCode.Normalize(typed);
        await _context.LockForUpdateAsync<DiscountCode, string>(c => c.Code, normalized, cancellationToken);
        var entry = await _context.DiscountCodes.FirstOrDefaultAsync(c => c.Code == normalized, cancellationToken);
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
    private async Task<OrderResponse?> FindAnsweredAsync(string idempotencyKey, string userId, string requestHash, CancellationToken cancellationToken = default)
    {
        var answered = await _context.IdempotencyKeys.AsNoTracking().FirstOrDefaultAsync(k => k.Key == idempotencyKey, cancellationToken);
        if (answered is null)
        {
            return null;
        }

        if (answered.UserId != userId || answered.RequestHash != requestHash)
        {
            throw new DomainValidationException($"{IdempotencyKeyHeader.Name} was already used for a different request");
        }

        var order = await _context.Orders.AsNoTracking().Include(o => o.Lines).Include(o => o.StatusHistory).SingleAsync(o => o.Id == answered.OrderId, cancellationToken);
        _logger.LogInformation("Checkout with idempotency key {Key} replayed order {OrderId}", idempotencyKey, order.Id);
        return OrderService.MapToOrderResponse(order);
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

}
