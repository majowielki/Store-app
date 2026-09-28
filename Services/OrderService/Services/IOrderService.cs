using Store.BuildingBlocks.Api;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Models;

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

    /// <summary>
    /// Ships or cancels an order (the administrator's moves); another status is a
    /// <c>DomainValidationException</c>, a move the order's status does not allow a <c>ConflictException</c>.
    /// </summary>
    Task<OrderResponse> ChangeStatusAsync(int orderId, OrderStatus status, string actorId);

    /// <summary>
    /// Opens (or finds) the payment of a customer's order waiting for its payment. An order still
    /// reserving its stock, paid, cancelled or past its deadline is a <c>ConflictException</c>.
    /// </summary>
    Task<OrderPaymentResponse> StartPaymentAsync(int orderId, string userId);

    Task<PagedResponse<OrderResponse>> GetUserOrdersAsync(string userId, PagedQuery paging);
    Task<PagedResponse<OrderResponse>> GetAllOrdersAsync(PagedQuery paging);
    Task<int> GetUserOrdersCountAsync(string userId);
    Task<OrderStatsResponse> GetOrderStatsAsync(int daysWindow = 30);
}
