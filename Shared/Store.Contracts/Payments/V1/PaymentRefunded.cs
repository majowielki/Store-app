namespace Store.Contracts.Payments.V1;

/// <summary>
/// Published by the order service after a verified webhook from the payment service says the
/// money of an order was returned. Consumed by the order saga, which marks the order refunded.
/// Published through the outbox, so it is delivered at least once.
/// </summary>
/// <param name="PaymentId">Id of the payment in the payment service</param>
/// <param name="OrderId">Order the payment was for</param>
/// <param name="Amount">Amount returned</param>
/// <param name="RefundedAt">When the payment service refunded it (UTC)</param>
public sealed record PaymentRefunded(Guid PaymentId, int OrderId, decimal Amount, DateTime RefundedAt);
