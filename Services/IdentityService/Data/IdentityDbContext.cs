using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Messaging;
using Store.IdentityService.Models;

namespace Store.IdentityService.Data;

public class IdentityDbContext : IdentityDbContext<ApplicationUser>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(ApplicationUser.NameMaxLength);
            entity.Property(e => e.LastName).HasMaxLength(ApplicationUser.NameMaxLength);
            entity.Property(e => e.SimpleAddress).HasMaxLength(ApplicationUser.AddressMaxLength);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ReplacedByHash).HasMaxLength(64);
            entity.Property(e => e.CreatedByIp).HasMaxLength(45);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.FamilyId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Inbox of the message bus (OrderPlaced saves the delivery address) and outbox for what it publishes
        builder.AddStoreMessagingTables();
    }
}
