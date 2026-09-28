namespace Store.Contracts.Orders.V1;

/// <summary>
/// Published by the order service when an administrator marks a paid order shipped.
/// Consumers: the product service (the reserved units leave the stock) and the notification
/// service (the shipping e-mail). Published through the outbox, so it is delivered at least
/// once - every consumer must be idempotent.
/// </summary>
/// <param name="OrderId">Id of the order in the order service</param>
/// <param name="UserId">Customer the order belongs to</param>
/// <param name="UserEmail">Customer e-mail as given at checkout</param>
/// <param name="CustomerName">Name as given at checkout</param>
/// <param name="Lines">Product lines</param>
/// <param name="DeliveryFrom">First day of the promised delivery window</param>
/// <param name="DeliveryTo">Last day of the promised delivery window</param>
/// <param name="ShippedAt">When (UTC)</param>
public sealed record OrderShipped(
    int OrderId,
    string UserId,
    string UserEmail,
    string CustomerName,
    IReadOnlyList<OrderItem> Lines,
    DateOnly? DeliveryFrom,
    DateOnly? DeliveryTo,
    DateTime ShippedAt);
