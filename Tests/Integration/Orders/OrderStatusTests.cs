using MassTransit.Testing;
using Store.Contracts.Orders.V1;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Orders;

/// <summary>
/// The statuses the customer sees and the two moves the administrator makes by hand: shipping a
/// paid order and cancelling one that is not shipped yet. Paying is the saga's business.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class OrderStatusTests : IClassFixture<OrderApiFactory>
{
    private readonly OrderApiFactory _factory;
    private readonly OrderJourney _journey;

    public OrderStatusTests(OrderApiFactory factory)
    {
        _factory = factory;
        _journey = new OrderJourney(factory);
    }

    private static Task<JsonElement> ReadJson(HttpResponseMessage response) => OrderJourney.ReadJson(response);

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

        var orderId = await _journey.PlaceAsync(user, 101);
        using var client = _factory.CreateClient().AsUser(user);
        var order = await ReadJson(await client.GetAsync($"/api/v1/orders/{orderId}"));

        Assert.Equal(["Placed"], History(order));
        Assert.Equal(["Cancelled"], order.GetProperty("nextStatuses").EnumerateArray().Select(s => s.GetString()));
        // The window is a pair of dates, the same the cart page was shown a moment before
        Assert.Equal(rules.GetProperty("deliveryFrom").GetString(), order.GetProperty("deliveryFrom").GetString());
        Assert.Equal(rules.GetProperty("deliveryTo").GetString(), order.GetProperty("deliveryTo").GetString());
        Assert.True(DateOnly.Parse(order.GetProperty("deliveryFrom").GetString()!) <= DateOnly.Parse(order.GetProperty("deliveryTo").GetString()!));
    }

    [Fact]
    public async Task A_paid_order_is_shipped_by_the_true_administrator_and_the_customer_sees_every_step()
    {
        const string user = "status-journey";
        var orderId = await _journey.PaidAsync(user, 102);
        using var admin = _factory.CreateClient().AsTrueAdmin();
        Assert.Equal(["Shipped", "Cancelled"], (await _journey.OrderAsync(orderId)).GetProperty("nextStatuses").EnumerateArray().Select(s => s.GetString()));

        var shipped = await MoveAsync(admin, orderId, "Shipped");

        Assert.Equal(HttpStatusCode.OK, shipped.StatusCode);
        Assert.Empty((await ReadJson(shipped)).GetProperty("nextStatuses").EnumerateArray());
        using var customer = _factory.CreateClient().AsUser(user);
        var order = await ReadJson(await customer.GetAsync($"/api/v1/orders/{orderId}"));
        Assert.Equal("Shipped", order.GetProperty("status").GetString());
        Assert.Equal(["Placed", "AwaitingPayment", "Paid", "Shipped"], History(order));
        Assert.Equal("visa", order.GetProperty("cardBrand").GetString());
        Assert.Equal("4242", order.GetProperty("cardLast4").GetString());
        var times = order.GetProperty("statusHistory").EnumerateArray().Select(c => c.GetProperty("changedAt").GetDateTime()).ToList();
        Assert.Equal(times.Order(), times);

        // The stock and the e-mails hear of it through the outbox, with the lines
        Assert.True(await Eventually.BecomesTrueAsync(() =>
            _factory.Bus.Published.Select<OrderShipped>(p => p.Context.Message.OrderId == orderId && p.Context.Message.Lines.Count == 1).Any()));
        // Every change went to the audit: the saga's without a person, the shipping with the administrator
        Assert.True(await Eventually.BecomesTrueAsync(() => _journey.Consumed<OrderStatusChanged>(c => c.OrderId == orderId).Count() == 3));
        var changes = _journey.Consumed<OrderStatusChanged>(c => c.OrderId == orderId).OrderBy(c => c.ChangedAt).ToList();
        Assert.Equal([("Placed", "AwaitingPayment", null), ("AwaitingPayment", "Paid", null), ("Paid", "Shipped", "true-admin-1")],
            changes.Select(c => (c.PreviousStatus, c.Status, c.ChangedBy)));
        Assert.All(changes, change => Assert.Equal(user, change.UserId));
    }

    [Theory]
    [InlineData("Shipped")]                   // placed → shipped skips the payment
    [InlineData("Cancelled,Cancelled")]       // twice the same
    [InlineData("Cancelled,Shipped")]         // cancelled goes nowhere the administrator can take it
    public async Task A_move_the_status_does_not_allow_is_a_conflict(string path)
    {
        var moves = path.Split(',');
        var orderId = await _journey.PlaceAsync($"status-conflict-{path}", 103);
        using var admin = _factory.CreateClient().AsTrueAdmin();

        foreach (var status in moves[..^1])
        {
            Assert.Equal(HttpStatusCode.OK, (await MoveAsync(admin, orderId, status)).StatusCode);
        }
        var refused = await MoveAsync(admin, orderId, moves[^1]);

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("application/problem+json", refused.Content.Headers.ContentType?.MediaType);
        Assert.Equal(moves.Length, History(await _journey.OrderAsync(orderId)).Count);
    }

    [Fact]
    public async Task Paying_is_not_for_the_administrator_to_set()
    {
        var orderId = await _journey.AwaitingPaymentAsync("status-not-by-hand", 106);
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var paid = await MoveAsync(admin, orderId, "Paid");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, paid.StatusCode);
        Assert.Equal("AwaitingPayment", (await _journey.OrderAsync(orderId)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task The_demo_administrator_and_customers_may_not_change_an_order()
    {
        const string user = "status-forbidden";
        var orderId = await _journey.PlaceAsync(user, 104);

        using var demo = _factory.CreateClient().AsDemoAdmin();
        using var customer = _factory.CreateClient().AsUser(user);
        using var anonymous = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await MoveAsync(demo, orderId, "Cancelled")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await MoveAsync(customer, orderId, "Cancelled")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await MoveAsync(anonymous, orderId, "Cancelled")).StatusCode);
        var order = await ReadJson(await customer.GetAsync($"/api/v1/orders/{orderId}"));
        Assert.Equal("Placed", order.GetProperty("status").GetString());
    }

    [Fact]
    public async Task An_unknown_status_is_a_validation_error_and_an_unknown_order_is_not_found()
    {
        var orderId = await _journey.PlaceAsync("status-invalid", 105);
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var invalid = await MoveAsync(admin, orderId, "Delivered");
        var missing = await MoveAsync(admin, 999_999, "Cancelled");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        Assert.True((await ReadJson(invalid)).GetProperty("errors").TryGetProperty("Status", out _));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    // The About page prints it; an order waiting for its payment or cancelled after it is not one the shop may boast of
    [Fact]
    public async Task The_public_order_count_holds_the_orders_paid_for_and_kept()
    {
        using var anonymous = _factory.CreateClient();
        async Task<int> PaidOrdersAsync()
            => (await ReadJson(await anonymous.GetAsync("/api/v1/orders/stats"))).GetProperty("paidOrders").GetInt32();
        var before = await PaidOrdersAsync();

        await _journey.AwaitingPaymentAsync("stats-awaiting", 107);
        var shipped = await _journey.PaidAsync("stats-shipped", 108);
        var cancelled = await _journey.PaidAsync("stats-cancelled", 109);
        Assert.Equal(before + 2, await PaidOrdersAsync());
        using var admin = _factory.CreateClient().AsTrueAdmin();
        Assert.Equal(HttpStatusCode.OK, (await MoveAsync(admin, shipped, "Shipped")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await MoveAsync(admin, cancelled, "Cancelled")).StatusCode);

        Assert.Equal(before + 1, await PaidOrdersAsync());
    }
}
