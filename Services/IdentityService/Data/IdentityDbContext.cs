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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.SimpleAddress).HasMaxLength(300);
        });

        // Inbox of the message bus (OrderPlaced saves the delivery address) and outbox for what it publishes
        builder.AddStoreMessagingTables();
    }
}
