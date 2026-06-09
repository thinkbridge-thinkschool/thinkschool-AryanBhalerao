using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace ServiceBusDemo;

/// Publishes QuoteCreated events to the 'quotes' TOPIC. The broker fans each
/// message out to BOTH subscriptions ('audit' and 'search-index') — every
/// subscriber gets its own copy.
public sealed class Publisher
{
    private readonly string _conn;
    public Publisher(string conn) => _conn = conn;

    public async Task RunAsync()
    {
        await using var client = new ServiceBusClient(_conn);
        await using var sender = client.CreateSender("quotes");

        var messages = new List<ServiceBusMessage>();

        var quotes = new (string id, string text, string author)[]
        {
            ("q-001", "Simplicity is the soul of efficiency.",          "Austin Freeman"),
            ("q-002", "Make it work, make it right, make it fast.",     "Kent Beck"),
            ("q-003", "Premature optimization is the root of all evil.", "Donald Knuth"),
            ("q-004", "Programs must be written for people to read.",   "Harold Abelson"),
            ("q-005", "Talk is cheap. Show me the code.",               "Linus Torvalds"),
        };
        foreach (var q in quotes)
            messages.Add(NewMessage(q.id, new QuoteCreated(q.id, q.text, q.author)));

        // DUPLICATE: same MessageId (q-002) as above. The broker won't drop it
        // (RequiresDuplicateDetection=false), so the *consumer's* idempotency
        // check is what must dedupe it.
        messages.Add(NewMessage("q-002",
            new QuoteCreated("q-002", "Make it work, make it right, make it fast.", "Kent Beck")));

        // POISON: tagged so the handler throws on every delivery. After
        // MaxDeliveryCount abandons the broker dead-letters it automatically.
        var poison = NewMessage("q-poison", new QuoteCreated("q-poison", "<corrupt payload>", "???"));
        poison.ApplicationProperties["poison"] = true;
        messages.Add(poison);

        await sender.SendMessagesAsync(messages);
        Console.WriteLine($"Published {messages.Count} messages to topic 'quotes' " +
                          "(5 unique + 1 duplicate id 'q-002' + 1 poison 'q-poison').");
    }

    private static ServiceBusMessage NewMessage(string id, QuoteCreated q) =>
        new(JsonSerializer.Serialize(q))
        {
            MessageId   = id,                 // <-- the idempotency key
            ContentType = "application/json",
            Subject     = "QuoteCreated",
        };
}
