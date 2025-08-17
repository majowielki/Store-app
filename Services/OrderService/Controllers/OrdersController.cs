using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Services;
using System.Security.Claims;

namespace Store.OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new order from user's cart
    /// </summary>
    /// <param name="request">Order creation data</param>
    /// <returns>Created order</returns>
    [HttpPost("from-cart")]
    public async Task<ActionResult<OrderResponse>> CreateOrderFromCart([FromBody] CreateOrderFromCartRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            // Ensure the order belongs to the authenticated user
            request.UserId = userId;

            var order = await _orderService.CreateOrderFromCartAsync(request);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order from cart for user: {UserId}", request.UserId);
            return StatusCode(500, "An error occurred while creating the order");
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Order details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(int id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            // If admin, bypass user filter
            if (User.IsInRole("true-admin") || User.IsInRole("demo-admin"))
            {
                var adminOrder = await _orderService.GetOrderByIdForAdminAsync(id);
                if (adminOrder == null) return NotFound($"Order with ID {id} not found");
                return Ok(adminOrder);
            }

            var order = await _orderService.GetOrderByIdAsync(id, userId);

            if (order == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order: {OrderId}", id);
            return StatusCode(500, "An error occurred while retrieving the order");
        }
    }

    /// <summary>
    /// Get user's orders
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of user's orders</returns>
    [HttpGet("my-orders")]
    public async Task<ActionResult<OrderListResponse>> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            var orders = await _orderService.GetUserOrdersAsync(userId, page, pageSize);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user orders");
            return StatusCode(500, "An error occurred while retrieving orders");
        }
    }

    /// <summary>
    /// Get all orders (Admin only)
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of all orders</returns>
    [HttpGet]
    [Authorize(Roles = "true-admin,demo-admin")]
    public async Task<ActionResult<OrderListResponse>> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var orders = await _orderService.GetAllOrdersAsync(page, pageSize);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all orders");
            return StatusCode(500, "An error occurred while retrieving orders");
        }
    }

    /// <summary>
    /// Get orders by user id (Admin only)
    /// </summary>
    [HttpGet("by-user/{userId}")]
    [Authorize(Roles = "true-admin,demo-admin")]
    public async Task<ActionResult<OrderListResponse>> GetOrdersByUserId([FromRoute] string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var orders = await _orderService.GetOrdersByUserIdAsync(userId, page, pageSize);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for user: {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving orders");
        }
    }

    /// <summary>
    /// Get aggregated order statistics (Admin only)
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "true-admin,demo-admin")]
    public async Task<ActionResult<OrderStatsResponse>> GetStats([FromQuery] int days = 30)
    {
        try
        {
            var stats = await _orderService.GetOrderStatsAsync(days <= 0 ? 30 : days);
            // Ensure always a valid response, even if no orders
            if (stats == null)
            {
                return Ok(new OrderStatsResponse
                {
                    TotalOrders = 0,
                    TotalRevenue = 0,
                    Daily = new List<TimeBucketStats>(),
                    Weekly = new List<TimeBucketStats>(),
                    TopProducts = new List<TopProductStats>()
                });
            }
            // Defensive: fill empty lists if null
            stats.Daily ??= new List<TimeBucketStats>();
            stats.Weekly ??= new List<TimeBucketStats>();
            stats.TopProducts ??= new List<TopProductStats>();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order stats");
            return StatusCode(500, "An error occurred while retrieving order stats");
        }
    }

    /// <summary>
    /// Check if current user has any orders
    /// </summary>
    [HttpGet("has-orders")] // For promotions eligibility checks
    public async Task<ActionResult<HasOrdersResponse>> HasOrders()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }
            var count = await _orderService.GetUserOrdersCountAsync(userId);
            return Ok(new HasOrdersResponse { HasOrders = count > 0, OrdersCount = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user orders existence");
            return StatusCode(500, "An error occurred while checking orders");
        }
    }
}

