using Microsoft.Extensions.DependencyInjection;
using Store.Contracts.Authorization;
using System.Security.Claims;

namespace Store.BuildingBlocks.Authorization;

/// <summary>
/// Registration of the store-wide authorization policies and helpers for checking roles
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Registers the <see cref="Policies"/> every service and the gateway share. Policies are
    /// role based (<see cref="Roles"/>) and evaluate the role claim the JWT bearer handler maps
    /// to <see cref="ClaimTypes.Role"/>.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddStoreAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.User, policy => policy.RequireRole(Roles.All))
            .AddPolicy(Policies.Admin, policy => policy.RequireRole(Roles.Admins))
            .AddPolicy(Policies.AdminWrite, policy => policy.RequireRole(Roles.TrueAdmin));

        return services;
    }

    /// <summary>True when the principal holds any admin role (true-admin or demo-admin).</summary>
    public static bool IsStoreAdmin(this ClaimsPrincipal principal)
        => Roles.Admins.Any(principal.IsInRole);

    /// <summary>True when the principal is the read-only demo administrator.</summary>
    public static bool IsDemoAdmin(this ClaimsPrincipal principal)
        => principal.IsInRole(Roles.DemoAdmin);

    /// <summary>
    /// Id of the signed-in user. Every token the identity service issues carries it, so a
    /// principal without one passed authentication with a token this store never issued;
    /// the request ends as 401.
    /// </summary>
    public static string GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(userId)
            ? throw new UnauthorizedAccessException("The token carries no user id")
            : userId;
    }
}
