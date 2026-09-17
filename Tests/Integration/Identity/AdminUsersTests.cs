using Store.Contracts.Authorization;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Identity;

/// <summary>
/// Regression for the admin "Users" panel: the detail route did not exist, search/isActive
/// were ignored and every caller - true-admin included - got anonymised placeholders.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class AdminUsersTests : IClassFixture<IdentityApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IdentityApiFactory _factory;

    public AdminUsersTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    private async Task<(string Id, string Email)> RegisterUser(string prefix)
    {
        using var client = _factory.CreateClient();
        var email = $"{prefix}-{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Admin-Panel-Password-1!",
            confirmPassword = "Admin-Panel-Password-1!",
            firstName = "Panel",
            lastName = prefix
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var id = (await ReadJson(response)).GetProperty("user").GetProperty("id").GetString()!;
        return (id, email);
    }

    [Fact]
    public async Task True_admin_sees_real_user_data_and_can_open_the_detail_route()
    {
        var (id, email) = await RegisterUser("real");
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var list = await ReadJson(await admin.GetAsync($"/api/v1/admin/users?search={Uri.EscapeDataString(email)}"));
        var detail = await admin.GetAsync($"/api/v1/admin/users/{id}");

        var row = Assert.Single(list.GetProperty("items").EnumerateArray());
        Assert.Equal(email, row.GetProperty("email").GetString());
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(email, (await ReadJson(detail)).GetProperty("email").GetString());
    }

    [Fact]
    public async Task Demo_admin_gets_masked_personal_data()
    {
        var (id, email) = await RegisterUser("masked");
        using var demoAdmin = _factory.CreateClient().AsDemoAdmin();

        var list = await ReadJson(await demoAdmin.GetAsync($"/api/v1/admin/users?search={Uri.EscapeDataString(email)}"));
        var detail = await ReadJson(await demoAdmin.GetAsync($"/api/v1/admin/users/{id}"));

        var row = Assert.Single(list.GetProperty("items").EnumerateArray());
        Assert.Equal("anonymized-user-email", row.GetProperty("email").GetString());
        Assert.Equal("anonymized-user-email", detail.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Search_and_active_filters_are_applied()
    {
        var (_, email) = await RegisterUser("filter");
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var byFragment = await ReadJson(await admin.GetAsync("/api/v1/admin/users?search=filter-"));
        var inactive = await ReadJson(await admin.GetAsync($"/api/v1/admin/users?search={Uri.EscapeDataString(email)}&isActive=false"));
        var nobody = await ReadJson(await admin.GetAsync("/api/v1/admin/users?search=no-such-user-anywhere"));

        Assert.Contains(byFragment.GetProperty("items").EnumerateArray(), u => u.GetProperty("email").GetString() == email);
        Assert.Empty(inactive.GetProperty("items").EnumerateArray());
        Assert.Equal(0, nobody.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Unknown_user_id_is_404()
    {
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var response = await admin.GetAsync($"/api/v1/admin/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData(Roles.User, HttpStatusCode.Forbidden)]
    public async Task Panel_is_closed_to_non_admins(string who, HttpStatusCode expected)
    {
        using var client = _factory.CreateClient().As(who);

        Assert.Equal(expected, (await client.GetAsync("/api/v1/admin/users")).StatusCode);
    }
}
