namespace Store.OrderService.Models;

/// <summary>
/// Stored by name. An order is placed at checkout, waits for its payment once the stock is
/// reserved, is paid when the payment provider confirms it and shipped by the administrator.
/// The order saga moves it along <see cref="OrderStatusFlow"/>; the administrator only ships
/// and cancels.
/// </summary>
public enum OrderStatus
{
    /// <summary>Checked out; the stock is being reserved.</summary>
    Placed,

    /// <summary>The stock is held; the customer has until the payment deadline to pay.</summary>
    AwaitingPayment,

    Paid,

    Shipped,

    Cancelled,

    /// <summary>Cancelled after it had been paid, and the money has been returned.</summary>
    Refunded
}

/// <summary>
/// The ways an order may move: placed → awaiting payment → paid → shipped; cancelled from any
/// status before shipping; refunded from cancelled once the money of a paid order is back.
/// Shipped and refunded are final. Kept apart from EF and HTTP so it can be unit tested.
/// </summary>
public static class OrderStatusFlow
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Next = new()
    {
        [OrderStatus.Placed] = [OrderStatus.AwaitingPayment, OrderStatus.Cancelled],
        [OrderStatus.AwaitingPayment] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [],
        [OrderStatus.Cancelled] = [OrderStatus.Refunded],
        [OrderStatus.Refunded] = []
    };

    /// <summary>The moves an administrator makes by hand; the rest belong to the saga.</summary>
    private static readonly OrderStatus[] ByAdministrator = [OrderStatus.Shipped, OrderStatus.Cancelled];

    /// <summary>The statuses an order in <paramref name="status"/> may move to, in the order they usually come.</summary>
    public static IReadOnlyList<OrderStatus> NextFrom(OrderStatus status) => Next[status];

    public static bool CanMove(OrderStatus from, OrderStatus to) => Next[from].Contains(to);

    /// <summary>What the administrator can do with an order in <paramref name="status"/>: ship a paid one, cancel one not shipped.</summary>
    public static IReadOnlyList<OrderStatus> AdministratorMovesFrom(OrderStatus status)
        => Next[status].Where(ByAdministrator.Contains).ToList();

    /// <summary>True for the statuses an administrator may set by hand.</summary>
    public static bool IsAdministratorMove(OrderStatus to) => ByAdministrator.Contains(to);
}

/// <summary>One step of an order's history: the status it entered, when and by whom.</summary>
public class OrderStatusChange
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime ChangedAt { get; set; }

    /// <summary>The customer for "placed", the administrator for a change by hand, none for the saga.</summary>
    public string? ChangedBy { get; set; }
}
