using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.OrderService.Data;
using Store.OrderService.Models;
using Store.OrderService.Webhooks;

namespace Store.OrderService.Services;

/// <summary>
/// Forgets the records that tell a repeated request from a new one once they can no longer be
/// needed, every hour: idempotency keys of checkouts after <see cref="IdempotencyKey.Lifetime"/>
/// (a client retrying a day-old checkout gets a fresh order, which is intended) and processed
/// payment webhooks after <see cref="PaymentWebhookOptions.ProcessedEventRetention"/> (a webhook
/// that old fails its signature's time check before its id is looked up).
/// </summary>
public sealed class DeduplicationCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _time;
    private readonly PaymentWebhookOptions _webhooks;
    private readonly ILogger<DeduplicationCleanupService> _logger;

    public DeduplicationCleanupService(
        IServiceScopeFactory scopeFactory,
        TimeProvider time,
        IOptions<PaymentWebhookOptions> webhooks,
        ILogger<DeduplicationCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _time = time;
        _webhooks = webhooks.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, _time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PurgeAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Deduplication cleanup failed; retrying next hour");
            }
        }
    }

    /// <summary>One round; returns how many records were forgotten.</summary>
    public async Task<int> PurgeAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var now = _time.GetUtcNow().UtcDateTime;

        var keysBefore = now - IdempotencyKey.Lifetime;
        var keys = await context.IdempotencyKeys.Where(k => k.CreatedAt < keysBefore).ExecuteDeleteAsync(cancellationToken);

        var webhooksBefore = now - _webhooks.ProcessedEventRetention;
        var webhooks = await context.ProcessedWebhookEvents.Where(e => e.ReceivedAt < webhooksBefore).ExecuteDeleteAsync(cancellationToken);

        if (keys + webhooks > 0)
        {
            _logger.LogInformation("Forgot {Keys} expired idempotency keys and {Webhooks} processed payment webhooks", keys, webhooks);
        }

        return keys + webhooks;
    }
}
