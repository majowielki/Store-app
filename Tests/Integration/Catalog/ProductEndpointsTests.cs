using Store.Shared.Authorization;
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

    [Fact]
    public async Task Writes_are_audited_through_the_audit_client()
    {
        using var client = _factory.CreateClient().AsTrueAdmin();

        await client.PostAsJsonAsync("/api/products", ValidProduct("Audited product"));

        Assert.Contains(_factory.AuditLog.Entries, e => e.Action == "PRODUCT_CREATED");
    }
}
