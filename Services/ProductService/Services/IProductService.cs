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
    Task<ProductDetailResponse> CreateProductAsync(CreateProductRequest request, string? actorId = null);
    Task<ProductDetailResponse> UpdateProductAsync(int id, UpdateProductRequest request, string? actorId = null);
    Task DeleteProductAsync(int id, string? actorId = null);

    // The public catalogue: active products only
    Task<PagedResponse<ProductResponse>> GetProductsAsync(ProductQueryParams queryParams);
    Task<ProductDetailResponse> GetProductAsync(int id);
    ProductsMeta GetProductsMeta();

    /// <summary>The filter values with how many products each shows under the rest of the query, and the search's correction.</summary>
    Task<ProductsMeta> GetProductsMetaAsync(ProductQueryParams queryParams);

    /// <summary>The best matches of a search while it is typed, corrected when it finds nothing as typed.</summary>
    Task<ProductSuggestions> SuggestAsync(string? search, int limit);

    // What other services may know about a product
    Task<ProductSnapshot?> GetSnapshotAsync(int id);

    // The admin listing: inactive products too, sortable
    Task<ProductDetailResponse> GetProductForAdminAsync(int id);
    Task<PagedResponse<ProductResponse>> GetProductsForAdminAsync(ProductQueryParams queryParams, string? sortBy, string? sortDir);
}
