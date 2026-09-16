using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Store.BuildingBlocks.Configuration;
using System.Text;

namespace Store.BuildingBlocks.Authentication;

public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Adds JWT bearer authentication configured from the validated <see cref="JwtOptions"/>.
    /// The signing key, issuer and audience have no fallbacks: when <c>JwtSettings</c> is
    /// missing or invalid the host fails to start.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="configure">Optional per-service customisation applied after the shared defaults (events, clock skew, claim types)</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtBearerOptions>? configure = null)
    {
        services.AddStoreOptions<JwtOptions>(configuration, JwtOptions.SectionName);

        // Set the authenticate/challenge schemes explicitly: AddIdentity (IdentityService)
        // registers cookie schemes as defaults and DefaultScheme alone would not override them.
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwt) =>
            {
                var settings = jwt.Value;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers["Token-Expired"] = "true";
                        }
                        return Task.CompletedTask;
                    }
                };

                configure?.Invoke(options);
            });

        return services;
    }
}
