using Microsoft.Extensions.Options;

namespace Store.GatewayService.Cors;

public static class CorsExtensions
{
    private const string PolicyName = "gateway";

    /// <summary>One policy with the configured origins; the services themselves have no CORS - only the gateway is public.</summary>
    public static IServiceCollection AddStoreCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()?.AllowedOrigins ?? [];
        services.AddCors(options => options.AddPolicy(PolicyName, policy =>
        {
            if (origins.Length > 0)
            {
                // Credentials, so the refresh cookie and the bearer header may travel cross-origin
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            }
        }));
        return services;
    }

    /// <summary>Adds the CORS middleware only when there is an origin to allow.</summary>
    public static IApplicationBuilder UseStoreCors(this IApplicationBuilder app)
    {
        var origins = app.ApplicationServices.GetRequiredService<IOptions<CorsOptions>>().Value.AllowedOrigins;
        return origins.Length > 0 ? app.UseCors(PolicyName) : app;
    }
}
