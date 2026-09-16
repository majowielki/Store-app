using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Store.BuildingBlocks.Configuration;

/// <summary>
/// Options registration helpers
/// </summary>
public static class OptionsRegistration
{
    /// <summary>
    /// Binds <typeparamref name="TOptions"/> to a configuration section and validates its
    /// data annotations when the host starts, so a configuration error surfaces at startup
    /// instead of at the first request.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="sectionName">Configuration section to bind</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddStoreOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName) where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
