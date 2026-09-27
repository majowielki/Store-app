using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Store.BuildingBlocks.Messaging;
using Store.ContentService.Models;

namespace Store.ContentService.Data;

/// <summary>
/// The shop's editorial content: makers, collections, journal articles and lookbooks. They refer
/// to catalogue products by slug but hold no copy of them; the shop resolves the products it shows.
/// </summary>
public class ContentDbContext : DbContext
{
    public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options)
    {
    }

    public DbSet<Maker> Makers => Set<Maker>();

    public DbSet<Collection> Collections => Set<Collection>();

    public DbSet<Article> Articles => Set<Article>();

    public DbSet<Lookbook> Lookbooks => Set<Lookbook>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Maker>(entity =>
        {
            MapEntry(entity);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(ContentConstraints.TitleMaxLength);
            // By name, as the catalogue stores it
            entity.Property(e => e.Company).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(e => e.Company).IsUnique();
            entity.Property(e => e.Tagline).HasMaxLength(ContentConstraints.LineMaxLength);
            entity.Property(e => e.Story).HasMaxLength(ContentConstraints.MarkdownMaxLength);
            entity.Property(e => e.Location).HasMaxLength(ContentConstraints.LineMaxLength);
            entity.Property(e => e.CoverImage).HasMaxLength(ContentConstraints.ImageMaxLength);
        });

        modelBuilder.Entity<Collection>(entity =>
        {
            MapEntry(entity);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(ContentConstraints.TitleMaxLength);
            entity.Property(e => e.Summary).HasMaxLength(ContentConstraints.ShortTextMaxLength);
            entity.Property(e => e.Body).HasMaxLength(ContentConstraints.MarkdownMaxLength);
            entity.Property(e => e.CoverImage).HasMaxLength(ContentConstraints.ImageMaxLength);
            entity.Property(e => e.ProductSlugs).HasColumnType("text[]");
        });

        modelBuilder.Entity<Article>(entity =>
        {
            MapEntry(entity);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(ContentConstraints.TitleMaxLength);
            entity.Property(e => e.Excerpt).HasMaxLength(ContentConstraints.ShortTextMaxLength);
            entity.Property(e => e.Body).HasMaxLength(ContentConstraints.MarkdownMaxLength);
            entity.Property(e => e.CoverImage).HasMaxLength(ContentConstraints.ImageMaxLength);
            entity.Property(e => e.Author).HasMaxLength(ContentConstraints.LineMaxLength);
            entity.Property(e => e.ProductSlugs).HasColumnType("text[]");
            entity.HasIndex(e => e.PublishedAt);
        });

        modelBuilder.Entity<Lookbook>(entity =>
        {
            MapEntry(entity);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(ContentConstraints.TitleMaxLength);
            entity.Property(e => e.Summary).HasMaxLength(ContentConstraints.ShortTextMaxLength);
            entity.Property(e => e.Image).HasMaxLength(ContentConstraints.ImageMaxLength);
            // The points belong to the picture and are always read with it: one jsonb column
            entity.OwnsMany(e => e.Hotspots, hotspot => hotspot.ToJson());
        });

        // Outbox of the message bus: content changes leave as audit events
        modelBuilder.AddStoreMessagingTables();
    }

    private static void MapEntry<TEntry>(EntityTypeBuilder<TEntry> entity) where TEntry : ContentEntry
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Slug).IsRequired().HasMaxLength(ContentConstraints.SlugMaxLength);
        entity.HasIndex(e => e.Slug).IsUnique();
        entity.HasIndex(e => e.IsPublished);
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
    }
}
