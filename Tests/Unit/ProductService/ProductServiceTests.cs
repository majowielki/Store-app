using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.ProductService.Data;
using Store.ProductService.DTOs.Requests;
using Xunit;

namespace Store.Tests.Unit.ProductService;

public class ProductServiceTests
{
    private readonly Mock<ILogger<Store.ProductService.Services.ProductService>> _loggerMock = new();
    private readonly Mock<IAuditTrail> _auditTrailMock = new();
    private readonly ProductDbContext _dbContext;
    private readonly Store.ProductService.Services.ProductService _productService;

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: $"ProductServiceTests-{Guid.NewGuid():N}") // one database per test class instance - xUnit creates one per test
            .Options;
        _dbContext = new ProductDbContext(options);
        _productService = new Store.ProductService.Services.ProductService(
            _dbContext,
            _loggerMock.Object,
            _auditTrailMock.Object
        );
    }

    [Fact]
    public async Task CreateProductAsync_Creates_And_Returns_Product()
    {
        var request = new CreateProductRequest
        {
            Title = "Test Product",
            Description = "A test product description",
            Price = 10.0m,
            Category = 0,
            Company = 0,
            Image = "http://img.com",
            Colors = new System.Collections.Generic.List<string> { "Red" }
        };
        var result = await _productService.CreateProductAsync(request);
        Assert.NotNull(result);
        Assert.Equal(request.Title, result.Title);
    }

    [Fact]
    public async Task UpdateProductAsync_Throws_NotFound_If_Not_Found()
    {
        var request = new UpdateProductRequest { Title = "Updated" };
        await Assert.ThrowsAsync<NotFoundException>(() => _productService.UpdateProductAsync(999, request));
    }

    [Fact]
    public async Task UpdateProductAsync_Updates_Fields()
    {
        // Arrange
        var product = new Store.ProductService.Models.Product
        {
            Title = "Old Title",
            Description = "Old Desc",
            Price = 5.0m,
            Category = 0,
            Company = 0,
            Image = "old.png",
            Colors = new System.Collections.Generic.List<string> { "Blue" },
            CreatedAt = System.DateTime.UtcNow,
            UpdatedAt = System.DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();
        var request = new UpdateProductRequest { Title = "New Title", Price = 20.0m };
        // Act
        var result = await _productService.UpdateProductAsync(product.Id, request);
        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Title", result.Title);
        Assert.Equal(20.0m, result.Price);
    }

    [Fact]
    public async Task DeleteProductAsync_Throws_NotFound_If_Not_Found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _productService.DeleteProductAsync(999));
    }

    [Fact]
    public async Task DeleteProductAsync_Deletes_Product()
    {
        var product = new Store.ProductService.Models.Product
        {
            Title = "To Delete",
            Description = "desc",
            Price = 1.0m,
            Category = 0,
            Company = 0,
            Image = "img.png",
            Colors = new System.Collections.Generic.List<string> { "Green" },
            CreatedAt = System.DateTime.UtcNow,
            UpdatedAt = System.DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();
        await _productService.DeleteProductAsync(product.Id);

        Assert.False((await _dbContext.Products.SingleAsync(p => p.Id == product.Id)).IsActive);
    }
}
