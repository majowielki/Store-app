using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Persistence;
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

    public async Task<WishlistResponse> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var items = await _context.WishlistItems.AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .ThenByDescending(w => w.Id)
            .ToListAsync(cancellationToken);
        return new WishlistResponse
        {
            Items = items.Select(w => new WishlistItemResponse { ProductId = w.ProductId, AddedAt = w.AddedAt }).ToList()
        };
    }

    public async Task<WishlistResponse> AddAsync(string userId, int productId, CancellationToken cancellationToken = default)
    {
        await AddMissingAsync(userId, [productId], refuseUnavailable: true, cancellationToken);
        return await GetAsync(userId, cancellationToken);
    }

    public async Task<WishlistResponse> RemoveAsync(string userId, int productId, CancellationToken cancellationToken = default)
    {
        await _context.WishlistItems.Where(w => w.UserId == userId && w.ProductId == productId).ExecuteDeleteAsync(cancellationToken);
        return await GetAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Merges the list a visitor kept in the browser: products already on the list and products
    /// the catalogue no longer sells are skipped, and the list stops at its limit.
    /// </summary>
    public async Task<WishlistResponse> SyncAsync(string userId, IReadOnlyCollection<int> productIds, CancellationToken cancellationToken = default)
    {
        await AddMissingAsync(userId, productIds, refuseUnavailable: false, cancellationToken);
        return await GetAsync(userId, cancellationToken);
    }

    private async Task AddMissingAsync(string userId, IReadOnlyCollection<int> productIds, bool refuseUnavailable, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.BeginStoreTransactionAsync(cancellationToken);
        await _context.LockKeyAsync($"wishlist:{userId}", cancellationToken);
        await TryAddMissingAsync(userId, productIds, refuseUnavailable, cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }

    private async Task TryAddMissingAsync(string userId, IReadOnlyCollection<int> productIds, bool refuseUnavailable, CancellationToken cancellationToken = default)
    {
        var known = (await _context.WishlistItems.Where(w => w.UserId == userId).Select(w => w.ProductId).ToListAsync(cancellationToken)).ToHashSet();
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

        // The catalogue is asked about every product at once, not one after another
        var candidates = missing.Take(Math.Max(room, 0)).ToList();
        var products = await Task.WhenAll(candidates.Select(id => _catalog.GetSnapshotAsync(id, cancellationToken)));

        var now = _time.GetUtcNow().UtcDateTime;
        for (var i = 0; i < candidates.Count; i++)
        {
            if (products[i] is not { IsActive: true })
            {
                if (refuseUnavailable)
                {
                    throw new DomainValidationException($"Product {candidates[i]} is not available");
                }
                continue;
            }

            _context.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = candidates[i], AddedAt = now });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
