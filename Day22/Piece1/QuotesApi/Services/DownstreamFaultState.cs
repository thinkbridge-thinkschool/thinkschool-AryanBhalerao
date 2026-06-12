namespace QuotesApi.Services;

public enum DownstreamFaultMode
{
    Ok,
    Fail,
    Slow
}

// Runtime switch controlling the simulated downstream behaviour
public sealed class DownstreamFaultState
{
    private volatile DownstreamFaultMode _mode = DownstreamFaultMode.Ok;
    private long _hits;

    public DownstreamFaultMode Mode
    {
        get => _mode;
        set => _mode = value;
    }

    public long Hits => Interlocked.Read(ref _hits);

    public long RecordHit() => Interlocked.Increment(ref _hits);

    public void ResetHits() => Interlocked.Exchange(ref _hits, 0);
}
