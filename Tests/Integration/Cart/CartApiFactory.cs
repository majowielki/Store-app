using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Store.CartService.Data;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;

namespace Store.Tests.Integration.Cart;

/// <summary>
/// CartService against its real database, with ProductService replaced at the HTTP boundary by
/// <see cref="FakeCatalog"/> - the only other service the cart talks to.
/// </summary>
public sealed class CartApiFactory : StoreApiFactory<CartDbContext>
{
    public CartApiFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    public FakeCatalog Catalog { get; } = new();

    protected override string? DatabaseName => "store_cart_test";

    protected override void ConfigureSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("Services:ProductService", "http://catalog.test");
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        // Typed clients keep their resilience pipeline; only the network is replaced by the fake
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => Catalog));
    }
}

/// <summary>
/// Answers GET /api/v1/products/{id}/snapshot the way the catalogue does, for the products a test
/// registers. Unknown ids get 404, like a product that was never created.
/// </summary>
public sealed class FakeCatalog : HttpMessageHandler
{
    private readonly Dictionary<int, object> _products = new();

    public void Add(int id, decimal price, decimal? salePrice = null, decimal? discountPercent = null, string title = "Fake product", bool isActive = true)
    {
        var effective = salePrice ?? (discountPercent is > 0 ? Math.Round(price * (1 - discountPercent.Value / 100m), 2) : price);
        _products[id] = new
        {
            id,
            title,
            image = "https://example.test/fake.jpg",
            company = "Modenza",
            colors = new[] { "black" },
            price,
            effectivePrice = effective,
            isActive,
            updatedAt = DateTime.UtcNow
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        const string prefix = "/api/v1/products/";
        const string suffix = "/snapshot";
        if (request.Method == HttpMethod.Get && path.StartsWith(prefix, StringComparison.Ordinal) && path.EndsWith(suffix, StringComparison.Ordinal)
            && int.TryParse(path[prefix.Length..^suffix.Length], out var id) && _products.TryGetValue(id, out var product))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(product) });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
    }
}
