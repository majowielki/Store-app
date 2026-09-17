using Microsoft.Extensions.DependencyInjection;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Store.BuildingBlocks.Api;

public static class ApiConventionsExtensions
{
    /// <summary>
    /// Registers controllers with the conventions every service shares: camelCase JSON with
    /// nulls omitted and enums as their names ("sofas", "modenza"), problem responses for
    /// errors, request models validated by their FluentValidation validators before the
    /// action runs (the service registers the validators themselves).
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>MVC builder to add filters to</returns>
    public static IMvcBuilder AddStandardApiControllers(this IServiceCollection services)
    {
        services.AddStoreProblemDetails();
        services.AddFluentValidationAutoValidation(options =>
            options.OverrideDefaultResultFactoryWith<UnprocessableEntityResultFactory>());

        return services.AddControllers()
            .AddJsonOptions(options => ConfigureStoreJson(options.JsonSerializerOptions));
    }

    /// <summary>Applies the shared JSON conventions to any <see cref="JsonSerializerOptions"/>.</summary>
    public static void ConfigureStoreJson(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.WriteIndented = false;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    /// <summary>
    /// CORS policy for the services. They sit behind the gateway, which is the only public
    /// entry point, so the policy is permissive here and restrictive at the gateway.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="policyName">CORS policy name</param>
    /// <param name="allowedOrigins">Allowed origins; any origin when empty</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddStandardCors(this IServiceCollection services, string policyName = "DefaultCorsPolicy", string[]? allowedOrigins = null)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(policyName, builder =>
            {
                if (allowedOrigins is { Length: > 0 })
                {
                    builder.WithOrigins(allowedOrigins).AllowCredentials();
                }
                else
                {
                    builder.AllowAnyOrigin();
                }

                builder.AllowAnyMethod().AllowAnyHeader();
            });
        });

        return services;
    }
}
