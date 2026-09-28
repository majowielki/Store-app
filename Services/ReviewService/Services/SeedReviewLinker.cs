using Microsoft.EntityFrameworkCore;
using Store.ReviewService.Clients;
using Store.ReviewService.Data;

namespace Store.ReviewService.Services;

/// <summary>
/// Gives the seeded reviews their product ids: <see cref="ReviewSeeder"/> names products by slug,
/// because the catalogue's ids differ between databases, and runs before the catalogue may be up.
/// Asks the catalogue every half a minute while reviews wait, then every ten minutes in case a
/// product comes back; each product that gets its reviews has its rating published.
/// </summary>
public sealed class SeedReviewLinker : BackgroundService
{
    private static readonly TimeSpan WhileWaiting = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan WhenDone = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _time;
    private readonly ILogger<SeedReviewLinker> _logger;

    public SeedReviewLinker(IServiceScopeFactory scopes, TimeProvider time, ILogger<SeedReviewLinker> logger)
    {
        _scopes = scopes;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = WhileWaiting;
            try
            {
                if (await LinkAsync(stoppingToken) == 0)
                {
                    delay = WhenDone;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "The catalogue could not be asked for the seeded reviews' products; retrying in {Delay}", delay);
            }

            await Task.Delay(delay, _time, stoppingToken);
        }
    }

    /// <summary>One round: links the reviews whose products the catalogue knows; returns how many still wait.</summary>
    public async Task<int> LinkAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopes.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();

        var slugs = await context.Reviews.AsNoTracking()
            .Where(r => r.ProductId == null && r.ProductSlug != null)
            .Select(r => r.ProductSlug!)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (slugs.Count == 0)
        {
            return 0;
        }

        var ids = await scope.ServiceProvider.GetRequiredService<ICatalogClient>().FindIdsAsync(slugs, cancellationToken);
        if (ids.Count > 0)
        {
            var found = ids.Keys.ToArray();
            var reviews = await context.Reviews
                .Where(r => r.ProductId == null && r.ProductSlug != null && found.Contains(r.ProductSlug))
                .ToListAsync(cancellationToken);
            foreach (var review in reviews)
            {
                review.ProductId = ids[review.ProductSlug!];
            }

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await scope.ServiceProvider.GetRequiredService<ReviewSummaries>().PublishAsync(ids.Values, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Linked {Reviews} seeded reviews to {Products} products", reviews.Count, ids.Count);
        }

        return slugs.Count - ids.Count;
    }
}
