using System.ComponentModel.DataAnnotations;

namespace OutboxDemo;

/// The domain aggregate. Creating one is the "domain change" that must stay in
/// lock-step with the message we publish about it.
public class Quote
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string Author { get; set; } = "";

    [MaxLength(500)]
    public string Text { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }
}

/// The integration event carried on the topic. The outbox row's Id is copied
/// onto ServiceBusMessage.MessageId and becomes the idempotency key the consumer
/// dedupes on, so a re-publish after a crash is recognised as the same message.
public record QuoteCreated(string QuoteId, string Text, string Author);
