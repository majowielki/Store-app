using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Messaging;
using Store.OrderService.Models;
using Store.OrderService.Saga;
using Store.OrderService.Webhooks;

namespace Store.OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<OrderStatusChange> OrderStatusChanges => Set<OrderStatusChange>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();
    public DbSet<OrderState> OrderStates => Set<OrderState>();
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();

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
            entity.Property(o => o.DiscountCode).HasMaxLength(DiscountCode.MaxCodeLength);
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(o => o.CardBrand).HasMaxLength(20);
            entity.Property(o => o.CardLast4).HasMaxLength(4);
            entity.Property(o => o.CancellationReason).HasMaxLength(50);

            // The user's order history and the admin statistics window
            entity.HasIndex(o => o.UserId);
            entity.HasIndex(o => o.CreatedAt);

            entity.HasMany(o => o.Lines)
                  .WithOne(l => l.Order)
                  .HasForeignKey(l => l.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(o => o.StatusHistory)
                  .WithOne()
                  .HasForeignKey(c => c.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderStatusChange>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(c => c.ChangedBy).HasMaxLength(450);
            entity.HasIndex(c => c.OrderId);
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

        modelBuilder.Entity<DiscountCode>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Code).IsRequired().HasMaxLength(DiscountCode.MaxCodeLength);
            entity.HasIndex(c => c.Code).IsUnique();
            entity.Property(c => c.Kind).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<IdempotencyKey>(entity =>
        {
            entity.HasKey(k => k.Key);
            entity.Property(k => k.Key).HasMaxLength(128);
            entity.Property(k => k.UserId).IsRequired().HasMaxLength(450);
            entity.Property(k => k.RequestHash).IsRequired().HasMaxLength(64);
            // The cleanup job deletes by age
            entity.HasIndex(k => k.CreatedAt);
        });

        // Payment webhooks already acted on, by event id
        modelBuilder.Entity<ProcessedWebhookEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.ReceivedAt);
        });

        // The order saga: one row per order, locked while a message about the order is handled
        modelBuilder.Entity<OrderState>(entity =>
        {
            entity.ToTable("OrderStates");
            entity.HasKey(s => s.CorrelationId);
            entity.Property(s => s.CurrentState).IsRequired().HasMaxLength(64);
            entity.HasIndex(s => s.OrderId).IsUnique();
            // The deadline job looks for the orders waiting past their deadline
            entity.HasIndex(s => new { s.CurrentState, s.PaymentDueAt });
        });

        // Outbox and inbox of the message bus, in the same database as the orders
        modelBuilder.AddStoreMessagingTables();
    }
}
