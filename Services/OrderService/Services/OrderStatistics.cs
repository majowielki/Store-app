using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.OrderService.Data;
using Store.OrderService.DTOs.Responses;

namespace Store.OrderService.Services;

/// <summary>Placed-order counts and values, including unpaid/cancelled orders. Line values are before discounts.</summary>
public sealed class OrderStatistics(OrderDbContext context, TimeProvider time)
{
    private readonly OrderDbContext _context = context;
    private readonly TimeProvider _time = time;
    public async Task<OrderStatsResponse> GetOrderStatsAsync(int daysWindow = 30, CancellationToken cancellationToken = default)
    {
        var since = StatisticsWindow.Since(_time, daysWindow);
        var window = _context.Orders.AsNoTracking().Where(o => o.CreatedAt >= since);

        // Aggregates run in SQL; only one row per day and per product comes back
        var daily = await window
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new TimeBucketStats { BucketStart = g.Key, Orders = g.Count(), Revenue = g.Sum(o => o.Total) })
            .OrderBy(b => b.BucketStart)
            .ToListAsync(cancellationToken);

        var weekly = daily
            .GroupBy(d => WeekStart(d.BucketStart))
            .OrderBy(g => g.Key)
            .Select(g => new TimeBucketStats { BucketStart = g.Key, Orders = g.Sum(d => d.Orders), Revenue = g.Sum(d => d.Revenue) })
            .ToList();

        var topProducts = await _context.OrderLines.AsNoTracking()
            .Where(l => l.Order.CreatedAt >= since)
            .GroupBy(l => new { l.ProductId, l.ProductTitle })
            .Select(g => new TopProductStats
            {
                ProductId = g.Key.ProductId,
                ProductTitle = g.Key.ProductTitle,
                Quantity = g.Sum(l => l.Quantity),
                Revenue = g.Sum(l => l.UnitPrice * l.Quantity)
            })
            .OrderByDescending(p => p.Quantity)
            .ThenByDescending(p => p.Revenue)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new OrderStatsResponse
        {
            TotalOrders = daily.Sum(d => d.Orders),
            TotalRevenue = daily.Sum(d => d.Revenue),
            Daily = daily,
            Weekly = weekly,
            TopProducts = topProducts
        };
    }

    /// <summary>Monday of the ISO week the date falls in.</summary>
    private static DateTime WeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

}
