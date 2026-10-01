namespace Store.ReviewService.Models;

/// <summary>Each submission attempt, including rewrites, for the rolling daily quota.</summary>
public sealed class ReviewSubmission
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid? DemoSessionId { get; set; }
    public DateTime SubmittedAt { get; set; }
}
