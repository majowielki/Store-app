using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace Store.Shared.Extensions;

/// <summary>
/// Service configuration extensions
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds JWT authentication with standard configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? 
            throw new InvalidOperationException("JWT SecretKey is not configured");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    /// <summary>
    /// Adds Swagger with JWT bearer authentication
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="version">API version</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services, string serviceName, string version = "v1")
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version, new OpenApiInfo
            {
                Title = serviceName,
                Version = version,
                Description = $"{serviceName} API documentation"
            });

            // Add JWT Bearer authentication
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

            // Include XML comments if available
            var xmlFile = $"{serviceName}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    /// <summary>
    /// Adds CORS with standard policy
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="policyName">CORS policy name</param>
    /// <param name="allowedOrigins">Allowed origins (optional)</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddStandardCors(this IServiceCollection services, string policyName = "DefaultCorsPolicy", string[]? allowedOrigins = null)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(policyName, builder =>
            {
                if (allowedOrigins?.Any() == true)
                {
                    builder.WithOrigins(allowedOrigins);
                }
                else
                {
                    builder.AllowAnyOrigin();
                }

                builder.AllowAnyMethod()
                       .AllowAnyHeader();

                if (allowedOrigins?.Any() == true)
                {
                    builder.AllowCredentials();
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Adds standard API controllers with JSON options
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddStandardApiControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            // Add global filters if needed
            // options.Filters.Add<GlobalActionFilter>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.WriteIndented = false;
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

        return services;
    }
}
