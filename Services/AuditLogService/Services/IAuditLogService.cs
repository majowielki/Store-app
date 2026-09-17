using Store.AuditLogService.Models;
using Store.BuildingBlocks.Api;

namespace Store.AuditLogService.Services;

/// <summary>Filters of the audit trail listing; every field is optional and they combine with AND.</summary>
public sealed class AuditLogQuery
{
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? UserId { get; set; }

    /// <summary>Entries at or after this instant. Offsets are honoured: "2026-09-01T00:00:00+02:00" is 22:00 UTC the day before.</summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>Entries at or before this instant.</summary>
    public DateTimeOffset? To { get; set; }
}

public interface IAuditLogService
{
    /// <summary>Stores an entry and returns its id; throws when the database refuses it, so the event is retried.</summary>
    Task<long> CreateAuditLogAsync(AuditLog auditLog);

    /// <summary>One entry; <c>NotFoundException</c> for an unknown id.</summary>
    Task<AuditLog> GetAuditLogAsync(long id);

    /// <summary>Entries matching the filters, newest first.</summary>
    Task<PagedResponse<AuditLog>> GetAuditLogsAsync(AuditLogQuery query, PagedQuery paging);
}
