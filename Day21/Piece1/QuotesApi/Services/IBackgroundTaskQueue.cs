namespace QuotesApi.Services;

// Producer/consumer contract for off-thread work
public interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem);

    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);

    int Count { get; }
}
