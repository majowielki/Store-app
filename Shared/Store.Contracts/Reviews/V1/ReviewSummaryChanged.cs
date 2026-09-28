namespace Store.Contracts.Reviews.V1;

/// <summary>
/// Published by the review service when the published reviews of a product change (one is
/// approved, hidden after a report, or removed). Consumed by the product service, which keeps
/// the average and the count for listings and sorting. Messages may arrive out of order: a
/// consumer keeps the summary with the latest <paramref name="ChangedAt"/>.
/// </summary>
/// <param name="ProductId">Catalogue product id</param>
/// <param name="AverageRating">Average of the published ratings (1-5), 0 without reviews</param>
/// <param name="ReviewCount">Number of published reviews</param>
/// <param name="ChangedAt">When the summary was computed (UTC)</param>
public sealed record ReviewSummaryChanged(int ProductId, decimal AverageRating, int ReviewCount, DateTime ChangedAt);
