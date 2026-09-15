using Store.Shared.Authorization;
using Store.Shared.Configuration;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Store.Tests.Integration.Audit;

[Collection(PostgresTests.Name)]
public sealed class AuditLogEndpointsTests : IClassFixture<AuditApiFactory>
{
    private readonly AuditApiFactory _factory;

    public AuditLogEndpointsTests(AuditApiFactory factory)
    {
        _factory = factory;
    }

    private static object Entry(string action = "INTEGRATION_TEST") => new
    {
        action,
        entityName = "Probe",
        entityId = Guid.NewGuid().ToString(),
        userId = "user-1",
        userEmail = "user-1@test.local",
        timestamp = DateTime.UtcNow
    };

    // BLK-02: reads required a role nobody has, so every admin got 403
    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData(Roles.User, HttpStatusCode.Forbidden)]
    [InlineData(Roles.DemoAdmin, HttpStatusCode.OK)]
    [InlineData(Roles.TrueAdmin, HttpStatusCode.OK)]
    public async Task Reading_audit_logs_requires_an_admin_role(string who, HttpStatusCode expected)
    {
        using var client = _factory.CreateClient().As(who);

        var response = await client.GetAsync("/api/auditlog?page=1&pageSize=10");

        Assert.Equal(expected, response.StatusCode);
    }

    // SEC-04: the internal endpoint accepts the shared service key only
    [Fact]
    public async Task Internal_endpoint_rejects_requests_without_the_service_key()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auditlog/internal", Entry());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Internal_endpoint_rejects_a_wrong_service_key()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalApiOptions.HeaderName, "definitely-not-the-configured-key-but-long");

        var response = await client.PostAsJsonAsync("/api/auditlog/internal", Entry());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Internal_endpoint_accepts_the_service_key_and_stores_the_entry()
    {
        using var writer = _factory.CreateClient();
        writer.DefaultRequestHeaders.Add(InternalApiOptions.HeaderName, TestTokens.InternalApiKey);
        var action = $"INTERNAL_{Guid.NewGuid():N}";

        var created = await writer.PostAsJsonAsync("/api/auditlog/internal", Entry(action));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var reader = _factory.CreateClient().AsTrueAdmin();
        var list = await reader.GetStringAsync("/api/auditlog?page=1&pageSize=100");
        Assert.Contains(action, list, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Service_key_does_not_grant_admin_reads()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalApiOptions.HeaderName, TestTokens.InternalApiKey);

        var response = await client.GetAsync("/api/auditlog");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Signed_in_users_can_write_their_own_entries()
    {
        using var client = _factory.CreateClient().AsUser();

        var response = await client.PostAsJsonAsync("/api/auditlog", Entry("USER_ACTION"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
