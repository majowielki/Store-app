namespace Store.CartService.DTOs.Responses;

public class CartResponse
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<CartItemResponse> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public decimal Total { get; set; } // Simplified total without shipping/tax
    public DateTime UpdatedAt { get; set; }
    public bool IsEmpty { get; set; }
}