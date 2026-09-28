using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.Contracts.Audit;
using Store.OrderService.Data;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Models;

namespace Store.OrderService.Services;

/// <summary>
/// Discount codes: the cart's preview of a code and the admin panel's list and forms. The code
/// is applied, counted and checked once more at checkout, by the order service under a lock.
/// </summary>
public sealed class DiscountCodeService
{
    private readonly OrderDbContext _context;
    private readonly IAuditTrail _audit;
    private readonly TimeProvider _time;

    public DiscountCodeService(OrderDbContext context, IAuditTrail audit, TimeProvider time)
    {
        _context = context;
        _audit = audit;
        _time = time;
    }

    /// <summary>What <paramref name="code"/> takes off <paramref name="subtotal"/>; a code that cannot be used is a 422 saying why.</summary>
    public async Task<DiscountCodeCheckResponse> CheckAsync(string code, decimal subtotal)
    {
        var normalized = DiscountCode.Normalize(code);
        var entry = await _context.DiscountCodes.AsNoTracking().FirstOrDefaultAsync(c => c.Code == normalized);
        var check = DiscountCodePolicy.Check(entry, subtotal, _time.GetUtcNow().UtcDateTime);
        if (!check.IsUsable)
        {
            throw new DomainValidationException(check.Refusal!);
        }

        return new DiscountCodeCheckResponse { Code = entry!.Code, Kind = entry.Kind.ToString(), Value = entry.Value, DiscountAmount = check.Amount };
    }

    public async Task<List<DiscountCodeResponse>> ListAsync()
        => (await _context.DiscountCodes.AsNoTracking().OrderBy(c => c.Code).ToListAsync()).Select(Map).ToList();

    public async Task<DiscountCodeResponse> GetAsync(int id) => Map(await FindAsync(id, tracked: false));

    public async Task<DiscountCodeResponse> CreateAsync(DiscountCodeRequest request, string actorId)
    {
        var code = new DiscountCode { CreatedAt = _time.GetUtcNow().UtcDateTime };
        Apply(code, request);
        await EnsureCodeIsFreeAsync(code.Code, exceptId: null);

        _context.DiscountCodes.Add(code);
        await _context.SaveChangesAsync();

        await _audit.RecordAsync(AuditActions.DiscountCodeCreated, nameof(DiscountCode), code.Id.ToString(), actorId, newValues: Map(code));
        return Map(code);
    }

    public async Task<DiscountCodeResponse> UpdateAsync(int id, DiscountCodeRequest request, string actorId)
    {
        var code = await FindAsync(id, tracked: true);
        var oldValues = Map(code);

        Apply(code, request);
        await EnsureCodeIsFreeAsync(code.Code, exceptId: id);
        await _context.SaveChangesAsync();

        await _audit.RecordAsync(AuditActions.DiscountCodeUpdated, nameof(DiscountCode), id.ToString(), actorId, oldValues: oldValues, newValues: Map(code));
        return Map(code);
    }

    /// <summary>A code no order used can be deleted; one that was used stays, deactivated, so the orders still name it.</summary>
    public async Task DeleteAsync(int id, string actorId)
    {
        var code = await FindAsync(id, tracked: true);
        if (code.TimesUsed > 0)
        {
            throw new ConflictException($"{code.Code} was used on {code.TimesUsed} order(s). Switch it off instead of deleting it.");
        }

        _context.DiscountCodes.Remove(code);
        await _context.SaveChangesAsync();

        await _audit.RecordAsync(AuditActions.DiscountCodeDeleted, nameof(DiscountCode), id.ToString(), actorId, oldValues: Map(code));
    }

    private async Task<DiscountCode> FindAsync(int id, bool tracked)
    {
        var codes = tracked ? _context.DiscountCodes : _context.DiscountCodes.AsNoTracking();
        return await codes.FirstOrDefaultAsync(c => c.Id == id) ?? throw new NotFoundException("Discount code", id);
    }

    private async Task EnsureCodeIsFreeAsync(string code, int? exceptId)
    {
        if (await _context.DiscountCodes.AnyAsync(c => c.Code == code && c.Id != exceptId))
        {
            throw new ConflictException($"The code {code} exists already.");
        }
    }

    private void Apply(DiscountCode code, DiscountCodeRequest request)
    {
        code.Code = DiscountCode.Normalize(request.Code);
        code.Kind = Enum.Parse<DiscountKind>(request.Kind);
        code.Value = request.Value;
        code.MinimumSubtotal = request.MinimumSubtotal;
        code.StartsAt = request.StartsAt?.ToUniversalTime();
        code.ExpiresAt = request.ExpiresAt?.ToUniversalTime();
        code.UsageLimit = request.UsageLimit;
        code.IsActive = request.IsActive;
        code.UpdatedAt = _time.GetUtcNow().UtcDateTime;
    }

    private static DiscountCodeResponse Map(DiscountCode code) => new()
    {
        Id = code.Id,
        Code = code.Code,
        Kind = code.Kind.ToString(),
        Value = code.Value,
        MinimumSubtotal = code.MinimumSubtotal,
        StartsAt = code.StartsAt,
        ExpiresAt = code.ExpiresAt,
        UsageLimit = code.UsageLimit,
        TimesUsed = code.TimesUsed,
        IsActive = code.IsActive,
        CreatedAt = code.CreatedAt,
        UpdatedAt = code.UpdatedAt
    };
}
