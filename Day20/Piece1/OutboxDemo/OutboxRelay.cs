using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;

namespace OutboxDemo;

/// The relay is a separate step from the transaction. It polls the outbox for
/// rows that have not been published, sends each to the broker, and only THEN
/// stamps ProcessedAt and commits.
///
/// The ordering is the entire guarantee:
///
///   publish to broker  ->  (CRASH WINDOW)  ->  mark sent in DB
///
///   * Crash BEFORE publish        : row still unsent  -> retried next run. No loss.
///   * Crash AFTER publish, BEFORE  : row still unsent  -> PUBLISHED AGAIN next run.
///     the mark commits               The message is therefore delivered
///                                     at-least-once; the duplicate is the
///                                     consumer's job to dedupe. No loss.
///   * Crash AFTER mark commits     : row is sent, never re-published. Done.
///
/// The forbidden ordering is the reverse — mark sent, THEN publish — because a
/// crash in its window marks a message sent that never reached the broker:
/// silent, permanent LOSS. So we always publish first and mark second.
public sealed class OutboxRelay
{
    private readonly string _conn;
    private readonly bool _crashAfterPublish;

    public OutboxRelay(string conn, bool crashAfterPublish)
    {
        _conn = conn;
        _crashAfterPublish = crashAfterPublish;
    }

    /// Drains every currently-unsent outbox row once, then returns.
    public async Task RunOnceAsync()
    {
        await using var db = Db.Open();
        await using var client = new ServiceBusClient(_conn);
        await using var sender = client.CreateSender("quotes");

        var pending = (await db.OutboxMessages
                .Where(m => m.ProcessedAt == null)     // the unpublished rows...
                .ToListAsync())
            .OrderBy(m => m.OccurredAt)                // ...oldest first (sorted client-side:
            .ToList();                                 // SQLite can't ORDER BY a DateTimeOffset)

        if (pending.Count == 0)
        {
            Console.WriteLine("relay: nothing to publish (outbox is drained).");
            return;
        }

        Console.WriteLine($"relay: {pending.Count} unsent row(s) to publish.");

        foreach (var m in pending)
        {
            // Count the attempt and commit it BEFORE publishing, so a row that
            // keeps killing the relay shows rising Attempts instead of looking
            // untouched (useful for spotting a poison row that needs a DLQ).
            m.Attempts++;
            await db.SaveChangesAsync();

            // ---- 1. PUBLISH to the broker ------------------------------------
            // MessageId = the outbox Id. It is STABLE across re-publishes, so the
            // copy sent now and the copy sent after a crash carry the same id and
            // the consumer recognises the second as a duplicate.
            var msg = new ServiceBusMessage(m.Payload)
            {
                MessageId = m.Id.ToString(),
                ContentType = "application/json",
                Subject = m.Type,
            };
            await sender.SendMessageAsync(msg);
            Console.WriteLine($"relay: PUBLISHED   {m.Id} (attempt #{m.Attempts}) -> topic 'quotes'");

            // ---- 2. CRASH WINDOW --------------------------------------------
            // The dangerous gap: the message is on the broker, but the DB does
            // not yet know. We simulate a process death right here.
            if (_crashAfterPublish)
            {
                Console.WriteLine($"relay: *** CRASH *** after publishing {m.Id}, before marking it sent.");
                Console.Out.Flush();
                Environment.Exit(70);   // hard exit: the mark below never runs
            }

            // ---- 3. MARK SENT and commit ------------------------------------
            m.ProcessedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            Console.WriteLine($"relay: MARKED SENT {m.Id} (ProcessedAt set).");
        }

        Console.WriteLine("relay: batch complete.");
    }
}
