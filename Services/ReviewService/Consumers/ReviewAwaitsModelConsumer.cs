using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.ReviewService.Data;
using Store.ReviewService.Moderation;
using Store.ReviewService.Services;

namespace Store.ReviewService.Consumers;

/// <summary>
/// Lets the model read a review that was just sent (ADR 019): a clean one is published, a doubtful
/// one waits for the administrator with the model's reason, and without a verdict (the model is
/// unreachable) it simply waits. A review moderated, rewritten or deleted meanwhile is left alone.
/// </summary>
public sealed class ReviewAwaitsModelConsumer : IConsumer<ReviewAwaitsModel>
{
    private readonly ReviewDbContext _context;
    private readonly IReviewModel _model;
    private readonly ReviewModeration _moderation;
    private readonly ReviewModelOptions _options;

    public ReviewAwaitsModelConsumer(ReviewDbContext context, IReviewModel model, ReviewModeration moderation, IOptions<ReviewModelOptions> options)
    {
        _context = context;
        _model = model;
        _moderation = moderation;
        _options = options.Value;
    }

    public async Task Consume(ConsumeContext<ReviewAwaitsModel> context)
    {
        var review = await _context.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.Id == context.Message.ReviewId, context.CancellationToken);
        if (review is null || !ReviewModeration.AwaitsModel(review, review.SubmittedAt))
        {
            return;
        }

        var judgement = await _model.JudgeAsync(new ReviewForModel(review.Rating, review.Title, review.Body), context.CancellationToken);
        if (judgement is not null)
        {
            // What was read is what was sent at SubmittedAt; a rewrite since then gets its own reading
            await _moderation.ApplyModelVerdictAsync(review.Id, review.SubmittedAt, judgement, _options.Model, context.CancellationToken);
        }
    }
}

/// <summary>Few reviews at once: each holds a database connection while the model answers.</summary>
public sealed class ReviewAwaitsModelConsumerDefinition : ConsumerDefinition<ReviewAwaitsModelConsumer>
{
    public ReviewAwaitsModelConsumerDefinition(IOptions<ReviewModelOptions> options)
    {
        ConcurrentMessageLimit = options.Value.ConcurrentReviews;
    }
}
