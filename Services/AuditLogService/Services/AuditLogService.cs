using Microsoft.EntityFrameworkCore;
using Store.AuditLogService.Data;
using Store.AuditLogService.Models;
using Store.BuildingBlocks.Api;

namespace Store.AuditLogService.Services;

public class AuditLogService : IAuditLogService
{
    private const int DefaultPageSize = 50;

    private readonly AuditLogDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AuditLogDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<long> CreateAuditLogAsync(AuditLog auditLog)
    {
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Audit log created successfully with ID: {AuditLogId}", auditLog.Id);
        return auditLog.Id;
    }

    public async Task<AuditLog> GetAuditLogAsync(long id)
        => await _context.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new NotFoundException("Audit log", id);

    public async Task<PagedResponse<AuditLog>> GetAuditLogsAsync(AuditLogQuery query, PagedQuery paging)
    {
        if (query.From > query.To)
        {
            throw new DomainValidationException("From must not be later than To");
        }

        paging = paging.Normalized(DefaultPageSize);
        var entries = _context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrEmpty(query.EntityName))
        {
            entries = entries.Where(a => a.EntityName == query.EntityName);
        }
        if (!string.IsNullOrEmpty(query.EntityId))
        {
            entries = entries.Where(a => a.EntityId == query.EntityId);
        }
        if (!string.IsNullOrEmpty(query.UserId))
        {
            entries = entries.Where(a => a.UserId == query.UserId);
        }
        // Timestamps are stored as UTC instants; the bounds are converted before they reach the database
        if (query.From is { } from)
        {
            var fromUtc = from.UtcDateTime;
            entries = entries.Where(a => a.Timestamp >= fromUtc);
        }
        if (query.To is { } to)
        {
            var toUtc = to.UtcDateTime;
            entries = entries.Where(a => a.Timestamp <= toUtc);
        }

        var totalCount = await entries.CountAsync();
        var page = await entries
            .OrderByDescending(a => a.Timestamp)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync();

        return new PagedResponse<AuditLog>(page, totalCount, paging);
    }
}
