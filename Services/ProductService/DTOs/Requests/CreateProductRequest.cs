using Store.Shared.Utility;
using System.ComponentModel.DataAnnotations;

namespace Store.ProductService.DTOs.Requests;

public class CreateProductRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(4000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, 999999.99)]
    public decimal Price { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? SalePrice { get; set; }

    [Range(0, 100)]
    public decimal? DiscountPercent { get; set; }
    
    [Required]
    public Category Category { get; set; }
    
    [Required]
    public Company Company { get; set; }
    
    public bool NewArrival { get; set; } = false;
    
    [Required]
    [Url]
    public string Image { get; set; } = string.Empty;
    
    [Required]
    [MinLength(1)]
    public List<string> Colors { get; set; } = new();

    // Optional groups (e.g. furniture, bathroom, kids, garden)
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

    // Materials (simple strings like wood, steel, glass)
    public List<string>? Materials { get; set; }
}
