using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.Contracts.Audit;
using Store.ReviewService.Data;
using Store.ReviewService.DTOs;
using Store.ReviewService.Models;

namespace Store.ReviewService.Services;

/// <summary>
/// The administrator's side of the reviews (ADR 012): the queue of new and reported reviews, and
/// approving or rejecting them one by one or many at once. Every decision is audited and a change
/// to what everyone sees updates the product's rating.
/// </summary>
public sealed class ReviewModeration
{
    public const int DefaultPageSize = 20;

    /// <summary>What the demo administrator gets instead of text nobody has approved (ADR 012: nothing a visitor writes is shown unread).</summary>
    public const string HiddenAuthor = "Hidden from the demo administrator";
    public const string HiddenText = "Only the true administrator reads a review before it is published.";
    public const string AnonymizedUserId = "anonymized-user-id";

    private readonly ReviewDbContext _context;
    private readonly ReviewSummaries _summaries;
    private readonly IAuditTrail _auditTrail;
    private readonly TimeProvider _time;
    private readonly ILogger<ReviewModeration> _logger;

    public ReviewModeration(ReviewDbContext context, ReviewSummaries summaries, IAuditTrail auditTrail, TimeProvider time, ILogger<ReviewModeration> logger)
    {
        _context = context;
        _summaries = summaries;
        _auditTrail = auditTrail;
        _time = time;
        _logger = logger;
    }

    /// <summary>
    /// Reviews by state; the queue (new and reported ones) oldest first, the rest newest first.
    /// The demo administrator sees the texts only of published reviews and no one's account.
    /// </summary>
    public async Task<PagedResponse<AdminReviewResponse>> ListAsync(AdminReviewQuery query, ReviewViewer viewer, CancellationToken cancellationToken = default)
    {
        var reviews = _context.Reviews.AsNoTracking();
        var queue = query.Status ?? ReviewQueueFilter.Queue;
        reviews = queue switch
        {
            ReviewQueueFilter.Pending => reviews.Where(r => r.Status == ReviewStatus.Pending),
            ReviewQueueFilter.Reported => reviews.Where(r => r.Reported),
            ReviewQueueFilter.Published => reviews.Where(r => r.Status == ReviewStatus.Published),
            ReviewQueueFilter.Rejected => reviews.Where(r => r.Status == ReviewStatus.Rejected),
            ReviewQueueFilter.All => reviews,
            _ => reviews.Where(r => r.Status == ReviewStatus.Pending || r.Reported)
        };

        if (query.ProductId is { } productId)
        {
            reviews = reviews.Where(r => r.ProductId == productId);
        }

        var waiting = queue is ReviewQueueFilter.Queue or ReviewQueueFilter.Pending or ReviewQueueFilter.Reported;
        reviews = waiting
            ? reviews.OrderBy(r => r.SubmittedAt).ThenBy(r => r.Id)
            : reviews.OrderByDescending(r => r.SubmittedAt).ThenBy(r => r.Id);

        var paging = new PagedQuery { Page = query.Page ?? 1, PageSize = query.PageSize ?? DefaultPageSize }.Normalized(DefaultPageSize);
        var total = await reviews.CountAsync(cancellationToken);
        var page = await reviews.Skip(paging.Skip).Take(paging.PageSize).ToListAsync(cancellationToken);

        var ids = page.Select(r => r.Id).ToArray();
        var reasons = (await _context.ReviewReports.AsNoTracking()
                .Where(r => ids.Contains(r.ReviewId) && r.DemoSessionId == null && r.Reason != null)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new { r.ReviewId, r.Reason })
                .ToListAsync(cancellationToken))
            .ToLookup(r => r.ReviewId, r => r.Reason!);

        return new PagedResponse<AdminReviewResponse>(page.Select(r => Map(r, reasons[r.Id].ToList(), viewer.IsDemoAdmin)).ToList(), total, paging);
    }

    /// <summary>
    /// Approves (publishes; a reported review is shown again) or rejects, with a reason its author
    /// sees, each of the reviews. Ids of reviews that no longer exist are returned, not refused.
    /// </summary>
    public async Task<ModerationResult> ModerateAsync(ModerateReviewsRequest request, string actorId, CancellationToken cancellationToken = default)
    {
        var ids = request.Ids.Distinct().ToArray();
        var now = _time.GetUtcNow().UtcDateTime;
        var approve = request.Decision == ModerationDecision.Approve;
        var reason = approve ? null : request.Reason?.Trim();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var reviews = await _context.Reviews.Where(r => ids.Contains(r.Id)).ToListAsync(cancellationToken);
        var changedRatings = new HashSet<int>();
        foreach (var review in reviews)
        {
            var wasPublic = IsPublic(review);
            review.Status = approve ? ReviewStatus.Published : ReviewStatus.Rejected;
            review.Reported = false;
            review.RejectionReason = reason;
            review.ModeratedAt = now;
            review.ModeratedBy = actorId;
            review.UpdatedAt = now;
            if (wasPublic != IsPublic(review) && review.ProductId is { } productId)
            {
                changedRatings.Add(productId);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _summaries.PublishAsync(changedRatings, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var review in reviews)
        {
            await _auditTrail.RecordAsync(approve ? AuditActions.ReviewApproved : AuditActions.ReviewRejected, nameof(Review), review.Id.ToString(), actorId,
                details: new { review.ProductId, review.Rating, source = review.Source.ToString(), reason },
                cancellationToken: cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation("{Count} reviews {Decision}; ratings of {Products} products changed", reviews.Count, approve ? "approved" : "rejected", changedRatings.Count);
        return new ModerationResult
        {
            Moderated = reviews.Count,
            NotFound = ids.Except(reviews.Select(r => r.Id)).ToList()
        };
    }

    private static bool IsPublic(Review review) => review is { Status: ReviewStatus.Published, Reported: false };

    private static AdminReviewResponse Map(Review review, List<string> reportReasons, bool forDemoAdmin)
    {
        // Published text was approved by a person or came with the catalogue; anything else a
        // visitor wrote stays with the true administrator
        var hideText = forDemoAdmin && review.Status != ReviewStatus.Published;
        return new AdminReviewResponse
        {
            Id = review.Id,
            ProductId = review.ProductId ?? 0,
            ProductSlug = review.ProductSlug,
            UserId = forDemoAdmin && review.UserId is not null ? AnonymizedUserId : review.UserId,
            AuthorName = hideText ? HiddenAuthor : review.AuthorName,
            Rating = review.Rating,
            Title = hideText ? null : review.Title,
            Body = hideText ? HiddenText : review.Body,
            Status = review.Status,
            Reported = review.Reported,
            ReportCount = review.ReportCount,
            ReportReasons = forDemoAdmin ? [] : reportReasons,
            RejectionReason = review.RejectionReason,
            Source = review.Source,
            VerifiedPurchase = review.VerifiedPurchase,
            Demo = review.DemoSessionId is not null,
            ExpiresAt = review.ExpiresAt,
            CreatedAt = review.CreatedAt,
            SubmittedAt = review.SubmittedAt,
            ModeratedAt = review.ModeratedAt,
            ModeratedBy = forDemoAdmin && review.ModeratedBy is not null ? AnonymizedUserId : review.ModeratedBy
        };
    }
}
