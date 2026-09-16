namespace Store.OrderService.Models;

/// <summary>
/// What the order service remembers about a customer. The row is locked while an order is
/// placed, so two orders arriving at once for the same customer are processed one after the
/// other and only the first of them can be "the first order".
/// </summary>
public class Customer
{
    public string UserId { get; set; } = string.Empty;

    public int OrdersPlaced { get; set; }

    public DateTime? FirstOrderAt { get; set; }

    public DateTime? LastOrderAt { get; set; }
}
