using BenchmarkDotNet.Attributes;
using EFCoreDemo;
using Microsoft.EntityFrameworkCore;

[MemoryDiagnoser]
[JsonExporter]
[SimpleJob(iterationCount: 5, warmupCount: 2)]
public class ReadBenchmark
{
    // BenchmarkDotNet creates a fresh instance per iteration so we
    // need to ensure the DB is seeded before benchmarks run.
    [GlobalSetup]
    public void Setup()
    {
        using var ctx = AppDbContext.Create();
        AppDbContext.SeedIfEmpty(ctx);
    }

    // --- Variant 1: default tracked read ---
    // EF Core materialises each row, creates an EntityEntry<Product> snapshot,
    // registers it in the identity map, and keeps original-values copy.
    [Benchmark(Baseline = true)]
    public int WithTracking()
    {
        using var ctx = AppDbContext.Create();
        return ctx.Products.ToList().Count;
    }

    // --- Variant 2: AsNoTracking ---
    // EF Core materialises each row but skips snapshot + identity-map registration.
    // Fewer allocations, no object-graph bookkeeping overhead.
    [Benchmark]
    public int WithoutTracking()
    {
        using var ctx = AppDbContext.Create();
        return ctx.Products.AsNoTracking().ToList().Count;
    }
}
