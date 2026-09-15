using Store.Shared.Authorization;
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
    [InlineData("/api/cart", "anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("/api/cart", Roles.User, PassedTheGateway)]
    [InlineData("/api/orders", "anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("/api/orders", Roles.User, PassedTheGateway)]
    [InlineData("/api/admin/users", "anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("/api/admin/users", Roles.User, HttpStatusCode.Forbidden)]
    [InlineData("/api/admin/users", Roles.DemoAdmin, PassedTheGateway)]
    [InlineData("/api/admin/users", Roles.TrueAdmin, PassedTheGateway)]
    [InlineData("/api/auditlog", Roles.User, HttpStatusCode.Forbidden)]
    [InlineData("/api/auditlog", Roles.TrueAdmin, PassedTheGateway)]
    [InlineData("/api/products", "anonymous", PassedTheGateway)]
    [InlineData("/api/auth/me", "anonymous", PassedTheGateway)]
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

        var response = await client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // SEC-06: credential endpoints are rate limited per client address
    [Fact]
    public async Task Login_attempts_beyond_the_limit_get_429_with_retry_after()
    {
        using var client = ClientFrom("10.0.2.1");
        var payload = new { email = "a@b.c", password = "x" };

        for (var i = 0; i < GatewayApiFactory.CredentialPermitLimit; i++)
        {
            var allowed = await client.PostAsJsonAsync("/api/auth/login", payload);
            Assert.Equal(PassedTheGateway, allowed.StatusCode);
        }

        var throttled = await client.PostAsJsonAsync("/api/auth/login", payload);

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
            await first.PostAsJsonAsync("/api/auth/login", payload);
        }

        var otherClient = await second.PostAsJsonAsync("/api/auth/login", payload);

        Assert.Equal(PassedTheGateway, otherClient.StatusCode);
    }

    [Fact]
    public async Task Profile_calls_are_not_throttled_like_credential_endpoints()
    {
        using var client = ClientFrom("10.0.4.1");

        for (var i = 0; i < GatewayApiFactory.CredentialPermitLimit * 2; i++)
        {
            var response = await client.GetAsync("/api/auth/me");
            Assert.Equal(PassedTheGateway, response.StatusCode);
        }
    }
}
