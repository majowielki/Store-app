using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Store.CartService.Data;
using Store.CartService.DTOs.Requests;
using Store.CartService.DTOs.Responses;
using Store.Shared.Models;
using Store.Shared.Services;

namespace Store.CartService.Services;

#nullable enable

public class CartService : ICartService
{
    private readonly CartDbContext _context;
    private readonly ILogger<CartService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IAuditLogClient _auditLogClient;

    private static readonly System.Text.Json.JsonSerializerOptions AuditJsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    public CartService(
        CartDbContext context,
        ILogger<CartService> logger,
        HttpClient httpClient,
        IConfiguration configuration,
        IAuditLogClient auditLogClient)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
        _auditLogClient = auditLogClient;
    }

    public async Task<ApiResponse<CartResponse?>> GetCartByUserIdAsync(string userId)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return ApiResponse<CartResponse?>.Error("Cart not found");

            return ApiResponse<CartResponse?>.Success(MapToCartResponse(cart));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart for user: {UserId}", userId);
            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "CART_RETRIEVE_FAILED",
                EntityName = "Cart",
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Exception = ex.Message, Source = "CartService" })
            });
            return ApiResponse<CartResponse?>.Error("An error occurred while retrieving the cart.");
        }
    }

    public async Task<ApiResponse<CartResponse>> CreateCartAsync(string userId)
    {
        try
        {
            var cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "CART_CREATED",
                EntityName = "Cart",
                EntityId = cart.Id.ToString(),
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                NewValues = System.Text.Json.JsonSerializer.Serialize(cart, AuditJsonOptions),
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Source = "CartService" }, AuditJsonOptions)
            });

            return ApiResponse<CartResponse>.Success(MapToCartResponse(cart));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cart for user: {UserId}", userId);
            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "CART_CREATION_FAILED",
                EntityName = "Cart",
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Exception = ex.Message, Source = "CartService" }, AuditJsonOptions)
            });
            return ApiResponse<CartResponse>.Error("An error occurred while creating the cart.");
        }
    }

    public async Task<ApiResponse<CartItemResponse>> AddItemToCartAsync(string userId, AddCartItemRequest request)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var product = await GetOrCreateProductAsync(request.ProductId);
            if (product == null)
            {
                return ApiResponse<CartItemResponse>.Error($"Product with ID {request.ProductId} not found");
            }

            var existingItem = cart.CartItems.FirstOrDefault(ci =>
                ci.ProductId == request.ProductId &&
                ci.ProductColor == request.Color);

            CartItem cartItem;

            if (existingItem != null)
            {
                existingItem.Amount += request.Quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
                cartItem = existingItem;
            }
            else
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = request.ProductId,
                    Title = product.Title,
                    Image = product.Image,
                    Price = product.Price,
                    Amount = request.Quantity,
                    ProductColor = request.Color,
                    Company = product.Company.ToString(),
                    Product = product,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.CartItems.Add(cartItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "CART_ITEM_ADDED",
                EntityName = "CartItem",
                EntityId = cartItem.Id.ToString(),
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                NewValues = System.Text.Json.JsonSerializer.Serialize(cartItem, AuditJsonOptions),
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Source = "CartService" }, AuditJsonOptions)
            });
            return ApiResponse<CartItemResponse>.Success(MapToCartItemResponse(cartItem));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart for user: {UserId}, Product: {ProductId}", userId, request.ProductId);
            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "CART_ITEM_ADD_FAILED",
                EntityName = "CartItem",
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Exception = ex.Message, Source = "CartService" })
            });
            return ApiResponse<CartItemResponse>.Error("An error occurred while adding item to cart.");
        }
    }

    public async Task<ApiResponse<CartItemResponse?>> UpdateCartItemAsync(string userId, int cartItemId, UpdateCartItemRequest request)
    {
        try
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null) return ApiResponse<CartItemResponse?>.Error("Cart item not found");

            if (request.Quantity.HasValue)
            {
                cartItem.Amount = request.Quantity.Value;
            }
            if (!string.IsNullOrEmpty(request.Color))
            {
                cartItem.ProductColor = request.Color;
            }
            cartItem.UpdatedAt = DateTime.UtcNow;
            cartItem.Cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "CART_ITEM_UPDATED",
                EntityName = "CartItem",
                EntityId = cartItemId.ToString(),
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                NewValues = System.Text.Json.JsonSerializer.Serialize(cartItem, AuditJsonOptions),
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Source = "CartService" }, AuditJsonOptions)
            });
            return ApiResponse<CartItemResponse?>.Success(MapToCartItemResponse(cartItem));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item: {CartItemId} for user: {UserId}", cartItemId, userId);
            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "CART_ITEM_UPDATE_FAILED",
                EntityName = "CartItem",
                EntityId = cartItemId.ToString(),
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Exception = ex.Message, Source = "CartService" })
            });
            return ApiResponse<CartItemResponse?>.Error("An error occurred while updating cart item.");
        }
    }

    public async Task<ApiResponse<bool>> RemoveItemFromCartAsync(string userId, int cartItemId)
    {
        try
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null) return ApiResponse<bool>.Error("Cart item not found");

            _context.CartItems.Remove(cartItem);
            cartItem.Cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "CART_ITEM_REMOVED",
                EntityName = "CartItem",
                EntityId = cartItemId.ToString(),
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Source = "CartService" }, AuditJsonOptions)
            });
            return ApiResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item: {CartItemId} for user: {UserId}", cartItemId, userId);
            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "CART_ITEM_REMOVE_FAILED",
                EntityName = "CartItem",
                EntityId = cartItemId.ToString(),
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Exception = ex.Message, Source = "CartService" })
            });
            return ApiResponse<bool>.Error("An error occurred while removing cart item.");
        }
    }

    public async Task<ApiResponse<bool>> ClearCartAsync(string userId)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return ApiResponse<bool>.Error("Cart not found");

            _context.CartItems.RemoveRange(cart.CartItems);
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "CART_CLEARED",
                EntityName = "Cart",
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Source = "CartService" }, AuditJsonOptions)
            });
            return ApiResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for user: {UserId}", userId);
            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "CART_CLEAR_FAILED",
                EntityName = "Cart",
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Exception = ex.Message, Source = "CartService" })
            });
            return ApiResponse<bool>.Error("An error occurred while clearing cart.");
        }
    }

    public async Task<ApiResponse<int>> GetCartItemCountAsync(string userId)
    {
        try
        {
            var count = await _context.CartItems
                .Where(ci => ci.Cart.UserId == userId)
                .SumAsync(ci => ci.Amount);
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
                .SumAsync(ci => ci.LineTotal);
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
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            foreach (var item in request.Items)
            {
                var product = await GetOrCreateProductAsync(item.ProductId);
                if (product == null)
                {
                    _logger.LogWarning("Skipping sync item - product not found: {ProductId}", item.ProductId);
                    continue;
                }
                var existingItem = cart.CartItems.FirstOrDefault(ci =>
                    ci.ProductId == item.ProductId &&
                    ci.ProductColor == item.Color);
                if (existingItem != null)
                {
                    existingItem.Amount += item.Quantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = item.ProductId,
                        Title = product.Title,
                        Image = product.Image,
                        Price = product.Price,
                        Amount = item.Quantity,
                        ProductColor = item.Color,
                        Company = product.Company.ToString(),
                        Product = product,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.CartItems.Add(newItem);
                    cart.CartItems.Add(newItem);
                }
            }
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstAsync(c => c.Id == cart.Id);
            return ApiResponse<CartResponse>.Success(MapToCartResponse(cart));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing cart for user: {UserId}", userId);
            return ApiResponse<CartResponse>.Error("An error occurred while syncing cart.");
        }
    }

    private async Task<Product?> GetOrCreateProductAsync(int productId)
    {
        // First, check if product exists in local database
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        
        // If we have a product but it's incomplete (from older buggy inserts), try to refresh it
        if (product != null)
        {
            if (string.IsNullOrWhiteSpace(product.Title) || string.IsNullOrWhiteSpace(product.Image) || product.Price <= 0)
            {
                try
                {
                    var refreshed = await FetchProductFromProductServiceAsync(productId);
                    if (refreshed != null)
                    {
                        product.Title = refreshed.Title;
                        product.Description = refreshed.Description;
                        product.Image = refreshed.Image;
                        product.Price = refreshed.Price;
                        product.Category = refreshed.Category;
                        product.Company = refreshed.Company;
                        product.Colors = refreshed.Colors ?? new List<string>();
                        product.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to refresh incomplete product {ProductId}", productId);
                }
            }
            return product;
        }

        // If not found locally, try to fetch from Product Service
        try
        {
            var fetched = await FetchProductFromProductServiceAsync(productId);
            if (fetched != null)
            {
                product = fetched;
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return product;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch product {ProductId} from Product Service", productId);
        }

        return null;
    }

    private async Task<Product?> FetchProductFromProductServiceAsync(int productId)
    {
        // Read base URL from config supporting both nested and flat keys. Defaults to Docker service DNS name.
        var productServiceUrl =
            _configuration["Services:ProductService:BaseUrl"] // e.g., Services:ProductService:BaseUrl=http://productservice:5003
            ?? _configuration["Services:ProductService"]      // e.g., Services:ProductService=http://productservice:5003
            ?? "http://productservice:5003";                  // sensible default for container network

        _logger.LogDebug("Fetching product {ProductId} from ProductService at {BaseUrl}", productId, productServiceUrl);
        var response = await _httpClient.GetAsync($"{productServiceUrl.TrimEnd('/')}/api/products/{productId}");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var single = await response.Content.ReadFromJsonAsync<SingleProductResponseDto>();
        var attr = single?.Data?.Attributes;
        if (single?.Data == null || attr == null)
        {
            return null;
        }

        // Parse and map fields
        decimal price = 0m;
        if (!string.IsNullOrWhiteSpace(attr.Price))
            decimal.TryParse(attr.Price, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out price);

        var category = Store.Shared.Utility.Category.All;
        if (!string.IsNullOrWhiteSpace(attr.Category))
            Enum.TryParse(attr.Category, true, out category);

        var company = Store.Shared.Utility.Company.All;
        if (!string.IsNullOrWhiteSpace(attr.Company))
            Enum.TryParse(attr.Company, true, out company);

        return new Product
        {
            Id = single.Data.Id,
            Title = attr.Title,
            Description = attr.Description ?? string.Empty,
            Image = attr.Image,
            Price = price,
            Category = category,
            Company = company,
            Colors = attr.Colors ?? new List<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static CartResponse MapToCartResponse(Cart cart)
    {
        return new CartResponse
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = cart.CartItems.Select(MapToCartItemResponse).ToList(),
            TotalItems = cart.TotalItems,
            Total = cart.Total,
            UpdatedAt = cart.UpdatedAt,
            IsEmpty = cart.IsEmpty
        };
    }

    private static CartItemResponse MapToCartItemResponse(CartItem cartItem)
    {
        // Fallback to Product fields if stored snapshot is incomplete
        var title = string.IsNullOrWhiteSpace(cartItem.Title) ? cartItem.Product?.Title ?? string.Empty : cartItem.Title;
        var image = string.IsNullOrWhiteSpace(cartItem.Image) ? cartItem.Product?.Image ?? string.Empty : cartItem.Image;
        var price = cartItem.Price <= 0 && cartItem.Product != null ? cartItem.Product.Price : cartItem.Price;
        var company = string.IsNullOrWhiteSpace(cartItem.Company) && cartItem.Product != null ? cartItem.Product.Company.ToString() : cartItem.Company;
        return new CartItemResponse
        {
            Id = cartItem.Id,
            ProductId = cartItem.ProductId,
            Title = title,
            Image = image,
            Price = price,
            Quantity = cartItem.Amount,
            Color = cartItem.ProductColor,
            Company = company,
            LineTotal = cartItem.LineTotal,
            CreatedAt = cartItem.CreatedAt,
            UpdatedAt = cartItem.UpdatedAt
        };
    }
}

// DTO for external Product Service calls
public class ProductDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Image { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Store.Shared.Utility.Category Category { get; set; }
    public Store.Shared.Utility.Company Company { get; set; }
    public List<string>? Colors { get; set; }
}

// DTOs matching ProductService's frontend response (SingleProductResponse)
public class SingleProductResponseDto
{
    public ProductDataDto? Data { get; set; }
    public object? Meta { get; set; }
}

public class ProductDataDto
{
    public int Id { get; set; }
    public ProductAttributesDto? Attributes { get; set; }
}

public class ProductAttributesDto
{
    public string Category { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Featured { get; set; }
    public string Image { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string PublishedAt { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public List<string>? Colors { get; set; }
}