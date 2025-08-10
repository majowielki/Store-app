// Enable nullable annotations in this file
#nullable enable
namespace Store.ProductService.DTOs.Requests;

public class ProductQueryParams
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? Company { get; set; }
    public string? Order { get; set; }
    public string? Price { get; set; }
    // Nullable to allow empty "page=" to bind as null instead of causing 400 with [ApiController]
    public int? Page { get; set; }
}