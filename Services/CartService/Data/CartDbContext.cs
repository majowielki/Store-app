using Microsoft.EntityFrameworkCore;
using Store.CartService.Models;

namespace Store.CartService.Data;

public class CartDbContext : DbContext
{
    public CartDbContext(DbContextOptions<CartDbContext> options) : base(options)
    {
    }

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(c => c.UserId).IsUnique();

            entity.HasMany(c => c.Items)
                  .WithOne(ci => ci.Cart)
                  .HasForeignKey(ci => ci.CartId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // The product columns are a snapshot taken from the catalogue - there is no Products
        // table in this database and no foreign key to another service's data
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(ci => ci.Id);
            entity.Property(ci => ci.Title).IsRequired().HasMaxLength(200);
            entity.Property(ci => ci.Image).IsRequired();
            entity.Property(ci => ci.Company).IsRequired().HasMaxLength(100);
            entity.Property(ci => ci.Color).IsRequired().HasMaxLength(50);
            entity.HasIndex(ci => new { ci.CartId, ci.ProductId, ci.Color }).IsUnique();
        });
    }
}
