using Store.Contracts.Audit.V1;
using Store.Contracts.Authorization;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Catalog;

[Collection(PostgresTests.Name)]
public sealed class ProductEndpointsTests : IClassFixture<CatalogApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CatalogApiFactory _factory;

    public ProductEndpointsTests(CatalogApiFactory factory)
    {
        _factory = factory;
    }

    private static object ValidProduct(string title = "Integration test sofa") => new
    {
        title,
        description = "A sofa created by the integration tests to prove the write path works.",
        price = 1299.99m,
        category = 1,
        company = 1,
        image = "https://example.test/sofa.jpg",
        colors = new[] { "Black" }
    };

    [Fact]
    public async Task Catalogue_is_public_and_seeded()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);
        Assert.True(body.GetProperty("data").GetArrayLength() > 0, "the seeder should have populated the catalogue");
    }

    // Regression: the create/update/delete endpoints used a policy the service never registered,
    // so even true-admin got an error. Now: true-admin writes, demo-admin is read-only.
    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData(Roles.User, HttpStatusCode.Forbidden)]
    [InlineData(Roles.DemoAdmin, HttpStatusCode.Forbidden)]
    [InlineData(Roles.TrueAdmin, HttpStatusCode.Created)]
    public async Task Creating_a_product_requires_the_true_admin_role(string who, HttpStatusCode expected)
    {
        using var client = _factory.CreateClient().As(who);

        var response = await client.PostAsJsonAsync("/api/products", ValidProduct($"Created by {who}"));

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData(Roles.User, HttpStatusCode.Forbidden)]
    [InlineData(Roles.DemoAdmin, HttpStatusCode.OK)]
    [InlineData(Roles.TrueAdmin, HttpStatusCode.OK)]
    public async Task Admin_listing_requires_an_admin_role(string who, HttpStatusCode expected)
    {
        using var client = _factory.CreateClient().As(who);

        var response = await client.GetAsync("/api/products/admin");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Demo_admin_cannot_delete_products()
    {
        using var trueAdmin = _factory.CreateClient().AsTrueAdmin();
        var created = await trueAdmin.PostAsJsonAsync("/api/products", ValidProduct("To be deleted"));
        var id = JsonSerializer.Deserialize<JsonElement>(await created.Content.ReadAsStringAsync(), Json).GetProperty("id").GetInt32();

        using var demoAdmin = _factory.CreateClient().AsDemoAdmin();
        var forbidden = await demoAdmin.DeleteAsync($"/api/products/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var deleted = await trueAdmin.DeleteAsync($"/api/products/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Invalid_product_is_rejected_with_validation_details()
    {
        using var client = _factory.CreateClient().AsTrueAdmin();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            title = "",
            description = "too short",
            price = -1,
            image = "not a url",
            colors = Array.Empty<string>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);
        var errors = body.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Title", out _));
        Assert.True(errors.TryGetProperty("Price", out _));
        Assert.True(errors.TryGetProperty("Colors", out _));
    }

    // Regression: the audit used to be two synchronous HTTP calls per request; now the write
    // publishes one business event through the outbox, signed by the acting administrator
    [Fact]
    public async Task Writes_are_audited_as_events()
    {
        using var client = _factory.CreateClient().AsTrueAdmin();

        var created = await client.PostAsJsonAsync("/api/products", ValidProduct("Audited product"));
        var id = JsonSerializer.Deserialize<JsonElement>(await created.Content.ReadAsStringAsync(), Json).GetProperty("id").GetInt32();

        Assert.True(await Eventually.BecomesTrueAsync(() => _factory.Bus.Consumed
            .Select<AuditEvent>(e => e.Context.Message.Action == "PRODUCT_CREATED" && e.Context.Message.EntityId == id.ToString()).Any()));
        var audit = _factory.Bus.Consumed.Select<AuditEvent>(e => e.Context.Message.EntityId == id.ToString()).Single().Context.Message;
        Assert.Equal("catalog", audit.ServiceName);
        Assert.Equal("true-admin-1", audit.UserId);
        Assert.Contains("Audited product", audit.NewValues);
    }
}

[Collection(PostgresTests.Name)]
public sealed class ProductLifecycleTests : IClassFixture<CatalogApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CatalogApiFactory _factory;

    public ProductLifecycleTests(CatalogApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    private static async Task<int> CreateAsync(HttpClient trueAdmin, string title, decimal price, decimal? salePrice = null)
    {
        var created = await trueAdmin.PostAsJsonAsync("/api/products", new
        {
            title,
            description = "A product created by the integration tests to exercise the lifecycle.",
            price,
            salePrice,
            category = "sofas",
            company = "modenza",
            image = "https://example.test/sofa.jpg",
            colors = new[] { "Black" }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await ReadJson(created)).GetProperty("id").GetInt32();
    }

    // Deleting keeps the row: orders reference it and the admin panel can bring it back
    [Fact]
    public async Task Delete_hides_the_product_from_the_public_catalogue_but_admins_can_restore_it()
    {
        using var trueAdmin = _factory.CreateClient().AsTrueAdmin();
        using var anyone = _factory.CreateClient();
        var id = await CreateAsync(trueAdmin, "Soft deleted sofa", 500m);

        Assert.Equal(HttpStatusCode.NoContent, (await trueAdmin.DeleteAsync($"/api/products/{id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await anyone.GetAsync($"/api/products/{id}")).StatusCode);
        var adminList = await ReadJson(await trueAdmin.GetAsync("/api/products/admin?search=Soft%20deleted%20sofa"));
        var row = adminList.GetProperty("data").EnumerateArray().Single(p => p.GetProperty("id").GetInt32() == id);
        Assert.False(row.GetProperty("attributes").GetProperty("isActive").GetBoolean());

        var restored = await trueAdmin.PutAsJsonAsync($"/api/products/{id}", new { isActive = true });
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anyone.GetAsync($"/api/products/{id}")).StatusCode);
    }

    // Regression: a promotion could be changed but never removed, because null meant "not sent"
    [Fact]
    public async Task Sending_null_clears_the_sale_price_and_absent_fields_keep_their_value()
    {
        using var trueAdmin = _factory.CreateClient().AsTrueAdmin();
        var id = await CreateAsync(trueAdmin, "Promoted lamp", 100m, salePrice: 80m);

        var untouched = await ReadJson(await trueAdmin.PutAsJsonAsync($"/api/products/{id}", new { title = "Promoted lamp (renamed)" }));
        Assert.Equal(80m, untouched.GetProperty("salePrice").GetDecimal());
        Assert.Equal(80m, untouched.GetProperty("effectivePrice").GetDecimal());

        var cleared = await ReadJson(await trueAdmin.PutAsJsonAsync($"/api/products/{id}", new { salePrice = (decimal?)null }));
        Assert.False(cleared.TryGetProperty("salePrice", out _), "a cleared sale price is not serialised");
        Assert.Equal(100m, cleared.GetProperty("effectivePrice").GetDecimal());
    }

    [Fact]
    public async Task Snapshot_is_for_services_only_and_carries_the_effective_price()
    {
        using var trueAdmin = _factory.CreateClient().AsTrueAdmin();
        var id = await CreateAsync(trueAdmin, "Snapshot chair", 200m, salePrice: 150m);

        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync($"/api/products/{id}/snapshot")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await trueAdmin.GetAsync($"/api/products/{id}/snapshot")).StatusCode);

        using var service = _factory.CreateClient();
        service.DefaultRequestHeaders.Add("X-Internal-Api-Key", TestTokens.InternalApiKey);
        var snapshot = await ReadJson(await service.GetAsync($"/api/products/{id}/snapshot"));
        Assert.Equal("Snapshot chair", snapshot.GetProperty("title").GetString());
        Assert.Equal(200m, snapshot.GetProperty("price").GetDecimal());
        Assert.Equal(150m, snapshot.GetProperty("effectivePrice").GetDecimal());
        Assert.True(snapshot.GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await service.GetAsync("/api/products/999999/snapshot")).StatusCode);
    }
}
