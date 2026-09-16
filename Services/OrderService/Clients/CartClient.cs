using Store.BuildingBlocks.Serialization;
using Store.Contracts.Cart;
using System.Net;

namespace Store.OrderService.Clients;

/// <summary>What the order service needs from the cart service at checkout.</summary>
public interface ICartClient
{
    /// <summary>The customer's cart, or null when they have none.</summary>
    Task<CartSnapshot?> GetSnapshotAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Empties the cart after an order was placed from it.</summary>
    Task ClearAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Typed client for the internal cart endpoints. It authenticates as this service (internal
/// API key), not as the customer, so no user token travels between services.
/// </summary>
public sealed class CartClient : ICartClient
{
    private readonly HttpClient _httpClient;

    public CartClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CartSnapshot?> GetSnapshotAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(new Uri($"api/cart/internal/{Uri.EscapeDataString(userId)}", UriKind.Relative), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CartSnapshot>(StoreJson.Web, cancellationToken);
    }

    public async Task ClearAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(new Uri($"api/cart/internal/{Uri.EscapeDataString(userId)}", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
