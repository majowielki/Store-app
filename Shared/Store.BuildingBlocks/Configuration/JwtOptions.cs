using System.ComponentModel.DataAnnotations;

namespace Store.BuildingBlocks.Configuration;

/// <summary>
/// JWT settings shared by the gateway and every service. Bound from the <c>JwtSettings</c>
/// section and validated when the host starts, so a missing or too short signing key stops
/// the application instead of silently falling back to a well-known value.
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

    [Range(1, 1440)]
    public int ExpirationInMinutes { get; init; } = 60;
}
