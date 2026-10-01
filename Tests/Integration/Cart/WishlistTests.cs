using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Cart;

[Collection(PostgresTests.Name)]
public sealed class WishlistTests : IClassFixture<CartApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CartApiFactory _factory;

    public WishlistTests(CartApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<List<int>> ProductIds(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);
        return body.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("productId").GetInt32()).ToList();
    }

    [Fact]
    public async Task A_product_goes_on_the_list_once_and_comes_off_again()
    {
        _factory.Catalog.Add(301, 100m);
        _factory.Catalog.Add(302, 200m);
        using var client = _factory.CreateClient().AsUser("wishlist-basic");

        await client.PostAsJsonAsync("/api/v1/wishlist/items", new { productId = 301 });
        await client.PostAsJsonAsync("/api/v1/wishlist/items", new { productId = 302 });
        var twice = await ProductIds(await client.PostAsJsonAsync("/api/v1/wishlist/items", new { productId = 301 }));

        // The last one added first; adding again changes nothing
        Assert.Equal([302, 301], twice);
        Assert.Equal([302], await ProductIds(await client.DeleteAsync("/api/v1/wishlist/items/301")));
        Assert.Equal([302], await ProductIds(await client.DeleteAsync("/api/v1/wishlist/items/301")));
    }

    [Fact]
    public async Task A_product_that_is_not_for_sale_cannot_be_added()
    {
        _factory.Catalog.Add(303, 100m, isActive: false);
        using var client = _factory.CreateClient().AsUser("wishlist-inactive");

        var inactive = await client.PostAsJsonAsync("/api/v1/wishlist/items", new { productId = 303 });
        var unknown = await client.PostAsJsonAsync("/api/v1/wishlist/items", new { productId = 999_303 });
        var invalid = await client.PostAsJsonAsync("/api/v1/wishlist/items", new { productId = 0 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, inactive.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        Assert.Empty(await ProductIds(await client.GetAsync("/api/v1/wishlist")));
    }

    // The visitor's list joins the account's at sign-in: nothing twice, nothing that left the shop
    [Fact]
    public async Task The_guest_list_is_merged_into_the_account_at_sign_in()
    {
        _factory.Catalog.Add(311, 100m);
        _factory.Catalog.Add(312, 100m);
        _factory.Catalog.Add(313, 100m, isActive: false);
        using var client = _factory.CreateClient().AsUser("wishlist-merge");
        await client.PostAsJsonAsync("/api/v1/wishlist/items", new { productId = 311 });

        var merged = await ProductIds(await client.PostAsJsonAsync("/api/v1/wishlist/sync", new { productIds = new[] { 312, 311, 313, 999_311 } }));

        Assert.Equal(2, merged.Count);
        Assert.Equal([311, 312], merged.Order());
        // A second sign-in with the same list adds nothing
        Assert.Equal(merged, await ProductIds(await client.PostAsJsonAsync("/api/v1/wishlist/sync", new { productIds = new[] { 312, 311 } })));
    }

    [Fact]
    public async Task Each_customer_sees_their_own_list_and_a_visitor_none()
    {
        _factory.Catalog.Add(321, 100m);
        using var anna = _factory.CreateClient().AsUser("wishlist-anna");
        using var ben = _factory.CreateClient().AsUser("wishlist-ben");
        using var anonymous = _factory.CreateClient();

        await anna.PostAsJsonAsync("/api/v1/wishlist/items", new { productId = 321 });

        Assert.Equal([321], await ProductIds(await anna.GetAsync("/api/v1/wishlist")));
        Assert.Empty(await ProductIds(await ben.GetAsync("/api/v1/wishlist")));
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/wishlist")).StatusCode);
    }

    [Fact]
    public async Task Parallel_additions_cannot_exceed_the_owner_limit()
    {
        var limit = Store.CartService.Models.WishlistItem.MaxItems;
        var products = Enumerable.Range(80000, limit + 1).ToArray();
        foreach (var id in products) _factory.Catalog.Add(id, 10m);
        using var client = _factory.CreateClient().AsUser("wishlist-parallel-limit");
        await client.PostAsJsonAsync("/api/v1/wishlist/sync", new { productIds = products.Take(limit - 1) });
        var responses = await Task.WhenAll(products.Skip(limit - 1).Select(id => client.PostAsJsonAsync("/api/v1/wishlist/items", new { productId = id })));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.UnprocessableEntity);
        Assert.Equal(limit, (await ProductIds(await client.GetAsync("/api/v1/wishlist"))).Count);
    }
}
