using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Messaging;
using Store.PaymentService.Models;

namespace Store.PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.OrderId).IsUnique();
            entity.HasIndex(p => p.IdempotencyKey).IsUnique();
            entity.Property(p => p.UserId).IsRequired().HasMaxLength(450);
            entity.Property(p => p.Currency).IsRequired().HasMaxLength(3);
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(p => p.IdempotencyKey).IsRequired().HasMaxLength(128);
            entity.Property(p => p.CardBrand).HasMaxLength(20);
            entity.Property(p => p.CardLast4).HasMaxLength(4);
            entity.Property(p => p.DeclineReason).HasMaxLength(50);
            entity.HasMany(p => p.Attempts).WithOne().HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentAttempt>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.CardBrand).IsRequired().HasMaxLength(20);
            entity.Property(a => a.CardLast4).IsRequired().HasMaxLength(4);
            entity.Property(a => a.Outcome).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(a => a.PaymentId);
        });

        modelBuilder.Entity<WebhookDelivery>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Type).IsRequired().HasMaxLength(50);
            entity.Property(d => d.Payload).IsRequired().HasColumnType("text");
            entity.Property(d => d.LastError).HasMaxLength(500);
            entity.HasIndex(d => d.PaymentId);
            // The dispatcher looks for what is due and not settled yet
            entity.HasIndex(d => d.NextAttemptAt).HasFilter("\"DeliveredAt\" IS NULL AND \"FailedAt\" IS NULL");
        });

        // Outbox and inbox of the message bus: the order events arrive here, audit events leave
        modelBuilder.AddStoreMessagingTables();
    }
}
