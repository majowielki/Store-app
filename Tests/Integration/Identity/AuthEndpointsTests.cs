using Store.Shared.Authorization;
using Store.Tests.Integration.TestSupport;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Identity;

[Collection(PostgresTests.Name)]
public sealed class AuthEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IdentityApiFactory _factory;

    public AuthEndpointsTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    private static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@test.local";

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    private static async Task<string> RegisterAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            confirmPassword = password,
            firstName = "Test",
            lastName = "User"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJson(response);
        return body.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    [Fact]
    public async Task Register_issues_a_token_with_the_user_role()
    {
        using var client = _factory.CreateClient();

        var accessToken = await RegisterAsync(client, UniqueEmail("register"), "Register-Password-1!");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        Assert.Equal(TestTokens.Issuer, jwt.Issuer);
        Assert.Contains(jwt.Claims, c => c.Type == "role" && c.Value == Roles.User);
    }

    [Fact]
    public async Task Seeded_true_admin_can_log_in_and_gets_the_true_admin_role()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestUsers.TrueAdminEmail,
            password = TestUsers.TrueAdminPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJson(response);
        var roles = body.GetProperty("data").GetProperty("user").GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Contains(Roles.TrueAdmin, roles);
    }

    [Fact]
    public async Task Wrong_password_returns_401_and_the_fifth_failure_locks_the_account()
    {
        using var client = _factory.CreateClient();
        var email = UniqueEmail("lockout");
        const string password = "Lockout-Password-1!";
        await RegisterAsync(client, email, password);

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong-password" });
            statuses.Add(response.StatusCode);
        }

        // Regression: Identity lockout (5 attempts / 5 minutes) is honoured and reported as 423
        Assert.Equal(
            new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Locked },
            statuses);

        var correctPasswordWhileLocked = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.Locked, correctPasswordWhileLocked.StatusCode);
    }

    [Fact]
    public async Task Login_failures_do_not_reveal_whether_the_account_exists()
    {
        using var client = _factory.CreateClient();

        var unknown = await client.PostAsJsonAsync("/api/auth/login", new { email = UniqueEmail("nobody"), password = "whatever-1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        var body = await ReadJson(unknown);
        Assert.Equal("Invalid email or password", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Me_is_anonymous_without_a_token_and_returns_the_profile_with_one()
    {
        using var client = _factory.CreateClient();
        var email = UniqueEmail("me");
        var accessToken = await RegisterAsync(client, email, "Me-Password-1!");

        var anonymous = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.NoContent, anonymous.StatusCode);

        var authenticated = await client.WithToken(accessToken).GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, authenticated.StatusCode);
        var profile = await ReadJson(authenticated);
        Assert.Equal(email, profile.GetProperty("email").GetString());
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData(Roles.User, HttpStatusCode.Forbidden)]
    [InlineData(Roles.DemoAdmin, HttpStatusCode.OK)]
    [InlineData(Roles.TrueAdmin, HttpStatusCode.OK)]
    public async Task User_list_requires_an_admin_role(string who, HttpStatusCode expected)
    {
        using var client = _factory.CreateClient().As(who);

        var response = await client.GetAsync("/api/auth/users");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Tokens_signed_with_another_key_are_rejected()
    {
        using var client = _factory.CreateClient();
        var forged = TestTokens.TrueAdmin().Replace('a', 'b');

        var response = await client.WithToken(forged).GetAsync("/api/auth/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
