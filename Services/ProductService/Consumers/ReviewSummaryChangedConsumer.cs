using MassTransit;
using Microsoft.EntityFrameworkCore;
using Store.Contracts.Reviews.V1;
using Store.ProductService.Data;

namespace Store.ProductService.Consumers;

/// <summary>
/// Keeps the copy of a product's rating the listings show and sort by. Summaries may arrive out of
/// order, so one computed before the copy held is ignored.
/// </summary>
public sealed class ReviewSummaryChangedConsumer : IConsumer<ReviewSummaryChanged>
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ReviewSummaryChangedConsumer> _logger;

    public ReviewSummaryChangedConsumer(ProductDbContext context, ILogger<ReviewSummaryChangedConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReviewSummaryChanged> context)
    {
        var summary = context.Message;
        var updated = await _context.Products
            .Where(p => p.Id == summary.ProductId && (p.RatingChangedAt == null || p.RatingChangedAt < summary.ChangedAt))
            .ExecuteUpdateAsync(set => set
                .SetProperty(p => p.RatingAverage, summary.AverageRating)
                .SetProperty(p => p.RatingCount, summary.ReviewCount)
                .SetProperty(p => p.RatingChangedAt, summary.ChangedAt), context.CancellationToken);

        if (updated == 0)
        {
            _logger.LogInformation("Rating of product {ProductId} from {ChangedAt} ignored: unknown product or a newer rating held", summary.ProductId, summary.ChangedAt);
        }
    }
}
