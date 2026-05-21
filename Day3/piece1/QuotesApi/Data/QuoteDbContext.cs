using Microsoft.EntityFrameworkCore;
using QuotesApi.Models;

namespace QuotesApi.Data;

public class QuoteDbContext : DbContext
{
    public QuoteDbContext(DbContextOptions<QuoteDbContext> options) : base(options) { }

    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quote>(b =>
        {
            b.HasKey(q => q.Id);
            b.Property(q => q.Author).HasMaxLength(200).IsRequired();
            b.Property(q => q.Text).HasMaxLength(1000).IsRequired();
            b.Property(q => q.IsDeleted).HasDefaultValue(false);
            b.Property(q => q.CreatedAt);
            b.HasQueryFilter(q => !q.IsDeleted);
        });

        modelBuilder.Entity<Collection>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).HasMaxLength(80).IsRequired();
            b.Property(c => c.OwnerId).HasMaxLength(256).IsRequired();

            b.OwnsMany(c => c.Items, item =>
            {
                item.WithOwner().HasForeignKey("CollectionId");
                item.HasKey("CollectionId", nameof(CollectionItem.QuoteId));
                item.Property(i => i.QuoteId);
                item.Property(i => i.AddedAt);
                item.ToTable("CollectionItems");
            });

            b.Navigation(c => c.Items).HasField("_items");
        });

        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).HasMaxLength(256).IsRequired();
            b.Property(u => u.PasswordHash).IsRequired();
            b.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.Token).IsRequired();
            b.Property(r => r.Family).IsRequired();
            b.HasIndex(r => r.Token).IsUnique();
            b.HasIndex(r => r.Family);
            b.HasOne(r => r.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
