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
[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = Policies.Admin)]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Get a specific audit log by ID
    /// </summary>
    /// <param name="id">Audit log ID</param>
    /// <returns>Audit log details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AuditLog?>>> GetAuditLog(long id)
    {
        var result = await _auditLogService.GetAuditLogAsync(id);
        if (!result.IsSuccess || result.Data == null)
            return StatusCode((int)result.StatusCode, result);

        return Ok(result);
    }

    /// <summary>
    /// Get audit logs with pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <returns>Paginated list of audit logs</returns>
    [HttpGet]
    public async Task<ActionResult<object>> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var logsResult = await _auditLogService.GetAuditLogsAsync(page, pageSize);
        var countResult = await _auditLogService.GetTotalCountAsync();
        if (!logsResult.IsSuccess || !countResult.IsSuccess)
            return StatusCode((int)(!logsResult.IsSuccess ? logsResult.StatusCode : countResult.StatusCode), !logsResult.IsSuccess ? logsResult : countResult);

        var totalCount = countResult.Data;
        var response = new
        {
            auditLogs = logsResult.Data,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            hasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize),
            hasPreviousPage = page > 1
        };

        return Ok(response);
    }

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    /// <param name="entityName">Entity name</param>
    /// <param name="entityId">Entity ID (optional)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <returns>Paginated list of audit logs for the entity</returns>
    [HttpGet("entity/{entityName}")]
    public async Task<ActionResult<object>> GetAuditLogsByEntity(string entityName, [FromQuery] string? entityId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var logsResult = await _auditLogService.GetAuditLogsByEntityAsync(entityName, entityId, page, pageSize);
        var countResult = await _auditLogService.GetTotalCountByEntityAsync(entityName, entityId);
        if (!logsResult.IsSuccess || !countResult.IsSuccess)
            return StatusCode((int)(!logsResult.IsSuccess ? logsResult.StatusCode : countResult.StatusCode), !logsResult.IsSuccess ? logsResult : countResult);

        var totalCount = countResult.Data;
        var response = new
        {
            auditLogs = logsResult.Data,
            totalCount,
            page,
            pageSize,
            entityName,
            entityId,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            hasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize),
            hasPreviousPage = page > 1
        };

        return Ok(response);
    }

    /// <summary>
    /// Get audit logs for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <returns>Paginated list of audit logs for the user</returns>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<object>> GetAuditLogsByUser(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var logsResult = await _auditLogService.GetAuditLogsByUserAsync(userId, page, pageSize);
        var countResult = await _auditLogService.GetTotalCountByUserAsync(userId);
        if (!logsResult.IsSuccess || !countResult.IsSuccess)
            return StatusCode((int)(!logsResult.IsSuccess ? logsResult.StatusCode : countResult.StatusCode), !logsResult.IsSuccess ? logsResult : countResult);

        var totalCount = countResult.Data;
        var response = new
        {
            auditLogs = logsResult.Data,
            totalCount,
            page,
            pageSize,
            userId,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            hasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize),
            hasPreviousPage = page > 1
        };

        return Ok(response);
    }

    /// <summary>
    /// Get audit logs within a date range
    /// </summary>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <returns>Paginated list of audit logs within the date range</returns>
    [HttpGet("daterange")]
    public async Task<ActionResult<object>> GetAuditLogsByDateRange([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (fromDate > toDate)
            return BadRequest("From date cannot be greater than to date");
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var logsResult = await _auditLogService.GetAuditLogsByDateRangeAsync(fromDate, toDate, page, pageSize);
        if (!logsResult.IsSuccess)
            return StatusCode((int)logsResult.StatusCode, logsResult);

        var auditLogsList = logsResult.Data?.ToList() ?? new List<AuditLog>();
        var totalCount = auditLogsList.Count;
        var response = new
        {
            auditLogs = auditLogsList,
            totalCount,
            page,
            pageSize,
            fromDate,
            toDate,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            hasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize),
            hasPreviousPage = page > 1
        };

        return Ok(response);
    }
}
