using MassTransit;
using Microsoft.EntityFrameworkCore;
using Store.Contracts.Orders.V1;
using Store.ReviewService.Data;
using Store.ReviewService.Models;

namespace Store.ReviewService.Consumers;

/// <summary>A paid order lets its customer review the products in it: each becomes a purchase.</summary>
public sealed class OrderPaidConsumer : IConsumer<OrderPaid>
{
    private readonly ReviewDbContext _context;
    private readonly ILogger<OrderPaidConsumer> _logger;

    public OrderPaidConsumer(ReviewDbContext context, ILogger<OrderPaidConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPaid> context)
    {
        var order = context.Message;
        var productIds = order.Lines.Select(line => line.ProductId).Distinct().ToArray();
        var known = await _context.Purchases
            .Where(p => p.UserId == order.UserId && productIds.Contains(p.ProductId))
            .Select(p => p.ProductId)
            .ToListAsync(context.CancellationToken);

        // A product bought before keeps its first order; a race with another order of the same
        // customer fails the save on the key and the retry finds the purchase
        foreach (var productId in productIds.Except(known))
        {
            _context.Purchases.Add(new Purchase { UserId = order.UserId, ProductId = productId, OrderId = order.OrderId, PaidAt = order.PaidAt });
        }

        await _context.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Order {OrderId} paid: {Count} products may be reviewed", order.OrderId, productIds.Length - known.Count);
    }
}
