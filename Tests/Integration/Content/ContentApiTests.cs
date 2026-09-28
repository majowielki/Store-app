using Microsoft.Extensions.DependencyInjection;
using Store.BuildingBlocks.Shop;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace Store.Tests.Integration.Content;

[Collection(PostgresTests.Name)]
public sealed class ContentApiTests : IClassFixture<ContentApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ContentApiFactory _factory;

    public ContentApiTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private static object Collection(string slug, bool published = true, params string[] products) => new
    {
        slug,
        title = "Quiet Corners",
        summary = "Reading corners that ask for nothing but a lamp and a chair.",
        body = "## A chair, a lamp, a book\n\nThat is all a corner needs.",
        coverImage = "https://example.test/corners.webp",
        productSlugs = products,
        sortOrder = 9,
        isPublished = published
    };

    private static async Task<JsonElement> Body(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    private static string Unique(string name) => $"{name}-{Guid.NewGuid().ToString("N")[..8]}";

    [Theory]
    [InlineData("makers", "modenza")]
    [InlineData("collections", "warm-minimal")]
    [InlineData("articles", "caring-for-oak")]
    [InlineData("lookbooks", "living-room")]
    public async Task The_demo_content_is_readable_by_anyone(string kind, string slug)
    {
        using var client = _factory.CreateClient();

        var list = await client.GetAsync($"/api/v1/content/{kind}");
        var one = await client.GetAsync($"/api/v1/content/{kind}/{slug}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains((await Body(list)).EnumerateArray(), e => e.GetProperty("slug").GetString() == slug);
        Assert.Equal(HttpStatusCode.OK, one.StatusCode);
        Assert.Equal(slug, (await Body(one)).GetProperty("slug").GetString());
    }

    [Fact]
    public async Task A_lookbook_carries_its_points_on_the_picture()
    {
        using var client = _factory.CreateClient();

        var lookbook = await Body(await client.GetAsync("/api/v1/content/lookbooks/living-room"));

        var sofa = lookbook.GetProperty("hotspots").EnumerateArray().Single(h => h.GetProperty("productSlug").GetString() == "boucle-modular-sofa");
        Assert.InRange(sofa.GetProperty("x").GetDecimal(), 0m, 100m);
        Assert.InRange(sofa.GetProperty("y").GetDecimal(), 0m, 100m);
    }

    // Search engines find the editorial pages through it (the UI serves it as /sitemap-content.xml)
    [Fact]
    public async Task The_sitemap_lists_the_published_pages_at_their_addresses_in_the_shop()
    {
        using var admin = _factory.CreateClient().AsTrueAdmin();
        var hidden = Unique("hidden-corners");
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/v1/content/admin/collections", Collection(hidden, published: false))).StatusCode);
        using var visitor = _factory.CreateClient();

        var response = await visitor.GetAsync("/api/v1/content/sitemap.xml");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Sitemap.ContentType, response.Content.Headers.ContentType?.MediaType);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var pages = XDocument.Parse(await response.Content.ReadAsStringAsync()).Descendants(ns + "loc").Select(l => l.Value).ToList();
        var links = _factory.Services.GetRequiredService<ShopLinks>();
        Assert.Contains(links.Maker("modenza"), pages);
        Assert.Contains(links.Collection("warm-minimal"), pages);
        Assert.Contains(links.Article("caring-for-oak"), pages);
        Assert.Contains(links.Lookbook("living-room"), pages);
        Assert.DoesNotContain(links.Collection(hidden), pages);
    }

    [Fact]
    public async Task An_unknown_slug_is_404()
    {
        using var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/content/collections/no-such-collection")).StatusCode);
    }

    [Fact]
    public async Task The_true_administrator_creates_a_collection_the_shop_sees_once_it_is_published()
    {
        using var admin = _factory.CreateClient().AsTrueAdmin();
        using var visitor = _factory.CreateClient();
        var slug = Unique("quiet-corners");

        var created = await admin.PostAsJsonAsync("/api/v1/content/admin/collections", Collection(slug, published: false, "linen-lounge-armchair"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await Body(created)).GetProperty("id").GetInt32();

        // Unpublished: in the admin list, not in the shop
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/v1/content/collections/{slug}")).StatusCode);
        Assert.Contains((await Body(await admin.GetAsync("/api/v1/content/admin/collections"))).EnumerateArray(), c => c.GetProperty("id").GetInt32() == id);

        var published = await admin.PutAsJsonAsync($"/api/v1/content/admin/collections/{id}", Collection(slug, published: true, "linen-lounge-armchair", "tripod-floor-lamp"));
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);

        var shown = await Body(await visitor.GetAsync($"/api/v1/content/collections/{slug}"));
        Assert.Equal(["linen-lounge-armchair", "tripod-floor-lamp"], shown.GetProperty("productSlugs").EnumerateArray().Select(p => p.GetString()!).ToArray());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/content/admin/collections/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/v1/content/collections/{slug}")).StatusCode);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("user", HttpStatusCode.Forbidden)]
    [InlineData("demo-admin", HttpStatusCode.Forbidden)]
    public async Task Only_the_true_administrator_may_change_content(string who, HttpStatusCode expected)
    {
        using var client = who switch
        {
            "user" => _factory.CreateClient().AsUser(),
            "demo-admin" => _factory.CreateClient().AsDemoAdmin(),
            _ => _factory.CreateClient()
        };

        var response = await client.PostAsJsonAsync("/api/v1/content/admin/collections", Collection(Unique("not-allowed")));

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task The_demo_administrator_may_read_the_admin_lists()
    {
        using var demoAdmin = _factory.CreateClient().AsDemoAdmin();

        Assert.Equal(HttpStatusCode.OK, (await demoAdmin.GetAsync("/api/v1/content/admin/lookbooks")).StatusCode);
    }

    [Fact]
    public async Task A_slug_taken_by_another_entry_of_the_same_kind_is_a_conflict()
    {
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var response = await admin.PostAsJsonAsync("/api/v1/content/admin/collections", Collection("warm-minimal"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("Not A Slug")]
    [InlineData("double--dash")]
    public async Task A_malformed_slug_is_rejected(string slug)
    {
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var response = await admin.PostAsJsonAsync("/api/v1/content/admin/collections", Collection(slug));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_point_outside_the_picture_is_rejected()
    {
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var response = await admin.PostAsJsonAsync("/api/v1/content/admin/lookbooks", new
        {
            slug = Unique("off-the-edge"),
            title = "Off the edge",
            summary = "",
            image = "https://example.test/room.webp",
            hotspots = new[] { new { x = 120m, y = 50m, productSlug = "tripod-floor-lamp" } },
            sortOrder = 9,
            isPublished = false
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // Content is read on every page of the shop; an unchanged list costs a bodiless 304
    [Fact]
    public async Task An_unchanged_list_answers_304_and_a_change_gives_a_new_tag()
    {
        using var visitor = _factory.CreateClient();
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var first = await visitor.GetAsync("/api/v1/content/collections");
        var tag = first.Headers.ETag;
        Assert.NotNull(tag);

        var again = new HttpRequestMessage(HttpMethod.Get, "/api/v1/content/collections");
        again.Headers.IfNoneMatch.Add(tag!);
        var unchanged = await visitor.SendAsync(again);
        Assert.Equal(HttpStatusCode.NotModified, unchanged.StatusCode);

        var created = await admin.PostAsJsonAsync("/api/v1/content/admin/collections", Collection(Unique("new-tag")));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var afterChange = new HttpRequestMessage(HttpMethod.Get, "/api/v1/content/collections");
        afterChange.Headers.IfNoneMatch.Add(tag!);
        var changed = await visitor.SendAsync(afterChange);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.NotEqual(tag, changed.Headers.ETag);

        await admin.DeleteAsync($"/api/v1/content/admin/collections/{(await Body(created)).GetProperty("id").GetInt32()}");
    }
}
