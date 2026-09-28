using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Serialization;
using Store.Contracts.Payments;
using System.Net;

namespace Store.OrderService.Clients;

/// <summary>
/// The payment service as the order service sees it: it opens the payment of an order. How the
/// payment ends is not asked for here - it arrives as a signed webhook.
/// </summary>
public interface IPaymentClient
{
    /// <summary>
    /// Opens the payment of an order under <paramref name="idempotencyKey"/>, or returns the one
    /// opened before with it. A payment the service will not open (another amount for the same
    /// order) is a <see cref="ConflictException"/>.
    /// </summary>
    Task<PaymentSnapshot> OpenAsync(CreatePaymentRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class PaymentClient : IPaymentClient
{
    private readonly HttpClient _httpClient;

    public PaymentClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentSnapshot> OpenAsync(CreatePaymentRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri("api/v1/payments/internal", UriKind.Relative))
        {
            Content = JsonContent.Create(request, options: StoreJson.Web)
        };
        // Retries of this call (the resilience pipeline) and of the customer's click open one payment
        message.Headers.Add("Idempotency-Key", idempotencyKey);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new ConflictException("The payment of this order could not be opened for its current amount.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentSnapshot>(StoreJson.Web, cancellationToken)
            ?? throw new InvalidOperationException("The payment service answered without a payment.");
    }
}
