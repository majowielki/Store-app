using MassTransit;
using Store.Contracts.Catalog.V1;
using Store.Contracts.Orders.V1;
using Store.Contracts.Payments.V1;

namespace Store.OrderService.Saga;

/// <summary>
/// The life of an order after checkout (ADR 013). The main road: reserving stock, awaiting
/// payment once it is reserved, paid once the payment is accepted, shipped by the administrator.
/// Out of stock, or no payment before the deadline, ends in cancelled; a paid order the
/// administrator cancels, or a payment that arrives for a cancelled one, goes through refunding
/// to refunded.
/// A refused card leaves the order waiting, so the customer can try another one until the
/// deadline. The administrator's own moves (ship, cancel) change the order at once and reach the
/// saga as the events they publish. Anything arriving in a state that does not expect it - a
/// duplicate, or news about an order that has moved on - is ignored.
/// </summary>
public sealed class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State ReservingStock { get; private set; } = null!;
    public State AwaitingPayment { get; private set; } = null!;
    public State Paid { get; private set; } = null!;
    public State Shipped { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;
    public State Refunding { get; private set; } = null!;
    public State Refunded { get; private set; } = null!;

    public Event<StockReserved> StockReserved { get; private set; } = null!;
    public Event<StockUnavailable> StockUnavailable { get; private set; } = null!;
    public Event<PaymentAccepted> PaymentAccepted { get; private set; } = null!;
    public Event<PaymentRefunded> PaymentRefunded { get; private set; } = null!;
    public Event<OrderPaymentTimedOut> PaymentTimedOut { get; private set; } = null!;
    public Event<OrderShipped> OrderShipped { get; private set; } = null!;
    public Event<OrderCancelled> OrderCancelled { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(order => order.CurrentState);

        // Every event names its order; the saga row was written with the order at checkout,
        // so an event without one is about an order older than the saga and is dropped
        Event(() => StockReserved, e => ByOrder(e, m => m.OrderId));
        Event(() => StockUnavailable, e => ByOrder(e, m => m.OrderId));
        Event(() => PaymentAccepted, e => ByOrder(e, m => m.OrderId));
        Event(() => PaymentRefunded, e => ByOrder(e, m => m.OrderId));
        Event(() => PaymentTimedOut, e => ByOrder(e, m => m.OrderId));
        Event(() => OrderShipped, e => ByOrder(e, m => m.OrderId));
        Event(() => OrderCancelled, e => ByOrder(e, m => m.OrderId));

        During(ReservingStock,
            When(StockReserved)
                .IfElseAsync(context => Actions(context).AwaitPaymentAsync(context.Saga),
                    reserved => reserved.TransitionTo(AwaitingPayment),
                    cancelledMeanwhile => cancelledMeanwhile.TransitionTo(Cancelled)),
            When(StockUnavailable)
                .ThenAsync(context => Actions(context).CancelAsync(context.Saga, OrderCancellationReasons.OutOfStock))
                .TransitionTo(Cancelled),
            When(OrderCancelled)
                .Then(context => Actions(context).Touch(context.Saga))
                .TransitionTo(Cancelled));

        During(AwaitingPayment,
            When(PaymentAccepted)
                .IfElseAsync(context => Actions(context).MarkPaidAsync(context.Saga, context.Message),
                    paid => paid.TransitionTo(Paid),
                    cancelledMeanwhile => cancelledMeanwhile.TransitionTo(Refunding)),
            When(PaymentTimedOut)
                .ThenAsync(context => Actions(context).CancelAsync(context.Saga, OrderCancellationReasons.PaymentTimedOut))
                .TransitionTo(Cancelled),
            When(OrderCancelled)
                .Then(context => Actions(context).Touch(context.Saga))
                .TransitionTo(Cancelled));

        During(Paid,
            When(OrderShipped)
                .Then(context => Actions(context).Touch(context.Saga))
                .TransitionTo(Shipped),
            When(OrderCancelled)
                .ThenAsync(context => Actions(context).RequestRefundAsync(context.Saga, paymentId: null, amount: null, RefundReasons.OrderCancelled))
                .TransitionTo(Refunding));

        During(Cancelled,
            When(PaymentAccepted)
                .ThenAsync(context => Actions(context).RequestRefundAsync(
                    context.Saga, context.Message.PaymentId, context.Message.Amount, RefundReasons.PaidAfterCancellation))
                .TransitionTo(Refunding));

        During(Refunding,
            When(PaymentRefunded)
                .ThenAsync(context => Actions(context).MarkRefundedAsync(context.Saga, context.Message))
                .TransitionTo(Refunded));

        OnUnhandledEvent(context => context.Ignore());
    }

    private static void ByOrder<T>(IEventCorrelationConfigurator<OrderState, T> e, Func<T, int> orderId) where T : class
    {
        e.CorrelateById(context => OrderSagaIds.For(orderId(context.Message)));
        e.OnMissingInstance(missing => missing.Discard());
    }

    private static OrderSagaActions Actions<T>(BehaviorContext<OrderState, T> context) where T : class
        => context.GetServiceOrCreateInstance<OrderSagaActions>();
}
