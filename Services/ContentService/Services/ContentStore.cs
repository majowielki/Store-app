using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.ContentService.Data;
using Store.ContentService.Models;
using Store.Contracts.Audit;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Store.ContentService.Services;

/// <summary>
/// Reading and editing one kind of content. Every kind behaves the same way: the public sees
/// published entries only, a slug is unique within its kind (a clash is a conflict, not a
/// silent rename), and every change is recorded in the audit trail.
/// </summary>
public sealed class ContentStore<TEntry> where TEntry : ContentEntry
{
    private static readonly JsonSerializerOptions AuditJson = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ContentDbContext _db;
    private readonly IAuditTrail _audit;
    private readonly TimeProvider _time;

    public ContentStore(ContentDbContext db, IAuditTrail audit, TimeProvider time)
    {
        _db = db;
        _audit = audit;
        _time = time;
    }

    private static string Kind => typeof(TEntry).Name;

    private DbSet<TEntry> Entries => _db.Set<TEntry>();

    public Task<List<TEntry>> PublishedAsync(Func<IQueryable<TEntry>, IQueryable<TEntry>> order)
        => order(Entries.AsNoTracking().Where(e => e.IsPublished)).ToListAsync();

    /// <summary>A published entry; 404 for an unknown slug or an unpublished entry alike.</summary>
    public async Task<TEntry> PublishedAsync(string slug)
        => await Entries.AsNoTracking().FirstOrDefaultAsync(e => e.IsPublished && e.Slug == slug)
            ?? throw new NotFoundException(Kind, slug);

    public Task<List<TEntry>> AllAsync(Func<IQueryable<TEntry>, IQueryable<TEntry>> order)
        => order(Entries.AsNoTracking()).ToListAsync();

    public async Task<TEntry> FindAsync(int id)
        => await Entries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new NotFoundException(Kind, id);

    public async Task<TEntry> CreateAsync(TEntry entry, string? actorId)
    {
        await EnsureSlugIsFreeAsync(entry.Slug, exceptId: null);

        entry.CreatedAt = entry.UpdatedAt = _time.GetUtcNow().UtcDateTime;
        Entries.Add(entry);
        await _db.SaveChangesAsync();

        await _audit.RecordAsync(AuditActions.Created(Kind), Kind, entry.Id.ToString(), actorId, newValues: entry);
        return entry;
    }

    /// <summary>Replaces the entry's content with what <paramref name="apply"/> writes into it.</summary>
    public async Task<TEntry> UpdateAsync(int id, Action<TEntry> apply, string? actorId)
    {
        var entry = await Entries.FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new NotFoundException(Kind, id);
        var oldValues = JsonSerializer.Serialize(entry, AuditJson);

        apply(entry);
        await EnsureSlugIsFreeAsync(entry.Slug, exceptId: id);
        entry.UpdatedAt = _time.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync();

        await _audit.RecordAsync(AuditActions.Updated(Kind), Kind, id.ToString(), actorId, oldValues: oldValues, newValues: entry);
        return entry;
    }

    /// <summary>Content has nothing depending on it, so a delete removes the row; unpublishing hides it instead.</summary>
    public async Task DeleteAsync(int id, string? actorId)
    {
        var entry = await Entries.FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new NotFoundException(Kind, id);
        var oldValues = JsonSerializer.Serialize(entry, AuditJson);

        Entries.Remove(entry);
        await _db.SaveChangesAsync();

        await _audit.RecordAsync(AuditActions.Deleted(Kind), Kind, id.ToString(), actorId, oldValues: oldValues);
    }

    private async Task EnsureSlugIsFreeAsync(string slug, int? exceptId)
    {
        if (await Entries.AnyAsync(e => e.Slug == slug && e.Id != exceptId))
        {
            throw new ConflictException($"Another {Kind.ToLowerInvariant()} already uses the address \"{slug}\".");
        }
    }
}
