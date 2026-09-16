using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Api;
using Store.Contracts.Orders.V1;
using Store.OrderService.Clients;
using Store.OrderService.Data;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Models;
using System.Net;
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
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrderDbContext context,
        ICartClient cart,
        ICatalogClient catalog,
        IPublishEndpoint publishEndpoint,
        IOptions<PricingOptions> pricing,
        ILogger<OrderService> logger)
    {
        _context = context;
        _cart = cart;
        _catalog = catalog;
        _publishEndpoint = publishEndpoint;
        _pricing = pricing.Value;
        _logger = logger;
    }

    public async Task<ApiResponse<OrderResponse>> CreateOrderFromCartAsync(CreateOrderFromCartRequest request, string? idempotencyKey = null)
    {
        try
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
                return ApiResponse<OrderResponse>.Error("Cart is empty or not found");
            }

            // Price the lines from the catalogue as it is now; the cart's prices may be stale
            var lines = new List<OrderLine>(cart.Lines.Count);
            foreach (var line in cart.Lines)
            {
                var product = await _catalog.GetSnapshotAsync(line.ProductId);
                if (product is null || !product.IsActive)
                {
                    return ApiResponse<OrderResponse>.Error(
                        $"\"{line.Title}\" is no longer available. Remove it from the cart to continue.",
                        HttpStatusCode.Conflict);
                }

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

            var (order, replayedAfterLock) = await PlaceOrderAsync(request, lines, idempotencyKey, requestHash);
            if (replayedAfterLock is not null)
            {
                return replayedAfterLock;
            }

            // The audit service records the order from the OrderPlaced event
            return ApiResponse<OrderResponse>.Success(MapToOrderResponse(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order from cart for user: {UserId}", request.UserId);
            return ApiResponse<OrderResponse>.Error("An error occurred while creating the order.", HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Writes the order, the idempotency key and the order-placed event in one transaction.
    /// The customer row is locked first, so concurrent checkouts of the same customer are
    /// serialised: only one of them can be the first order, and a duplicate that waited on the
    /// lock finds the key its twin stored and returns that order instead of creating another.
    /// </summary>
    private async Task<(Order Order, ApiResponse<OrderResponse>? Replayed)> PlaceOrderAsync(
        CreateOrderFromCartRequest request, List<OrderLine> lines, string? idempotencyKey, string? requestHash)
    {
        var now = DateTime.UtcNow;

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
                return (null!, replayed);
            }
        }

        var totals = PricingPolicy.Calculate(lines.Sum(l => l.LineTotal), isFirstOrder: customer.OrdersPlaced == 0, _pricing);

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
            CreatedAt = now
        };

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

        return (order, null);
    }

    /// <summary>The response an earlier request with this key received, or null when the key is new.</summary>
    private async Task<ApiResponse<OrderResponse>?> FindAnsweredAsync(string idempotencyKey, string userId, string requestHash)
    {
        var answered = await _context.IdempotencyKeys.AsNoTracking().FirstOrDefaultAsync(k => k.Key == idempotencyKey);
        if (answered is null)
        {
            return null;
        }

        if (answered.UserId != userId || answered.RequestHash != requestHash)
        {
            return ApiResponse<OrderResponse>.Error(
                "Idempotency-Key was already used for a different request",
                HttpStatusCode.UnprocessableEntity);
        }

        var order = await _context.Orders.AsNoTracking().Include(o => o.Lines).SingleAsync(o => o.Id == answered.OrderId);
        _logger.LogInformation("Checkout with Idempotency-Key {Key} replayed order {OrderId}", idempotencyKey, order.Id);
        return ApiResponse<OrderResponse>.Success(MapToOrderResponse(order));
    }

    private static string HashRequest(CreateOrderFromCartRequest request)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            request.UserEmail,
            request.CustomerName,
            request.DeliveryAddress,
            request.Notes,
            request.SaveAddress
        }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public async Task<ApiResponse<OrderResponse?>> GetOrderByIdAsync(int orderId, string userId)
    {
        try
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return ApiResponse<OrderResponse?>.Error("Order not found", HttpStatusCode.NotFound);
            }

            // Users only see their own orders; admins go through GetOrderByIdForAdminAsync
            if (order.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to access order {OrderId} belonging to {OrderUserId}",
                    userId, orderId, order.UserId);
                return ApiResponse<OrderResponse?>.Error("Unauthorized", HttpStatusCode.Forbidden);
            }

            return ApiResponse<OrderResponse?>.Success(MapToOrderResponse(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order: {OrderId}", orderId);
            return ApiResponse<OrderResponse?>.Error("An error occurred while retrieving the order.", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiResponse<OrderResponse?>> GetOrderByIdForAdminAsync(int orderId)
    {
        try
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return ApiResponse<OrderResponse?>.Error("Order not found", HttpStatusCode.NotFound);
            }

            return ApiResponse<OrderResponse?>.Success(MapToOrderResponse(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order for admin: {OrderId}", orderId);
            return ApiResponse<OrderResponse?>.Error("An error occurred while retrieving the order.", HttpStatusCode.InternalServerError);
        }
    }

    public Task<ApiResponse<OrderListResponse>> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 20)
        => ListOrdersAsync(_context.Orders.Where(o => o.UserId == userId), page, pageSize);

    public Task<ApiResponse<OrderListResponse>> GetOrdersByUserIdAsync(string userId, int page = 1, int pageSize = 20)
        => GetUserOrdersAsync(userId, page, pageSize);

    public Task<ApiResponse<OrderListResponse>> GetAllOrdersAsync(int page = 1, int pageSize = 20)
        => ListOrdersAsync(_context.Orders, page, pageSize);

    private async Task<ApiResponse<OrderListResponse>> ListOrdersAsync(IQueryable<Order> query, int page, int pageSize)
    {
        try
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var totalCount = await query.CountAsync();

            var orders = await query
                .AsNoTracking()
                .Include(o => o.Lines)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return ApiResponse<OrderListResponse>.Success(new OrderListResponse
            {
                Orders = orders.Select(MapToOrderResponse).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing orders");
            return ApiResponse<OrderListResponse>.Error("An error occurred while retrieving orders.", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiResponse<int>> GetUserOrdersCountAsync(string userId)
    {
        try
        {
            var count = await _context.Orders.CountAsync(o => o.UserId == userId);
            return ApiResponse<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order count for user: {UserId}", userId);
            return ApiResponse<int>.Error("An error occurred while getting order count.", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiResponse<OrderStatsResponse>> GetOrderStatsAsync(int daysWindow = 30)
    {
        try
        {
            var since = DateTime.UtcNow.Date.AddDays(-Math.Abs(daysWindow));
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

            return ApiResponse<OrderStatsResponse>.Success(new OrderStatsResponse
            {
                TotalOrders = daily.Sum(d => d.Orders),
                TotalRevenue = daily.Sum(d => d.Revenue),
                Daily = daily,
                Weekly = weekly,
                TopProducts = topProducts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order stats");
            return ApiResponse<OrderStatsResponse>.Error("An error occurred while getting order stats.", HttpStatusCode.InternalServerError);
        }
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
            DeliveryFee = order.DeliveryFee,
            Total = order.Total,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            Notes = order.Notes
        };
    }
}
