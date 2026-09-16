namespace Store.OrderService.Models;

/// <summary>
/// Stored by name. Only <see cref="Placed"/> is reachable today; the others exist so the
/// column, the API and the UI do not have to change when payment and shipping arrive.
/// </summary>
public enum OrderStatus
{
    Placed,
    Paid,
    Shipped,
    Cancelled
}
