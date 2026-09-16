namespace Store.CartService.DTOs.Requests;

/// <summary>Body of PUT /api/cart/items/{id}; absent fields keep their value. Rules: <c>UpdateCartItemRequestValidator</c>.</summary>
public class UpdateCartItemRequest
{
    public int? Quantity { get; set; }

    public string? Color { get; set; }
}
