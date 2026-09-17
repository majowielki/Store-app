using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.BuildingBlocks.Authorization;
using Store.Contracts.Authorization;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.DTOs.Responses;
using Store.IdentityService.Services;
using System.Security.Claims;

namespace Store.IdentityService.Controllers;

/// <summary>Sign-up, sign-in and the signed-in user's own profile.</summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Creates an account and signs it in; 409 when the e-mail is taken, 422 when the password is too weak.</summary>
    [HttpPost("register")]
    public Task<AuthResponse> Register([FromBody] RegisterRequest request)
        => _authService.RegisterAsync(request);

    /// <summary>Signs in with e-mail and password; 401 when they do not match, 423 while the account is locked.</summary>
    [HttpPost("login")]
    public Task<AuthResponse> Login([FromBody] LoginRequest request)
        => _authService.LoginAsync(request);

    /// <summary>Signs in as the demo customer, without a password.</summary>
    [HttpPost("demo-login")]
    public Task<AuthResponse> DemoLogin()
        => _authService.DemoLoginAsync();

    /// <summary>Signs in as the read-only demo administrator, without a password.</summary>
    [HttpPost("demo-admin-login")]
    public Task<AuthResponse> DemoAdminLogin()
        => _authService.DemoAdminLoginAsync();

    /// <summary>A new access token for a token that is still valid apart from its expiry.</summary>
    [HttpPost("refresh")]
    public Task<AuthResponse> RefreshToken([FromBody] RefreshTokenRequest request)
        => _authService.RefreshTokenAsync(request);

    /// <summary>
    /// The signed-in user's profile; 204 for an anonymous caller, so a client can ask without
    /// knowing whether its stored token is still good. 404 when the account no longer exists.
    /// </summary>
    [HttpGet("me")]
    [AllowAnonymous]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return NoContent();
        }

        return await _authService.GetUserAsync(userId);
    }

    /// <summary>Replaces the delivery address stored in the profile; an empty value clears it.</summary>
    [HttpPut("me/address")]
    [Authorize(Policy = Policies.User)]
    public Task<UserResponse> UpdateMyAddress([FromBody] UpdateAddressRequest request)
        => _authService.UpdateAddressAsync(User.GetRequiredUserId(), request.SimpleAddress);

    /// <summary>Ends the session. Tokens are stateless for now, so the client discards its token; server-side revocation comes with refresh tokens.</summary>
    [HttpPost("logout")]
    [Authorize(Policy = Policies.User)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout() => NoContent();
}
