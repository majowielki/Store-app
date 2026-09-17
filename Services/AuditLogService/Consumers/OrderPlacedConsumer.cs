using MassTransit;
using Store.AuditLogService.Models;
using Store.AuditLogService.Services;
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

    public OrderPlacedConsumer(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var order = context.Message;
        // A database that refuses the row throws: the bus retries and, after that, parks the event in the error queue
        await _auditLogService.CreateAuditLogAsync(new AuditLog
        {
            Action = "ORDER_PLACED",
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
    }
}
