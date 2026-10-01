using Store.ReviewService.Models;

namespace Store.ReviewService.Moderation;

/// <summary>A first reader of new reviews: it tells the clean ones from those a person should read (ADR 019).</summary>
public interface IReviewModel
{
    /// <summary>
    /// The verdict on a review; null when there is none to be had - the model is off, unreachable
    /// or overloaded - and the review simply waits for the administrator.
    /// </summary>
    Task<ModelJudgement?> JudgeAsync(ReviewForModel review, CancellationToken cancellationToken = default);
}

/// <summary>What the model reads: the stars and the words, nothing about who wrote them.</summary>
public sealed record ReviewForModel(int Rating, string? Title, string Body);

/// <summary>The model's verdict and its one sentence on why.</summary>
public sealed record ModelJudgement(ModelVerdict Verdict, string Reason);

/// <summary>Where no model is configured: no verdict, every review waits for the administrator.</summary>
public sealed class NoReviewModel : IReviewModel
{
    public Task<ModelJudgement?> JudgeAsync(ReviewForModel review, CancellationToken cancellationToken = default)
        => Task.FromResult<ModelJudgement?>(null);
}
