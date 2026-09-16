using Microsoft.AspNetCore.Hosting;
using Store.AuditLogService.Data;
using Store.Tests.Integration.TestSupport;
using System.Net;
using Testcontainers.PostgreSql;
using Xunit;

namespace Store.Tests.Integration.Health;

/// <summary>
/// AuditLogService with a database of its own that is stopped once the service is up. The
/// process stays alive, but it must report itself as not ready. Before this, a controller
/// answered "Healthy" on /health no matter what, so orchestrators never noticed a dead database.
/// (A database that is unreachable at start-up is a different case: the service does not start.)
/// </summary>
public sealed class DatabaseLostAfterStartFactory : StoreApiFactory<AuditLogDbContext>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _ownDatabase = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("store_audit_lost")
        .WithUsername("store_test")
        .WithPassword("store_test")
        .WithCleanUp(true)
        .Build();

    public DatabaseLostAfterStartFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    protected override string? DatabaseName => null;

    protected override void ConfigureSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _ownDatabase.GetConnectionString() + ";Timeout=2;Command Timeout=2");
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _ownDatabase.StartAsync();
        _ = Server; // starts and migrates against the live database
        await _ownDatabase.StopAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _ownDatabase.DisposeAsync();
    }
}

[Collection(PostgresTests.Name)]
public sealed class HealthEndpointsTests : IClassFixture<Audit.AuditApiFactory>, IClassFixture<DatabaseLostAfterStartFactory>
{
    private readonly Audit.AuditApiFactory _healthy;
    private readonly DatabaseLostAfterStartFactory _withoutDatabase;

    public HealthEndpointsTests(Audit.AuditApiFactory healthy, DatabaseLostAfterStartFactory withoutDatabase)
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
