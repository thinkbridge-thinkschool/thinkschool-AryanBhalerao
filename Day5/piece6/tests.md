# Polly Resilience — Retry Tests

Quotes.Tests.Unit/EntraIdResilienceTests.cs
---

## Test 1 — succeeds after two transient failures, logs each retry

```csharp
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
                Delay            = TimeSpan.Zero,
                BackoffType      = DelayBackoffType.Constant,
                UseJitter        = false,
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

    var sp     = services.BuildServiceProvider();
    var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("entra-id");

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
```

### Console output captured by xunit

```
[WRN] Entra ID retry 1 of 3 after 00:00:00. Status: ServiceUnavailable. Exception: n/a
[WRN] Entra ID retry 2 of 3 after 00:00:00. Status: ServiceUnavailable. Exception: n/a
```

(Delay is 00:00:00 because tests use `TimeSpan.Zero`; production uses exponential back-off.)

---

## Test 2 — exhausts all retries, last bad response bubbles up, all 3 retry lines logged

```csharp
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
                Delay            = TimeSpan.Zero,
                BackoffType      = DelayBackoffType.Constant,
                UseJitter        = false,
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

    var sp     = services.BuildServiceProvider();
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
```

### Console output captured by xunit

```
[WRN] Entra ID retry 1 of 3 after 00:00:00. Status: ServiceUnavailable. Exception: n/a
[WRN] Entra ID retry 2 of 3 after 00:00:00. Status: ServiceUnavailable. Exception: n/a
[WRN] Entra ID retry 3 of 3 after 00:00:00. Status: ServiceUnavailable. Exception: n/a
```

---

## Test run

```
dotnet test Day5/piece6/Quotes.Tests.Unit --no-restore -v minimal
```

```
Passed!  - Failed: 0, Passed: 44, Skipped: 0, Total: 44, Duration: 2 s
```
