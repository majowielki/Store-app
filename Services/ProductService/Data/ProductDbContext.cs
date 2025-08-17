using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Store.Shared.Models;

namespace Store.ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

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
            // Configure Colors with conversion and a ValueComparer to avoid EF warnings for mutable lists
            var listComparer = new ValueComparer<List<string>>(
                (l1, l2) => l1 != null && l2 != null && l1.SequenceEqual(l2),
                l => l == null ? 0 : l.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                l => l == null ? new List<string>() : l.ToList()
            );

            var colorsProperty = entity.Property(e => e.Colors)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                .HasMaxLength(500);
            colorsProperty.Metadata.SetValueComparer(listComparer);

            // Configure Groups as CSV similar to Colors
            var groupsProperty = entity.Property(e => e.Groups)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                .HasMaxLength(200);
            groupsProperty.Metadata.SetValueComparer(listComparer);

            // Materials as CSV list
            var materialsProperty = entity.Property(e => e.Materials)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                .HasMaxLength(500);
            materialsProperty.Metadata.SetValueComparer(listComparer);

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
