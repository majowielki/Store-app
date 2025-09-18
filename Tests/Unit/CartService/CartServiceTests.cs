using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Store.CartService.DTOs.Requests;
using Store.Shared.Models;
using Store.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Store.CartService.Data;

namespace Store.Tests.Unit.CartService;

public class CartServiceTests
{
    private readonly Mock<ILogger<Store.CartService.Services.CartService>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<IAuditLogClient> _auditLogClientMock = new();
    private readonly CartDbContext _dbContext;
    private readonly Store.CartService.Services.CartService _cartService;

    public CartServiceTests()
    {
        var options = new DbContextOptionsBuilder<CartDbContext>()
            .UseInMemoryDatabase(databaseName: "CartServiceTests")
            .Options;
        _dbContext = new CartDbContext(options);
        _cartService = new Store.CartService.Services.CartService(
            _dbContext,
            _loggerMock.Object,
            new HttpClient(), // Use real HttpClient for now
            _configMock.Object,
            _auditLogClientMock.Object
        );
    }

    [Fact]
    public async Task GetCartByUserIdAsync_Returns_Error_When_Not_Found()
    {
        var result = await _cartService.GetCartByUserIdAsync("user1");
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task CreateCartAsync_Creates_And_Returns_Cart()
    {
        var result = await _cartService.CreateCartAsync("user2");
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("user2", result.Data.UserId);
    }
}
