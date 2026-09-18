using Microsoft.EntityFrameworkCore;
using Store.IdentityService.Data;

namespace Store.IdentityService.Services;

/// <summary>
/// Deletes refresh tokens that can no longer be presented - expired, or revoked long enough
/// ago that reuse detection has nothing left to learn from them - once a day.
/// </summary>
public sealed class RefreshTokenCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    /// <summary>A revoked token is kept this long so a late replay is still recognised as reuse.</summary>
    private static readonly TimeSpan RevokedRetention = TimeSpan.FromDays(7);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;

    private readonly TimeProvider _time;

    public RefreshTokenCleanupService(IServiceScopeFactory scopeFactory, TimeProvider time, ILogger<RefreshTokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var deleted = await PurgeAsync(stoppingToken);
                if (deleted > 0)
                {
                    _logger.LogInformation("Deleted {Count} stale refresh tokens", deleted);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Refresh token cleanup failed; retrying tomorrow");
            }
        }
    }

    /// <summary>One purge; public so a test can run it without waiting a day.</summary>
    public async Task<int> PurgeAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var now = _time.GetUtcNow().UtcDateTime;
        var revokedBefore = now - RevokedRetention;
        return await context.RefreshTokens
            .Where(t => t.ExpiresAt < now || t.RevokedAt < revokedBefore)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
