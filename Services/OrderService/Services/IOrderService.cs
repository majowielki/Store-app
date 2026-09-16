using Store.BuildingBlocks.Api;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;

namespace Store.OrderService.Services;

public interface IOrderService
{
    // Core simplified operations
    /// <param name="request">Checkout data; UserId is set by the caller from the token</param>
    /// <param name="idempotencyKey">Optional Idempotency-Key header: a retry with the same key gets the same order</param>
    Task<ApiResponse<OrderResponse>> CreateOrderFromCartAsync(CreateOrderFromCartRequest request, string? idempotencyKey = null);
    Task<ApiResponse<OrderResponse?>> GetOrderByIdAsync(int orderId, string userId);
    Task<ApiResponse<OrderResponse?>> GetOrderByIdForAdminAsync(int orderId); // Admin override
    Task<ApiResponse<OrderListResponse>> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 20);
    Task<ApiResponse<OrderListResponse>> GetOrdersByUserIdAsync(string userId, int page = 1, int pageSize = 20); // Admin list by user
    Task<ApiResponse<OrderListResponse>> GetAllOrdersAsync(int page = 1, int pageSize = 20); // Admin only
    Task<ApiResponse<int>> GetUserOrdersCountAsync(string userId);
    Task<ApiResponse<OrderStatsResponse>> GetOrderStatsAsync(int daysWindow = 30); // Admin stats
}

