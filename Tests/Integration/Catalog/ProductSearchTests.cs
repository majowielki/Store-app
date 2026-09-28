using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Catalog;

/// <summary>
/// Search over the demo catalogue: whole and partial words, accents, the title before the
/// description, a mistyped search corrected to the catalogue's words, the suggestions of the search
/// box and the filter counts of a query. Full-text search only exists in PostgreSQL, hence here.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class ProductSearchTests : IClassFixture<CatalogApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CatalogApiFactory _factory;

    public ProductSearchTests(CatalogApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<JsonElement> GetJson(string url)
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);
    }

    private async Task<List<string>> TitlesAsync(string query)
        => (await GetJson($"/api/v1/products?{query}&pageSize=100")).GetProperty("items").EnumerateArray()
            .Select(p => p.GetProperty("title").GetString()!)
            .ToList();

    [Fact]
    public async Task A_search_puts_the_products_named_after_it_first()
    {
        var titles = await TitlesAsync("search=sofa");

        Assert.Contains("Bouclé Modular Sofa", titles);
        Assert.Contains("Linen Slipcover Sofa", titles);
        // Every product with the word in its title comes before those that only mention it
        var named = titles.TakeWhile(t => t.Contains("Sofa", StringComparison.OrdinalIgnoreCase)).Count();
        Assert.Equal(titles.Count(t => t.Contains("Sofa", StringComparison.OrdinalIgnoreCase)), named);
    }

    [Theory]
    [InlineData("search=oak%20tab", "Modern Oak Dining Table")]
    [InlineData("search=boucle", "Bouclé Modular Sofa")]
    [InlineData("search=BOUCLÉ", "Bouclé Modular Sofa")]
    [InlineData("search=sofas", "Linen Slipcover Sofa")]
    public async Task Words_match_as_word_starts_without_accents_or_plural(string query, string expected)
    {
        Assert.Contains(expected, await TitlesAsync(query));
    }

    [Fact]
    public async Task A_mistyped_search_is_corrected_to_the_catalogue_words()
    {
        // The same products, in the same order, as the search for the right word
        var titles = await TitlesAsync("search=sfoa");
        Assert.NotEmpty(titles);
        Assert.Equal(await TitlesAsync("search=sofa"), titles);

        var meta = await GetJson("/api/v1/products/meta?search=sfoa");
        Assert.Equal("sofa", meta.GetProperty("searchCorrection").GetString());
        Assert.Equal(titles.Count, meta.GetProperty("counts").GetProperty("total").GetInt32());

        var suggestions = await GetJson("/api/v1/products/suggest?q=walnutt%20sidebord");
        Assert.Equal("walnut sideboard", suggestions.GetProperty("correction").GetString());
        Assert.Contains(suggestions.GetProperty("products").EnumerateArray(), p => p.GetProperty("title").GetString() == "Walnut Sideboard");
    }

    [Fact]
    public async Task A_search_found_as_typed_is_not_corrected_and_nonsense_finds_nothing()
    {
        var found = await GetJson("/api/v1/products/suggest?q=lamp");
        Assert.False(found.TryGetProperty("correction", out var correction) && correction.ValueKind == JsonValueKind.String);
        Assert.True(found.GetProperty("totalCount").GetInt32() > 0);
        Assert.InRange(found.GetProperty("products").GetArrayLength(), 1, 6);

        Assert.Empty(await TitlesAsync("search=qzxwvtq"));
        var nothing = await GetJson("/api/v1/products/suggest?q=qzxwvtq");
        Assert.Equal(0, nothing.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task The_counts_follow_the_query_but_leave_out_each_menus_own_filter()
    {
        var sofas = (await GetJson("/api/v1/products?search=sofa&pageSize=100")).GetProperty("totalCount").GetInt32();
        var meta = await GetJson("/api/v1/products/meta?search=sofa");
        var counts = meta.GetProperty("counts");

        Assert.Equal(sofas, counts.GetProperty("total").GetInt32());
        Assert.Equal(sofas, counts.GetProperty("categories").EnumerateObject().Sum(c => c.Value.GetInt32()));
        Assert.True(counts.GetProperty("categories").GetProperty("sofas").GetInt32() > 0);

        // Picking a company keeps the other companies' counts; the categories count within the company
        var company = counts.GetProperty("companies").EnumerateObject().First();
        var withCompany = (await GetJson($"/api/v1/products/meta?search=sofa&company={company.Name}")).GetProperty("counts");
        Assert.Equal(company.Value.GetInt32(), withCompany.GetProperty("total").GetInt32());
        Assert.Equal(counts.GetProperty("companies").GetRawText(), withCompany.GetProperty("companies").GetRawText());
        Assert.Equal(company.Value.GetInt32(), withCompany.GetProperty("categories").EnumerateObject().Sum(c => c.Value.GetInt32()));

        // Without a query the counts cover the whole active catalogue
        var all = (await GetJson("/api/v1/products/meta")).GetProperty("counts");
        Assert.Equal((await GetJson("/api/v1/products?pageSize=1")).GetProperty("totalCount").GetInt32(), all.GetProperty("total").GetInt32());
        Assert.True(all.GetProperty("colors").EnumerateObject().Any());
        Assert.True(all.GetProperty("groups").GetProperty("furniture").GetInt32() > 0);
    }
}
