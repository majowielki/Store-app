using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Authentication;
using Store.BuildingBlocks.Configuration;
using Store.Shared.Services;

namespace Store.Shared.Extensions;

public static class AuditLogClientExtensions
{
    /// <summary>
    /// Registers the typed client that sends audit entries to AuditLogService. The base address
    /// comes from <c>Services:AuditLogService</c> (declare it as required with
    /// <see cref="ServiceEndpointsRegistration.AddServiceEndpoints"/>) and every call carries the
    /// shared service key the internal endpoint demands.
    /// </summary>
    public static IServiceCollection AddAuditLogClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInternalApiKeyClient(configuration);

        services.AddHttpClient<IAuditLogClient, AuditLogClient>((provider, client) =>
        {
            client.BaseAddress = provider.GetRequiredService<IOptions<ServiceEndpointsOptions>>().Value
                .Require(nameof(ServiceEndpointsOptions.AuditLogService));
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<InternalApiKeyMessageHandler>();

        return services;
    }
}
