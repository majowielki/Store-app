using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Store.BuildingBlocks.Configuration;
using Store.Contracts.Authorization;
using Store.IdentityService.Controllers;
using Store.IdentityService.Data;
using Store.IdentityService.Services;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Identity;

/// <summary>
/// Sessions: a sign-in hands out a short-lived access token and a refresh token in an httpOnly
/// cookie; refresh rotates the token, a replayed one ends the whole session (unless it comes back
/// within the reuse window while its successor is unused - a lost answer), logout ends it on
/// purpose. Regression: the old refresh endpoint renewed any signed access token forever.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class SessionTests : IClassFixture<IdentityApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IdentityApiFactory _factory;

    public SessionTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    /// <summary>The refresh cookie a response set, with its attributes, or null when it set none.</summary>
    private static string? RefreshCookieOf(HttpResponseMessage response)
        => response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith(AuthController.RefreshCookie + "=", StringComparison.Ordinal))
            : null;

    private static string ValueOf(string setCookie) => setCookie.Split(';')[0][(AuthController.RefreshCookie.Length + 1)..];

    private static HttpRequestMessage Post(string path, string? refreshToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (refreshToken is not null)
        {
            request.Headers.Add("Cookie", $"{AuthController.RefreshCookie}={refreshToken}");
        }
        return request;
    }

    private static async Task<(string AccessToken, string RefreshToken)> SignInAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/demo-login", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = RefreshCookieOf(response);
        Assert.NotNull(cookie);
        return ((await ReadJson(response)).GetProperty("accessToken").GetString()!, ValueOf(cookie!));
    }

    [Fact]
    public async Task Sign_in_sets_an_http_only_refresh_cookie_scoped_to_the_auth_routes()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/demo-login", new { });

        var cookie = RefreshCookieOf(response)!;
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", cookie, StringComparison.OrdinalIgnoreCase);
        var body = await ReadJson(response);
        Assert.False(body.TryGetProperty("refreshToken", out _), "the refresh token travels in the cookie only");

        // The access token is short-lived: minutes, not the hour it used to be
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(body.GetProperty("accessToken").GetString());
        Assert.InRange(jwt.ValidTo - DateTime.UtcNow, TimeSpan.FromMinutes(13), TimeSpan.FromMinutes(16));
    }

    /// <summary>Refreshes with <paramref name="refreshToken"/>: the status, and the token the response set (null when it set none).</summary>
    private static async Task<(HttpStatusCode Status, string? Token)> RefreshAsync(HttpClient client, string refreshToken)
    {
        var response = await client.SendAsync(Post("/api/v1/auth/refresh", refreshToken));
        var cookie = response.StatusCode == HttpStatusCode.OK ? RefreshCookieOf(response) : null;
        return (response.StatusCode, cookie is null ? null : ValueOf(cookie));
    }

    private TimeSpan ReuseWindow => _factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value.RefreshTokenReuseWindow;

    [Fact]
    public async Task Refresh_rotates_the_token_and_one_replayed_after_the_reuse_window_ends_the_session()
    {
        using var client = _factory.CreateClient();
        var (_, first) = await SignInAsync(client);

        var refreshed = await client.SendAsync(Post("/api/v1/auth/refresh", first));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var second = ValueOf(RefreshCookieOf(refreshed)!);
        Assert.NotEqual(first, second);
        Assert.NotEmpty((await ReadJson(refreshed)).GetProperty("accessToken").GetString()!);

        // The spent token is replayed long after its rotation - somebody has a copy - so the fresh one dies with it
        using (_factory.Clock.Advance(ReuseWindow + TimeSpan.FromSeconds(1)))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(client, first)).Status);
            Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(client, second)).Status);
        }
    }

    [Fact]
    public async Task A_token_presented_again_within_the_reuse_window_gets_a_new_successor_until_the_successor_is_used()
    {
        using var client = _factory.CreateClient();
        var (_, first) = await SignInAsync(client);

        // The browser left the page while its refresh was on the way: the successor never arrived
        var (_, lost) = await RefreshAsync(client, first);

        // The next page presents the spent token again, even twice (each page load renews the session)
        var (status, second) = await RefreshAsync(client, first);
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.NotEqual(lost, second);
        var (again, third) = await RefreshAsync(client, first);
        Assert.Equal(HttpStatusCode.OK, again);

        // The newest token works and carries the session on; the older successors were spent unseen
        var (next, fourth) = await RefreshAsync(client, third!);
        Assert.Equal(HttpStatusCode.OK, next);
        Assert.NotNull(fourth);

        // Its successor has now been used, so the first token coming back is a copy: the session ends
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(client, first)).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(client, fourth!)).Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Tabs_refreshing_the_same_token_together_all_keep_the_session(int keptByTheBrowser)
    {
        using var client = _factory.CreateClient();
        var (_, first) = await SignInAsync(client);

        var tabs = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => RefreshAsync(client, first)));

        Assert.All(tabs, tab => Assert.Equal(HttpStatusCode.OK, tab.Status));
        // Whichever answer the browser applied last, the token it keeps still renews the session
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(client, tabs[keptByTheBrowser].Token!)).Status);
    }

    [Fact]
    public async Task After_logout_a_token_spent_within_the_reuse_window_is_refused()
    {
        using var client = _factory.CreateClient();
        var (_, first) = await SignInAsync(client);
        var (_, second) = await RefreshAsync(client, first);

        var logout = await client.SendAsync(Post("/api/v1/auth/logout", second));
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        // The successor is revoked with the session, so the earlier token is not a lost answer
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(client, first)).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(client, second!)).Status);
    }

    [Fact]
    public async Task The_token_names_its_session_and_marks_the_shared_demo_accounts()
    {
        using var client = _factory.CreateClient();
        var (accessToken, refreshToken) = await SignInAsync(client);
        var (another, _) = await SignInAsync(client);

        var refreshed = await client.SendAsync(Post("/api/v1/auth/refresh", refreshToken));
        var renewed = (await ReadJson(refreshed)).GetProperty("accessToken").GetString()!;

        // The review service keeps a demo visitor's reviews per session: the same across refreshes, new at each sign-in
        var session = SessionOf(accessToken);
        Assert.NotNull(session);
        Assert.Equal(session, SessionOf(renewed));
        Assert.NotEqual(session, SessionOf(another));
        Assert.Contains(Read(accessToken).Claims, c => c.Type == StoreClaims.DemoAccount && c.Value == StoreClaims.DemoAccountValue);

        var registered = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"session-{Guid.NewGuid():N}@test.local",
            password = "Session-Password-1!",
            confirmPassword = "Session-Password-1!",
            firstName = "Real",
            lastName = "Customer"
        });
        var customerToken = (await ReadJson(registered)).GetProperty("accessToken").GetString()!;
        Assert.NotNull(SessionOf(customerToken));
        Assert.DoesNotContain(Read(customerToken).Claims, c => c.Type == StoreClaims.DemoAccount);

        static JsonWebToken Read(string token) => new JsonWebTokenHandler().ReadJsonWebToken(token);
        static string? SessionOf(string token) => Read(token).Claims.FirstOrDefault(c => c.Type == StoreClaims.SessionId)?.Value;
    }

    [Fact]
    public async Task Logout_ends_the_session_and_clears_the_cookie()
    {
        using var client = _factory.CreateClient();
        var (_, refreshToken) = await SignInAsync(client);

        var logout = await client.SendAsync(Post("/api/v1/auth/logout", refreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        var cleared = RefreshCookieOf(logout)!;
        Assert.Contains("expires=", cleared, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, ValueOf(cleared));

        var afterLogout = await client.SendAsync(Post("/api/v1/auth/refresh", refreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task Refresh_needs_a_cookie_and_an_access_token_is_not_one()
    {
        using var client = _factory.CreateClient();
        var (accessToken, _) = await SignInAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Post("/api/v1/auth/refresh"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Post("/api/v1/auth/refresh", accessToken))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Post("/api/v1/auth/refresh", "not-a-token"))).StatusCode);
    }

    [Fact]
    public async Task Expired_and_long_revoked_tokens_are_purged()
    {
        using var client = _factory.CreateClient();
        var (_, refreshToken) = await SignInAsync(client);
        await client.SendAsync(Post("/api/v1/auth/logout", refreshToken));

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hash = scope.ServiceProvider.GetRequiredService<ITokenService>().HashRefreshToken(refreshToken);
        var stored = await context.RefreshTokens.SingleAsync(t => t.TokenHash == hash);
        stored.RevokedAt = DateTime.UtcNow.AddDays(-30);
        await context.SaveChangesAsync();

        var cleanup = _factory.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>().OfType<RefreshTokenCleanupService>().Single();
        var deleted = await cleanup.PurgeAsync(CancellationToken.None);

        Assert.True(deleted >= 1);
        Assert.False(await context.RefreshTokens.AnyAsync(t => t.TokenHash == hash));
    }
}

/// <summary>Without Demo:Enabled the showcase accounts are not seeded and their logins do not exist.</summary>
[Collection(PostgresTests.Name)]
public sealed class DemoDisabledTests
{
    private readonly PostgresFixture _postgres;

    public DemoDisabledTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    private sealed class DemoDisabledFactory : StoreApiFactory<IdentityDbContext>
    {
        public DemoDisabledFactory(PostgresFixture postgres) : base(postgres)
        {
        }

        protected override string? DatabaseName => "store_identity_nodemo_test";

        protected override void ConfigureSettings(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseSetting("Demo:Enabled", "false");
        }
    }

    [Fact]
    public async Task Demo_logins_answer_404_and_no_demo_account_exists()
    {
        await using var factory = new DemoDisabledFactory(_postgres);
        await ((IAsyncLifetime)factory).InitializeAsync();
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/v1/auth/demo-login", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/v1/auth/demo-admin-login", null)).StatusCode);

        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users;
        Assert.False(await users.AnyAsync(u => u.Email!.StartsWith("demo")));
        Assert.True(await users.AnyAsync(u => u.Email == TestUsers.TrueAdminEmail), "the true admin is seeded regardless");
    }
}
