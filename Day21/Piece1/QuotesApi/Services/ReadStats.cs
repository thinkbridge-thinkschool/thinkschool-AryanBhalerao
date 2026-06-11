namespace QuotesApi.Services;

// Cache effectiveness counters
public sealed class ReadStats
{
    private long _requests;
    private long _dbReads;

    public void RecordRequest() => Interlocked.Increment(ref _requests);
    public void RecordDbRead() => Interlocked.Increment(ref _dbReads);

    public long Requests => Interlocked.Read(ref _requests);
    public long DbReads => Interlocked.Read(ref _dbReads);

    public void Reset()
    {
        Interlocked.Exchange(ref _requests, 0);
        Interlocked.Exchange(ref _dbReads, 0);
    }

    public object Snapshot()
    {
        var requests = Requests;
        var dbReads = DbReads;
        var hits = requests - dbReads;
        return new
        {
            Requests = requests,
            DbReads = dbReads,
            CacheHits = hits,
            HitRatePercent = requests == 0 ? 0 : Math.Round(100.0 * hits / requests, 2)
        };
    }
}
