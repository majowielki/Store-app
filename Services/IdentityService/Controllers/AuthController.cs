using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.DTOs.Responses;
using Store.IdentityService.Services;
using Store.Shared.Models;
using Store.Shared.Utility;
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
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.RegisterAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch
        {
            return StatusCode(500, new AuthResponse 
            { 
                Success = false, 
                Message = "An error occurred during registration" 
            });
        }
    }

    /// <summary>
    /// Login user or true admin
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with token</returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.LoginAsync(request);
            
            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }
        catch
        {
            return StatusCode(500, new AuthResponse 
            { 
                Success = false, 
                Message = "An error occurred during login" 
            });
        }
    }

    /// <summary>
    /// Demo login - No authentication required, returns demo user token
    /// </summary>
    /// <param name="request">Demo login request (empty)</param>
    /// <returns>Authentication response with demo user token</returns>
    [HttpPost("demo-login")]
    public async Task<ActionResult<AuthResponse>> DemoLogin([FromBody] DemoLoginRequest request)
    {
        try
        {
            var result = await _authService.DemoLoginAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch
        {
            return StatusCode(500, new AuthResponse 
            { 
                Success = false, 
                Message = "An error occurred during demo login" 
            });
        }
    }

    /// <summary>
    /// Demo admin login - No authentication required, returns demo admin token
    /// </summary>
    /// <param name="request">Demo admin login request (empty)</param>
    /// <returns>Authentication response with demo admin token</returns>
    [HttpPost("demo-admin-login")]
    public async Task<ActionResult<AuthResponse>> DemoAdminLogin([FromBody] DemoAdminLoginRequest request)
    {
        try
        {
            var result = await _authService.DemoAdminLoginAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch
        {
            return StatusCode(500, new AuthResponse 
            { 
                Success = false, 
                Message = "An error occurred during demo admin login" 
            });
        }
    }

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New authentication response with refreshed token</returns>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.RefreshTokenAsync(request);
            
            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }
        catch
        {
            return StatusCode(500, new AuthResponse 
            { 
                Success = false, 
                Message = "An error occurred during token refresh" 
            });
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <returns>Current user profile data</returns>
    [HttpGet("me")]
    [Authorize(Policy = "UserAccess")]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            var result = await _authService.GetCurrentUserAsync(userId);
            if (!result.IsSuccess || result.Data is null)
            {
                if ((int)result.StatusCode == StatusCodes.Status401Unauthorized)
                {
                    return Unauthorized(result.Message);
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
    [Authorize(Policy = "UserAccess")]
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
    [Authorize(Policy = "UserAccess")]
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
    [Authorize(Policy = "AdminAccess")]
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
    [Authorize(Policy = "AdminAccess")]
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