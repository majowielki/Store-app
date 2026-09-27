namespace Store.ProductService.Models;

/// <summary>
/// A picture of a product's gallery. The main picture stays in <see cref="Product.Image"/> (cards,
/// carts and orders show it); the gallery holds the ones shown after it, in <see cref="SortOrder"/>.
/// </summary>
public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string Url { get; set; } = string.Empty;

    /// <summary>What the picture shows, for screen readers.</summary>
    public string Alt { get; set; } = string.Empty;

    /// <summary>Position in the gallery, from 0.</summary>
    public int SortOrder { get; set; }
}
