using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.Contracts.Authorization;
using Store.Contracts.Payments;
using Store.Contracts.Payments.V1;
using Store.Contracts.Payments.Webhooks;
using Store.PaymentService.Models;

namespace Store.PaymentService.Data;

public class PaymentDbContext : DbContext
{
    /// <summary>The longest name of an enum stored by name (a status, an outcome).</summary>
    private const int EnumMaxLength = 30;

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
            entity.Property(p => p.UserId).IsRequired().HasMaxLength(UserIds.MaxLength);
            entity.Property(p => p.Currency).IsRequired().HasMaxLength(Currencies.CodeLength);
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);
            entity.Property(p => p.IdempotencyKey).IsRequired().HasMaxLength(IdempotencyKeyHeader.MaxLength);
            entity.Property(p => p.CardBrand).HasMaxLength(CardBrands.MaxLength);
            entity.Property(p => p.CardLast4).HasMaxLength(CardLast4.Length);
            entity.Property(p => p.DeclineReason).HasMaxLength(PaymentDeclineReasons.MaxLength);
            entity.HasMany(p => p.Attempts).WithOne().HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentAttempt>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.CardBrand).IsRequired().HasMaxLength(CardBrands.MaxLength);
            entity.Property(a => a.CardLast4).IsRequired().HasMaxLength(CardLast4.Length);
            entity.Property(a => a.Outcome).HasConversion<string>().HasMaxLength(EnumMaxLength);
            entity.HasIndex(a => a.PaymentId);
        });

        modelBuilder.Entity<WebhookDelivery>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Type).IsRequired().HasMaxLength(PaymentWebhookTypes.MaxLength);
            entity.Property(d => d.Payload).IsRequired().HasColumnType("text");
            entity.Property(d => d.LastError).HasMaxLength(WebhookDelivery.LastErrorMaxLength);
            entity.HasIndex(d => d.PaymentId);
            // The dispatcher looks for what is due and not settled yet
            entity.HasIndex(d => d.NextAttemptAt).HasFilter($"\"{nameof(WebhookDelivery.DeliveredAt)}\" IS NULL AND \"{nameof(WebhookDelivery.FailedAt)}\" IS NULL");
        });

        // Outbox and inbox of the message bus: the order events arrive here, audit events leave
        modelBuilder.AddStoreMessagingTables();
    }
}
