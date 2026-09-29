using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Cart;

/// <summary>
/// Requests that change one cart at the same moment - a double click on "Add to bag", two tabs, the
/// guest cart merged while a piece goes in - take turns: none fails and no quantity is lost.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class CartConcurrencyTests : IClassFixture<CartApiFactory>
{
    private const int Requests = 6;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CartApiFactory _factory;

    public CartConcurrencyTests(CartApiFactory factory)
    {
        _factory = factory;
    }

    // Regression: the second of two adds of the same line inserted it again and failed with 500
    [Fact]
    public async Task Adds_of_the_same_product_at_the_same_moment_all_land_on_one_line()
    {
        _factory.Catalog.Add(id: 9301, price: 90m, title: "Popular stool");
        using var client = _factory.CreateClient().AsUser("cart-concurrent-user");

        // The user has no cart yet, so the first adds race to create it too
        var responses = await Task.WhenAll(Enumerable.Range(0, Requests)
            .Select(_ => client.PostAsJsonAsync("/api/v1/cart/items", new { productId = 9301, quantity = 1, color = "black" })));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        var cart = JsonSerializer.Deserialize<JsonElement>(await (await client.GetAsync("/api/v1/cart")).Content.ReadAsStringAsync(), Json);
        var line = Assert.Single(cart.GetProperty("items").EnumerateArray());
        Assert.Equal(Requests, line.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task A_guest_cart_merged_while_a_piece_goes_in_keeps_both()
    {
        _factory.Catalog.Add(id: 9302, price: 40m, title: "Linen cushion");
        using var client = _factory.CreateClient().AsUser("cart-merge-race-user");

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/v1/cart/sync", new { items = new[] { new { productId = 9302, quantity = 2, color = "white" } } }),
            client.PostAsJsonAsync("/api/v1/cart/items", new { productId = 9302, quantity = 1, color = "white" }));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        var cart = JsonSerializer.Deserialize<JsonElement>(await (await client.GetAsync("/api/v1/cart")).Content.ReadAsStringAsync(), Json);
        Assert.Equal(3, Assert.Single(cart.GetProperty("items").EnumerateArray()).GetProperty("quantity").GetInt32());
    }
}
