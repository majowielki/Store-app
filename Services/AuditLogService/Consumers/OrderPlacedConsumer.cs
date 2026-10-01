using MassTransit;
using Microsoft.EntityFrameworkCore;
using Store.AuditLogService.Data;
using Store.AuditLogService.Models;
using Store.AuditLogService.Services;
using Store.BuildingBlocks.Persistence;
using Store.Contracts.Audit;
using Store.Contracts.Orders.V1;
using System.Text.Json;

namespace Store.AuditLogService.Consumers;

/// <summary>
/// Records a placed order in the audit trail: identifiers and amounts, not the customer's
/// address or e-mail. The inbox keeps a redelivered event from producing a second row.
/// </summary>
public sealed class OrderPlacedConsumer : IConsumer<OrderPlaced>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IAuditLogService _auditLogService;
    private readonly AuditLogDbContext _db;

    public OrderPlacedConsumer(IAuditLogService auditLogService, AuditLogDbContext db)
    {
        _auditLogService = auditLogService;
        _db = db;
    }

    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var order = context.Message;
        await using var transaction = await _db.BeginStoreTransactionAsync(context.CancellationToken);
        await _db.LockKeyAsync($"order-receipt:{order.OrderId}", context.CancellationToken);
        if (await _db.OrderReceipts.AnyAsync(o => o.OrderId == order.OrderId, context.CancellationToken)) return;
        _db.OrderReceipts.Add(new OrderReceipt { OrderId = order.OrderId });
        // A database that refuses the row throws: the bus retries and, after that, parks the event in the error queue
        await _auditLogService.CreateAuditLogAsync(new AuditLog
        {
            Action = AuditActions.OrderPlaced,
            EntityName = "Order",
            EntityId = order.OrderId.ToString(),
            UserId = order.UserId,
            ServiceName = "order",
            CorrelationId = context.MessageId?.ToString(),
            Timestamp = order.PlacedAt,
            Details = JsonSerializer.Serialize(new
            {
                order.Subtotal,
                order.DiscountAmount,
                order.DeliveryFee,
                order.Total,
                Lines = order.Lines.Select(l => new { l.ProductId, l.Quantity, l.UnitPrice })
            }, JsonOptions)
        });
        if (transaction is not null) await transaction.CommitAsync(context.CancellationToken);
    }
}
