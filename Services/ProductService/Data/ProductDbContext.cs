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

    public DbSet<CatalogueSeed> CatalogueSeeds => Set<CatalogueSeed>();

    public DbSet<StockOrder> StockOrders => Set<StockOrder>();

    public DbSet<StockOrderLine> StockOrderLines => Set<StockOrderLine>();

    public DbSet<StockAlert> StockAlerts => Set<StockAlert>();

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
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(ProductConstraints.SlugMaxLength);
            entity.HasIndex(e => e.Slug).IsUnique();
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

            // Held units come out of the units on hand; a bug that breaks it fails its transaction
            entity.ToTable(table => table.HasCheckConstraint("CK_Products_Stock",
                "\"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"StockQuantity\""));

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");

            // The gallery is read only with one product, so it gets its own table; a picture
            // taken out of the list is deleted with it
            entity.HasMany(e => e.Images).WithOne().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);

            // The points belong to the main picture and are always read with it: one jsonb column
            entity.OwnsMany(e => e.Hotspots, hotspot => hotspot.ToJson());
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("ProductImages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(ProductConstraints.ImageUrlMaxLength);
            entity.Property(e => e.Alt).IsRequired().HasMaxLength(ProductConstraints.ImageAltMaxLength);
            entity.HasIndex(e => new { e.ProductId, e.SortOrder });
        });

        modelBuilder.Entity<StockOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.OrderId).ValueGeneratedNever();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(ProductConstraints.EnumMaxLength);
            entity.HasMany(e => e.Lines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockOrderLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OrderId, e.ProductId }).IsUnique();
            entity.HasOne<Product>().WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(ProductConstraints.EmailMaxLength);
            // One waiting alert per address and product; asking twice changes nothing
            entity.HasIndex(e => new { e.ProductId, e.Email }).IsUnique();
            entity.HasOne<Product>().WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogueSeed>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        // Outbox of the message bus: catalogue changes leave as audit events
        modelBuilder.AddStoreMessagingTables();
    }
}
