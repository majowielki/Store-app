using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Observability;
using Store.OrderService.Clients;
using Store.OrderService.Data;
using Store.OrderService.Models;
using Xunit;

namespace Store.Tests.Unit.OrderService;

public class OrderServiceTests
{
    private readonly Mock<ILogger<Store.OrderService.Services.OrderService>> _loggerMock = new();

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
            Mock.Of<ICartClient>(),
            Mock.Of<ICatalogClient>(),
            Mock.Of<IPublishEndpoint>(),
            Options.Create(new PricingOptions()),
            new StoreMetrics(),
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task GetOrderAsync_Throws_NotFound_When_Order_Not_Found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _orderService.GetOrderAsync(999, "user1"));
    }

    [Fact]
    public async Task GetOrderAsync_Throws_Forbidden_For_Another_Customers_Order()
    {
        // Arrange: Add order for user2
        var order = new Order { UserId = "user2", UserEmail = "user2@email.com", CreatedAt = DateTime.UtcNow };
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();
        // Act
        await Assert.ThrowsAsync<ForbiddenException>(() => _orderService.GetOrderAsync(order.Id, "user1"));
    }

    [Fact]
    public async Task GetOrderAsync_Returns_The_Customers_Own_Order()
    {
        // Arrange: Add order for user1
        var order = new Order { UserId = "user1", UserEmail = "user1@email.com", CreatedAt = DateTime.UtcNow };
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();
        // Act
        var result = await _orderService.GetOrderAsync(order.Id, "user1");
        Assert.Equal(order.UserId, result.UserId);
    }
}
