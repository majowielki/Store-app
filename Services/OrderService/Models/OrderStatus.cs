namespace Store.OrderService.Models;

/// <summary>
/// Stored by name. An order is placed at checkout; until payments arrive the administrator
/// moves it on (paid, shipped) or cancels it, following <see cref="OrderStatusFlow"/>.
/// </summary>
public enum OrderStatus
{
    Placed,
    Paid,
    Shipped,
    Cancelled
}

/// <summary>
/// The ways an order may move: placed → paid → shipped, and cancelled from placed or paid.
/// Shipped and cancelled are final. Kept apart from EF and HTTP so it can be unit tested.
/// </summary>
public static class OrderStatusFlow
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Next = new()
    {
        [OrderStatus.Placed] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [],
        [OrderStatus.Cancelled] = []
    };

    /// <summary>The statuses an order in <paramref name="status"/> may move to, in the order they usually come.</summary>
    public static IReadOnlyList<OrderStatus> NextFrom(OrderStatus status) => Next[status];

    public static bool CanMove(OrderStatus from, OrderStatus to) => Next[from].Contains(to);
}

/// <summary>One step of an order's history: the status it entered, when and by whom.</summary>
public class OrderStatusChange
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime ChangedAt { get; set; }

    /// <summary>The customer for "placed", the administrator for a later change.</summary>
    public string? ChangedBy { get; set; }
}
