using Microsoft.EntityFrameworkCore;
using Store.Shared.Models;

namespace Store.OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    // OrderService should not depend on Product table from another microservice
    // Ensure EF does not create a FK or the Product table in this context
    modelBuilder.Entity<OrderItem>().Ignore(oi => oi.Product);
    modelBuilder.Ignore<Product>();
    }
}
