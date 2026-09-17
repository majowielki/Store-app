using System.ComponentModel.DataAnnotations;

namespace Store.GatewayService.RateLimiting;

/// <summary>
/// Limits for the proxied routes, per signed-in user (or per client address for anonymous
/// requests). Bound from <c>RateLimiting:Routes</c>. Attached in appsettings.json through
/// <c>"RateLimiterPolicy": "api"</c> (shop routes) and <c>"admin"</c> (admin panel routes).
/// </summary>
public sealed class RouteRateLimitOptions
{
    public const string SectionName = "RateLimiting:Routes";

    public const string ApiPolicyName = "api";
    public const string AdminPolicyName = "admin";

    [Range(1, 3600)]
    public int WindowSeconds { get; init; } = 60;

    /// <summary>Requests per window per client on the catalogue, cart and order routes.</summary>
    [Range(1, 100_000)]
    public int ApiPermitLimit { get; init; } = 300;

    /// <summary>Requests per window per client on the admin routes.</summary>
    [Range(1, 100_000)]
    public int AdminPermitLimit { get; init; } = 100;
}
