using Store.BuildingBlocks.Api;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.DTOs.Responses;

namespace Store.IdentityService.Services;

/// <summary>
/// What a sign-in produces: the response the client reads (access token, profile) and the
/// refresh token the controller hands out in an httpOnly cookie, never in the body.
/// </summary>
/// <param name="Auth">Body of the response</param>
/// <param name="RefreshToken">The opaque refresh token, shown to the client once</param>
/// <param name="RefreshTokenExpiresAt">When the refresh token, and with it the session, ends</param>
public sealed record SignedIn(AuthResponse Auth, string RefreshToken, DateTime RefreshTokenExpiresAt);

/// <summary>
/// Accounts and sessions. Failures are <c>ApiException</c>s: wrong credentials are
/// <see cref="InvalidCredentialsException"/> (401), a locked account
/// <see cref="AccountLockedException"/> (423), an unknown user <c>NotFoundException</c>.
/// </summary>
public interface IAuthService
{
    Task<SignedIn> RegisterAsync(RegisterRequest request, string? clientAddress);
    Task<SignedIn> LoginAsync(LoginRequest request, string? clientAddress);
    Task<SignedIn> DemoLoginAsync(string? clientAddress);
    Task<SignedIn> DemoAdminLoginAsync(string? clientAddress);

    /// <summary>
    /// Trades a refresh token for a new access token and a new refresh token. The old one is
    /// spent; presenting it again reveals a stolen copy and ends the whole session.
    /// </summary>
    Task<SignedIn> RefreshAsync(string refreshToken, string? clientAddress);

    /// <summary>Ends the session the refresh token belongs to; an unknown or spent token is not an error.</summary>
    Task LogoutAsync(string? refreshToken);

    Task<UserResponse> GetUserAsync(string userId);
    Task<UserResponse> UpdateAddressAsync(string userId, string simpleAddress);
}

/// <summary>The e-mail and password do not match an active account; which of the two is wrong stays unsaid.</summary>
public sealed class InvalidCredentialsException : ApiException
{
    public InvalidCredentialsException(string message = "Invalid email or password")
        : base(StatusCodes.Status401Unauthorized, message)
    {
    }
}

/// <summary>Too many failed sign-ins; the account opens again after the lockout period.</summary>
public sealed class AccountLockedException : ApiException
{
    public AccountLockedException()
        : base(StatusCodes.Status423Locked, "Account is temporarily locked. Try again later.", title: "Locked")
    {
    }
}
