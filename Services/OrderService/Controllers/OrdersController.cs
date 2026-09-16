using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authorization;
using Store.Contracts.Authorization;
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
    /// Create a new order from the user's cart. Send an Idempotency-Key header (a UUID per
    /// checkout attempt): a retry with the same key returns the order created the first time.
    /// </summary>
    /// <param name="request">Order creation data</param>
    /// <param name="idempotencyKey">Idempotency-Key header, optional</param>
    /// <returns>Created order</returns>
    [HttpPost("from-cart")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> CreateOrderFromCart(
        [FromBody] CreateOrderFromCartRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        // FluentValidation will handle validation automatically
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<OrderResponse>.Error("User not found"));
        }
        request.UserId = userId;
        if (idempotencyKey is { Length: > 128 })
        {
            return BadRequest(ApiResponse<OrderResponse>.Error("Idempotency-Key must be at most 128 characters"));
        }
        var response = await _orderService.CreateOrderFromCartAsync(request, string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim());
        if (!response.IsSuccess)
            return StatusCode((int)response.StatusCode, response);
        return StatusCode(201, response);
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Order details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OrderResponse?>>> GetOrder(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<OrderResponse?>.Error("User not found"));
        }
        if (User.IsStoreAdmin())
        {
            var adminOrder = await _orderService.GetOrderByIdForAdminAsync(id);
            return StatusCode((int)adminOrder.StatusCode, adminOrder);
        }
        var response = await _orderService.GetOrderByIdAsync(id, userId);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Get user's orders
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of user's orders</returns>
    [HttpGet("my-orders")]
    public async Task<ActionResult<ApiResponse<OrderListResponse>>> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<OrderListResponse>.Error("User not found"));
        }
        var response = await _orderService.GetUserOrdersAsync(userId, page, pageSize);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Get all orders (Admin only)
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of all orders</returns>
    [HttpGet]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<ApiResponse<OrderListResponse>>> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var response = await _orderService.GetAllOrdersAsync(page, pageSize);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Get orders by user id (Admin only)
    /// </summary>
    [HttpGet("by-user/{userId}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<ApiResponse<OrderListResponse>>> GetOrdersByUserId([FromRoute] string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var response = await _orderService.GetOrdersByUserIdAsync(userId, page, pageSize);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Get aggregated order statistics (Admin only)
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<ApiResponse<OrderStatsResponse>>> GetStats([FromQuery] int days = 30)
    {
        var response = await _orderService.GetOrderStatsAsync(days <= 0 ? 30 : days);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Check if current user has any orders
    /// </summary>
    [HttpGet("has-orders")] // For promotions eligibility checks
    public async Task<ActionResult<ApiResponse<HasOrdersResponse>>> HasOrders()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<HasOrdersResponse>.Error("User not found"));
        }
        var countResponse = await _orderService.GetUserOrdersCountAsync(userId);
        if (!countResponse.IsSuccess)
            return StatusCode((int)countResponse.StatusCode, countResponse);
        return Ok(ApiResponse<HasOrdersResponse>.Success(new HasOrdersResponse { HasOrders = countResponse.Data > 0, OrdersCount = countResponse.Data }));
    }
}

