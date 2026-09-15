using System.ComponentModel.DataAnnotations;

namespace Store.GatewayService.RateLimiting;

/// <summary>
/// Limits applied per client address to the identity route (SEC-06). Bound from
/// <c>RateLimiting:Auth</c>. Credential endpoints (login, register, refresh, demo logins) get the
/// stricter <see cref="CredentialPermitLimit"/>; every other /api/auth/* call the general one.
/// </summary>
public sealed class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting:Auth";

    /// <summary>Name of the YARP rate limiter policy attached to the identity route.</summary>
    public const string PolicyName = "auth";

    /// <summary>Endpoints that accept or renew credentials - brute-force targets.</summary>
    public static readonly string[] CredentialPaths =
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/auth/demo-login",
        "/api/auth/demo-admin-login"
    };

    [Range(1, 3600)]
    public int WindowSeconds { get; init; } = 60;

    /// <summary>Requests per window per client for /api/auth/* in general (profile, logout, ...).</summary>
    [Range(1, 100_000)]
    public int PermitLimit { get; init; } = 60;

    /// <summary>Requests per window per client for <see cref="CredentialPaths"/>.</summary>
    [Range(1, 100_000)]
    public int CredentialPermitLimit { get; init; } = 10;
}
