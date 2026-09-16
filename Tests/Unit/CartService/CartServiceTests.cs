using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Store.CartService.Data;
using Store.Shared.Configuration;
using Store.Shared.Services;
using Store.Tests.Unit.TestSupport;
using Xunit;

namespace Store.Tests.Unit.CartService;

public class CartServiceTests
{
    private readonly Mock<ILogger<Store.CartService.Services.CartService>> _loggerMock = new();
    private static readonly IOptions<ServiceEndpointsOptions> Endpoints = Options.Create(new ServiceEndpointsOptions
    {
        IdentityService = "http://identity.test",
        ProductService = "http://product.test",
        CartService = "http://cart.test",
        OrderService = "http://order.test",
        AuditLogService = "http://audit.test"
    });
    private readonly Mock<IAuditLogClient> _auditLogClientMock = new();
    private readonly CartDbContext _dbContext;
    private readonly Store.CartService.Services.CartService _cartService;

    public CartServiceTests()
    {
        var options = new DbContextOptionsBuilder<CartDbContext>()
            .UseInMemoryDatabase(databaseName: $"CartServiceTests-{Guid.NewGuid():N}") // one database per test class instance - xUnit creates one per test
            .Options;
        _dbContext = new CartDbContext(options);
        _cartService = new Store.CartService.Services.CartService(
            _dbContext,
            _loggerMock.Object,
            NoNetworkHttpClient.Create(),
            Endpoints,
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
