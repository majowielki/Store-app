using Store.BuildingBlocks.Api;
using Store.Contracts.Catalog;
using Store.ProductService.DTOs.Requests;
using Store.ProductService.DTOs.Responses;

namespace Store.ProductService.Services;

/// <summary>
/// The catalogue. Methods return the product as the API shows it or throw an
/// <see cref="ApiException"/> (a missing product is <see cref="NotFoundException"/>).
/// </summary>
public interface IProductService
{
    // Changes (true-admin only); actorId is the administrator recorded in the audit trail
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request, string? actorId = null);
    Task<ProductResponse> UpdateProductAsync(int id, UpdateProductRequest request, string? actorId = null);
    Task DeleteProductAsync(int id, string? actorId = null);

    // The public catalogue: active products only
    Task<PagedResponse<ProductResponse>> GetProductsAsync(ProductQueryParams queryParams);
    Task<ProductResponse> GetProductAsync(int id);
    ProductsMeta GetProductsMeta();

    // What other services may know about a product
    Task<ProductSnapshot?> GetSnapshotAsync(int id);

    // The admin listing: inactive products too, sortable
    Task<PagedResponse<ProductResponse>> GetProductsForAdminAsync(ProductQueryParams queryParams, string? sortBy, string? sortDir);
}
