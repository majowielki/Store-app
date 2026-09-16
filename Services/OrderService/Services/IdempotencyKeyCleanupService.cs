using Microsoft.EntityFrameworkCore;
using Store.OrderService.Data;
using Store.OrderService.Models;

namespace Store.OrderService.Services;

/// <summary>
/// Deletes idempotency keys older than <see cref="IdempotencyKey.Lifetime"/> once an hour.
/// A client retrying a day-old checkout gets a fresh order, which is the intended behaviour.
/// </summary>
public sealed class IdempotencyKeyCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IdempotencyKeyCleanupService> _logger;

    public IdempotencyKeyCleanupService(IServiceScopeFactory scopeFactory, ILogger<IdempotencyKeyCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                var cutoff = DateTime.UtcNow - IdempotencyKey.Lifetime;
                var deleted = await context.IdempotencyKeys
                    .Where(k => k.CreatedAt < cutoff)
                    .ExecuteDeleteAsync(stoppingToken);
                if (deleted > 0)
                {
                    _logger.LogInformation("Deleted {Count} expired idempotency keys", deleted);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Idempotency key cleanup failed; retrying next hour");
            }
        }
    }
}
