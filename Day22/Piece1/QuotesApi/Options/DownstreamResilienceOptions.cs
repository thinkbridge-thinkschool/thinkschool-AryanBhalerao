namespace QuotesApi.Options;

// Bound from "DownstreamResilience" config section
public sealed class DownstreamResilienceOptions
{
    public string BaseAddress { get; set; } = "http://localhost:5051";

    // Bulkhead: concurrent in-flight calls allowed to the dependency
    public int BulkheadPermitLimit { get; set; } = 8;

    public int BulkheadQueueLimit { get; set; } = 4;

    // Whole-operation budget, covers all retry attempts together
    public int TotalTimeoutSeconds { get; set; } = 10;

    // Retry (idempotent requests only)
    public int MaxRetryAttempts { get; set; } = 3;

    public int RetryBaseDelayMs { get; set; } = 200;

    // Circuit breaker
    public double FailureRatio { get; set; } = 0.5;

    public int MinimumThroughput { get; set; } = 10;

    public int SamplingDurationSeconds { get; set; } = 10;

    public int BreakDurationSeconds { get; set; } = 10;

    // Per-attempt budget
    public int AttemptTimeoutSeconds { get; set; } = 2;
}
