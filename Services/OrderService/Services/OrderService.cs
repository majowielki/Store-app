using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Store.OrderService.Data;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;
using Store.Shared.Models;
using Store.Shared.MessageBus;
using System.Text.Json;
using Store.Shared.Services;
using System.Text.Json.Serialization;
using SharedOrderItemResponse = Store.Shared.Models.OrderItemResponse;

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
    private readonly IAuditLogClient _auditLogClient;

    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OrderService(
        OrderDbContext context,
        ILogger<OrderService> logger,
        HttpClient httpClient,
        IConfiguration configuration,
        IMessageBus? messageBus = null,
        IHttpContextAccessor? httpContextAccessor = null,
        IAuditLogClient? auditLogClient = null)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
        _messageBus = messageBus;
        _httpContextAccessor = httpContextAccessor ?? new HttpContextAccessor();
        _auditLogClient = auditLogClient ?? throw new ArgumentNullException(nameof(auditLogClient));
    }

    public async Task<OrderResponse> CreateOrderFromCartAsync(CreateOrderFromCartRequest request)
    {
        try
        {
            // Get cart items from Cart Service
            var cartItems = await GetCartItemsAsync(request.UserId);
            
            if (cartItems == null || !cartItems.Any())
            {
                throw new InvalidOperationException("Cart is empty or not found");
            }

            // Check if this is the user's first order
            var hasPlacedFirstOrder = await _context.Orders.AnyAsync(o => o.UserId == request.UserId);

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
                Price = (ci.Price > 0 ? ci.Price : 0.01m),
                Quantity = ci.Quantity,
                Color = ci.Color
            }).ToList();

            // Apply first-order discount
            if (!hasPlacedFirstOrder)
            {
                var discount = order.OrderTotal * 0.20m;
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = 0, // Placeholder for discount
                    ProductTitle = "First Order Discount",
                    Price = -discount,
                    Quantity = 1,
                    Color = "N/A",
                    OrderDiscount = discount // Map the discount amount
                });
            }

            // Add delivery fee
            var deliveryFee = order.DeliveryFee;
            if (deliveryFee > 0)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = 0, // Placeholder for delivery fee
                    ProductTitle = "Delivery Fee",
                    Price = deliveryFee,
                    Quantity = 1,
                    Color = "N/A",
                    DeliveryCost = deliveryFee // Map the delivery fee
                });
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Audit log: order created
            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "ORDER_CREATED",
                EntityName = nameof(Order),
                EntityId = order.Id.ToString(),
                UserId = order.UserId,
                UserEmail = order.UserEmail,
                Timestamp = DateTime.UtcNow,
                NewValues = JsonSerializer.Serialize(order, AuditJsonOptions),
                AdditionalInfo = JsonSerializer.Serialize(new { Source = "OrderService" }, AuditJsonOptions)
            });

            // Clear the cart after successful order creation
            await ClearCartAsync(request.UserId);

            // Publish order created event
            await PublishOrderCreatedEventAsync(order);

            // Save address to user profile if requested and not demo user
            if (request.SaveAddress && !string.IsNullOrWhiteSpace(request.DeliveryAddress))
            {
                try
                {
                    var httpContext = _httpContextAccessor.HttpContext;
                    var authHeader = httpContext?.Request.Headers["Authorization"].FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(authHeader))
                    {
                        // Always ensure 'Bearer ' prefix
                        var headerValue = authHeader.StartsWith("Bearer ") ? authHeader : $"Bearer {authHeader}";
                        var handler = new JwtSecurityTokenHandler();
                        var jwt = handler.ReadJwtToken(headerValue.Replace("Bearer ", ""));
                        var userId = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                        var roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
                        var email = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

                        var identityServiceUrl =
                            _configuration["Services:IdentityService:BaseUrl"]
                            ?? _configuration["Services:IdentityService"];
                        if (string.IsNullOrWhiteSpace(identityServiceUrl))
                        {
                            _logger.LogError("IdentityService URL is not configured. Cannot update user address for user: {UserId}", request.UserId);
                        }
                        else
                        {
                            var updateAddressRequest = new
                            {
                                SimpleAddress = request.DeliveryAddress
                            };
                            var url = $"{identityServiceUrl.TrimEnd('/')}/api/auth/me/address";
                            var httpRequest = new HttpRequestMessage(HttpMethod.Put, url)
                            {
                                Content = new StringContent(JsonSerializer.Serialize(updateAddressRequest), System.Text.Encoding.UTF8, "application/json")
                            };
                            httpRequest.Headers.Remove("Authorization"); // Remove any existing
                            httpRequest.Headers.TryAddWithoutValidation("Authorization", headerValue);

                            _logger.LogInformation("Sending address update to IdentityService. URL: {Url}, Payload: {Payload}, Authorization: {Authorization}", url, JsonSerializer.Serialize(updateAddressRequest), headerValue);
                            var response = await _httpClient.SendAsync(httpRequest);
                            var responseBody = await response.Content.ReadAsStringAsync();
                            _logger.LogInformation("IdentityService response: StatusCode={StatusCode}, Body={Body}", response.StatusCode, responseBody);
                            if (!response.IsSuccessStatusCode)
                            {
                                _logger.LogWarning("Failed to save address to user profile for user: {UserId}. Status: {StatusCode}, Body: {Body}", request.UserId, response.StatusCode, responseBody);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No Authorization header found in HttpContext for address update!");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving address to user profile for user: {UserId}", request.UserId);
                }
            }

            return new Store.OrderService.DTOs.Responses.OrderResponse
            {
                Id = order.Id,
                UserId = order.UserId,
                UserEmail = order.UserEmail,
                DeliveryAddress = order.DeliveryAddress,
                CustomerName = order.CustomerName,
                OrderItems = order.OrderItems.Select(oi => new Store.OrderService.DTOs.Responses.OrderItemResponse
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductTitle = oi.ProductTitle,
                    ProductImage = oi.ProductImage,
                    Price = oi.Price,
                    Quantity = oi.Quantity,
                    Color = oi.Color,
                    DeliveryCost = oi.DeliveryCost, // Map delivery cost
                    OrderDiscount = oi.OrderDiscount // Map order discount
                }).ToList(),
                TotalItems = order.TotalItems,
                OrderTotal = order.OrderTotal,
                CreatedAt = order.CreatedAt,
                Notes = order.Notes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order from cart for user: {UserId}", request.UserId);
            // Audit log: order creation failed
            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "ORDER_CREATION_FAILED",
                EntityName = nameof(Order),
                UserId = request.UserId,
                UserEmail = request.UserEmail,
                Timestamp = DateTime.UtcNow,
                AdditionalInfo = JsonSerializer.Serialize(new { Exception = ex.Message, Source = "OrderService" }, AuditJsonOptions)
            });
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

    public async Task<OrderResponse?> GetOrderByIdForAdminAsync(int orderId)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            return order == null ? null : MapToOrderResponse(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order for admin: {OrderId}", orderId);
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

    public async Task<OrderListResponse> GetOrdersByUserIdAsync(string userId, int page = 1, int pageSize = 20)
    {
        // Same as GetUserOrdersAsync but intended for admin queries without caller restriction
        return await GetUserOrdersAsync(userId, page, pageSize);
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

    public async Task<OrderStatsResponse> GetOrderStatsAsync(int daysWindow = 30)
    {
        var since = DateTime.UtcNow.Date.AddDays(-Math.Abs(daysWindow));

        // Preload needed data
        var ordersQuery = _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Where(o => o.CreatedAt >= since);

        var orders = await ordersQuery.ToListAsync();

        if (orders.Count == 0)
        {
            // Always return a valid, empty stats object
            return new OrderStatsResponse
            {
                TotalOrders = 0,
                TotalRevenue = 0,
                Daily = new List<TimeBucketStats>(),
                Weekly = new List<TimeBucketStats>(),
                TopProducts = new List<TopProductStats>()
            };
        }

        var response = new OrderStatsResponse
        {
            TotalOrders = orders.Count,
            TotalRevenue = orders.Sum(o => o.OrderTotal)
        };

        // Daily buckets
        var daily = orders
            .GroupBy(o => o.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TimeBucketStats
            {
                BucketStart = g.Key,
                Orders = g.Count(),
                Revenue = g.Sum(o => o.OrderTotal)
            })
            .ToList();

        response.Daily = daily;

        // Weekly buckets (ISO week by Monday start)
        static DateTime WeekStart(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }

        var weekly = orders
            .GroupBy(o => WeekStart(o.CreatedAt))
            .OrderBy(g => g.Key)
            .Select(g => new TimeBucketStats
            {
                BucketStart = g.Key,
                Orders = g.Count(),
                Revenue = g.Sum(o => o.OrderTotal)
            })
            .ToList();

        response.Weekly = weekly;

        // Top products by quantity and revenue in window
        var topProducts = orders
            .SelectMany(o => o.OrderItems)
            .GroupBy(i => new { i.ProductId, i.ProductTitle })
            .Select(g => new TopProductStats
            {
                ProductId = g.Key.ProductId,
                ProductTitle = g.Key.ProductTitle,
                Quantity = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.LineTotal)
            })
            .OrderByDescending(x => x.Quantity)
            .ThenByDescending(x => x.Revenue)
            .Take(10)
            .ToList();

        response.TopProducts = topProducts;

        return response;
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
        return new Store.OrderService.DTOs.Responses.OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            UserEmail = order.UserEmail,
            DeliveryAddress = order.DeliveryAddress,
            CustomerName = order.CustomerName,
            OrderItems = order.OrderItems.Select(oi => new Store.OrderService.DTOs.Responses.OrderItemResponse
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductTitle = oi.ProductTitle,
                ProductImage = oi.ProductImage,
                Price = oi.Price,
                Quantity = oi.Quantity,
                Color = oi.Color,
                DeliveryCost = oi.DeliveryCost, // Map delivery cost
                OrderDiscount = oi.OrderDiscount // Map order discount
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