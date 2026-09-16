namespace Store.CartService.Models;

/// <summary>A customer's shopping cart; one per user, created on first use.</summary>
public class Cart
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public List<CartItem> Items { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int TotalItems => Items.Sum(item => item.Quantity);

    public decimal Total => Items.Sum(item => item.LineTotal);

    public bool IsEmpty => Items.Count == 0;
}
