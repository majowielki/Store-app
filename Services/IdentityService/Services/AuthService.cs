using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.Contracts.Authorization;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.DTOs.Responses;
using Store.IdentityService.Models;
using Store.IdentityService.Seeding;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
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
    private readonly IAuditTrail _auditTrail;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IAuditTrail auditTrail)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
        _auditTrail = auditTrail;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return ApiResponse<AuthResponse>.Error("User with this email already exists");
            }
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
                return ApiResponse<AuthResponse>.ValidationError(errors);
            }
            await EnsureRoleExistsAsync(Roles.User);
            await _userManager.AddToRoleAsync(user, Roles.User);
            _logger.LogInformation("User registered successfully: {Email}", request.Email);
            var (accessToken, expiresAt) = await GenerateAccessTokenAsync(user);
            var authResponse = new AuthResponse
            {
                Success = true,
                Message = "Registration successful",
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = await MapToUserResponseAsync(user)
            };
            return ApiResponse<AuthResponse>.Success(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return ApiResponse<AuthResponse>.Error("An error occurred during registration");
        }
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return ApiResponse<AuthResponse>.Error("Invalid email or password");
            }
            if (!user.IsActive)
            {
                return ApiResponse<AuthResponse>.Error("Account is deactivated");
            }
            // lockoutOnFailure: true - failed attempts count towards Identity's lockout
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (result.IsLockedOut)
            {
                _logger.LogWarning("Login rejected: account locked out for {Email}", request.Email);
                return ApiResponse<AuthResponse>.Error("Account is temporarily locked. Try again later.", HttpStatusCode.Locked);
            }
            if (!result.Succeeded)
            {
                return ApiResponse<AuthResponse>.Error("Invalid email or password");
            }
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            var (accessToken, expiresAt) = await GenerateAccessTokenAsync(user);
            _logger.LogInformation("User logged in successfully: {Email}", request.Email);
            var authResponse = new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = await MapToUserResponseAsync(user)
            };
            return ApiResponse<AuthResponse>.Success(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return ApiResponse<AuthResponse>.Error("An error occurred during login");
        }
    }

    public async Task<ApiResponse<AuthResponse>> DemoLoginAsync(DemoLoginRequest request)
    {
        try
        {
            var demoUser = await _userManager.FindByEmailAsync(SeedAccounts.DemoUserEmail);
            if (demoUser == null)
            {
                _logger.LogError("Demo user not found. Should be created during database initialization.");
                return ApiResponse<AuthResponse>.Error("Demo user not available");
            }
            demoUser.LastLoginAt = DateTime.UtcNow;
            demoUser.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(demoUser);
            var (accessToken, expiresAt) = await GenerateAccessTokenAsync(demoUser);
            _logger.LogInformation("Demo user logged in successfully");
            var authResponse = new AuthResponse
            {
                Success = true,
                Message = "Demo login successful",
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = await MapToUserResponseAsync(demoUser)
            };
            return ApiResponse<AuthResponse>.Success(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during demo login");
            return ApiResponse<AuthResponse>.Error("An error occurred during demo login");
        }
    }

    public async Task<ApiResponse<AuthResponse>> DemoAdminLoginAsync(DemoAdminLoginRequest request)
    {
        try
        {
            var demoAdmin = await _userManager.FindByEmailAsync(SeedAccounts.DemoAdminEmail);
            if (demoAdmin == null)
            {
                _logger.LogError("Demo admin not found. Should be created during database initialization.");
                return ApiResponse<AuthResponse>.Error("Demo admin not available");
            }
            demoAdmin.LastLoginAt = DateTime.UtcNow;
            demoAdmin.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(demoAdmin);
            var (accessToken, expiresAt) = await GenerateAccessTokenAsync(demoAdmin);
            _logger.LogInformation("Demo admin logged in successfully");
            var authResponse = new AuthResponse
            {
                Success = true,
                Message = "Demo admin login successful",
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = await MapToUserResponseAsync(demoAdmin)
            };
            return ApiResponse<AuthResponse>.Success(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during demo admin login");
            return ApiResponse<AuthResponse>.Error("An error occurred during demo admin login");
        }
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!);
            ClaimsPrincipal principal;
            try
            {
                principal = tokenHandler.ValidateToken(request.Token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ValidateLifetime = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);
            }
            catch (Exception)
            {
                return ApiResponse<AuthResponse>.Error("Invalid token");
            }
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return ApiResponse<AuthResponse>.Error("Invalid token");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return ApiResponse<AuthResponse>.Error("User not found or inactive");
            }
            var (accessToken, expiresAt) = await GenerateAccessTokenAsync(user);
            _logger.LogInformation("Token refreshed successfully for user: {UserId}", userId);
            var authResponse = new AuthResponse
            {
                Success = true,
                Message = "Token refreshed successfully",
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = await MapToUserResponseAsync(user)
            };
            return ApiResponse<AuthResponse>.Success(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return ApiResponse<AuthResponse>.Error("An error occurred during token refresh");
        }
    }

    public async Task<ApiResponse<UserResponse>> GetCurrentUserAsync(string userId)
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
            _logger.LogError(ex, "Error retrieving current user");
            return ApiResponse<UserResponse>.Error("An error occurred while retrieving user information");
        }
    }

    public async Task<ApiResponse<UserResponse>> GetUserAsync(string userId)
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
            _logger.LogError(ex, "Error retrieving user");
            return ApiResponse<UserResponse>.Error("An error occurred while retrieving user information");
        }
    }

    public async Task<ApiResponse<UserResponse>> UpdateAddressAsync(string userId, string simpleAddress)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserResponse>.Error("User not found", System.Net.HttpStatusCode.NotFound);
            }

            var oldAddress = user.SimpleAddress;
            user.SimpleAddress = string.IsNullOrWhiteSpace(simpleAddress) ? null : simpleAddress.Trim();
            user.UpdatedAt = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<UserResponse>.ValidationError(errors);
            }

            await _auditTrail.RecordAsync("USER_ADDRESS_UPDATED", nameof(ApplicationUser), user.Id, user.Id,
                oldValues: new { SimpleAddress = oldAddress is null ? null : "(set)" },
                newValues: new { SimpleAddress = user.SimpleAddress is null ? null : "(set)" });

            var mapped = await MapToUserResponseAsync(user);
            return ApiResponse<UserResponse>.Success(mapped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user address for {UserId}", userId);
            return ApiResponse<UserResponse>.Error("An error occurred while updating address");
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

    private async Task<(string token, DateTime expiresAt)> GenerateAccessTokenAsync(ApplicationUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var secretKey = _configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("JWT SecretKey is not configured");
        var key = Encoding.UTF8.GetBytes(secretKey);
        var expiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:ExpirationInMinutes"]!));

        var roles = (await _userManager.GetRolesAsync(user)).Distinct().ToList();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new("firstName", user.FirstName ?? ""),
            new("lastName", user.LastName ?? ""),
            new("displayName", user.DisplayName)
        };

        // Add each role as a single 'role' claim (standard JWT)
        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
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

}
