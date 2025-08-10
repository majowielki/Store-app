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
    Task<AuthResponse> DemoAdminLoginAsync(DemoAdminLoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    
    // User operations
    Task<ApiResponse<UserResponse>> GetCurrentUserAsync(string userId);
    Task<ApiResponse<UserResponse>> GetUserAsync(string userId);
    
    // Admin operations (Admin access required)
    Task<ApiResponse<IEnumerable<UserResponse>>> GetAllUsersAsync(int page = 1, int pageSize = 20);
}
