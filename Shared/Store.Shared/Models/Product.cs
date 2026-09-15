using Store.Shared.Utility;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Store.Shared.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999999.99)]
    public decimal Price { get; set; }

    // Optional sale price; when set it represents the current price during a sale
    [Range(0.01, 999999.99)]
    public decimal? SalePrice { get; set; }

    // Optional discount percent (0-100)
    [Range(0, 100)]
    public decimal? DiscountPercent { get; set; }

    [Required]
    public Category Category { get; set; }

    [Required]
    public Company Company { get; set; }

    public bool NewArrival { get; set; }

    [Required]
    [Url]
    public string Image { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<string> Colors { get; set; } = new();

    // Optional groups (e.g., furniture, kids, bathroom, garden). Lowercase preferred.
    public List<string> Groups { get; set; } = new();

    // New fields: dimensions and weight
    [Range(0, 100000)]
    public decimal? WidthCm { get; set; }

    [Range(0, 100000)]
    public decimal? HeightCm { get; set; }

    [Range(0, 100000)]
    public decimal? DepthCm { get; set; }

    [Range(0, 100000)]
    public decimal? WeightKg { get; set; }

    // Materials list (simple strings like wood, steel, glass)
    public List<string> Materials { get; set; } = new();

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The price a customer actually pays: the sale price when one is set, otherwise the list
    /// price reduced by the discount percent. Cart and order snapshots must use this value.
    /// </summary>
    [NotMapped]
    public decimal EffectivePrice => SalePrice is > 0
        ? SalePrice.Value
        : DiscountPercent is > 0
            ? Math.Round(Price * (1 - DiscountPercent.Value / 100m), 2, MidpointRounding.AwayFromZero)
            : Price;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
