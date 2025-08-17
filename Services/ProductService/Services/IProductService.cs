using Store.ProductService.DTOs.Requests;
using Store.ProductService.DTOs.Responses;

namespace Store.ProductService.Services;

public interface IProductService
{
    // Core product operations (admin only)
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request);
    Task<ProductResponse?> UpdateProductAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteProductAsync(int id);
    
    // Frontend-compatible operations (public)
    Task<ProductsResponse> GetProductsForFrontendAsync(ProductQueryParams queryParams);
    Task<SingleProductResponse> GetProductForFrontendAsync(int id);
    Task<ProductsMeta> GetProductsMetaAsync();
    
    // Admin advanced endpoint
    Task<ProductsResponse> GetProductsForAdminAsync(ProductQueryParams queryParams, string? sortBy, string? sortDir);
}
