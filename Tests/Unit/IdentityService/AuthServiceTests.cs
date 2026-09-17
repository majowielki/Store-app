using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.Models;
using Store.IdentityService.Services;
using Xunit;

namespace Store.Tests.Unit.IdentityService;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<IAuditTrail> _auditTrailMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userManagerMock = MockUserManager();
        _signInManagerMock = MockSignInManager(_userManagerMock.Object);
        _roleManagerMock = MockRoleManager();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _auditTrailMock = new Mock<IAuditTrail>();

        // Setup configuration for JWT
        _configurationMock.Setup(c => c["JwtSettings:SecretKey"]).Returns("test-secret-key-12345678901234567890123456789012");
        _configurationMock.Setup(c => c["JwtSettings:ExpirationInMinutes"]).Returns("60");
        _configurationMock.Setup(c => c["JwtSettings:Issuer"]).Returns("TestIssuer");
        _configurationMock.Setup(c => c["JwtSettings:Audience"]).Returns("TestAudience");

        // Setup UserManager to return a non-null list for GetRolesAsync
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "user" });

        _authService = new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _roleManagerMock.Object,
            _configurationMock.Object,
            _loggerMock.Object,
            _auditTrailMock.Object
        );
    }

    [Fact]
    public async Task RegisterAsync_Throws_Conflict_WhenUserExists()
    {
        var request = new RegisterRequest { Email = "test@example.com", Password = "Password123", ConfirmPassword = "Password123" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(new ApplicationUser());
        var conflict = await Assert.ThrowsAsync<ConflictException>(() => _authService.RegisterAsync(request));
        Assert.Contains("already exists", conflict.Message);
    }

    [Fact]
    public async Task RegisterAsync_Throws_Validation_WhenPasswordInvalid()
    {
        var request = new RegisterRequest { Email = "test2@example.com", Password = "short", ConfirmPassword = "short" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort", Description = "Password too short" }));
        var rejected = await Assert.ThrowsAsync<DomainValidationException>(() => _authService.RegisterAsync(request));
        Assert.Equal(422, rejected.StatusCode);
        Assert.Contains("Password too short", rejected.Errors!["Password"]);
    }

    [Fact]
    public async Task RegisterAsync_Issues_A_Token_WhenValid()
    {
        var request = new RegisterRequest { Email = "test3@example.com", Password = "Password123", ConfirmPassword = "Password123" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(x => x.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        var result = await _authService.RegisterAsync(request);
        Assert.NotEmpty(result.AccessToken);
        Assert.Equal(request.Email, result.User.Email);
    }

    // Helper mocks for UserManager/SignInManager/RoleManager
    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
    private static Mock<SignInManager<ApplicationUser>> MockSignInManager(UserManager<ApplicationUser> userManager)
    {
        var context = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        return new Mock<SignInManager<ApplicationUser>>(userManager, context.Object, claimsFactory.Object, null!, null!, null!, null!);
    }
    private static Mock<RoleManager<IdentityRole>> MockRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(store.Object, null!, null!, null!, null!);
    }
}
