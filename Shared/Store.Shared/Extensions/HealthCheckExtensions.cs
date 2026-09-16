using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Store.Shared.Serialization;
using System.Text.Json;

namespace Store.Shared.Extensions;

/// <summary>
/// Health endpoints every service exposes the same way:
/// <list type="bullet">
/// <item><c>/health/live</c> - the process answers; never looks at dependencies</item>
/// <item><c>/health/ready</c> - checks tagged <see cref="ReadyTag"/> (the database); 503 when any is Unhealthy</item>
/// <item><c>/health</c> - every check with details, for humans and dashboards</item>
/// </list>
/// Orchestrators (compose, YARP active checks, Container Apps probes) use <c>/health/ready</c>.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>Tag for checks that gate readiness.</summary>
    public const string ReadyTag = "ready";

    /// <summary>
    /// Registers the self check and the PostgreSQL check. A database the service cannot reach
    /// makes it Unhealthy - the service cannot serve requests without it.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">Database connection string</param>
    /// <returns>Health checks builder to attach more checks to</returns>
    public static IHealthChecksBuilder AddStoreHealthChecks(this IServiceCollection services, string connectionString)
    {
        return services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("API is running"))
            .AddNpgSql(connectionString, name: "database", failureStatus: HealthStatus.Unhealthy, tags: new[] { ReadyTag });
    }

    /// <summary>
    /// Registers only the self check, for hosts without a database of their own (the gateway).
    /// </summary>
    public static IHealthChecksBuilder AddStoreHealthChecks(this IServiceCollection services)
    {
        return services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("API is running"));
    }

    /// <summary>
    /// Maps <c>/health</c>, <c>/health/live</c> and <c>/health/ready</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapStoreHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedReport
        });

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = (context, _) => context.Response.WriteAsync("Healthy")
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
            ResponseWriter = WriteSummary
        });

        return endpoints;
    }

    private static Task WriteDetailedReport(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration,
                tags = entry.Value.Tags
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, StoreJson.CamelCaseIndented));
    }

    private static Task WriteSummary(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString())
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, StoreJson.CamelCase));
    }
}
