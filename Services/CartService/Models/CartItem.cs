using Store.Contracts.Catalog;

namespace Store.CartService.Models;

/// <summary>
/// A line in the cart with a snapshot of the product as the catalogue described it when the
/// line was added or last refreshed. The cart service keeps no copy of the catalogue: this is
/// all it knows about the product, and <see cref="SnapshotAt"/> says how old that knowledge is.
/// </summary>
public class CartItem
{
    public int Id { get; set; }

    public int CartId { get; set; }

    public Cart Cart { get; set; } = null!;

    public int ProductId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Image { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    /// <summary>Colour variant chosen by the customer.</summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>The price the customer pays per unit: the catalogue's effective price at snapshot time.</summary>
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; } = 1;

    /// <summary>When <see cref="Title"/>, <see cref="Image"/> and <see cref="UnitPrice"/> were last taken from the catalogue.</summary>
    public DateTime SnapshotAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public decimal LineTotal => UnitPrice * Quantity;

    public void ApplySnapshot(ProductSnapshot product, DateTime now)
    {
        Title = product.Title;
        Image = product.Image;
        Company = product.Company;
        UnitPrice = product.EffectivePrice;
        SnapshotAt = now;
        UpdatedAt = now;
    }
}
