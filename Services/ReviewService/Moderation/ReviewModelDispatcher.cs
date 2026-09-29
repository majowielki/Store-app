using MassTransit;
using Microsoft.Extensions.Options;
using Store.ReviewService.Models;

namespace Store.ReviewService.Moderation;

/// <summary>
/// Asks the review service itself to have the model read a review. Published through the outbox
/// with the review, so the model reads only what was saved; kept inside the service.
/// </summary>
/// <param name="ReviewId">The review</param>
public sealed record ReviewAwaitsModel(Guid ReviewId);

/// <summary>Hands a review that was just sent to the model, when there is one to read it.</summary>
public sealed class ReviewModelDispatcher
{
    private readonly IPublishEndpoint _publish;
    private readonly ReviewModelOptions _options;

    public ReviewModelDispatcher(IPublishEndpoint publish, IOptions<ReviewModelOptions> options)
    {
        _publish = publish;
        _options = options.Value;
    }

    /// <summary>Must run before the review is saved: the request goes out with that save.</summary>
    public Task SendAsync(Review review, CancellationToken cancellationToken = default)
        => _options.IsOn ? _publish.Publish(new ReviewAwaitsModel(review.Id), cancellationToken) : Task.CompletedTask;
}
