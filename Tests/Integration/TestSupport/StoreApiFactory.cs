using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Store.Tests.Integration.TestSupport;

/// <summary>
/// Hosts one service in-process against a real PostgreSQL database (migrated by the service's
/// own startup code) with the HTTP boundaries to other services replaced by fakes and the
/// message bus running on MassTransit's in-memory transport (outbox, inbox and consumers are
/// the real ones; published events are observable through <see cref="Bus"/>).
/// <typeparamref name="TMarker"/> is any type from the service assembly; WebApplicationFactory uses it only to locate the entry point, which avoids six
/// ambiguous "Program" classes.
/// </summary>
public abstract class StoreApiFactory<TMarker> : WebApplicationFactory<TMarker>, IAsyncLifetime
    where TMarker : class
{
    private readonly PostgresFixture _postgres;
    private bool _hostStarted;
    private int _stopping;
    private string _connectionString = string.Empty;
    private readonly List<Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>> _probes = [];
    private readonly string _busPrefix = $"test-{Guid.NewGuid():N}-";

    protected StoreApiFactory(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    /// <summary>Database name inside the shared container, unique per service; null for services without a database.</summary>
    protected abstract string? DatabaseName { get; }

    /// <summary>
    /// The bus of the service under test: publish events into it, observe what it published
    /// and what its consumers handled. Only for services that register messaging.
    /// </summary>
    public ITestHarness Bus => Services.GetRequiredService<ITestHarness>();

    public async Task InitializeAsync()
    {
        if (DatabaseName is not null)
        {
            _connectionString = await _postgres.GetConnectionStringAsync(DatabaseName);
        }
        // Forces the host to build (and the service to migrate its database) before the first test
        _ = Server;
        _hostStarted = true;
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    public override async ValueTask DisposeAsync()
    {
        await StopBusAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) StopBusAsync().GetAwaiter().GetResult();
        base.Dispose(disposing);
    }

    private async Task StopBusAsync()
    {
        if (!_hostStarted || Interlocked.Exchange(ref _stopping, 1) != 0) return;
        // Drain consumers before application cancellation or provider disposal can
        // invalidate their scoped inbox/outbox work. Cover both disposal entry points.
        if (Services.GetService<IBusControl>() is { } bus) await bus.StopAsync();
        Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

    }

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
        // The demo accounts and their password-less logins, which several tests sign in with
        builder.UseSetting("Demo:Enabled", "true");
        // Validated on start but unused: the harness swaps RabbitMQ for the in-memory transport
        builder.UseSetting("RabbitMQ:Host", "broker.test");
        builder.UseSetting("RabbitMQ:Username", "tests");
        builder.UseSetting("RabbitMQ:Password", "tests");
        builder.UseSetting("Logging:LogLevel:Default", "Warning");

        ConfigureSettings(builder);

        builder.ConfigureTestServices(services =>
        {
            // Hosts coexist in one process; MassTransit's log context can outlive a
            // host's Windows EventLog provider. Keep test diagnostics on the console.
            services.AddLogging(logging => { logging.ClearProviders(); logging.AddConsole(); });
            // Keeps the service's consumers and outbox, replaces the transport with in-memory
            if (services.Any(d => d.ServiceType == typeof(IBus)))
            {
                services.AddMassTransitTestHarness(bus =>
                {
                    // Factories share databases but not in-memory endpoint addresses.
                    // A later host must never route to a disposed predecessor's probe.
                    var formatter = new KebabCaseEndpointNameFormatter(_busPrefix, includeNamespace: false);
                    bus.SetEndpointNameFormatter(formatter);
                    AddProbe<AuditEventProbe>(bus);
                    ConfigureTestBus(bus);
                    bus.UsingInMemory((context, cfg) =>
                    {
                        foreach (var configure in _probes) configure(context, cfg);
                        cfg.ConfigureEndpoints(context, formatter);
                    });
                });
            }

            ConfigureTestServices(services);
        });
    }

    /// <summary>
    /// Extra consumers on the test bus, typically probes for the events the service publishes from
    /// its own consumers (those are observable only as consumed, see <see cref="AuditEventProbe"/>).
    /// </summary>
    protected virtual void ConfigureTestBus(IBusRegistrationConfigurator bus)
    {
    }

    /// <summary>Observers have no durable business state; only production consumers use the service's EF inbox.</summary>
    protected void AddProbe<T>(IBusRegistrationConfigurator bus) where T : class, IConsumer
    {
        bus.AddConsumer<T>().ExcludeFromConfigureEndpoints();
        var name = _busPrefix + KebabCaseEndpointNameFormatter.Instance.Consumer<T>();
        _probes.Add((context, cfg) => cfg.ReceiveEndpoint(name, endpoint => endpoint.ConfigureConsumer<T>(context)));
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
