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

        // Configure FK relationship but do NOT call HasIndex on AuthorId here.
        // EF Core's ForeignKeyIndexConvention will still add IX_Quotes_AuthorId;
        // Program.cs drops it after EnsureCreated() to demonstrate a missing-index bug.
        modelBuilder.Entity<Quote>()
            .HasOne<Author>()
            .WithMany()
            .HasForeignKey(q => q.AuthorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
