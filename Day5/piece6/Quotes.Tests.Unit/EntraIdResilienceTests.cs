using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace Quotes.Tests.Unit;

public class EntraIdResilienceTests
{
    // ---------------------------------------------------------------------------
    // Fake handler: returns HTTP 503 for the first `failCount` calls, then 200.
    // ---------------------------------------------------------------------------
    private sealed class TransientFailureHandler(int failCount) : HttpMessageHandler
    {
        private int _callCount;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var call = Interlocked.Increment(ref _callCount);
            var status = call <= failCount
                ? HttpStatusCode.ServiceUnavailable
                : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status) { RequestMessage = request });
        }
    }

    // ---------------------------------------------------------------------------
    // Minimal ILogger that captures Warning messages into a list.
    // ---------------------------------------------------------------------------
    private sealed class CapturingLogger(List<string> sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Warning;

        public void Log<TState>(LogLevel level, EventId _, TState state,
            Exception? ex, Func<TState, Exception?, string> formatter)
        {
            if (level >= LogLevel.Warning)
                sink.Add(formatter(state, ex));
        }
    }

    private sealed class CapturingLoggerFactory(List<string> sink) : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider) { }
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(sink);
        public void Dispose() { }
    }

    // ---------------------------------------------------------------------------

    [Fact]
    public async Task EntraId_Pipeline_Retries_Twice_Then_Succeeds_And_Logs_Each_Retry()
    {
        // Arrange
        var logLines = new List<string>();

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(new CapturingLoggerFactory(logLines));

        services.AddHttpClient("entra-id")
            .ConfigurePrimaryHttpMessageHandler(() => new TransientFailureHandler(failCount: 2))
            .AddResilienceHandler("default", (builder, ctx) =>
            {
                var logger = ctx.ServiceProvider
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Resilience.EntraId");

                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.Zero,          // no artificial wait in tests
                    BackoffType = DelayBackoffType.Constant,
                    UseJitter = false,
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "Entra ID retry {Attempt} of 3 after {Delay:g}. " +
                            "Status: {Status}. Exception: {Exception}",
                            args.AttemptNumber + 1,
                            args.RetryDelay,
                            args.Outcome.Result?.StatusCode.ToString() ?? "n/a",
                            args.Outcome.Exception?.Message ?? "n/a");
                        return ValueTask.CompletedTask;
                    }
                });

                builder.AddTimeout(TimeSpan.FromSeconds(10));
            });

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("entra-id");

        // Act
        var response = await client.GetAsync(
            "https://login.microsoftonline.com/test-tenant/v2.0/.well-known/openid-configuration");

        // Assert — eventual success after two transient failures
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert — exactly two retry log lines were emitted (one per 503 response)
        var retryLogs = logLines.Where(l => l.Contains("Entra ID retry")).ToList();
        retryLogs.Should().HaveCount(2);
        retryLogs[0].Should().Contain("retry 1 of 3").And.Contain("ServiceUnavailable");
        retryLogs[1].Should().Contain("retry 2 of 3").And.Contain("ServiceUnavailable");
    }

    [Fact]
    public async Task EntraId_Pipeline_ExhaustsRetries_AndBubbles_LastOutcome()
    {
        // Arrange: fail more times than MaxRetryAttempts (3 retries = 4 total attempts)
        var logLines = new List<string>();

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(new CapturingLoggerFactory(logLines));

        services.AddHttpClient("entra-id")
            .ConfigurePrimaryHttpMessageHandler(() => new TransientFailureHandler(failCount: 99))
            .AddResilienceHandler("default", (builder, ctx) =>
            {
                var logger = ctx.ServiceProvider
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Resilience.EntraId");

                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.Zero,
                    BackoffType = DelayBackoffType.Constant,
                    UseJitter = false,
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "Entra ID retry {Attempt} of 3 after {Delay:g}. " +
                            "Status: {Status}. Exception: {Exception}",
                            args.AttemptNumber + 1,
                            args.RetryDelay,
                            args.Outcome.Result?.StatusCode.ToString() ?? "n/a",
                            args.Outcome.Exception?.Message ?? "n/a");
                        return ValueTask.CompletedTask;
                    }
                });

                builder.AddTimeout(TimeSpan.FromSeconds(10));
            });

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("entra-id");

        // Act
        var response = await client.GetAsync("https://login.microsoftonline.com/test-tenant/v2.0/");

        // Assert — after 3 retries (4 total attempts) Polly returns the last bad response
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        // Assert — all 3 retry log lines were emitted; failure was never silently swallowed
        var retryLogs = logLines.Where(l => l.Contains("Entra ID retry")).ToList();
        retryLogs.Should().HaveCount(3);
        retryLogs.Should().AllSatisfy(l => l.Should().Contain("ServiceUnavailable"));
    }
}
