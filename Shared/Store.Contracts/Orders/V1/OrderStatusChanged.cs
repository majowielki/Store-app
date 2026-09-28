namespace Store.Contracts.Orders.V1;

/// <summary>
/// Published by the order service when an order moves to another status (awaiting payment,
/// paid, shipped, cancelled, refunded). Consumers: the audit service. Published through the
/// outbox with the change, so it is delivered at least once - every consumer must be idempotent.
/// </summary>
/// <param name="OrderId">Id of the order in the order service</param>
/// <param name="UserId">Customer the order belongs to</param>
/// <param name="PreviousStatus">Status the order left</param>
/// <param name="Status">Status the order entered</param>
/// <param name="ChangedBy">User id of the administrator who changed it; null when the order saga did</param>
/// <param name="ChangedAt">When (UTC)</param>
public sealed record OrderStatusChanged(
    int OrderId,
    string UserId,
    string PreviousStatus,
    string Status,
    string? ChangedBy,
    DateTime ChangedAt);
