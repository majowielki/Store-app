using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.CartService.Services;
using Store.Contracts.Authorization;
using Store.Contracts.Cart;

namespace Store.CartService.Controllers;

/// <summary>
/// Endpoints for other services, authenticated with the shared internal key instead of a
/// user token: the order service reads a cart at checkout without carrying the customer's
/// credentials around. Emptying the cart afterwards happens through the order-placed event.
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

}
