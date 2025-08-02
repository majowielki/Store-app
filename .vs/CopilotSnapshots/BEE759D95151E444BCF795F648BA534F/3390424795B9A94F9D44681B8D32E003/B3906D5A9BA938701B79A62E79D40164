using Microsoft.EntityFrameworkCore;
using Store.AuditLogService.Data;
using Store.Shared.Models;
using Microsoft.Extensions.Logging;

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

    public async Task<long> CreateAuditLogAsync(AuditLog auditLog)
    {
        try
        {
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Audit log created successfully with ID: {AuditLogId}", auditLog.Id);
            return auditLog.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log for entity {EntityName} with ID {EntityId}", 
                auditLog.EntityName, auditLog.EntityId);
            throw;
        }
    }

    public async Task<AuditLog?> GetAuditLogAsync(long id)
    {
        try
        {
            return await _context.AuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log with ID: {AuditLogId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            
            return await _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(a => a.Timestamp)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for page {Page}, pageSize {PageSize}", page, pageSize);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByEntityAsync(string entityName, string? entityId = null, int page = 1, int pageSize = 50)
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

            return await query
                .OrderByDescending(a => a.Timestamp)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for entity {EntityName} with ID {EntityId}", entityName, entityId);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(string userId, int page = 1, int pageSize = 50)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            
            return await _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByDateRangeAsync(DateTime fromDate, DateTime toDate, int page = 1, int pageSize = 50)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            
            return await _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.Timestamp >= fromDate && a.Timestamp <= toDate)
                .OrderByDescending(a => a.Timestamp)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for date range {FromDate} to {ToDate}", fromDate, toDate);
            throw;
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        try
        {
            return await _context.AuditLogs.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total audit log count");
            throw;
        }
    }

    public async Task<int> GetTotalCountByEntityAsync(string entityName, string? entityId = null)
    {
        try
        {
            var query = _context.AuditLogs.Where(a => a.EntityName == entityName);
            
            if (!string.IsNullOrEmpty(entityId))
            {
                query = query.Where(a => a.EntityId == entityId);
            }

            return await query.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit log count for entity {EntityName} with ID {EntityId}", entityName, entityId);
            throw;
        }
    }

    public async Task<int> GetTotalCountByUserAsync(string userId)
    {
        try
        {
            return await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit log count for user {UserId}", userId);
            throw;
        }
    }
}
