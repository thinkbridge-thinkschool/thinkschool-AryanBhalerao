# Serilog Setup

## Packages

```
Serilog.AspNetCore  10.0.0   — core + request logging + UseSerilog() host extension
Serilog.Sinks.Console 6.1.1  — console sink (already pulled in transitively, added explicitly)
```

## Program.cs

```csharp
// Stage 1: bootstrap logger so startup exceptions are captured before config is read.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Stage 2: full config-driven setup; reads the "Serilog" section from appsettings.
    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // ... AddInfrastructure, Build ...

    // Correlation: push ASP.NET Core's TraceIdentifier into every log event for this request.
    app.Use(async (ctx, next) =>
    {
        using (LogContext.PushProperty("TraceId", ctx.TraceIdentifier))
            await next(ctx);
    });

    app.UseSerilogRequestLogging(); // emits one INF summary line per request
```

## appsettings.json — Serilog section

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft.AspNetCore": "Warning",        // suppress chatty framework info
      "Microsoft.EntityFrameworkCore": "Warning",
      "System": "Warning"
    }
  },
  "Enrich": [ "FromLogContext" ],               // picks up TraceId pushed by middleware
  "WriteTo": [
    {
      "Name": "Console",
      "Args": {
        "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {TraceId} {SourceContext}: {Message:lj}{NewLine}{Exception}"
      }
    }
  ]
}
```

## appsettings.Development.json — EF Core SQL at Debug (dev only)

```json
"Serilog": {
  "MinimumLevel": {
    "Override": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Debug"
    }
  }
}
```

## Structured logging — the two rules

**Rule 1 — Structured (indexed key-value pairs, not interpolated strings):**

```csharp
// GOOD: QuoteId and UserId become searchable fields in any log backend
logger.LogInformation("Created quote {QuoteId} for user {UserId}", created.Id, ownerId);

// BAD: collapses to an unsearchable string
logger.LogInformation($"Created quote {created.Id} for user {ownerId}");
```

**Rule 2 — Correlated (every log line in a request shares a TraceId):**

`LogContext.PushProperty("TraceId", ctx.TraceIdentifier)` in the middleware scopes the
property to the current async execution context. Any logger called during that request
— whether in an endpoint, repository, or exception handler — inherits the same value.
