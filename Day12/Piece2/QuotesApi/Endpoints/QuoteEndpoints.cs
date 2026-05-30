using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using QuotesApi.Commands;
using QuotesApi.Models;
using QuotesApi.Queries;
using QuotesApi.Repositories;

namespace QuotesApi.Endpoints;

public static class QuoteEndpoints
{
    public const string ActivitySourceName = "QuotesApi.Quotes";
    private static readonly ActivitySource Source = new(ActivitySourceName);

    public static IEndpointRouteBuilder MapQuoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/quotes");

        group.MapGet("/", async (int page, int size, IQuoteQueryService queries, CancellationToken ct) =>
        {
            using var activity = Source.StartActivity("list-quotes");
            activity?.SetTag("page", page);
            activity?.SetTag("size", size);

            var quotes = await queries.GetPagedAsync(page, size, ct);
            return Results.Ok(quotes);
        });

        group.MapGet("/{id:int}", async (int id, IQuoteQueryService queries, CancellationToken ct) =>
        {
            using var activity = Source.StartActivity("get-quote");
            activity?.SetTag("quote.id", id);

            var quote = await queries.GetByIdAsync(id, ct);
            activity?.SetTag("found", quote is not null);
            return quote is null ? Results.NotFound() : Results.Ok(quote);
        });

        // Benchmark: runs both EF and Dapper GetPagedAsync back-to-back and returns wall-clock ms for each.
        // Hit GET /api/quotes/benchmark?page=1&size=20 to compare.
        group.MapGet("/benchmark", async (
            int page,
            int size,
            [FromKeyedServices("ef")]     IQuoteQueryService efQuery,
            [FromKeyedServices("dapper")] IQuoteQueryService dapperQuery,
            CancellationToken ct) =>
        {
            const int warmupRuns = 3;
            const int timedRuns  = 10;

            // Warm both paths so plan cache and connection pool are primed.
            for (var i = 0; i < warmupRuns; i++)
            {
                await efQuery.GetPagedAsync(page, size, ct);
                await dapperQuery.GetPagedAsync(page, size, ct);
            }

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < timedRuns; i++)
                await efQuery.GetPagedAsync(page, size, ct);
            sw.Stop();
            var efTotalMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            for (var i = 0; i < timedRuns; i++)
                await dapperQuery.GetPagedAsync(page, size, ct);
            sw.Stop();
            var dapperTotalMs = sw.Elapsed.TotalMilliseconds;

            return Results.Ok(new
            {
                Runs         = timedRuns,
                EF = new
                {
                    TotalMs   = Math.Round(efTotalMs,     2),
                    AvgMs     = Math.Round(efTotalMs     / timedRuns, 2)
                },
                Dapper = new
                {
                    TotalMs   = Math.Round(dapperTotalMs, 2),
                    AvgMs     = Math.Round(dapperTotalMs / timedRuns, 2)
                },
                SpeedupFactor = Math.Round(efTotalMs / dapperTotalMs, 2)
            });
        });

        group.MapPost("/", async (
            CreateQuoteRequest request,
            CreateQuoteCommandHandler handler,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("QuotesApi.Quotes");

            var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ownerId = sub is not null && int.TryParse(sub, out var id) ? (int?)id : null;

            var command = new CreateQuoteCommand(request.Author, request.Text, ownerId);

            using var activity = Source.StartActivity("create-quote");
            activity?.SetTag("user.id", ownerId?.ToString() ?? "anonymous");

            var (quoteId, errors) = await handler.HandleAsync(command, ct);
            if (errors is not null)
                return Results.ValidationProblem(errors);

            activity?.SetTag("quote.id", quoteId);
            logger.LogInformation("Created quote {QuoteId} for user {UserId}", quoteId, ownerId);
            return Results.Created($"/api/quotes/{quoteId}", new { Id = quoteId });
        }).RequireAuthorization("can-edit-quotes");

        group.MapDelete("/{id:int}", async (
            int id,
            IQuoteRepository repository,
            IAuthorizationService authService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("QuotesApi.Quotes");
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            using var deleteActivity = Source.StartActivity("delete-quote");
            deleteActivity?.SetTag("quote.id", id);
            deleteActivity?.SetTag("user.id", userId);

            var quote = await repository.GetByIdAsync(id, ct);
            if (quote is null)
            {
                deleteActivity?.SetTag("found", false);
                return Results.NotFound();
            }
            deleteActivity?.SetTag("found", true);

            AuthorizationResult result;
            using (var authActivity = Source.StartActivity("authorize-delete-quote"))
            {
                authActivity?.SetTag("quote.id", id);
                authActivity?.SetTag("user.id", userId);
                authActivity?.SetTag("quote.owner_id", quote.OwnerId?.ToString() ?? "none");

                result = await authService.AuthorizeAsync(user, quote, "can-delete-own-quote");
                authActivity?.SetTag("authorized", result.Succeeded);
            }

            if (!result.Succeeded)
            {
                deleteActivity?.SetTag("authorized", false);
                return Results.Forbid();
            }

            await repository.DeleteAsync(id, ct);
            deleteActivity?.SetTag("authorized", true);
            logger.LogInformation("Deleted quote {QuoteId} by user {UserId}", id, userId);
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }
}
