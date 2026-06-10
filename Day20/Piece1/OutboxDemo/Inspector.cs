using Microsoft.EntityFrameworkCore;

namespace OutboxDemo;

/// Dumps the outbox table so the crash proof is visible at the DB level:
/// after a crash the row is still UNSENT; after the relay re-runs it is SENT.
public static class Inspector
{
    public static async Task RunAsync()
    {
        await using var db = Db.Open();
        // Sorted client-side: SQLite can't ORDER BY a DateTimeOffset.
        var rows = (await db.OutboxMessages.ToListAsync()).OrderBy(m => m.OccurredAt).ToList();

        Console.WriteLine($"OutboxMessages: {rows.Count} row(s)");
        Console.WriteLine($"{"Id",-38} {"Type",-14} {"Attempts",-9} {"State",-7} ProcessedAt");
        Console.WriteLine(new string('-', 100));
        foreach (var m in rows)
        {
            var state = m.ProcessedAt is null ? "UNSENT" : "SENT";
            var processed = m.ProcessedAt?.ToString("HH:mm:ss") ?? "(null)";
            Console.WriteLine($"{m.Id,-38} {m.Type,-14} {m.Attempts,-9} {state,-7} {processed}");
        }
    }
}
