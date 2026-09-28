using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.Contracts.Orders.V1;
using Store.Contracts.Payments.V1;
using Store.OrderService.Data;
using Store.OrderService.Models;
using Store.OrderService.Services;

namespace Store.OrderService.Saga;

/// <summary>
/// What the order saga does to the order itself. Each step moves the order through
/// <see cref="OrderStatusWriter"/>, which checks the move against the status the order has at
/// that moment - the administrator may have cancelled it in between - and says whether it moved;
/// the saga picks its next state from that. Everything runs in the transaction of the message.
/// </summary>
public sealed class OrderSagaActions
{
    private readonly OrderStatusWriter _writer;
    private readonly OrderDbContext _context;
    private readonly IPublishEndpoint _publish;
    private readonly TimeProvider _time;
    private readonly OrderSagaOptions _options;

    public OrderSagaActions(OrderStatusWriter writer, OrderDbContext context, IPublishEndpoint publish, TimeProvider time, IOptions<OrderSagaOptions> options)
    {
        _writer = writer;
        _context = context;
        _publish = publish;
        _time = time;
        _options = options.Value;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    /// <summary>The stock is held: the customer has the payment window to pay. False when the order was cancelled meanwhile.</summary>
    public async Task<bool> AwaitPaymentAsync(OrderState saga)
    {
        var due = Now + _options.PaymentWindow;
        var move = await _writer.MoveAsync(saga.OrderId, OrderStatus.AwaitingPayment, actorId: null, order => order.PaymentDueAt = due);
        if (move.Moved)
        {
            saga.PaymentDueAt = due;
        }

        Touch(saga);
        return move.Moved;
    }

    /// <summary>Cancels the order and tells the stock and the payment service; nothing to do when it is cancelled already.</summary>
    public async Task CancelAsync(OrderState saga, string reason)
    {
        var move = await _writer.MoveAsync(saga.OrderId, OrderStatus.Cancelled, actorId: null, order => order.CancellationReason = reason);
        if (move.Moved)
        {
            await _publish.Publish(new OrderCancelled(saga.OrderId, move.Order.UserId, reason, Now));
        }

        Touch(saga);
    }

    /// <summary>
    /// The payment went through: the order is paid. False when it had been cancelled meanwhile -
    /// then the money is on its way back.
    /// </summary>
    public async Task<bool> MarkPaidAsync(OrderState saga, PaymentAccepted payment)
    {
        var move = await _writer.MoveAsync(saga.OrderId, OrderStatus.Paid, actorId: null, order =>
        {
            order.PaymentId = payment.PaymentId;
            order.CardBrand = payment.CardBrand;
            order.CardLast4 = payment.CardLast4;
        });

        if (move.Moved)
        {
            await _publish.Publish(OrderEvents.Paid(move.Order, Now));
            Touch(saga);
            return true;
        }

        await RequestRefundAsync(saga, payment.PaymentId, payment.Amount, RefundReasons.PaidAfterCancellation);
        return false;
    }

    /// <summary>Asks the payment service to return the money of the order (all of it, unless <paramref name="amount"/> says otherwise).</summary>
    public async Task RequestRefundAsync(OrderState saga, Guid? paymentId, decimal? amount, string reason)
    {
        var order = await _context.Orders.AsNoTracking().SingleAsync(o => o.Id == saga.OrderId);
        await _publish.Publish(new PaymentRefundRequested(saga.OrderId, paymentId ?? order.PaymentId, amount ?? order.Total, reason, Now));
        Touch(saga);
    }

    /// <summary>The money is back with the customer: the cancelled order is refunded.</summary>
    public async Task MarkRefundedAsync(OrderState saga, PaymentRefunded refund)
    {
        var move = await _writer.MoveAsync(saga.OrderId, OrderStatus.Refunded, actorId: null);
        if (move.Moved)
        {
            await _publish.Publish(new OrderRefunded(saga.OrderId, move.Order.UserId, refund.Amount, Now));
        }

        Touch(saga);
    }

    /// <summary>For a change the saga only follows (the administrator shipped or cancelled the order).</summary>
    public void Touch(OrderState saga) => saga.UpdatedAt = Now;
}
