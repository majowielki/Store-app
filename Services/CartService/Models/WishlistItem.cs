namespace Store.CartService.Models;

/// <summary>
/// A product a customer keeps for later. Only the id is stored: the list is shown with the
/// catalogue's current data, and a product the catalogue no longer lists simply is not shown.
/// </summary>
public class WishlistItem
{
    public const int MaxItems = 100;

    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int ProductId { get; set; }

    public DateTime AddedAt { get; set; }
}
