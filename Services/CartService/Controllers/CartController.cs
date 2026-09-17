using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.BuildingBlocks.Authorization;
using Store.CartService.DTOs.Requests;
using Store.CartService.DTOs.Responses;
using Store.CartService.Services;

namespace Store.CartService.Controllers;

/// <summary>
/// The signed-in customer's cart. Every change answers with the whole cart afterwards.
/// </summary>
[ApiController]
[Route("api/v1/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    private string UserId => User.GetRequiredUserId();

    /// <summary>The cart with today's prices; empty for a customer who has not added anything yet.</summary>
    [HttpGet]
    public Task<CartResponse> GetCart()
        => _cartService.GetCartAsync(UserId);

    /// <summary>Merges the lines of a guest cart into the server cart, in one request.</summary>
    [HttpPost("sync")]
    public Task<CartResponse> SyncCart([FromBody] SyncCartRequest request)
        => _cartService.SyncCartAsync(UserId, request);

    /// <summary>Adds a line, or raises the quantity of the line with the same product and colour.</summary>
    [HttpPost("items")]
    public Task<CartResponse> AddItem([FromBody] AddCartItemRequest request)
        => _cartService.AddItemAsync(UserId, request);

    /// <summary>Changes the quantity or colour of a line; 404 for a line that is not in this cart.</summary>
    [HttpPut("items/{cartItemId:int}")]
    public Task<CartResponse> UpdateItem(int cartItemId, [FromBody] UpdateCartItemRequest request)
        => _cartService.UpdateItemAsync(UserId, cartItemId, request);

    /// <summary>Removes a line; 404 for a line that is not in this cart.</summary>
    [HttpDelete("items/{cartItemId:int}")]
    public Task<CartResponse> RemoveItem(int cartItemId)
        => _cartService.RemoveItemAsync(UserId, cartItemId);

    /// <summary>Removes every line; a cart that is already empty stays that way.</summary>
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        await _cartService.ClearCartAsync(UserId);
        return NoContent();
    }

    /// <summary>Number of pieces in the cart (quantities summed).</summary>
    [HttpGet("count")]
    public Task<int> GetItemCount()
        => _cartService.GetItemCountAsync(UserId);

    /// <summary>Sum of the lines, before discount and delivery.</summary>
    [HttpGet("total")]
    public Task<decimal> GetTotal()
        => _cartService.GetTotalAsync(UserId);
}
