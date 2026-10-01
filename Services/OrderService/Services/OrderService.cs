using MassTransit;
using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.Contracts.Orders.V1;
using Store.OrderService.Data;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Models;

namespace Store.OrderService.Services;

public class OrderService : IOrderService
{
    private readonly OrderCheckout _checkout;
    private readonly OrderStatistics _statistics;
    private readonly OrderDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly TimeProvider _time;
    private readonly OrderStatusWriter _writer;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrderDbContext context,
        OrderCheckout checkout,
        OrderStatistics statistics,
        IPublishEndpoint publishEndpoint,
        TimeProvider time,
        OrderStatusWriter writer,
        ILogger<OrderService> logger)
    {
        _checkout = checkout;
        _statistics = statistics;
        _context = context;
        _publishEndpoint = publishEndpoint;
        _time = time;
        _writer = writer;
        _logger = logger;
    }

    public Task<OrderResponse> CreateOrderFromCartAsync(CreateOrderFromCartRequest request, string? idempotencyKey = null, CancellationToken cancellationToken = default)
        => _checkout.CreateOrderFromCartAsync(request, idempotencyKey, cancellationToken);

    public async Task<OrderResponse> GetOrderAsync(int orderId, string userId, CancellationToken cancellationToken = default)
    {
        var order = await FindOrderAsync(orderId, cancellationToken);

        // Customers only see their own orders; admins go through GetOrderForAdminAsync
        if (order.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to access order {OrderId} belonging to {OrderUserId}",
                userId, orderId, order.UserId);
            throw new ForbiddenException("This order belongs to another customer");
        }

        return MapToOrderResponse(order);
    }

    public async Task<OrderResponse> GetOrderForAdminAsync(int orderId, CancellationToken cancellationToken = default)
        => MapToOrderResponse(await FindOrderAsync(orderId, cancellationToken));

    /// <summary>
    /// The administrator ships a paid order or cancels one that is not shipped yet; paying and
    /// refunding belong to the saga. The change goes through <see cref="OrderStatusWriter"/>, and
    /// the event it publishes (shipped, cancelled) takes it to the stock, the payment service and
    /// the saga - which refunds a cancelled order that had been paid.
    /// </summary>
    public async Task<OrderResponse> ChangeStatusAsync(int orderId, OrderStatus status, string actorId, CancellationToken cancellationToken = default)
    {
        if (!OrderStatusFlow.IsAdministratorMove(status))
        {
            throw new DomainValidationException("An order is shipped or cancelled by hand; payments move it on their own.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var move = await _writer.MoveAsync(orderId, status, actorId, order =>
        {
            if (status == OrderStatus.Cancelled) order.CancellationReason = OrderCancellationReasons.ByAdministrator;
        }, cancellationToken: cancellationToken);
        var order = move.Order;
        if (!move.Moved)
        {
            throw new ConflictException(move.Previous == status
                ? $"The order is already {Describe(status)}."
                : $"An order that is {Describe(move.Previous)} cannot be marked {Describe(status)}.");
        }

        var now = _time.GetUtcNow().UtcDateTime;
        if (status == OrderStatus.Shipped)
        {
            await _publishEndpoint.Publish(OrderEvents.Shipped(order, now), cancellationToken);
        }
        else
        {
            await _publishEndpoint.Publish(OrderEvents.Cancelled(order, OrderCancellationReasons.ByAdministrator, now), cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return MapToOrderResponse(order);
    }

    /// <summary>"awaiting payment" for AwaitingPayment.</summary>
    private static string Describe(OrderStatus status)
        => string.Concat(status.ToString().Select((c, i) => i > 0 && char.IsUpper(c) ? " " + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

    private async Task<Order> FindOrderAsync(int orderId, CancellationToken cancellationToken = default)
        => await _context.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), orderId);

    public Task<PagedResponse<OrderResponse>> GetUserOrdersAsync(string userId, PagedQuery paging, CancellationToken cancellationToken = default)
        => ListOrdersAsync(_context.Orders.Where(o => o.UserId == userId), paging, cancellationToken);

    public Task<PagedResponse<OrderResponse>> GetAllOrdersAsync(PagedQuery paging, CancellationToken cancellationToken = default)
        => ListOrdersAsync(_context.Orders, paging, cancellationToken);

    private static async Task<PagedResponse<OrderResponse>> ListOrdersAsync(IQueryable<Order> query, PagedQuery paging, CancellationToken cancellationToken = default)
    {
        paging = paging.Normalized();
        var totalCount = await query.CountAsync(cancellationToken);

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
            .ToListAsync(cancellationToken);

        return new PagedResponse<OrderResponse>(orders.Select(MapToOrderResponse).ToList(), totalCount, paging);
    }

    public Task<int> GetUserOrdersCountAsync(string userId, CancellationToken cancellationToken = default)
        => _context.Orders.CountAsync(o => o.UserId == userId, cancellationToken);

    public Task<OrderStatsResponse> GetOrderStatsAsync(int daysWindow = StatisticsWindow.DefaultDays, CancellationToken cancellationToken = default)
        => _statistics.GetOrderStatsAsync(daysWindow, cancellationToken);

    internal static OrderResponse MapToOrderResponse(Order order)
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
            NextStatuses = OrderStatusFlow.AdministratorMovesFrom(order.Status).Select(s => s.ToString()).ToList(),
            DeliveryFrom = order.DeliveryFrom,
            DeliveryTo = order.DeliveryTo,
            PaymentDueAt = order.PaymentDueAt,
            CardBrand = order.CardBrand,
            CardLast4 = order.CardLast4,
            CancellationReason = order.CancellationReason,
            CreatedAt = order.CreatedAt,
            Notes = order.Notes
        };
    }
}
