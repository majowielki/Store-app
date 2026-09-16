namespace Store.OrderService.DTOs.Responses;

public class OrderResponse
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? DeliveryAddress { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderItemResponse> OrderItems { get; set; } = new();
    public int TotalItems { get; set; }

    /// <summary>Sum of the lines before discount and delivery.</summary>
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountReason { get; set; }
    public decimal DeliveryFee { get; set; }

    /// <summary>What the customer pays.</summary>
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
}

public class OrderItemResponse
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
}
