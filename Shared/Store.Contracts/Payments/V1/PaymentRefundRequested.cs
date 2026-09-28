namespace Store.Contracts.Payments.V1;

/// <summary>
/// Published by the order saga when a paid order must give the money back: an administrator
/// cancelled it, or its payment arrived after it had been cancelled. Consumed by the payment
/// service, which refunds the payment of the order and reports it with a webhook. Published
/// through the outbox, so it is delivered at least once - a second request for a refunded
/// payment changes nothing.
/// </summary>
/// <param name="OrderId">Order whose payment is refunded</param>
/// <param name="PaymentId">The payment, when the order service knows it</param>
/// <param name="Amount">Amount to return</param>
/// <param name="Reason">One of <see cref="RefundReasons"/></param>
/// <param name="RequestedAt">When (UTC)</param>
public sealed record PaymentRefundRequested(int OrderId, Guid? PaymentId, decimal Amount, string Reason, DateTime RequestedAt);

/// <summary>Why a refund was requested, as <see cref="PaymentRefundRequested.Reason"/> carries it.</summary>
public static class RefundReasons
{
    /// <summary>An administrator cancelled a paid order.</summary>
    public const string OrderCancelled = "order-cancelled";

    /// <summary>The payment arrived after the order had been cancelled.</summary>
    public const string PaidAfterCancellation = "paid-after-cancellation";
}
