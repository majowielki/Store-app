using MassTransit.Testing;
using Store.Contracts.Catalog.V1;
using Store.Contracts.Payments.V1;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Orders;

/// <summary>
/// Walks an order through its saga the way the rest of the system would: the test publishes what
/// the stock and the payment webhook would publish and waits for the order to follow.
/// </summary>
public sealed class OrderJourney
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly OrderApiFactory _factory;

    public OrderJourney(OrderApiFactory factory)
    {
        _factory = factory;
    }

    public static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    /// <summary>Checks out a cart of one product; the order is placed and its stock is being reserved.</summary>
    public async Task<int> PlaceAsync(string user, int productId, decimal price = 120m)
    {
        _factory.Upstreams.AddProduct(productId, effectivePrice: price);
        _factory.Upstreams.SetCart(user, (productId, 1, price));
        using var client = _factory.CreateClient().AsUser(user);
        var response = await client.PostAsJsonAsync("/api/v1/orders/from-cart", new { customerName = "Saga Buyer", deliveryAddress = "1 Test Street" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJson(response)).GetProperty("id").GetInt32();
    }

    /// <summary>The order as the true administrator sees it.</summary>
    public async Task<JsonElement> OrderAsync(int orderId)
    {
        using var admin = _factory.CreateClient().AsTrueAdmin();
        return await ReadJson(await admin.GetAsync($"/api/v1/admin/orders/{orderId}"));
    }

    public async Task WaitForStatusAsync(int orderId, string status)
        => await Eventually.AssertAsync(async () => Assert.Equal(status, (await OrderAsync(orderId)).GetProperty("status").GetString()));

    public Task PublishAsync<T>(T message) where T : class => _factory.Bus.Bus.Publish(message);

    /// <summary>What the stock answers once every line is held.</summary>
    public static StockReserved Reserved(int orderId) => new(orderId, [new StockLine(1, 1)], DateTime.UtcNow);

    /// <summary>What the payment webhook turns into once the card went through.</summary>
    public static PaymentAccepted Accepted(int orderId, decimal amount, Guid? paymentId = null)
        => new(paymentId ?? Guid.NewGuid(), orderId, amount, "visa", "4242", DateTime.UtcNow);

    /// <summary>Placed, and the stock answered: the order waits for its payment.</summary>
    public async Task<int> AwaitingPaymentAsync(string user, int productId, decimal price = 120m)
    {
        var orderId = await PlaceAsync(user, productId, price);
        await PublishAsync(Reserved(orderId));
        await WaitForStatusAsync(orderId, "AwaitingPayment");
        return orderId;
    }

    /// <summary>Waiting for its payment, and the payment went through.</summary>
    public async Task<int> PaidAsync(string user, int productId, decimal price = 120m)
    {
        var orderId = await AwaitingPaymentAsync(user, productId, price);
        var total = (await OrderAsync(orderId)).GetProperty("total").GetDecimal();
        await PublishAsync(Accepted(orderId, total));
        await WaitForStatusAsync(orderId, "Paid");
        return orderId;
    }

    /// <summary>Messages the saga published from its consumers, seen by the probe.</summary>
    public IEnumerable<T> Consumed<T>(Func<T, bool> match) where T : class
        => _factory.Bus.Consumed.Select<T>(e => match(e.Context.Message)).Select(e => e.Context.Message);

    public Task<bool> ConsumedAsync<T>(Func<T, bool> match) where T : class
        => Eventually.BecomesTrueAsync(() => Consumed(match).Any());
}
