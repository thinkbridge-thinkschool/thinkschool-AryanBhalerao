using QueryProjections;
using QueryProjections.Demos;

// ── Seed (no logging — keep output clean) ────────────────────────────────────
using (var ctx = AppDbContext.Create())
    AppDbContext.SeedIfEmpty(ctx);

// ── Demo (SQL logging active) ─────────────────────────────────────────────────
using var logged = AppDbContext.CreateWithLogging();
ProjectionDemo.Run(logged);
