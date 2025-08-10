using Store.Shared.Utility;
using System.ComponentModel.DataAnnotations;

namespace Store.ProductService.DTOs.Requests;

public class CreateProductRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, 999999.99)]
    public decimal Price { get; set; }
    
    [Required]
    public Category Category { get; set; }
    
    [Required]
    public Company Company { get; set; }
    
    public bool Featured { get; set; } = false;
    
    [Required]
    [Url]
    public string Image { get; set; } = string.Empty;
    
    [Required]
    [MinLength(1)]
    public List<string> Colors { get; set; } = new();
}
