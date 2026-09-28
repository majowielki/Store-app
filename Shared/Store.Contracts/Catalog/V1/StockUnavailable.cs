namespace Store.Contracts.Catalog.V1;

/// <summary>
/// Published by the product service when a placed order asks for more units than are
/// available. Nothing is reserved - an order gets all of its lines or none. Consumed by the
/// order saga, which cancels the order. Published through the outbox, so it is delivered at
/// least once.
/// </summary>
/// <param name="OrderId">Order that could not be served</param>
/// <param name="Shortages">The lines that lacked stock</param>
/// <param name="CheckedAt">When (UTC)</param>
public sealed record StockUnavailable(int OrderId, IReadOnlyList<StockShortage> Shortages, DateTime CheckedAt);

/// <summary>A line of an order the stock could not cover.</summary>
/// <param name="ProductId">Catalogue product id</param>
/// <param name="ProductTitle">Its title, for messages to the customer</param>
/// <param name="Requested">Units the order asked for</param>
/// <param name="Available">Units that were available</param>
public sealed record StockShortage(int ProductId, string ProductTitle, int Requested, int Available);
