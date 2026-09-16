using MassTransit.Testing;
using Store.Contracts.Orders.V1;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Orders;

[Collection(PostgresTests.Name)]
public sealed class CheckoutTests : IClassFixture<OrderApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly OrderApiFactory _factory;

    public CheckoutTests(OrderApiFactory factory)
    {
        _factory = factory;
    }

    private static object CheckoutBody(string email = "buyer@test.local") => new
    {
        userEmail = email,
        customerName = "Integration Buyer",
        deliveryAddress = "1 Test Street",
        notes = (string?)null,
        saveAddress = false
    };

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    // Regression: the order copied the cart's prices; a promotion that ended (or started)
    // after the item was added was not reflected in what the customer paid
    [Fact]
    public async Task Order_is_priced_from_the_catalogue_and_stores_its_totals()
    {
        const string user = "checkout-pricing";
        _factory.Upstreams.AddProduct(1, effectivePrice: 80m, title: "Repriced lamp");
        _factory.Upstreams.AddProduct(2, effectivePrice: 30m, title: "Cushion");
        _factory.Upstreams.SetCart(user, (1, 2, 100m), (2, 1, 30m));
        using var client = _factory.CreateClient().AsUser(user);

        var response = await client.PostAsJsonAsync("/api/orders/from-cart", CheckoutBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await ReadJson(response)).GetProperty("data");
        Assert.Equal(190m, order.GetProperty("subtotal").GetDecimal());
        Assert.Equal(38m, order.GetProperty("discountAmount").GetDecimal());
        Assert.Equal("first-order", order.GetProperty("discountReason").GetString());
        Assert.Equal(10m, order.GetProperty("deliveryFee").GetDecimal());
        Assert.Equal(162m, order.GetProperty("total").GetDecimal());
        Assert.Equal("Placed", order.GetProperty("status").GetString());
        var lines = order.GetProperty("orderItems").EnumerateArray().ToList();
        Assert.Equal(2, lines.Count);
        Assert.All(lines, line => Assert.NotEqual(0, line.GetProperty("productId").GetInt32()));
        Assert.Equal(80m, lines.Single(l => l.GetProperty("productId").GetInt32() == 1).GetProperty("price").GetDecimal());

        // The event left through the outbox after the commit, with the same amounts
        var orderId = order.GetProperty("id").GetInt32();
        Assert.True(await Eventually.BecomesTrueAsync(() => _factory.Bus.Published.Select<OrderPlaced>(p => p.Context.Message.OrderId == orderId).Any()));
        var placed = _factory.Bus.Published.Select<OrderPlaced>(p => p.Context.Message.OrderId == orderId).Single().Context.Message;
        Assert.Equal(user, placed.UserId);
        Assert.Equal(162m, placed.Total);
        Assert.Equal(2, placed.Lines.Count);
    }

    [Fact]
    public async Task Only_the_first_order_is_discounted()
    {
        const string user = "checkout-second";
        _factory.Upstreams.AddProduct(3, effectivePrice: 400m);
        using var client = _factory.CreateClient().AsUser(user);

        _factory.Upstreams.SetCart(user, (3, 1, 400m));
        var first = (await ReadJson(await client.PostAsJsonAsync("/api/orders/from-cart", CheckoutBody()))).GetProperty("data");
        _factory.Upstreams.SetCart(user, (3, 1, 400m));
        var second = (await ReadJson(await client.PostAsJsonAsync("/api/orders/from-cart", CheckoutBody()))).GetProperty("data");

        Assert.Equal(80m, first.GetProperty("discountAmount").GetDecimal());
        Assert.Equal(320m, first.GetProperty("total").GetDecimal());
        Assert.Equal(0m, second.GetProperty("discountAmount").GetDecimal());
        Assert.Equal(400m, second.GetProperty("total").GetDecimal());
        Assert.Equal(0m, second.GetProperty("deliveryFee").GetDecimal());
    }

    // Regression: the first-order check was a read without a lock, so parallel checkouts
    // all saw "no orders yet" and every one of them got the discount
    [Fact]
    public async Task Ten_parallel_checkouts_grant_the_first_order_discount_once()
    {
        const string user = "checkout-race";
        _factory.Upstreams.AddProduct(4, effectivePrice: 100m);
        _factory.Upstreams.SetCart(user, (4, 1, 100m));
        using var client = _factory.CreateClient().AsUser(user);

        var responses = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => client.PostAsJsonAsync("/api/orders/from-cart", CheckoutBody())));

        var orders = new List<JsonElement>();
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            orders.Add((await ReadJson(response)).GetProperty("data"));
        }

        Assert.Equal(10, orders.Select(o => o.GetProperty("id").GetInt32()).Distinct().Count());
        Assert.Equal(1, orders.Count(o => o.GetProperty("discountAmount").GetDecimal() > 0));
    }

    [Fact]
    public async Task A_product_that_left_the_catalogue_blocks_the_checkout()
    {
        const string user = "checkout-inactive";
        _factory.Upstreams.AddProduct(5, effectivePrice: 50m, title: "Discontinued stool", isActive: false);
        _factory.Upstreams.SetCart(user, (5, 1, 50m));
        using var client = _factory.CreateClient().AsUser(user);

        var response = await client.PostAsJsonAsync("/api/orders/from-cart", CheckoutBody());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await Task.Delay(TimeSpan.FromSeconds(2));
        Assert.Empty(_factory.Bus.Published.Select<OrderPlaced>(p => p.Context.Message.UserId == user));
    }

    [Fact]
    public async Task Empty_cart_cannot_be_ordered()
    {
        using var client = _factory.CreateClient().AsUser("checkout-empty");

        var response = await client.PostAsJsonAsync("/api/orders/from-cart", CheckoutBody());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Customers_only_see_their_own_orders()
    {
        const string owner = "checkout-owner";
        _factory.Upstreams.AddProduct(6, effectivePrice: 20m);
        _factory.Upstreams.SetCart(owner, (6, 1, 20m));
        using var ownerClient = _factory.CreateClient().AsUser(owner);
        var id = (await ReadJson(await ownerClient.PostAsJsonAsync("/api/orders/from-cart", CheckoutBody()))).GetProperty("data").GetProperty("id").GetInt32();

        using var other = _factory.CreateClient().AsUser("checkout-other");
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/orders/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync("/api/orders/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ownerClient.GetAsync($"/api/orders/{id}")).StatusCode);

        using var admin = _factory.CreateClient().AsDemoAdmin();
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/orders/{id}")).StatusCode);
    }
}
