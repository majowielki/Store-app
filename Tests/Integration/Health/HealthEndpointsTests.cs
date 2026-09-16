using Microsoft.AspNetCore.Hosting;
using Store.AuditLogService.Data;
using Store.Tests.Integration.TestSupport;
using System.Net;
using Xunit;

namespace Store.Tests.Integration.Health;

/// <summary>
/// AuditLogService pointed at a database that does not exist: the process is alive, but it must
/// report itself as not ready. Before this, a controller answered "Healthy" on /health no matter
/// what, so orchestrators never noticed a dead database.
/// </summary>
public sealed class UnreachableDatabaseFactory : StoreApiFactory<AuditLogDbContext>
{
    public UnreachableDatabaseFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    protected override string? DatabaseName => null;

    protected override void ConfigureSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=127.0.0.1;Port=1;Database=nowhere;Username=x;Password=x;Timeout=1;Command Timeout=1");
    }
}

[Collection(PostgresTests.Name)]
public sealed class HealthEndpointsTests : IClassFixture<Audit.AuditApiFactory>, IClassFixture<UnreachableDatabaseFactory>
{
    private readonly Audit.AuditApiFactory _healthy;
    private readonly UnreachableDatabaseFactory _withoutDatabase;

    public HealthEndpointsTests(Audit.AuditApiFactory healthy, UnreachableDatabaseFactory withoutDatabase)
    {
        _healthy = healthy;
        _withoutDatabase = withoutDatabase;
    }

    [Fact]
    public async Task Ready_and_live_are_ok_when_the_database_is_reachable()
    {
        using var client = _healthy.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task Ready_is_503_when_the_database_is_down_but_live_stays_200()
    {
        using var client = _withoutDatabase.CreateClient();

        var live = await client.GetAsync("/health/live");
        var ready = await client.GetAsync("/health/ready");
        var details = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, details.StatusCode);
        Assert.Contains("\"database\":\"Unhealthy\"", await ready.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_old_always_healthy_controller_route_is_gone()
    {
        using var client = _withoutDatabase.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.NotEqual("Healthy", await response.Content.ReadAsStringAsync());
    }
}
