using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.CartService.DTOs.Requests;
using Store.CartService.DTOs.Responses;
using Store.CartService.Services;
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
    public async Task<ActionResult<CartResponse>> GetCart()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            var cart = await _cartService.GetCartByUserIdAsync(userId);
            
            if (cart == null)
            {
                // Create new cart if doesn't exist
                cart = await _cartService.CreateCartAsync(userId);
            }

            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart");
            return StatusCode(500, "An error occurred while retrieving the cart");
        }
    }

    /// <summary>
    /// Sync local cart with server in a single request
    /// </summary>
    /// <param name="request">Items to merge into the server cart</param>
    /// <returns>Updated server cart</returns>
    [HttpPost("sync")]
    public async Task<ActionResult<CartResponse>> SyncCart([FromBody] SyncCartRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            // If no items provided, just return current cart (do not error)
            if (request?.Items == null || request.Items.Count == 0)
            {
                var current = await _cartService.GetCartByUserIdAsync(userId) ?? await _cartService.CreateCartAsync(userId);
                return Ok(current);
            }

            var cart = await _cartService.SyncCartAsync(userId, request);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid sync request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing cart");
            return StatusCode(500, "An error occurred while syncing the cart");
        }
    }

    /// <summary>
    /// Add item to cart
    /// </summary>
    /// <param name="request">Item to add to cart</param>
    /// <returns>Added cart item</returns>
    [HttpPost("items")]
    public async Task<ActionResult<CartResponse>> AddItemToCart([FromBody] AddCartItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            await _cartService.AddItemToCartAsync(userId, request);
            var cart = await _cartService.GetCartByUserIdAsync(userId);
            if (cart == null) return NotFound("Cart not found");
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            // Product not found or invalid input
            _logger.LogWarning(ex, "Invalid add-to-cart request for product: {ProductId}", request.ProductId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart for product: {ProductId}", request.ProductId);
            return StatusCode(500, "An error occurred while adding item to cart");
        }
    }

    /// <summary>
    /// Update cart item quantity or color
    /// </summary>
    /// <param name="cartItemId">Cart item ID</param>
    /// <param name="request">Update data</param>
    /// <returns>Updated cart item</returns>
    [HttpPut("items/{cartItemId}")]
    public async Task<ActionResult<CartResponse>> UpdateCartItem(int cartItemId, [FromBody] UpdateCartItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            var cartItem = await _cartService.UpdateCartItemAsync(userId, cartItemId, request);
            if (cartItem == null)
            {
                return NotFound($"Cart item with ID {cartItemId} not found");
            }

            var cart = await _cartService.GetCartByUserIdAsync(userId);
            if (cart == null) return NotFound("Cart not found");
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item: {CartItemId}", cartItemId);
            return StatusCode(500, "An error occurred while updating cart item");
        }
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    /// <param name="cartItemId">Cart item ID to remove</param>
    /// <returns>Success status</returns>
    [HttpDelete("items/{cartItemId}")]
    public async Task<ActionResult<CartResponse>> RemoveItemFromCart(int cartItemId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            var success = await _cartService.RemoveItemFromCartAsync(userId, cartItemId);
            if (!success)
            {
                return NotFound($"Cart item with ID {cartItemId} not found");
            }

            var cart = await _cartService.GetCartByUserIdAsync(userId) ?? await _cartService.CreateCartAsync(userId);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item: {CartItemId}", cartItemId);
            return StatusCode(500, "An error occurred while removing cart item");
        }
    }

    /// <summary>
    /// Clear all items from cart
    /// </summary>
    /// <returns>Success status</returns>
    [HttpDelete]
    public async Task<ActionResult> ClearCart()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            var success = await _cartService.ClearCartAsync(userId);
            
            if (!success)
            {
                return NotFound("Cart not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return StatusCode(500, "An error occurred while clearing cart");
        }
    }

    /// <summary>
    /// Get cart item count
    /// </summary>
    /// <returns>Number of items in cart</returns>
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetCartItemCount()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            var count = await _cartService.GetCartItemCountAsync(userId);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart item count");
            return StatusCode(500, "An error occurred while getting cart item count");
        }
    }

    /// <summary>
    /// Get cart total amount
    /// </summary>
    /// <returns>Total cart amount</returns>
    [HttpGet("total")]
    public async Task<ActionResult<decimal>> GetCartTotal()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            var total = await _cartService.GetCartTotalAsync(userId);
            return Ok(total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart total");
            return StatusCode(500, "An error occurred while getting cart total");
        }
    }
}

