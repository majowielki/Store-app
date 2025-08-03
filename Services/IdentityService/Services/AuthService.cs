using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Store.IdentityService.Data;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.DTOs.Responses;
using Store.IdentityService.Models;
using Store.Shared.Models;
using Store.Shared.Utility;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Store.IdentityService.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "User with this email already exists"
                };
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return new AuthResponse
                {
                    Success = false,
                    Message = string.Join(", ", errors)
                };
            }

            // Assign default user role
            await EnsureRoleExistsAsync(Constants.Role_User);
            await _userManager.AddToRoleAsync(user, Constants.Role_User);

            _logger.LogInformation("User registered successfully: {Email}", request.Email);

            // Generate JWT token
            var (accessToken, expiresAt) = await GenerateAccessTokenAsync(user);

            return new AuthResponse
            {
                Success = true,
                Message = "Registration successful",
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = await MapToUserResponseAsync(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during registration"
            };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            if (!user.IsActive)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Account is deactivated"
                };
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Generate JWT token
            var (accessToken, expiresAt) = await GenerateAccessTokenAsync(user);

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = await MapToUserResponseAsync(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during login"
            };
        }
    }

    public async Task<AuthResponse> DemoLoginAsync(DemoLoginRequest request)
    {
        try
        {
            // Find demo user
            var demoUser = await _userManager.FindByEmailAsync(Constants.DemoUserEmail);
            if (demoUser == null)
            {
                _logger.LogError("Demo user not found. Should be created during database initialization.");
                return new AuthResponse
                {
                    Success = false,
                    Message = "Demo user not available"
                };
            }

            // Update last login
            demoUser.LastLoginAt = DateTime.UtcNow;
            demoUser.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(demoUser);

            // Generate JWT token
            var (accessToken, expiresAt) = await GenerateAccessTokenAsync(demoUser);

            _logger.LogInformation("Demo user logged in successfully");

            return new AuthResponse
            {
                Success = true,
                Message = "Demo login successful",
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = await MapToUserResponseAsync(demoUser)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during demo login");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during demo login"
            };
        }
    }

    public async Task<AuthResponse> CreateTrueAdminAsync(CreateTrueAdminRequest request)
    {
        try
        {
            // Verify admin creation token
            var validToken = _configuration[Constants.AdminCreationTokenKey];
            if (string.IsNullOrEmpty(validToken) || request.AdminCreationToken != validToken)
            {
                _logger.LogWarning("Invalid admin creation token attempted");
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid admin creation token"
                };
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "User with this email already exists"
                };
            }

            // Create new true admin user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return new AuthResponse
                {
                    Success = false,
                    Message = string.Join(", ", errors)
                };
            }

            // Assign true admin role
            await EnsureRoleExistsAsync(Constants.Role_TrueAdmin);
            await _userManager.AddToRoleAsync(user, Constants.Role_TrueAdmin);

            _logger.LogInformation("True admin created successfully: {Email}", request.Email);

            // Generate JWT token
            var (accessToken, expiresAt) = await GenerateAccessTokenAsync(user);

            return new AuthResponse
            {
                Success = true,
                Message = "True admin created successfully",
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = await MapToUserResponseAsync(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during true admin creation");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during admin creation"
            };
        }
    }

    public async Task<ApiResponse<UserResponse>> GetUserProfileAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserResponse>.Error("User not found");
            }

            var userResponse = await MapToUserResponseAsync(user);
            return ApiResponse<UserResponse>.Success(userResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return ApiResponse<UserResponse>.Error("An error occurred while retrieving profile");
        }
    }

    public async Task<ApiResponse<UserResponse>> UpdateUserProfileAsync(string userId, UpdateProfileRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserResponse>.Error("User not found");
            }

            // Update user properties
            if (!string.IsNullOrEmpty(request.FirstName))
                user.FirstName = request.FirstName;
            
            if (!string.IsNullOrEmpty(request.LastName))
                user.LastName = request.LastName;
            
            if (!string.IsNullOrEmpty(request.SimpleAddress))
                user.SimpleAddress = request.SimpleAddress;

            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<UserResponse>.ValidationError(errors);
            }

            var userResponse = await MapToUserResponseAsync(user);
            return ApiResponse<UserResponse>.Success(userResponse, "Profile updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return ApiResponse<UserResponse>.Error("An error occurred while updating profile");
        }
    }

    public async Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<string>.Error("User not found");
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<string>.ValidationError(errors);
            }

            return ApiResponse<string>.Success("", "Password changed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return ApiResponse<string>.Error("An error occurred while changing password");
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!);
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ApiResponse<IEnumerable<UserResponse>>> GetAllUsersAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var users = await _userManager.Users
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var userResponses = new List<UserResponse>();
            foreach (var user in users)
            {
                userResponses.Add(await MapToUserResponseAsync(user));
            }

            return ApiResponse<IEnumerable<UserResponse>>.Success(userResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return ApiResponse<IEnumerable<UserResponse>>.Error("An error occurred while retrieving users");
        }
    }

    public async Task<ApiResponse<string>> AssignRoleAsync(string userId, string role)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<string>.Error("User not found");
            }

            await EnsureRoleExistsAsync(role);

            if (await _userManager.IsInRoleAsync(user, role))
            {
                return ApiResponse<string>.Success("", $"User already has {role} role");
            }

            var result = await _userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<string>.ValidationError(errors);
            }

            return ApiResponse<string>.Success("", $"Role {role} assigned successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role");
            return ApiResponse<string>.Error("An error occurred while assigning role");
        }
    }

    public async Task<ApiResponse<string>> RemoveRoleAsync(string userId, string role)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<string>.Error("User not found");
            }

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                return ApiResponse<string>.Success("", $"User doesn't have {role} role");
            }

            var result = await _userManager.RemoveFromRoleAsync(user, role);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<string>.ValidationError(errors);
            }

            return ApiResponse<string>.Success("", $"Role {role} removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role");
            return ApiResponse<string>.Error("An error occurred while removing role");
        }
    }

    public async Task<ApiResponse<string>> DeleteUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<string>.Error("User not found");
            }

            // Prevent deletion of demo user
            if (user.Email == Constants.DemoUserEmail)
            {
                return ApiResponse<string>.Error("Cannot delete demo user");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<string>.ValidationError(errors);
            }

            _logger.LogInformation("User deleted successfully: {UserId}", userId);
            return ApiResponse<string>.Success("", "User deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            return ApiResponse<string>.Error("An error occurred while deleting user");
        }
    }

    public async Task<ApiResponse<string>> DemoAdminAttemptCreateAsync()
    {
        return ApiResponse<string>.Error("Demo admin cannot create users. Contact a true administrator for user creation.");
    }

    public async Task<ApiResponse<string>> DemoAdminAttemptUpdateAsync()
    {
        return ApiResponse<string>.Error("Demo admin cannot update users. Contact a true administrator for user updates.");
    }

    public async Task<ApiResponse<string>> DemoAdminAttemptDeleteAsync()
    {
        return ApiResponse<string>.Error("Demo admin cannot delete users. Contact a true administrator for user deletion.");
    }

    #region Private Methods

    private async Task<(string token, DateTime expiresAt)> GenerateAccessTokenAsync(ApplicationUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!);
        var expiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:ExpirationInMinutes"]!));

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new("firstName", user.FirstName ?? ""),
            new("lastName", user.LastName ?? ""),
            new("displayName", user.DisplayName)
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role)); // For policy-based authorization
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiresAt);
    }

    private async Task<UserResponse> MapToUserResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            SimpleAddress = user.SimpleAddress,
            Roles = roles.ToList(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    #endregion
}