using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace OutboxDemo;

/// The idempotent consumer. It subscribes to 'search-index' and applies a side
/// effect (here: "index this quote") exactly once per business event, even though
/// the relay may deliver the same event more than once after a crash.
///
/// The dedupe gate keys on MessageId — which the relay set to the stable outbox
/// Id — so a re-published copy is recognised as a duplicate and its side effect
/// is skipped. That is what turns the relay's at-least-once delivery into
/// effectively-once processing: no loss (relay re-publishes) and no double
/// effect (consumer dedupes).
public sealed class Consumer
{
    private readonly string _conn;
    private readonly string _subscription;
    private readonly string _id;
    private readonly IdempotencyStore _store;

    public Consumer(string conn, string subscription, string id, IdempotencyStore store)
    {
        _conn = conn;
        _subscription = subscription;
        _id = id;
        _store = store;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        await using var client = new ServiceBusClient(_conn);
        var processor = client.CreateProcessor("quotes", _subscription, new ServiceBusProcessorOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1,
        });

        processor.ProcessMessageAsync += OnMessageAsync;
        processor.ProcessErrorAsync += args =>
        {
            Console.WriteLine($"[{_id}] processor error ({args.EntityPath}): {args.Exception.Message}");
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(ct);
        Console.WriteLine($"[{_id}] idempotent consumer up on '{_subscription}'. Ctrl+C to stop.");

        try { await Task.Delay(Timeout.Infinite, ct); }
        catch (OperationCanceledException) { /* shutdown */ }

        await processor.StopProcessingAsync(CancellationToken.None);
        Console.WriteLine($"[{_id}] stopped.");
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        var msg = args.Message;
        var id = msg.MessageId;

        // ---- IDEMPOTENCY GATE: have we already applied this event? -----------
        if (_store.IsProcessed(_subscription, id))
        {
            Console.WriteLine($"[{_id}] DUP   {id} (delivery#{msg.DeliveryCount}) -> already indexed, skipping side effect.");
            await args.CompleteMessageAsync(msg);
            return;
        }

        try
        {
            var quote = JsonSerializer.Deserialize<QuoteCreated>(msg.Body.ToString())!;

            // ---- the side effect that must happen exactly once ----
            Console.WriteLine($"[{_id}] INDEX {id} (delivery#{msg.DeliveryCount}) -> {quote.Author}: \"{quote.Text}\"");

            _store.MarkProcessed(_subscription, id, _id);   // record AFTER success
            await args.CompleteMessageAsync(msg);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_id}] FAIL  {id} (delivery#{msg.DeliveryCount}) -> {ex.Message}; abandoning.");
            await args.AbandonMessageAsync(msg);
        }
    }
}
