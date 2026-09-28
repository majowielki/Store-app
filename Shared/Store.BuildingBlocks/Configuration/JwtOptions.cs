using System.ComponentModel.DataAnnotations;

namespace Store.BuildingBlocks.Configuration;

/// <summary>
/// JWT settings shared by the gateway and every service. Bound from the <c>JwtSettings</c>
/// section and validated when the host starts, so a missing or too short signing key stops
/// the application instead of silently falling back to a well-known value. The lifetimes
/// matter to the identity service only, which issues the tokens; the others validate them.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// HMAC-SHA256 signing key. Never commit it: use <c>dotnet user-secrets</c> locally
    /// (see <c>Scripts/Set-Local-Secrets.ps1</c>) and the <c>JwtSettings__SecretKey</c>
    /// environment variable in containers.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(32)]
    public string SecretKey { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Lifetime of an access token. Short on purpose: a leaked token is usable only until it
    /// expires, and the client renews it with the refresh token.
    /// </summary>
    [Range(1, 1440)]
    public int AccessTokenMinutes { get; init; } = 15;

    /// <summary>How long a session lasts without signing in again.</summary>
    [Range(1, 365)]
    public int RefreshTokenDays { get; init; } = 14;

    /// <summary>
    /// How long after a refresh token was rotated it may be presented once more without ending the
    /// session (the "reuse interval" of Auth0 and Okta). A browser that leaves a page while its
    /// refresh is on the way never stores the new token and presents the old one again; within this
    /// window that is taken for such a race and answered with a fresh token, after it for a stolen
    /// copy. 0 turns the tolerance off.
    /// </summary>
    [Range(0, 300)]
    public int RefreshTokenReuseSeconds { get; init; } = 30;

    public TimeSpan RefreshTokenReuseWindow => TimeSpan.FromSeconds(RefreshTokenReuseSeconds);
}
