using MassTransit;
using Microsoft.EntityFrameworkCore;
using Store.Contracts.Reviews.V1;
using Store.ReviewService.Data;
using Store.ReviewService.DTOs;
using Store.ReviewService.Models;

namespace Store.ReviewService.Services;

/// <summary>
/// The rating of a product: the average and the count of its reviews everyone can see. The
/// product service keeps a copy for listings and sorting, sent as <see cref="ReviewSummaryChanged"/>
/// whenever a review joins or leaves that set.
/// </summary>
public sealed class ReviewSummaries
{
    private readonly ReviewDbContext _context;
    private readonly IPublishEndpoint _publish;
    private readonly TimeProvider _time;

    public ReviewSummaries(ReviewDbContext context, IPublishEndpoint publish, TimeProvider time)
    {
        _context = context;
        _publish = publish;
        _time = time;
    }

    /// <summary>The reviews everyone sees: published and not hidden by a report.</summary>
    public static IQueryable<Review> Public(IQueryable<Review> reviews)
        => reviews.Where(r => r.Status == ReviewStatus.Published && !r.Reported && r.ProductId != null);

    /// <summary>The summary of each product in <paramref name="productIds"/>, zeros for one without reviews.</summary>
    public async Task<IReadOnlyList<ReviewSummaryResponse>> ForAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken = default)
    {
        var ids = productIds.Distinct().ToArray();
        var counts = await Public(_context.Reviews.AsNoTracking())
            .Where(r => ids.Contains(r.ProductId!.Value))
            .GroupBy(r => new { ProductId = r.ProductId!.Value, r.Rating })
            .Select(g => new { g.Key.ProductId, g.Key.Rating, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return ids.Select(id =>
        {
            var distribution = Enumerable.Range(ReviewConstraints.MinRating, ReviewConstraints.MaxRating - ReviewConstraints.MinRating + 1).ToDictionary(
                stars => stars.ToString(System.Globalization.CultureInfo.InvariantCulture),
                stars => counts.Where(c => c.ProductId == id && c.Rating == stars).Sum(c => c.Count));
            var count = distribution.Values.Sum();
            var total = counts.Where(c => c.ProductId == id).Sum(c => c.Rating * c.Count);
            return new ReviewSummaryResponse
            {
                ProductId = id,
                ReviewCount = count,
                AverageRating = count == 0 ? 0 : Math.Round((decimal)total / count, 2, MidpointRounding.AwayFromZero),
                Distribution = distribution
            };
        }).ToList();
    }

    /// <summary>
    /// Publishes the current summary of each product through the outbox; the caller's next save
    /// writes the messages, in its transaction when it has one.
    /// </summary>
    public async Task PublishAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default)
    {
        var ids = productIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        var now = _time.GetUtcNow().UtcDateTime;
        foreach (var summary in await ForAsync(ids, cancellationToken))
        {
            await _publish.Publish(new ReviewSummaryChanged(summary.ProductId, summary.AverageRating, summary.ReviewCount, now), cancellationToken);
        }
    }
}
