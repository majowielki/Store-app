using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Store.Contracts.Reviews.V1;
using Store.ReviewService.Data;
using Store.Tests.Integration.TestSupport;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

namespace Store.Tests.Integration.Reviews;

/// <summary>
/// ReviewService against its real database, with the catalogue played at the HTTP boundary by
/// <see cref="FakeCatalog"/> (the typed client and its resilience pipeline stay real) and the
/// ratings it publishes caught by <see cref="ReviewSummaryProbe"/>.
/// </summary>
public sealed class ReviewApiFactory : StoreApiFactory<ReviewDbContext>
{
    public ReviewApiFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    public FakeCatalog Catalog { get; } = new();

    protected override string? DatabaseName => "store_review_test";

    protected override void ConfigureSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("Services:ProductService", "http://catalog.test");
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => Catalog));
    }

    protected override void ConfigureTestBus(IBusRegistrationConfigurator bus)
    {
        AddProbe<ReviewSummaryProbe>(bus);
    }
}

/// <summary>
/// Plays the catalogue's product listing filtered by slugs: the products of the demo catalogue get
/// ids from 9000 up, in slug order, except the ones kept out (<see cref="LateSlug"/> at first).
/// </summary>
public sealed class FakeCatalog : HttpMessageHandler
{
    public const int FirstId = 9000;

    /// <summary>A product the catalogue does not know when the service starts, to see its reviews wait.</summary>
    public const string LateSlug = "walnut-sideboard";

    private readonly ConcurrentDictionary<string, bool> _hidden = new(StringComparer.Ordinal) { [LateSlug] = true };

    public static IReadOnlyDictionary<string, int> Ids { get; } = DemoReviews.Products.Keys
        .Order(StringComparer.Ordinal)
        .Select((slug, index) => (slug, id: FirstId + index))
        .ToDictionary(p => p.slug, p => p.id, StringComparer.Ordinal);

    public void Show(string slug) => _hidden.TryRemove(slug, out _);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!;
        if (uri.Host != "catalog.test" || uri.AbsolutePath != "/api/v1/products")
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        var slugs = System.Web.HttpUtility.ParseQueryString(uri.Query)["slugs"]?.Split(',') ?? [];
        var items = slugs
            .Where(slug => Ids.ContainsKey(slug) && !_hidden.ContainsKey(slug))
            .Select(slug => new { id = Ids[slug], slug, title = slug })
            .ToList();
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { items, totalCount = items.Count, page = 1, pageSize = 100 })
        });
    }
}

/// <summary>Consumes the ratings the service publishes, so tests can observe them through the harness.</summary>
public sealed class ReviewSummaryProbe : IConsumer<ReviewSummaryChanged>
{
    public Task Consume(ConsumeContext<ReviewSummaryChanged> context) => Task.CompletedTask;
}
