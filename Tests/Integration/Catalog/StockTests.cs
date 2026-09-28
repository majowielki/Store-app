using Store.Contracts.Catalog.V1;
using Store.Contracts.Orders.V1;
using Store.ProductService.Models;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Catalog;

/// <summary>
/// The stock follows the order events: a placed order holds every line or nothing, a cancelled
/// one gives its units back (even when the cancellation arrives first), a shipped one takes them
/// off the stock. Visitors waiting for a product are told once it is back.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class StockTests : IClassFixture<CatalogApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static int _lastOrderId = 5000;

    private readonly CatalogApiFactory _factory;

    public StockTests(CatalogApiFactory factory)
    {
        _factory = factory;
    }

    private static int NextOrderId() => Interlocked.Increment(ref _lastOrderId);

    private async Task<int> ProductAsync(string name, int stock)
    {
        using var admin = _factory.CreateClient().AsTrueAdmin();
        var response = await admin.PostAsJsonAsync("/api/v1/products", new
        {
            title = $"Stock test {name} {Guid.NewGuid():N}",
            description = "A product the stock tests reserve, release and ship.",
            price = 100m,
            category = 1,
            company = 1,
            image = "https://example.test/stock.jpg",
            colors = new[] { FinishCatalogue.BlackSteelOak.Key },
            stockQuantity = stock
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetInt32();
    }

    private async Task<JsonElement> AdminViewAsync(int productId)
    {
        using var admin = _factory.CreateClient().AsTrueAdmin();
        return JsonSerializer.Deserialize<JsonElement>(await admin.GetStringAsync($"/api/v1/products/admin/{productId}"), Json);
    }

    private async Task<int> AvailableAsync(int productId)
        => (await AdminViewAsync(productId)).GetProperty("availableQuantity").GetInt32();

    private static OrderPlaced Order(int orderId, params (int ProductId, int Quantity)[] lines) => new(
        orderId, "stock-buyer", "buyer@test.local", "Buyer", "1 Test Street", SaveAddress: false,
        Subtotal: 100m, DiscountAmount: 0m, DeliveryFee: 0m, Total: 100m,
        Lines: lines.Select(l => new OrderPlacedLine(l.ProductId, $"Product {l.ProductId}", l.Quantity, 100m)).ToList(),
        PlacedAt: DateTime.UtcNow);

    private Task Publish<T>(T message) where T : class => _factory.Bus.Bus.Publish(message);

    private Task<bool> ConsumedAsync<T>(Func<T, bool> match) where T : class
        => Eventually.BecomesTrueAsync(() => _factory.Bus.Consumed.Select<T>(e => match(e.Context.Message)).Any());

    private int ConsumedCount<T>(Func<T, bool> match) where T : class
        => _factory.Bus.Consumed.Select<T>(e => match(e.Context.Message)).Count();

    [Fact]
    public async Task Placed_order_holds_every_line()
    {
        var sofa = await ProductAsync("sofa", stock: 6);
        var lamp = await ProductAsync("lamp", stock: 2);
        var orderId = NextOrderId();

        await Publish(Order(orderId, (sofa, 2), (lamp, 2)));

        Assert.True(await ConsumedAsync<StockReserved>(e => e.OrderId == orderId && e.Lines.Count == 2));
        var sofaView = await AdminViewAsync(sofa);
        Assert.Equal(6, sofaView.GetProperty("stockQuantity").GetInt32());
        Assert.Equal(2, sofaView.GetProperty("reservedQuantity").GetInt32());
        Assert.Equal(4, sofaView.GetProperty("availableQuantity").GetInt32());
        Assert.Equal("inStock", sofaView.GetProperty("availability").GetString());
        Assert.Equal("outOfStock", (await AdminViewAsync(lamp)).GetProperty("availability").GetString());
    }

    [Fact]
    public async Task Order_a_line_cannot_be_served_for_holds_nothing()
    {
        var table = await ProductAsync("table", stock: 5);
        var chair = await ProductAsync("chair", stock: 1);
        var orderId = NextOrderId();

        await Publish(Order(orderId, (table, 1), (chair, 2)));

        Assert.True(await ConsumedAsync<StockUnavailable>(e => e.OrderId == orderId
            && e.Shortages.Single() is { Requested: 2, Available: 1 } shortage && shortage.ProductId == chair));
        Assert.Equal(5, await AvailableAsync(table));
        Assert.Equal(1, await AvailableAsync(chair));
    }

    [Fact]
    public async Task Two_orders_for_the_last_unit_get_one_reservation()
    {
        var vase = await ProductAsync("vase", stock: 1);
        var first = NextOrderId();
        var second = NextOrderId();

        await Task.WhenAll(Publish(Order(first, (vase, 1))), Publish(Order(second, (vase, 1))));

        Assert.True(await ConsumedAsync<StockReserved>(e => e.OrderId == first || e.OrderId == second));
        Assert.True(await ConsumedAsync<StockUnavailable>(e => e.OrderId == first || e.OrderId == second));
        Assert.Equal(1, ConsumedCount<StockReserved>(e => e.OrderId == first || e.OrderId == second));
        var view = await AdminViewAsync(vase);
        Assert.Equal(1, view.GetProperty("reservedQuantity").GetInt32());
        Assert.Equal(0, view.GetProperty("availableQuantity").GetInt32());
    }

    [Fact]
    public async Task Cancelled_order_gives_its_units_back_once()
    {
        var rug = await ProductAsync("rug", stock: 4);
        var orderId = NextOrderId();
        await Publish(Order(orderId, (rug, 3)));
        Assert.True(await ConsumedAsync<StockReserved>(e => e.OrderId == orderId));

        await Publish(new OrderCancelled(orderId, "stock-buyer", OrderCancellationReasons.PaymentTimedOut, DateTime.UtcNow));
        Assert.True(await ConsumedAsync<StockReleased>(e => e.OrderId == orderId && e.Lines.Single().Quantity == 3));
        Assert.Equal(4, await AvailableAsync(rug));

        // A second cancellation (another message) finds nothing left to give back
        await Publish(new OrderCancelled(orderId, "stock-buyer", OrderCancellationReasons.ByAdministrator, DateTime.UtcNow));
        Assert.True(await Eventually.BecomesTrueAsync(() => ConsumedCount<OrderCancelled>(e => e.OrderId == orderId) >= 2));
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        Assert.Equal(4, await AvailableAsync(rug));
        Assert.Equal(1, ConsumedCount<StockReleased>(e => e.OrderId == orderId));
    }

    [Fact]
    public async Task Cancellation_that_overtakes_its_order_keeps_the_order_from_reserving()
    {
        var mirror = await ProductAsync("mirror", stock: 3);
        var orderId = NextOrderId();

        await Publish(new OrderCancelled(orderId, "stock-buyer", OrderCancellationReasons.ByAdministrator, DateTime.UtcNow));
        Assert.True(await ConsumedAsync<StockReleased>(e => e.OrderId == orderId && e.Lines.Count == 0));

        await Publish(Order(orderId, (mirror, 2)));
        Assert.True(await Eventually.BecomesTrueAsync(() => ConsumedCount<OrderPlaced>(e => e.OrderId == orderId) >= 1));
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        Assert.Equal(3, await AvailableAsync(mirror));
        Assert.Equal(0, ConsumedCount<StockReserved>(e => e.OrderId == orderId));
    }

    [Fact]
    public async Task Shipped_order_takes_its_units_off_the_stock()
    {
        var bench = await ProductAsync("bench", stock: 4);
        var orderId = NextOrderId();
        await Publish(Order(orderId, (bench, 2)));
        Assert.True(await ConsumedAsync<StockReserved>(e => e.OrderId == orderId));

        await Publish(new OrderShipped(orderId, "stock-buyer", "buyer@test.local", "Buyer",
            [new OrderItem(bench, "Bench", 2, 100m)], null, null, DateTime.UtcNow));

        await Eventually.AssertAsync(async () =>
        {
            var view = await AdminViewAsync(bench);
            Assert.Equal(2, view.GetProperty("stockQuantity").GetInt32());
            Assert.Equal(0, view.GetProperty("reservedQuantity").GetInt32());
        });

        // Shipped is final: a late cancellation gives nothing back
        await Publish(new OrderCancelled(orderId, "stock-buyer", OrderCancellationReasons.ByAdministrator, DateTime.UtcNow));
        Assert.True(await Eventually.BecomesTrueAsync(() => ConsumedCount<OrderCancelled>(e => e.OrderId == orderId) >= 1));
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        Assert.Equal(2, await AvailableAsync(bench));
    }

    [Fact]
    public async Task Visitors_waiting_for_a_product_hear_once_the_administrator_restocks_it()
    {
        var stool = await ProductAsync("stool", stock: 0);
        using var visitor = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.NoContent, (await visitor.PostAsJsonAsync($"/api/v1/products/{stool}/notify", new { email = "Wait@Test.Local" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await visitor.PostAsJsonAsync($"/api/v1/products/{stool}/notify", new { email = "wait@test.local" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await visitor.PostAsJsonAsync($"/api/v1/products/{stool}/notify", new { email = "not an address" })).StatusCode);

        using var admin = _factory.CreateClient().AsTrueAdmin();
        var restock = await admin.PutAsJsonAsync($"/api/v1/products/{stool}/stock", new { stockQuantity = 6 });
        Assert.Equal(HttpStatusCode.OK, restock.StatusCode);
        Assert.Equal(6, (await restock.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("availableQuantity").GetInt32());

        Assert.True(await ConsumedAsync<ProductBackInStock>(e => e.ProductId == stool && e.SubscriberEmail == "wait@test.local"));
        await admin.PutAsJsonAsync($"/api/v1/products/{stool}/stock", new { stockQuantity = 7 });
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        Assert.Equal(1, ConsumedCount<ProductBackInStock>(e => e.ProductId == stool));

        // In stock again: nothing to wait for
        Assert.Equal(HttpStatusCode.Conflict, (await visitor.PostAsJsonAsync($"/api/v1/products/{stool}/notify", new { email = "late@test.local" })).StatusCode);
    }

    [Fact]
    public async Task Units_given_back_by_a_cancelled_order_reach_the_waiting_visitors()
    {
        var lamp = await ProductAsync("last lamp", stock: 1);
        var orderId = NextOrderId();
        await Publish(Order(orderId, (lamp, 1)));
        Assert.True(await ConsumedAsync<StockReserved>(e => e.OrderId == orderId));

        using var visitor = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.NoContent, (await visitor.PostAsJsonAsync($"/api/v1/products/{lamp}/notify", new { email = "lamp@test.local" })).StatusCode);

        await Publish(new OrderCancelled(orderId, "stock-buyer", OrderCancellationReasons.PaymentTimedOut, DateTime.UtcNow));

        Assert.True(await ConsumedAsync<ProductBackInStock>(e => e.ProductId == lamp && e.SubscriberEmail == "lamp@test.local"));
    }

    [Fact]
    public async Task Stock_cannot_go_below_what_orders_hold_and_only_the_true_admin_sets_it()
    {
        var shelf = await ProductAsync("shelf", stock: 3);
        var orderId = NextOrderId();
        await Publish(Order(orderId, (shelf, 2)));
        Assert.True(await ConsumedAsync<StockReserved>(e => e.OrderId == orderId));

        using var admin = _factory.CreateClient().AsTrueAdmin();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PutAsJsonAsync($"/api/v1/products/{shelf}/stock", new { stockQuantity = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PutAsJsonAsync($"/api/v1/products/{shelf}/stock", new { stockQuantity = -1 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/products/{shelf}/stock", new { stockQuantity = 2 })).StatusCode);

        using var demoAdmin = _factory.CreateClient().AsDemoAdmin();
        Assert.Equal(HttpStatusCode.Forbidden, (await demoAdmin.PutAsJsonAsync($"/api/v1/products/{shelf}/stock", new { stockQuantity = 9 })).StatusCode);
    }

    [Fact]
    public async Task Public_catalogue_shows_availability_but_not_the_stock_figures()
    {
        var chest = await ProductAsync("chest", stock: 2);
        using var visitor = _factory.CreateClient();

        var product = JsonSerializer.Deserialize<JsonElement>(await visitor.GetStringAsync($"/api/v1/products/{chest}"), Json);

        Assert.Equal("lowStock", product.GetProperty("availability").GetString());
        Assert.Equal(2, product.GetProperty("availableQuantity").GetInt32());
        Assert.False(product.TryGetProperty("stockQuantity", out _));
        Assert.False(product.TryGetProperty("reservedQuantity", out _));

        using var service = _factory.CreateClient();
        service.DefaultRequestHeaders.Add("X-Internal-Api-Key", TestTokens.InternalApiKey);
        var snapshot = JsonSerializer.Deserialize<JsonElement>(await service.GetStringAsync($"/api/v1/products/{chest}/snapshot"), Json);
        Assert.Equal(2, snapshot.GetProperty("availableQuantity").GetInt32());
    }
}
