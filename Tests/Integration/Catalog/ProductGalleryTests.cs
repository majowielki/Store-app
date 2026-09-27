using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Catalog;

[Collection(PostgresTests.Name)]
public sealed class ProductGalleryTests : IClassFixture<CatalogApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CatalogApiFactory _factory;

    public ProductGalleryTests(CatalogApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    private static string[] Urls(JsonElement product)
        => product.GetProperty("images").EnumerateArray().Select(i => i.GetProperty("url").GetString()!).ToArray();

    [Fact]
    public async Task A_seeded_product_shows_its_gallery_and_the_points_on_its_main_picture()
    {
        using var client = _factory.CreateClient();
        var listed = await ReadJson(await client.GetAsync("/api/v1/products?slugs=boucle-modular-sofa"));
        var id = listed.GetProperty("items")[0].GetProperty("id").GetInt32();

        var product = await ReadJson(await client.GetAsync($"/api/v1/products/{id}"));

        Assert.EndsWith("/BoucleModularSofa-1.webp", product.GetProperty("image").GetString(), StringComparison.Ordinal);
        Assert.Collection(Urls(product),
            url => Assert.EndsWith("/BoucleModularSofa-2.webp", url, StringComparison.Ordinal),
            url => Assert.EndsWith("/BoucleModularSofa-3.webp", url, StringComparison.Ordinal));
        var points = product.GetProperty("hotspots").EnumerateArray().ToList();
        var cushions = points.Single(p => p.GetProperty("productSlug").GetString() == "linen-cushion-cover-set");
        Assert.InRange(cushions.GetProperty("x").GetDecimal(), 0m, 100m);
        // A point to a product the catalogue does not have yet is kept for when it arrives
        Assert.Contains(points, p => p.GetProperty("productSlug").GetString() == "round-oak-coffee-table");
        // The listing stays light: no gallery on the cards
        Assert.False(listed.GetProperty("items")[0].TryGetProperty("images", out _));
    }

    [Fact]
    public async Task A_product_added_without_a_gallery_has_its_main_picture_only_until_an_admin_adds_one()
    {
        using var trueAdmin = _factory.CreateClient().AsTrueAdmin();
        var created = await trueAdmin.PostAsJsonAsync("/api/v1/products", new
        {
            title = "Gallery test chair",
            description = "A chair created by the integration tests to exercise the gallery.",
            price = 199m,
            category = "chairs",
            company = "modenza",
            image = "https://example.test/chair-1.webp",
            colors = new[] { "Brown" }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await ReadJson(created)).GetProperty("id").GetInt32();
        Assert.Empty(Urls(await ReadJson(await trueAdmin.GetAsync($"/api/v1/products/{id}"))));

        var edited = await trueAdmin.PutAsJsonAsync($"/api/v1/products/{id}", new
        {
            images = new[]
            {
                new { url = "https://example.test/chair-2.webp", alt = "The seat up close" },
                new { url = "https://example.test/chair-3.webp", alt = "The chair on its own" }
            },
            hotspots = new[] { new { x = 20.5m, y = 70m, productSlug = "jute-rug-200x300" } }
        });
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);

        var product = await ReadJson(await trueAdmin.GetAsync($"/api/v1/products/{id}"));
        Assert.Equal(["https://example.test/chair-2.webp", "https://example.test/chair-3.webp"], Urls(product));
        Assert.Equal("The seat up close", product.GetProperty("images")[0].GetProperty("alt").GetString());
        Assert.Equal(20.5m, product.GetProperty("hotspots")[0].GetProperty("x").GetDecimal());

        // Absent fields keep the gallery; a new list replaces it in its order
        await trueAdmin.PutAsJsonAsync($"/api/v1/products/{id}", new { title = "Gallery test chair (renamed)" });
        Assert.Equal(2, Urls(await ReadJson(await trueAdmin.GetAsync($"/api/v1/products/{id}"))).Length);
        await trueAdmin.PutAsJsonAsync($"/api/v1/products/{id}", new
        {
            images = new[] { new { url = "https://example.test/chair-3.webp", alt = "The chair on its own" } },
            hotspots = Array.Empty<object>()
        });
        product = await ReadJson(await trueAdmin.GetAsync($"/api/v1/products/{id}"));
        Assert.Equal(["https://example.test/chair-3.webp"], Urls(product));
        Assert.Empty(product.GetProperty("hotspots").EnumerateArray());

        using var demoAdmin = _factory.CreateClient().AsDemoAdmin();
        var refused = await demoAdmin.PutAsJsonAsync($"/api/v1/products/{id}", new { images = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
    }

    [Fact]
    public async Task A_point_off_the_picture_or_without_a_slug_is_refused()
    {
        using var trueAdmin = _factory.CreateClient().AsTrueAdmin();
        var listed = await ReadJson(await trueAdmin.GetAsync("/api/v1/products?slugs=oak-bath-stool"));
        var id = listed.GetProperty("items")[0].GetProperty("id").GetInt32();

        var offThePicture = await trueAdmin.PutAsJsonAsync($"/api/v1/products/{id}", new
        {
            hotspots = new[] { new { x = 120m, y = 50m, productSlug = "waffle-cotton-towel-set" } }
        });
        var notASlug = await trueAdmin.PutAsJsonAsync($"/api/v1/products/{id}", new
        {
            hotspots = new[] { new { x = 40m, y = 50m, productSlug = "Waffle Cotton Towel Set" } }
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, offThePicture.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, notASlug.StatusCode);
    }
}
