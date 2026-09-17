using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Orders;

[Collection(PostgresTests.Name)]
public sealed class IdempotencyTests : IClassFixture<OrderApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly OrderApiFactory _factory;

    public IdempotencyTests(OrderApiFactory factory)
    {
        _factory = factory;
    }

    private static object CheckoutBody(string name = "Integration Buyer") => new
    {
        userEmail = "buyer@test.local",
        customerName = name,
        deliveryAddress = "1 Test Street",
        saveAddress = false
    };

    private static async Task<int> OrderIdOf(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json).GetProperty("id").GetInt32();

    private static async Task<HttpResponseMessage> CheckoutAsync(HttpClient client, string key, object? body = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/from-cart")
        {
            Content = JsonContent.Create(body ?? CheckoutBody())
        };
        request.Headers.Add("Idempotency-Key", key);
        return await client.SendAsync(request);
    }

    // Regression: a timeout after the order was written made the client retry and pay twice
    [Fact]
    public async Task Retrying_with_the_same_key_returns_the_same_order()
    {
        const string user = "idem-retry";
        _factory.Upstreams.AddProduct(11, effectivePrice: 50m);
        _factory.Upstreams.SetCart(user, (11, 1, 50m));
        using var client = _factory.CreateClient().AsUser(user);
        var key = Guid.NewGuid().ToString();

        var first = await CheckoutAsync(client, key);
        var retry = await CheckoutAsync(client, key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
        Assert.Equal(await OrderIdOf(first), await OrderIdOf(retry));

        var orders = JsonSerializer.Deserialize<JsonElement>(await client.GetStringAsync("/api/v1/orders/my-orders"), Json)
            .GetProperty("totalCount").GetInt32();
        Assert.Equal(1, orders);
    }

    [Fact]
    public async Task Reusing_a_key_for_a_different_request_is_rejected()
    {
        const string user = "idem-mismatch";
        _factory.Upstreams.AddProduct(12, effectivePrice: 50m);
        _factory.Upstreams.SetCart(user, (12, 1, 50m));
        using var client = _factory.CreateClient().AsUser(user);
        var key = Guid.NewGuid().ToString();

        Assert.Equal(HttpStatusCode.Created, (await CheckoutAsync(client, key)).StatusCode);
        var reused = await CheckoutAsync(client, key, CheckoutBody(name: "Someone Else"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, reused.StatusCode);
    }

    [Fact]
    public async Task Parallel_requests_with_one_key_create_one_order()
    {
        const string user = "idem-parallel";
        _factory.Upstreams.AddProduct(13, effectivePrice: 50m);
        _factory.Upstreams.SetCart(user, (13, 1, 50m));
        using var client = _factory.CreateClient().AsUser(user);
        var key = Guid.NewGuid().ToString();

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => CheckoutAsync(client, key)));

        var ids = new HashSet<int>();
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            ids.Add(await OrderIdOf(response));
        }
        Assert.Single(ids);
    }

    [Fact]
    public async Task Without_a_key_every_request_is_a_new_order()
    {
        const string user = "idem-none";
        _factory.Upstreams.AddProduct(14, effectivePrice: 50m);
        _factory.Upstreams.SetCart(user, (14, 1, 50m));
        using var client = _factory.CreateClient().AsUser(user);

        var first = await client.PostAsJsonAsync("/api/v1/orders/from-cart", CheckoutBody());
        var second = await client.PostAsJsonAsync("/api/v1/orders/from-cart", CheckoutBody());

        Assert.NotEqual(await OrderIdOf(first), await OrderIdOf(second));
    }
}
