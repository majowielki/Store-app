namespace Store.ProductService.Models;

/// <summary>
/// A point on a product's main picture leading to another product the picture shows: its position
/// in percent of the picture's width and height, and the product it stands for, by slug. A slug
/// that names no active product (a retired one, or one not in the catalogue yet) is kept but not
/// shown, so the point appears by itself once that product is added.
/// </summary>
public class ProductHotspot
{
    public decimal X { get; set; }

    public decimal Y { get; set; }

    public string ProductSlug { get; set; } = string.Empty;
}
