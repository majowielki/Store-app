using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Store.AuditLogService.Data;
using Store.AuditLogService.Models;
using Store.AuditLogService.Services;
using Store.Contracts.Authorization;
using Store.Contracts.Orders.V1;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Audit;

/// <summary>
/// The purchase funnel (ADR 020): the shop's pages count views and additions to the bag, the order
/// events count the orders, and the funnel adds them up over the dashboard's window. The database is
/// shared with the other audit tests, so every check compares before and after.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class PurchaseFunnelTests : IClassFixture<AuditApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static int _lastOrderId = 91000;

    private readonly AuditApiFactory _factory;

    public PurchaseFunnelTests(AuditApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<Dictionary<string, int>> FunnelAsync(int days = PurchaseFunnel.DefaultDays)
    {
        using var admin = _factory.CreateClient().AsDemoAdmin();
        var funnel = JsonSerializer.Deserialize<JsonElement>(await admin.GetStringAsync($"/api/v1/auditlog/funnel?days={days}"), Json);
        return funnel.GetProperty("stages").EnumerateArray().ToDictionary(s => s.GetProperty("stage").GetString()!, s => s.GetProperty("count").GetInt32());
    }

    private async Task RecordAsync(string kind, int productId)
    {
        using var visitor = _factory.CreateClient();
        var response = await visitor.PostAsJsonAsync("/api/v1/shop-events", new { kind, productId });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    private static OrderPlaced Placed(DateTime at) => new(
        Interlocked.Increment(ref _lastOrderId), "funnel-buyer", "buyer@test.local", "Funnel Buyer", null, SaveAddress: false,
        Subtotal: 100m, DiscountAmount: 0m, DeliveryFee: 0m, Total: 100m, Lines: [new OrderPlacedLine(5, "Chair", 1, 100m)], PlacedAt: at);

    [Fact]
    public async Task Views_and_bag_additions_from_the_pages_and_the_orders_of_the_window_make_the_funnel()
    {
        var before = await FunnelAsync();

        await RecordAsync("productViewed", 5);
        await RecordAsync("productViewed", 5);
        await RecordAsync("productViewed", 6);
        await RecordAsync("addedToBag", 5);
        var today = Placed(DateTime.UtcNow);
        var longAgo = Placed(DateTime.UtcNow.Date.AddDays(-PurchaseFunnel.DefaultDays - 5));
        await _factory.Bus.Bus.Publish(today);
        await _factory.Bus.Bus.Publish(longAgo);
        Assert.True(await Eventually.BecomesTrueAsync(() => Task.FromResult(
            _factory.Bus.Consumed.Select<OrderPlaced>(e => e.Context.Message.OrderId == today.OrderId || e.Context.Message.OrderId == longAgo.OrderId).Count() == 2)));

        await Eventually.AssertAsync(async () =>
        {
            var after = await FunnelAsync();
            Assert.Equal(before["productViewed"] + 3, after["productViewed"]);
            Assert.Equal(before["addedToBag"] + 1, after["addedToBag"]);
            // Only the order placed inside the window counts, as on the dashboard
            Assert.Equal(before["orderPlaced"] + 1, after["orderPlaced"]);
        });
        Assert.Equal(["productViewed", "addedToBag", "orderPlaced"], (await FunnelAsync()).Keys);
    }

    [Theory]
    [InlineData("""{"productId":5}""")]
    [InlineData("""{"kind":"productViewed","productId":0}""")]
    [InlineData("""{"kind":"purchased","productId":5}""")]
    public async Task A_step_the_funnel_does_not_know_is_refused(string body)
    {
        using var visitor = _factory.CreateClient();

        var response = await visitor.PostAsync("/api/v1/shop-events", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData(Roles.User, HttpStatusCode.Forbidden)]
    [InlineData(Roles.DemoAdmin, HttpStatusCode.OK)]
    public async Task Only_administrators_read_the_funnel(string who, HttpStatusCode expected)
    {
        using var client = _factory.CreateClient().As(who);

        Assert.Equal(expected, (await client.GetAsync("/api/v1/auditlog/funnel")).StatusCode);
    }

    [Fact]
    public async Task Steps_older_than_the_retention_period_are_purged_with_the_entries()
    {
        long old;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AuditLogDbContext>();
            var entry = new ShopEvent { Kind = ShopEventKind.ProductViewed, ProductId = 9, OccurredAt = DateTime.UtcNow.AddDays(-400) };
            context.ShopEvents.Add(entry);
            await context.SaveChangesAsync();
            old = entry.Id;
        }

        var retention = _factory.Services.GetServices<IHostedService>().OfType<AuditRetentionService>().Single();
        await retention.PurgeAsync(CancellationToken.None);

        using var check = _factory.Services.CreateScope();
        Assert.Null(await check.ServiceProvider.GetRequiredService<AuditLogDbContext>().ShopEvents.FindAsync(old));
    }

    [Fact]
    public async Task Replayed_order_with_a_new_transport_id_is_counted_once()
    {
        var before = await FunnelAsync();
        var order = Placed(DateTime.UtcNow);
        await _factory.Bus.Bus.Publish(order);
        await Eventually.AssertAsync(async () => Assert.Equal(before["orderPlaced"] + 1, (await FunnelAsync())["orderPlaced"]));
        await _factory.Bus.Bus.Publish(order, c => c.MessageId = Guid.NewGuid());
        Assert.True(await Eventually.BecomesTrueAsync(() => Task.FromResult(_factory.Bus.Consumed.Select<OrderPlaced>(e => e.Context.Message.OrderId == order.OrderId).Count() == 2)));
        Assert.Equal(before["orderPlaced"] + 1, (await FunnelAsync())["orderPlaced"]);
    }

    [Fact]
    public async Task Retention_preserves_the_whole_oldest_funnel_day()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));
        var options = Options.Create(new AuditRetentionOptions { RetentionDays = 3 });
        var boundary = clock.GetUtcNow().UtcDateTime.Date.AddDays(-3);
        long oldest;
        long expired;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuditLogDbContext>();
            var retained = new ShopEvent { Kind = ShopEventKind.ProductViewed, ProductId = 990002, OccurredAt = boundary.AddHours(1) };
            var removed = new ShopEvent { Kind = ShopEventKind.ProductViewed, ProductId = 990002, OccurredAt = boundary.AddTicks(-10) };
            db.ShopEvents.AddRange(retained, removed);
            await db.SaveChangesAsync();
            oldest = retained.Id;
            expired = removed.Id;
            var funnel = await new PurchaseFunnel(db, options, clock).CountAsync(30);
            Assert.Equal(3, funnel.Days);
            Assert.Equal(boundary, funnel.Since);
        }
        var retention = new AuditRetentionService(_factory.Services.GetRequiredService<IServiceScopeFactory>(), options, clock, NullLogger<AuditRetentionService>.Instance);
        await retention.PurgeAsync(CancellationToken.None);
        using var check = _factory.Services.CreateScope();
        var context = check.ServiceProvider.GetRequiredService<AuditLogDbContext>();
        Assert.True(await context.ShopEvents.AnyAsync(e => e.Id == oldest));
        Assert.False(await context.ShopEvents.AnyAsync(e => e.Id == expired));
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
