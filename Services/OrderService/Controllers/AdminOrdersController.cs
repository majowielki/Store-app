using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authorization;
using Store.Contracts.Authorization;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Models;
using Store.OrderService.Services;

namespace Store.OrderService.Controllers;

/// <summary>
/// Orders as the admin panel sees them. The demo administrator sees the panel but not the
/// customers' data.
/// </summary>
[ApiController]
[Route("api/v1/admin/orders")]
[Authorize(Policy = Policies.Admin)]
public class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>All orders, newest first.</summary>
    [HttpGet]
    public async Task<PagedResponse<OrderResponse>> GetOrders([FromQuery] PagedQuery paging)
        => (await _orderService.GetAllOrdersAsync(paging, cancellationToken: HttpContext.RequestAborted)).ForViewer(User);

    /// <summary>Orders of one customer, newest first; an empty page when they have none.</summary>
    [HttpGet("by-user/{userId}")]
    public async Task<PagedResponse<OrderResponse>> GetOrdersByUser(string userId, [FromQuery] PagedQuery paging)
        => (await _orderService.GetUserOrdersAsync(userId, paging, cancellationToken: HttpContext.RequestAborted)).ForViewer(User);

    /// <summary>One order; 404 when the id is unknown.</summary>
    [HttpGet("{id:int}")]
    public async Task<OrderResponse> GetOrder(int id)
        => (await _orderService.GetOrderForAdminAsync(id, cancellationToken: HttpContext.RequestAborted)).ForViewer(User);

    /// <summary>
    /// Ships a paid order or cancels one not shipped yet; a paid order that is cancelled is refunded
    /// by the saga. A move the order's status does not allow is 409; only the true administrator may
    /// change orders.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = Policies.AdminWrite)]
    public Task<OrderResponse> ChangeStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        => _orderService.ChangeStatusAsync(id, Enum.Parse<OrderStatus>(request.Status), User.GetRequiredUserId(), cancellationToken: HttpContext.RequestAborted);

    /// <summary>Orders and revenue per day, per week and per product over the last <paramref name="days"/> days.</summary>
    [HttpGet("stats")]
    public Task<OrderStatsResponse> GetStats([FromQuery] int days = 30)
        => _orderService.GetOrderStatsAsync(days, cancellationToken: HttpContext.RequestAborted);
}
