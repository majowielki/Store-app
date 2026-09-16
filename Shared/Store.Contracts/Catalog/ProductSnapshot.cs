namespace Store.Contracts.Catalog;

/// <summary>
/// What the catalogue tells other services about a product: enough to show a cart line and
/// to charge the right price. Returned by <c>GET /api/products/{id}/snapshot</c>; the caller
/// stores the fields it needs instead of keeping a copy of the product.
/// </summary>
/// <param name="Id">Product id</param>
/// <param name="Title">Display name</param>
/// <param name="Image">Image URL</param>
/// <param name="Company">Brand name as text</param>
/// <param name="Colors">Colour variants the product is sold in</param>
/// <param name="Price">List price</param>
/// <param name="EffectivePrice">The price a customer pays right now (sale price or discounted list price)</param>
/// <param name="IsActive">False once the product was deleted from the catalogue</param>
/// <param name="UpdatedAt">Last change in the catalogue, for callers that cache</param>
public sealed record ProductSnapshot(
    int Id,
    string Title,
    string Image,
    string Company,
    IReadOnlyList<string> Colors,
    decimal Price,
    decimal EffectivePrice,
    bool IsActive,
    DateTime UpdatedAt);
