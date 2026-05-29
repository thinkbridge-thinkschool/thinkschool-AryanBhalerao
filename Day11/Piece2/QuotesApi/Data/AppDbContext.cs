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
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.FamilyId);

        modelBuilder.Entity<Quote>()
            .HasOne<Author>()
            .WithMany(a => a.Quotes)
            .HasForeignKey(q => q.AuthorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Covering index: AuthorId as key + Text/CreatedAt as included columns.
        // Eliminates key lookups for projection queries that only need these fields.
        modelBuilder.Entity<Quote>()
            .HasIndex(q => q.AuthorId)
            .HasDatabaseName("IX_Quotes_AuthorId")
            .IncludeProperties(nameof(Quote.Text), nameof(Quote.CreatedAt));
    }
}
