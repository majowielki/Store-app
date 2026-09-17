using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.BuildingBlocks.Authorization;
using Store.Contracts.Authorization;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.DTOs.Responses;
using Store.IdentityService.Services;
using System.Security.Claims;

namespace Store.IdentityService.Controllers;

/// <summary>
/// Sign-up, sign-in and the signed-in user's own profile. A sign-in answers with a short-lived
/// access token in the body and a refresh token in an httpOnly cookie scoped to this
/// controller's path, so a script in the page can never read the refresh token.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    /// <summary>Cookie that carries the refresh token; sent only to /api/v1/auth/*.</summary>
    public const string RefreshCookie = "store_refresh";
    private const string RefreshCookiePath = "/api/v1/auth";

    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    private string? ClientAddress => HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>Creates an account and signs it in; 409 when the e-mail is taken, 422 when the password is too weak.</summary>
    [HttpPost("register")]
    public async Task<AuthResponse> Register([FromBody] RegisterRequest request)
        => SignedIn(await _authService.RegisterAsync(request, ClientAddress));

    /// <summary>Signs in with e-mail and password; 401 when they do not match, 423 while the account is locked.</summary>
    [HttpPost("login")]
    public async Task<AuthResponse> Login([FromBody] LoginRequest request)
        => SignedIn(await _authService.LoginAsync(request, ClientAddress));

    /// <summary>Signs in as the demo customer, without a password.</summary>
    [HttpPost("demo-login")]
    public async Task<AuthResponse> DemoLogin()
        => SignedIn(await _authService.DemoLoginAsync(ClientAddress));

    /// <summary>Signs in as the read-only demo administrator, without a password.</summary>
    [HttpPost("demo-admin-login")]
    public async Task<AuthResponse> DemoAdminLogin()
        => SignedIn(await _authService.DemoAdminLoginAsync(ClientAddress));

    /// <summary>
    /// A new access token for the session in the refresh cookie; the cookie is replaced with a
    /// new refresh token. 401 without a cookie, with a spent or expired one, or after the
    /// session was ended - the client signs in again then.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<AuthResponse> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshCookie];
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new InvalidCredentialsException("No refresh token");
        }

        try
        {
            return SignedIn(await _authService.RefreshAsync(refreshToken, ClientAddress));
        }
        catch (InvalidCredentialsException)
        {
            // Whatever the cookie held is useless now
            Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = RefreshCookiePath });
            throw;
        }
    }

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

    /// <summary>
    /// Ends the session: the refresh token family in the cookie is revoked and the cookie
    /// removed. The access token stays valid until it expires (minutes); the client discards it.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(Request.Cookies[RefreshCookie]);
        Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = RefreshCookiePath });
        return NoContent();
    }

    /// <summary>Writes the refresh cookie and returns the body of the sign-in response.</summary>
    private AuthResponse SignedIn(SignedIn session)
    {
        Response.Cookies.Append(RefreshCookie, session.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = session.RefreshTokenExpiresAt,
            IsEssential = true
        });
        return session.Auth;
    }
}
