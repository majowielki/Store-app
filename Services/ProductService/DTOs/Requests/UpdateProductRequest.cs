using Store.Shared.Utility;
using System.ComponentModel.DataAnnotations;

namespace Store.ProductService.DTOs.Requests;

public class UpdateProductRequest
{
    [StringLength(200, MinimumLength = 3)]
    public string? Title { get; set; }
    
    [StringLength(1000, MinimumLength = 10)]
    public string? Description { get; set; }
    
    [Range(0.01, 999999.99)]
    public decimal? Price { get; set; }
    
    public Category? Category { get; set; }
    
    public Company? Company { get; set; }
    
    [Url]
    public string? Image { get; set; }
    
    [MinLength(1)]
    public List<string>? Colors { get; set; }
}
