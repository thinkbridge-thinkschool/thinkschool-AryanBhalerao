using System.Text.Json;

namespace OutboxDemo;

/// Writes the domain change and the outbox row inside ONE database transaction.
///
/// This is the crux of the pattern. The naive alternative — save the quote, then
/// call the broker — has a window where the DB commit succeeds but the publish
/// fails (or the process dies), leaving the quote with no event ever sent: the
/// DB and the queue have DIVERGED. By writing the event as a row in the same
/// transaction as the quote, the two facts ("quote exists", "event is pending")
/// commit together or not at all. Publishing is deferred to the relay.
public static class Producer
{
    public static async Task RunAsync(string author, string text)
    {
        await using var db = Db.Open();
        await using var tx = await db.Database.BeginTransactionAsync();

        // 1) the domain change
        var quote = new Quote
        {
            Author = author,
            Text = text,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Quotes.Add(quote);
        await db.SaveChangesAsync();          // assigns quote.Id (still inside the tx)

        // 2) the outbox row describing that change — same DbContext, same tx
        var evt = new QuoteCreated(quote.Id.ToString(), quote.Text, quote.Author);
        var outbox = new OutboxMessage
        {
            Id = Guid.NewGuid(),               // becomes the broker MessageId == idempotency key
            Type = nameof(QuoteCreated),
            Payload = JsonSerializer.Serialize(evt),
            OccurredAt = DateTimeOffset.UtcNow,
            ProcessedAt = null,                // unsent
            Attempts = 0,
        };
        db.OutboxMessages.Add(outbox);
        await db.SaveChangesAsync();

        // 3) commit BOTH rows atomically. A crash anywhere above rolls back both:
        //    no orphan quote, no orphan event.
        await tx.CommitAsync();

        Console.WriteLine($"COMMIT  quote #{quote.Id} + outbox {outbox.Id} in one transaction.");
        Console.WriteLine($"        \"{text}\" — {author}");
        Console.WriteLine($"        outbox row is UNSENT (ProcessedAt = NULL).");
    }
}
