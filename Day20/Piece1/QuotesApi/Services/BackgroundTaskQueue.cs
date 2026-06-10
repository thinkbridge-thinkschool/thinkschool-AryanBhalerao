using System.Threading.Channels;

namespace QuotesApi.Services;

// A bounded in-memory work queue backed by System.Threading.Channels.
// Bounded so a burst of requests cannot grow memory without limit: when the
// channel is full, QueueAsync awaits (back-pressure) until the consumer frees
// a slot. SingleReader = true because only the QueuedHostedService drains it.
public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

    public BackgroundTaskQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        };
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
    }

    public int Count => _queue.Reader.Count;

    public async ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
        => await _queue.Reader.ReadAsync(cancellationToken);
}
