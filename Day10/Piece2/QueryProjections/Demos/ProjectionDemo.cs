using Microsoft.EntityFrameworkCore;
using QueryProjections.Models;

namespace QueryProjections.Demos;

public static class ProjectionDemo
{
    public static void Run(AppDbContext ctx)
    {
        Console.WriteLine(new string('─', 60));
        Console.WriteLine("1. FULL ENTITY — all 7 columns fetched");
        Console.WriteLine(new string('─', 60));

        var fullEntities = ctx.Products
            .Where(p => p.Price < 100m)
            .ToList();

        Console.WriteLine($"\n→ Materialised {fullEntities.Count} Product objects (all 7 cols)\n");

        // ─────────────────────────────────────────────────────────
        Console.WriteLine(new string('─', 60));
        Console.WriteLine("2. PROJECTED — only Id, Name, Price fetched");
        Console.WriteLine(new string('─', 60));

        var projected = ctx.Products
            .Where(p => p.Price < 100m)
            .Select(p => new ProductSummaryDto(p.Id, p.Name, p.Price))
            .ToList();

        Console.WriteLine($"\n→ Materialised {projected.Count} ProductSummaryDto objects (3 cols)\n");

        // ─────────────────────────────────────────────────────────
        Console.WriteLine(new string('─', 60));
        Console.WriteLine("3. CLIENT-SIDE EVAL (the bug) — early .ToList()");
        Console.WriteLine(new string('─', 60));

        // .ToList() fires immediately → EF sends SELECT * with no WHERE.
        // The .Where() and .Select() then run in C# on all 10,000 rows.
        var clientEval = ctx.Products
            .ToList()                               // ← full table scan, no WHERE
            .Where(p => p.Price < 100m)             // C#-side filter
            .Select(p => new ProductSummaryDto(p.Id, p.Name, p.Price))
            .ToList();

        Console.WriteLine($"\n→ Got {clientEval.Count} items, but 10,000 rows crossed the wire\n");

        // ─────────────────────────────────────────────────────────
        Console.WriteLine(new string('─', 60));
        Console.WriteLine("4. FIX — WHERE + projection stay in the LINQ provider");
        Console.WriteLine(new string('─', 60));

        var fixed_ = ctx.Products
            .Where(p => p.Price < 100m)             // translated to SQL WHERE
            .Select(p => new ProductSummaryDto(p.Id, p.Name, p.Price))
            .ToList();

        Console.WriteLine($"\n→ Got {fixed_.Count} items, only matching rows + 3 cols sent\n");
    }
}
