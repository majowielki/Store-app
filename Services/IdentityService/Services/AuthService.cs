using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.Contracts.Authorization;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.DTOs.Responses;
using Store.IdentityService.Models;
using Store.IdentityService.Seeding;
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

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new ConflictException("User with this email already exists");
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
            throw RejectedByIdentity(result, "The account could not be created");
        }

        await EnsureRoleExistsAsync(Roles.User);
        await _userManager.AddToRoleAsync(user, Roles.User);
        _logger.LogInformation("User registered successfully: {Email}", request.Email);
        return await IssueAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new InvalidCredentialsException();
        if (!user.IsActive)
        {
            throw new InvalidCredentialsException("Account is deactivated");
        }

        // lockoutOnFailure: true - failed attempts count towards Identity's lockout
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            _logger.LogWarning("Login rejected: account locked out for {Email}", request.Email);
            throw new AccountLockedException();
        }
        if (!result.Succeeded)
        {
            throw new InvalidCredentialsException();
        }

        await RecordLoginAsync(user);
        _logger.LogInformation("User logged in successfully: {Email}", request.Email);
        return await IssueAsync(user);
    }

    public Task<AuthResponse> DemoLoginAsync() => DemoSignInAsync(SeedAccounts.DemoUserEmail, "Demo user");

    public Task<AuthResponse> DemoAdminLoginAsync() => DemoSignInAsync(SeedAccounts.DemoAdminEmail, "Demo admin");

    private async Task<AuthResponse> DemoSignInAsync(string email, string account)
    {
        // The demo accounts are seeded at start-up; a missing one is a deployment fault, not a client error
        var user = await _userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"{account} not found. It should be created during database initialization.");

        await RecordLoginAsync(user);
        _logger.LogInformation("{Account} logged in successfully", account);
        return await IssueAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
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
            }, out _);
        }
        catch (Exception)
        {
            throw new InvalidCredentialsException("Invalid token");
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidCredentialsException("Invalid token");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            throw new InvalidCredentialsException("User not found or inactive");
        }

        _logger.LogInformation("Token refreshed successfully for user: {UserId}", userId);
        return await IssueAsync(user);
    }

    public async Task<UserResponse> GetUserAsync(string userId)
        => await MapToUserResponseAsync(await FindUserAsync(userId));

    public async Task<UserResponse> UpdateAddressAsync(string userId, string simpleAddress)
    {
        var user = await FindUserAsync(userId);

        var oldAddress = user.SimpleAddress;
        user.SimpleAddress = string.IsNullOrWhiteSpace(simpleAddress) ? null : simpleAddress.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw RejectedByIdentity(result, "The address could not be saved");
        }

        await _auditTrail.RecordAsync("USER_ADDRESS_UPDATED", nameof(ApplicationUser), user.Id, user.Id,
            oldValues: new { SimpleAddress = oldAddress is null ? null : "(set)" },
            newValues: new { SimpleAddress = user.SimpleAddress is null ? null : "(set)" });

        return await MapToUserResponseAsync(user);
    }

    private async Task<ApplicationUser> FindUserAsync(string userId)
        => await _userManager.FindByIdAsync(userId) ?? throw new NotFoundException("User", userId);

    private async Task RecordLoginAsync(ApplicationUser user)
    {
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    /// <summary>Identity's own rules (password policy, user name characters) as a validation problem.</summary>
    private static DomainValidationException RejectedByIdentity(IdentityResult result, string message)
    {
        var errors = result.Errors
            .GroupBy(e => e.Code.Contains("Password", StringComparison.Ordinal) ? "Password" : "Email")
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
        return new DomainValidationException(message, errors);
    }

    private async Task<AuthResponse> IssueAsync(ApplicationUser user)
    {
        var (accessToken, expiresAt) = await GenerateAccessTokenAsync(user);
        return new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            User = await MapToUserResponseAsync(user)
        };
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
