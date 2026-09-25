using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Store.ProductService.Data;
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
}
