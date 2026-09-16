namespace Store.AuditLogService.Models;

/// <summary>
/// One business action in the audit trail, built from an event another service published.
/// Deliberately narrow: identifiers, the acting user's id, the service and the business
/// fields that changed. Nothing about the HTTP request and no personal data.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? UserId { get; set; }

    public string? ServiceName { get; set; }

    /// <summary>Id of the message that carried the event, for tracing back to the publisher.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>When the action happened in the publishing service (UTC).</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Business context as JSON (ids, amounts, counts).</summary>
    public string? Details { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }
}

/// <summary>Column lengths, applied by the model and by the consumer that trims oversized values.</summary>
public static class AuditLogConstraints
{
    public const int ActionMaxLength = 50;
    public const int EntityNameMaxLength = 100;
    public const int EntityIdMaxLength = 100;
    public const int UserIdMaxLength = 450;
    public const int ServiceNameMaxLength = 100;
    public const int CorrelationIdMaxLength = 100;
}
