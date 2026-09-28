using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Configuration;
using Store.IdentityService.Data;
using Store.IdentityService.Models;

namespace Store.IdentityService.Services;

/// <summary>A refresh token just handed out: the token itself (the client sees it once), its session and when it ends.</summary>
/// <param name="Token">The opaque token for the cookie; only its hash is stored</param>
/// <param name="SessionId">The token family, the same across the rotations of one sign-in</param>
/// <param name="ExpiresAt">When the token, and with it the session, ends</param>
public sealed record IssuedRefreshToken(string Token, Guid SessionId, DateTime ExpiresAt);

/// <summary>A refresh that went through: whose session it is and the token that replaces the presented one.</summary>
public sealed record RotatedSession(ApplicationUser User, IssuedRefreshToken RefreshToken);

/// <summary>
/// The refresh tokens of the sign-in sessions. A session is a family of tokens: every refresh
/// spends the presented token and hands out its successor in the same family; logout, a
/// deactivated account and a stolen copy end the family whole. Failures are
/// <see cref="InvalidCredentialsException"/>s.
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>A new session (a new family) for a user who has just proved who they are.</summary>
    Task<IssuedRefreshToken> StartSessionAsync(ApplicationUser user, string? clientAddress);

    /// <summary>
    /// Spends <paramref name="refreshToken"/> and hands out its successor. A token presented again
    /// within <see cref="JwtOptions.RefreshTokenReuseWindow"/> of its rotation, while the successor it
    /// got has not been used, gets a new successor instead (the client never received the first one);
    /// any other spent token ends the whole session.
    /// </summary>
    Task<RotatedSession> RotateAsync(string refreshToken, string? clientAddress);

    /// <summary>Ends the session <paramref name="refreshToken"/> belongs to; an unknown token changes nothing.</summary>
    Task EndSessionAsync(string refreshToken);
}

public sealed class RefreshTokenStore : IRefreshTokenStore
{
    /// <summary>
    /// How often a refresh looks again after losing a race with a simultaneous refresh of the same
    /// token (tabs opened together): each look sees what the other one committed.
    /// </summary>
    private const int MaxRaceRetries = 3;

    private const string AlreadyUsed = "Refresh token was already used";

    /// <summary>Why a whole session ends; written to the log.</summary>
    private enum SessionEnd
    {
        Logout,
        Reuse,
        InactiveAccount
    }

    private readonly IdentityDbContext _context;
    private readonly ITokenService _tokens;
    private readonly JwtOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<RefreshTokenStore> _logger;

    public RefreshTokenStore(IdentityDbContext context, ITokenService tokens, IOptions<JwtOptions> options, TimeProvider time, ILogger<RefreshTokenStore> logger)
    {
        _context = context;
        _tokens = tokens;
        _options = options.Value;
        _time = time;
        _logger = logger;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    public async Task<IssuedRefreshToken> StartSessionAsync(ApplicationUser user, string? clientAddress)
    {
        var (token, stored) = NewToken(user, Guid.NewGuid(), clientAddress, Now);
        _context.RefreshTokens.Add(stored);
        await _context.SaveChangesAsync();
        return Issued(token, stored);
    }

    public async Task<RotatedSession> RotateAsync(string refreshToken, string? clientAddress)
    {
        var hash = _tokens.HashRefreshToken(refreshToken);
        for (var attempt = 0; attempt < MaxRaceRetries; attempt++)
        {
            var presented = await _context.RefreshTokens.AsNoTracking()
                .Include(t => t.User)
                .SingleOrDefaultAsync(t => t.TokenHash == hash)
                ?? throw new InvalidCredentialsException("Invalid refresh token");

            var rotated = presented.RevokedAt is null
                ? await TryRotateAsync(presented, clientAddress)
                : await TryReissueAsync(presented, clientAddress);
            if (rotated is not null)
            {
                return rotated;
            }

            // Another refresh with the same token committed first: look again at what it left
            _context.ChangeTracker.Clear();
        }

        throw new InvalidCredentialsException(AlreadyUsed);
    }

    public async Task EndSessionAsync(string refreshToken)
    {
        var hash = _tokens.HashRefreshToken(refreshToken);
        var presented = await _context.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == hash);
        if (presented is not null)
        {
            await EndFamilyAsync(presented, SessionEnd.Logout);
        }
    }

    /// <summary>
    /// The usual refresh: the presented token is spent and its successor takes over. Spending is one
    /// conditional update in the transaction that stores the successor, so a simultaneous refresh
    /// with the same token either sees both or neither; null when that other refresh won.
    /// </summary>
    private async Task<RotatedSession?> TryRotateAsync(RefreshToken presented, string? clientAddress)
    {
        var now = Now;
        await EnsureUsableAsync(presented, now);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var (token, successor) = NewToken(presented.User, presented.FamilyId, clientAddress, now);
        if (!await SpendAsync(presented.Id, successor.TokenHash, now))
        {
            return null;
        }

        _context.RefreshTokens.Add(successor);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return new RotatedSession(presented.User, Issued(token, successor));
    }

    /// <summary>
    /// A spent token presented again. Within the reuse window, and while the successor it got is
    /// unused, the client lost the answer that carried the successor (a page left mid-refresh, tabs
    /// refreshing together): that successor is spent unseen and a new one issued, and the presented
    /// token points at it, so the same race repeated inside the window resolves the same way.
    /// Anything else is a replayed copy, and the session ends. Null when a simultaneous refresh won.
    /// </summary>
    private async Task<RotatedSession?> TryReissueAsync(RefreshToken presented, string? clientAddress)
    {
        var now = Now;
        var withinWindow = presented.ReplacedByHash is not null && now - presented.RevokedAt <= _options.RefreshTokenReuseWindow;
        var successor = withinWindow
            ? await _context.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == presented.ReplacedByHash)
            : null;
        if (successor is not { RevokedAt: null })
        {
            await EndFamilyAsync(presented, SessionEnd.Reuse);
            throw new InvalidCredentialsException(AlreadyUsed);
        }

        await EnsureUsableAsync(presented, now);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var (token, replacement) = NewToken(presented.User, presented.FamilyId, clientAddress, now);
        if (!await SpendAsync(successor.Id, replacement.TokenHash, now))
        {
            return null;
        }

        // Every token that led to the spent successor - the presented one, and any spent unseen the
        // same way by a refresh racing this one - now leads to the replacement, so whichever of
        // them the browser kept still resolves within the window
        await _context.RefreshTokens
            .Where(t => t.FamilyId == presented.FamilyId && t.ReplacedByHash == successor.TokenHash)
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.ReplacedByHash, replacement.TokenHash));
        _context.RefreshTokens.Add(replacement);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation("Refresh token of session {FamilyId} presented again {Elapsed} after its rotation; its unreceived successor was replaced",
            presented.FamilyId, now - presented.RevokedAt);
        return new RotatedSession(presented.User, Issued(token, replacement));
    }

    /// <summary>An expired token or a deactivated account ends here; the latter takes the whole session with it.</summary>
    private async Task EnsureUsableAsync(RefreshToken presented, DateTime now)
    {
        if (presented.ExpiresAt <= now)
        {
            throw new InvalidCredentialsException("Refresh token expired");
        }

        if (!presented.User.IsActive)
        {
            await EndFamilyAsync(presented, SessionEnd.InactiveAccount);
            throw new InvalidCredentialsException("Account is deactivated");
        }
    }

    /// <summary>Spends a token still unspent, naming its successor; false when another request spent it first.</summary>
    private async Task<bool> SpendAsync(long tokenId, string successorHash, DateTime now)
        => await _context.RefreshTokens
            .Where(t => t.Id == tokenId && t.RevokedAt == null)
            .ExecuteUpdateAsync(set => set
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.ReplacedByHash, successorHash)) == 1;

    private async Task EndFamilyAsync(RefreshToken presented, SessionEnd reason)
    {
        var now = Now;
        await _context.RefreshTokens
            .Where(t => t.FamilyId == presented.FamilyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.RevokedAt, now));

        if (reason == SessionEnd.Reuse)
        {
            _logger.LogWarning("Refresh token reuse for user {UserId}; session family {FamilyId} revoked", presented.UserId, presented.FamilyId);
        }
        else
        {
            _logger.LogInformation("Session family {FamilyId} of user {UserId} ended: {Reason}", presented.FamilyId, presented.UserId, reason);
        }
    }

    private (string Token, RefreshToken Stored) NewToken(ApplicationUser user, Guid familyId, string? clientAddress, DateTime now)
    {
        var (token, hash) = _tokens.CreateRefreshToken();
        return (token, new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            FamilyId = familyId,
            CreatedAt = now,
            ExpiresAt = now + _tokens.RefreshTokenLifetime,
            CreatedByIp = clientAddress
        });
    }

    private static IssuedRefreshToken Issued(string token, RefreshToken stored) => new(token, stored.FamilyId, stored.ExpiresAt);
}
