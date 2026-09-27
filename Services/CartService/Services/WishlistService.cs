using Microsoft.EntityFrameworkCore;
using Npgsql;
using Store.BuildingBlocks.Api;
using Store.CartService.Clients;
using Store.CartService.Data;
using Store.CartService.DTOs.Responses;
using Store.CartService.Models;

namespace Store.CartService.Services;

/// <summary>
/// The customer's wishlist: product ids kept for later, at most <see cref="WishlistItem.MaxItems"/>.
/// Adding a product twice changes nothing; a product must exist and be for sale to be added.
/// </summary>
public sealed class WishlistService
{
    private readonly CartDbContext _context;
    private readonly ICatalogClient _catalog;
    private readonly TimeProvider _time;

    public WishlistService(CartDbContext context, ICatalogClient catalog, TimeProvider time)
    {
        _context = context;
        _catalog = catalog;
        _time = time;
    }

    public async Task<WishlistResponse> GetAsync(string userId)
    {
        var items = await _context.WishlistItems.AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .ThenByDescending(w => w.Id)
            .ToListAsync();
        return new WishlistResponse
        {
            Items = items.Select(w => new WishlistItemResponse { ProductId = w.ProductId, AddedAt = w.AddedAt }).ToList()
        };
    }

    public async Task<WishlistResponse> AddAsync(string userId, int productId)
    {
        await AddMissingAsync(userId, [productId], refuseUnavailable: true);
        return await GetAsync(userId);
    }

    public async Task<WishlistResponse> RemoveAsync(string userId, int productId)
    {
        await _context.WishlistItems.Where(w => w.UserId == userId && w.ProductId == productId).ExecuteDeleteAsync();
        return await GetAsync(userId);
    }

    /// <summary>
    /// Merges the list a visitor kept in the browser: products already on the list and products
    /// the catalogue no longer sells are skipped, and the list stops at its limit.
    /// </summary>
    public async Task<WishlistResponse> SyncAsync(string userId, IReadOnlyCollection<int> productIds)
    {
        await AddMissingAsync(userId, productIds, refuseUnavailable: false);
        return await GetAsync(userId);
    }

    private async Task AddMissingAsync(string userId, IReadOnlyCollection<int> productIds, bool refuseUnavailable)
    {
        var known = (await _context.WishlistItems.Where(w => w.UserId == userId).Select(w => w.ProductId).ToListAsync()).ToHashSet();
        var missing = productIds.Distinct().Where(id => !known.Contains(id)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var room = WishlistItem.MaxItems - known.Count;
        if (refuseUnavailable && room <= 0)
        {
            throw new DomainValidationException($"A wishlist holds at most {WishlistItem.MaxItems} products. Remove one to add another.");
        }

        var now = _time.GetUtcNow().UtcDateTime;
        foreach (var productId in missing.Take(Math.Max(room, 0)))
        {
            var product = await _catalog.GetSnapshotAsync(productId);
            if (product is null || !product.IsActive)
            {
                if (refuseUnavailable)
                {
                    throw new DomainValidationException($"Product {productId} is not available");
                }
                continue;
            }

            _context.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId, AddedAt = now });
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // The same product added twice at once, from two tabs: it is on the list either way
            _context.ChangeTracker.Clear();
        }
    }
}
