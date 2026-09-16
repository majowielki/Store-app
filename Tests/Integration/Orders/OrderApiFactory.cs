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
/// OrderService against its real database. The cart, the catalogue and the identity service
/// are replaced at the HTTP boundary by <see cref="FakeUpstreams"/>; the resilience pipeline
/// and the typed clients stay real.
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
        builder.UseSetting("Services:IdentityService", "http://identity.test");
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => Upstreams));
    }
}

/// <summary>
/// Plays the cart, the catalogue and the identity service for the order service under test.
/// </summary>
public sealed class FakeUpstreams : HttpMessageHandler
{
    private readonly ConcurrentDictionary<string, CartSnapshot> _carts = new();
    private readonly ConcurrentDictionary<int, ProductSnapshot> _products = new();

    /// <summary>Users whose cart the order service asked to clear.</summary>
    public ConcurrentQueue<string> ClearedCarts { get; } = new();

    /// <summary>Addresses the order service asked the identity service to save.</summary>
    public ConcurrentQueue<string> SavedAddresses { get; } = new();

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

        if (host == "cart.test" && path.StartsWith("/api/cart/internal/", StringComparison.Ordinal))
        {
            var userId = Uri.UnescapeDataString(path["/api/cart/internal/".Length..]);
            if (request.Method == HttpMethod.Get)
            {
                return Task.FromResult(_carts.TryGetValue(userId, out var cart)
                    ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(cart) }
                    : new HttpResponseMessage(HttpStatusCode.NotFound));
            }
            if (request.Method == HttpMethod.Delete)
            {
                ClearedCarts.Enqueue(userId);
                _carts.TryRemove(userId, out _);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }
        }

        if (host == "catalog.test" && request.Method == HttpMethod.Get
            && path.StartsWith("/api/products/", StringComparison.Ordinal) && path.EndsWith("/snapshot", StringComparison.Ordinal)
            && int.TryParse(path["/api/products/".Length..^"/snapshot".Length], out var productId))
        {
            return Task.FromResult(_products.TryGetValue(productId, out var product)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(product) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        if (host == "identity.test" && request.Method == HttpMethod.Put && path == "/api/auth/me/address")
        {
            SavedAddresses.Enqueue(request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult() ?? string.Empty);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
    }
}
