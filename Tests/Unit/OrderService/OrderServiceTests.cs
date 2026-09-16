using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Store.BuildingBlocks.Configuration;
using Store.OrderService.Data;
using Store.Shared.Services;
using Store.Tests.Unit.TestSupport;
using Xunit;

namespace Store.Tests.Unit.OrderService;

public class OrderServiceTests
{
    private readonly Mock<ILogger<Store.OrderService.Services.OrderService>> _loggerMock = new();
    private static readonly IOptions<ServiceEndpointsOptions> Endpoints = Options.Create(new ServiceEndpointsOptions
    {
        IdentityService = "http://identity.test",
        ProductService = "http://product.test",
        CartService = "http://cart.test",
        OrderService = "http://order.test",
        AuditLogService = "http://audit.test"
    });
    private readonly Mock<IAuditLogClient> _auditLogClientMock = new();
    private readonly OrderDbContext _dbContext;
    private readonly Store.OrderService.Services.OrderService _orderService;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: $"OrderServiceTests-{Guid.NewGuid():N}") // one database per test class instance - xUnit creates one per test
            .Options;
        _dbContext = new OrderDbContext(options);
        _orderService = new Store.OrderService.Services.OrderService(
            _dbContext,
            _loggerMock.Object,
            NoNetworkHttpClient.Create(),
            Endpoints,
            null,
            null,
            _auditLogClientMock.Object
        );
    }

    [Fact]
    public async Task GetOrderByIdAsync_Returns_Error_When_Order_Not_Found()
    {
        var result = await _orderService.GetOrderByIdAsync(999, "user1");
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task GetOrderByIdAsync_Returns_Error_When_Unauthorized()
    {
        // Arrange: Add order for user2
        var order = new Store.Shared.Models.Order { UserId = "user2", UserEmail = "user2@email.com", CreatedAt = System.DateTime.UtcNow };
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();
        // Act
        var result = await _orderService.GetOrderByIdAsync(order.Id, "user1");
        Assert.False(result.IsSuccess);
        Assert.Contains("Unauthorized", result.Message);
    }

    [Fact]
    public async Task GetOrderByIdAsync_Returns_Success_When_Authorized()
    {
        // Arrange: Add order for user1
        var order = new Store.Shared.Models.Order { UserId = "user1", UserEmail = "user1@email.com", CreatedAt = System.DateTime.UtcNow };
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();
        // Act
        var result = await _orderService.GetOrderByIdAsync(order.Id, "user1");
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(order.UserId, result.Data.UserId);
    }
}
