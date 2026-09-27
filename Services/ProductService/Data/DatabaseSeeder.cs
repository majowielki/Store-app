using Microsoft.EntityFrameworkCore;
using Store.ProductService.Models;

namespace Store.ProductService.Data;

public static class DatabaseSeeder
{
    /// <summary>
    /// Brings the database to the current <see cref="DemoCatalogue"/>:
    /// <list type="bullet">
    /// <item>once per catalogue version, demo products seeded by an earlier version get the
    /// current data (description, pictures and their points, colours...) and the retired ones
    /// are deactivated; what an administrator changes after that stays as it is</item>
    /// <item>every time, the demo products the catalogue does not have yet are added, matched by
    /// title - so products added to the list later reach a database seeded earlier. Products
    /// already there, edited or deactivated (a delete only deactivates), are not added again; a
    /// demo product renamed by an admin does come back under its original title</item>
    /// </list>
    /// </summary>
    public static async Task SeedAsync(ProductDbContext context)
    {
        var demo = DemoCatalogue.Products();
        var existing = (await context.Products.Include(p => p.Images).ToListAsync())
            .GroupBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var seed = await context.CatalogueSeeds.FindAsync(CatalogueSeed.SingletonId);
        if (seed is null || seed.Version < DemoCatalogue.Version)
        {
            var now = DateTime.UtcNow;
            foreach (var product in demo)
            {
                if (existing.TryGetValue(product.Title, out var seeded))
                {
                    CopyCatalogueData(product, seeded, now);
                }
            }

            foreach (var title in DemoCatalogue.RetiredTitles)
            {
                if (existing.TryGetValue(title, out var retired) && retired.IsActive)
                {
                    retired.IsActive = false;
                    retired.UpdatedAt = now;
                }
            }

            if (seed is null)
            {
                seed = new CatalogueSeed();
                context.CatalogueSeeds.Add(seed);
            }

            seed.Version = DemoCatalogue.Version;
            seed.AppliedAt = now;
        }

        // A product an administrator created under a similar title may hold a demo slug already
        var slugs = existing.Values.Select(p => p.Slug).ToHashSet(StringComparer.Ordinal);
        context.Products.AddRange(demo.Where(product => !existing.ContainsKey(product.Title) && slugs.Add(product.Slug)));
        await context.SaveChangesAsync();
    }

    /// <summary>Everything the demo catalogue defines; the id, the active flag and the creation time stay.</summary>
    private static void CopyCatalogueData(Product from, Product to, DateTime now)
    {
        to.Description = from.Description;
        to.Price = from.Price;
        to.SalePrice = from.SalePrice;
        to.DiscountPercent = from.DiscountPercent;
        to.Category = from.Category;
        to.Company = from.Company;
        to.NewArrival = from.NewArrival;
        to.Image = from.Image;
        to.Colors = from.Colors;
        to.Groups = from.Groups;
        to.Materials = from.Materials;
        to.WidthCm = from.WidthCm;
        to.HeightCm = from.HeightCm;
        to.DepthCm = from.DepthCm;
        to.WeightKg = from.WeightKg;
        to.Hotspots = from.Hotspots;
        to.Images.Clear();
        to.Images.AddRange(from.Images);
        to.UpdatedAt = now;
    }
}
