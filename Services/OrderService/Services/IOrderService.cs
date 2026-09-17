using Store.BuildingBlocks.Api;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;

namespace Store.OrderService.Services;

/// <summary>
/// Orders of the store. Failures are <c>ApiException</c>s: an unknown order is
/// <c>NotFoundException</c>, somebody else's order <c>ForbiddenException</c>, a checkout
/// the cart does not allow <c>DomainValidationException</c> or <c>ConflictException</c>.
/// </summary>
public interface IOrderService
{
    /// <param name="request">Checkout data; UserId is set by the caller from the token</param>
    /// <param name="idempotencyKey">Optional Idempotency-Key header: a retry with the same key gets the same order</param>
    Task<OrderResponse> CreateOrderFromCartAsync(CreateOrderFromCartRequest request, string? idempotencyKey = null);

    /// <summary>An order of the given customer; other customers' orders are forbidden.</summary>
    Task<OrderResponse> GetOrderAsync(int orderId, string userId);

    /// <summary>Any order, for the admin panel.</summary>
    Task<OrderResponse> GetOrderForAdminAsync(int orderId);

    Task<PagedResponse<OrderResponse>> GetUserOrdersAsync(string userId, PagedQuery paging);
    Task<PagedResponse<OrderResponse>> GetAllOrdersAsync(PagedQuery paging);
    Task<int> GetUserOrdersCountAsync(string userId);
    Task<OrderStatsResponse> GetOrderStatsAsync(int daysWindow = 30);
}
