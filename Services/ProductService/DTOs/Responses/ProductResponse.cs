using Store.Shared.Utility;

namespace Store.ProductService.DTOs.Responses;

public class ProductResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Category Category { get; set; }
    public Company Company { get; set; }
    public string Image { get; set; } = string.Empty;
    
    // Basic color names for filtering/selection
    public List<string> Colors { get; set; } = new();
    
    // Enhanced color information for frontend
    public List<ColorInfo> AvailableColors => Colors
        .Select(c => ColorHelper.ParseColor(c))
        .Where(c => c.HasValue)
        .Select(c => new ColorInfo
        {
            Name = c!.Value.ToString(),
            DisplayName = c.Value.GetDisplayName(),
            HexCode = c.Value.GetHexCode(),
            Value = (int)c.Value
        })
        .ToList();
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
