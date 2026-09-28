using Store.Contracts.Catalog;
using Store.ProductService.Models;
using Store.ProductService.Services;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Catalog;

/// <summary>
/// Regression: colour/material filters used to throw (list attributes were CSV strings EF could
/// not query) and every listing loaded the whole table into memory. All of this only shows up
/// against a real PostgreSQL, which is why these tests run on Testcontainers.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class CatalogQueryTests : IClassFixture<CatalogApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CatalogApiFactory _factory;

    public CatalogQueryTests(CatalogApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> GetJson(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);
    }

    private async Task<int> CreateProduct(object product)
    {
        using var admin = _factory.CreateClient().AsTrueAdmin();
        var response = await admin.PostAsJsonAsync("/api/v1/products", product);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json).GetProperty("id").GetInt32();
    }

    [Theory]
    [InlineData("colors=black")]
    [InlineData("colors=Black")]
    [InlineData("materials=wood")]
    [InlineData("colors=black,white&materials=wood")]
    [InlineData("colors=natural-oak,white")]
    [InlineData("group=furniture")]
    [InlineData("search=sofa")]
    [InlineData("category=sofas&company=modenza")]
    [InlineData("price=100-5000&order=high")]
    [InlineData("sale=true")]
    [InlineData("newArrival=true")]
    public async Task List_attribute_filters_execute_in_the_database(string query)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/products?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Colour_filter_finds_a_finish_by_its_key_and_by_its_family()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var finish = FinishCatalogue.TealBeech;
        await CreateProduct(new
        {
            title = $"Teal chair {tag}",
            description = "A chair that exists only to be found by the colour filter in a test.",
            price = 199.99m,
            category = "chairs",
            company = "luxora",
            image = "https://example.test/chair.jpg",
            colors = new[] { finish.Key },
            materials = new[] { "Rattan" }
        });
        using var client = _factory.CreateClient();
        var family = ProductFilters.Key(finish.Family);

        var byFinish = await GetJson(client, $"/api/v1/products?colors={finish.Key}&pageSize=100");
        var byFamily = await GetJson(client, $"/api/v1/products?color={family}&pageSize=100");
        var byOtherFamily = await GetJson(client, $"/api/v1/products?color={ProductFilters.Key(Color.Brown)}&pageSize=100");
        var byMaterial = await GetJson(client, "/api/v1/products?materials=rattan&pageSize=100");
        var byOther = await GetJson(client, $"/api/v1/products?colors={finish.Key}&materials=steel");

        static bool IsTagged(JsonElement product, string tag) => product.GetProperty("title").GetString()!.Contains(tag, StringComparison.Ordinal);
        Assert.Contains(byFinish.GetProperty("items").EnumerateArray(), p => IsTagged(p, tag));
        Assert.All(byFinish.GetProperty("items").EnumerateArray(), p =>
            Assert.Contains(finish.Key, p.GetProperty("colors").EnumerateArray().Select(c => c.GetString())));
        // The shop's filter form sends a single "color", a family: every product with a finish of it
        Assert.Contains(byFamily.GetProperty("items").EnumerateArray(), p => IsTagged(p, tag));
        Assert.All(byFamily.GetProperty("items").EnumerateArray(), p =>
            Assert.Contains(p.GetProperty("colors").EnumerateArray(), c => FinishCatalogue.Find(c.GetString()!)?.Family == finish.Family));
        Assert.DoesNotContain(byOtherFamily.GetProperty("items").EnumerateArray(), p => IsTagged(p, tag));
        Assert.Contains(byMaterial.GetProperty("items").EnumerateArray(), p => IsTagged(p, tag));
        Assert.DoesNotContain(byOther.GetProperty("items").EnumerateArray(), p => IsTagged(p, tag));
    }

    // Two finishes of one family make a product count once under it
    [Fact]
    public async Task Colour_counts_count_a_product_once_under_each_family_of_its_finishes()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        await CreateProduct(new
        {
            title = $"Counted stool {tag}",
            description = "A stool that exists only to be counted by the colour filter in a test.",
            price = 99.99m,
            category = "chairs",
            company = "artifex",
            image = "https://example.test/stool.jpg",
            colors = new[] { FinishCatalogue.TealWalnut.Key, FinishCatalogue.TealBeech.Key, FinishCatalogue.NaturalOak.Key }
        });
        using var client = _factory.CreateClient();

        var meta = await GetJson(client, $"/api/v1/products/meta?slugs=counted-stool-{tag}");

        var colours = meta.GetProperty("counts").GetProperty("colors");
        Assert.Equal(1, colours.GetProperty(ProductFilters.Key(Color.Teal)).GetInt32());
        Assert.Equal(1, colours.GetProperty(ProductFilters.Key(Color.Brown)).GetInt32());
        Assert.Equal(2, colours.EnumerateObject().Count());
    }

    [Fact]
    public async Task Finishes_list_every_finish_with_its_family_and_swatch()
    {
        using var client = _factory.CreateClient();

        var finishes = (await GetJson(client, "/api/v1/products/finishes")).EnumerateArray().ToList();

        Assert.Equal(FinishCatalogue.All.Select(f => f.Key), finishes.Select(f => f.GetProperty("key").GetString()));
        var pair = finishes.Single(f => f.GetProperty("key").GetString() == FinishCatalogue.OakBlackSteel.Key);
        Assert.Equal(FinishCatalogue.OakBlackSteel.Name, pair.GetProperty("name").GetString());
        Assert.Equal("brown", pair.GetProperty("family").GetString());
        Assert.Equal(["naturalOak", "blackSteel"], pair.GetProperty("swatch").EnumerateArray().Select(p => p.GetProperty("texture").GetString()));
        // A fabric is a colour alone: no texture
        var fabric = finishes.Single(f => f.GetProperty("key").GetString() == FinishCatalogue.NavyLinen.Key);
        var part = Assert.Single(fabric.GetProperty("swatch").EnumerateArray());
        Assert.Matches("^#[0-9a-f]{6}$", part.GetProperty("color").GetString());
        Assert.False(part.TryGetProperty("texture", out _));
    }

    [Fact]
    public async Task Pagination_is_computed_by_the_database_not_from_the_page()
    {
        using var client = _factory.CreateClient();

        var page = await GetJson(client, "/api/v1/products?page=1");
        var total = page.GetProperty("totalCount").GetInt32();
        var pageSize = page.GetProperty("pageSize").GetInt32();
        var totalPages = page.GetProperty("totalPages").GetInt32();

        Assert.True(total > pageSize, "the seed data should span more than one page");
        Assert.Equal(pageSize, page.GetProperty("items").GetArrayLength());
        Assert.Equal((int)Math.Ceiling(total / (double)pageSize), totalPages);
        Assert.True(page.GetProperty("hasNextPage").GetBoolean());

        var lastPage = await GetJson(client, $"/api/v1/products?page={totalPages}");
        Assert.InRange(lastPage.GetProperty("items").GetArrayLength(), 1, pageSize);
        Assert.False(lastPage.GetProperty("hasNextPage").GetBoolean());
    }

    [Fact]
    public async Task Search_is_case_insensitive()
    {
        using var client = _factory.CreateClient();

        var lower = await GetJson(client, "/api/v1/products?search=sofa");
        var upper = await GetJson(client, "/api/v1/products?search=SOFA");

        Assert.True(lower.GetProperty("items").GetArrayLength() > 0);
        Assert.Equal(lower.GetProperty("totalCount").GetInt32(), upper.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task New_arrivals_filter_returns_only_new_arrivals()
    {
        using var client = _factory.CreateClient();

        var newArrivals = await GetJson(client, "/api/v1/products?newArrival=true&pageSize=100");
        var fromCheckbox = await GetJson(client, "/api/v1/products?newArrival=on&pageSize=100");
        var everything = await GetJson(client, "/api/v1/products?newArrival=false&pageSize=100");

        Assert.True(newArrivals.GetProperty("totalCount").GetInt32() > 0);
        Assert.All(newArrivals.GetProperty("items").EnumerateArray(), p => Assert.True(p.GetProperty("newArrival").GetBoolean()));
        Assert.Equal(newArrivals.GetProperty("totalCount").GetInt32(), fromCheckbox.GetProperty("totalCount").GetInt32());
        Assert.True(everything.GetProperty("totalCount").GetInt32() > newArrivals.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Products_get_a_unique_slug_and_can_be_listed_by_it()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var product = new
        {
            title = $"Rattan Pendant {tag}",
            description = "A pendant lamp that exists only so its slug can be looked at.",
            price = 89.99m,
            category = "decor",
            company = "artifex",
            image = "https://example.test/pendant.jpg",
            colors = new[] { FinishCatalogue.NaturalOak.Key }
        };
        var first = await CreateProduct(product);
        var second = await CreateProduct(product);
        using var client = _factory.CreateClient();

        var listed = await GetJson(client, $"/api/v1/products?slugs=rattan-pendant-{tag},rattan-pendant-{tag}-2,unknown-slug");

        var slugs = listed.GetProperty("items").EnumerateArray().ToDictionary(p => p.GetProperty("id").GetInt32(), p => p.GetProperty("slug").GetString());
        Assert.Equal(2, slugs.Count);
        Assert.Equal($"rattan-pendant-{tag}", slugs[first]);
        Assert.Equal($"rattan-pendant-{tag}-2", slugs[second]);

        // A wishlist lists its products by id; what is not a number is ignored
        var byId = await GetJson(client, $"/api/v1/products?ids={first},x,{second},0,999999");
        Assert.Equal([first, second], byId.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("id").GetInt32()).Order());
    }

    // Regression: the sale price was shown on the page but never charged
    [Fact]
    public async Task Effective_price_reflects_sale_price_and_discount()
    {
        var onSale = await CreateProduct(new
        {
            title = "Sale sofa",
            description = "A sofa with an explicit sale price used by the pricing test.",
            price = 1000m,
            salePrice = 800m,
            category = "sofas",
            company = "modenza",
            image = "https://example.test/sale.jpg",
            colors = new[] { FinishCatalogue.BlackSteelOak.Key }
        });
        var discounted = await CreateProduct(new
        {
            title = "Discounted table",
            description = "A table with a percentage discount used by the pricing test.",
            price = 200m,
            discountPercent = 25m,
            category = "tables",
            company = "modenza",
            image = "https://example.test/table.jpg",
            colors = new[] { FinishCatalogue.BlackSteelOak.Key }
        });
        using var client = _factory.CreateClient();

        var saleProduct = await GetJson(client, $"/api/v1/products/{onSale}");
        var discountedProduct = await GetJson(client, $"/api/v1/products/{discounted}");

        Assert.Equal(800m, saleProduct.GetProperty("effectivePrice").GetDecimal());
        Assert.Equal(150m, discountedProduct.GetProperty("effectivePrice").GetDecimal());
    }

    // Regression: the admin form sent enum names and strings the API could not deserialise
    [Fact]
    public async Task Create_accepts_the_payload_the_admin_form_sends()
    {
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var response = await admin.PostAsJsonAsync("/api/v1/products", new
        {
            title = "Form product",
            description = "Created with enum names, arrays and a boolean the way the admin form submits.",
            price = 149.5m,
            salePrice = (decimal?)null,
            category = "chairs",
            company = "Luxora",
            newArrival = true,
            image = "https://example.test/form.jpg",
            colors = new[] { FinishCatalogue.BlackSteelOak.Key, FinishCatalogue.PaintedWhite.Key },
            groups = new[] { "furniture" },
            materials = new[] { "wood", "steel" },
            widthCm = 50m
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);
        Assert.Equal("chairs", body.GetProperty("category").GetString());
        Assert.Equal("luxora", body.GetProperty("company").GetString());
        Assert.True(body.GetProperty("newArrival").GetBoolean());
        Assert.Equal(149.5m, body.GetProperty("effectivePrice").GetDecimal());
    }
}
