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
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for email: {Email}", request.Email);
            return StatusCode(500, new AuthResponse 
            { 
                Success = false, 
                Message = "An error occurred during registration" 
            });
        }
    }

    /// <summary>
    /// Login user
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login for email: {Email}", request.Email);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during demo login");
            return StatusCode(500, new AuthResponse 
            { 
                Success = false, 
                Message = "An error occurred during demo login" 
            });
        }
    }

    /// <summary>
    /// Create True Admin - Requires admin creation token
    /// </summary>
    /// <param name="request">True admin creation data with token</param>
    /// <returns>Authentication response with true admin token</returns>
    [HttpPost("create-true-admin")]
    public async Task<ActionResult<AuthResponse>> CreateTrueAdmin([FromBody] CreateTrueAdminRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.CreateTrueAdminAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during true admin creation");
            return StatusCode(500, new AuthResponse 
            { 
                Success = false, 
                Message = "An error occurred during admin creation" 
            });
        }
    }

    /// <summary>
    /// Validate JWT token
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>Token validation result</returns>
    [HttpPost("validate-token")]
    public async Task<ActionResult<bool>> ValidateToken([FromBody] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Token is required");
        }

        try
        {
            var isValid = await _authService.ValidateTokenAsync(token);
            return Ok(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, "An error occurred while validating token");
        }
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    /// <returns>User profile data</returns>
    [HttpGet("profile")]
    [Authorize(Policy = "UserAccess")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetProfile()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<UserResponse> 
                { 
                    IsSuccess = false, 
                    Message = "User not found" 
                });
            }

            var result = await _authService.GetUserProfileAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, new ApiResponse<UserResponse> 
            { 
                IsSuccess = false, 
                Message = "An error occurred while retrieving profile" 
            });
        }
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    /// <param name="request">Profile update data</param>
    /// <returns>Updated user profile</returns>
    [HttpPut("profile")]
    [Authorize(Policy = "UserAccess")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<UserResponse> 
                { 
                    IsSuccess = false, 
                    Message = "User not found" 
                });
            }

            var result = await _authService.UpdateUserProfileAsync(userId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, new ApiResponse<UserResponse> 
            { 
                IsSuccess = false, 
                Message = "An error occurred while updating profile" 
            });
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="request">Password change data</param>
    /// <returns>Success message</returns>
    [HttpPost("change-password")]
    [Authorize(Policy = "UserAccess")]
    public async Task<ActionResult<ApiResponse<string>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<string> 
                { 
                    IsSuccess = false, 
                    Message = "User not found" 
                });
            }

            var result = await _authService.ChangePasswordAsync(userId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing user password");
            return StatusCode(500, new ApiResponse<string> 
            { 
                IsSuccess = false, 
                Message = "An error occurred while changing password" 
            });
        }
    }

    /// <summary>
    /// Get all users (True Admin only)
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>List of users</returns>
    [HttpGet("users")]
    [Authorize(Policy = "TrueAdminOnly")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserResponse>>>> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _authService.GetAllUsersAsync(page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return StatusCode(500, new ApiResponse<IEnumerable<UserResponse>> 
            { 
                IsSuccess = false, 
                Message = "An error occurred while retrieving users" 
            });
        }
    }

    /// <summary>
    /// Assign role to user (True Admin only)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="role">Role to assign</param>
    /// <returns>Success message</returns>
    [HttpPost("users/{userId}/roles/{role}")]
    [Authorize(Policy = "TrueAdminOnly")]
    public async Task<ActionResult<ApiResponse<string>>> AssignRole(string userId, string role)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
        {
            return BadRequest(new ApiResponse<string>
            {
                IsSuccess = false,
                Message = "User ID and role are required"
            });
        }

        try
        {
            var result = await _authService.AssignRoleAsync(userId, role);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {Role} to user {UserId}", role, userId);
            return StatusCode(500, new ApiResponse<string> 
            { 
                IsSuccess = false, 
                Message = "An error occurred while assigning role" 
            });
        }
    }

    /// <summary>
    /// Remove role from user (True Admin only)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="role">Role to remove</param>
    /// <returns>Success message</returns>
    [HttpDelete("users/{userId}/roles/{role}")]
    [Authorize(Policy = "TrueAdminOnly")]
    public async Task<ActionResult<ApiResponse<string>>> RemoveRole(string userId, string role)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
        {
            return BadRequest(new ApiResponse<string>
            {
                IsSuccess = false,
                Message = "User ID and role are required"
            });
        }

        try
        {
            var result = await _authService.RemoveRoleAsync(userId, role);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {Role} from user {UserId}", role, userId);
            return StatusCode(500, new ApiResponse<string> 
            { 
                IsSuccess = false, 
                Message = "An error occurred while removing role" 
            });
        }
    }

    /// <summary>
    /// Delete user (True Admin only)
    /// </summary>
    /// <param name="userId">User ID to delete</param>
    /// <returns>Success message</returns>
    [HttpDelete("users/{userId}")]
    [Authorize(Policy = "TrueAdminOnly")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteUser(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ApiResponse<string>
            {
                IsSuccess = false,
                Message = "User ID is required"
            });
        }

        try
        {
            var result = await _authService.DeleteUserAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return StatusCode(500, new ApiResponse<string> 
            { 
                IsSuccess = false, 
                Message = "An error occurred while deleting user" 
            });
        }
    }

    /// <summary>
    /// Demo Admin Create Attempt - Returns appropriate message for demo admin
    /// </summary>
    /// <returns>Error message explaining demo admin limitations</returns>
    [HttpPost("demo-admin/create-user")]
    [Authorize(Policy = "AdminAccess")]
    public async Task<ActionResult<ApiResponse<string>>> DemoAdminCreateAttempt()
    {
        try
        {
            // Check if user is actually a demo admin
            var userRoles = User.FindAll("role").Select(c => c.Value).ToList();
            if (userRoles.Contains(Constants.Role_DemoAdmin) && !userRoles.Contains(Constants.Role_TrueAdmin))
            {
                var result = await _authService.DemoAdminAttemptCreateAsync();
                return StatusCode(403, result);
            }

            // If true admin, redirect to proper endpoint
            return BadRequest(new ApiResponse<string>
            {
                IsSuccess = false,
                Message = "Use the appropriate user creation endpoint for true admins"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in demo admin create attempt");
            return StatusCode(500, new ApiResponse<string> 
            { 
                IsSuccess = false, 
                Message = "An error occurred" 
            });
        }
    }

    /// <summary>
    /// Demo Admin Update Attempt - Returns appropriate message for demo admin
    /// </summary>
    /// <returns>Error message explaining demo admin limitations</returns>
    [HttpPut("demo-admin/update-user")]
    [Authorize(Policy = "AdminAccess")]
    public async Task<ActionResult<ApiResponse<string>>> DemoAdminUpdateAttempt()
    {
        try
        {
            // Check if user is actually a demo admin
            var userRoles = User.FindAll("role").Select(c => c.Value).ToList();
            if (userRoles.Contains(Constants.Role_DemoAdmin) && !userRoles.Contains(Constants.Role_TrueAdmin))
            {
                var result = await _authService.DemoAdminAttemptUpdateAsync();
                return StatusCode(403, result);
            }

            // If true admin, redirect to proper endpoint
            return BadRequest(new ApiResponse<string>
            {
                IsSuccess = false,
                Message = "Use the appropriate user update endpoint for true admins"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in demo admin update attempt");
            return StatusCode(500, new ApiResponse<string> 
            { 
                IsSuccess = false, 
                Message = "An error occurred" 
            });
        }
    }

    /// <summary>
    /// Demo Admin Delete Attempt - Returns appropriate message for demo admin
    /// </summary>
    /// <returns>Error message explaining demo admin limitations</returns>
    [HttpDelete("demo-admin/delete-user")]
    [Authorize(Policy = "AdminAccess")]
    public async Task<ActionResult<ApiResponse<string>>> DemoAdminDeleteAttempt()
    {
        try
        {
            // Check if user is actually a demo admin
            var userRoles = User.FindAll("role").Select(c => c.Value).ToList();
            if (userRoles.Contains(Constants.Role_DemoAdmin) && !userRoles.Contains(Constants.Role_TrueAdmin))
            {
                var result = await _authService.DemoAdminAttemptDeleteAsync();
                return StatusCode(403, result);
            }

            // If true admin, redirect to proper endpoint
            return BadRequest(new ApiResponse<string>
            {
                IsSuccess = false,
                Message = "Use the appropriate user deletion endpoint for true admins"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in demo admin delete attempt");
            return StatusCode(500, new ApiResponse<string> 
            { 
                IsSuccess = false, 
                Message = "An error occurred" 
            });
        }
    }
}