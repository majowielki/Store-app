using MassTransit;
using Store.AuditLogService.Models;
using Store.AuditLogService.Services;
using Store.Contracts.Audit;
using Store.Contracts.Orders.V1;
using System.Text.Json;

namespace Store.AuditLogService.Consumers;

/// <summary>
/// Records a change of an order's status: the order, the administrator who made it and the
/// statuses before and after. The inbox keeps a redelivered event from producing a second row.
/// </summary>
public sealed class OrderStatusChangedConsumer : IConsumer<OrderStatusChanged>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IAuditLogService _auditLogService;

    public OrderStatusChangedConsumer(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task Consume(ConsumeContext<OrderStatusChanged> context)
    {
        var change = context.Message;
        await _auditLogService.CreateAuditLogAsync(new AuditLog
        {
            Action = AuditActions.OrderStatusChanged,
            EntityName = "Order",
            EntityId = change.OrderId.ToString(),
            UserId = change.ChangedBy,
            ServiceName = "order",
            CorrelationId = context.MessageId?.ToString(),
            Timestamp = change.ChangedAt,
            OldValues = JsonSerializer.Serialize(new { status = change.PreviousStatus }, JsonOptions),
            NewValues = JsonSerializer.Serialize(new { status = change.Status }, JsonOptions),
            Details = JsonSerializer.Serialize(new { customerId = change.UserId }, JsonOptions)
        });
    }
}
