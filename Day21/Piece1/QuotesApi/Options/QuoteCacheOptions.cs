namespace QuotesApi.Options;

// Bound from "QuoteCache" config section
public sealed class QuoteCacheOptions
{
    public bool Enabled { get; set; }

    public string RedisConnection { get; set; } = "localhost:6379";

    public int L2ExpirationSeconds { get; set; } = 300;

    public int L1ExpirationSeconds { get; set; } = 60;

    public int SimulatedDbLatencyMs { get; set; }
}
