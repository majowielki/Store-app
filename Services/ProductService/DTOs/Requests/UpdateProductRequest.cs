using Store.BuildingBlocks.Api;
using Store.Contracts.Catalog;

namespace Store.ProductService.DTOs.Requests;

/// <summary>
/// Body of PUT /api/products/{id}: a partial update. A property that is absent keeps the
/// current value. Fields that can be empty use <see cref="Optional{T}"/>, so sending them as
/// null clears them - that is how a promotion is taken off a product.
/// Rules: <c>UpdateProductRequestValidator</c>.
/// </summary>
public class UpdateProductRequest
{
    public string? Title { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public Optional<decimal?> SalePrice { get; set; }

    public Optional<decimal?> DiscountPercent { get; set; }

    public Category? Category { get; set; }

    public Company? Company { get; set; }

    public bool? NewArrival { get; set; }

    public string? Image { get; set; }

    public List<string>? Colors { get; set; }

    /// <summary>Replaces the whole list when present; an empty list removes every group.</summary>
    public List<string>? Groups { get; set; }

    public Optional<decimal?> WidthCm { get; set; }

    public Optional<decimal?> HeightCm { get; set; }

    public Optional<decimal?> DepthCm { get; set; }

    public Optional<decimal?> WeightKg { get; set; }

    /// <summary>Replaces the whole list when present; an empty list removes every material.</summary>
    public List<string>? Materials { get; set; }

    /// <summary>False hides the product from the public catalogue, true brings a deleted one back.</summary>
    public bool? IsActive { get; set; }
}
