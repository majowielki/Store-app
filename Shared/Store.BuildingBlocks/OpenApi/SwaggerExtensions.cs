using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Store.BuildingBlocks.OpenApi;

public static class SwaggerExtensions
{
    /// <summary>
    /// Adds Swagger with the JWT bearer security scheme, so every service documents itself the
    /// same way and the "Authorize" button works in each Swagger UI.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="serviceName">Title of the document</param>
    /// <param name="version">API version</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services, string serviceName, string version = "v1")
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version, new OpenApiInfo
            {
                Title = serviceName,
                Version = version,
                Description = $"{serviceName} API documentation"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFile = $"{serviceName}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }
}
