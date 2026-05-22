# OpenTelemetry Setup — QuotesApi

## Packages Added

```xml
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.15.3" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.15.2" />
<PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.15.1-beta.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.15.1" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.15.3" />
```

## Tracing Configuration (`InfrastructureExtensions.cs`)

```csharp
services.AddOpenTelemetry()
    .WithTracing(t => t
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("QuotesApi"))
        .AddSource(QuoteEndpoints.ActivitySourceName)
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

## Custom Span — `QuoteEndpoints.cs`

```csharp
public const string ActivitySourceName = "QuotesApi.Quotes";
private static readonly ActivitySource Source = new(ActivitySourceName);

using (var activity = Source.StartActivity("authorize-delete-quote"))
{
    activity?.SetTag("quote.id", id);
    activity?.SetTag("user.id", userId);
    activity?.SetTag("quote.owner_id", quote.OwnerId?.ToString() ?? "none");

    result = await authService.AuthorizeAsync(user, quote, "can-delete-own-quote");
    activity?.SetTag("authorized", result.Succeeded);
}
```

## Log–Trace Correlation (`Program.cs`)

```csharp
app.Use(async (ctx, next) =>
{
    using (LogContext.PushProperty("TraceId", ctx.TraceIdentifier))
        await next(ctx);
});
```
