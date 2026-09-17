using Microsoft.Extensions.DependencyInjection;
using Store.Contracts.Audit.V1;
using Store.Contracts.Authorization;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Audit;

[Collection(PostgresTests.Name)]
public sealed class AuditLogEndpointsTests : IClassFixture<AuditApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly AuditApiFactory _factory;

    public AuditLogEndpointsTests(AuditApiFactory factory)
    {
        _factory = factory;
    }

    private static AuditEvent Entry(string action, string entityId) => new(
        action, "Probe", entityId, "user-1", "tests", DateTime.UtcNow,
        Details: """{"probe":true}""", OldValues: null, NewValues: """{"title":"after"}""");

    // Regression: reads required a role nobody has, so every admin got 403
    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData(Roles.User, HttpStatusCode.Forbidden)]
    [InlineData(Roles.DemoAdmin, HttpStatusCode.OK)]
    [InlineData(Roles.TrueAdmin, HttpStatusCode.OK)]
    public async Task Reading_audit_logs_requires_an_admin_role(string who, HttpStatusCode expected)
    {
        using var client = _factory.CreateClient().As(who);

        var response = await client.GetAsync("/api/v1/auditlog?page=1&pageSize=10");

        Assert.Equal(expected, response.StatusCode);
    }

    // Entries arrive as events; there is no endpoint that accepts them from anyone
    [Fact]
    public async Task Audit_entries_cannot_be_written_over_http()
    {
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var direct = await admin.PostAsJsonAsync("/api/v1/auditlog", new { action = "FORGED", entityName = "Probe" });
        var internalRoute = await admin.PostAsJsonAsync("/api/v1/auditlog/internal", new { action = "FORGED", entityName = "Probe" });

        Assert.Equal(HttpStatusCode.MethodNotAllowed, direct.StatusCode);
        Assert.Contains(internalRoute.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
    }

    [Fact]
    public async Task Published_event_becomes_an_entry_readable_by_admins()
    {
        var entityId = Guid.NewGuid().ToString("N");
        await _factory.Bus.Bus.Publish(Entry("PROBE_RECORDED", entityId));

        using var reader = _factory.CreateClient().AsTrueAdmin();
        JsonElement entry = default;
        await Eventually.AssertAsync(async () =>
        {
            var page = JsonSerializer.Deserialize<JsonElement>(await reader.GetStringAsync($"/api/v1/auditlog?entityName=Probe&entityId={entityId}"), Json);
            entry = Assert.Single(page.GetProperty("items").EnumerateArray());
        });

        Assert.Equal("PROBE_RECORDED", entry.GetProperty("action").GetString());
        Assert.Equal("user-1", entry.GetProperty("userId").GetString());
        Assert.Equal("tests", entry.GetProperty("serviceName").GetString());
        Assert.Equal("""{"title":"after"}""", entry.GetProperty("newValues").GetString());
        Assert.False(entry.TryGetProperty("ipAddress", out _), "request data is not part of the trail any more");
    }

    [Fact]
    public async Task Oversized_identifiers_are_trimmed_instead_of_rejected()
    {
        var longAction = new string('X', 500);
        var entityId = Guid.NewGuid().ToString("N");
        await _factory.Bus.Bus.Publish(Entry(longAction, entityId));

        using var reader = _factory.CreateClient().AsTrueAdmin();
        JsonElement entry = default;
        await Eventually.AssertAsync(async () =>
        {
            var page = JsonSerializer.Deserialize<JsonElement>(await reader.GetStringAsync($"/api/v1/auditlog?entityName=Probe&entityId={entityId}"), Json);
            entry = Assert.Single(page.GetProperty("items").EnumerateArray());
        });

        Assert.Equal(50, entry.GetProperty("action").GetString()!.Length);
    }

    // Regression: the date-range listing reported the page length as the total, and a bound
    // without an offset reached Npgsql with Kind=Unspecified and failed against timestamptz
    [Fact]
    public async Task Date_bounds_filter_in_the_database_and_count_the_whole_range()
    {
        var entityId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        await _factory.Bus.Bus.Publish(new AuditEvent("OLD", "Range", entityId, null, "tests", now.AddDays(-10)));
        await _factory.Bus.Bus.Publish(new AuditEvent("RECENT", "Range", entityId, null, "tests", now.AddMinutes(-1)));
        await _factory.Bus.Bus.Publish(new AuditEvent("RECENT", "Range", entityId, null, "tests", now));

        using var reader = _factory.CreateClient().AsTrueAdmin();
        var from = Uri.EscapeDataString(now.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ss"));
        var to = Uri.EscapeDataString(now.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"));
        JsonElement page = default;
        await Eventually.AssertAsync(async () =>
        {
            page = JsonSerializer.Deserialize<JsonElement>(await reader.GetStringAsync($"/api/v1/auditlog?entityName=Range&entityId={entityId}&from={from}&to={to}&pageSize=1"), Json);
            Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
        });
        Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal(2, page.GetProperty("totalPages").GetInt32());

        var reversed = await reader.GetAsync($"/api/v1/auditlog?from={to}&to={from}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reversed.StatusCode);
    }

    // Every error is a problem response, including the ones the framework produces on its own
    [Fact]
    public async Task Errors_are_problem_responses()
    {
        using var anonymous = _factory.CreateClient();
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var unauthorized = await anonymous.GetAsync("/api/v1/auditlog");
        var notFound = await admin.GetAsync("/api/v1/auditlog/999999999");

        Assert.Equal("application/problem+json", unauthorized.Content.Headers.ContentType?.MediaType);
        Assert.Equal(401, JsonSerializer.Deserialize<JsonElement>(await unauthorized.Content.ReadAsStringAsync(), Json).GetProperty("status").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        var problem = JsonSerializer.Deserialize<JsonElement>(await notFound.Content.ReadAsStringAsync(), Json);
        Assert.Equal("Not Found", problem.GetProperty("title").GetString());
        Assert.Equal("Audit log 999999999 was not found", problem.GetProperty("detail").GetString());
    }
}

[Collection(PostgresTests.Name)]
public sealed class AuditRetentionTests : IClassFixture<AuditApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly AuditApiFactory _factory;

    public AuditRetentionTests(AuditApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<int> CountAsync(HttpClient reader, string entityId)
        => JsonSerializer.Deserialize<JsonElement>(await reader.GetStringAsync($"/api/v1/auditlog?entityName=Retention&entityId={entityId}"), Json)
            .GetProperty("totalCount").GetInt32();

    // Regression: the trail grew without limit
    [Fact]
    public async Task Entries_older_than_the_retention_period_are_purged()
    {
        var old = Guid.NewGuid().ToString("N");
        var recent = Guid.NewGuid().ToString("N");
        await _factory.Bus.Bus.Publish(new AuditEvent("OLD", "Retention", old, null, "tests", DateTime.UtcNow.AddDays(-400)));
        await _factory.Bus.Bus.Publish(new AuditEvent("RECENT", "Retention", recent, null, "tests", DateTime.UtcNow));
        using var reader = _factory.CreateClient().AsTrueAdmin();
        await Eventually.AssertAsync(async () =>
        {
            Assert.Equal(1, await CountAsync(reader, old));
            Assert.Equal(1, await CountAsync(reader, recent));
        });

        var retention = _factory.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>().OfType<Store.AuditLogService.Services.AuditRetentionService>().Single();
        var deleted = await retention.PurgeAsync(CancellationToken.None);

        Assert.True(deleted >= 1);
        Assert.Equal(0, await CountAsync(reader, old));
        Assert.Equal(1, await CountAsync(reader, recent));
    }
}
