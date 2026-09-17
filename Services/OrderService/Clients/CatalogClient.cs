using Store.BuildingBlocks.Serialization;
using Store.Contracts.Catalog;
using System.Net;

namespace Store.OrderService.Clients;

/// <summary>
/// The catalogue as the order service sees it. An order is priced from these snapshots, not
/// from what the cart stored when the customer added the line.
/// </summary>
public interface ICatalogClient
{
    /// <summary>The product as the catalogue describes it now, or null when it does not exist.</summary>
    Task<ProductSnapshot?> GetSnapshotAsync(int productId, CancellationToken cancellationToken = default);
}

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
