using Store.CartService.DTOs.Requests;
using Store.CartService.DTOs.Responses;

namespace Store.CartService.Services;

public interface ICartService
{
    // Core cart operations
    Task<CartResponse?> GetCartByUserIdAsync(string userId);
    Task<CartResponse> CreateCartAsync(string userId);
    Task<CartItemResponse> AddItemToCartAsync(string userId, AddCartItemRequest request);
    Task<CartItemResponse?> UpdateCartItemAsync(string userId, int cartItemId, UpdateCartItemRequest request);
    Task<bool> RemoveItemFromCartAsync(string userId, int cartItemId);
    Task<bool> ClearCartAsync(string userId);
    Task<int> GetCartItemCountAsync(string userId);
    Task<decimal> GetCartTotalAsync(string userId);
    Task<CartResponse> SyncCartAsync(string userId, SyncCartRequest request);
}
