using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Store.Contracts.Catalog;
using Store.ProductService.Data;
using Store.ProductService.Models;
using Store.Tests.Integration.TestSupport;
using Xunit;

namespace Store.Tests.Integration.Catalog;

[Collection(PostgresTests.Name)]
public sealed class DatabaseSeederTests : IClassFixture<CatalogApiFactory>
{
    private readonly CatalogApiFactory _factory;

    public DatabaseSeederTests(CatalogApiFactory factory)
    {
        _factory = factory;
    }

    // The seeder used to do nothing once the catalogue had any product, so a database seeded
    // before new demo products were added never received them
    [Fact]
    public async Task Seeding_an_existing_catalogue_adds_only_the_demo_products_it_lacks()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        // The catalogue other tests read is left as it was
        await using var transaction = await db.Database.BeginTransactionAsync();

        var removed = await db.Products.OrderBy(p => p.Id).FirstAsync();
        var deactivated = await db.Products.OrderBy(p => p.Id).Skip(1).FirstAsync();
        db.Products.Remove(removed);
        deactivated.IsActive = false;
        await db.SaveChangesAsync();
        var before = await db.Products.CountAsync();

        await DatabaseSeeder.SeedAsync(db);
        await DatabaseSeeder.SeedAsync(db);

        Assert.Equal(before + 1, await db.Products.CountAsync());
        Assert.Equal(1, await db.Products.CountAsync(p => p.Title == removed.Title));
        var stillDeactivated = await db.Products.AsNoTracking().SingleAsync(p => p.Title == deactivated.Title);
        Assert.False(stillDeactivated.IsActive);
    }

    // A database seeded with the first catalogue (grey laminate dresser, JPEG pictures, no gallery,
    // the memory foam mattress) is brought to the current one once; after that the seeder leaves
    // what an administrator changes alone
    [Fact]
    public async Task A_catalogue_seeded_by_an_earlier_version_is_brought_up_to_date_once()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.CatalogueSeeds.ExecuteDeleteAsync();
        var dresser = await db.Products.Include(p => p.Images).SingleAsync(p => p.Title == "8-Drawer Dresser");
        dresser.Images.Clear();
        dresser.Hotspots = [];
        dresser.Image = "http://localhost:10000/devstoreaccount1/product-images/8DrawerDresser.jpg";
        dresser.Colors = ["Gray"];
        dresser.Materials = ["engineered-wood", "metal"];
        dresser.Description = "Wide dresser with soft-close drawers and metal handles.";
        db.Products.Add(new Product
        {
            Title = "Memory Foam Mattress",
            Slug = "memory-foam-mattress",
            Description = "Queen size memory foam mattress with cooling gel layer.",
            Price = 499.99m,
            Category = Category.Mattresses,
            Company = Company.Comfora,
            Image = "http://localhost:10000/devstoreaccount1/product-images/MemoryFoamMattress.jpg",
            Colors = ["White"],
            Groups = ["bedroom"]
        });
        await db.SaveChangesAsync();

        await DatabaseSeeder.SeedAsync(db);
        db.ChangeTracker.Clear();

        var updated = await db.Products.SingleAsync(p => p.Title == "8-Drawer Dresser");
        Assert.EndsWith("/8DrawerDresser-1.webp", updated.Image, StringComparison.Ordinal);
        Assert.Equal(new[] { "Brown" }, updated.Colors);
        Assert.Contains("brass", updated.Materials);
        Assert.Equal(2, await db.Set<ProductImage>().CountAsync(i => i.ProductId == updated.Id));
        Assert.Contains(updated.Hotspots, point => point.ProductSlug == "rattan-round-wall-mirror");
        var mattress = await db.Products.SingleAsync(p => p.Title == "Memory Foam Mattress");
        Assert.False(mattress.IsActive);
        Assert.Equal(DemoCatalogue.Version, (await db.CatalogueSeeds.SingleAsync()).Version);

        // An administrator restores the mattress and rewrites the dresser; the next start keeps both
        mattress.IsActive = true;
        updated.Description = "The dresser as the shop owner describes it.";
        await db.SaveChangesAsync();
        await DatabaseSeeder.SeedAsync(db);
        db.ChangeTracker.Clear();

        Assert.True((await db.Products.SingleAsync(p => p.Title == "Memory Foam Mattress")).IsActive);
        Assert.Equal("The dresser as the shop owner describes it.", (await db.Products.SingleAsync(p => p.Title == "8-Drawer Dresser")).Description);
    }

    [Fact]
    public async Task A_new_database_holds_the_whole_demo_catalogue_and_none_of_the_retired_products()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var titles = await db.Products.Select(p => p.Title).ToListAsync();

        Assert.All(DemoCatalogue.Products(), product => Assert.Contains(product.Title, titles));
        Assert.Empty(titles.Intersect(DemoCatalogue.RetiredTitles));
        Assert.Equal(DemoCatalogue.Version, (await db.CatalogueSeeds.SingleAsync()).Version);
    }
}
