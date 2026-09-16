using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.CartService.Services;
using Store.Contracts.Authorization;
using Store.Contracts.Cart;

namespace Store.CartService.Controllers;

/// <summary>
/// Endpoints for other services, authenticated with the shared internal key instead of a
/// user token: the order service reads a cart at checkout without carrying the customer's
/// credentials around.
/// </summary>
[ApiController]
[Route("api/cart/internal")]
[Authorize(Policy = Policies.InternalService)]
public class InternalCartController : ControllerBase
{
    private readonly ICartService _cartService;

    public InternalCartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>The cart of a user as a snapshot; 404 when the user has no cart yet.</summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult<CartSnapshot>> GetSnapshot(string userId)
    {
        var snapshot = await _cartService.GetSnapshotAsync(userId);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }

    /// <summary>Empties the cart of a user after an order was placed from it.</summary>
    [HttpDelete("{userId}")]
    public async Task<IActionResult> Clear(string userId)
    {
        var result = await _cartService.ClearCartAsync(userId);
        return result.IsSuccess || result.Message == "Cart not found" ? NoContent() : StatusCode(500, result);
    }
}
