using Microsoft.EntityFrameworkCore;
using Store.OrderService.Data;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;
using Store.Shared.Models;
using Store.Shared.MessageBus;
using System.Text.Json;

#nullable enable

namespace Store.OrderService.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _context;
    private readonly ILogger<OrderService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMessageBus? _messageBus;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrderService(
        OrderDbContext context,
        ILogger<OrderService> logger,
        HttpClient httpClient,
        IConfiguration configuration,
    IMessageBus? messageBus = null,
    IHttpContextAccessor? httpContextAccessor = null)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
        _messageBus = messageBus;
    _httpContextAccessor = httpContextAccessor ?? new HttpContextAccessor();
    }

    public async Task<OrderResponse> CreateOrderFromCartAsync(CreateOrderFromCartRequest request)
    {
        try
        {
            _logger.LogInformation("Creating order from cart for user: {UserId}", request.UserId);

            // Get cart items from Cart Service
            var cartItems = await GetCartItemsAsync(request.UserId);
            
            if (cartItems == null || !cartItems.Any())
            {
                throw new InvalidOperationException("Cart is empty or not found");
            }

            // Create the order
            var order = new Order
            {
                UserId = request.UserId,
                UserEmail = request.UserEmail,
                DeliveryAddress = request.DeliveryAddress,
                CustomerName = request.CustomerName,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            // Create order items from cart items
            order.OrderItems = cartItems.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                ProductTitle = ci.ProductTitle,
                ProductImage = ci.ProductImage,
                Price = ci.Price,
                Quantity = ci.Quantity,
                Color = ci.Color,
                Company = ci.Company
            }).ToList();

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order created successfully with ID: {OrderId}", order.Id);

            // Clear the cart after successful order creation
            await ClearCartAsync(request.UserId);

            // Publish order created event
            await PublishOrderCreatedEventAsync(order);

            return MapToOrderResponse(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order from cart for user: {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<OrderResponse?> GetOrderByIdAsync(int orderId, string userId)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return null;
            }

            // Check if user has access to this order (user can only see their own orders unless admin)
            if (order.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to access order {OrderId} belonging to {OrderUserId}", 
                    userId, orderId, order.UserId);
                return null;
            }

            return MapToOrderResponse(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order: {OrderId}", orderId);
            throw;
        }
    }

    public async Task<OrderListResponse> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt);

            var totalCount = await query.CountAsync();
            
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new OrderListResponse
            {
                Orders = orders.Select(MapToOrderResponse),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<OrderListResponse> GetAllOrdersAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt);

            var totalCount = await query.CountAsync();
            
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new OrderListResponse
            {
                Orders = orders.Select(MapToOrderResponse),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all orders");
            throw;
        }
    }

    public async Task<int> GetUserOrdersCountAsync(string userId)
    {
        try
        {
            return await _context.Orders
                .Where(o => o.UserId == userId)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order count for user: {UserId}", userId);
            throw;
        }
    }

    private async Task<List<CartItemDto>?> GetCartItemsAsync(string userId)
    {
        try
        {
            var cartServiceUrl =
                _configuration["Services:CartService:BaseUrl"]
                ?? _configuration["Services:CartService"]
                ?? "http://cartservice:5005";

            // Forward the bearer token so CartService can authorize the user
            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{cartServiceUrl.TrimEnd('/')}/api/cart");
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.TryAddWithoutValidation("Authorization", token);
            }

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Cart not found for user: {UserId}", userId);
                    return null;
                }
                
                _logger.LogError("Error retrieving cart from CartService. Status: {StatusCode}", response.StatusCode);
                throw new Exception($"Failed to retrieve cart from CartService. Status: {response.StatusCode}");
            }

            var cartJson = await response.Content.ReadAsStringAsync();
            var cartResponse = JsonSerializer.Deserialize<CartServiceResponseDto>(cartJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return cartResponse?.Items?.Select(i => new CartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductTitle = i.Title,
                ProductImage = i.Image,
                Price = i.Price,
                Quantity = i.Quantity,
                Color = i.Color,
                Company = i.Company,
                LineTotal = i.LineTotal
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CartService for user: {UserId}", userId);
            throw;
        }
    }

    private async Task ClearCartAsync(string userId)
    {
        try
        {
            var cartServiceUrl =
                _configuration["Services:CartService:BaseUrl"]
                ?? _configuration["Services:CartService"]
                ?? "http://cartservice:5005";

            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{cartServiceUrl.TrimEnd('/')}/api/cart");
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.TryAddWithoutValidation("Authorization", token);
            }

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Cart cleared successfully for user: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Failed to clear cart for user: {UserId}. Status: {StatusCode}", userId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for user: {UserId}", userId);
            // Don't throw here as order was already created successfully
        }
    }

    private async Task PublishOrderCreatedEventAsync(Order order)
    {
        if (_messageBus == null)
        {
            _logger.LogWarning("MessageBus is not configured, skipping event publishing");
            return;
        }

        try
        {
            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = Guid.NewGuid(), // Create a Guid representation for the event
                UserId = Guid.Parse(order.UserId),
                TotalAmount = order.OrderTotal,
                Items = order.OrderItems.Select(oi => new OrderItemEvent
                {
                    ProductId = Guid.NewGuid(), // This should ideally be the actual product Guid if available
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            };

            await _messageBus.PublishAsync(orderCreatedEvent);
            _logger.LogInformation("Published OrderCreatedEvent for order: {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing OrderCreatedEvent for order: {OrderId}", order.Id);
            // Don't throw here as order was already created successfully
        }
    }

    private OrderResponse MapToOrderResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            UserEmail = order.UserEmail,
            DeliveryAddress = order.DeliveryAddress,
            CustomerName = order.CustomerName,
            OrderItems = order.OrderItems.Select(oi => new OrderItemResponse
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductTitle = oi.ProductTitle,
                ProductImage = oi.ProductImage,
                Price = oi.Price,
                Quantity = oi.Quantity,
                Color = oi.Color,
                Company = oi.Company,
                LineTotal = oi.LineTotal
            }).ToList(),
            TotalItems = order.TotalItems,
            OrderTotal = order.OrderTotal,
            CreatedAt = order.CreatedAt,
            Notes = order.Notes
        };
    }
}

// DTOs for CartService communication
public class CartServiceResponseDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<CartServiceCartItemDto> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CartServiceCartItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public decimal LineTotal { get; set; }
}

public class CartItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public decimal LineTotal { get; set; }
}