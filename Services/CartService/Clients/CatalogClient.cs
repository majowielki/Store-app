using Store.BuildingBlocks.Serialization;
using Store.Contracts.Catalog;
using System.Net;

namespace Store.CartService.Clients;

/// <summary>The cart's view of the catalogue: one product at a time, as a snapshot.</summary>
public interface ICatalogClient
{
    /// <summary>The product as the catalogue describes it now, or null when it does not exist.</summary>
    Task<ProductSnapshot?> GetSnapshotAsync(int productId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Typed client for ProductService. Base address, internal API key and the resilience pipeline
/// come from <c>AddServiceClient</c>; this class only knows the endpoint and its response.
/// </summary>
public sealed class CatalogClient : ICatalogClient
{
    private readonly HttpClient _httpClient;

    public CatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductSnapshot?> GetSnapshotAsync(int productId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(new Uri($"api/v1/products/{productId}/snapshot", UriKind.Relative), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductSnapshot>(StoreJson.Web, cancellationToken);
    }
}
