using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Api;
using Store.OrderService.Clients;
using Store.OrderService.Data;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Models;
using Store.Shared.MessageBus;
using Store.Shared.Models;
using Store.Shared.Services;
using System.Net;
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
    private readonly IIdentityClient _identity;
    private readonly PricingOptions _pricing;
    private readonly ILogger<OrderService> _logger;
    private readonly IMessageBus? _messageBus;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditLogClient _auditLogClient;

    public OrderService(
        OrderDbContext context,
        ICartClient cart,
        ICatalogClient catalog,
        IIdentityClient identity,
        IOptions<PricingOptions> pricing,
        ILogger<OrderService> logger,
        IHttpContextAccessor httpContextAccessor,
        IAuditLogClient auditLogClient,
        IMessageBus? messageBus = null)
    {
        _context = context;
        _cart = cart;
        _catalog = catalog;
        _identity = identity;
        _pricing = pricing.Value;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _auditLogClient = auditLogClient;
        _messageBus = messageBus;
    }

    public async Task<ApiResponse<OrderResponse>> CreateOrderFromCartAsync(CreateOrderFromCartRequest request)
    {
        try
        {
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

            var order = await PlaceOrderAsync(request, lines);

            await AuditAsync("ORDER_CREATED", order.Id.ToString(), order.UserId,
                new { order.Id, order.Subtotal, order.DiscountAmount, order.DeliveryFee, order.Total, Lines = order.Lines.Count });

            await ClearCartAsync(request.UserId);
            await PublishOrderCreatedEventAsync(order);
            await SaveAddressAsync(request);

            return ApiResponse<OrderResponse>.Success(MapToOrderResponse(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order from cart for user: {UserId}", request.UserId);
            await AuditAsync("ORDER_CREATION_FAILED", null, request.UserId, new { Exception = ex.Message });
            return ApiResponse<OrderResponse>.Error("An error occurred while creating the order.", HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Writes the order in one transaction. The customer row is locked first, so concurrent
    /// orders of the same customer are serialised and only one of them can be the first order.
    /// </summary>
    private async Task<Order> PlaceOrderAsync(CreateOrderFromCartRequest request, List<OrderLine> lines)
    {
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""INSERT INTO "Customers" ("UserId", "OrdersPlaced") VALUES ({request.UserId}, 0) ON CONFLICT ("UserId") DO NOTHING""");
        var customer = await _context.Customers
            .FromSqlInterpolated($"""SELECT * FROM "Customers" WHERE "UserId" = {request.UserId} FOR UPDATE""")
            .SingleAsync();

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
        await transaction.CommitAsync();

        return order;
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

    private async Task ClearCartAsync(string userId)
    {
        try
        {
            await _cart.ClearAsync(userId);
        }
        catch (Exception ex)
        {
            // The order exists; a cart that stays full is reported, not fatal
            _logger.LogError(ex, "Error clearing cart for user: {UserId}", userId);
        }
    }

    private async Task PublishOrderCreatedEventAsync(Order order)
    {
        if (_messageBus == null)
        {
            _logger.LogWarning("MessageBus is not configured, skipping event publishing");
            return;
        }

        try
        {
            await _messageBus.PublishAsync(new OrderCreatedEvent
            {
                OrderId = Guid.NewGuid(),
                UserId = Guid.TryParse(order.UserId, out var userGuid) ? userGuid : Guid.Empty,
                TotalAmount = order.Total,
                Items = order.Lines.Select(l => new OrderItemEvent { Quantity = l.Quantity, Price = l.UnitPrice }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing OrderCreatedEvent for order: {OrderId}", order.Id);
        }
    }

    private async Task SaveAddressAsync(CreateOrderFromCartRequest request)
    {
        if (!request.SaveAddress || string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            return;
        }

        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
        {
            _logger.LogWarning("No Authorization header in the request; the address of user {UserId} is not saved", request.UserId);
            return;
        }

        try
        {
            var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..] : authorization;
            await _identity.SaveAddressAsync(request.DeliveryAddress, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving address to user profile for user: {UserId}", request.UserId);
        }
    }

    private async Task AuditAsync(string action, string? entityId, string userId, object details)
    {
        await _auditLogClient.CreateAuditLogAsync(new AuditLog
        {
            Action = action,
            EntityName = nameof(Order),
            EntityId = entityId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            AdditionalInfo = JsonSerializer.Serialize(new { Source = "OrderService", Details = details }, JsonOptions)
        });
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
