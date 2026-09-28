using Microsoft.EntityFrameworkCore;
using Store.OrderService.Data;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Models;

namespace Store.OrderService.Services;

/// <summary>
/// The numbers the shop may print about itself: how many orders customers paid for and kept.
/// Nothing about a customer or an amount leaves this way; the admin panel has the full statistics.
/// </summary>
public sealed class ShopOrderStats
{
    private readonly OrderDbContext _context;

    public ShopOrderStats(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<ShopOrderStatsResponse> GetAsync(CancellationToken cancellationToken = default) => new()
    {
        PaidOrders = await _context.Orders.CountAsync(o => OrderStatusFlow.PaidFor.Contains(o.Status), cancellationToken)
    };
}
