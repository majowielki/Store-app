namespace Store.CartService.DTOs.Requests;

/// <summary>Body of POST /api/v1/wishlist/items.</summary>
public class AddWishlistItemRequest
{
    public int ProductId { get; set; }
}

/// <summary>Body of POST /api/v1/wishlist/sync: the list a visitor kept in the browser, merged at sign-in.</summary>
public class SyncWishlistRequest
{
    public List<int> ProductIds { get; set; } = new();
}
