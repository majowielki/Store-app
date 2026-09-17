using MassTransit;
using Store.AuditLogService.Models;
using Store.AuditLogService.Services;
using Store.Contracts.Audit.V1;

namespace Store.AuditLogService.Consumers;

/// <summary>
/// Stores a business action another service reported. Oversized identifiers are trimmed
/// rather than rejected: the trail must not lose an entry because a title was long.
/// </summary>
public sealed class AuditEventConsumer : IConsumer<AuditEvent>
{
    private readonly IAuditLogService _auditLogService;

    public AuditEventConsumer(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task Consume(ConsumeContext<AuditEvent> context)
    {
        var e = context.Message;
        // A database that refuses the row throws: the bus retries and, after that, parks the event in the error queue
        await _auditLogService.CreateAuditLogAsync(new AuditLog
        {
            Action = Trim(e.Action, AuditLogConstraints.ActionMaxLength)!,
            EntityName = Trim(e.EntityName, AuditLogConstraints.EntityNameMaxLength)!,
            EntityId = Trim(e.EntityId, AuditLogConstraints.EntityIdMaxLength),
            UserId = Trim(e.UserId, AuditLogConstraints.UserIdMaxLength),
            ServiceName = Trim(e.ServiceName, AuditLogConstraints.ServiceNameMaxLength),
            CorrelationId = context.MessageId?.ToString(),
            Timestamp = e.OccurredAt,
            Details = e.Details,
            OldValues = e.OldValues,
            NewValues = e.NewValues
        });
    }

    private static string? Trim(string? value, int maxLength)
        => value is null || value.Length <= maxLength ? value : value[..maxLength];
}
