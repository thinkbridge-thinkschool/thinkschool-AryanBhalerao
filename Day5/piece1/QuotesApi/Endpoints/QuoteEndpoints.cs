using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;
using QuotesApi.Models;
using QuotesApi.Repositories;
using QuotesApi.Services;

namespace QuotesApi.Endpoints;

public static class QuoteEndpoints
{
    public const string ActivitySourceName = "QuotesApi.Quotes";
    private static readonly ActivitySource Source = new(ActivitySourceName);

    public static IEndpointRouteBuilder MapQuoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/quotes");

        group.MapGet("/", async (int page, int size, IQuoteRepository repository, AppDbContext db, CancellationToken ct) =>
        {
            using var activity = Source.StartActivity("list-quotes");
            activity?.SetTag("page", page);
            activity?.SetTag("size", size);

            var quotes = await repository.GetPagedAsync(page, size, ct);

            // SLOW PATH (intentional N+1 for observability demo):
            // For each quote we fire a separate SELECT to load its owner, then sleep
            // 150 ms to simulate realistic DB round-trip latency.
            // With 8 rows this adds ~1.2 s and produces 8 child DB spans in Jaeger.
            using var ownerActivity = Source.StartActivity("fetch-owner-details");
            ownerActivity?.SetTag("quote.count", quotes.Count);
            var ownersFound = 0;
            foreach (var quote in quotes)
            {
                if (quote.OwnerId.HasValue)
                {
                    _ = await db.Users.FirstOrDefaultAsync(u => u.Id == quote.OwnerId.Value, ct);
                    await Task.Delay(150, ct); // simulate per-row IO latency
                    ownersFound++;
                }
            }
            ownerActivity?.SetTag("owners.fetched", ownersFound);

            return Results.Ok(quotes);
        });

        group.MapGet("/{id:int}", async (int id, IQuoteRepository repository, CancellationToken ct) =>
        {
            using var activity = Source.StartActivity("get-quote");
            activity?.SetTag("quote.id", id);

            var quote = await repository.GetByIdAsync(id, ct);
            activity?.SetTag("found", quote is not null);
            return quote is null ? Results.NotFound() : Results.Ok(quote);
        });

        group.MapPost("/", async (
            CreateQuoteRequest request,
            IQuoteRepository repository,
            IQuoteValidator validator,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("QuotesApi.Quotes");

            using var validateActivity = Source.StartActivity("validate-quote");
            var errors = validator.Validate(request);
            if (errors.Count > 0)
            {
                validateActivity?.SetTag("valid", false);
                return Results.ValidationProblem(errors);
            }
            validateActivity?.SetTag("valid", true);
            validateActivity?.Stop();

            var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ownerId = sub is not null && int.TryParse(sub, out var id) ? (int?)id : null;

            using var createActivity = Source.StartActivity("create-quote");
            createActivity?.SetTag("user.id", ownerId?.ToString() ?? "anonymous");

            var quote = new Quote { Author = request.Author, Text = request.Text, OwnerId = ownerId };
            var created = await repository.CreateAsync(quote, ct);
            createActivity?.SetTag("quote.id", created.Id);

            logger.LogInformation("Created quote {QuoteId} for user {UserId}", created.Id, ownerId);
            return Results.Created($"/api/quotes/{created.Id}", created);
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
