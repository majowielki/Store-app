using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Api;
using Store.CartService.Clients;
using Store.CartService.Data;
using Store.CartService.DTOs.Requests;
using Store.CartService.DTOs.Responses;
using Store.CartService.Models;
using Store.Contracts.Cart;
using Store.Contracts.Catalog;
using Store.Shared.Models;
using Store.Shared.Services;
using System.Text.Json;

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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly CartDbContext _context;
    private readonly ICatalogClient _catalog;
    private readonly CartOptions _options;
    private readonly ILogger<CartService> _logger;
    private readonly IAuditLogClient _auditLogClient;

    public CartService(
        CartDbContext context,
        ICatalogClient catalog,
        IOptions<CartOptions> options,
        ILogger<CartService> logger,
        IAuditLogClient auditLogClient)
    {
        _context = context;
        _catalog = catalog;
        _options = options.Value;
        _logger = logger;
        _auditLogClient = auditLogClient;
    }

    public async Task<ApiResponse<CartResponse?>> GetCartByUserIdAsync(string userId)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return ApiResponse<CartResponse?>.Error("Cart not found");

            var priceChanged = await RefreshStaleSnapshotsAsync(cart);

            return ApiResponse<CartResponse?>.Success(MapToCartResponse(cart, priceChanged));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart for user: {UserId}", userId);
            await AuditAsync("CART_RETRIEVE_FAILED", "Cart", null, userId, new { Exception = ex.Message });
            return ApiResponse<CartResponse?>.Error("An error occurred while retrieving the cart.");
        }
    }

    public async Task<ApiResponse<CartResponse>> CreateCartAsync(string userId)
    {
        try
        {
            var cart = await GetOrCreateCartAsync(userId);
            await AuditAsync("CART_CREATED", "Cart", cart.Id.ToString(), userId, new { cart.Id });
            return ApiResponse<CartResponse>.Success(MapToCartResponse(cart, priceChanged: false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cart for user: {UserId}", userId);
            await AuditAsync("CART_CREATION_FAILED", "Cart", null, userId, new { Exception = ex.Message });
            return ApiResponse<CartResponse>.Error("An error occurred while creating the cart.");
        }
    }

    public async Task<ApiResponse<CartItemResponse>> AddItemToCartAsync(string userId, AddCartItemRequest request)
    {
        try
        {
            var product = await _catalog.GetSnapshotAsync(request.ProductId);
            if (product is null || !product.IsActive)
            {
                return ApiResponse<CartItemResponse>.Error($"Product with ID {request.ProductId} not found");
            }

            var cart = await GetOrCreateCartAsync(userId);
            var item = AddOrMerge(cart, product, request.Color, request.Quantity);
            await _context.SaveChangesAsync();

            await AuditAsync("CART_ITEM_ADDED", "CartItem", item.Id.ToString(), userId,
                new { item.ProductId, item.Color, item.Quantity, item.UnitPrice });
            return ApiResponse<CartItemResponse>.Success(MapToCartItemResponse(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart for user: {UserId}, Product: {ProductId}", userId, request.ProductId);
            await AuditAsync("CART_ITEM_ADD_FAILED", "CartItem", null, userId, new { request.ProductId, Exception = ex.Message });
            return ApiResponse<CartItemResponse>.Error("An error occurred while adding item to cart.");
        }
    }

    public async Task<ApiResponse<CartItemResponse?>> UpdateCartItemAsync(string userId, int cartItemId, UpdateCartItemRequest request)
    {
        try
        {
            var item = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

            if (item == null) return ApiResponse<CartItemResponse?>.Error("Cart item not found");

            var now = DateTime.UtcNow;
            if (request.Quantity.HasValue)
            {
                item.Quantity = request.Quantity.Value;
            }
            if (!string.IsNullOrEmpty(request.Color))
            {
                item.Color = request.Color;
            }
            item.UpdatedAt = now;
            item.Cart.UpdatedAt = now;
            await _context.SaveChangesAsync();

            await AuditAsync("CART_ITEM_UPDATED", "CartItem", cartItemId.ToString(), userId,
                new { item.ProductId, item.Color, item.Quantity });
            return ApiResponse<CartItemResponse?>.Success(MapToCartItemResponse(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item: {CartItemId} for user: {UserId}", cartItemId, userId);
            await AuditAsync("CART_ITEM_UPDATE_FAILED", "CartItem", cartItemId.ToString(), userId, new { Exception = ex.Message });
            return ApiResponse<CartItemResponse?>.Error("An error occurred while updating cart item.");
        }
    }

    public async Task<ApiResponse<bool>> RemoveItemFromCartAsync(string userId, int cartItemId)
    {
        try
        {
            var item = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

            if (item == null) return ApiResponse<bool>.Error("Cart item not found");

            _context.CartItems.Remove(item);
            item.Cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await AuditAsync("CART_ITEM_REMOVED", "CartItem", cartItemId.ToString(), userId, new { item.ProductId });
            return ApiResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item: {CartItemId} for user: {UserId}", cartItemId, userId);
            await AuditAsync("CART_ITEM_REMOVE_FAILED", "CartItem", cartItemId.ToString(), userId, new { Exception = ex.Message });
            return ApiResponse<bool>.Error("An error occurred while removing cart item.");
        }
    }

    public async Task<ApiResponse<bool>> ClearCartAsync(string userId)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return ApiResponse<bool>.Error("Cart not found");

            _context.CartItems.RemoveRange(cart.Items);
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await AuditAsync("CART_CLEARED", "Cart", cart.Id.ToString(), userId, null);
            return ApiResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for user: {UserId}", userId);
            await AuditAsync("CART_CLEAR_FAILED", "Cart", null, userId, new { Exception = ex.Message });
            return ApiResponse<bool>.Error("An error occurred while clearing cart.");
        }
    }

    public async Task<ApiResponse<int>> GetCartItemCountAsync(string userId)
    {
        try
        {
            var count = await _context.CartItems
                .Where(ci => ci.Cart.UserId == userId)
                .SumAsync(ci => ci.Quantity);
            return ApiResponse<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart item count for user: {UserId}", userId);
            return ApiResponse<int>.Error("An error occurred while getting cart item count.");
        }
    }

    public async Task<ApiResponse<decimal>> GetCartTotalAsync(string userId)
    {
        try
        {
            var total = await _context.CartItems
                .Where(ci => ci.Cart.UserId == userId)
                .SumAsync(ci => ci.UnitPrice * ci.Quantity);
            return ApiResponse<decimal>.Success(total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart total for user: {UserId}", userId);
            return ApiResponse<decimal>.Error("An error occurred while getting cart total.");
        }
    }

    public async Task<ApiResponse<CartResponse>> SyncCartAsync(string userId, SyncCartRequest request)
    {
        if (request == null || request.Items == null || request.Items.Count == 0)
        {
            return ApiResponse<CartResponse>.ValidationError(new List<string> { "Sync request must contain at least one item" });
        }
        try
        {
            var cart = await GetOrCreateCartAsync(userId);

            foreach (var item in request.Items)
            {
                var product = await _catalog.GetSnapshotAsync(item.ProductId);
                if (product is null || !product.IsActive)
                {
                    _logger.LogWarning("Skipping sync item - product not found: {ProductId}", item.ProductId);
                    continue;
                }
                AddOrMerge(cart, product, item.Color, item.Quantity);
            }
            await _context.SaveChangesAsync();
            return ApiResponse<CartResponse>.Success(MapToCartResponse(cart, priceChanged: false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing cart for user: {UserId}", userId);
            return ApiResponse<CartResponse>.Error("An error occurred while syncing cart.");
        }
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

    private async Task<Cart> GetOrCreateCartAsync(string userId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart != null)
        {
            return cart;
        }

        var now = DateTime.UtcNow;
        cart = new Cart { UserId = userId, CreatedAt = now, UpdatedAt = now };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }

    /// <summary>
    /// Adds a line for the product and colour, or raises the quantity of the line that already
    /// exists. Either way the line carries the product as the catalogue describes it now.
    /// </summary>
    private static CartItem AddOrMerge(Cart cart, ProductSnapshot product, string color, int quantity)
    {
        var now = DateTime.UtcNow;
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
        var now = DateTime.UtcNow;
        var maxAge = TimeSpan.FromMinutes(_options.SnapshotMaxAgeMinutes);
        var stale = cart.Items.Where(i => now - i.SnapshotAt > maxAge).ToList();
        if (stale.Count == 0)
        {
            return false;
        }

        var priceChanged = false;
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

            priceChanged |= product.EffectivePrice != item.UnitPrice;
            item.ApplySnapshot(product, now);
        }

        await _context.SaveChangesAsync();
        return priceChanged;
    }

    private async Task AuditAsync(string action, string entityName, string? entityId, string userId, object? details)
    {
        await _auditLogClient.CreateAuditLogAsync(new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            AdditionalInfo = JsonSerializer.Serialize(new { Source = "CartService", Details = details }, JsonOptions)
        });
    }

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
