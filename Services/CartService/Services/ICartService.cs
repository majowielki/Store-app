using Store.CartService.DTOs.Requests;
using Store.CartService.DTOs.Responses;
using Store.Shared.Models;

namespace Store.CartService.Services;

public interface ICartService
{
    // Core cart operations
    Task<ApiResponse<CartResponse?>> GetCartByUserIdAsync(string userId);
    Task<ApiResponse<CartResponse>> CreateCartAsync(string userId);
    Task<ApiResponse<CartItemResponse>> AddItemToCartAsync(string userId, AddCartItemRequest request);
    Task<ApiResponse<CartItemResponse?>> UpdateCartItemAsync(string userId, int cartItemId, UpdateCartItemRequest request);
    Task<ApiResponse<bool>> RemoveItemFromCartAsync(string userId, int cartItemId);
    Task<ApiResponse<bool>> ClearCartAsync(string userId);
    Task<ApiResponse<int>> GetCartItemCountAsync(string userId);
    Task<ApiResponse<decimal>> GetCartTotalAsync(string userId);
    Task<ApiResponse<CartResponse>> SyncCartAsync(string userId, SyncCartRequest request);
}
