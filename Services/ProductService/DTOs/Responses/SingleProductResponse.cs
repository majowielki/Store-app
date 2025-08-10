namespace Store.ProductService.DTOs.Responses;

public class SingleProductResponse
{
    public ProductData Data { get; set; } = new();
    public object Meta { get; set; } = new();
}