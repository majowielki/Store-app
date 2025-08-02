using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Store.Shared.Models;

public class Cart
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public List<CartItem> CartItems { get; set; } = new();
    
    public int TotalItems => CartItems?.Sum(item => item.Amount) ?? 0;
    
    public decimal Total => CartItems?.Sum(item => item.LineTotal) ?? 0; // Simplified total calculation

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsEmpty => !CartItems.Any();
}
