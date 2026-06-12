using QuotesApi.Services;

namespace QuotesApi.Endpoints;

// Simulates an external author-profile service hosted by a third party.
// The fault switch lets a drill flip it between healthy, erroring, and hanging.
public static class DownstreamEndpoints
{
    public static IEndpointRouteBuilder MapDownstreamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/downstream");

        group.MapGet("/author-profile/{name}", async (
            string name,
            DownstreamFaultState fault,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("QuotesApi.Downstream");
            var hit = fault.RecordHit();
            logger.LogInformation("Downstream hit #{Hit} ({Mode}) for {Author}", hit, fault.Mode, name);

            switch (fault.Mode)
            {
                case DownstreamFaultMode.Fail:
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);
                case DownstreamFaultMode.Slow:
                    // Hang past the attempt timeout
                    await Task.Delay(TimeSpan.FromSeconds(30), ct);
                    break;
            }

            return Results.Ok(new
            {
                Author = name,
                Bio = $"{name} is a frequently quoted author.",
                QuoteCount = Random.Shared.Next(1, 100),
                Source = "downstream-profile-service"
            });
        });

        // Non-idempotent operation: the resilience pipeline must NOT retry this
        group.MapPost("/author-profile/{name}/refresh", (string name, DownstreamFaultState fault, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("QuotesApi.Downstream");
            var hit = fault.RecordHit();
            logger.LogInformation("Downstream POST hit #{Hit} ({Mode}) for {Author}", hit, fault.Mode, name);

            return fault.Mode == DownstreamFaultMode.Fail
                ? Results.StatusCode(StatusCodes.Status500InternalServerError)
                : Results.Accepted();
        });

        // Fault controls for the drill
        group.MapPost("/fault/{mode}", (string mode, DownstreamFaultState fault) =>
        {
            if (!Enum.TryParse<DownstreamFaultMode>(mode, ignoreCase: true, out var parsed))
                return Results.BadRequest(new { Error = $"Unknown mode '{mode}'. Use ok, fail or slow." });

            fault.Mode = parsed;
            fault.ResetHits();
            return Results.Ok(new { Mode = parsed.ToString(), HitsReset = true });
        });

        group.MapGet("/fault", (DownstreamFaultState fault) =>
            Results.Ok(new { Mode = fault.Mode.ToString(), Hits = fault.Hits }));

        return app;
    }
}
