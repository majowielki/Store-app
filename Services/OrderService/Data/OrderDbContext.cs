using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.Contracts.Authorization;
using Store.Contracts.Orders.V1;
using Store.Contracts.Payments;
using Store.OrderService.Models;
using Store.OrderService.Saga;

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
            entity.Property(o => o.UserId).IsRequired().HasMaxLength(UserIds.MaxLength);
            entity.Property(o => o.UserEmail).IsRequired().HasMaxLength(OrderConstraints.EmailMaxLength);
            entity.Property(o => o.CustomerName).IsRequired().HasMaxLength(OrderConstraints.CustomerNameMaxLength);
            entity.Property(o => o.DeliveryAddress).HasMaxLength(OrderConstraints.DeliveryAddressMaxLength);
            entity.Property(o => o.Notes).HasMaxLength(OrderConstraints.NotesMaxLength);
            entity.Property(o => o.DiscountReason).HasMaxLength(OrderConstraints.DiscountReasonMaxLength);
            entity.Property(o => o.DiscountCode).HasMaxLength(DiscountCode.MaxCodeLength);
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(OrderConstraints.EnumMaxLength);
            entity.Property(o => o.CardBrand).HasMaxLength(CardBrands.MaxLength);
            entity.Property(o => o.CardLast4).HasMaxLength(CardLast4.Length);
            entity.Property(o => o.CancellationReason).HasMaxLength(OrderCancellationReasons.MaxLength);

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
            entity.Property(c => c.Status).HasConversion<string>().HasMaxLength(OrderConstraints.EnumMaxLength);
            entity.Property(c => c.ChangedBy).HasMaxLength(UserIds.MaxLength);
            entity.HasIndex(c => c.OrderId);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.ProductTitle).IsRequired().HasMaxLength(OrderConstraints.ProductTitleMaxLength);
            entity.Property(l => l.ProductImage).IsRequired();
            entity.Property(l => l.Company).IsRequired().HasMaxLength(OrderConstraints.CompanyMaxLength);
            entity.Property(l => l.Color).IsRequired().HasMaxLength(OrderConstraints.ColorMaxLength);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.UserId);
            entity.Property(c => c.UserId).HasMaxLength(UserIds.MaxLength);
        });

        modelBuilder.Entity<DiscountCode>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Code).IsRequired().HasMaxLength(DiscountCode.MaxCodeLength);
            entity.HasIndex(c => c.Code).IsUnique();
            entity.Property(c => c.Kind).HasConversion<string>().HasMaxLength(OrderConstraints.EnumMaxLength);
        });

        modelBuilder.Entity<IdempotencyKey>(entity =>
        {
            entity.HasKey(k => k.Key);
            entity.Property(k => k.Key).HasMaxLength(IdempotencyKeyHeader.MaxLength);
            entity.Property(k => k.UserId).IsRequired().HasMaxLength(UserIds.MaxLength);
            entity.Property(k => k.RequestHash).IsRequired().HasMaxLength(OrderConstraints.RequestHashLength);
            // The cleanup job deletes by age
            entity.HasIndex(k => k.CreatedAt);
        });

        // Payment webhooks already acted on, by event id
        modelBuilder.Entity<ProcessedWebhookEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(ProcessedWebhookEvent.TypeMaxLength);
            entity.HasIndex(e => e.ReceivedAt);
        });

        // The order saga: one row per order, locked while a message about the order is handled
        modelBuilder.Entity<OrderState>(entity =>
        {
            entity.HasKey(s => s.CorrelationId);
            entity.Property(s => s.CurrentState).IsRequired().HasMaxLength(OrderConstraints.SagaStateMaxLength);
            entity.HasIndex(s => s.OrderId).IsUnique();
            // The deadline job looks for the orders waiting past their deadline
            entity.HasIndex(s => new { s.CurrentState, s.PaymentDueAt });
        });

        // Outbox and inbox of the message bus, in the same database as the orders
        modelBuilder.AddStoreMessagingTables();
    }
}
