using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.BuildingBlocks.Observability;
using Store.CartService.Clients;
using Store.CartService.Data;
using Store.CartService.Services;
using Store.Contracts.Catalog;
using Xunit;

namespace Store.Tests.Unit.CartService;

public class CartServiceTests
{
    private readonly Mock<ILogger<Store.CartService.Services.CartService>> _loggerMock = new();
    private readonly Mock<IAuditTrail> _auditTrailMock = new();
    private readonly Mock<ICatalogClient> _catalogMock = new();
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
            _catalogMock.Object,
            Options.Create(new CartOptions()),
            _loggerMock.Object,
            _auditTrailMock.Object,
            new StoreMetrics(),
            TimeProvider.System
        );
    }

    private static ProductSnapshot Snapshot(int id, decimal effectivePrice, bool isActive = true)
        => new(id, $"Product {id}", "https://example.test/p.jpg", "Modenza", new[] { "black" }, effectivePrice, effectivePrice, isActive, DateTime.UtcNow);

    [Fact]
    public async Task GetCartAsync_Returns_An_Empty_Cart_Without_Creating_One()
    {
        var result = await _cartService.GetCartAsync("user1");

        Assert.True(result.IsEmpty);
        Assert.Empty(result.Items);
        Assert.Empty(_dbContext.Carts);
    }

    [Fact]
    public async Task AddItemAsync_Creates_The_Cart_With_The_First_Line()
    {
        _catalogMock.Setup(c => c.GetSnapshotAsync(6, It.IsAny<CancellationToken>())).ReturnsAsync(Snapshot(6, 5m));

        var result = await _cartService.AddItemAsync("user2", new() { ProductId = 6, Quantity = 1, Color = "black" });

        Assert.Equal("user2", result.UserId);
        Assert.Single(result.Items);
        Assert.Equal("user2", (await _dbContext.Carts.SingleAsync()).UserId);
    }

    [Fact]
    public async Task AddItemAsync_Stores_A_Snapshot_Of_The_Product()
    {
        _catalogMock.Setup(c => c.GetSnapshotAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(Snapshot(7, 49.99m));

        var result = await _cartService.AddItemAsync("user3", new() { ProductId = 7, Quantity = 2, Color = "black" });

        Assert.Equal(2, result.TotalItems);
        var item = await _dbContext.CartItems.SingleAsync();
        Assert.Equal("Product 7", item.Title);
        Assert.Equal(49.99m, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public async Task AddItemAsync_Merges_The_Same_Product_And_Colour()
    {
        _catalogMock.Setup(c => c.GetSnapshotAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(Snapshot(7, 10m));

        await _cartService.AddItemAsync("user4", new() { ProductId = 7, Quantity = 1, Color = "black" });
        await _cartService.AddItemAsync("user4", new() { ProductId = 7, Quantity = 2, Color = "black" });
        await _cartService.AddItemAsync("user4", new() { ProductId = 7, Quantity = 1, Color = "white" });

        var items = await _dbContext.CartItems.OrderBy(i => i.Id).ToListAsync();
        Assert.Equal(2, items.Count);
        Assert.Equal(3, items[0].Quantity);
        Assert.Equal(1, items[1].Quantity);
    }

    [Fact]
    public async Task AddItemAsync_Rejects_A_Product_The_Catalogue_Deleted()
    {
        _catalogMock.Setup(c => c.GetSnapshotAsync(8, It.IsAny<CancellationToken>())).ReturnsAsync(Snapshot(8, 10m, isActive: false));

        await Assert.ThrowsAsync<DomainValidationException>(() => _cartService.AddItemAsync("user5", new() { ProductId = 8, Quantity = 1, Color = "black" }));

        Assert.Empty(_dbContext.CartItems);
    }

    [Fact]
    public async Task Reading_The_Cart_Refreshes_Stale_Prices_From_The_Catalogue()
    {
        _catalogMock.Setup(c => c.GetSnapshotAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(Snapshot(9, 100m));
        await _cartService.AddItemAsync("user6", new() { ProductId = 9, Quantity = 1, Color = "black" });

        var item = await _dbContext.CartItems.SingleAsync();
        item.SnapshotAt = DateTime.UtcNow.AddHours(-1);
        await _dbContext.SaveChangesAsync();
        _catalogMock.Setup(c => c.GetSnapshotAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(Snapshot(9, 80m));

        var result = await _cartService.GetCartAsync("user6");

        Assert.True(result.PriceChanged);
        Assert.Equal(80m, result.Items.Single().Price);
    }

    [Fact]
    public async Task Reading_The_Cart_Keeps_The_Snapshot_When_The_Catalogue_Is_Down()
    {
        _catalogMock.Setup(c => c.GetSnapshotAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(Snapshot(10, 100m));
        await _cartService.AddItemAsync("user7", new() { ProductId = 10, Quantity = 1, Color = "black" });

        var item = await _dbContext.CartItems.SingleAsync();
        item.SnapshotAt = DateTime.UtcNow.AddHours(-1);
        await _dbContext.SaveChangesAsync();
        _catalogMock.Setup(c => c.GetSnapshotAsync(10, It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("down"));

        var result = await _cartService.GetCartAsync("user7");

        Assert.False(result.PriceChanged);
        Assert.Equal(100m, result.Items.Single().Price);
    }
}
