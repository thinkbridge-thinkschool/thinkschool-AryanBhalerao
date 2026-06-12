using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using QuotesApi.Options;
using QuotesApi.Services;

namespace QuotesApi.Extensions;

public static class ResilienceExtensions
{
    public static void AddDownstreamResilience(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DownstreamResilienceOptions>(configuration.GetSection("DownstreamResilience"));

        // Simulated external dependency state (drill control)
        services.AddSingleton<DownstreamFaultState>();

        services.AddHttpClient<AuthorProfileClient>((sp, http) =>
            {
                var opts = sp.GetRequiredService<IOptions<DownstreamResilienceOptions>>().Value;
                http.BaseAddress = new Uri(opts.BaseAddress);
            })
            .AddResilienceHandler("downstream-profile", (builder, context) =>
            {
                var opts = context.ServiceProvider
                    .GetRequiredService<IOptions<DownstreamResilienceOptions>>().Value;
                var logger = context.ServiceProvider
                    .GetRequiredService<ILoggerFactory>().CreateLogger("QuotesApi.Resilience");

                // 1. Bulkhead
                builder.AddConcurrencyLimiter(
                    permitLimit: opts.BulkheadPermitLimit,
                    queueLimit: opts.BulkheadQueueLimit);

                // 2. Total timeout
                builder.AddTimeout(TimeSpan.FromSeconds(opts.TotalTimeoutSeconds));

                // 3. Retry
                var retry = new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = opts.MaxRetryAttempts,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(opts.RetryBaseDelayMs),
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "Retry {Attempt} after {Delay} ms ({Reason})",
                            args.AttemptNumber + 1,
                            (int)args.RetryDelay.TotalMilliseconds,
                            args.Outcome.Exception?.GetType().Name
                                ?? $"HTTP {(int?)args.Outcome.Result?.StatusCode}");
                        return default;
                    }
                };
                // Narrow the transient predicate to GETs only
                var transient = retry.ShouldHandle;
                retry.ShouldHandle = async args =>
                    args.Context.GetRequestMessage()?.Method == HttpMethod.Get
                    && await transient(args);
                builder.AddRetry(retry);

                // 4. Circuit breaker
                builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = opts.FailureRatio,
                    MinimumThroughput = opts.MinimumThroughput,
                    SamplingDuration = TimeSpan.FromSeconds(opts.SamplingDurationSeconds),
                    BreakDuration = TimeSpan.FromSeconds(opts.BreakDurationSeconds),
                    OnOpened = args =>
                    {
                        logger.LogError(
                            "CIRCUIT OPENED for {BreakSeconds}s — downstream failing ({Reason})",
                            args.BreakDuration.TotalSeconds,
                            args.Outcome.Exception?.GetType().Name
                                ?? $"HTTP {(int?)args.Outcome.Result?.StatusCode}");
                        return default;
                    },
                    OnHalfOpened = _ =>
                    {
                        logger.LogWarning("CIRCUIT HALF-OPEN — sending probe request downstream");
                        return default;
                    },
                    OnClosed = _ =>
                    {
                        logger.LogInformation("CIRCUIT CLOSED — downstream healthy again");
                        return default;
                    }
                });

                // 5. Attempt timeout
                builder.AddTimeout(TimeSpan.FromSeconds(opts.AttemptTimeoutSeconds));
            });
    }
}
