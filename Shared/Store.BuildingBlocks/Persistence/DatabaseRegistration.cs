using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Store.BuildingBlocks.Persistence;

/// <summary>
/// The service's database: one connection string (<c>ConnectionStrings:DefaultConnection</c>)
/// for the context and for the readiness check, with the settings every host shares applied
/// in one place.
/// </summary>
public static class DatabaseRegistration
{
    public const string ConnectionName = "DefaultConnection";

    /// <summary>Registers <typeparamref name="TDbContext"/> on PostgreSQL.</summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration holding the connection string</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddStoreDbContext<TDbContext>(this IServiceCollection services, IConfiguration configuration)
        where TDbContext : DbContext
        => services.AddDbContext<TDbContext>(options => options.UseNpgsql(configuration.GetStoreConnectionString()));

    /// <summary>
    /// The connection string of the service's database. GSS encryption negotiation is switched
    /// off: Npgsql asks the server for it by default, which loads a Kerberos library the
    /// chiseled image does not carry (two "cannot load libgssapi_krb5" lines at every start
    /// before the fallback), and nothing in the store authenticates with Kerberos.
    /// </summary>
    /// <param name="configuration">Configuration holding the connection string</param>
    /// <returns>The connection string with the shared settings applied</returns>
    public static string GetStoreConnectionString(this IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{ConnectionName} is not configured");

        return new NpgsqlConnectionStringBuilder(configured) { GssEncryptionMode = GssEncryptionMode.Disable }.ConnectionString;
    }
}
