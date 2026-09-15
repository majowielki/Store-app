using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Store.Tests.Integration.TestSupport;

/// <summary>
/// One PostgreSQL container for the whole test run (started lazily, disposed at the end).
/// Every service gets its own database inside it, exactly like production
/// (database-per-service), so migrations of one service never collide with another's.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("store_test")
        .WithUsername("store_test")
        .WithPassword("store_test")
        .WithCleanUp(true)
        .Build();

    private readonly HashSet<string> _databases = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _gate = new(1, 1);

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>
    /// Connection string to a database dedicated to <paramref name="databaseName"/>, created on
    /// first use. Each factory asks for its own name (for example "store_identity_test").
    /// </summary>
    public async Task<string> GetConnectionStringAsync(string databaseName)
    {
        await _gate.WaitAsync();
        try
        {
            if (_databases.Add(databaseName))
            {
                await using var connection = new NpgsqlConnection(_container.GetConnectionString());
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await command.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            _gate.Release();
        }

        return new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = databaseName
        }.ConnectionString;
    }
}

/// <summary>
/// All integration test classes share the container through this collection.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgresTests : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
