namespace QuotesApi.Services;

// Producer/consumer contract for off-thread work.
// HTTP endpoints (producers) call QueueAsync and return immediately; the
// QueuedHostedService (the single consumer) calls DequeueAsync in a loop.
// A work item is a delegate that takes the host's stopping token so it can
// cooperate with graceful shutdown.
public interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem);

    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);

    int Count { get; }
}
