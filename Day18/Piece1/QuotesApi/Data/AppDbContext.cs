using Microsoft.EntityFrameworkCore;
using QuotesApi.Models;

namespace QuotesApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.FamilyId);

        modelBuilder.Entity<Quote>()
            .HasOne(q => q.Owner)
            .WithMany()
            .HasForeignKey(q => q.OwnerId)
            .IsRequired(false);

        // Map many-to-many to the existing junction tables created by the raw-SQL migration.
        // FK column names must match the schema: QuoteId/TagId, QuoteId/CategoryId.
        modelBuilder.Entity<Quote>()
            .HasMany(q => q.Tags)
            .WithMany(t => t.Quotes)
            .UsingEntity(
                "QuoteTag",
                l => l.HasOne(typeof(Tag)).WithMany().HasForeignKey("TagId").HasPrincipalKey(nameof(Tag.Id)),
                r => r.HasOne(typeof(Quote)).WithMany().HasForeignKey("QuoteId").HasPrincipalKey(nameof(Quote.Id)),
                j => j.ToTable("QuoteTags").HasKey("QuoteId", "TagId"));

        modelBuilder.Entity<Quote>()
            .HasMany(q => q.Categories)
            .WithMany(c => c.Quotes)
            .UsingEntity(
                "QuoteCategory",
                l => l.HasOne(typeof(Category)).WithMany().HasForeignKey("CategoryId").HasPrincipalKey(nameof(Category.Id)),
                r => r.HasOne(typeof(Quote)).WithMany().HasForeignKey("QuoteId").HasPrincipalKey(nameof(Quote.Id)),
                j => j.ToTable("QuoteCategories").HasKey("QuoteId", "CategoryId"));
    }
}
