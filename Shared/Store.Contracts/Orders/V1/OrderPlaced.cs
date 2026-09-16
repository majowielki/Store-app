namespace Store.Contracts.Orders.V1;

/// <summary>
/// Published by the order service once an order is committed. Consumers: the cart service
/// (empties the cart), the identity service (stores the delivery address when asked to) and
/// the audit service. Published through the outbox, so it is delivered at least once - every
/// consumer must be idempotent.
/// </summary>
/// <param name="OrderId">Id of the order in the order service</param>
/// <param name="UserId">Customer who placed it</param>
/// <param name="UserEmail">Customer e-mail as given at checkout</param>
/// <param name="CustomerName">Name as given at checkout</param>
/// <param name="DeliveryAddress">Delivery address as given at checkout</param>
/// <param name="SaveAddress">The customer asked for the address to be stored in their profile</param>
/// <param name="Subtotal">Sum of the lines before discount and delivery</param>
/// <param name="DiscountAmount">Discount granted on this order</param>
/// <param name="DeliveryFee">Delivery fee charged</param>
/// <param name="Total">Amount the customer pays</param>
/// <param name="Lines">Product lines</param>
/// <param name="PlacedAt">When the order was committed (UTC)</param>
public sealed record OrderPlaced(
    int OrderId,
    string UserId,
    string UserEmail,
    string CustomerName,
    string? DeliveryAddress,
    bool SaveAddress,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal DeliveryFee,
    decimal Total,
    IReadOnlyList<OrderPlacedLine> Lines,
    DateTime PlacedAt);

/// <summary>One product line of a placed order.</summary>
/// <param name="ProductId">Catalogue product id</param>
/// <param name="ProductTitle">Title at the time of the order</param>
/// <param name="Quantity">Units</param>
/// <param name="UnitPrice">Price per unit charged</param>
public sealed record OrderPlacedLine(int ProductId, string ProductTitle, int Quantity, decimal UnitPrice);
