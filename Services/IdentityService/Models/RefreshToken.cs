namespace Store.IdentityService.Models;

/// <summary>
/// One refresh token of a session. The token itself never touches the database: the client
/// holds it (in an httpOnly cookie) and the row keeps its SHA-256 hash. Each use replaces the
/// token with a new one of the same family; a token presented after it was replaced means
/// somebody else holds a copy, and the whole family is revoked.
/// </summary>
public class RefreshToken
{
    public long Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    /// <summary>SHA-256 of the token, base64url; the only thing stored.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>The session: every rotation stays in the family, logout and reuse revoke it whole.</summary>
    public Guid FamilyId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    /// <summary>Address the token was issued to, for the audit trail only.</summary>
    public string? CreatedByIp { get; set; }

    /// <summary>Set when the token was rotated, revoked or its family compromised.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Hash of the token that took this one's place after a rotation.</summary>
    public string? ReplacedByHash { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
