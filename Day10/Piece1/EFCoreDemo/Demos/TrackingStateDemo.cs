using Microsoft.EntityFrameworkCore;

namespace EFCoreDemo.Demos;

public static class TrackingStateDemo
{
    public static void Run(AppDbContext ctx)
    {
        Console.WriteLine("\n=== Tracking State ===");

        // --- How many entries does the tracker hold right now?
        Console.WriteLine($"Tracker entries before query : {ctx.ChangeTracker.Entries().Count()}");

        var products = ctx.Products.Take(5).ToList();

        Console.WriteLine($"Tracker entries after Take(5): {ctx.ChangeTracker.Entries().Count()}");

        foreach (var entry in ctx.ChangeTracker.Entries())
            Console.WriteLine($"  [{entry.State}] {entry.Entity.GetType().Name} Id={((dynamic)entry.Entity).Id}");

        // Modify one entity in memory.
        products[0].Price = 9999m;

        // The tracker detects the change automatically (snapshot comparison).
        Console.WriteLine("\nAfter mutating products[0].Price:");
        foreach (var entry in ctx.ChangeTracker.Entries())
            Console.WriteLine($"  [{entry.State}] Id={((dynamic)entry.Entity).Id}  Price={((dynamic)entry.Entity).Price}");

        // SaveChanges would only UPDATE the one Modified row.
        // We skip the actual save to keep the demo db clean.
        Console.WriteLine("  (SaveChanges skipped – demo only)");

        // --- Untracked: zero entries added to the tracker.
        ctx.ChangeTracker.Clear();
        _ = ctx.Products.AsNoTracking().Take(5).ToList();
        Console.WriteLine($"\nAfter AsNoTracking Take(5) tracker entries: {ctx.ChangeTracker.Entries().Count()}");
    }
}
