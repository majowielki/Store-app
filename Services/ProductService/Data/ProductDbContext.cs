using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Messaging;
using Store.ProductService.Models;

namespace Store.ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Money and dimensions: two decimal places everywhere unless a property says otherwise
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(ProductConstraints.TitleMaxLength);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(ProductConstraints.DescriptionMaxLength);
            entity.Property(e => e.DiscountPercent).HasPrecision(5, 2);
            entity.Property(e => e.Image).IsRequired();

            // Enums are stored by name: adding a value in the middle of the enum then changes
            // nothing in the database, which it would with the integer mapping
            entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(ProductConstraints.EnumMaxLength);
            entity.Property(e => e.Company).HasConversion<string>().HasMaxLength(ProductConstraints.EnumMaxLength);

            // Colors, Groups and Materials are native PostgreSQL text[] columns (EF primitive
            // collections), so filters such as p.Colors.Any(...) translate to SQL instead of throwing.
            // The GIN indexes back the overlap/containment operators those filters use.
            entity.Property(e => e.Colors).HasColumnType("text[]");
            entity.Property(e => e.Groups).HasColumnType("text[]");
            entity.Property(e => e.Materials).HasColumnType("text[]");
            entity.HasIndex(e => e.Colors).HasMethod("gin");
            entity.HasIndex(e => e.Groups).HasMethod("gin");
            entity.HasIndex(e => e.Materials).HasMethod("gin");

            // Columns every catalogue query filters or sorts on
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Company);
            entity.HasIndex(e => e.Title);

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
        });

        // Outbox of the message bus: catalogue changes leave as audit events
        modelBuilder.AddStoreMessagingTables();
    }
}
