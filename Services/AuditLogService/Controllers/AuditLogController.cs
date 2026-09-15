using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.AuditLogService.Services;
using Store.Shared.Authentication;
using Store.Shared.Authorization;
using Store.Shared.Models;
using System.Security.Claims;

namespace Store.AuditLogService.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogController> _logger;

    public AuditLogController(IAuditLogService auditLogService, ILogger<AuditLogController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new audit log entry
    /// </summary>
    /// <param name="auditLog">Audit log data</param>
    /// <returns>Created audit log ID</returns>
    [HttpPost]
    [Authorize(Policy = Policies.User)]
    public async Task<ActionResult<ApiResponse<long>>> CreateAuditLog([FromBody] AuditLog auditLog)
    {
        // FluentValidation will handle validation automatically
        try
        {
            // Set the timestamp if not provided
            if (auditLog.Timestamp == default) auditLog.Timestamp = DateTime.UtcNow;

            // Set user information from JWT token if not provided
            if (string.IsNullOrEmpty(auditLog.UserId)) auditLog.UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(auditLog.UserEmail)) auditLog.UserEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            // Set IP address from request context
            if (string.IsNullOrEmpty(auditLog.IpAddress)) auditLog.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Set User-Agent from request headers
            if (string.IsNullOrEmpty(auditLog.UserAgent)) auditLog.UserAgent = Request.Headers.UserAgent.ToString();

            var result = await _auditLogService.CreateAuditLogAsync(auditLog);
            if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result);

            return CreatedAtAction(nameof(GetAuditLog), new { id = result.Data }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log");
            return StatusCode(500, "An error occurred while creating the audit log");
        }
    }

    /// <summary>
    /// Create audit log entry for internal services.
    /// Intended for inter-service communication only: the caller must present the shared
    /// service key in the X-Internal-Api-Key header.
    /// </summary>
    /// <param name="auditLog">Audit log data</param>
    /// <returns>Created audit log ID</returns>
    [HttpPost("internal")]
    [Authorize(Policy = InternalApiKeyDefaults.PolicyName)]
    public async Task<ActionResult<ApiResponse<long>>> CreateInternalAuditLog([FromBody] AuditLog auditLog)
    {
        // FluentValidation will handle validation automatically
        try
        {
            // Set the timestamp if not provided
            if (auditLog.Timestamp == default) auditLog.Timestamp = DateTime.UtcNow;

            // For internal audit logs, accept the provided user information as-is
            // since it comes from other authenticated services

            // Set IP address from request context if not provided
            if (string.IsNullOrEmpty(auditLog.IpAddress)) auditLog.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Set User-Agent from request headers if not provided
            if (string.IsNullOrEmpty(auditLog.UserAgent)) auditLog.UserAgent = Request.Headers.UserAgent.ToString();

            var result = await _auditLogService.CreateAuditLogAsync(auditLog);
            if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result);

            return CreatedAtAction(nameof(GetAuditLog), new { id = result.Data }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating internal audit log");
            return StatusCode(500, "An error occurred while creating the audit log");
        }
    }

    /// <summary>
    /// Get a specific audit log by ID
    /// </summary>
    /// <param name="id">Audit log ID</param>
    /// <returns>Audit log details</returns>
    [HttpGet("{id}")]
    [Authorize(Policy = Policies.Admin)]
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
    [Authorize(Policy = Policies.Admin)]
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
    [Authorize(Policy = Policies.Admin)]
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
    [Authorize(Policy = Policies.Admin)]
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
    [Authorize(Policy = Policies.Admin)]
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
