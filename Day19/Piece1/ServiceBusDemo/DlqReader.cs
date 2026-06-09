using Azure.Messaging.ServiceBus;

namespace ServiceBusDemo;

/// Reads the dead-letter queue of a subscription. The DLQ is a system sub-queue
/// hanging off the subscription; the SDK targets it with SubQueue.DeadLetter.
/// Messages here carry DeadLetterReason / DeadLetterErrorDescription explaining
/// why the broker gave up on them.
public sealed class DlqReader
{
    private readonly string _conn;
    private readonly string _subscription;

    public DlqReader(string conn, string subscription)
    {
        _conn = conn;
        _subscription = subscription;
    }

    public async Task RunAsync()
    {
        await using var client = new ServiceBusClient(_conn);
        await using var receiver = client.CreateReceiver("quotes", _subscription, new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter,
        });

        var dead = await receiver.ReceiveMessagesAsync(maxMessages: 20, maxWaitTime: TimeSpan.FromSeconds(5));
        Console.WriteLine($"Dead-letter queue of subscription '{_subscription}': {dead.Count} message(s).");

        foreach (var m in dead)
        {
            Console.WriteLine($"  - id={m.MessageId}  deliveryCount={m.DeliveryCount}");
            Console.WriteLine($"      DeadLetterReason           = {m.DeadLetterReason}");
            Console.WriteLine($"      DeadLetterErrorDescription = {m.DeadLetterErrorDescription}");
            Console.WriteLine($"      body                       = {m.Body}");
            // Leave it on the DLQ for inspection; switch to CompleteMessageAsync
            // to purge after triage / replay.
        }
    }
}
