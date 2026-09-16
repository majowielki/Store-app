using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Contracts.Authorization;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Services;

namespace Store.OrderService.Controllers;

/// <summary>
/// Orders as the admin panel sees them. Served by the order service itself - the identity
/// service used to proxy these calls with the administrator's token and its own copy of the
/// response types. The demo administrator sees the panel but not the customers' data.
/// </summary>
[ApiController]
[Route("api/admin/orders")]
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
    public async Task<ActionResult<AdminOrderListResponse>> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _orderService.GetAllOrdersAsync(page, pageSize);
        if (!result.IsSuccess || result.Data is null)
        {
            return StatusCode((int)result.StatusCode, result);
        }

        return Ok(ToList(result.Data));
    }

    /// <summary>One order; 404 when the id is unknown.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(int id)
    {
        var result = await _orderService.GetOrderByIdForAdminAsync(id);
        if (!result.IsSuccess || result.Data is null)
        {
            return StatusCode((int)result.StatusCode, result);
        }

        return Ok(result.Data.ForViewer(User));
    }

    private AdminOrderListResponse ToList(OrderListResponse list) => new()
    {
        Items = list.ForViewer(User).Orders.ToList(),
        TotalCount = list.TotalCount,
        Page = list.Page,
        PageSize = list.PageSize,
        TotalPages = list.TotalPages,
        HasNextPage = list.HasNextPage,
        HasPreviousPage = list.HasPreviousPage
    };
}

/// <summary>Page of orders for the admin panel.</summary>
public class AdminOrderListResponse
{
    public List<OrderResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
