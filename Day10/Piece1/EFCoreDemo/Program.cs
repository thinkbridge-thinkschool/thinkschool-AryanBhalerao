using BenchmarkDotNet.Running;
using EFCoreDemo;
using EFCoreDemo.Demos;

// ── Seed ─────────────────────────────────────────────────────────────────────
using (var ctx = AppDbContext.Create())
    AppDbContext.SeedIfEmpty(ctx);

// ── Demos ─────────────────────────────────────────────────────────────────────
using (var ctx = AppDbContext.Create())
    IdentityResolutionDemo.Run(ctx);

using (var ctx = AppDbContext.Create())
    TrackingStateDemo.Run(ctx);

// ── Benchmarks ────────────────────────────────────────────────────────────────
// BenchmarkDotNet REQUIRES a Release build; it refuses to run under Debug.
// Run with: dotnet run -c Release -- --benchmark
if (args.Contains("--benchmark"))
{
    BenchmarkRunner.Run<ReadBenchmark>();
    return;
}

Console.WriteLine("""

─────────────────────────────────────────────────────────────
 Run  dotnet run -c Release -- --benchmark  for full numbers.
─────────────────────────────────────────────────────────────
""");
