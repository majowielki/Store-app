using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Store.BuildingBlocks.Configuration;

namespace Store.BuildingBlocks.Authentication;

/// <summary>
/// Registration of the shared internal API key
/// </summary>
public static class InternalApiExtensions
{
    /// <summary>
    /// Registers the validated <see cref="InternalApiOptions"/> and the
    /// <see cref="InternalApiKeyMessageHandler"/> for services that call internal endpoints.
    /// Attach the handler with <c>.AddHttpMessageHandler&lt;InternalApiKeyMessageHandler&gt;()</c>.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddInternalApiKeyClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStoreOptions<InternalApiOptions>(configuration, InternalApiOptions.SectionName);
        services.AddTransient<InternalApiKeyMessageHandler>();
        return services;
    }

    /// <summary>
    /// Registers the <see cref="InternalApiKeyDefaults.AuthenticationScheme"/> scheme and the
    /// <see cref="InternalApiKeyDefaults.PolicyName"/> policy for services that expose internal
    /// endpoints. Must be called after the default (JWT) authentication is registered.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddInternalApiKeyAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStoreOptions<InternalApiOptions>(configuration, InternalApiOptions.SectionName);

        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, InternalApiKeyAuthenticationHandler>(
                InternalApiKeyDefaults.AuthenticationScheme, displayName: null, configureOptions: null);

        services.AddAuthorizationBuilder()
            .AddPolicy(InternalApiKeyDefaults.PolicyName, policy => policy
                .AddAuthenticationSchemes(InternalApiKeyDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireClaim(System.Security.Claims.ClaimTypes.Name, InternalApiKeyDefaults.ServiceIdentityName));

        return services;
    }
}
