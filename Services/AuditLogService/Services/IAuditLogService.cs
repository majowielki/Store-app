using Store.AuditLogService.Models;
using Store.BuildingBlocks.Api;

namespace Store.AuditLogService.Services;

public interface IAuditLogService
{
    Task<ApiResponse<long>> CreateAuditLogAsync(AuditLog auditLog);
    Task<ApiResponse<AuditLog?>> GetAuditLogAsync(long id);
    Task<ApiResponse<IEnumerable<AuditLog>>> GetAuditLogsAsync(int page = 1, int pageSize = 50);
    Task<ApiResponse<IEnumerable<AuditLog>>> GetAuditLogsByEntityAsync(string entityName, string? entityId = null, int page = 1, int pageSize = 50);
    Task<ApiResponse<IEnumerable<AuditLog>>> GetAuditLogsByUserAsync(string userId, int page = 1, int pageSize = 50);
    Task<ApiResponse<IEnumerable<AuditLog>>> GetAuditLogsByDateRangeAsync(DateTime fromDate, DateTime toDate, int page = 1, int pageSize = 50);
    Task<ApiResponse<int>> GetTotalCountAsync();
    Task<ApiResponse<int>> GetTotalCountByEntityAsync(string entityName, string? entityId = null);
    Task<ApiResponse<int>> GetTotalCountByUserAsync(string userId);
}
