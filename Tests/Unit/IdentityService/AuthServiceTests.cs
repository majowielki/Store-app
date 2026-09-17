using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Configuration;
using Store.BuildingBlocks.Messaging;
using Store.IdentityService.Data;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.Models;
using Store.IdentityService.Services;
using Xunit;

namespace Store.Tests.Unit.IdentityService;

public class AuthServiceTests
{
    private static readonly JwtOptions Jwt = new()
    {
        SecretKey = "test-secret-key-12345678901234567890123456789012",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 14
    };

    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly IdentityDbContext _dbContext;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userManagerMock = MockUserManager();
        _signInManagerMock = MockSignInManager(_userManagerMock.Object);
        _roleManagerMock = MockRoleManager();
        _dbContext = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"AuthServiceTests-{Guid.NewGuid():N}")
            .Options);

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "user" });
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        _authService = new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _roleManagerMock.Object,
            _dbContext,
            new TokenService(Options.Create(Jwt)),
            Mock.Of<ILogger<AuthService>>(),
            Mock.Of<IAuditTrail>()
        );
    }

    [Fact]
    public async Task RegisterAsync_Throws_Conflict_WhenUserExists()
    {
        var request = new RegisterRequest { Email = "test@example.com", Password = "Password123", ConfirmPassword = "Password123" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(new ApplicationUser());
        var conflict = await Assert.ThrowsAsync<ConflictException>(() => _authService.RegisterAsync(request, null));
        Assert.Contains("already exists", conflict.Message);
    }

    [Fact]
    public async Task RegisterAsync_Throws_Validation_WhenPasswordInvalid()
    {
        var request = new RegisterRequest { Email = "test2@example.com", Password = "short", ConfirmPassword = "short" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort", Description = "Password too short" }));
        var rejected = await Assert.ThrowsAsync<DomainValidationException>(() => _authService.RegisterAsync(request, null));
        Assert.Equal(422, rejected.StatusCode);
        Assert.Contains("Password too short", rejected.Errors!["Password"]);
    }

    [Fact]
    public async Task RegisterAsync_Starts_A_Session_WhenValid()
    {
        var request = new RegisterRequest { Email = "test3@example.com", Password = "Password123", ConfirmPassword = "Password123" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(x => x.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

        var session = await _authService.RegisterAsync(request, "10.0.0.1");

        // The access token carries the identity the services check; the refresh token is stored only as a hash
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(session.Auth.AccessToken);
        Assert.Equal(Jwt.Issuer, jwt.Issuer);
        Assert.Contains(jwt.Claims, c => c.Type == "role" && c.Value == "user");
        Assert.Equal(request.Email, session.Auth.User.Email);
        Assert.NotEmpty(session.RefreshToken);
        var stored = Assert.Single(_dbContext.RefreshTokens);
        Assert.NotEqual(session.RefreshToken, stored.TokenHash);
        Assert.Equal("10.0.0.1", stored.CreatedByIp);
        Assert.InRange(stored.ExpiresAt, DateTime.UtcNow.AddDays(13), DateTime.UtcNow.AddDays(15));
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
