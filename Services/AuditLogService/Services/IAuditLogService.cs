using Store.Shared.Models;

namespace Store.AuditLogService.Services;

public interface IAuditLogService
{
    Task<long> CreateAuditLogAsync(AuditLog auditLog);
    Task<AuditLog?> GetAuditLogAsync(long id);
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int page = 1, int pageSize = 50);
    Task<IEnumerable<AuditLog>> GetAuditLogsByEntityAsync(string entityName, string? entityId = null, int page = 1, int pageSize = 50);
    Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(string userId, int page = 1, int pageSize = 50);
    Task<IEnumerable<AuditLog>> GetAuditLogsByDateRangeAsync(DateTime fromDate, DateTime toDate, int page = 1, int pageSize = 50);
    Task<int> GetTotalCountAsync();
    Task<int> GetTotalCountByEntityAsync(string entityName, string? entityId = null);
    Task<int> GetTotalCountByUserAsync(string userId);
}