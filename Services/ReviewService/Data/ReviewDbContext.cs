using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Messaging;
using Store.ReviewService.Models;
using Store.Contracts.Authorization;

namespace Store.ReviewService.Data;

public class ReviewDbContext : DbContext
{
    public ReviewDbContext(DbContextOptions<ReviewDbContext> options) : base(options)
    {
    }

    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<ReviewReport> ReviewReports => Set<ReviewReport>();
    public DbSet<ReviewSeed> ReviewSeeds => Set<ReviewSeed>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.ProductSlug).HasMaxLength(ReviewConstraints.SlugMaxLength);
            entity.Property(r => r.UserId).HasMaxLength(UserIds.MaxLength);
            entity.Property(r => r.AuthorName).IsRequired().HasMaxLength(ReviewConstraints.AuthorNameMaxLength);
            entity.Property(r => r.Title).HasMaxLength(ReviewConstraints.TitleMaxLength);
            entity.Property(r => r.Body).IsRequired().HasMaxLength(ReviewConstraints.BodyMaxLength);
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.Source).HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.RejectionReason).HasMaxLength(ReviewConstraints.ReasonMaxLength);
            entity.Property(r => r.ModeratedBy).HasMaxLength(UserIds.MaxLength);
            entity.ToTable(table => table.HasCheckConstraint("CK_Reviews_Rating", "\"Rating\" BETWEEN 1 AND 5"));

            // A product's page and its summary read the published reviews of one product
            entity.HasIndex(r => new { r.ProductId, r.Status });
            // The moderation queue, oldest first
            entity.HasIndex(r => new { r.Status, r.SubmittedAt });
            // One review per product and account; a shared demo account gets one per sign-in session
            entity.HasIndex(r => new { r.ProductId, r.UserId }).IsUnique()
                .HasFilter("\"UserId\" IS NOT NULL AND \"DemoSessionId\" IS NULL")
                .HasDatabaseName("IX_Reviews_One_Per_Account");
            entity.HasIndex(r => new { r.ProductId, r.UserId, r.DemoSessionId }).IsUnique()
                .HasFilter("\"DemoSessionId\" IS NOT NULL")
                .HasDatabaseName("IX_Reviews_One_Per_Demo_Session");
            // The daily limit counts an author's reviews; the demo cleanup looks for expired ones
            entity.HasIndex(r => new { r.UserId, r.SubmittedAt });
            entity.HasIndex(r => r.ExpiresAt).HasFilter("\"ExpiresAt\" IS NOT NULL");
            // Seeded reviews still waiting for their product id
            entity.HasIndex(r => r.ProductSlug).HasFilter("\"ProductId\" IS NULL");
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(p => new { p.UserId, p.ProductId });
            entity.Property(p => p.UserId).HasMaxLength(UserIds.MaxLength);
        });

        modelBuilder.Entity<ReviewReport>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.ReporterId).IsRequired().HasMaxLength(UserIds.MaxLength);
            entity.Property(r => r.Reason).HasMaxLength(ReviewConstraints.ReasonMaxLength);
            // One report per review and account, or per sign-in session of a shared demo account
            entity.HasIndex(r => new { r.ReviewId, r.ReporterId }).IsUnique()
                .HasFilter("\"DemoSessionId\" IS NULL")
                .HasDatabaseName("IX_ReviewReports_One_Per_Account");
            entity.HasIndex(r => new { r.ReviewId, r.ReporterId, r.DemoSessionId }).IsUnique()
                .HasFilter("\"DemoSessionId\" IS NOT NULL")
                .HasDatabaseName("IX_ReviewReports_One_Per_Demo_Session");
            entity.HasIndex(r => r.ExpiresAt).HasFilter("\"ExpiresAt\" IS NOT NULL");
            entity.HasOne<Review>().WithMany().HasForeignKey(r => r.ReviewId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReviewSeed>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedNever();
        });

        // Outbox and inbox of the message bus: purchases arrive, summaries and audit entries leave
        modelBuilder.AddStoreMessagingTables();
    }
}
