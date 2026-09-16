using MassTransit;
using Store.AuditLogService.Services;
using Store.Contracts.Orders.V1;
using Store.Shared.Models;
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
        var result = await _auditLogService.CreateAuditLogAsync(new AuditLog
        {
            Action = "ORDER_PLACED",
            EntityName = "Order",
            EntityId = order.OrderId.ToString(),
            UserId = order.UserId,
            ServiceName = "OrderService",
            CorrelationId = context.MessageId?.ToString(),
            Timestamp = order.PlacedAt,
            AdditionalInfo = JsonSerializer.Serialize(new
            {
                order.Subtotal,
                order.DiscountAmount,
                order.DeliveryFee,
                order.Total,
                Lines = order.Lines.Select(l => new { l.ProductId, l.Quantity, l.UnitPrice })
            }, JsonOptions)
        });

        if (!result.IsSuccess)
        {
            // Let the bus retry and, after that, park the event in the error queue
            throw new InvalidOperationException($"Audit entry for order {order.OrderId} could not be stored: {result.Message}");
        }
    }
}
