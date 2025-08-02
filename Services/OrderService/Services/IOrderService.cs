using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;

namespace Store.OrderService.Services;

public interface IOrderService
{
    // Core simplified operations
    Task<OrderResponse> CreateOrderFromCartAsync(CreateOrderFromCartRequest request);
    Task<OrderResponse?> GetOrderByIdAsync(int orderId, string userId);
    Task<OrderListResponse> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 20);
    Task<OrderListResponse> GetAllOrdersAsync(int page = 1, int pageSize = 20); // Admin only
    Task<int> GetUserOrdersCountAsync(string userId);
}

