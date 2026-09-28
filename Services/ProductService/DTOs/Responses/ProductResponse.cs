using Store.Contracts.Catalog;
using Store.ProductService.Models;

namespace Store.ProductService.DTOs.Responses;

public class ProductResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    /// <summary>Stable, address-friendly name; content (collections, lookbooks) refers to products by it.</summary>
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    /// <summary>Price the customer pays (sale price or discounted price, else the list price).</summary>
    public decimal EffectivePrice { get; set; }
    public Category Category { get; set; }
    public Company Company { get; set; }
    public bool NewArrival { get; set; }
    public string Image { get; set; } = string.Empty;

    /// <summary>The finishes it is sold in, by key (GET /products/finishes), the photographed one first.</summary>
    public List<string> Colors { get; set; } = new();
    public List<string> Groups { get; set; } = new();

    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? DepthCm { get; set; }
    public decimal? WeightKg { get; set; }
    public List<string> Materials { get; set; } = new();
    public bool IsActive { get; set; } = true;

    /// <summary>Whether a customer can buy it now: in stock, only a few left (low), or out of stock.</summary>
    public StockAvailability Availability { get; set; }

    /// <summary>Units a new order can get.</summary>
    public int AvailableQuantity { get; set; }

    /// <summary>Average of the published reviews (1-5), 0 without any.</summary>
    public decimal RatingAverage { get; set; }

    /// <summary>Number of published reviews.</summary>
    public int RatingCount { get; set; }

    /// <summary>Units on hand, held ones included; only in the admin responses.</summary>
    public int? StockQuantity { get; set; }

    /// <summary>Units held for orders not shipped yet; only in the admin responses.</summary>
    public int? ReservedQuantity { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
