using Store.CartService.DTOs.Requests;
using Store.CartService.DTOs.Responses;
using Store.Contracts.Cart;

namespace Store.CartService.Services;

/// <summary>
/// The customer's cart. Every change answers with the whole cart as it is afterwards, so the
/// client replaces what it shows; failures are <c>ApiException</c>s (a line that does not
/// exist is <c>NotFoundException</c>, a product the catalogue does not sell is
/// <c>DomainValidationException</c>).
/// </summary>
public interface ICartService
{
    /// <summary>The cart, with prices refreshed from the catalogue; an empty cart for a user who has none yet.</summary>
    Task<CartResponse> GetCartAsync(string userId);
    Task<CartResponse> AddItemAsync(string userId, AddCartItemRequest request);
    Task<CartResponse> UpdateItemAsync(string userId, int cartItemId, UpdateCartItemRequest request);
    Task<CartResponse> RemoveItemAsync(string userId, int cartItemId);
    Task ClearCartAsync(string userId);
    Task<int> GetItemCountAsync(string userId);
    Task<decimal> GetTotalAsync(string userId);

    /// <summary>Merges the lines of a guest cart into the server cart at sign-in.</summary>
    Task<CartResponse> SyncCartAsync(string userId, SyncCartRequest request);

    // What the order service reads at checkout
    Task<CartSnapshot?> GetSnapshotAsync(string userId);

    /// <summary>Removes every line after an order was placed from the cart; returns how many. Throws on failure so the event is retried.</summary>
    Task<int> ClearAfterOrderAsync(string userId, int orderId);
}
