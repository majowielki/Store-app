using MassTransit;
using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Persistence;
using Store.Contracts.Catalog.V1;
using Store.ProductService.Data;
using Store.ProductService.Models;
using System.Runtime.CompilerServices;

namespace Store.ProductService.Services;

/// <summary>The visitors waiting for a product that ran out, and telling them when it is back.</summary>
public interface IStockAlerts
{
    /// <summary>Asks for an e-mail when a product that ran out is back; asking twice changes nothing.</summary>
    Task SubscribeAsync(int productId, string email);

    /// <summary>
    /// One <see cref="ProductBackInStock"/> per visitor waiting for any of <paramref name="products"/>
    /// that can be bought again; their alerts go, so the address is kept only as long as it is
    /// needed. Saved with the caller's next save.
    /// </summary>
    Task NotifyBackInStockAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default);
}

public sealed class StockAlerts : IStockAlerts
{
    private readonly ProductDbContext _context;
    private readonly IPublishEndpoint _publish;
    private readonly TimeProvider _time;
    private readonly ILogger<StockAlerts> _logger;

    public StockAlerts(ProductDbContext context, IPublishEndpoint publish, TimeProvider time, ILogger<StockAlerts> logger)
    {
        _context = context;
        _publish = publish;
        _time = time;
        _logger = logger;
    }

    public async Task SubscribeAsync(int productId, string email)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId && p.IsActive)
            ?? throw new NotFoundException(nameof(Product), productId);
        if (product.AvailableQuantity > 0)
        {
            throw new ConflictException($"\"{product.Title}\" is in stock - it can be added to the bag now.");
        }

        // One alert per address and product: a second request finds the first one there
        var alerts = _context.Sql<StockAlert>();
        var productColumn = alerts.Column(a => a.ProductId);
        var emailColumn = alerts.Column(a => a.Email);
        await _context.Database.ExecuteSqlAsync(FormattableStringFactory.Create(
            $"INSERT INTO {alerts.Table} ({productColumn}, {emailColumn}, {alerts.Column(a => a.CreatedAt)}) VALUES ({{0}}, {{1}}, {{2}}) " +
            $"ON CONFLICT ({productColumn}, {emailColumn}) DO NOTHING",
            productId, email.Trim().ToLowerInvariant(), _time.GetUtcNow().UtcDateTime));
    }

    public async Task NotifyBackInStockAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        foreach (var product in products.Where(p => p.IsActive && p.AvailableQuantity > 0).DistinctBy(p => p.Id))
        {
            var alerts = await _context.StockAlerts.Where(a => a.ProductId == product.Id).ToListAsync(cancellationToken);
            foreach (var alert in alerts)
            {
                await _publish.Publish(new ProductBackInStock(
                    product.Id, product.Title, product.Slug, product.Image, product.EffectivePrice, alert.Email, now), cancellationToken);
            }

            _context.StockAlerts.RemoveRange(alerts);
            if (alerts.Count > 0)
            {
                _logger.LogInformation("Product {ProductId} is back: {Count} waiting visitors notified", product.Id, alerts.Count);
            }
        }
    }
}
