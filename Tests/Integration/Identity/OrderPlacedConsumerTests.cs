using Store.Contracts.Orders.V1;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Identity;

/// <summary>
/// The delivery address reaches the profile through the order-placed event; the order service
/// no longer calls the identity service with the customer's own token.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class OrderPlacedConsumerTests : IClassFixture<IdentityApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IdentityApiFactory _factory;

    public OrderPlacedConsumerTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<(string UserId, string Token)> RegisterAsync(HttpClient client)
    {
        var email = $"consumer-{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Consumer-Password-1!",
            confirmPassword = "Consumer-Password-1!",
            firstName = "Event",
            lastName = "Buyer"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);
        return (data.GetProperty("user").GetProperty("id").GetString()!, data.GetProperty("accessToken").GetString()!);
    }

    private static async Task<string?> AddressOf(HttpClient client)
    {
        var me = JsonSerializer.Deserialize<JsonElement>(await client.GetStringAsync("/api/v1/auth/me"), Json);
        return me.TryGetProperty("simpleAddress", out var address) ? address.GetString() : null;
    }

    private static OrderPlaced OrderFor(string userId, int orderId, bool saveAddress, string address = "7 Event Lane") => new(
        orderId, userId, $"{userId}@test.local", "Event Buyer", address, saveAddress,
        Subtotal: 100m, DiscountAmount: 0m, DeliveryFee: 10m, Total: 110m,
        Lines: new[] { new OrderPlacedLine(1, "Lamp", 1, 100m) }, PlacedAt: DateTime.UtcNow);

    [Fact]
    public async Task Address_is_saved_when_the_checkout_asked_for_it()
    {
        using var client = _factory.CreateClient();
        var (userId, token) = await RegisterAsync(client);
        client.WithToken(token);

        await _factory.Bus.Bus.Publish(OrderFor(userId, orderId: 601, saveAddress: true));

        await Eventually.AssertAsync(async () => Assert.Equal("7 Event Lane", await AddressOf(client)));
    }

    [Fact]
    public async Task Address_is_left_alone_when_the_checkout_did_not_ask()
    {
        using var client = _factory.CreateClient();
        var (userId, token) = await RegisterAsync(client);
        client.WithToken(token);

        await _factory.Bus.Bus.Publish(OrderFor(userId, orderId: 602, saveAddress: false));

        Assert.True(await Eventually.BecomesTrueAsync(() => _factory.Bus.Consumed.Select<OrderPlaced>(c => c.Context.Message.OrderId == 602).Any()));
        Assert.Null(await AddressOf(client));
    }
}
