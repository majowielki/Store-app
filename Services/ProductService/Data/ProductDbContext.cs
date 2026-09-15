using Microsoft.EntityFrameworkCore;
using Store.Shared.Models;

namespace Store.ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SalePrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Image).IsRequired();

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

            // New fields mapping
            entity.Property(e => e.WidthCm).HasColumnType("decimal(18,2)");
            entity.Property(e => e.HeightCm).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DepthCm).HasColumnType("decimal(18,2)");
            entity.Property(e => e.WeightKg).HasColumnType("decimal(18,2)");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()"); // PostgreSQL syntax
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()"); // PostgreSQL syntax
        });
    }
}
