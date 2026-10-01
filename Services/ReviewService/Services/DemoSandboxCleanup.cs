using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Persistence;
using Store.ReviewService.Data;
using Store.ReviewService.Models;

namespace Store.ReviewService.Services;

/// <summary>
/// Deletes what visitors of the shared demo accounts wrote once its day is over (ADR 012):
/// their reviews and their reports. A deleted review that was published leaves its product's
/// rating, which is published again.
/// </summary>
public sealed class DemoSandboxCleanup : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _time;
    private readonly ILogger<DemoSandboxCleanup> _logger;

    public DemoSandboxCleanup(IServiceScopeFactory scopes, TimeProvider time, ILogger<DemoSandboxCleanup> logger)
    {
        _scopes = scopes;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, _time);
        do
        {
            try
            {
                await CleanAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Demo review cleanup failed; retrying on the next round");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>One round; returns the number of reviews deleted.</summary>
    public async Task<int> CleanAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopes.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
        var now = _time.GetUtcNow().UtcDateTime;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var expiredIds = await context.Reviews.Where(r => r.ExpiresAt != null && r.ExpiresAt <= now).Select(r => r.Id).ToListAsync(cancellationToken);
        await context.LockAllForUpdateAsync<Review, Guid>(r => r.Id, expiredIds, cancellationToken);
        var expired = await context.Reviews.AsNoTracking()
            .Where(r => r.ExpiresAt != null && r.ExpiresAt <= now)
            .Select(r => new { r.Id, r.ProductId, Public = r.Status == ReviewStatus.Published && !r.Reported })
            .ToListAsync(cancellationToken);

        var ids = expired.Select(r => r.Id).ToArray();
        // Their reports go with them (cascade)
        var reviews = ids.Length == 0 ? 0 : await context.Reviews.Where(r => ids.Contains(r.Id)).ExecuteDeleteAsync(cancellationToken);
        var reports = await context.ReviewReports.Where(r => r.ExpiresAt != null && r.ExpiresAt <= now).ExecuteDeleteAsync(cancellationToken);
        await context.ReviewSubmissions.Where(s => s.SubmittedAt < now - ReviewConstraints.DailyLimitWindow).ExecuteDeleteAsync(cancellationToken);

        var ratings = expired.Where(r => r.Public && r.ProductId != null).Select(r => r.ProductId!.Value).ToList();
        await scope.ServiceProvider.GetRequiredService<ReviewSummaries>().PublishAsync(ratings, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (reviews + reports > 0)
        {
            _logger.LogInformation("Deleted {Reviews} demo reviews and {Reports} demo reports after their day", reviews, reports);
        }

        return reviews;
    }
}
