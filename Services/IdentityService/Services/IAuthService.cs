using Store.BuildingBlocks.Api;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.DTOs.Responses;

namespace Store.IdentityService.Services;

public interface IAuthService
{
    // Core authentication operations
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<AuthResponse>> DemoLoginAsync(DemoLoginRequest request);
    Task<ApiResponse<AuthResponse>> DemoAdminLoginAsync(DemoAdminLoginRequest request);
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);

    // User operations
    Task<ApiResponse<UserResponse>> GetCurrentUserAsync(string userId);
    Task<ApiResponse<UserResponse>> GetUserAsync(string userId);
    Task<ApiResponse<UserResponse>> UpdateAddressAsync(string userId, string simpleAddress);

    // Admin operations (Admin access required)
    Task<ApiResponse<IEnumerable<UserResponse>>> GetAllUsersAsync(int page = 1, int pageSize = 20);
}
