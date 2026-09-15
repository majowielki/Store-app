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
        // The cart resolves a plain HttpClient from the factory; route every request through the fake
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => Catalog));
    }
}

/// <summary>
/// Answers GET /api/products/{id} in the catalogue's public JSON shape for the products a test registers.
/// </summary>
public sealed class FakeCatalog : HttpMessageHandler
{
    private readonly Dictionary<int, object> _products = new();

    public void Add(int id, decimal price, decimal? salePrice = null, decimal? discountPercent = null, string title = "Fake product")
    {
        var effective = salePrice ?? (discountPercent is > 0 ? Math.Round(price * (1 - discountPercent.Value / 100m), 2) : price);
        _products[id] = new
        {
            data = new
            {
                id,
                attributes = new
                {
                    title,
                    description = "fake",
                    image = "https://example.test/fake.jpg",
                    category = "sofas",
                    company = "modenza",
                    price = price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    salePrice = salePrice?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    discountPercent,
                    effectivePrice = effective.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    colors = new[] { "black" },
                    createdAt = "2026-01-01T00:00:00.000Z",
                    updatedAt = "2026-01-01T00:00:00.000Z",
                    publishedAt = "2026-01-01T00:00:00.000Z"
                }
            },
            meta = new { }
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var prefix = "/api/products/";
        if (request.Method == HttpMethod.Get && path.StartsWith(prefix, StringComparison.Ordinal)
            && int.TryParse(path[prefix.Length..], out var id) && _products.TryGetValue(id, out var product))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(product) });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
    }
}
