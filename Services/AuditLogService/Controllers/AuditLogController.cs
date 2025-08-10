using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.AuditLogService.Services;
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
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<ActionResult<long>> CreateAuditLog([FromBody] AuditLog auditLog)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Set the timestamp if not provided
            if (auditLog.Timestamp == default)
            {
                auditLog.Timestamp = DateTime.UtcNow;
            }

            // Set user information from JWT token if not provided
            if (string.IsNullOrEmpty(auditLog.UserId))
            {
                auditLog.UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
            
            if (string.IsNullOrEmpty(auditLog.UserEmail))
            {
                auditLog.UserEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            }

            // Set IP address from request context
            if (string.IsNullOrEmpty(auditLog.IpAddress))
            {
                auditLog.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            }

            // Set User-Agent from request headers
            if (string.IsNullOrEmpty(auditLog.UserAgent))
            {
                auditLog.UserAgent = Request.Headers.UserAgent.ToString();
            }

            var auditLogId = await _auditLogService.CreateAuditLogAsync(auditLog);
            
            return CreatedAtAction(nameof(GetAuditLog), new { id = auditLogId }, auditLogId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log");
            return StatusCode(500, "An error occurred while creating the audit log");
        }
    }

    /// <summary>
    /// Create audit log entry for internal services (no authentication required)
    /// This endpoint is intended for inter-service communication for audit logging
    /// </summary>
    /// <param name="auditLog">Audit log data</param>
    /// <returns>Created audit log ID</returns>
    [HttpPost("internal")]
    [AllowAnonymous]
    public async Task<ActionResult<long>> CreateInternalAuditLog([FromBody] AuditLog auditLog)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Set the timestamp if not provided
            if (auditLog.Timestamp == default)
            {
                auditLog.Timestamp = DateTime.UtcNow;
            }

            // For internal audit logs, accept the provided user information as-is
            // since it comes from other authenticated services

            // Set IP address from request context if not provided
            if (string.IsNullOrEmpty(auditLog.IpAddress))
            {
                auditLog.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            }

            // Set User-Agent from request headers if not provided
            if (string.IsNullOrEmpty(auditLog.UserAgent))
            {
                auditLog.UserAgent = Request.Headers.UserAgent.ToString();
            }

            var auditLogId = await _auditLogService.CreateAuditLogAsync(auditLog);
            
            return CreatedAtAction(nameof(GetAuditLog), new { id = auditLogId }, auditLogId);
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
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<AuditLog>> GetAuditLog(long id)
    {
        try
        {
            var auditLog = await _auditLogService.GetAuditLogAsync(id);
            
            if (auditLog == null)
            {
                return NotFound($"Audit log with ID {id} not found");
            }

            return Ok(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log with ID: {AuditLogId}", id);
            return StatusCode(500, "An error occurred while retrieving the audit log");
        }
    }

    /// <summary>
    /// Get audit logs with pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <returns>Paginated list of audit logs</returns>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<object>> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var auditLogs = await _auditLogService.GetAuditLogsAsync(page, pageSize);
            var totalCount = await _auditLogService.GetTotalCountAsync();

            var response = new
            {
                auditLogs,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                hasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize),
                hasPreviousPage = page > 1
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, "An error occurred while retrieving audit logs");
        }
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
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<object>> GetAuditLogsByEntity(
        string entityName, 
        [FromQuery] string? entityId = null,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var auditLogs = await _auditLogService.GetAuditLogsByEntityAsync(entityName, entityId, page, pageSize);
            var totalCount = await _auditLogService.GetTotalCountByEntityAsync(entityName, entityId);

            var response = new
            {
                auditLogs,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for entity {EntityName}", entityName);
            return StatusCode(500, "An error occurred while retrieving audit logs for the entity");
        }
    }

    /// <summary>
    /// Get audit logs for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <returns>Paginated list of audit logs for the user</returns>
    [HttpGet("user/{userId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<object>> GetAuditLogsByUser(
        string userId,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var auditLogs = await _auditLogService.GetAuditLogsByUserAsync(userId, page, pageSize);
            var totalCount = await _auditLogService.GetTotalCountByUserAsync(userId);

            var response = new
            {
                auditLogs,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving audit logs for the user");
        }
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
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<object>> GetAuditLogsByDateRange(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate date range
            if (fromDate > toDate)
            {
                return BadRequest("From date cannot be greater than to date");
            }

            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var auditLogs = await _auditLogService.GetAuditLogsByDateRangeAsync(fromDate, toDate, page, pageSize);
            
            // For date range count, we need to implement a specific method or count the results
            var auditLogsList = auditLogs.ToList();
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for date range {FromDate} to {ToDate}", fromDate, toDate);
            return StatusCode(500, "An error occurred while retrieving audit logs for the date range");
        }
    }
}
