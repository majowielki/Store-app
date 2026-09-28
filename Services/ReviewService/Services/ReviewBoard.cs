using Microsoft.EntityFrameworkCore;
using Npgsql;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.Contracts.Audit;
using Store.ReviewService.Data;
using Store.ReviewService.DTOs;
using Store.ReviewService.Models;

namespace Store.ReviewService.Services;

/// <summary>
/// The reviews as the shop shows them and customers write them (ADR 012): the published reviews
/// of a product, a customer's own ones in any state, a new review after a paid order, and
/// reports. The administrator's side is <see cref="ReviewModeration"/>.
/// </summary>
public sealed class ReviewBoard
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    /// <summary>The most of their own reviews a customer is shown at once.</summary>
    public const int MaxOwnReviews = 200;

    private readonly ReviewDbContext _context;
    private readonly ReviewSummaries _summaries;
    private readonly IAuditTrail _auditTrail;
    private readonly TimeProvider _time;
    private readonly ILogger<ReviewBoard> _logger;

    public ReviewBoard(ReviewDbContext context, ReviewSummaries summaries, IAuditTrail auditTrail, TimeProvider time, ILogger<ReviewBoard> logger)
    {
        _context = context;
        _summaries = summaries;
        _auditTrail = auditTrail;
        _time = time;
        _logger = logger;
    }

    /// <summary>
    /// The published reviews of a product, with the filters and the order the product page offers.
    /// A visitor of a demo account does not see the ones they reported in this session.
    /// </summary>
    public async Task<PagedResponse<ReviewResponse>> ListAsync(ReviewQuery query, ReviewViewer viewer, CancellationToken cancellationToken = default)
    {
        var reviews = ReviewSummaries.Public(_context.Reviews.AsNoTracking()).Where(r => r.ProductId == query.ProductId);

        if (viewer.DemoSessionId is { } session)
        {
            reviews = reviews.Where(r => !_context.ReviewReports.Any(report => report.ReviewId == r.Id && report.DemoSessionId == session));
        }

        if (query.Rating is >= ReviewConstraints.MinRating and <= ReviewConstraints.MaxRating)
        {
            reviews = reviews.Where(r => r.Rating == query.Rating);
        }

        if (query.Verified == true)
        {
            reviews = reviews.Where(r => r.VerifiedPurchase);
        }

        reviews = query.Sort switch
        {
            ReviewSort.Oldest => reviews.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id),
            ReviewSort.Highest => reviews.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt).ThenBy(r => r.Id),
            ReviewSort.Lowest => reviews.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt).ThenBy(r => r.Id),
            _ => reviews.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
        };

        var paging = new PagedQuery { Page = query.Page ?? 1, PageSize = query.PageSize ?? DefaultPageSize }.Normalized(DefaultPageSize, MaxPageSize);
        var total = await reviews.CountAsync(cancellationToken);
        var page = await reviews.Skip(paging.Skip).Take(paging.PageSize).ToListAsync(cancellationToken);
        return new PagedResponse<ReviewResponse>(page.Select(r => ReviewResponse.From(r)).ToList(), total, paging);
    }

    /// <summary>Whether the customer may review the product, and their review of it in any state.</summary>
    public async Task<MyProductReviewResponse> MineForProductAsync(int productId, ReviewViewer viewer, CancellationToken cancellationToken = default)
    {
        var response = new MyProductReviewResponse { ProductId = productId };
        if (viewer.IsDemoAccount && viewer.DemoSessionId is null)
        {
            response.Reason = ReviewBlockReason.SignInAgain;
            return response;
        }

        var review = await Own(viewer).AsNoTracking().FirstOrDefaultAsync(r => r.ProductId == productId, cancellationToken);
        response.Review = review is null ? null : ReviewResponse.From(review, forAuthor: true);
        response.Reason = review is { Status: not ReviewStatus.Rejected }
            ? ReviewBlockReason.AlreadyReviewed
            : !await HasBoughtAsync(viewer, productId, cancellationToken)
                ? ReviewBlockReason.NotPurchased
                : await SubmittedTodayAsync(viewer, cancellationToken) >= ReviewConstraints.DailyLimit
                    ? ReviewBlockReason.DailyLimit
                    : null;
        response.CanReview = response.Reason is null;
        return response;
    }

    /// <summary>The customer's reviews of every product, newest first.</summary>
    public async Task<IReadOnlyList<ReviewResponse>> MineAsync(ReviewViewer viewer, CancellationToken cancellationToken = default)
    {
        var reviews = await Own(viewer).AsNoTracking()
            .Where(r => r.ProductId != null)
            .OrderByDescending(r => r.SubmittedAt)
            .Take(MaxOwnReviews)
            .ToListAsync(cancellationToken);
        return reviews.Select(r => ReviewResponse.From(r, forAuthor: true)).ToList();
    }

    /// <summary>
    /// A new review of a product the customer has paid for, waiting for the administrator. One per
    /// product (per sign-in session on a demo account, where it lives for a day); a rejected one
    /// may be written again. At most <see cref="ReviewConstraints.DailyLimit"/> a day.
    /// </summary>
    public async Task<ReviewResponse> SubmitAsync(CreateReviewRequest request, ReviewViewer viewer, CancellationToken cancellationToken = default)
    {
        if (viewer.IsDemoAccount && viewer.DemoSessionId is null)
        {
            throw new ForbiddenException("Sign in again to write a review.");
        }

        if (!await HasBoughtAsync(viewer, request.ProductId, cancellationToken))
        {
            throw new ForbiddenException("You can review the products you have bought - this one is not in any of your paid orders.");
        }

        var existing = await Own(viewer).FirstOrDefaultAsync(r => r.ProductId == request.ProductId, cancellationToken);
        if (existing is { Status: not ReviewStatus.Rejected })
        {
            throw new ConflictException("You have already reviewed this product.");
        }

        if (await SubmittedTodayAsync(viewer, cancellationToken) >= ReviewConstraints.DailyLimit)
        {
            throw new DomainValidationException($"You can write {ReviewConstraints.DailyLimit} reviews a day - please come back tomorrow.");
        }

        var now = _time.GetUtcNow().UtcDateTime;
        var review = existing ?? new Review
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            UserId = viewer.UserId,
            Source = ReviewSource.Customer,
            VerifiedPurchase = true,
            DemoSessionId = viewer.DemoSessionId,
            ExpiresAt = viewer.IsDemoAccount ? now + ReviewConstraints.DemoLifetime : null,
            CreatedAt = now
        };

        review.AuthorName = viewer.AuthorName;
        review.Rating = request.Rating;
        review.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        review.Body = request.Body.Trim();
        review.Status = ReviewStatus.Pending;
        review.RejectionReason = null;
        review.ModeratedAt = null;
        review.ModeratedBy = null;
        review.SubmittedAt = now;
        review.UpdatedAt = now;
        if (existing is null)
        {
            _context.Reviews.Add(review);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // The same review sent twice at once: the other request wrote it
            throw new ConflictException("You have already reviewed this product.");
        }

        _logger.LogInformation("Review {ReviewId} of product {ProductId} waits for moderation", review.Id, review.ProductId);
        await _auditTrail.RecordAsync(AuditActions.ReviewSubmitted, nameof(Review), review.Id.ToString(), viewer.UserId,
            details: new { review.ProductId, review.Rating, rewritten = existing is not null, demo = viewer.IsDemoAccount },
            cancellationToken: cancellationToken);
        return ReviewResponse.From(review, forAuthor: true);
    }

    /// <summary>
    /// A visitor's report of a published review. From a customer's account it hides the review
    /// from everyone until the administrator looks at it; from a shared demo account it hides it
    /// from that sign-in session only, for a day. Reporting a review twice changes nothing.
    /// </summary>
    public async Task ReportAsync(Guid reviewId, ReportReviewRequest request, ReviewViewer viewer, CancellationToken cancellationToken = default)
    {
        if (viewer.IsDemoAccount && viewer.DemoSessionId is null)
        {
            throw new ForbiddenException("Sign in again to report a review.");
        }

        var review = await ReviewSummaries.Public(_context.Reviews).FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken)
            ?? throw new NotFoundException(nameof(Review), reviewId);

        if (review.UserId == viewer.UserId && review.DemoSessionId == viewer.DemoSessionId)
        {
            throw new DomainValidationException("You cannot report your own review.");
        }

        var userId = viewer.UserId!;
        var session = viewer.DemoSessionId;
        if (await _context.ReviewReports.AnyAsync(r => r.ReviewId == reviewId && r.ReporterId == userId && r.DemoSessionId == session, cancellationToken))
        {
            return;
        }

        var now = _time.GetUtcNow().UtcDateTime;
        _context.ReviewReports.Add(new ReviewReport
        {
            Id = Guid.NewGuid(),
            ReviewId = reviewId,
            ReporterId = userId,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            DemoSessionId = session,
            ExpiresAt = viewer.IsDemoAccount ? now + ReviewConstraints.DemoLifetime : null,
            CreatedAt = now
        });

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        if (!viewer.IsDemoAccount)
        {
            review.Reported = true;
            review.ReportCount++;
            review.UpdatedAt = now;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Reported twice at once: the other request did it
            return;
        }

        if (!viewer.IsDemoAccount)
        {
            // The review left the rating everyone sees
            await _summaries.PublishAsync([review.ProductId!.Value], cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        await _auditTrail.RecordAsync(AuditActions.ReviewReported, nameof(Review), reviewId.ToString(), userId,
            details: new { review.ProductId, hidden = !viewer.IsDemoAccount, demo = viewer.IsDemoAccount },
            cancellationToken: cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation("Review {ReviewId} reported; hidden from everyone: {Hidden}", reviewId, !viewer.IsDemoAccount);
    }

    /// <summary>The viewer's reviews: of their account, or of this sign-in session of a demo account.</summary>
    private IQueryable<Review> Own(ReviewViewer viewer)
    {
        var userId = viewer.UserId;
        var session = viewer.DemoSessionId;
        if (userId is null || (viewer.IsDemoAccount && session is null))
        {
            return _context.Reviews.Where(_ => false);
        }

        return viewer.IsDemoAccount
            ? _context.Reviews.Where(r => r.UserId == userId && r.DemoSessionId == session)
            : _context.Reviews.Where(r => r.UserId == userId && r.DemoSessionId == null);
    }

    private Task<bool> HasBoughtAsync(ReviewViewer viewer, int productId, CancellationToken cancellationToken)
        => _context.Purchases.AnyAsync(p => p.UserId == viewer.UserId && p.ProductId == productId, cancellationToken);

    /// <summary>Reviews the viewer sent within <see cref="ReviewConstraints.DailyLimitWindow"/>, rewritten ones included.</summary>
    private Task<int> SubmittedTodayAsync(ReviewViewer viewer, CancellationToken cancellationToken)
    {
        var since = _time.GetUtcNow().UtcDateTime - ReviewConstraints.DailyLimitWindow;
        return Own(viewer).CountAsync(r => r.SubmittedAt > since, cancellationToken);
    }
}
