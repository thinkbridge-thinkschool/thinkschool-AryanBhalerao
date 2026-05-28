using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QueryProjections.Models;

namespace QueryProjections;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    private const string ConnectionString =
        @"Server=.\SQLEXPRESS;Database=QueryProjectionsDemo;Trusted_Connection=True;TrustServerCertificate=True";

    public static AppDbContext Create() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);

    // SQL-only logging: filters to Database.Command so EF infrastructure noise is suppressed.
    public static AppDbContext CreateWithLogging() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .LogTo(
                Console.WriteLine,
                new[] { DbLoggerCategory.Database.Command.Name },
                LogLevel.Information)
            .EnableSensitiveDataLogging()
            .Options);

    public static void SeedIfEmpty(AppDbContext ctx)
    {
        ctx.Database.EnsureCreated();
        if (ctx.Products.Any()) return;

        var rng = new Random(42);
        var batch = Enumerable.Range(1, 10_000).Select(i => new Product
        {
            Name        = $"Product-{i}",
            Price       = Math.Round(1m + (i % 999), 2),
            Stock       = i % 500,
            Description = $"Full description for product {i}. Contains marketing copy, specs, and legal text.",
            CreatedAt   = DateTime.UtcNow.AddDays(-(i % 365)),
            ImageUrl    = $"https://cdn.example.com/products/{i}.jpg"
        });

        ctx.Products.AddRange(batch);
        ctx.SaveChanges();
    }
}
