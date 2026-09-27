namespace Store.CartService.DTOs.Responses;

/// <summary>The customer's wishlist, the piece added last first.</summary>
public class WishlistResponse
{
    public List<WishlistItemResponse> Items { get; set; } = new();
}

public class WishlistItemResponse
{
    public int ProductId { get; set; }
    public DateTime AddedAt { get; set; }
}
