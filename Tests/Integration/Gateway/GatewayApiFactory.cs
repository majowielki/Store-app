using Microsoft.AspNetCore.Hosting;
using Store.GatewayService.HealthChecks;
using Store.Tests.Integration.TestSupport;

namespace Store.Tests.Integration.Gateway;

/// <summary>
/// The gateway in-process: routes, authorization policies and rate limiting are real; the
/// downstream services are not running, so a proxied request ends in 502 - which proves the
/// request passed the gateway's own checks.
/// </summary>
public sealed class GatewayApiFactory : StoreApiFactory<RabbitMQHealthCheck>
{
    public const int CredentialPermitLimit = 5;

    public GatewayApiFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    protected override string? DatabaseName => null;

    protected override void ConfigureSettings(IWebHostBuilder builder)
    {
        // Small, deterministic limits for the rate-limiting tests
        builder.UseSetting("RateLimiting:Auth:WindowSeconds", "60");
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "50");
        builder.UseSetting("RateLimiting:Auth:CredentialPermitLimit", CredentialPermitLimit.ToString());
        // YARP probes the (absent) downstream health endpoints every 30 s - keep test output quiet
        builder.UseSetting("Logging:LogLevel:Yarp", "None");
    }
}
