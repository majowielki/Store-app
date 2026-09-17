using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Store.BuildingBlocks.Observability;

namespace Store.BuildingBlocks.Messaging;

/// <summary>
/// Keeps the <c>store.outbox.pending</c> gauge fed: every half minute it counts the outbox rows
/// the delivery service has not sent yet. The count runs here, on a timer, so reading the
/// metric never touches the database.
/// </summary>
internal sealed class OutboxMonitor<TDbContext> : BackgroundService where TDbContext : DbContext
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<OutboxMonitor<TDbContext>> _logger;
    private long _pending;

    public OutboxMonitor(IServiceScopeFactory scopes, StoreMetrics metrics, ILogger<OutboxMonitor<TDbContext>> logger)
    {
        _scopes = scopes;
        _logger = logger;
        metrics.ObserveOutboxPending(() => Interlocked.Read(ref _pending));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<TDbContext>();
                // Bus outbox rows (no inbox, no consumer outbox) are deleted as soon as they are delivered,
                // so every row still there is waiting for the broker
                var pending = await context.Set<OutboxMessage>()
                    .LongCountAsync(m => m.OutboxId == null && m.InboxMessageId == null, stoppingToken);
                Interlocked.Exchange(ref _pending, pending);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // The gauge keeps its last value; the database being down shows up in the readiness check
                _logger.LogDebug(ex, "Outbox count skipped");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
