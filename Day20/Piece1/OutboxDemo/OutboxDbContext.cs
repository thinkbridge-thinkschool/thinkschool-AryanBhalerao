using Microsoft.EntityFrameworkCore;

namespace OutboxDemo;

/// One database holding BOTH the domain table and the outbox table. That is the
/// point: because they live in the same database, a single transaction can cover
/// the domain change and the outbox insert atomically — something you cannot do
/// across a database and a separate message broker.
public class OutboxDbContext : DbContext
{
    public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options) { }

    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<OutboxMessage>().HasKey(m => m.Id);

        // The relay's hot query is "give me the unpublished rows, oldest first".
        // Index ProcessedAt so that scan stays cheap as the table grows.
        b.Entity<OutboxMessage>().HasIndex(m => m.ProcessedAt);
    }
}
