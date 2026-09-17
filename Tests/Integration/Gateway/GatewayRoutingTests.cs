using Store.Contracts.Authorization;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Store.Tests.Integration.Gateway;

[Collection(PostgresTests.Name)]
public sealed class GatewayRoutingTests : IClassFixture<GatewayApiFactory>
{
    private readonly GatewayApiFactory _factory;

    public GatewayRoutingTests(GatewayApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Each test gets its own client address so rate-limit buckets never interfere.</summary>
    private HttpClient ClientFrom(string address)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", address);
        return client;
    }

    // Requests that reach the proxy end in 502 because no downstream service is running
    private const HttpStatusCode PassedTheGateway = HttpStatusCode.BadGateway;

    [Theory]
    [InlineData("/api/v1/cart", "anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("/api/v1/cart", Roles.User, PassedTheGateway)]
    [InlineData("/api/v1/orders", "anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("/api/v1/orders", Roles.User, PassedTheGateway)]
    [InlineData("/api/v1/admin/users", "anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("/api/v1/admin/users", Roles.User, HttpStatusCode.Forbidden)]
    [InlineData("/api/v1/admin/users", Roles.DemoAdmin, PassedTheGateway)]
    [InlineData("/api/v1/admin/users", Roles.TrueAdmin, PassedTheGateway)]
    [InlineData("/api/v1/admin/orders", Roles.User, HttpStatusCode.Forbidden)]
    [InlineData("/api/v1/admin/orders/1", Roles.DemoAdmin, PassedTheGateway)]
    [InlineData("/api/v1/auditlog", Roles.User, HttpStatusCode.Forbidden)]
    [InlineData("/api/v1/auditlog", Roles.TrueAdmin, PassedTheGateway)]
    [InlineData("/api/v1/products", "anonymous", PassedTheGateway)]
    [InlineData("/api/v1/auth/me", "anonymous", PassedTheGateway)]
    public async Task Routes_enforce_the_shared_policies_before_proxying(string path, string who, HttpStatusCode expected)
    {
        using var client = ClientFrom($"10.0.0.{Random.Shared.Next(1, 250)}").As(who);

        var response = await client.GetAsync(path);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_paths_are_not_proxied()
    {
        using var client = ClientFrom("10.0.1.1");

        var response = await client.GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Regression: credential endpoints are rate limited per client address
    [Fact]
    public async Task Login_attempts_beyond_the_limit_get_429_with_retry_after()
    {
        using var client = ClientFrom("10.0.2.1");
        var payload = new { email = "a@b.c", password = "x" };

        for (var i = 0; i < GatewayApiFactory.CredentialPermitLimit; i++)
        {
            var allowed = await client.PostAsJsonAsync("/api/v1/auth/login", payload);
            Assert.Equal(PassedTheGateway, allowed.StatusCode);
        }

        var throttled = await client.PostAsJsonAsync("/api/v1/auth/login", payload);

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.NotNull(throttled.Headers.RetryAfter);
    }

    [Fact]
    public async Task Rate_limit_buckets_are_per_client_address()
    {
        using var first = ClientFrom("10.0.3.1");
        using var second = ClientFrom("10.0.3.2");
        var payload = new { email = "a@b.c", password = "x" };

        for (var i = 0; i <= GatewayApiFactory.CredentialPermitLimit; i++)
        {
            await first.PostAsJsonAsync("/api/v1/auth/login", payload);
        }

        var otherClient = await second.PostAsJsonAsync("/api/v1/auth/login", payload);

        Assert.Equal(PassedTheGateway, otherClient.StatusCode);
    }

    // The UI renews the session on every page load; that must not run into the credential limit
    [Theory]
    [InlineData("GET", "/api/v1/auth/me")]
    [InlineData("POST", "/api/v1/auth/refresh")]
    public async Task Profile_and_refresh_calls_are_not_throttled_like_credential_endpoints(string method, string path)
    {
        using var client = ClientFrom("10.0.4." + (method == "GET" ? "1" : "2"));

        for (var i = 0; i < GatewayApiFactory.CredentialPermitLimit * 2; i++)
        {
            var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
            Assert.Equal(PassedTheGateway, response.StatusCode);
        }
    }

    // The shop routes are limited per signed-in user, wherever the requests come from
    [Fact]
    public async Task Shop_routes_are_limited_per_user_and_the_next_user_is_not_affected()
    {
        using var first = ClientFrom("10.0.5.1").WithToken(TestTokens.User("rate-user-a"));
        using var sameUserElsewhere = ClientFrom("10.0.5.2").WithToken(TestTokens.User("rate-user-a"));
        using var otherUser = ClientFrom("10.0.5.1").WithToken(TestTokens.User("rate-user-b"));

        for (var i = 0; i < GatewayApiFactory.ApiPermitLimit; i++)
        {
            Assert.Equal(PassedTheGateway, (await first.GetAsync("/api/v1/cart")).StatusCode);
        }

        var throttled = await sameUserElsewhere.GetAsync("/api/v1/cart");
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.NotNull(throttled.Headers.RetryAfter);
        Assert.Equal("application/problem+json", throttled.Content.Headers.ContentType?.MediaType);

        Assert.Equal(PassedTheGateway, (await otherUser.GetAsync("/api/v1/cart")).StatusCode);
    }

    [Fact]
    public async Task Anonymous_catalogue_requests_are_limited_per_address()
    {
        using var client = ClientFrom("10.0.6.1");

        for (var i = 0; i < GatewayApiFactory.ApiPermitLimit; i++)
        {
            Assert.Equal(PassedTheGateway, (await client.GetAsync("/api/v1/products")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/api/v1/products")).StatusCode);
        Assert.Equal(PassedTheGateway, (await ClientFrom("10.0.6.2").GetAsync("/api/v1/products")).StatusCode);
    }

    [Fact]
    public async Task Admin_routes_have_their_own_stricter_limit()
    {
        using var client = ClientFrom("10.0.7.1").WithToken(TestTokens.TrueAdmin("rate-admin-a"));

        for (var i = 0; i < GatewayApiFactory.AdminPermitLimit; i++)
        {
            Assert.Equal(PassedTheGateway, (await client.GetAsync("/api/v1/admin/users")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/api/v1/admin/users")).StatusCode);
        // The shop bucket of the same user is untouched
        Assert.Equal(PassedTheGateway, (await client.GetAsync("/api/v1/products")).StatusCode);
    }

    // The cart page needs the pricing rules before anyone signs in; the rest of /orders stays private
    [Fact]
    public async Task Pricing_rules_are_public_while_orders_are_not()
    {
        using var client = ClientFrom("10.0.8.1");

        Assert.Equal(PassedTheGateway, (await client.GetAsync("/api/v1/orders/pricing-rules")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/orders/my-orders")).StatusCode);
    }

    [Fact]
    public async Task Gateway_answers_carry_the_security_headers()
    {
        using var client = ClientFrom("10.0.9.1");

        var response = await client.GetAsync("/api/v1/cart");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Cors_allows_only_the_configured_origin()
    {
        using var client = ClientFrom("10.0.10.1");

        var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/v1/products");
        preflight.Headers.Add("Origin", GatewayApiFactory.AllowedOrigin);
        preflight.Headers.Add("Access-Control-Request-Method", "GET");
        var allowed = await client.SendAsync(preflight);
        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
        Assert.Equal(GatewayApiFactory.AllowedOrigin, allowed.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", allowed.Headers.GetValues("Access-Control-Allow-Credentials").Single());

        var stranger = new HttpRequestMessage(HttpMethod.Options, "/api/v1/products");
        stranger.Headers.Add("Origin", "http://evil.test");
        stranger.Headers.Add("Access-Control-Request-Method", "GET");
        var refused = await client.SendAsync(stranger);
        Assert.False(refused.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task The_gateway_serves_no_endpoints_of_its_own()
    {
        using var client = ClientFrom("10.0.11.1");

        var response = await client.PostAsJsonAsync("/api/v1/newsletter/subscribe", new { email = "a@b.c" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
