using Microsoft.AspNetCore.Hosting;
using Store.AuditLogService.Data;
using Store.BuildingBlocks.Persistence;
using Store.Tests.Integration.TestSupport;
using Xunit;

namespace Store.Tests.Integration.Health;

/// <summary>
/// Outside Development a service does not migrate its own database; it refuses to start when
/// migrations are pending instead of answering "healthy" and failing every query.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class PendingMigrationsTests
{
    private readonly PostgresFixture _postgres;

    public PendingMigrationsTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    private sealed class VerifyOnlyFactory : StoreApiFactory<AuditLogDbContext>
    {
        public VerifyOnlyFactory(PostgresFixture postgres) : base(postgres)
        {
        }

        // A database nobody migrated yet
        protected override string? DatabaseName => "store_audit_unmigrated_test";

        protected override void ConfigureSettings(IWebHostBuilder builder)
        {
            builder.UseSetting(DatabaseStartup.SchemaSetting, nameof(SchemaStartup.Verify));
        }
    }

    [Fact]
    public async Task Service_refuses_to_start_while_migrations_are_pending()
    {
        await using var factory = new VerifyOnlyFactory(_postgres);

        var failure = await Assert.ThrowsAnyAsync<Exception>(async () => await ((IAsyncLifetime)factory).InitializeAsync());

        var message = failure.ToString();
        Assert.Contains("behind the code", message);
        Assert.Contains(DatabaseStartup.MigrateArgument, message);
    }
}
