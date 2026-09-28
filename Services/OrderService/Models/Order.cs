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

    /// <summary>Why the discount was granted: <see cref="PricingPolicy.FirstOrderDiscountReason"/> or <see cref="PricingPolicy.CodeDiscountReason"/>.</summary>
    public string? DiscountReason { get; set; }

    /// <summary>The discount code the order was discounted by; none when there was none or the first-order discount was larger.</summary>
    public string? DiscountCode { get; set; }

    public decimal DeliveryFee { get; set; }

    /// <summary>What the customer pays: subtotal - discount + delivery.</summary>
    public decimal Total { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Placed;

    /// <summary>Every status the order has been in, "placed" first.</summary>
    public List<OrderStatusChange> StatusHistory { get; set; } = new();

    /// <summary>The delivery window promised at checkout (<see cref="DeliveryPolicy"/>); none for older orders.</summary>
    public DateOnly? DeliveryFrom { get; set; }

    public DateOnly? DeliveryTo { get; set; }

    /// <summary>When the customer must have paid by; set once the stock is reserved.</summary>
    public DateTime? PaymentDueAt { get; set; }

    /// <summary>The payment at the payment service, once the customer has started paying.</summary>
    public Guid? PaymentId { get; set; }

    /// <summary>Brand of the card that paid (visa, mastercard); the full number never reaches this service.</summary>
    public string? CardBrand { get; set; }

    /// <summary>Last four digits of that card.</summary>
    public string? CardLast4 { get; set; }

    /// <summary>Why the order was cancelled (<c>OrderCancellationReasons</c>); none while it is not.</summary>
    public string? CancellationReason { get; set; }

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
