# Polly Resilience — HttpClient + Handler Config

## Package

```xml
<!-- QuotesApi.csproj -->
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="10.6.0" />
```

## HttpClient Config

`InfrastructureExtensions.cs`
```csharp
services.AddHttpClient("entra-id")
    .AddResilienceHandler("default", (builder, ctx) =>
    {
        var logger = ctx.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Resilience.EntraId");

        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay             = TimeSpan.FromSeconds(1),
            BackoffType       = DelayBackoffType.Exponential,
            UseJitter         = true,
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

        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio      = 0.5,
            SamplingDuration  = TimeSpan.FromSeconds(30),
            MinimumThroughput = 5,
            BreakDuration     = TimeSpan.FromSeconds(30),
            OnOpened = args =>
            {
                logger.LogError(
                    "Entra ID circuit breaker opened for {Duration:g}. " +
                    "Failure ratio exceeded 50%% over the last 30 s.",
                    args.BreakDuration);
                return ValueTask.CompletedTask;
            }
        });

        builder.AddTimeout(TimeSpan.FromSeconds(10));
    });
```

## Handler Config

`InfrastructureExtensions.cs`
```csharp
services.AddOptions<JwtBearerOptions>(EntraScheme)
    .Configure<IHttpMessageHandlerFactory>((opts, handlerFactory) =>
    {
        opts.BackchannelHttpHandler = handlerFactory.CreateHandler("entra-id");
    });
```

## Retry delay schedule (exponential + jitter, base = 1 s)

| Attempt | Nominal delay | With ±25 % jitter |
|---------|--------------|-------------------|
| 1st retry | 1 s | 0.75 s – 1.25 s |
| 2nd retry | 2 s | 1.5 s – 2.5 s |
| 3rd retry | 4 s | 3 s – 5 s |

## Logging contract

Every retry emits a **Warning**-level structured log via `Resilience.EntraId` category:

```
[WRN] Entra ID retry 1 of 3 after 00:00:01. Status: ServiceUnavailable. Exception: n/a
[WRN] Entra ID retry 2 of 3 after 00:00:02. Status: ServiceUnavailable. Exception: n/a
```

Circuit-breaker open events emit an **Error**-level log:

```
[ERR] Entra ID circuit breaker opened for 00:00:30. Failure ratio exceeded 50% over the last 30 s.
```
