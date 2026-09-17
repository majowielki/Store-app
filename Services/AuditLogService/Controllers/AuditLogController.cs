using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.AuditLogService.Models;
using Store.AuditLogService.Services;
using Store.BuildingBlocks.Api;
using Store.Contracts.Authorization;

namespace Store.AuditLogService.Controllers;

/// <summary>
/// Read API of the audit trail for administrators. Entries are written by the event
/// consumers only; there is no HTTP endpoint that accepts them.
/// </summary>
[ApiController]
[Route("api/v1/auditlog")]
[Authorize(Policy = Policies.Admin)]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Entries newest first, narrowed by any of entityName, entityId, userId, from and to
    /// (ISO 8601 instants). Pages hold 50 entries unless pageSize says otherwise, 100 at most.
    /// </summary>
    [HttpGet]
    public Task<PagedResponse<AuditLog>> GetAuditLogs([FromQuery] AuditLogQuery query, [FromQuery] PagedQuery paging)
        => _auditLogService.GetAuditLogsAsync(query, paging);

    /// <summary>One entry; 404 for an unknown id.</summary>
    [HttpGet("{id:long}")]
    public Task<AuditLog> GetAuditLog(long id)
        => _auditLogService.GetAuditLogAsync(id);
}
