using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Store.BuildingBlocks.Configuration;
using Store.IdentityService.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Store.IdentityService.Services;

/// <summary>Creates the tokens of a session: a short-lived JWT and an opaque refresh token.</summary>
public interface ITokenService
{
    /// <summary>An HS256 access token with the user's identity and roles, and when it expires.</summary>
    (string Token, DateTime ExpiresAt) CreateAccessToken(ApplicationUser user, IEnumerable<string> roles);

    /// <summary>A new random refresh token and the hash the database keeps for it.</summary>
    (string Token, string Hash) CreateRefreshToken();

    /// <summary>The hash a presented refresh token is looked up by.</summary>
    string HashRefreshToken(string token);

    /// <summary>How long a refresh token issued now stays valid.</summary>
    TimeSpan RefreshTokenLifetime { get; }
}

public sealed class TokenService : ITokenService
{
    private const int RefreshTokenBytes = 32; // 256 bits of entropy

    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly JsonWebTokenHandler _handler = new();

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    public (string Token, DateTime ExpiresAt) CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new("firstName", user.FirstName ?? string.Empty),
            new("lastName", user.LastName ?? string.Empty),
            new("displayName", user.DisplayName)
        };
        // One "role" claim per role, the way the bearer handlers map it to ClaimTypes.Role
        claims.AddRange(roles.Distinct().Select(role => new Claim("role", role)));

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            NotBefore = now,
            IssuedAt = now,
            Expires = expiresAt,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = _signingCredentials
        });

        return (token, expiresAt);
    }

    public (string Token, string Hash) CreateRefreshToken()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        return (token, HashRefreshToken(token));
    }

    public string HashRefreshToken(string token)
        => Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
