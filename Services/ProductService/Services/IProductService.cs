using Store.Contracts.Catalog;
using Store.ProductService.DTOs.Requests;
using Store.ProductService.DTOs.Responses;

namespace Store.ProductService.Services;

public interface IProductService
{
    // Core product operations (admin only)
    // actorId: the administrator performing the change, recorded in the audit trail
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request, string? actorId = null);
    Task<ProductResponse?> UpdateProductAsync(int id, UpdateProductRequest request, string? actorId = null);
    Task<bool> DeleteProductAsync(int id, string? actorId = null);

    // Frontend-compatible operations (public)
    Task<ProductsResponse> GetProductsForFrontendAsync(ProductQueryParams queryParams);
    Task<SingleProductResponse> GetProductForFrontendAsync(int id);
    Task<ProductsMeta> GetProductsMetaAsync();

    // What other services may know about a product
    Task<ProductSnapshot?> GetSnapshotAsync(int id);

    // Admin advanced endpoint
    Task<ProductsResponse> GetProductsForAdminAsync(ProductQueryParams queryParams, string? sortBy, string? sortDir);
}
