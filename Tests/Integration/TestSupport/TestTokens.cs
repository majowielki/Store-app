using Microsoft.IdentityModel.Tokens;
using Store.Contracts.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace Store.Tests.Integration.TestSupport;

/// <summary>
/// Issues HS256 tokens the way IdentityService does (claims "sub"/"email"/"role"), signed with
/// the key the factories put into JwtSettings, so any service under test accepts them.
/// </summary>
public static class TestTokens
{
    public const string SigningKey = "integration-tests-signing-key-with-at-least-32-characters";
    public const string Issuer = "Store.API";
    public const string Audience = "Store.Client";
    public const string InternalApiKey = "integration-tests-internal-api-key-32-characters-long";

    public static string For(string userId, string email, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string User(string id = "user-1") => For(id, $"{id}@test.local", Roles.User);
    public static string DemoAdmin(string id = "demo-admin-1") => For(id, $"{id}@test.local", Roles.DemoAdmin);
    public static string TrueAdmin(string id = "true-admin-1") => For(id, $"{id}@test.local", Roles.TrueAdmin);
}

public static class HttpClientAuthExtensions
{
    public static HttpClient WithToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static HttpClient AsUser(this HttpClient client, string id = "user-1") => client.WithToken(TestTokens.User(id));
    public static HttpClient AsDemoAdmin(this HttpClient client) => client.WithToken(TestTokens.DemoAdmin());
    public static HttpClient AsTrueAdmin(this HttpClient client) => client.WithToken(TestTokens.TrueAdmin());

    /// <summary>Token for a role name, or no token for "anonymous" - handy in [Theory] data.</summary>
    public static HttpClient As(this HttpClient client, string who) => who switch
    {
        "anonymous" => client,
        Roles.User => client.AsUser(),
        Roles.DemoAdmin => client.AsDemoAdmin(),
        Roles.TrueAdmin => client.AsTrueAdmin(),
        _ => throw new ArgumentOutOfRangeException(nameof(who), who, "unknown role")
    };
}
