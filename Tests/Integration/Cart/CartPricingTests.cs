using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Cart;

[Collection(PostgresTests.Name)]
public sealed class CartPricingTests : IClassFixture<CartApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CartApiFactory _factory;

    public CartPricingTests(CartApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    /// <summary>POST /api/v1/cart/items answers with the whole cart; pick the line for the product.</summary>
    private static async Task<JsonElement> LineFor(HttpResponseMessage response, int productId)
        => (await ReadJson(response)).GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("productId").GetInt32() == productId);

    // Regression: the cart copied the list price even when the page showed a sale price
    [Fact]
    public async Task Cart_line_uses_the_sale_price_the_customer_saw()
    {
        _factory.Catalog.Add(id: 9001, price: 899.99m, salePrice: 799.99m, title: "Sale sofa");
        using var client = _factory.CreateClient().AsUser("cart-sale-user");

        var added = await client.PostAsJsonAsync("/api/v1/cart/items", new { productId = 9001, quantity = 2, color = "black" });

        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        Assert.Equal(799.99m, (await LineFor(added, 9001)).GetProperty("price").GetDecimal());

        var cart = await ReadJson(await client.GetAsync("/api/v1/cart"));
        Assert.Equal(1599.98m, cart.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Cart_line_applies_a_percentage_discount()
    {
        _factory.Catalog.Add(id: 9002, price: 200m, discountPercent: 25m, title: "Discounted table");
        using var client = _factory.CreateClient().AsUser("cart-discount-user");

        var added = await client.PostAsJsonAsync("/api/v1/cart/items", new { productId = 9002, quantity = 1, color = "black" });

        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        Assert.Equal(150m, (await LineFor(added, 9002)).GetProperty("price").GetDecimal());
    }

    [Fact]
    public async Task Cart_line_uses_the_list_price_when_there_is_no_promotion()
    {
        _factory.Catalog.Add(id: 9003, price: 120m, title: "Plain chair");
        using var client = _factory.CreateClient().AsUser("cart-plain-user");

        var added = await client.PostAsJsonAsync("/api/v1/cart/items", new { productId = 9003, quantity = 1, color = "black" });

        Assert.Equal(120m, (await LineFor(added, 9003)).GetProperty("price").GetDecimal());
    }

    [Fact]
    public async Task Price_change_in_the_catalogue_is_picked_up_on_the_next_add()
    {
        _factory.Catalog.Add(id: 9004, price: 300m, title: "Repriced lamp");
        using var first = _factory.CreateClient().AsUser("cart-reprice-user-1");
        await first.PostAsJsonAsync("/api/v1/cart/items", new { productId = 9004, quantity = 1, color = "black" });

        _factory.Catalog.Add(id: 9004, price: 300m, salePrice: 240m, title: "Repriced lamp");
        using var second = _factory.CreateClient().AsUser("cart-reprice-user-2");
        var added = await second.PostAsJsonAsync("/api/v1/cart/items", new { productId = 9004, quantity = 1, color = "black" });

        Assert.Equal(240m, (await LineFor(added, 9004)).GetProperty("price").GetDecimal());
    }

    // Regression: GET /api/v1/cart/total summed an unmapped computed property and failed
    [Fact]
    public async Task Cart_total_endpoint_sums_lines_in_the_database()
    {
        _factory.Catalog.Add(id: 9005, price: 10m, title: "Ten");
        _factory.Catalog.Add(id: 9006, price: 5.5m, title: "Five fifty");
        using var client = _factory.CreateClient().AsUser("cart-total-user");
        await client.PostAsJsonAsync("/api/v1/cart/items", new { productId = 9005, quantity = 3, color = "black" });
        await client.PostAsJsonAsync("/api/v1/cart/items", new { productId = 9006, quantity = 2, color = "black" });

        var response = await client.GetAsync("/api/v1/cart/total");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(41m, (await ReadJson(response)).GetDecimal());
    }

    [Fact]
    public async Task Unknown_product_cannot_be_added()
    {
        using var client = _factory.CreateClient().AsUser("cart-unknown-user");

        var added = await client.PostAsJsonAsync("/api/v1/cart/items", new { productId = 424242, quantity = 1, color = "black" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, added.StatusCode);
    }
}
