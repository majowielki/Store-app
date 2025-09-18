using Microsoft.EntityFrameworkCore;
using Store.AuditLogService.Data;
using Store.Shared.Models;
using Microsoft.Extensions.Logging;
using Store.Shared.Models;

namespace Store.AuditLogService.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AuditLogDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AuditLogDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<long>> CreateAuditLogAsync(AuditLog auditLog)
    {
        try
        {
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Audit log created successfully with ID: {AuditLogId}", auditLog.Id);
            return ApiResponse<long>.Success(auditLog.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log for entity {EntityName} with ID {EntityId}", 
                auditLog.EntityName, auditLog.EntityId);
            return ApiResponse<long>.Error("An error occurred while creating audit log");
        }
    }

    public async Task<ApiResponse<AuditLog?>> GetAuditLogAsync(long id)
    {
        try
        {
            var log = await _context.AuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
            if (log == null)
                return ApiResponse<AuditLog?>.Error("Audit log not found");
            return ApiResponse<AuditLog?>.Success(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log with ID: {AuditLogId}", id);
            return ApiResponse<AuditLog?>.Error("An error occurred while retrieving audit log");
        }
    }

    public async Task<ApiResponse<IEnumerable<AuditLog>>> GetAuditLogsAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var logs = await _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(a => a.Timestamp)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
            
            return ApiResponse<IEnumerable<AuditLog>>.Success(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for page {Page}, pageSize {PageSize}", page, pageSize);
            return ApiResponse<IEnumerable<AuditLog>>.Error("An error occurred while retrieving audit logs");
        }
    }

    public async Task<ApiResponse<IEnumerable<AuditLog>>> GetAuditLogsByEntityAsync(string entityName, string? entityId = null, int page = 1, int pageSize = 50)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var query = _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.EntityName == entityName);

            if (!string.IsNullOrEmpty(entityId))
            {
                query = query.Where(a => a.EntityId == entityId);
            }

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
            
            return ApiResponse<IEnumerable<AuditLog>>.Success(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for entity {EntityName} with ID {EntityId}", entityName, entityId);
            return ApiResponse<IEnumerable<AuditLog>>.Error("An error occurred while retrieving audit logs");
        }
    }

    public async Task<ApiResponse<IEnumerable<AuditLog>>> GetAuditLogsByUserAsync(string userId, int page = 1, int pageSize = 50)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var logs = await _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
            
            return ApiResponse<IEnumerable<AuditLog>>.Success(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for user {UserId}", userId);
            return ApiResponse<IEnumerable<AuditLog>>.Error("An error occurred while retrieving audit logs");
        }
    }

    public async Task<ApiResponse<IEnumerable<AuditLog>>> GetAuditLogsByDateRangeAsync(DateTime fromDate, DateTime toDate, int page = 1, int pageSize = 50)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var logs = await _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.Timestamp >= fromDate && a.Timestamp <= toDate)
                .OrderByDescending(a => a.Timestamp)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
            
            return ApiResponse<IEnumerable<AuditLog>>.Success(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for date range {FromDate} to {ToDate}", fromDate, toDate);
            return ApiResponse<IEnumerable<AuditLog>>.Error("An error occurred while retrieving audit logs");
        }
    }

    public async Task<ApiResponse<int>> GetTotalCountAsync()
    {
        try
        {
            var count = await _context.AuditLogs.CountAsync();
            return ApiResponse<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total audit log count");
            return ApiResponse<int>.Error("An error occurred while getting audit log count");
        }
    }

    public async Task<ApiResponse<int>> GetTotalCountByEntityAsync(string entityName, string? entityId = null)
    {
        try
        {
            var query = _context.AuditLogs.Where(a => a.EntityName == entityName);
            
            if (!string.IsNullOrEmpty(entityId))
            {
                query = query.Where(a => a.EntityId == entityId);
            }

            var count = await query.CountAsync();
            return ApiResponse<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit log count for entity {EntityName} with ID {EntityId}", entityName, entityId);
            return ApiResponse<int>.Error("An error occurred while getting audit log count");
        }
    }

    public async Task<ApiResponse<int>> GetTotalCountByUserAsync(string userId)
    {
        try
        {
            var count = await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .CountAsync();
            
            return ApiResponse<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit log count for user {UserId}", userId);
            return ApiResponse<int>.Error("An error occurred while getting audit log count");
        }
    }
}
