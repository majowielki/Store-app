namespace Store.Contracts.Orders.V1;

/// <summary>
/// Published by the order service when an order is cancelled: out of stock, not paid in time
/// or cancelled by an administrator. Consumers: the product service (releases the stock the
/// order holds) and the payment service (a payment still waiting for a card can no longer be
/// made). A paid order is refunded afterwards with <see cref="Payments.V1.PaymentRefundRequested"/>.
/// Published through the outbox, so it is delivered at least once - every consumer must be idempotent.
/// </summary>
/// <param name="OrderId">Id of the order in the order service</param>
/// <param name="UserId">Customer the order belongs to</param>
/// <param name="Reason">One of <see cref="OrderCancellationReasons"/></param>
/// <param name="CancelledAt">When (UTC)</param>
public sealed record OrderCancelled(int OrderId, string UserId, string Reason, DateTime CancelledAt);

/// <summary>Why an order was cancelled, as <see cref="OrderCancelled.Reason"/> carries it.</summary>
public static class OrderCancellationReasons
{
    /// <summary>The stock could not be reserved for every line.</summary>
    public const string OutOfStock = "out-of-stock";

    /// <summary>No payment arrived before the payment deadline.</summary>
    public const string PaymentTimedOut = "payment-timed-out";

    /// <summary>An administrator cancelled it.</summary>
    public const string ByAdministrator = "cancelled-by-administrator";
}
