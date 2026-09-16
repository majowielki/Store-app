using Store.Shared.Models;
using Store.Shared.Services;
using System.Collections.Concurrent;

namespace Store.Tests.Integration.TestSupport;

/// <summary>
/// Records every audit entry a service tries to send instead of calling AuditLogService.
/// Only the network boundary is faked - the service's own database is real.
/// </summary>
public sealed class RecordingAuditLogClient : IAuditLogClient
{
    public ConcurrentQueue<AuditLog> Entries { get; } = new();

    public Task<long?> CreateAuditLogAsync(AuditLog auditLog)
    {
        Entries.Enqueue(auditLog);
        return Task.FromResult<long?>(Entries.Count);
    }

    public Task CreateLocalAuditLogAsync(AuditLog auditLog)
    {
        Entries.Enqueue(auditLog);
        return Task.CompletedTask;
    }
}
