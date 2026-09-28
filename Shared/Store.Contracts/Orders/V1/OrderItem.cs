namespace Store.Contracts.Orders.V1;

/// <summary>One product line of an order, as the events after checkout carry it.</summary>
/// <param name="ProductId">Catalogue product id</param>
/// <param name="ProductTitle">Title at the time of the order</param>
/// <param name="Quantity">Units</param>
/// <param name="UnitPrice">Price per unit charged</param>
public sealed record OrderItem(int ProductId, string ProductTitle, int Quantity, decimal UnitPrice);
