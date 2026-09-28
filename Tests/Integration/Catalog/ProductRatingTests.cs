using Store.Contracts.Reviews.V1;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Catalog;

/// <summary>
/// The catalogue keeps a copy of each product's rating from the review service's events: the
/// listings show it and sort the best rated first, and a summary that arrives late is ignored.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class ProductRatingTests : IClassFixture<CatalogApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CatalogApiFactory _factory;

    public ProductRatingTests(CatalogApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(int Id, string Slug)> ProductAsync(string name)
    {
        using var admin = _factory.CreateClient().AsTrueAdmin();
        var response = await admin.PostAsJsonAsync("/api/v1/products", new
        {
            title = $"Rating test {name} {Guid.NewGuid():N}",
            description = "A product the rating tests review.",
            price = 100m,
            category = 1,
            company = 1,
            image = "https://example.test/rating.jpg",
            colors = new[] { "Black" },
            stockQuantity = 5
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return (product.GetProperty("id").GetInt32(), product.GetProperty("slug").GetString()!);
    }

    private async Task<JsonElement> ProductViewAsync(int id)
    {
        using var client = _factory.CreateClient();
        return JsonSerializer.Deserialize<JsonElement>(await client.GetStringAsync($"/api/v1/products/{id}"), Json);
    }

    private async Task PublishAndWaitAsync(ReviewSummaryChanged summary)
    {
        await _factory.Bus.Bus.Publish(summary);
        Assert.True(await Eventually.BecomesTrueAsync(() => _factory.Bus.Consumed
            .Select<ReviewSummaryChanged>(e => e.Context.Message == summary).Any()));
    }

    [Fact]
    public async Task A_summary_sets_the_rating_the_product_shows()
    {
        var (id, _) = await ProductAsync("shown");

        await PublishAndWaitAsync(new ReviewSummaryChanged(id, 4.25m, 8, DateTime.UtcNow));

        var product = await ProductViewAsync(id);
        Assert.Equal(4.25m, product.GetProperty("ratingAverage").GetDecimal());
        Assert.Equal(8, product.GetProperty("ratingCount").GetInt32());
    }

    [Fact]
    public async Task A_summary_older_than_the_one_held_is_ignored()
    {
        var (id, _) = await ProductAsync("late");
        var now = DateTime.UtcNow;

        await PublishAndWaitAsync(new ReviewSummaryChanged(id, 3.5m, 2, now));
        await PublishAndWaitAsync(new ReviewSummaryChanged(id, 5m, 1, now.AddSeconds(-30)));

        var product = await ProductViewAsync(id);
        Assert.Equal(3.5m, product.GetProperty("ratingAverage").GetDecimal());
        Assert.Equal(2, product.GetProperty("ratingCount").GetInt32());
    }

    [Fact]
    public async Task The_listing_sorts_the_best_rated_first_and_more_reviews_break_a_tie()
    {
        var (good, goodSlug) = await ProductAsync("good");
        var (best, bestSlug) = await ProductAsync("best");
        var (popular, popularSlug) = await ProductAsync("popular");
        var (unrated, unratedSlug) = await ProductAsync("unrated");
        var now = DateTime.UtcNow;
        await PublishAndWaitAsync(new ReviewSummaryChanged(good, 4.1m, 12, now));
        await PublishAndWaitAsync(new ReviewSummaryChanged(best, 4.9m, 3, now));
        await PublishAndWaitAsync(new ReviewSummaryChanged(popular, 4.1m, 40, now));

        using var client = _factory.CreateClient();
        var slugs = string.Join(',', goodSlug, bestSlug, popularSlug, unratedSlug);
        var page = JsonSerializer.Deserialize<JsonElement>(await client.GetStringAsync($"/api/v1/products?slugs={slugs}&order=rating"), Json);

        var ids = page.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("id").GetInt32()).ToList();
        Assert.Equal(new[] { best, popular, good, unrated }, ids);
    }
}
