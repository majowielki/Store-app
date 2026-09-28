using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Shop;
using Store.ProductService.Data;

namespace Store.ProductService.Services;

/// <summary>The catalogue's pages for search engines: every active product's page in the shop and the day it last changed.</summary>
public sealed class ProductSitemap
{
    private readonly ProductDbContext _context;
    private readonly ShopLinks _links;

    public ProductSitemap(ProductDbContext context, ShopLinks links)
    {
        _context = context;
        _links = links;
    }

    public async Task<string> RenderAsync(CancellationToken cancellationToken = default)
    {
        var products = await _context.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.UpdatedAt })
            .Take(Sitemap.MaxEntries)
            .ToListAsync(cancellationToken);

        return Sitemap.Render(products.Select(p => new SitemapEntry(_links.Product(p.Id), p.UpdatedAt)));
    }
}
