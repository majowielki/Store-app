using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authorization;
using Store.Contracts.Authorization;
using Store.ReviewService.DTOs;
using Store.ReviewService.Services;

namespace Store.ReviewService.Controllers;

/// <summary>
/// Product reviews (ADR 012). Anyone reads the published ones and the ratings; a signed-in
/// customer writes a review of a product from a paid order, sees their own in any state and
/// reports others'. New reviews wait for the true administrator (<see cref="AdminReviewsController"/>).
/// </summary>
[ApiController]
[Route("api/v1/reviews")]
public class ReviewsController : ControllerBase
{
    private const int MaxSummaries = 100;

    private readonly ReviewBoard _board;
    private readonly ReviewSummaries _summaries;

    public ReviewsController(ReviewBoard board, ReviewSummaries summaries)
    {
        _board = board;
        _summaries = summaries;
    }

    private ReviewViewer Viewer => ReviewViewer.From(User);

    /// <summary>The published reviews of a product, filtered by stars or verified purchase, newest first unless sorted otherwise.</summary>
    [HttpGet]
    public Task<PagedResponse<ReviewResponse>> List([FromQuery] ReviewQuery query, CancellationToken cancellationToken)
        => _board.ListAsync(query, Viewer, cancellationToken);

    /// <summary>The rating of a product: average, count and reviews per number of stars.</summary>
    [HttpGet("products/{productId:int}/summary")]
    public async Task<ReviewSummaryResponse> Summary(int productId, CancellationToken cancellationToken)
        => (await _summaries.ForAsync([productId], cancellationToken))[0];

    /// <summary>The ratings of many products at once (comma-separated ids, at most 100; others are ignored).</summary>
    [HttpGet("summaries")]
    public Task<IReadOnlyList<ReviewSummaryResponse>> Summaries([FromQuery] string? ids, CancellationToken cancellationToken)
    {
        var productIds = (ids ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => int.TryParse(id, out var value) ? value : 0)
            .Where(id => id > 0)
            .Distinct()
            .Take(MaxSummaries)
            .ToArray();
        return _summaries.ForAsync(productIds, cancellationToken);
    }

    /// <summary>Whether the signed-in customer may review the product, and their review of it in any state.</summary>
    [HttpGet("products/{productId:int}/mine")]
    [Authorize(Policy = Policies.User)]
    public Task<MyProductReviewResponse> MineForProduct(int productId, CancellationToken cancellationToken)
        => _board.MineForProductAsync(productId, Viewer, cancellationToken);

    /// <summary>The signed-in customer's reviews in any state (on a demo account: the ones of this sign-in session).</summary>
    [HttpGet("mine")]
    [Authorize(Policy = Policies.User)]
    public Task<IReadOnlyList<ReviewResponse>> Mine(CancellationToken cancellationToken)
        => _board.MineAsync(Viewer, cancellationToken);

    /// <summary>
    /// Writes a review; it waits for moderation. 403 for a product not in a paid order, 409 for a
    /// second review of it (a rejected one may be written again), 422 over three reviews a day or
    /// for text the automatic checks refuse.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.User)]
    [ProducesResponseType<ReviewResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ReviewResponse>> Create([FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var review = await _board.SubmitAsync(request, Viewer, cancellationToken);
        return CreatedAtAction(nameof(MineForProduct), new { productId = review.ProductId }, review);
    }

    /// <summary>Reports a published review; from a customer's account it is hidden until the administrator looks at it.</summary>
    [HttpPost("{id:guid}/report")]
    [Authorize(Policy = Policies.User)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Report(Guid id, [FromBody] ReportReviewRequest request, CancellationToken cancellationToken)
    {
        await _board.ReportAsync(id, request, Viewer, cancellationToken);
        return NoContent();
    }
}

/// <summary>
/// The moderation queue. Both administrators see it (the demo one without the texts nobody has
/// approved and without accounts); only the true administrator decides.
/// </summary>
[ApiController]
[Route("api/v1/reviews/admin")]
[Authorize(Policy = Policies.Admin)]
public class AdminReviewsController : ControllerBase
{
    private readonly ReviewModeration _moderation;

    public AdminReviewsController(ReviewModeration moderation)
    {
        _moderation = moderation;
    }

    /// <summary>Reviews by state: queue (default: new and reported, oldest first), pending, reported, published, rejected or all.</summary>
    [HttpGet]
    public Task<PagedResponse<AdminReviewResponse>> List([FromQuery] AdminReviewQuery query, CancellationToken cancellationToken)
        => _moderation.ListAsync(query, ReviewViewer.From(User), cancellationToken);

    /// <summary>Approves or rejects (with a reason the author sees) one review or many at once.</summary>
    [HttpPost("moderate")]
    [Authorize(Policy = Policies.AdminWrite)]
    public Task<ModerationResult> Moderate([FromBody] ModerateReviewsRequest request, CancellationToken cancellationToken)
        => _moderation.ModerateAsync(request, User.GetRequiredUserId(), cancellationToken);
}
