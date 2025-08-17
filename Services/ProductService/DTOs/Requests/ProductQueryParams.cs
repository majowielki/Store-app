// Enable nullable annotations in this file
#nullable enable
namespace Store.ProductService.DTOs.Requests;

public class ProductQueryParams
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? Group { get; set; }
    public string? Company { get; set; }
    public string? Color { get; set; }
    public string? Order { get; set; }
    public string? Price { get; set; }
    // Accept common truthy strings like "on", "true", "1" from UI checkboxes
    public string? Sale { get; set; }
    // Nullable to allow empty "page=" to bind as null instead of causing 400 with [ApiController]
    public int? Page { get; set; }
    // Optional page size for admin or custom UI
    public int? PageSize { get; set; }
}