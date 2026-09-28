namespace Store.Contracts.Catalog.V1;

/// <summary>
/// Published by the product service when every line of a placed order has been reserved.
/// Consumed by the order saga, which then waits for the payment. Published through the outbox,
/// so it is delivered at least once.
/// </summary>
/// <param name="OrderId">Order the stock is held for</param>
/// <param name="Lines">Units held per product</param>
/// <param name="ReservedAt">When (UTC)</param>
public sealed record StockReserved(int OrderId, IReadOnlyList<StockLine> Lines, DateTime ReservedAt);

/// <summary>Units of one product held for, or given back by, an order.</summary>
/// <param name="ProductId">Catalogue product id</param>
/// <param name="Quantity">Units</param>
public sealed record StockLine(int ProductId, int Quantity);
