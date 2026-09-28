using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Shop;
using Store.ContentService.Data;
using Store.ContentService.Models;

namespace Store.ContentService.Services;

/// <summary>
/// The editorial pages for search engines: every published maker, collection, article and
/// lookbook at its address in the shop, with the day it last changed.
/// </summary>
public sealed class ContentSitemap
{
    private readonly ContentDbContext _db;
    private readonly ShopLinks _links;

    public ContentSitemap(ContentDbContext db, ShopLinks links)
    {
        _db = db;
        _links = links;
    }

    public async Task<string> RenderAsync(CancellationToken cancellationToken = default)
    {
        var entries = new List<SitemapEntry>();
        entries.AddRange(await PagesAsync<Maker>(_links.Maker, cancellationToken));
        entries.AddRange(await PagesAsync<Collection>(_links.Collection, cancellationToken));
        entries.AddRange(await PagesAsync<Article>(_links.Article, cancellationToken));
        entries.AddRange(await PagesAsync<Lookbook>(_links.Lookbook, cancellationToken));
        return Sitemap.Render(entries);
    }

    private async Task<IEnumerable<SitemapEntry>> PagesAsync<TEntry>(Func<string, string> page, CancellationToken cancellationToken)
        where TEntry : ContentEntry
    {
        var published = await _db.Set<TEntry>().AsNoTracking()
            .Where(e => e.IsPublished)
            .OrderBy(e => e.Slug)
            .Select(e => new { e.Slug, e.UpdatedAt })
            .ToListAsync(cancellationToken);
        return published.Select(e => new SitemapEntry(page(e.Slug), e.UpdatedAt));
    }
}
