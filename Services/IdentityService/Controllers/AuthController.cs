using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.DTOs.Responses;
using Store.IdentityService.Services;
using Store.Shared.Authorization;
using Store.Shared.Models;
using System.Net;
using System.Security.Claims;

namespace Store.IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration data</param>
    /// <returns>Authentication response with token</returns>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        // FluentValidation will handle validation automatically
        var result = await _authService.RegisterAsync(request);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Login user or true admin
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with token</returns>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponse>.ValidationError(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }
        var result = await _authService.LoginAsync(request);
        if (!result.IsSuccess)
        {
            // 423 Locked when Identity's lockout kicked in (SEC-06), 401 for every other failure
            return result.StatusCode == HttpStatusCode.Locked
                ? StatusCode(StatusCodes.Status423Locked, result)
                : Unauthorized(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Demo login - No authentication required, returns demo user token
    /// </summary>
    /// <param name="request">Demo login request (empty)</param>
    /// <returns>Authentication response with demo user token</returns>
    [HttpPost("demo-login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> DemoLogin([FromBody] DemoLoginRequest request)
    {
        var result = await _authService.DemoLoginAsync(request);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Demo admin login - No authentication required, returns demo admin token
    /// </summary>
    /// <param name="request">Demo admin login request (empty)</param>
    /// <returns>Authentication response with demo admin token</returns>
    [HttpPost("demo-admin-login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> DemoAdminLogin([FromBody] DemoAdminLoginRequest request)
    {
        var result = await _authService.DemoAdminLoginAsync(request);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New authentication response with refreshed token</returns>
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponse>.ValidationError(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }
        var result = await _authService.RefreshTokenAsync(request);
        if (!result.IsSuccess)
        {
            return Unauthorized(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <returns>Current user profile data</returns>
    [HttpGet("me")]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        try
        {
            // Try to get userId from claims (token)
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                // No token or invalid token: return 204 No Content (anonymous user)
                return NoContent();
            }

            var result = await _authService.GetCurrentUserAsync(userId);
            if (!result.IsSuccess || result.Data is null)
            {
                if ((int)result.StatusCode == StatusCodes.Status401Unauthorized)
                {
                    return NoContent(); // treat as anonymous
                }
                if ((int)result.StatusCode == StatusCodes.Status404NotFound)
                {
                    return NotFound(result.Message);
                }
                return BadRequest(result.Message);
            }

            return Ok(result.Data);
        }
        catch
        {
            return StatusCode(500, "An error occurred while retrieving user information");
        }
    }

    /// <summary>
    /// Update current user's simple address
    /// </summary>
    [HttpPut("me/address")]
    [Authorize(Policy = Policies.User)]
    public async Task<ActionResult<UserResponse>> UpdateMyAddress([FromBody] UpdateAddressRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            var result = await _authService.UpdateAddressAsync(userId, request.SimpleAddress);

            if (!result.IsSuccess || result.Data is null)
            {
                if ((int)result.StatusCode == StatusCodes.Status404NotFound)
                {
                    return NotFound(result.Message);
                }
                return BadRequest(result.Message);
            }

            return Ok(result.Data);
        }
        catch
        {
            return StatusCode(500, "An error occurred while updating address");
        }
    }

    /// <summary>
    /// Logout current user. For stateless JWT this is a no-op on server; client should delete token.
    /// </summary>
    /// <returns>Status 204 on success</returns>
    [HttpPost("logout")]
    [Authorize(Policy = Policies.User)]
    public ActionResult Logout()
    {
        // In a future iteration, implement token revocation/blacklist if refresh tokens are stored server-side.
        return NoContent();
    }

    /// <summary>
    /// Get user by ID (Admin access required to check orders)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User profile data</returns>
    [HttpGet("users/{userId}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetUser(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ApiResponse<UserResponse>
            {
                IsSuccess = false,
                Message = "User ID is required"
            });
        }

        try
        {
            var result = await _authService.GetUserAsync(userId);
            return Ok(result);
        }
        catch
        {
            return StatusCode(500, new ApiResponse<UserResponse>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving user information"
            });
        }
    }

    /// <summary>
    /// Get all users (Admin access required)
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>List of users</returns>
    [HttpGet("users")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserResponse>>>> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _authService.GetAllUsersAsync(page, pageSize);
            return Ok(result);
        }
        catch
        {
            return StatusCode(500, new ApiResponse<IEnumerable<UserResponse>>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving users"
            });
        }
    }
}
