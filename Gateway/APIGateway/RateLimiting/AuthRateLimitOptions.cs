using System.ComponentModel.DataAnnotations;

namespace Store.GatewayService.RateLimiting;

/// <summary>
/// Limits applied per client address to the identity route. Bound from
/// <c>RateLimiting:Auth</c>. Credential endpoints (login, register, demo logins) get the stricter
/// <see cref="CredentialPermitLimit"/>; every other /api/v1/auth/* call (refresh, profile, logout) the general one.
/// </summary>
public sealed class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting:Auth";

    /// <summary>Name of the YARP rate limiter policy attached to the identity route.</summary>
    public const string PolicyName = "auth";

    /// <summary>
    /// Endpoints that accept credentials - brute-force targets. The refresh endpoint is not one
    /// of them: a refresh token is 256 random bits (guessing is hopeless, reuse is detected), and
    /// the UI calls it on every page load, so it only gets the general per-client limit.
    /// </summary>
    public static readonly string[] CredentialPaths =
    {
        "/api/v1/auth/login",
        "/api/v1/auth/register",
        "/api/v1/auth/demo-login",
        "/api/v1/auth/demo-admin-login"
    };

    [Range(1, 3600)]
    public int WindowSeconds { get; init; } = 60;

    /// <summary>Requests per window per client for /api/v1/auth/* in general (profile, logout, ...).</summary>
    [Range(1, 100_000)]
    public int PermitLimit { get; init; } = 60;

    /// <summary>Requests per window per client for <see cref="CredentialPaths"/>.</summary>
    [Range(1, 100_000)]
    public int CredentialPermitLimit { get; init; } = 10;
}
