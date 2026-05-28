## Query Variants

**Tracked** — EF Core snapshots every entity; changes detectable by `SaveChanges` 

BenchmarkEndpoints.cs:
```csharp
List<Quote> allTracked = await db.Quotes.ToListAsync();
```

**AsNoTracking** — EF Core skips snapshotting; read-only, no identity map 

BenchmarkEndpoints.cs:
```csharp
List<Quote> allUntracked = await db.Quotes.AsNoTracking().ToListAsync();
```

---

## Benchmark Results (10,000 rows, SQLite, localhost)

Raw response from `GET /benchmark/change-tracker`:
```json
{
  "rows": 10000,
  "tracked":     { "elapsedMs": 92,  "allocatedKb": 14545, "changeTrackerEntries": 10000, "identityResolution": true  },
  "asNoTracking":{ "elapsedMs": 31,  "allocatedKb": 7862,  "changeTrackerEntries": 0,     "identityResolution": false }
}
```

| | Elapsed | Allocated | CT entries |
|---|---|---|---|
| Tracked | 92 ms | 14,545 KB | 10,000 |
| AsNoTracking | 31 ms | 7,862 KB | 0 |
| **Delta** | **+61 ms** | **+6,683 KB** | — |

Tracked is ~3× slower and allocates ~1.85× more than AsNoTracking.

The extra allocation comes from EF Core's per-entity snapshot: for each tracked entity it stores the original column values so `SaveChanges` can compute an UPDATE diff. With 10k entities that overhead accumulates to ~6.7 MB.

---

## Identity Resolution

| | Code | Same instance? |
|---|---|---|
| Tracked | `db.Quotes.FirstAsync(q => q.Id == id)` called after the entity is already in the identity map | **true** — EF Core returns the cached reference instead of materialising a new object |
| AsNoTracking | `db.Quotes.AsNoTracking().FirstAsync(q => q.Id == id)` called twice | **false** — no identity map, two separate heap objects |

---

## When NOT to use AsNoTracking

Do **not** use `AsNoTracking` on any read that feeds into a write path — if you load an entity to modify it and then call `SaveChanges`, the change tracker must own the instance or EF Core won't generate an `UPDATE`.
