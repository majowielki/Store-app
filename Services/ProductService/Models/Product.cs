using Store.Contracts.Catalog;

namespace Store.ProductService.Models;

/// <summary>
/// A catalogue product as ProductService owns it. Other services never see this type: they
/// get a <see cref="ProductSnapshot"/> and keep whatever they need of it themselves.
/// Column lengths and precision live in <c>ProductDbContext</c>, request rules in the validators;
/// both read <see cref="ProductConstraints"/>.
/// </summary>
public class Product
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>List price.</summary>
    public decimal Price { get; set; }

    /// <summary>Promotional price; when set it is what the customer pays.</summary>
    public decimal? SalePrice { get; set; }

    /// <summary>Percentage discount applied to the list price when no sale price is set.</summary>
    public decimal? DiscountPercent { get; set; }

    public Category Category { get; set; }

    public Company Company { get; set; }

    public bool NewArrival { get; set; }

    public string Image { get; set; } = string.Empty;

    public List<string> Colors { get; set; } = new();

    /// <summary>Navigation groups (furniture, kids, bathroom, garden), lowercase.</summary>
    public List<string> Groups { get; set; } = new();

    public decimal? WidthCm { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? DepthCm { get; set; }

    public decimal? WeightKg { get; set; }

    /// <summary>Materials (wood, steel, glass), lowercase.</summary>
    public List<string> Materials { get; set; } = new();

    /// <summary>
    /// Deleting a product only clears this flag: orders keep referencing it, the public
    /// catalogue hides it and the admin panel can bring it back.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The price a customer actually pays: the sale price when one is set, otherwise the list
    /// price reduced by the discount percent. Cart and order snapshots must use this value.
    /// </summary>
    public decimal EffectivePrice => SalePrice is > 0
        ? SalePrice.Value
        : DiscountPercent is > 0
            ? Math.Round(Price * (1 - DiscountPercent.Value / 100m), 2, MidpointRounding.AwayFromZero)
            : Price;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ProductSnapshot ToSnapshot() => new(
        Id,
        Title,
        Image,
        Company.ToString(),
        Colors,
        Price,
        EffectivePrice,
        IsActive,
        UpdatedAt);
}
