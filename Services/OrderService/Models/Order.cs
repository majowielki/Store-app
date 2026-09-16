namespace Store.OrderService.Models;

/// <summary>
/// An order as OrderService owns it. Amounts are stored, not derived: the discount and the
/// delivery fee are columns of the order, never lines pretending to be products.
/// </summary>
public class Order
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string? DeliveryAddress { get; set; }

    public string? Notes { get; set; }

    public List<OrderLine> Lines { get; set; } = new();

    /// <summary>Sum of the lines before discount and delivery.</summary>
    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    /// <summary>Why the discount was granted, e.g. <see cref="PricingPolicy.FirstOrderDiscountReason"/>.</summary>
    public string? DiscountReason { get; set; }

    public decimal DeliveryFee { get; set; }

    /// <summary>What the customer pays: subtotal - discount + delivery.</summary>
    public decimal Total { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Placed;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int TotalItems => Lines.Sum(line => line.Quantity);
}

/// <summary>A product line of an order: what was bought, at what unit price, how many.</summary>
public class OrderLine
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }

    public string ProductTitle { get; set; } = string.Empty;

    public string ProductImage { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; } = 1;

    public decimal LineTotal => UnitPrice * Quantity;
}
