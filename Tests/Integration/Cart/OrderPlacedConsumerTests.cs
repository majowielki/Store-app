using MassTransit;
using Store.Contracts.Audit.V1;
using Store.Contracts.Orders.V1;
using Store.Tests.Integration.TestSupport;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Cart;

/// <summary>
/// The cart reacts to the order service's event instead of being called over HTTP with the
/// customer's token: an order placed while the cart service was down is still applied once
/// the service is back, and a redelivered event does not touch the cart twice.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class OrderPlacedConsumerTests : IClassFixture<CartApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CartApiFactory _factory;

    public OrderPlacedConsumerTests(CartApiFactory factory)
    {
        _factory = factory;
    }

    private static OrderPlaced OrderFor(string userId, int orderId) => new(
        orderId, userId, $"{userId}@test.local", "Buyer", "1 Test Street", SaveAddress: false,
        Subtotal: 10m, DiscountAmount: 0m, DeliveryFee: 10m, Total: 20m,
        Lines: new[] { new OrderPlacedLine(9101, "Ten", 1, 10m) }, PlacedAt: DateTime.UtcNow);

    private static async Task<int> LineCount(HttpClient client)
        => JsonSerializer.Deserialize<JsonElement>(await client.GetStringAsync("/api/cart"), Json)
            .GetProperty("data").GetProperty("items").GetArrayLength();

    [Fact]
    public async Task Placed_order_empties_the_cart()
    {
        const string user = "cart-consumer-clear";
        _factory.Catalog.Add(id: 9101, price: 10m, title: "Ten");
        using var client = _factory.CreateClient().AsUser(user);
        await client.PostAsJsonAsync("/api/cart/items", new { productId = 9101, quantity = 2, color = "black" });
        Assert.Equal(1, await LineCount(client));

        await _factory.Bus.Bus.Publish(OrderFor(user, orderId: 501));

        await Eventually.AssertAsync(async () => Assert.Equal(0, await LineCount(client)));
        Assert.True(await Eventually.BecomesTrueAsync(() => _factory.Bus.Consumed
            .Select<AuditEvent>(e => e.Context.Message.Action == "CART_CLEARED" && e.Context.Message.UserId == user).Any()));
    }

    [Fact]
    public async Task Redelivered_event_is_ignored_by_the_inbox()
    {
        const string user = "cart-consumer-dedupe";
        _factory.Catalog.Add(id: 9101, price: 10m, title: "Ten");
        using var client = _factory.CreateClient().AsUser(user);
        await client.PostAsJsonAsync("/api/cart/items", new { productId = 9101, quantity = 1, color = "black" });
        var messageId = Guid.NewGuid();

        await _factory.Bus.Bus.Publish(OrderFor(user, orderId: 502), context => context.MessageId = messageId);
        await Eventually.AssertAsync(async () => Assert.Equal(0, await LineCount(client)));

        // The customer starts a new cart, then the broker delivers the same message again
        await client.PostAsJsonAsync("/api/cart/items", new { productId = 9101, quantity = 1, color = "white" });
        await _factory.Bus.Bus.Publish(OrderFor(user, orderId: 502), context => context.MessageId = messageId);
        await Task.Delay(TimeSpan.FromSeconds(2));

        Assert.Equal(1, await LineCount(client));
    }
}
