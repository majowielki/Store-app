using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.BuildingBlocks.Observability;
using Store.Contracts.Authorization;
using Store.IdentityService.Data;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.DTOs.Responses;
using Store.IdentityService.Models;
using Store.IdentityService.Seeding;

namespace Store.IdentityService.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IdentityDbContext _context;
    private readonly ITokenService _tokens;
    private readonly DemoOptions _demo;
    private readonly ILogger<AuthService> _logger;
    private readonly IAuditTrail _auditTrail;
    private readonly StoreMetrics _metrics;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IdentityDbContext context,
        ITokenService tokens,
        IOptions<DemoOptions> demo,
        ILogger<AuthService> logger,
        IAuditTrail auditTrail,
        StoreMetrics metrics)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _context = context;
        _tokens = tokens;
        _demo = demo.Value;
        _logger = logger;
        _auditTrail = auditTrail;
        _metrics = metrics;
    }

    public async Task<SignedIn> RegisterAsync(RegisterRequest request, string? clientAddress)
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
        return await StartSessionAsync(user, clientAddress);
    }

    public async Task<SignedIn> LoginAsync(LoginRequest request, string? clientAddress)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            _metrics.LoginFailed("unknown-account");
            throw new InvalidCredentialsException();
        }
        if (!user.IsActive)
        {
            // Same answer as a wrong password: the caller learns nothing about the account
            _metrics.LoginFailed("inactive");
            throw new InvalidCredentialsException();
        }

        // lockoutOnFailure: true - failed attempts count towards Identity's lockout
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            _logger.LogWarning("Login rejected: account locked out for {Email}", request.Email);
            _metrics.LoginFailed("locked-out");
            throw new AccountLockedException();
        }
        if (!result.Succeeded)
        {
            _metrics.LoginFailed("wrong-password");
            throw new InvalidCredentialsException();
        }

        _logger.LogInformation("User logged in successfully: {Email}", request.Email);
        return await StartSessionAsync(user, clientAddress);
    }

    public Task<SignedIn> DemoLoginAsync(string? clientAddress) => DemoSignInAsync(_demo.UserEmail, "Demo user", clientAddress);

    public Task<SignedIn> DemoAdminLoginAsync(string? clientAddress) => DemoSignInAsync(_demo.AdminEmail, "Demo admin", clientAddress);

    private async Task<SignedIn> DemoSignInAsync(string email, string account, string? clientAddress)
    {
        if (!_demo.Enabled)
        {
            // Where the showcase is off, the endpoints do not exist as far as the client can tell
            throw new NotFoundException("Demo accounts are not available");
        }

        // The demo accounts are seeded at start-up; a missing one is a deployment fault, not a client error
        var user = await _userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"{account} not found. It should be created during database initialization.");

        _logger.LogInformation("{Account} logged in successfully", account);
        return await StartSessionAsync(user, clientAddress);
    }

    public async Task<SignedIn> RefreshAsync(string refreshToken, string? clientAddress)
    {
        var presented = await _context.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == _tokens.HashRefreshToken(refreshToken))
            ?? throw new InvalidCredentialsException("Invalid refresh token");

        if (presented.RevokedAt is not null)
        {
            // A spent token presented again: either the client replayed it or somebody stole a
            // copy. Both copies become useless, and the user signs in again.
            await RevokeFamilyAsync(presented.FamilyId, "reuse");
            _logger.LogWarning("Refresh token reuse for user {UserId}; session family {FamilyId} revoked", presented.UserId, presented.FamilyId);
            throw new InvalidCredentialsException("Refresh token was already used");
        }

        if (presented.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidCredentialsException("Refresh token expired");
        }

        if (!presented.User.IsActive)
        {
            await RevokeFamilyAsync(presented.FamilyId, "inactive user");
            throw new InvalidCredentialsException("Account is deactivated");
        }

        // Rotate: the presented token is spent, its successor takes over in the same family.
        // Spending it is one conditional update, so two simultaneous refreshes with the same
        // token (two tabs) rotate it once; the loser is told to try again with the new one.
        var (token, hash) = _tokens.CreateRefreshToken();
        var now = DateTime.UtcNow;
        var spent = await _context.RefreshTokens
            .Where(t => t.Id == presented.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(set => set
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.ReplacedByHash, hash));
        if (spent == 0)
        {
            throw new InvalidCredentialsException("Refresh token was already used");
        }

        var successor = NewRefreshToken(presented.User, presented.FamilyId, hash, clientAddress, now);
        _context.RefreshTokens.Add(successor);
        await _context.SaveChangesAsync();

        return await IssueAsync(presented.User, token, successor.ExpiresAt);
    }

    public async Task LogoutAsync(string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return;
        }

        var hash = _tokens.HashRefreshToken(refreshToken);
        var presented = await _context.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == hash);
        if (presented is not null)
        {
            await RevokeFamilyAsync(presented.FamilyId, "logout");
            _logger.LogInformation("User {UserId} logged out; session family {FamilyId} revoked", presented.UserId, presented.FamilyId);
        }
    }

    public async Task<UserResponse> GetUserAsync(string userId)
        => await MapToUserResponseAsync(await FindUserAsync(userId));

    public async Task<UserResponse> UpdateAddressAsync(string userId, string simpleAddress)
    {
        var user = await FindUserAsync(userId);
        if (IsDemoAccount(user))
        {
            throw new ForbiddenException("The demo account is shared; its address cannot be changed");
        }

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

    /// <summary>A fresh session for a user who just proved who they are: new family, new tokens.</summary>
    private async Task<SignedIn> StartSessionAsync(ApplicationUser user, string? clientAddress)
    {
        var now = DateTime.UtcNow;
        user.LastLoginAt = now;
        user.UpdatedAt = now;
        await _userManager.UpdateAsync(user);

        var (token, hash) = _tokens.CreateRefreshToken();
        var refreshToken = NewRefreshToken(user, Guid.NewGuid(), hash, clientAddress, now);
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return await IssueAsync(user, token, refreshToken.ExpiresAt);
    }

    private RefreshToken NewRefreshToken(ApplicationUser user, Guid familyId, string hash, string? clientAddress, DateTime now) => new()
    {
        User = user,
        UserId = user.Id,
        TokenHash = hash,
        FamilyId = familyId,
        CreatedAt = now,
        ExpiresAt = now + _tokens.RefreshTokenLifetime,
        CreatedByIp = clientAddress
    };

    private Task<int> RevokeFamilyAsync(Guid familyId, string reason)
        => _context.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.RevokedAt, DateTime.UtcNow));

    private async Task<SignedIn> IssueAsync(ApplicationUser user, string refreshToken, DateTime refreshTokenExpiresAt)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = _tokens.CreateAccessToken(user, roles);
        var auth = new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            User = MapToUserResponse(user, roles)
        };
        return new SignedIn(auth, refreshToken, refreshTokenExpiresAt);
    }

    /// <summary>Identity's own rules (password policy, user name characters) as a validation problem.</summary>
    private static DomainValidationException RejectedByIdentity(IdentityResult result, string message)
    {
        var errors = result.Errors
            .GroupBy(e => e.Code.Contains("Password", StringComparison.Ordinal) ? "Password" : "Email")
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
        return new DomainValidationException(message, errors);
    }

    private async Task<UserResponse> MapToUserResponseAsync(ApplicationUser user)
        => MapToUserResponse(user, await _userManager.GetRolesAsync(user));

    /// <summary>The showcase accounts every visitor shares; their profile is read-only.</summary>
    private bool IsDemoAccount(ApplicationUser user)
        => _demo.Enabled
           && (string.Equals(user.Email, _demo.UserEmail, StringComparison.OrdinalIgnoreCase)
               || string.Equals(user.Email, _demo.AdminEmail, StringComparison.OrdinalIgnoreCase));

    private UserResponse MapToUserResponse(ApplicationUser user, IList<string> roles) => new()
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
        IsDemo = IsDemoAccount(user),
        CreatedAt = user.CreatedAt
    };

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}
