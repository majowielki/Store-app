using MassTransit.Testing;
using Store.Contracts.Orders.V1;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Orders;

[Collection(PostgresTests.Name)]
public sealed class OrderStatusTests : IClassFixture<OrderApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly OrderApiFactory _factory;

    public OrderStatusTests(OrderApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    private async Task<int> PlaceOrderAsync(string user, int productId)
    {
        _factory.Upstreams.AddProduct(productId, effectivePrice: 120m);
        _factory.Upstreams.SetCart(user, (productId, 1, 120m));
        using var client = _factory.CreateClient().AsUser(user);
        var response = await client.PostAsJsonAsync("/api/v1/orders/from-cart", new { customerName = "Status Buyer", deliveryAddress = "1 Test Street" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJson(response)).GetProperty("id").GetInt32();
    }

    private static Task<HttpResponseMessage> MoveAsync(HttpClient client, int orderId, string status)
        => client.PatchAsJsonAsync($"/api/v1/admin/orders/{orderId}/status", new { status });

    private static List<string> History(JsonElement order)
        => order.GetProperty("statusHistory").EnumerateArray().Select(c => c.GetProperty("status").GetString()!).ToList();

    [Fact]
    public async Task A_placed_order_starts_its_history_and_carries_the_delivery_window_of_the_rules()
    {
        const string user = "status-placed";
        using var anonymous = _factory.CreateClient();
        var rules = await ReadJson(await anonymous.GetAsync("/api/v1/orders/pricing-rules"));

        var orderId = await PlaceOrderAsync(user, 101);
        using var client = _factory.CreateClient().AsUser(user);
        var order = await ReadJson(await client.GetAsync($"/api/v1/orders/{orderId}"));

        Assert.Equal(["Placed"], History(order));
        Assert.Equal(["Paid", "Cancelled"], order.GetProperty("nextStatuses").EnumerateArray().Select(s => s.GetString()));
        // The window is a pair of dates, the same the cart page was shown a moment before
        Assert.Equal(rules.GetProperty("deliveryFrom").GetString(), order.GetProperty("deliveryFrom").GetString());
        Assert.Equal(rules.GetProperty("deliveryTo").GetString(), order.GetProperty("deliveryTo").GetString());
        Assert.True(DateOnly.Parse(order.GetProperty("deliveryFrom").GetString()!) <= DateOnly.Parse(order.GetProperty("deliveryTo").GetString()!));
    }

    [Fact]
    public async Task The_true_administrator_moves_an_order_to_paid_and_shipped_and_the_customer_sees_every_step()
    {
        const string user = "status-journey";
        var orderId = await PlaceOrderAsync(user, 102);
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var paid = await MoveAsync(admin, orderId, "Paid");
        Assert.Equal(HttpStatusCode.OK, paid.StatusCode);
        Assert.Equal("Paid", (await ReadJson(paid)).GetProperty("status").GetString());
        var shipped = await ReadJson(await MoveAsync(admin, orderId, "Shipped"));
        Assert.Empty(shipped.GetProperty("nextStatuses").EnumerateArray());

        using var customer = _factory.CreateClient().AsUser(user);
        var order = await ReadJson(await customer.GetAsync($"/api/v1/orders/{orderId}"));
        Assert.Equal("Shipped", order.GetProperty("status").GetString());
        Assert.Equal(["Placed", "Paid", "Shipped"], History(order));
        var times = order.GetProperty("statusHistory").EnumerateArray().Select(c => c.GetProperty("changedAt").GetDateTime()).ToList();
        Assert.Equal(times.Order(), times);

        // Each change left through the outbox with the administrator who made it
        Assert.True(await Eventually.BecomesTrueAsync(() =>
            _factory.Bus.Published.Select<OrderStatusChanged>(p => p.Context.Message.OrderId == orderId).Count() == 2));
        var changes = _factory.Bus.Published.Select<OrderStatusChanged>(p => p.Context.Message.OrderId == orderId)
            .Select(p => p.Context.Message).OrderBy(m => m.ChangedAt).ToList();
        Assert.Equal(("Placed", "Paid"), (changes[0].PreviousStatus, changes[0].Status));
        Assert.Equal(("Paid", "Shipped"), (changes[1].PreviousStatus, changes[1].Status));
        Assert.All(changes, change => Assert.Equal("true-admin-1", change.ChangedBy));
        Assert.All(changes, change => Assert.Equal(user, change.UserId));
    }

    [Theory]
    [InlineData("Shipped")]                 // placed → shipped skips the payment
    [InlineData("Paid,Paid")]               // twice the same
    [InlineData("Cancelled,Paid")]          // cancelled is final
    [InlineData("Paid,Shipped,Cancelled")]  // so is shipped
    public async Task A_move_the_status_does_not_allow_is_a_conflict(string path)
    {
        var moves = path.Split(',');
        var orderId = await PlaceOrderAsync($"status-conflict-{path}", 103);
        using var admin = _factory.CreateClient().AsTrueAdmin();

        foreach (var status in moves[..^1])
        {
            Assert.Equal(HttpStatusCode.OK, (await MoveAsync(admin, orderId, status)).StatusCode);
        }
        var refused = await MoveAsync(admin, orderId, moves[^1]);

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("application/problem+json", refused.Content.Headers.ContentType?.MediaType);
        var order = await ReadJson(await admin.GetAsync($"/api/v1/admin/orders/{orderId}"));
        Assert.Equal(moves.Length, History(order).Count);
    }

    [Fact]
    public async Task The_demo_administrator_and_customers_may_not_change_an_order()
    {
        const string user = "status-forbidden";
        var orderId = await PlaceOrderAsync(user, 104);

        using var demo = _factory.CreateClient().AsDemoAdmin();
        using var customer = _factory.CreateClient().AsUser(user);
        using var anonymous = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await MoveAsync(demo, orderId, "Paid")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await MoveAsync(customer, orderId, "Cancelled")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await MoveAsync(anonymous, orderId, "Cancelled")).StatusCode);
        var order = await ReadJson(await customer.GetAsync($"/api/v1/orders/{orderId}"));
        Assert.Equal("Placed", order.GetProperty("status").GetString());
    }

    [Fact]
    public async Task An_unknown_status_is_a_validation_error_and_an_unknown_order_is_not_found()
    {
        var orderId = await PlaceOrderAsync("status-invalid", 105);
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var invalid = await MoveAsync(admin, orderId, "Delivered");
        var missing = await MoveAsync(admin, 999_999, "Paid");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        Assert.True((await ReadJson(invalid)).GetProperty("errors").TryGetProperty("Status", out _));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
