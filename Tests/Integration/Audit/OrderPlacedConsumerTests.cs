using Store.Contracts.Orders.V1;
using Store.Tests.Integration.TestSupport;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Audit;

/// <summary>A placed order becomes an audit row with identifiers and amounts, never the address.</summary>
[Collection(PostgresTests.Name)]
public sealed class OrderPlacedConsumerTests : IClassFixture<AuditApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly AuditApiFactory _factory;

    public OrderPlacedConsumerTests(AuditApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Placed_order_is_recorded_without_personal_data()
    {
        const string user = "audit-consumer-user";
        var placed = new OrderPlaced(
            701, user, "buyer@test.local", "Audit Buyer", "9 Private Street", SaveAddress: true,
            Subtotal: 200m, DiscountAmount: 40m, DeliveryFee: 0m, Total: 160m,
            Lines: new[] { new OrderPlacedLine(3, "Sofa", 1, 200m) }, PlacedAt: DateTime.UtcNow);

        await _factory.Bus.Bus.Publish(placed);

        using var admin = _factory.CreateClient().AsTrueAdmin();
        JsonElement entry = default;
        await Eventually.AssertAsync(async () =>
        {
            var page = JsonSerializer.Deserialize<JsonElement>(await admin.GetStringAsync("/api/auditlog/entity/Order?entityId=701"), Json);
            entry = Assert.Single(page.GetProperty("auditLogs").EnumerateArray());
        });
        Assert.Equal("ORDER_PLACED", entry.GetProperty("action").GetString());
        Assert.Equal(user, entry.GetProperty("userId").GetString());
        Assert.Equal("order", entry.GetProperty("serviceName").GetString());
        var details = entry.GetProperty("details").GetString()!;
        Assert.Contains("160", details);
        Assert.DoesNotContain("Private Street", details);
        Assert.DoesNotContain("buyer@test.local", details);
    }
}
