using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Orders;

[Collection(PostgresTests.Name)]
public sealed class DiscountCodeTests : IClassFixture<OrderApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly OrderApiFactory _factory;

    public DiscountCodeTests(OrderApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    private static async Task<string> DetailOf(HttpResponseMessage response)
        => (await ReadJson(response)).GetProperty("detail").GetString()!;

    private async Task<int> CreateCodeAsync(object code)
    {
        using var admin = _factory.CreateClient().AsTrueAdmin();
        var response = await admin.PostAsJsonAsync("/api/v1/admin/discount-codes", code);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJson(response)).GetProperty("id").GetInt32();
    }

    /// <summary>A customer with one order behind them, so the first-order discount is out of the way.</summary>
    private async Task<HttpClient> ReturningCustomerAsync(string user, int productId, decimal price)
    {
        _factory.Upstreams.AddProduct(productId, effectivePrice: price);
        _factory.Upstreams.SetCart(user, (productId, 1, price));
        var client = _factory.CreateClient().AsUser(user);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/orders/from-cart", new { customerName = "Code Buyer" })).StatusCode);
        _factory.Upstreams.SetCart(user, (productId, 1, price));
        return client;
    }

    [Fact]
    public async Task The_cart_can_check_a_code_before_signing_in_and_learns_what_it_takes_off()
    {
        using var anonymous = _factory.CreateClient();

        var percent = await ReadJson(await anonymous.GetAsync("/api/v1/orders/discount-codes/linen15?subtotal=200"));
        var amount = await ReadJson(await anonymous.GetAsync("/api/v1/orders/discount-codes/OAK50?subtotal=450"));

        // The demo codes are seeded with the migrations; the letters are matched in any case
        Assert.Equal("LINEN15", percent.GetProperty("code").GetString());
        Assert.Equal(30m, percent.GetProperty("discountAmount").GetDecimal());
        Assert.Equal("Amount", amount.GetProperty("kind").GetString());
        Assert.Equal(50m, amount.GetProperty("discountAmount").GetDecimal());
    }

    [Theory]
    [InlineData("NOPE", 500, "There is no such code.")]
    [InlineData("SUMMER25", 500, "This code expired on Sep 1, 2026.")]
    [InlineData("OAK50", 399.99, "This code needs an order of at least $400.00.")]
    public async Task A_code_that_cannot_be_used_is_refused_with_the_reason(string code, decimal subtotal, string reason)
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync($"/api/v1/orders/discount-codes/{code}?subtotal={subtotal}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(reason, await DetailOf(response));
    }

    [Fact]
    public async Task The_order_takes_the_code_off_counts_the_use_and_keeps_the_code()
    {
        var id = await CreateCodeAsync(new { code = "IT-TWENTY", kind = "Amount", value = 20m });
        using var client = await ReturningCustomerAsync("code-applied", 201, 120m);

        var response = await client.PostAsJsonAsync("/api/v1/orders/from-cart", new { customerName = "Code Buyer", discountCode = "it-twenty" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await ReadJson(response);
        Assert.Equal(20m, order.GetProperty("discountAmount").GetDecimal());
        Assert.Equal("code", order.GetProperty("discountReason").GetString());
        Assert.Equal("IT-TWENTY", order.GetProperty("discountCode").GetString());
        Assert.Equal(110m, order.GetProperty("total").GetDecimal()); // 120 - 20 + 10 delivery
        using var admin = _factory.CreateClient().AsTrueAdmin();
        Assert.Equal(1, (await ReadJson(await admin.GetAsync($"/api/v1/admin/discount-codes/{id}"))).GetProperty("timesUsed").GetInt32());
    }

    [Fact]
    public async Task On_a_first_order_the_larger_discount_wins_and_a_smaller_code_stays_unused()
    {
        var id = await CreateCodeAsync(new { code = "IT-SMALL", kind = "Percent", value = 5m });
        _factory.Upstreams.AddProduct(202, effectivePrice: 300m);
        _factory.Upstreams.SetCart("code-first-order", (202, 1, 300m));
        using var client = _factory.CreateClient().AsUser("code-first-order");

        var order = await ReadJson(await client.PostAsJsonAsync("/api/v1/orders/from-cart", new { customerName = "Code Buyer", discountCode = "IT-SMALL" }));

        Assert.Equal(60m, order.GetProperty("discountAmount").GetDecimal());
        Assert.Equal("first-order", order.GetProperty("discountReason").GetString());
        Assert.False(order.TryGetProperty("discountCode", out var code) && code.ValueKind != JsonValueKind.Null);
        using var admin = _factory.CreateClient().AsTrueAdmin();
        Assert.Equal(0, (await ReadJson(await admin.GetAsync($"/api/v1/admin/discount-codes/{id}"))).GetProperty("timesUsed").GetInt32());
    }

    // The limit is checked under a lock on the code: five checkouts at once, two uses left
    [Fact]
    public async Task A_used_up_code_refuses_the_order_even_when_checkouts_race_for_it()
    {
        await CreateCodeAsync(new { code = "IT-TWO-LEFT", kind = "Amount", value = 10m, usageLimit = 2 });
        var clients = new List<HttpClient>();
        for (var i = 0; i < 5; i++)
        {
            clients.Add(await ReturningCustomerAsync($"code-race-{i}", 210 + i, 100m));
        }

        var responses = await Task.WhenAll(clients.Select(client =>
            client.PostAsJsonAsync("/api/v1/orders/from-cart", new { customerName = "Code Buyer", discountCode = "IT-TWO-LEFT" })));

        Assert.Equal(2, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        var refused = responses.Where(r => r.StatusCode != HttpStatusCode.Created).ToList();
        Assert.Equal(3, refused.Count);
        Assert.All(refused, r => Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode));
        Assert.Equal("This code has been used up.", await DetailOf(refused[0]));
        clients.ForEach(client => client.Dispose());
    }

    [Fact]
    public async Task The_demo_administrator_may_read_the_codes_but_not_change_them()
    {
        using var demo = _factory.CreateClient().AsDemoAdmin();

        var list = await demo.GetAsync("/api/v1/admin/discount-codes");
        var create = await demo.PostAsJsonAsync("/api/v1/admin/discount-codes", new { code = "IT-DEMO", kind = "Percent", value = 5m });

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains((await ReadJson(list)).EnumerateArray(), code => code.GetProperty("code").GetString() == "WELCOME10");
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task A_duplicate_is_a_conflict_and_a_used_code_cannot_be_deleted_only_switched_off()
    {
        var id = await CreateCodeAsync(new { code = "IT-USED", kind = "Amount", value = 5m });
        using var admin = _factory.CreateClient().AsTrueAdmin();
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/v1/admin/discount-codes", new { code = "it-used", kind = "Amount", value = 5m })).StatusCode);

        using var client = await ReturningCustomerAsync("code-used", 220, 50m);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/orders/from-cart", new { customerName = "Code Buyer", discountCode = "IT-USED" })).StatusCode);

        var delete = await admin.DeleteAsync($"/api/v1/admin/discount-codes/{id}");
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);

        var switchedOff = await admin.PutAsJsonAsync($"/api/v1/admin/discount-codes/{id}", new { code = "IT-USED", kind = "Amount", value = 5m, isActive = false });
        Assert.Equal(HttpStatusCode.OK, switchedOff.StatusCode);
        Assert.Equal(1, (await ReadJson(switchedOff)).GetProperty("timesUsed").GetInt32());
        using var anonymous = _factory.CreateClient();
        Assert.Equal("There is no such code.", await DetailOf(await anonymous.GetAsync("/api/v1/orders/discount-codes/IT-USED?subtotal=50")));

        var unused = await CreateCodeAsync(new { code = "IT-UNUSED", kind = "Amount", value = 5m });
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/discount-codes/{unused}")).StatusCode);
    }
}
