using Store.ReviewService.Models;

namespace Store.ReviewService.DTOs;

/// <summary>Query of GET /api/v1/reviews: the published reviews of one product.</summary>
public class ReviewQuery
{
    public int ProductId { get; set; }

    /// <summary>Only the reviews with this many stars (1-5).</summary>
    public int? Rating { get; set; }

    /// <summary>true: only reviews by customers who bought the product.</summary>
    public bool? Verified { get; set; }

    /// <summary>newest (default), oldest, highest or lowest.</summary>
    public string? Sort { get; set; }

    public int? Page { get; set; }

    public int? PageSize { get; set; }
}

/// <summary>A review as the shop prints it.</summary>
public class ReviewResponse
{
    public Guid Id { get; set; }

    public int ProductId { get; set; }

    /// <summary>A first name and an initial.</summary>
    public string AuthorName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Title { get; set; }

    public string Body { get; set; } = string.Empty;

    public bool VerifiedPurchase { get; set; }

    /// <summary>published for everyone; pending or rejected only in its author's own view.</summary>
    public ReviewStatus Status { get; set; }

    /// <summary>Why it was not published; only in its author's own view.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>True in its author's view while a report keeps it hidden from the others.</summary>
    public bool Reported { get; set; }

    public DateTime CreatedAt { get; set; }

    public static ReviewResponse From(Review review, bool forAuthor = false) => new()
    {
        Id = review.Id,
        ProductId = review.ProductId ?? 0,
        AuthorName = review.AuthorName,
        Rating = review.Rating,
        Title = review.Title,
        Body = review.Body,
        VerifiedPurchase = review.VerifiedPurchase,
        Status = review.Status,
        RejectionReason = forAuthor ? review.RejectionReason : null,
        Reported = forAuthor && review.Reported,
        CreatedAt = review.CreatedAt
    };
}

/// <summary>What the published reviews of a product add up to.</summary>
public class ReviewSummaryResponse
{
    public int ProductId { get; set; }

    /// <summary>Average of the published ratings, two decimals; 0 without reviews.</summary>
    public decimal AverageRating { get; set; }

    public int ReviewCount { get; set; }

    /// <summary>Reviews per number of stars, "1" to "5", each key present.</summary>
    public Dictionary<string, int> Distribution { get; set; } = new();
}

/// <summary>Whether the signed-in customer may review a product, and their review of it if they wrote one.</summary>
public class MyProductReviewResponse
{
    public int ProductId { get; set; }

    public bool CanReview { get; set; }

    /// <summary>Why not: notPurchased, alreadyReviewed, dailyLimit or signInAgain (a demo session from an old token); null when they can.</summary>
    public string? Reason { get; set; }

    /// <summary>Their review of the product in any state; a rejected one may be written again.</summary>
    public ReviewResponse? Review { get; set; }
}

/// <summary>Body of POST /api/v1/reviews. Rules: <c>CreateReviewRequestValidator</c> and <see cref="Services.ReviewContentRules"/>.</summary>
public class CreateReviewRequest
{
    public int ProductId { get; set; }

    /// <summary>1 to 5 stars.</summary>
    public int Rating { get; set; }

    /// <summary>Optional, up to 80 characters.</summary>
    public string? Title { get; set; }

    /// <summary>20 to 1000 characters.</summary>
    public string Body { get; set; } = string.Empty;
}

/// <summary>Body of POST /api/v1/reviews/{id}/report.</summary>
public class ReportReviewRequest
{
    /// <summary>What is wrong with it, optional, up to 300 characters.</summary>
    public string? Reason { get; set; }
}

/// <summary>Query of GET /api/v1/reviews/admin.</summary>
public class AdminReviewQuery
{
    /// <summary>
    /// queue (default: waiting for a decision - new or reported), pending, reported, published,
    /// rejected or all.
    /// </summary>
    public string? Status { get; set; }

    public int? ProductId { get; set; }

    public int? Page { get; set; }

    public int? PageSize { get; set; }
}

/// <summary>A review in the moderation queue.</summary>
public class AdminReviewResponse
{
    public Guid Id { get; set; }

    /// <summary>0 for a seeded review whose product has not been found in the catalogue yet.</summary>
    public int ProductId { get; set; }

    public string? ProductSlug { get; set; }

    public string? UserId { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Title { get; set; }

    public string Body { get; set; } = string.Empty;

    public ReviewStatus Status { get; set; }

    public bool Reported { get; set; }

    public int ReportCount { get; set; }

    /// <summary>The reasons given with the reports, newest first.</summary>
    public List<string> ReportReasons { get; set; } = new();

    public string? RejectionReason { get; set; }

    /// <summary>seed (came with the catalogue) or customer.</summary>
    public ReviewSource Source { get; set; }

    public bool VerifiedPurchase { get; set; }

    /// <summary>Written from a shared demo account; deleted at <see cref="ExpiresAt"/>.</summary>
    public bool Demo { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? ModeratedAt { get; set; }

    public string? ModeratedBy { get; set; }
}

/// <summary>Body of POST /api/v1/reviews/admin/moderate: one decision for one or many reviews.</summary>
public class ModerateReviewsRequest
{
    /// <summary>1 to 100 reviews.</summary>
    public List<Guid> Ids { get; set; } = new();

    /// <summary>approve (publish; a reported review is shown again) or reject.</summary>
    public ModerationDecision Decision { get; set; }

    /// <summary>Required to reject, up to 300 characters; the review's author sees it.</summary>
    public string? Reason { get; set; }
}

/// <summary>Stored nowhere; serialised by name.</summary>
public enum ModerationDecision
{
    Approve,
    Reject
}

/// <summary>What a moderation request changed.</summary>
public class ModerationResult
{
    /// <summary>Reviews the decision was applied to.</summary>
    public int Moderated { get; set; }

    /// <summary>Ids that were not found (deleted demo reviews, for instance).</summary>
    public List<Guid> NotFound { get; set; } = new();
}
