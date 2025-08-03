using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.DTOs.Responses;
using Store.Shared.Models;

namespace Store.IdentityService.Services;

public interface IAuthService
{
    // Core authentication operations
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> DemoLoginAsync(DemoLoginRequest request);
    Task<AuthResponse> CreateTrueAdminAsync(CreateTrueAdminRequest request);
    Task<ApiResponse<UserResponse>> GetUserProfileAsync(string userId);
    Task<ApiResponse<UserResponse>> UpdateUserProfileAsync(string userId, UpdateProfileRequest request);
    Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<bool> ValidateTokenAsync(string token);
    
    // Admin operations (True Admin only)
    Task<ApiResponse<IEnumerable<UserResponse>>> GetAllUsersAsync(int page = 1, int pageSize = 20);
    Task<ApiResponse<string>> AssignRoleAsync(string userId, string role);
    Task<ApiResponse<string>> RemoveRoleAsync(string userId, string role);
    Task<ApiResponse<string>> DeleteUserAsync(string userId);
    
    // Demo Admin operations (returns appropriate messages for unauthorized actions)
    Task<ApiResponse<string>> DemoAdminAttemptCreateAsync();
    Task<ApiResponse<string>> DemoAdminAttemptUpdateAsync();
    Task<ApiResponse<string>> DemoAdminAttemptDeleteAsync();
}
