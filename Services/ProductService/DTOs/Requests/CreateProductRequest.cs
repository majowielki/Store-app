using Store.Contracts.Catalog;
using Store.ProductService.DTOs.Responses;

namespace Store.ProductService.DTOs.Requests;

/// <summary>Body of POST /api/v1/products. Rules: <c>CreateProductRequestValidator</c>.</summary>
public class CreateProductRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal? SalePrice { get; set; }

    public decimal? DiscountPercent { get; set; }

    public Category Category { get; set; }

    public Company Company { get; set; }

    public bool NewArrival { get; set; }

    public string Image { get; set; } = string.Empty;

    public List<string> Colors { get; set; } = new();

    public List<string>? Groups { get; set; }

    public decimal? WidthCm { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? DepthCm { get; set; }

    public decimal? WeightKg { get; set; }

    public List<string>? Materials { get; set; }

    /// <summary>Units on hand to start with; none means the product starts out of stock.</summary>
    public int StockQuantity { get; set; }

    /// <summary>Pictures shown after the main one, in order; none leaves the product with its main picture only.</summary>
    public List<ProductImageDto>? Images { get; set; }

    /// <summary>Points on the main picture leading to other products.</summary>
    public List<ProductHotspotDto>? Hotspots { get; set; }
}
