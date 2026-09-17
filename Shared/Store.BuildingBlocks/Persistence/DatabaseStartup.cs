using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Store.BuildingBlocks.Persistence;

/// <summary>What a service does with its database schema when it starts (<c>Database:Schema</c>).</summary>
public enum SchemaStartup
{
    /// <summary>Apply pending migrations and the seed. The default in Development and Testing.</summary>
    Migrate,

    /// <summary>Refuse to start while migrations are pending. The default everywhere else.</summary>
    Verify,

    /// <summary>
    /// Touch nothing - for tools that start the host without a database, such as the OpenAPI
    /// export. Never for a deployment: a service that skips the check answers "healthy" and
    /// fails every query.
    /// </summary>
    None
}

/// <summary>
/// How a service treats its database schema at start-up. Migrating from inside the running
/// application is fine for one developer instance and wrong for a deployment: two replicas
/// would migrate at once, and a failed migration would leave a "healthy" service that fails
/// every query. So:
/// <list type="bullet">
/// <item><c>--migrate</c> on the command line applies the migrations and the seed, then exits -
/// the deployment step (CD job, init container) runs exactly this</item>
/// <item>otherwise <c>Database:Schema</c> decides (<see cref="SchemaStartup"/>): migrate and seed
/// while starting, verify that nothing is pending, or leave the database alone</item>
/// </list>
/// </summary>
public static class DatabaseStartup
{
    public const string MigrateArgument = "--migrate";
    public const string SchemaSetting = "Database:Schema";

    /// <summary>
    /// Runs the schema step for <typeparamref name="TDbContext"/>. Returns true when the
    /// process was started only to migrate and should now exit instead of serving requests.
    /// </summary>
    /// <param name="app">The built application</param>
    /// <param name="args">Command line arguments</param>
    /// <param name="seed">Optional data seeding, run after migrating</param>
    public static async Task<bool> PrepareDatabaseAsync<TDbContext>(
        this WebApplication app,
        string[] args,
        Func<IServiceProvider, Task>? seed = null)
        where TDbContext : DbContext
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseStartup));
        var migrateOnly = args.Contains(MigrateArgument, StringComparer.OrdinalIgnoreCase);
        var mode = app.Configuration.GetValue<SchemaStartup?>(SchemaSetting)
            ?? (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing") ? SchemaStartup.Migrate : SchemaStartup.Verify);

        if (!migrateOnly && mode == SchemaStartup.None)
        {
            logger.LogWarning("Database schema step skipped ({Setting}=None); this host must not serve traffic", SchemaSetting);
            return false;
        }

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        if (migrateOnly || mode == SchemaStartup.Migrate)
        {
            var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
            await context.Database.MigrateAsync();
            logger.LogInformation("Database {Database}: applied {Count} migration(s) {Migrations}",
                context.Database.GetDbConnection().Database, pending.Count, pending);

            if (seed is not null)
            {
                await seed(scope.ServiceProvider);
            }

            return migrateOnly;
        }

        var missing = (await context.Database.GetPendingMigrationsAsync()).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Database {context.Database.GetDbConnection().Database} is behind the code by {missing.Count} migration(s): " +
                $"{string.Join(", ", missing)}. Run the service with {MigrateArgument} (or set {SchemaSetting}=Migrate) before starting it.");
        }

        return false;
    }
}
