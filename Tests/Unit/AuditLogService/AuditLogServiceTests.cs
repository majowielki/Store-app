using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Store.AuditLogService.Data;
using Store.AuditLogService.Models;
using Store.AuditLogService.Services;
using Store.BuildingBlocks.Api;
using Xunit;

namespace Store.Tests.Unit.AuditLogService;

public class AuditLogServiceTests
{
    private readonly Mock<ILogger<Store.AuditLogService.Services.AuditLogService>> _loggerMock = new();
    private readonly AuditLogDbContext _dbContext;
    private readonly Store.AuditLogService.Services.AuditLogService _auditLogService;

    public AuditLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<AuditLogDbContext>()
            .UseInMemoryDatabase(databaseName: $"AuditLogServiceTests-{Guid.NewGuid():N}") // one database per test class instance - xUnit creates one per test
            .Options;
        _dbContext = new AuditLogDbContext(options);
        _auditLogService = new Store.AuditLogService.Services.AuditLogService(
            _dbContext,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task CreateAuditLogAsync_Creates_And_Returns_Id()
    {
        var log = new AuditLog { Action = "CREATE", EntityName = "TestEntity", Timestamp = System.DateTime.UtcNow };
        var id = await _auditLogService.CreateAuditLogAsync(log);
        Assert.True(id > 0);
    }

    [Fact]
    public async Task GetAuditLogAsync_Throws_NotFound_If_Not_Found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _auditLogService.GetAuditLogAsync(999));
    }

    [Fact]
    public async Task GetAuditLogAsync_Returns_Log_If_Found()
    {
        var log = new AuditLog { Action = "READ", EntityName = "TestEntity", Timestamp = System.DateTime.UtcNow };
        _dbContext.AuditLogs.Add(log);
        await _dbContext.SaveChangesAsync();
        var result = await _auditLogService.GetAuditLogAsync(log.Id);
        Assert.Equal(log.Id, result.Id);
    }

    [Fact]
    public async Task GetAuditLogsAsync_Returns_Paginated_Logs()
    {
        for (int i = 0; i < 5; i++)
        {
            _dbContext.AuditLogs.Add(new AuditLog { Action = "PAGE", EntityName = "Entity", Timestamp = System.DateTime.UtcNow });
        }
        await _dbContext.SaveChangesAsync();
        var result = await _auditLogService.GetAuditLogsAsync(new AuditLogQuery(), new PagedQuery { Page = 1, PageSize = 3 });
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }
}
