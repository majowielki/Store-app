using MassTransit.Logging;
using MassTransit.Monitoring;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Store.BuildingBlocks.Observability;

/// <summary>
/// OpenTelemetry for every host: one trace per request across the gateway, the services, the
/// database and the bus (W3C traceparent travels with HttpClient and with MassTransit on its
/// own), runtime and HTTP metrics, the store's own business metrics (<see cref="StoreMetrics"/>)
/// and structured logs that carry the trace id. Everything leaves through OTLP to whatever
/// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> names - the Aspire dashboard in compose, the Container Apps
/// environment's managed agent (which forwards to Azure Monitor) in the cloud. Without the
/// variable nothing is exported and nothing is lost but the export.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>The environment variable the OTLP exporter reads; set = export on.</summary>
    public const string EndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    public static WebApplicationBuilder AddStoreObservability(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
        });

        builder.Services.TryAddSingleton<StoreMetrics>();

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName,
                serviceNamespace: "store",
                serviceVersion: typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString(),
                serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing => tracing
                .AddProcessor(new BackgroundDatabaseSpanFilter())
                .AddAspNetCoreInstrumentation(options =>
                {
                    // Probes would drown everything else
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
                    // The user behind the request, never the token
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        var userId = request.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        if (userId is not null) activity.SetTag("enduser.id", userId);
                    };
                })
                .AddHttpClientInstrumentation()
                // Npgsql records the statement text (parameters stay out of it)
                .AddNpgsql()
                .AddSource(DiagnosticHeaders.DefaultListenerName))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(InstrumentationOptions.MeterName)
                .AddMeter(StoreMetrics.MeterName));

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EndpointVariable)))
        {
            // Traces, metrics and logs alike; endpoint, protocol and headers come from the OTEL_* variables
            telemetry.UseOtlpExporter();
        }

        return builder;
    }
}
