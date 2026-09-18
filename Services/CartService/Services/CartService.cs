using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.BuildingBlocks.Observability;
using Store.CartService.Clients;
using Store.CartService.Data;
using Store.CartService.DTOs.Requests;
using Store.CartService.DTOs.Responses;
using Store.CartService.Models;
using Store.Contracts.Cart;
using Store.Contracts.Catalog;

namespace Store.CartService.Services;

/// <summary>Behaviour of the cart that is configuration rather than code.</summary>
public sealed class CartOptions
{
    public const string SectionName = "Cart";

    /// <summary>
    /// A line whose product snapshot is older than this is refreshed from the catalogue when
    /// the cart is read, so the customer sees today's price before checking out.
    /// </summary>
    public int SnapshotMaxAgeMinutes { get; init; } = 5;
}

public class CartService : ICartService
{
    private readonly CartDbContext _context;
    private readonly ICatalogClient _catalog;
    private readonly CartOptions _options;
    private readonly ILogger<CartService> _logger;
    private readonly IAuditTrail _auditTrail;
    private readonly StoreMetrics _metrics;
    private readonly TimeProvider _time;

    public CartService(
        CartDbContext context,
        ICatalogClient catalog,
        IOptions<CartOptions> options,
        ILogger<CartService> logger,
        IAuditTrail auditTrail,
        StoreMetrics metrics,
        TimeProvider time)
    {
        _context = context;
        _catalog = catalog;
        _options = options.Value;
        _logger = logger;
        _auditTrail = auditTrail;
        _metrics = metrics;
        _time = time;
    }

    public async Task<CartResponse> GetCartAsync(string userId)
    {
        var cart = await FindCartAsync(userId);
        if (cart is null)
        {
            // Nothing is written for a customer who only looks: the cart appears with the first line
            return EmptyCart(userId);
        }

        var priceChanged = await RefreshStaleSnapshotsAsync(cart);
        return MapToCartResponse(cart, priceChanged);
    }

    public async Task<CartResponse> AddItemAsync(string userId, AddCartItemRequest request)
    {
        var product = await _catalog.GetSnapshotAsync(request.ProductId);
        if (product is null || !product.IsActive)
        {
            throw new DomainValidationException($"Product {request.ProductId} is not available");
        }

        var cart = await GetOrCreateCartAsync(userId);
        var item = AddOrMerge(cart, product, request.Color, request.Quantity);
        await _context.SaveChangesAsync();
        _metrics.CartItemsAdded(request.Quantity);

        await AuditAsync("CART_ITEM_ADDED", "CartItem", item.Id.ToString(), userId,
            new { item.ProductId, item.Color, item.Quantity, item.UnitPrice });
        return await ReadBackAsync(cart);
    }

    public async Task<CartResponse> UpdateItemAsync(string userId, int cartItemId, UpdateCartItemRequest request)
    {
        var (cart, item) = await FindLineAsync(userId, cartItemId);

        var now = _time.GetUtcNow().UtcDateTime;
        if (request.Quantity.HasValue)
        {
            item.Quantity = request.Quantity.Value;
        }
        if (!string.IsNullOrEmpty(request.Color))
        {
            item.Color = request.Color;
        }
        item.UpdatedAt = now;
        cart.UpdatedAt = now;
        await _context.SaveChangesAsync();

        await AuditAsync("CART_ITEM_UPDATED", "CartItem", cartItemId.ToString(), userId,
            new { item.ProductId, item.Color, item.Quantity });
        return await ReadBackAsync(cart);
    }

    public async Task<CartResponse> RemoveItemAsync(string userId, int cartItemId)
    {
        var (cart, item) = await FindLineAsync(userId, cartItemId);

        cart.Items.Remove(item);
        _context.CartItems.Remove(item);
        cart.UpdatedAt = _time.GetUtcNow().UtcDateTime;
        await _context.SaveChangesAsync();

        await AuditAsync("CART_ITEM_REMOVED", "CartItem", cartItemId.ToString(), userId, new { item.ProductId });
        return await ReadBackAsync(cart);
    }

    public async Task ClearCartAsync(string userId)
    {
        var cart = await FindCartAsync(userId);
        if (cart is null || cart.Items.Count == 0)
        {
            // Already empty - clearing it again changes nothing
            return;
        }

        _context.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = _time.GetUtcNow().UtcDateTime;
        await _context.SaveChangesAsync();

        await AuditAsync("CART_CLEARED", "Cart", cart.Id.ToString(), userId, null);
    }

    public Task<int> GetItemCountAsync(string userId)
        => _context.CartItems.Where(ci => ci.Cart.UserId == userId).SumAsync(ci => ci.Quantity);

    public Task<decimal> GetTotalAsync(string userId)
        => _context.CartItems.Where(ci => ci.Cart.UserId == userId).SumAsync(ci => ci.UnitPrice * ci.Quantity);

    public async Task<CartResponse> SyncCartAsync(string userId, SyncCartRequest request)
    {
        if (request.Items.Count == 0)
        {
            // Nothing to merge: the server cart as it is
            return await GetCartAsync(userId);
        }

        var cart = await GetOrCreateCartAsync(userId);
        foreach (var item in request.Items)
        {
            var product = await _catalog.GetSnapshotAsync(item.ProductId);
            if (product is null || !product.IsActive)
            {
                // A guest cart may hold a product that left the catalogue since; the rest still merges
                _logger.LogWarning("Skipping sync item - product not found: {ProductId}", item.ProductId);
                continue;
            }
            AddOrMerge(cart, product, item.Color, item.Quantity);
        }
        await _context.SaveChangesAsync();

        return MapToCartResponse(cart, priceChanged: false);
    }

    public async Task<CartSnapshot?> GetSnapshotAsync(string userId)
    {
        var cart = await _context.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        return cart is null
            ? null
            : new CartSnapshot(
                cart.UserId,
                cart.Items.Select(i => new CartLineSnapshot(i.ProductId, i.Title, i.Image, i.Company, i.Color, i.UnitPrice, i.Quantity)).ToList(),
                cart.UpdatedAt);
    }

    public async Task<int> ClearAfterOrderAsync(string userId, int orderId)
    {
        var cart = await FindCartAsync(userId);
        if (cart is null || cart.Items.Count == 0)
        {
            return 0;
        }

        var removed = cart.Items.Count;
        _context.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = _time.GetUtcNow().UtcDateTime;
        await _context.SaveChangesAsync();

        await AuditAsync("CART_CLEARED", "Cart", cart.Id.ToString(), userId, new { OrderId = orderId, Lines = removed });
        return removed;
    }

    private Task<Cart?> FindCartAsync(string userId)
        => _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);

    private async Task<Cart> GetOrCreateCartAsync(string userId)
    {
        var cart = await FindCartAsync(userId);
        if (cart != null)
        {
            return cart;
        }

        var now = _time.GetUtcNow().UtcDateTime;
        cart = new Cart { UserId = userId, CreatedAt = now, UpdatedAt = now };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }

    /// <summary>A line of the user's own cart; lines of other carts are as unknown as missing ones.</summary>
    private async Task<(Cart Cart, CartItem Item)> FindLineAsync(string userId, int cartItemId)
    {
        var cart = await FindCartAsync(userId);
        var item = cart?.Items.FirstOrDefault(ci => ci.Id == cartItemId);
        return item is null
            ? throw new NotFoundException("Cart item", cartItemId)
            : (cart!, item);
    }

    /// <summary>The cart after a change, with the same price refresh a plain read gets.</summary>
    private async Task<CartResponse> ReadBackAsync(Cart cart)
        => MapToCartResponse(cart, await RefreshStaleSnapshotsAsync(cart));

    /// <summary>
    /// Adds a line for the product and colour, or raises the quantity of the line that already
    /// exists. Either way the line carries the product as the catalogue describes it now.
    /// </summary>
    private CartItem AddOrMerge(Cart cart, ProductSnapshot product, string color, int quantity)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var item = cart.Items.FirstOrDefault(ci => ci.ProductId == product.Id && ci.Color == color);

        if (item is null)
        {
            item = new CartItem
            {
                ProductId = product.Id,
                Color = color,
                Quantity = quantity,
                CreatedAt = now
            };
            cart.Items.Add(item);
        }
        else
        {
            item.Quantity += quantity;
        }

        item.ApplySnapshot(product, now);
        cart.UpdatedAt = now;
        return item;
    }

    /// <summary>
    /// Refreshes lines whose snapshot is older than the configured age. A catalogue that is
    /// unreachable leaves the cart as it was: stale prices are re-checked at checkout anyway.
    /// </summary>
    private async Task<bool> RefreshStaleSnapshotsAsync(Cart cart)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var maxAge = TimeSpan.FromMinutes(_options.SnapshotMaxAgeMinutes);
        var stale = cart.Items.Where(i => now - i.SnapshotAt > maxAge).ToList();
        if (stale.Count == 0)
        {
            return false;
        }

        var priceChanged = false;
        var repricedLines = 0;
        foreach (var item in stale)
        {
            ProductSnapshot? product;
            try
            {
                product = await _catalog.GetSnapshotAsync(item.ProductId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Could not refresh product {ProductId} for the cart; keeping the stored snapshot", item.ProductId);
                continue;
            }

            if (product is null || !product.IsActive)
            {
                // The product left the catalogue: keep the line, the checkout will report it
                continue;
            }

            if (product.EffectivePrice != item.UnitPrice)
            {
                priceChanged = true;
                repricedLines++;
            }
            item.ApplySnapshot(product, now);
        }

        await _context.SaveChangesAsync();
        if (repricedLines > 0) _metrics.PriceMismatch(repricedLines, "cart");
        return priceChanged;
    }

    private Task AuditAsync(string action, string entityName, string? entityId, string userId, object? details)
        => _auditTrail.RecordAsync(action, entityName, entityId, userId, details);

    private CartResponse EmptyCart(string userId) => new()
    {
        UserId = userId,
        IsEmpty = true,
        UpdatedAt = _time.GetUtcNow().UtcDateTime
    };

    private static CartResponse MapToCartResponse(Cart cart, bool priceChanged)
    {
        return new CartResponse
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = cart.Items.Select(MapToCartItemResponse).ToList(),
            TotalItems = cart.TotalItems,
            Total = cart.Total,
            UpdatedAt = cart.UpdatedAt,
            IsEmpty = cart.IsEmpty,
            PriceChanged = priceChanged
        };
    }

    private static CartItemResponse MapToCartItemResponse(CartItem item)
    {
        return new CartItemResponse
        {
            Id = item.Id,
            ProductId = item.ProductId,
            Title = item.Title,
            Image = item.Image,
            Price = item.UnitPrice,
            Quantity = item.Quantity,
            Color = item.Color,
            Company = item.Company,
            LineTotal = item.LineTotal,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
