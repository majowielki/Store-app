namespace Store.Contracts.Payments.V1;

/// <summary>
/// Published by the order service after a verified webhook from the payment service says the
/// payment of an order went through. Consumed by the order saga, which marks the order paid
/// (or refunds the money when the order was cancelled meanwhile). Published through the outbox
/// together with the record of the processed webhook, so it is delivered at least once.
/// </summary>
/// <param name="PaymentId">Id of the payment in the payment service</param>
/// <param name="OrderId">Order it pays for</param>
/// <param name="Amount">Amount taken</param>
/// <param name="CardBrand">Brand of the card (visa, mastercard)</param>
/// <param name="CardLast4">Last four digits of the card; the full number never leaves the payment service</param>
/// <param name="AcceptedAt">When the payment service accepted it (UTC)</param>
public sealed record PaymentAccepted(
    Guid PaymentId,
    int OrderId,
    decimal Amount,
    string CardBrand,
    string CardLast4,
    DateTime AcceptedAt);
