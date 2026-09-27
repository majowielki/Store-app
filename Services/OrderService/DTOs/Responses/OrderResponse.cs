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

    /// <summary>The discount code the order was discounted by, if any.</summary>
    public string? DiscountCode { get; set; }
    public decimal DeliveryFee { get; set; }

    /// <summary>What the customer pays.</summary>
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>Every status the order has been in, oldest first, with the time it changed.</summary>
    public List<OrderStatusChangeResponse> StatusHistory { get; set; } = new();

    /// <summary>The statuses the order may still move to; empty once it is shipped or cancelled.</summary>
    public List<string> NextStatuses { get; set; } = new();

    /// <summary>First and last day of the delivery window promised at checkout; null for older orders.</summary>
    public DateOnly? DeliveryFrom { get; set; }
    public DateOnly? DeliveryTo { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
}

public class OrderStatusChangeResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
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
