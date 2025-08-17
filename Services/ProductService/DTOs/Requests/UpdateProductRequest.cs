using Store.Shared.Utility;
using System.ComponentModel.DataAnnotations;

namespace Store.ProductService.DTOs.Requests;

public class UpdateProductRequest
{
    [StringLength(200, MinimumLength = 3)]
    public string? Title { get; set; }
    
    [StringLength(4000, MinimumLength = 10)]
    public string? Description { get; set; }
    
    [Range(0.01, 999999.99)]
    public decimal? Price { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? SalePrice { get; set; }

    [Range(0, 100)]
    public decimal? DiscountPercent { get; set; }
    
    public Category? Category { get; set; }
    
    public Company? Company { get; set; }
    
    public bool? NewArrival { get; set; }
    
    [Url]
    public string? Image { get; set; }
    
    [MinLength(1)]
    public List<string>? Colors { get; set; }

    // Replace Groups completely if provided
    public List<string>? Groups { get; set; }

    // Dimensions and weight (optional)
    [Range(0, 100000)]
    public decimal? WidthCm { get; set; }
    [Range(0, 100000)]
    public decimal? HeightCm { get; set; }
    [Range(0, 100000)]
    public decimal? DepthCm { get; set; }
    [Range(0, 100000)]
    public decimal? WeightKg { get; set; }

    // Materials list (replaces completely if provided)
    public List<string>? Materials { get; set; }
}
