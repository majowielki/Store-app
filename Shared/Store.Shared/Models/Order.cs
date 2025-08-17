using Store.Shared.Utility;
using System.ComponentModel.DataAnnotations;

namespace Store.Shared.Models;

public class Order
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string UserEmail { get; set; } = string.Empty;
    
    [StringLength(300)]
    public string? DeliveryAddress { get; set; } // Simplified single address field
    
    [Required]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;
    
    public List<OrderItem> OrderItems { get; set; } = new();
    
    public int TotalItems => OrderItems?.Sum(item => item.Quantity) ?? 0;
    
    public decimal OrderTotal => OrderItems?.Sum(item => item.LineTotal) ?? 0;
    
    public decimal DeliveryFee => OrderTotal < 299 ? 10 : 0;
    
    public OrderStatus Status { get; set; } = OrderStatus.Completed; // Always completed for simplicity
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(500)]
    public string? Notes { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string ProductTitle { get; set; } = string.Empty;
    
    [Required]
    public string ProductImage { get; set; } = string.Empty; // Keep images as requested
    
    [Range(0.01, 999999.99)]
    public decimal Price { get; set; }
    
    [Range(1, 999)]
    public int Quantity { get; set; } = 1;
    
    [Required]
    [StringLength(50)]
    public string Color { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Company { get; set; } = string.Empty;
    
    public decimal LineTotal => Price * Quantity;

    public decimal? DeliveryCost { get; set; } // New field for delivery cost

    public decimal? OrderDiscount { get; set; } // New field for first order discount
    
    // Navigation property to Product (optional, for reference)
    public Product? Product { get; set; }
}

public class OrderItemResponse
{
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string ProductTitle { get; set; } = string.Empty;
    
    [Required]
    public string ProductImage { get; set; } = string.Empty; // Keep images as requested
    
    [Range(0.01, 999999.99)]
    public decimal Price { get; set; }
    
    [Range(1, 999)]
    public int Quantity { get; set; } = 1;
    
    [Required]
    [StringLength(50)]
    public string Color { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Company { get; set; } = string.Empty;
    
    public decimal LineTotal => Price * Quantity;

    public decimal? DeliveryCost { get; set; } // Include delivery cost in response

    public decimal? OrderDiscount { get; set; } // Include order discount in response
}

