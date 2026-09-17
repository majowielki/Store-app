using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authorization;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Services;

namespace Store.OrderService.Controllers;

/// <summary>The signed-in customer's orders. The admin panel has its own routes under /admin/orders.</summary>
[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private const int IdempotencyKeyMaxLength = 128;

    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    private string UserId => User.GetRequiredUserId();

    /// <summary>
    /// Places an order from the cart. Send an Idempotency-Key header (a UUID per checkout
    /// attempt): a retry with the same key returns the order created the first time.
    /// </summary>
    [HttpPost("from-cart")]
    public async Task<ActionResult<OrderResponse>> CreateOrderFromCart(
        [FromBody] CreateOrderFromCartRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        request.UserId = UserId;
        if (idempotencyKey is { Length: > IdempotencyKeyMaxLength })
        {
            throw new DomainValidationException($"Idempotency-Key must be at most {IdempotencyKeyMaxLength} characters");
        }

        var order = await _orderService.CreateOrderFromCartAsync(request, string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim());
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    /// <summary>One order: the customer's own, or any order for an administrator (masked for the demo one).</summary>
    [HttpGet("{id:int}")]
    public async Task<OrderResponse> GetOrder(int id)
    {
        if (User.IsStoreAdmin())
        {
            var adminOrder = await _orderService.GetOrderForAdminAsync(id);
            return adminOrder.ForViewer(User);
        }

        return await _orderService.GetOrderAsync(id, UserId);
    }

    /// <summary>The customer's orders, newest first.</summary>
    [HttpGet("my-orders")]
    public Task<PagedResponse<OrderResponse>> GetMyOrders([FromQuery] PagedQuery paging)
        => _orderService.GetUserOrdersAsync(UserId, paging);

    /// <summary>Whether the customer has ordered before - the first order is discounted.</summary>
    [HttpGet("has-orders")]
    public async Task<HasOrdersResponse> HasOrders()
    {
        var count = await _orderService.GetUserOrdersCountAsync(UserId);
        return new HasOrdersResponse { HasOrders = count > 0, OrdersCount = count };
    }
}
