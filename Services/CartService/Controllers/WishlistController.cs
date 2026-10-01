using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.BuildingBlocks.Authorization;
using Store.CartService.DTOs.Requests;
using Store.CartService.DTOs.Responses;
using Store.CartService.Services;

namespace Store.CartService.Controllers;

/// <summary>
/// The signed-in customer's wishlist. A visitor keeps theirs in the browser and it is merged
/// here at sign-in. Every change answers with the whole list afterwards.
/// </summary>
[ApiController]
[Route("api/v1/wishlist")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly WishlistService _wishlist;

    public WishlistController(WishlistService wishlist)
    {
        _wishlist = wishlist;
    }

    private string UserId => User.GetRequiredUserId();

    /// <summary>The product ids on the list, the one added last first.</summary>
    [HttpGet]
    public Task<WishlistResponse> GetWishlist() => _wishlist.GetAsync(UserId, cancellationToken: HttpContext.RequestAborted);

    /// <summary>Adds a product; one already on the list stays as it is. 422 for a product that is not for sale.</summary>
    [HttpPost("items")]
    public Task<WishlistResponse> AddItem([FromBody] AddWishlistItemRequest request) => _wishlist.AddAsync(UserId, request.ProductId, cancellationToken: HttpContext.RequestAborted);

    /// <summary>Removes a product; one that is not on the list changes nothing.</summary>
    [HttpDelete("items/{productId:int}")]
    public Task<WishlistResponse> RemoveItem(int productId) => _wishlist.RemoveAsync(UserId, productId, cancellationToken: HttpContext.RequestAborted);

    /// <summary>Merges the list a visitor kept in the browser, in one request.</summary>
    [HttpPost("sync")]
    public Task<WishlistResponse> Sync([FromBody] SyncWishlistRequest request) => _wishlist.SyncAsync(UserId, request.ProductIds, cancellationToken: HttpContext.RequestAborted);
}
