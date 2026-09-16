namespace Store.CartService.DTOs.Requests;

/// <summary>Body of POST /api/cart/items. Rules: <c>AddCartItemRequestValidator</c>.</summary>
public class AddCartItemRequest
{
    public int ProductId { get; set; }

    public int Quantity { get; set; } = 1;

    public string Color { get; set; } = string.Empty;
}
