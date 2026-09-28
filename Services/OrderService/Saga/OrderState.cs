using MassTransit;

namespace Store.OrderService.Saga;

/// <summary>
/// Where the order saga stands for one order. The row is written with the order at checkout, in
/// <see cref="OrderStateMachine.ReservingStock"/>, so every event about the order finds it; the
/// customer-facing status stays on the order itself.
/// </summary>
public class OrderState : SagaStateMachineInstance
{
    /// <summary>Derived from the order id (<see cref="OrderSagaIds.For"/>).</summary>
    public Guid CorrelationId { get; set; }

    /// <summary>Name of the state machine's state.</summary>
    public string CurrentState { get; set; } = string.Empty;

    public int OrderId { get; set; }

    /// <summary>When the payment deadline passes; the deadline job looks for the orders past it.</summary>
    public DateTime? PaymentDueAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>The saga of an order just placed, waiting for its stock: whatever event about the order comes first finds it.</summary>
    public static OrderState StartFor(int orderId, DateTime now) => new()
    {
        CorrelationId = OrderSagaIds.For(orderId),
        CurrentState = nameof(OrderStateMachine.ReservingStock),
        OrderId = orderId,
        CreatedAt = now,
        UpdatedAt = now
    };
}

/// <summary>The saga of an order is found by the order id every event carries.</summary>
public static class OrderSagaIds
{
    /// <summary>"order" in the last bytes, the order id in the first four: the same Guid for the same order, always.</summary>
    public static Guid For(int orderId) => new(orderId, 0, 0, [0x6f, 0x72, 0x64, 0x65, 0x72, 0, 0, 0]);
}

/// <summary>
/// Sent by <see cref="PaymentDeadlineService"/> for an order still waiting for its payment after
/// the deadline. Internal to the order service.
/// </summary>
/// <param name="OrderId">The order</param>
/// <param name="DueAt">The deadline that passed</param>
public sealed record OrderPaymentTimedOut(int OrderId, DateTime DueAt);
