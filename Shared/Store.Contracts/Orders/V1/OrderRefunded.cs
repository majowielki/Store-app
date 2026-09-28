namespace Store.Contracts.Orders.V1;

/// <summary>
/// Published by the order service when the payment provider has returned the money of a
/// cancelled order that had been paid. Published through the outbox, so it is delivered at
/// least once - every consumer must be idempotent.
/// </summary>
/// <param name="OrderId">Id of the order in the order service</param>
/// <param name="UserId">Customer the order belongs to</param>
/// <param name="Amount">Amount returned</param>
/// <param name="RefundedAt">When the provider confirmed the refund (UTC)</param>
public sealed record OrderRefunded(int OrderId, string UserId, decimal Amount, DateTime RefundedAt);
