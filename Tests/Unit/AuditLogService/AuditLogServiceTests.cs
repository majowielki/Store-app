using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Store.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Store.AuditLogService.Data;

namespace Store.Tests.Unit.AuditLogService;

public class AuditLogServiceTests
{
    private readonly Mock<ILogger<Store.AuditLogService.Services.AuditLogService>> _loggerMock = new();
    private readonly AuditLogDbContext _dbContext;
    private readonly Store.AuditLogService.Services.AuditLogService _auditLogService;

    public AuditLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<AuditLogDbContext>()
            .UseInMemoryDatabase(databaseName: "AuditLogServiceTests")
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
        var result = await _auditLogService.CreateAuditLogAsync(log);
        Assert.True(result.IsSuccess);
        Assert.True(result.Data > 0);
    }

    [Fact]
    public async Task GetAuditLogAsync_Returns_Error_If_Not_Found()
    {
        var result = await _auditLogService.GetAuditLogAsync(999);
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task GetAuditLogAsync_Returns_Log_If_Found()
    {
        var log = new AuditLog { Action = "READ", EntityName = "TestEntity", Timestamp = System.DateTime.UtcNow };
        _dbContext.AuditLogs.Add(log);
        await _dbContext.SaveChangesAsync();
        var result = await _auditLogService.GetAuditLogAsync(log.Id);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(log.Id, result.Data.Id);
    }

    [Fact]
    public async Task GetAuditLogsAsync_Returns_Paginated_Logs()
    {
        for (int i = 0; i < 5; i++)
        {
            _dbContext.AuditLogs.Add(new AuditLog { Action = "PAGE", EntityName = "Entity", Timestamp = System.DateTime.UtcNow });
        }
        await _dbContext.SaveChangesAsync();
        var result = await _auditLogService.GetAuditLogsAsync(1, 3);
        Assert.True(result.IsSuccess);
        Assert.True(result.Data.Count() <= 3);
    }
}
