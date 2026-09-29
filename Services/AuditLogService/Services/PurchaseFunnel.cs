using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.AuditLogService.Data;
using Store.AuditLogService.Models;
using Store.Contracts.Audit;

namespace Store.AuditLogService.Services;

/// <summary>
/// The purchase funnel (ADR 020): the product views and the additions to the bag the shop's pages
/// send, and the orders placed, which the order service's events brought to the audit trail. It
/// counts from the same midnight as the dashboard's order statistics, so its last stage is the
/// number of orders the dashboard shows.
/// </summary>
public sealed class PurchaseFunnel
{
    public const int DefaultDays = 30;

    private readonly AuditLogDbContext _context;
    private readonly AuditRetentionOptions _retention;
    private readonly TimeProvider _time;

    public PurchaseFunnel(AuditLogDbContext context, IOptions<AuditRetentionOptions> retention, TimeProvider time)
    {
        _context = context;
        _retention = retention.Value;
        _time = time;
    }

    /// <summary>Counts one step a visitor took; nothing about the visitor is kept.</summary>
    public async Task RecordAsync(ShopEventKind kind, int productId, CancellationToken cancellationToken = default)
    {
        _context.ShopEvents.Add(new ShopEvent { Kind = kind, ProductId = productId, OccurredAt = _time.GetUtcNow().UtcDateTime });
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// How often each stage was reached over the last <paramref name="days"/> days, counted from
    /// midnight UTC; never further back than the entries are kept.
    /// </summary>
    public async Task<FunnelResponse> CountAsync(int days, CancellationToken cancellationToken = default)
    {
        var window = Math.Clamp(days, 1, _retention.RetentionDays);
        var since = _time.GetUtcNow().UtcDateTime.Date.AddDays(-window);

        var events = await _context.ShopEvents.AsNoTracking()
            .Where(e => e.OccurredAt >= since)
            .GroupBy(e => e.Kind)
            .Select(g => new { Kind = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Kind, g => g.Count, cancellationToken);
        var orders = await _context.AuditLogs.AsNoTracking()
            .CountAsync(a => a.Action == AuditActions.OrderPlaced && a.Timestamp >= since, cancellationToken);

        return new FunnelResponse
        {
            Since = since,
            Days = window,
            Stages =
            [
                new FunnelStageCount { Stage = FunnelStage.ProductViewed, Count = events.GetValueOrDefault(ShopEventKind.ProductViewed) },
                new FunnelStageCount { Stage = FunnelStage.AddedToBag, Count = events.GetValueOrDefault(ShopEventKind.AddedToBag) },
                new FunnelStageCount { Stage = FunnelStage.OrderPlaced, Count = orders }
            ]
        };
    }
}
