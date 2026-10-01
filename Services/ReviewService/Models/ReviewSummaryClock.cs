namespace Store.ReviewService.Models;

/// <summary>A durable, strictly increasing timestamp for each product's rating projection.</summary>
public sealed class ReviewSummaryClock
{
    public int ProductId { get; set; }
    public DateTime ChangedAt { get; set; }
}
