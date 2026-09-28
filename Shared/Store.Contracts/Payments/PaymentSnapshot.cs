namespace Store.Contracts.Payments;

/// <summary>
/// Body of <c>POST /api/v1/payments/internal</c>, with which the order service opens the payment
/// of an order (an internal call, with the key "order-{id}" in the Idempotency-Key header). One
/// payment per order: asking again returns the payment made the first time.
/// </summary>
/// <param name="OrderId">The order to be paid</param>
/// <param name="UserId">The customer; only they may pay it</param>
/// <param name="Amount">What the order costs</param>
/// <param name="Currency">ISO code, lowercase ("usd")</param>
public sealed record CreatePaymentRequest(int OrderId, string UserId, decimal Amount, string Currency);

/// <summary>What the payment service tells the order service about a payment.</summary>
/// <param name="Id">Id of the payment; the browser confirms it with a card at the payment service</param>
/// <param name="OrderId">The order it pays for</param>
/// <param name="Amount">Amount to take</param>
/// <param name="Currency">ISO code, lowercase</param>
/// <param name="Status">requiresPaymentMethod, requiresAction, succeeded, cancelled or refunded</param>
/// <param name="CreatedAt">When it was opened (UTC)</param>
public sealed record PaymentSnapshot(Guid Id, int OrderId, decimal Amount, string Currency, string Status, DateTime CreatedAt);
