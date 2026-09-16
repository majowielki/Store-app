using System.Net.Http.Headers;

namespace Store.OrderService.Clients;

/// <summary>
/// Saves the delivery address to the customer's profile. Transitional: it still acts with the
/// customer's own token; the identity service takes this over as a consumer of the
/// order-placed event in the messaging step.
/// </summary>
public interface IIdentityClient
{
    Task SaveAddressAsync(string address, string bearerToken, CancellationToken cancellationToken = default);
}

public sealed class IdentityClient : IIdentityClient
{
    private readonly HttpClient _httpClient;

    public IdentityClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SaveAddressAsync(string address, string bearerToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri("api/auth/me/address", UriKind.Relative))
        {
            Content = JsonContent.Create(new { SimpleAddress = address })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
