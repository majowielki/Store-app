namespace Store.ReviewService.Models;

/// <summary>
/// A review of a product (ADR 012). A customer's review waits for the true administrator before
/// anyone but its author sees it; the reviews seeded with the catalogue are published from the
/// start. A review written from a shared demo account belongs to the sign-in session it was
/// written in and is deleted a day later.
/// </summary>
public class Review
{
    public Guid Id { get; set; }

    /// <summary>The product in the catalogue; a seeded review has it once its slug has been looked up.</summary>
    public int? ProductId { get; set; }

    /// <summary>The product's slug, for seeded reviews: the catalogue's ids differ between databases, its slugs do not.</summary>
    public string? ProductSlug { get; set; }

    /// <summary>The customer who wrote it; none for a seeded review.</summary>
    public string? UserId { get; set; }

    /// <summary>What the shop prints under it: a first name and an initial ("Anna N.").</summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>1 to 5 stars.</summary>
    public int Rating { get; set; }

    public string? Title { get; set; }

    public string Body { get; set; } = string.Empty;

    public ReviewStatus Status { get; set; }

    /// <summary>Hidden again after a visitor reported it, until the administrator looks at it.</summary>
    public bool Reported { get; set; }

    public int ReportCount { get; set; }

    /// <summary>Why the administrator did not publish it; its author sees it.</summary>
    public string? RejectionReason { get; set; }

    public ReviewSource Source { get; set; }

    /// <summary>Written by a customer who paid for the product (every customer review is; seeded ones say so too).</summary>
    public bool VerifiedPurchase { get; set; }

    /// <summary>The sign-in session of a shared demo account the review was written in; none for a real account.</summary>
    public Guid? DemoSessionId { get; set; }

    /// <summary>When a demo account's review is deleted.</summary>
    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>When its author last sent it (written, or rewritten after a rejection); the daily limit counts these.</summary>
    public DateTime SubmittedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ModeratedAt { get; set; }

    /// <summary>The administrator who approved or rejected it.</summary>
    public string? ModeratedBy { get; set; }
}

/// <summary>Stored by name.</summary>
public enum ReviewStatus
{
    /// <summary>Waiting for the true administrator; only its author sees it.</summary>
    Pending,

    Published,

    Rejected
}

/// <summary>Stored by name; tells seeded reviews from customers' ones in the admin panel.</summary>
public enum ReviewSource
{
    Seed,
    Customer
}

/// <summary>A product a customer has paid for, learnt from the order events: what makes a review a verified purchase.</summary>
public class Purchase
{
    public string UserId { get; set; } = string.Empty;

    public int ProductId { get; set; }

    /// <summary>The first paid order the product was in.</summary>
    public int OrderId { get; set; }

    public DateTime PaidAt { get; set; }
}

/// <summary>A visitor's report of a review; one per visitor and review.</summary>
public class ReviewReport
{
    public Guid Id { get; set; }

    public Guid ReviewId { get; set; }

    public string ReporterId { get; set; } = string.Empty;

    public string? Reason { get; set; }

    /// <summary>
    /// The sign-in session of a shared demo account that reported it. Such a report hides the review
    /// from that session only - a visitor of the demo account must not hide reviews from everyone.
    /// </summary>
    public Guid? DemoSessionId { get; set; }

    /// <summary>When a demo account's report is deleted.</summary>
    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>The version of the demo reviews a database was seeded with - a single row.</summary>
public class ReviewSeed
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public int Version { get; set; }

    public DateTime AppliedAt { get; set; }
}

/// <summary>Limits the schema and the validators share.</summary>
public static class ReviewConstraints
{
    public const int BodyMinLength = 20;
    public const int BodyMaxLength = 1000;
    public const int TitleMaxLength = 80;
    public const int AuthorNameMaxLength = 60;
    public const int ReasonMaxLength = 300;
    public const int SlugMaxLength = 220;

    /// <summary>Reviews one account (one session of a demo account) may write in a day.</summary>
    public const int DailyLimit = 3;

    /// <summary>How long a demo account's review lives.</summary>
    public static readonly TimeSpan DemoLifetime = TimeSpan.FromHours(24);
}
