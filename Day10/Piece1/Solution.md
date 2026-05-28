## Query Variants

### With Tracking

**`EFCoreDemo/Benchmarks/ReadBenchmark.cs`**
```csharp
[Benchmark(Baseline = true)]
public int WithTracking()
{
    using var ctx = AppDbContext.Create();
    return ctx.Products.ToList().Count;
}
```

### Without Tracking

**`EFCoreDemo/Benchmarks/ReadBenchmark.cs`**
```csharp
[Benchmark]
public int WithoutTracking()
{
    using var ctx = AppDbContext.Create();
    return ctx.Products.AsNoTracking().ToList().Count;
}
```

---

## Benchmark Results

**`EFCoreDemo/BenchmarkDotNet.Artifacts/results/ReadBenchmark-report.json`**
```json
{
  "Title": "ReadBenchmark-20260528-114248",
  "HostEnvironmentInfo": {
    "BenchmarkDotNetVersion": "0.15.8",
    "OsVersion": "Windows 11 (10.0.26200.8457/25H2)",
    "ProcessorName": "Intel Core i5-9300H CPU 2.40GHz",
    "PhysicalCoreCount": 4,
    "LogicalCoreCount": 8,
    "RuntimeVersion": ".NET 10.0.8 (10.0.8, 10.0.826.23019)",
    "Architecture": "X64",
    "Configuration": "RELEASE"
  },
  "Benchmarks": [
    {
      "Method": "WithTracking",
      "Statistics": {
        "N": 5,
        "Min": 21464162.5,
        "Mean": 22066313.75,
        "Max": 22471790.625,
        "StandardDeviation": 542793.89,
        "ConfidenceInterval": { "Lower": 19976207.75, "Upper": 24156419.75 }
      },
      "Memory": {
        "Gen0Collections": 49,
        "Gen1Collections": 30,
        "Gen2Collections": 14,
        "BytesAllocatedPerOperation": 8883015
      }
    },
    {
      "Method": "WithoutTracking",
      "Statistics": {
        "N": 5,
        "Min": 9166862.5,
        "Mean": 9662540.625,
        "Max": 9955318.75,
        "StandardDeviation": 312451.61,
        "ConfidenceInterval": { "Lower": 8459400.75, "Upper": 10865680.50 }
      },
      "Memory": {
        "Gen0Collections": 34,
        "Gen1Collections": 14,
        "Gen2Collections": 5,
        "BytesAllocatedPerOperation": 3236591
      }
    }
  ]
}
```

---

## Timing and Allocation Difference

| Method                     | Time (ms) | Memory (KB) |
|----------------------------|----------:|------------:|
| without `AsNoTracking()`   | 22.1      | 8,676       |
| with `AsNoTracking()`      | 9.7       | 3,161       |
| **Delta**                  | **12.4**  | **5,515**   |

- `AsNoTracking()` is **2.3× faster** and allocates **2.7× less memory** on a 10 000-row read.
- For every tracked row EF Core allocates an `EntityEntry<T>`, clones a full property-value snapshot, and registers the entity in the identity map. Across 10 000 rows those allocations accumulate into Gen1/Gen2 GC pressure.
- `AsNoTracking()` materialises the POCO and stops there, so the allocations stay small and GC stays in Gen0.

---

## Identity Resolution

### With Tracking

**`EFCoreDemo/Demos/IdentityResolutionDemo.cs`**
```csharp
var a = ctx.Products.First(p => p.Id == 1);
var b = ctx.Products.First(p => p.Id == 1);
ReferenceEquals(a, b);   // true
a.Name = "MUTATED";
Console.WriteLine(b.Name); // "MUTATED"
```

- Two queries for the same PK return the **same object reference**, thus `ReferenceEquals` is **true**
- Mutating the entity through one variable is immediately visible through any other variable holding the same row

### Without Tracking

**`EFCoreDemo/Demos/IdentityResolutionDemo.cs`**
```csharp
var c = ctx.Products.AsNoTracking().First(p => p.Id == 2);
var d = ctx.Products.AsNoTracking().First(p => p.Id == 2);
ReferenceEquals(c, d);   // false
c.Name = "MUTATED";
Console.WriteLine(d.Name); // unchanged
```

- Two queries for the same PK return **different object references**, thus `ReferenceEquals` is **false**
- Mutating one instance has no effect on the other and each variable holds an independent copy, so the value stays **unchanged**

---

## When NOT to use AsNoTracking

Do not use `AsNoTracking()` when the same PK may be loaded more than once in a unit of work, since without the identity map each load gives a separate instance and mutations on one will not be visible through the other.