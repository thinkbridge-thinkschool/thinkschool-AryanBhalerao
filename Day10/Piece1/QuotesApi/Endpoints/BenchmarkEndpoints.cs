using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;
using QuotesApi.Models;

namespace QuotesApi.Endpoints;

public static class BenchmarkEndpoints
{
    private const int TargetRows = 10_000;

    public static IEndpointRouteBuilder MapBenchmarkEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/benchmark/change-tracker", async (AppDbContext db) =>
        {
            await EnsureSeededAsync(db);

            // ── 1. Tracked read ───────────────────────────────────────────────
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            long allocBefore = GC.GetAllocatedBytesForCurrentThread();
            var  sw          = Stopwatch.StartNew();

            List<Quote> allTracked = await db.Quotes.ToListAsync();

            sw.Stop();
            long trackedAllocKb = (GC.GetAllocatedBytesForCurrentThread() - allocBefore) / 1024;
            long trackedMs      = sw.ElapsedMilliseconds;
            int  ctEntries      = db.ChangeTracker.Entries().Count();

            // Identity resolution: same key queried again → identity map returns existing instance
            int  seedId         = allTracked.OrderBy(q => q.Id).First().Id;
            var  reloadTracked  = await db.Quotes.FirstAsync(q => q.Id == seedId);
            bool trackedSameRef = ReferenceEquals(allTracked.First(q => q.Id == seedId), reloadTracked);

            db.ChangeTracker.Clear();

            // ── 2. AsNoTracking read ──────────────────────────────────────────
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            allocBefore = GC.GetAllocatedBytesForCurrentThread();
            sw.Restart();

            List<Quote> allUntracked = await db.Quotes.AsNoTracking().ToListAsync();

            sw.Stop();
            long untrackedAllocKb = (GC.GetAllocatedBytesForCurrentThread() - allocBefore) / 1024;
            long untrackedMs      = sw.ElapsedMilliseconds;
            int  ctEntriesAfter   = db.ChangeTracker.Entries().Count();

            // Identity resolution: no identity map → always a new object
            var  reloadUntracked    = await db.Quotes.AsNoTracking().FirstAsync(q => q.Id == seedId);
            bool untrackedSameRef   = ReferenceEquals(allUntracked.First(q => q.Id == seedId), reloadUntracked);

            return Results.Ok(new
            {
                rows = TargetRows,
                tracked = new
                {
                    elapsedMs            = trackedMs,
                    allocatedKb          = trackedAllocKb,
                    changeTrackerEntries = ctEntries,
                    identityResolution   = trackedSameRef
                },
                asNoTracking = new
                {
                    elapsedMs            = untrackedMs,
                    allocatedKb          = untrackedAllocKb,
                    changeTrackerEntries = ctEntriesAfter,
                    identityResolution   = untrackedSameRef
                }
            });
        });

        return app;
    }

    private static async Task EnsureSeededAsync(AppDbContext db)
    {
        int existing = await db.Quotes.AsNoTracking().CountAsync();
        if (existing >= TargetRows) return;

        int needed = TargetRows - existing;
        var batch = Enumerable.Range(existing + 1, needed).Select(i => new Quote
        {
            Author    = $"Author_{i}",
            Text      = $"Benchmark quote {i}. The quick brown fox jumps over the lazy dog.",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.AddRangeAsync(batch);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }
}
