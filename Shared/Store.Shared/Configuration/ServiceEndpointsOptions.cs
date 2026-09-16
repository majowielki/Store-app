using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace Store.Shared.Configuration;

/// <summary>
/// Base addresses of the other services, bound from the <c>Services</c> section - the one and
/// only key convention (<c>Services:ProductService</c> / <c>Services__ProductService</c>).
/// Each service declares which endpoints it needs; missing ones stop the host at startup
/// instead of silently falling back to localhost.
/// </summary>
public sealed class ServiceEndpointsOptions
{
    public const string SectionName = "Services";

    [Url] public string? IdentityService { get; init; }
    [Url] public string? ProductService { get; init; }
    [Url] public string? CartService { get; init; }
    [Url] public string? OrderService { get; init; }
    [Url] public string? AuditLogService { get; init; }

    /// <summary>Configured value for a service name, e.g. <c>nameof(ProductService)</c>.</summary>
    public string? Get(string serviceName) => serviceName switch
    {
        nameof(IdentityService) => IdentityService,
        nameof(ProductService) => ProductService,
        nameof(CartService) => CartService,
        nameof(OrderService) => OrderService,
        nameof(AuditLogService) => AuditLogService,
        _ => throw new ArgumentOutOfRangeException(nameof(serviceName), serviceName, "unknown service")
    };

    /// <summary>Base address of a service that this host declared as required.</summary>
    public Uri Require(string serviceName)
    {
        var value = Get(serviceName);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{SectionName}:{serviceName} is not configured")
            : new Uri(value.TrimEnd('/') + "/", UriKind.Absolute);
    }
}

public static class ServiceEndpointsRegistration
{
    /// <summary>
    /// Binds <see cref="ServiceEndpointsOptions"/> and fails startup unless every service in
    /// <paramref name="requiredServices"/> has an absolute URL configured.
    /// </summary>
    public static IServiceCollection AddServiceEndpoints(
        this IServiceCollection services,
        IConfiguration configuration,
        params string[] requiredServices)
    {
        services.AddOptions<ServiceEndpointsOptions>()
            .Bind(configuration.GetSection(ServiceEndpointsOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => requiredServices.All(name => !string.IsNullOrWhiteSpace(options.Get(name))),
                $"{ServiceEndpointsOptions.SectionName}: the following addresses are required: {string.Join(", ", requiredServices)}")
            .ValidateOnStart();

        return services;
    }
}
