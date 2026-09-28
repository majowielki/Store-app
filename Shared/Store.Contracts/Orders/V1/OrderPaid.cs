namespace Store.Contracts.Orders.V1;

/// <summary>
/// Published by the order service when the payment of an order has been confirmed by the
/// payment provider's webhook. Consumers: the review service (the customer may now review the
/// products) and the notification service (the confirmation e-mail). Published through the
/// outbox, so it is delivered at least once - every consumer must be idempotent.
/// </summary>
/// <param name="OrderId">Id of the order in the order service</param>
/// <param name="UserId">Customer who paid</param>
/// <param name="UserEmail">Customer e-mail as given at checkout</param>
/// <param name="CustomerName">Name as given at checkout</param>
/// <param name="Total">Amount paid</param>
/// <param name="CardBrand">Brand of the card that paid (visa, mastercard), when known</param>
/// <param name="CardLast4">Last four digits of that card, when known</param>
/// <param name="Lines">Product lines</param>
/// <param name="DeliveryFrom">First day of the promised delivery window</param>
/// <param name="DeliveryTo">Last day of the promised delivery window</param>
/// <param name="PaidAt">When the payment was confirmed (UTC)</param>
public sealed record OrderPaid(
    int OrderId,
    string UserId,
    string UserEmail,
    string CustomerName,
    decimal Total,
    string? CardBrand,
    string? CardLast4,
    IReadOnlyList<OrderItem> Lines,
    DateOnly? DeliveryFrom,
    DateOnly? DeliveryTo,
    DateTime PaidAt);
