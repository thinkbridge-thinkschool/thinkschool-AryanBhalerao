using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace ServiceBusDemo;

/// A competing-consumer worker bound to ONE subscription. Run two of these
/// against the same subscription and they compete: peek-lock means each message
/// is delivered to exactly one of them. MaxConcurrentCalls adds in-process
/// competition on top of that.
public sealed class Worker
{
    private readonly string _conn;
    private readonly string _subscription;
    private readonly int _concurrency;
    private readonly string _workerId;
    private readonly IdempotencyStore _store;

    public Worker(string conn, string subscription, int concurrency, string workerId, IdempotencyStore store)
    {
        _conn = conn;
        _subscription = subscription;
        _concurrency = concurrency;
        _workerId = workerId;
        _store = store;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        await using var client = new ServiceBusClient(_conn);
        var processor = client.CreateProcessor("quotes", _subscription, new ServiceBusProcessorOptions
        {
            ReceiveMode          = ServiceBusReceiveMode.PeekLock,
            AutoCompleteMessages = false,          // we decide complete/abandon
            MaxConcurrentCalls   = _concurrency,
        });

        processor.ProcessMessageAsync += OnMessageAsync;
        processor.ProcessErrorAsync   += args =>
        {
            Console.WriteLine($"[{_workerId}] processor error ({args.EntityPath}): {args.Exception.Message}");
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(ct);
        Console.WriteLine($"[{_workerId}] competing consumer up on '{_subscription}' " +
                          $"(MaxConcurrentCalls={_concurrency}). Ctrl+C to stop.");

        try { await Task.Delay(Timeout.Infinite, ct); }
        catch (OperationCanceledException) { /* shutdown */ }

        await processor.StopProcessingAsync(CancellationToken.None);
        Console.WriteLine($"[{_workerId}] stopped.");
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        var msg = args.Message;
        var id  = msg.MessageId;

        // ---- IDEMPOTENCY: dedupe on MessageId ---------------------------------
        // If we've already applied the side effect for this id, this is a
        // duplicate send or a redelivery of an already-committed message.
        // Skip the work but still Complete so it leaves the subscription.
        if (_store.IsProcessed(_subscription, id))
        {
            Console.WriteLine($"[{_workerId}] DUP   {id} (delivery#{msg.DeliveryCount}) -> already processed, skipping side effect.");
            await args.CompleteMessageAsync(msg);
            return;
        }

        try
        {
            var quote = JsonSerializer.Deserialize<QuoteCreated>(msg.Body.ToString())!;

            // Poison pill: a message we can never process successfully.
            if (msg.ApplicationProperties.TryGetValue("poison", out var p) && p is true)
                throw new InvalidOperationException("cannot process poison payload");

            // ---- the side effect (the thing that must happen exactly once) ----
            Console.WriteLine($"[{_workerId}] OK    {id} (delivery#{msg.DeliveryCount}) -> {quote.Author}: \"{quote.Text}\"");

            // Mark processed AFTER the side effect succeeded, then Complete.
            _store.MarkProcessed(_subscription, id, _workerId);
            await args.CompleteMessageAsync(msg);
        }
        catch (Exception ex)
        {
            // Abandon releases the lock -> broker redelivers. Once DeliveryCount
            // exceeds the subscription's MaxDeliveryCount (3), the broker moves
            // the message to the subscription's dead-letter queue on its own.
            Console.WriteLine($"[{_workerId}] FAIL  {id} (delivery#{msg.DeliveryCount}) -> {ex.Message}; abandoning.");
            await args.AbandonMessageAsync(msg);
        }
    }
}
