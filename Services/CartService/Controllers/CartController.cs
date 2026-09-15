using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.CartService.DTOs.Requests;
using Store.CartService.DTOs.Responses;
using Store.CartService.Services;
using Store.Shared.Models;
using System.Security.Claims;

namespace Store.CartService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cartService, ILogger<CartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    /// <summary>
    /// Get user's cart
    /// </summary>
    /// <returns>User's cart with items</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<CartResponse?>>> GetCart()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<CartResponse?>.Error("User not found"));

        var response = await _cartService.GetCartByUserIdAsync(userId);
        if (!response.IsSuccess && response.Message == "Cart not found")
        {
            var createResponse = await _cartService.CreateCartAsync(userId);
            return StatusCode((int)createResponse.StatusCode, createResponse);
        }
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Sync local cart with server in a single request
    /// </summary>
    /// <param name="request">Items to merge into the server cart</param>
    /// <returns>Updated server cart</returns>
    [HttpPost("sync")]
    public async Task<ActionResult<ApiResponse<CartResponse>>> SyncCart([FromBody] SyncCartRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<CartResponse>.Error("User not found"));

        if (request?.Items == null || request.Items.Count == 0)
        {
            var current = await _cartService.GetCartByUserIdAsync(userId);
            if (!current.IsSuccess)
                return StatusCode((int)current.StatusCode, current);
            return Ok(current);
        }
        var response = await _cartService.SyncCartAsync(userId, request);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Add item to cart
    /// </summary>
    /// <param name="request">Item to add to cart</param>
    /// <returns>Added cart item</returns>
    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse<CartResponse?>>> AddItemToCart([FromBody] AddCartItemRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<CartResponse?>.Error("User not found"));

        var addResponse = await _cartService.AddItemToCartAsync(userId, request);
        if (!addResponse.IsSuccess)
            return StatusCode((int)addResponse.StatusCode, addResponse);
        var cartResponse = await _cartService.GetCartByUserIdAsync(userId);
        return StatusCode((int)cartResponse.StatusCode, cartResponse);
    }

    /// <summary>
    /// Update cart item quantity or color
    /// </summary>
    /// <param name="cartItemId">Cart item ID</param>
    /// <param name="request">Update data</param>
    /// <returns>Updated cart item</returns>
    [HttpPut("items/{cartItemId}")]
    public async Task<ActionResult<ApiResponse<CartResponse?>>> UpdateCartItem(int cartItemId, [FromBody] UpdateCartItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<CartResponse?>.ValidationError(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<CartResponse?>.Error("User not found"));

        var updateResponse = await _cartService.UpdateCartItemAsync(userId, cartItemId, request);
        if (!updateResponse.IsSuccess)
            return StatusCode((int)updateResponse.StatusCode, updateResponse);
        var cartResponse = await _cartService.GetCartByUserIdAsync(userId);
        return StatusCode((int)cartResponse.StatusCode, cartResponse);
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    /// <param name="cartItemId">Cart item ID to remove</param>
    /// <returns>Success status</returns>
    [HttpDelete("items/{cartItemId}")]
    public async Task<ActionResult<ApiResponse<CartResponse?>>> RemoveItemFromCart(int cartItemId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<CartResponse?>.Error("User not found"));

        var removeResponse = await _cartService.RemoveItemFromCartAsync(userId, cartItemId);
        if (!removeResponse.IsSuccess)
            return StatusCode((int)removeResponse.StatusCode, removeResponse);
        var cartResponse = await _cartService.GetCartByUserIdAsync(userId);
        return StatusCode((int)cartResponse.StatusCode, cartResponse);
    }

    /// <summary>
    /// Clear all items from cart
    /// </summary>
    /// <returns>Success status</returns>
    [HttpDelete]
    public async Task<ActionResult<ApiResponse<bool>>> ClearCart()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<bool>.Error("User not found"));

        var response = await _cartService.ClearCartAsync(userId);
        if (!response.IsSuccess)
            return StatusCode((int)response.StatusCode, response);
        return NoContent();
    }

    /// <summary>
    /// Get cart item count
    /// </summary>
    /// <returns>Number of items in cart</returns>
    [HttpGet("count")]
    public async Task<ActionResult<ApiResponse<int>>> GetCartItemCount()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<int>.Error("User not found"));

        var response = await _cartService.GetCartItemCountAsync(userId);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Get cart total amount
    /// </summary>
    /// <returns>Total cart amount</returns>
    [HttpGet("total")]
    public async Task<ActionResult<ApiResponse<decimal>>> GetCartTotal()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<decimal>.Error("User not found"));

        var response = await _cartService.GetCartTotalAsync(userId);
        return StatusCode((int)response.StatusCode, response);
    }
}

