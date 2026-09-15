using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Store.Shared.MessageBus;
using Store.Shared.Services;
using Xunit;

namespace Store.Tests.Integration.TestSupport;

/// <summary>
/// Hosts one service in-process against a real PostgreSQL database (migrated by the service's
/// own startup code) with the network boundaries to other services replaced by recording fakes.
/// <typeparamref name="TMarker"/> is any type from the service assembly; WebApplicationFactory
/// uses it only to locate the entry point, which avoids six ambiguous "Program" classes.
/// </summary>
public abstract class StoreApiFactory<TMarker> : WebApplicationFactory<TMarker>, IAsyncLifetime
    where TMarker : class
{
    private readonly PostgresFixture _postgres;
    private string _connectionString = string.Empty;

    protected StoreApiFactory(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    /// <summary>Database name inside the shared container, unique per service; null for services without a database.</summary>
    protected abstract string? DatabaseName { get; }

    /// <summary>What the service under test tried to send to AuditLogService.</summary>
    public RecordingAuditLogClient AuditLog { get; } = new();

    /// <summary>What the service under test published to the message bus.</summary>
    public RecordingMessageBus MessageBus { get; } = new();

    public async Task InitializeAsync()
    {
        if (DatabaseName is not null)
        {
            _connectionString = await _postgres.GetConnectionStringAsync(DatabaseName);
        }
        // Forces the host to build (and the service to migrate its database) before the first test
        _ = Server;
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        if (DatabaseName is not null)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        }
        builder.UseSetting("JwtSettings:SecretKey", TestTokens.SigningKey);
        builder.UseSetting("JwtSettings:Issuer", TestTokens.Issuer);
        builder.UseSetting("JwtSettings:Audience", TestTokens.Audience);
        builder.UseSetting("InternalApi:ApiKey", TestTokens.InternalApiKey);
        builder.UseSetting("TrueAdmin:Password", TestUsers.TrueAdminPassword);
        builder.UseSetting("Logging:LogLevel:Default", "Warning");

        ConfigureSettings(builder);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuditLogClient>();
            services.AddSingleton<IAuditLogClient>(AuditLog);

            // No broker in tests: swap the bus and drop the background subscriber that connects to it
            services.RemoveAll<IMessageBus>();
            services.AddSingleton<IMessageBus>(MessageBus);
            services.RemoveAll<IMessageBusConnection>();
            var subscriber = services.FirstOrDefault(d =>
                d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(MessageBusSubscriptionService));
            if (subscriber is not null)
            {
                services.Remove(subscriber);
            }

            ConfigureTestServices(services);
        });
    }

    /// <summary>Extra configuration values for a specific service.</summary>
    protected virtual void ConfigureSettings(IWebHostBuilder builder)
    {
    }

    /// <summary>Extra service replacements for a specific service.</summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }
}

/// <summary>Accounts IdentityService seeds at startup, usable in login tests.</summary>
public static class TestUsers
{
    public const string TrueAdminEmail = "trueadmin@store.com";
    public const string TrueAdminPassword = "TrueAdmin-Test-Password-1!";
}
