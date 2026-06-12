using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;
using QuotesApi.Repositories;
using QuotesApi.Services;

namespace QuotesApi.Endpoints;

public static class AuthorEndpoints
{
    public static IEndpointRouteBuilder MapAuthorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/authors");

        group.MapGet("/with-quotes", async (IAuthorRepository repository, CancellationToken ct) =>
        {
            var result = await repository.GetAllWithQuotesAsync(ct);
            return Results.Ok(result);
        });

        // Calls the external profile service through the resilience pipeline.
        // Each pipeline rejection maps to a distinct status so callers can tell
        // "dependency down" apart from "we are shedding load".
        group.MapGet("/{name}/profile", async (string name, AuthorProfileClient client, CancellationToken ct) =>
        {
            try
            {
                var profile = await client.GetProfileAsync(name, ct);
                return Results.Ok(profile);
            }
            catch (BrokenCircuitException)
            {
                // Breaker open: fail fast
                return Results.Json(
                    new { Error = "Author profile service unavailable (circuit open)" },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (TimeoutRejectedException)
            {
                return Results.Json(
                    new { Error = "Author profile service timed out" },
                    statusCode: StatusCodes.Status504GatewayTimeout);
            }
            catch (RateLimiterRejectedException)
            {
                // Bulkhead full
                return Results.Json(
                    new { Error = "Too many concurrent profile lookups" },
                    statusCode: StatusCodes.Status429TooManyRequests);
            }
            catch (HttpRequestException)
            {
                // Survived all retries
                return Results.Json(
                    new { Error = "Author profile service returned an error" },
                    statusCode: StatusCodes.Status502BadGateway);
            }
        });

        // Non-idempotent path through the same pipeline — never retried.
        group.MapPost("/{name}/profile/refresh", async (string name, AuthorProfileClient client, CancellationToken ct) =>
        {
            try
            {
                var accepted = await client.RefreshProfileAsync(name, ct);
                return accepted
                    ? Results.Accepted()
                    : Results.Json(
                        new { Error = "Refresh failed downstream (not retried: non-idempotent)" },
                        statusCode: StatusCodes.Status502BadGateway);
            }
            catch (BrokenCircuitException)
            {
                return Results.Json(
                    new { Error = "Author profile service unavailable (circuit open)" },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });

        return app;
    }
}
