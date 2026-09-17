using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
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
/// cookie; refresh rotates the token, a replayed one ends the whole session, logout ends it on
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

    [Fact]
    public async Task Refresh_rotates_the_token_and_a_replayed_token_ends_the_session()
    {
        using var client = _factory.CreateClient();
        var (_, first) = await SignInAsync(client);

        var refreshed = await client.SendAsync(Post("/api/v1/auth/refresh", first));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var second = ValueOf(RefreshCookieOf(refreshed)!);
        Assert.NotEqual(first, second);
        Assert.NotEmpty((await ReadJson(refreshed)).GetProperty("accessToken").GetString()!);

        // The spent token is replayed - somebody has a copy - so the fresh one dies with it
        var replayed = await client.SendAsync(Post("/api/v1/auth/refresh", first));
        Assert.Equal(HttpStatusCode.Unauthorized, replayed.StatusCode);
        var afterReplay = await client.SendAsync(Post("/api/v1/auth/refresh", second));
        Assert.Equal(HttpStatusCode.Unauthorized, afterReplay.StatusCode);
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
