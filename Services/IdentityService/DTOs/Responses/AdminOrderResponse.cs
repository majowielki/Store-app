namespace Store.IdentityService.DTOs.Responses;

public class AdminOrderResponse
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? DeliveryAddress { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public decimal OrderTotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
    public List<OrderServiceOrderItemResponse> OrderItems { get; set; } = new();
}

// Add the DTO for order items for admin responses
public class OrderServiceOrderItemResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public decimal LineTotal { get; set; }
    public decimal? DeliveryCost { get; set; }
    public decimal? OrderDiscount { get; set; }
}
