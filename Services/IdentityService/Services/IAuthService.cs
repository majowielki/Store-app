using Store.BuildingBlocks.Api;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.DTOs.Responses;

namespace Store.IdentityService.Services;

/// <summary>
/// Accounts and tokens. Failures are <c>ApiException</c>s: wrong credentials are
/// <see cref="InvalidCredentialsException"/> (401), a locked account
/// <see cref="AccountLockedException"/> (423), an unknown user <c>NotFoundException</c>.
/// </summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> DemoLoginAsync();
    Task<AuthResponse> DemoAdminLoginAsync();
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);

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
