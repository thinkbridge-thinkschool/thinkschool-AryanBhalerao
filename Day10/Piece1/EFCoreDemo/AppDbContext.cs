using EFCoreDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace EFCoreDemo;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    private const string ConnectionString =
        @"Server=.\SQLEXPRESS;Database=EFCoreDemo;Trusted_Connection=True;TrustServerCertificate=True";

    public static AppDbContext Create()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new AppDbContext(opts);
    }

    // EnsureCreated only creates the schema; seeding is handled by DBSetup.sql.
    // This fallback seeds programmatically when the table is empty (e.g. first run
    // without running DBSetup.sql manually).
    public static void SeedIfEmpty(AppDbContext ctx)
    {
        ctx.Database.EnsureCreated();
        if (ctx.Products.Any()) return;

        var batch = Enumerable.Range(1, 10_000).Select(i => new Product
        {
            Name  = $"Product-{i}",
            Price = Math.Round(1m + (i % 999), 2),
            Stock = i % 500
        });

        ctx.Products.AddRange(batch);
        ctx.SaveChanges();
    }
}
