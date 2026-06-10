using Microsoft.EntityFrameworkCore;

namespace OutboxDemo;

/// Builds an OutboxDbContext over a SQLite file that lives next to the binary,
/// so every subcommand (produce / relay / outbox) shares one database.
public static class Db
{
    public static string Path => System.IO.Path.Combine(AppContext.BaseDirectory, "outbox.db");

    public static OutboxDbContext Open()
    {
        var options = new DbContextOptionsBuilder<OutboxDbContext>()
            .UseSqlite($"Data Source={Path}")
            .Options;
        var db = new OutboxDbContext(options);
        db.Database.EnsureCreated();   // first run materialises Quotes + OutboxMessages
        return db;
    }
}
