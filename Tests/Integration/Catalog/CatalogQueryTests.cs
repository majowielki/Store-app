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
    [InlineData("group=furniture")]
    [InlineData("search=sofa")]
    [InlineData("category=sofas&company=modenza")]
    [InlineData("price=100-5000&order=high")]
    [InlineData("sale=true")]
    public async Task List_attribute_filters_execute_in_the_database(string query)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/products?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Colour_filter_returns_only_products_with_that_colour()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        await CreateProduct(new
        {
            title = $"Turquoise chair {tag}",
            description = "A chair that exists only to be found by the colour filter in a test.",
            price = 199.99m,
            category = "chairs",
            company = "luxora",
            image = "https://example.test/chair.jpg",
            colors = new[] { "Turquoise" },
            materials = new[] { "Rattan" }
        });
        using var client = _factory.CreateClient();

        var byColour = await GetJson(client, "/api/v1/products?colors=turquoise");
        var byShopFilter = await GetJson(client, "/api/v1/products?color=turquoise");
        var byMaterial = await GetJson(client, "/api/v1/products?materials=rattan");
        var byOther = await GetJson(client, "/api/v1/products?colors=turquoise&materials=steel");

        Assert.Contains(byColour.GetProperty("items").EnumerateArray(), p => p.GetProperty("title").GetString()!.Contains(tag, StringComparison.Ordinal));
        Assert.All(byColour.GetProperty("items").EnumerateArray(), p =>
            Assert.Contains("turquoise", p.GetProperty("colors").EnumerateArray().Select(c => c.GetString())));
        // The shop's filter form sends a single "color"
        Assert.Contains(byShopFilter.GetProperty("items").EnumerateArray(), p => p.GetProperty("title").GetString()!.Contains(tag, StringComparison.Ordinal));
        Assert.Contains(byMaterial.GetProperty("items").EnumerateArray(), p => p.GetProperty("title").GetString()!.Contains(tag, StringComparison.Ordinal));
        Assert.DoesNotContain(byOther.GetProperty("items").EnumerateArray(), p => p.GetProperty("title").GetString()!.Contains(tag, StringComparison.Ordinal));
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
            colors = new[] { "black" }
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
            colors = new[] { "black" }
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
            colors = new[] { "Black", "White" },
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
