using Microsoft.EntityFrameworkCore;
using Store.CartService.Data;
using Store.CartService.DTOs.Requests;
using Store.CartService.DTOs.Responses;
using Store.Shared.Models;

namespace Store.CartService.Services;

public class CartService : ICartService
{
    private readonly CartDbContext _context;
    private readonly ILogger<CartService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public CartService(
        CartDbContext context, 
        ILogger<CartService> logger, 
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<CartResponse?> GetCartByUserIdAsync(string userId)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return null;

            return MapToCartResponse(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<CartResponse> CreateCartAsync(string userId)
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

            return MapToCartResponse(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cart for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<CartItemResponse> AddItemToCartAsync(string userId, AddCartItemRequest request)
    {
        try
        {
            // Get or create cart
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

            // Get product information
            var product = await GetOrCreateProductAsync(request.ProductId);
            
            if (product == null)
            {
                throw new ArgumentException($"Product with ID {request.ProductId} not found");
            }

            // Check if item already exists in cart with same color
            var existingItem = cart.CartItems.FirstOrDefault(ci => 
                ci.ProductId == request.ProductId && 
                ci.ProductColor == request.Color);

            CartItem cartItem;

            if (existingItem != null)
            {
                // Update quantity
                existingItem.Amount += request.Quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
                cartItem = existingItem;
            }
            else
            {
                // Create new cart item
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

            return MapToCartItemResponse(cartItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart for user: {UserId}, Product: {ProductId}", userId, request.ProductId);
            throw;
        }
    }

    public async Task<CartItemResponse?> UpdateCartItemAsync(string userId, int cartItemId, UpdateCartItemRequest request)
    {
        try
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null) return null;

            // Update quantity if provided
            if (request.Quantity.HasValue)
            {
                cartItem.Amount = request.Quantity.Value;
            }

            // Update color if provided
            if (!string.IsNullOrEmpty(request.Color))
            {
                cartItem.ProductColor = request.Color;
            }

            cartItem.UpdatedAt = DateTime.UtcNow;
            cartItem.Cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToCartItemResponse(cartItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item: {CartItemId} for user: {UserId}", cartItemId, userId);
            throw;
        }
    }

    public async Task<bool> RemoveItemFromCartAsync(string userId, int cartItemId)
    {
        try
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null) return false;

            _context.CartItems.Remove(cartItem);
            cartItem.Cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item: {CartItemId} for user: {UserId}", cartItemId, userId);
            throw;
        }
    }

    public async Task<bool> ClearCartAsync(string userId)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return false;

            _context.CartItems.RemoveRange(cart.CartItems);
            cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetCartItemCountAsync(string userId)
    {
        try
        {
            var count = await _context.CartItems
                .Where(ci => ci.Cart.UserId == userId)
                .SumAsync(ci => ci.Amount);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart item count for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<decimal> GetCartTotalAsync(string userId)
    {
        try
        {
            var total = await _context.CartItems
                .Where(ci => ci.Cart.UserId == userId)
                .SumAsync(ci => ci.LineTotal);

            return total;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart total for user: {UserId}", userId);
            throw;
        }
    }

    private async Task<Product?> GetOrCreateProductAsync(int productId)
    {
        // First, check if product exists in local database
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        
        if (product != null) return product;

        // If not found locally, try to fetch from Product Service
        try
        {
            var productServiceUrl = _configuration["Services:ProductService:BaseUrl"] ?? "http://localhost:5001";
            var response = await _httpClient.GetAsync($"{productServiceUrl}/api/products/{productId}");
            
            if (response.IsSuccessStatusCode)
            {
                var productData = await response.Content.ReadFromJsonAsync<ProductDto>();
                if (productData != null)
                {
                    // Create local copy of product for cart operations
                    product = new Product
                    {
                        Id = productData.Id,
                        Title = productData.Title,
                        Description = productData.Description ?? "",
                        Image = productData.Image,
                        Price = productData.Price,
                        Category = productData.Category,
                        Company = productData.Company,
                        Colors = productData.Colors ?? new List<string>(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();
                    return product;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch product {ProductId} from Product Service", productId);
        }

        return null;
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
        return new CartItemResponse
        {
            Id = cartItem.Id,
            ProductId = cartItem.ProductId,
            Title = cartItem.Title,
            Image = cartItem.Image,
            Price = cartItem.Price,
            Quantity = cartItem.Amount,
            Color = cartItem.ProductColor,
            Company = cartItem.Company,
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