namespace Store.Contracts.Catalog.V1;

/// <summary>
/// Published by the product service when the units held for a cancelled order are available
/// again. Published through the outbox, so it is delivered at least once.
/// </summary>
/// <param name="OrderId">Order that held them</param>
/// <param name="Lines">Units given back per product; empty when the order held nothing</param>
/// <param name="ReleasedAt">When (UTC)</param>
public sealed record StockReleased(int OrderId, IReadOnlyList<StockLine> Lines, DateTime ReleasedAt);
