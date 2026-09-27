using Microsoft.EntityFrameworkCore;
using Store.ContentService.Models;

namespace Store.ContentService.Data;

public static class ContentSeeder
{
    /// <summary>
    /// Adds the demo content the database does not have yet, matched by slug within each kind,
    /// so entries added to <see cref="DemoContent"/> later reach a database seeded earlier. What
    /// is there already - edited, unpublished - is left alone; a deleted entry comes back.
    /// </summary>
    public static async Task SeedAsync(ContentDbContext context, TimeProvider time)
    {
        var now = time.GetUtcNow().UtcDateTime;

        // A maker is also unique by company: one edited to another slug is not added twice
        var companies = (await context.Makers.Select(m => m.Company).ToListAsync()).ToHashSet();
        await AddMissingAsync(context.Makers, DemoContent.Makers().Where(m => companies.Add(m.Company)), now);
        await AddMissingAsync(context.Collections, DemoContent.Collections(), now);
        await AddMissingAsync(context.Articles, DemoContent.Articles(), now);
        await AddMissingAsync(context.Lookbooks, DemoContent.Lookbooks(), now);

        await context.SaveChangesAsync();
    }

    private static async Task AddMissingAsync<TEntry>(DbSet<TEntry> entries, IEnumerable<TEntry> demo, DateTime now)
        where TEntry : ContentEntry
    {
        var known = (await entries.Select(e => e.Slug).ToListAsync()).ToHashSet(StringComparer.Ordinal);
        foreach (var entry in demo.Where(e => !known.Contains(e.Slug)))
        {
            entry.CreatedAt = entry.UpdatedAt = now;
            entries.Add(entry);
        }
    }
}
