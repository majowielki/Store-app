using MassTransit;
using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Persistence;
using Store.Contracts.Orders.V1;
using Store.OrderService.Data;
using Store.OrderService.Models;

namespace Store.OrderService.Services;

/// <summary>What a status change did: the order after it, the status it had before, whether it moved.</summary>
public readonly record struct StatusMove(Order Order, OrderStatus Previous, bool Moved);

/// <summary>
/// The one way an order changes status, for the administrator and the saga alike. The order row
/// is locked until the caller's transaction ends, so a payment and a cancellation arriving at the
/// same moment are applied one after the other, each seeing what the other did; the move must be
/// one <see cref="OrderStatusFlow"/> allows from the status the order has then. The history row
/// and <see cref="OrderStatusChanged"/> for the audit go with the change.
/// </summary>
public sealed class OrderStatusWriter
{
    private readonly OrderDbContext _context;
    private readonly IPublishEndpoint _publish;
    private readonly TimeProvider _time;
    private readonly ILogger<OrderStatusWriter> _logger;

    public OrderStatusWriter(OrderDbContext context, IPublishEndpoint publish, TimeProvider time, ILogger<OrderStatusWriter> logger)
    {
        _context = context;
        _publish = publish;
        _time = time;
        _logger = logger;
    }

    /// <summary>
    /// Moves the order to <paramref name="to"/> and applies <paramref name="change"/> to it, or
    /// leaves it as it is when its current status does not allow the move (<see cref="StatusMove.Moved"/>
    /// is false then). Must run inside a transaction: the caller's, which it saves into.
    /// </summary>
    /// <param name="orderId">The order</param>
    /// <param name="to">The status to move it to</param>
    /// <param name="actorId">The administrator making the change; null for the saga</param>
    /// <param name="change">What else changes with the status (the payment deadline, the card...)</param>
    /// <param name="cancellationToken">Cancellation</param>
    public async Task<StatusMove> MoveAsync(int orderId, OrderStatus to, string? actorId, Action<Order>? change = null, CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("An order changes status inside a transaction; the row lock lasts until it ends.");
        }

        await _context.LockForUpdateAsync<Order, int>(o => o.Id, orderId, cancellationToken);
        var order = await _context.Orders
            .Include(o => o.Lines)
            .Include(o => o.StatusHistory)
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), orderId);

        var previous = order.Status;
        if (!OrderStatusFlow.CanMove(previous, to))
        {
            _logger.LogInformation("Order {OrderId} is {Status}; it does not move to {To}", orderId, previous, to);
            return new StatusMove(order, previous, Moved: false);
        }

        change?.Invoke(order);
        if (to == OrderStatus.Cancelled && order.DiscountCode is { } used)
        {
            await ReleaseDiscountCodeAsync(used, cancellationToken);
        }

        var now = _time.GetUtcNow().UtcDateTime;
        order.Status = to;
        order.StatusHistory.Add(new OrderStatusChange { Status = to, ChangedAt = now, ChangedBy = actorId });
        await _publish.Publish(new OrderStatusChanged(order.Id, order.UserId, previous.ToString(), to.ToString(), actorId, now), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} moved from {Previous} to {Status} by {ActorId}", order.Id, previous, to, actorId ?? "the saga");
        return new StatusMove(order, previous, Moved: true);
    }

    /// <summary>
    /// Gives a cancelled order's code its use back, so a code limited to a few orders is not used
    /// up by orders that never went through. The order keeps naming the code it was placed with.
    /// </summary>
    private async Task ReleaseDiscountCodeAsync(string code, CancellationToken cancellationToken)
    {
        await _context.LockForUpdateAsync<DiscountCode, string>(c => c.Code, code, cancellationToken);
        var entry = await _context.DiscountCodes.FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
        if (entry is { TimesUsed: > 0 })
        {
            entry.TimesUsed--;
        }
    }
}
