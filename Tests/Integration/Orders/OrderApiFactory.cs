using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Store.Contracts.Cart;
using Store.Contracts.Catalog;
using Store.OrderService.Data;
using Store.Tests.Integration.TestSupport;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

namespace Store.Tests.Integration.Orders;

/// <summary>
/// OrderService against its real database. The cart and the catalogue are replaced at the HTTP
/// boundary by <see cref="FakeUpstreams"/>; the resilience pipeline, the typed clients and the
/// outbox stay real, with the bus on the in-memory transport.
/// </summary>
public sealed class OrderApiFactory : StoreApiFactory<OrderDbContext>
{
    public OrderApiFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    public FakeUpstreams Upstreams { get; } = new();

    protected override string? DatabaseName => "store_order_test";

    protected override void ConfigureSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("Services:CartService", "http://cart.test");
        builder.UseSetting("Services:ProductService", "http://catalog.test");
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => Upstreams));
    }
}

/// <summary>
/// Plays the cart and the catalogue for the order service under test.
/// </summary>
public sealed class FakeUpstreams : HttpMessageHandler
{
    private readonly ConcurrentDictionary<string, CartSnapshot> _carts = new();
    private readonly ConcurrentDictionary<int, ProductSnapshot> _products = new();


    public void AddProduct(int id, decimal effectivePrice, string title = "Fake product", bool isActive = true)
        => _products[id] = new ProductSnapshot(id, title, "https://example.test/fake.jpg", "Modenza", new[] { "black" }, effectivePrice, effectivePrice, isActive, DateTime.UtcNow);

    public void SetCart(string userId, params (int ProductId, int Quantity, decimal PriceInCart)[] lines)
        => _carts[userId] = new CartSnapshot(
            userId,
            lines.Select(l => new CartLineSnapshot(l.ProductId, $"Line {l.ProductId}", "https://example.test/fake.jpg", "Modenza", "black", l.PriceInCart, l.Quantity)).ToList(),
            DateTime.UtcNow);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var host = request.RequestUri?.Host ?? string.Empty;
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (host == "cart.test" && request.Method == HttpMethod.Get && path.StartsWith("/api/cart/internal/", StringComparison.Ordinal))
        {
            var userId = Uri.UnescapeDataString(path["/api/cart/internal/".Length..]);
            return Task.FromResult(_carts.TryGetValue(userId, out var cart)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(cart) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        if (host == "catalog.test" && request.Method == HttpMethod.Get
            && path.StartsWith("/api/products/", StringComparison.Ordinal) && path.EndsWith("/snapshot", StringComparison.Ordinal)
            && int.TryParse(path["/api/products/".Length..^"/snapshot".Length], out var productId))
        {
            return Task.FromResult(_products.TryGetValue(productId, out var product)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(product) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }


        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
    }
}
