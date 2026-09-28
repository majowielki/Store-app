using Microsoft.EntityFrameworkCore;
using Store.ReviewService.Models;

namespace Store.ReviewService.Data;

public static class ReviewSeeder
{
    /// <summary>
    /// Adds the reviews of <see cref="DemoReviews"/> for every product of the demo catalogue that
    /// has none seeded yet, so products added to the catalogue later get theirs in a database seeded
    /// earlier. The reviews name their product by slug; <see cref="Services.SeedReviewLinker"/>
    /// fills in the ids once the catalogue answers. Runs with the migrations, when the catalogue
    /// may not be up yet.
    /// </summary>
    public static async Task SeedAsync(ReviewDbContext context, TimeProvider time)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var seeded = (await context.Reviews
                .Where(r => r.Source == ReviewSource.Seed && r.ProductSlug != null)
                .Select(r => r.ProductSlug!)
                .Distinct()
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        foreach (var (slug, kind) in DemoReviews.Products.Where(p => !seeded.Contains(p.Key)))
        {
            context.Reviews.AddRange(DemoReviews.For(slug, kind, now));
        }

        var version = await context.ReviewSeeds.FirstOrDefaultAsync(s => s.Id == ReviewSeed.SingletonId);
        if (version is null)
        {
            context.ReviewSeeds.Add(new ReviewSeed { Version = DemoReviews.Version, AppliedAt = now });
        }
        else if (version.Version != DemoReviews.Version)
        {
            version.Version = DemoReviews.Version;
            version.AppliedAt = now;
        }

        await context.SaveChangesAsync();
    }
}
