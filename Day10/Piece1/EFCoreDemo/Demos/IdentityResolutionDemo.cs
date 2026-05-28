using EFCoreDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace EFCoreDemo.Demos;

public static class IdentityResolutionDemo
{
    public static void Run(AppDbContext ctx)
    {
        Console.WriteLine("=== Identity Resolution ===");

        // --- Tracked queries: the change tracker acts as an identity map.
        // Two separate queries for the same PK return the SAME object reference.
        var a = ctx.Products.First(p => p.Id == 1);
        var b = ctx.Products.First(p => p.Id == 1);

        Console.WriteLine($"Tracked   – same reference? {ReferenceEquals(a, b)}");   // True
        Console.WriteLine($"  a.Name = \"{a.Name}\"  |  b.Name = \"{b.Name}\"");

        // Mutate via one handle; the other reflects the change instantly because
        // they point at the same object.
        a.Name = "MUTATED";
        Console.WriteLine($"  After a.Name = \"MUTATED\" → b.Name = \"{b.Name}\"");

        // --- Untracked queries: each materializes a NEW object.
        // No identity map, no snapshot overhead.
        var c = ctx.Products.AsNoTracking().First(p => p.Id == 2);
        var d = ctx.Products.AsNoTracking().First(p => p.Id == 2);

        Console.WriteLine($"\nUntracked – same reference? {ReferenceEquals(c, d)}");   // False
        Console.WriteLine($"  c.Name = \"{c.Name}\"  |  d.Name = \"{d.Name}\"");

        c.Name = "MUTATED";
        Console.WriteLine($"  After c.Name = \"MUTATED\" → d.Name = \"{d.Name}\" (unchanged)");
    }
}
