using Microsoft.EntityFrameworkCore;
using Store.OrderService.Models;

namespace Store.OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.UserId).IsRequired().HasMaxLength(450);
            entity.Property(o => o.UserEmail).IsRequired().HasMaxLength(256);
            entity.Property(o => o.CustomerName).IsRequired().HasMaxLength(100);
            entity.Property(o => o.DeliveryAddress).HasMaxLength(300);
            entity.Property(o => o.Notes).HasMaxLength(500);
            entity.Property(o => o.DiscountReason).HasMaxLength(50);
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

            // The user's order history and the admin statistics window
            entity.HasIndex(o => o.UserId);
            entity.HasIndex(o => o.CreatedAt);

            entity.HasMany(o => o.Lines)
                  .WithOne(l => l.Order)
                  .HasForeignKey(l => l.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.ProductTitle).IsRequired().HasMaxLength(200);
            entity.Property(l => l.ProductImage).IsRequired();
            entity.Property(l => l.Company).IsRequired().HasMaxLength(100);
            entity.Property(l => l.Color).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.UserId);
            entity.Property(c => c.UserId).HasMaxLength(450);
        });
    }
}
