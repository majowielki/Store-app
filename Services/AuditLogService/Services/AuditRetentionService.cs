using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.AuditLogService.Data;
using System.ComponentModel.DataAnnotations;

namespace Store.AuditLogService.Services;

/// <summary>How long audit entries are kept, bound from the <c>AuditRetention</c> section.</summary>
public sealed class AuditRetentionOptions
{
    public const string SectionName = "AuditRetention";

    [Range(1, 3650)]
    public int RetentionDays { get; init; } = 90;

    /// <summary>Rows deleted per statement, so the job never holds a long lock on a busy table.</summary>
    [Range(100, 100_000)]
    public int BatchSize { get; init; } = 5000;
}

/// <summary>
/// Deletes entries older than the retention period, in batches, once a day (and once at
/// start-up). An audit trail that grows without limit ends up as the largest table in the
/// system and the hardest one to justify keeping.
/// </summary>
public sealed class AuditRetentionService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AuditRetentionOptions _options;
    private readonly ILogger<AuditRetentionService> _logger;

    public AuditRetentionService(IServiceScopeFactory scopeFactory, IOptions<AuditRetentionOptions> options, ILogger<AuditRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the host a moment to finish starting (migrations run before this)
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await PurgeAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Audit retention run failed; retrying in {Interval}", Interval);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>Deletes expired entries batch by batch; returns how many rows went.</summary>
    public async Task<int> PurgeAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuditLogDbContext>();
        var cutoff = DateTime.UtcNow.AddDays(-_options.RetentionDays);

        var total = 0;
        int deleted;
        do
        {
            // DELETE ... WHERE Id IN (SELECT Id ... ORDER BY Id LIMIT batch)
            var batch = context.AuditLogs
                .Where(x => x.Timestamp < cutoff)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(_options.BatchSize);
            deleted = await context.AuditLogs
                .Where(a => batch.Contains(a.Id))
                .ExecuteDeleteAsync(cancellationToken);
            total += deleted;
        }
        while (deleted > 0 && !cancellationToken.IsCancellationRequested);

        if (total > 0)
        {
            _logger.LogInformation("Audit retention: deleted {Count} entries older than {Cutoff:u}", total, cutoff);
        }

        return total;
    }
}
