using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Authentication;
using Store.BuildingBlocks.Configuration;

namespace Store.BuildingBlocks.Http;

/// <summary>
/// Typed HTTP clients for calling another store service. Every such client gets its base
/// address from <see cref="ServiceEndpointsOptions"/> (no addresses in code), the shared
/// internal API key on each request, and the standard resilience pipeline: a timeout per
/// attempt, retries with exponential backoff and jitter on transient failures, and a circuit
/// breaker so a dependency that is down stops consuming request threads.
/// </summary>
public static class ServiceClientExtensions
{
    /// <summary>Longest a single attempt may take before it is retried.</summary>
    public static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Longest a call may take including every retry.</summary>
    public static readonly TimeSpan TotalTimeout = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Registers <typeparamref name="TClient"/> as a typed client for the service named
    /// <paramref name="serviceName"/> (a property of <see cref="ServiceEndpointsOptions"/>,
    /// e.g. <c>nameof(ServiceEndpointsOptions.ProductService)</c>). Declare the address as
    /// required with <see cref="ServiceEndpointsRegistration.AddServiceEndpoints"/>.
    /// </summary>
    public static IHttpClientBuilder AddServiceClient<TClient, TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddInternalApiKeyClient(configuration);

        var httpClientBuilder = services.AddHttpClient<TClient, TImplementation>((provider, client) =>
            {
                client.BaseAddress = provider.GetRequiredService<IOptions<ServiceEndpointsOptions>>().Value.Require(serviceName);
                // The resilience pipeline owns the timeouts; the client's own must not cut them short
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddHttpMessageHandler<InternalApiKeyMessageHandler>();

        httpClientBuilder.AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = AttemptTimeout;
                options.TotalRequestTimeout.Timeout = TotalTimeout;
                options.Retry.MaxRetryAttempts = 3;
                // The breaker needs to observe at least two attempt timeouts per sampling window
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            });

        return httpClientBuilder;
    }
}
