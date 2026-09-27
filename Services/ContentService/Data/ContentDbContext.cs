using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Messaging;

namespace Store.ContentService.Data;

/// <summary>
/// The shop's editorial content: makers, collections, journal articles and lookbooks. They refer
/// to catalogue products but hold no copy of them; the shop resolves the products it shows.
/// </summary>
public class ContentDbContext : DbContext
{
    public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Outbox of the message bus: content changes leave as audit events
        modelBuilder.AddStoreMessagingTables();
    }
}
