namespace Store.Contracts.Cart;

/// <summary>
/// A customer's cart as another service (the order service) reads it through
/// <c>GET /api/cart/internal/{userId}</c>. Prices are what the cart last took from the
/// catalogue; the order service re-prices the lines before it charges anything.
/// </summary>
/// <param name="UserId">Owner of the cart</param>
/// <param name="Lines">Product lines; empty for an empty cart</param>
/// <param name="UpdatedAt">Last change to the cart</param>
public sealed record CartSnapshot(string UserId, IReadOnlyList<CartLineSnapshot> Lines, DateTime UpdatedAt);

/// <summary>One line of a <see cref="CartSnapshot"/>.</summary>
/// <param name="ProductId">Catalogue product id</param>
/// <param name="Title">Product title at snapshot time</param>
/// <param name="Image">Product image at snapshot time</param>
/// <param name="Company">Brand</param>
/// <param name="Color">Colour variant chosen by the customer</param>
/// <param name="UnitPrice">Price per unit the cart last saw</param>
/// <param name="Quantity">Units</param>
public sealed record CartLineSnapshot(
    int ProductId,
    string Title,
    string Image,
    string Company,
    string Color,
    decimal UnitPrice,
    int Quantity);
