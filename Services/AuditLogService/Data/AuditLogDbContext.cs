using Microsoft.EntityFrameworkCore;
using Store.AuditLogService.Models;
using Store.BuildingBlocks.Messaging;

namespace Store.AuditLogService.Data;

public class AuditLogDbContext : DbContext
{
    public AuditLogDbContext(DbContextOptions<AuditLogDbContext> options) : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ShopEvent> ShopEvents => Set<ShopEvent>();
    public DbSet<OrderReceipt> OrderReceipts => Set<OrderReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Action).IsRequired().HasMaxLength(AuditLogConstraints.ActionMaxLength);
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(AuditLogConstraints.EntityNameMaxLength);
            entity.Property(e => e.EntityId).HasMaxLength(AuditLogConstraints.EntityIdMaxLength);
            entity.Property(e => e.UserId).HasMaxLength(AuditLogConstraints.UserIdMaxLength);
            entity.Property(e => e.ServiceName).HasMaxLength(AuditLogConstraints.ServiceNameMaxLength);
            entity.Property(e => e.CorrelationId).HasMaxLength(AuditLogConstraints.CorrelationIdMaxLength);

            // The queries the admin API offers, plus Timestamp for the retention job
            entity.HasIndex(e => e.EntityName);
            entity.HasIndex(e => e.EntityId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.EntityName, e.EntityId });
        });

        // The purchase funnel counts over a window of time and the retention job deletes by it
        modelBuilder.Entity<ShopEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Kind).HasConversion<string>().HasMaxLength(ShopEvent.KindMaxLength);
            entity.HasIndex(e => e.OccurredAt);
        });

        // Inbox of the message bus: audit entries arrive as events
        modelBuilder.Entity<OrderReceipt>().HasKey(o => o.OrderId);
        modelBuilder.Entity<OrderReceipt>().Property(o => o.OrderId).ValueGeneratedNever();
        modelBuilder.AddStoreMessagingTables();
    }
}
