using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QuotesApi.Commands;
using QuotesApi.Data;
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

        group.MapGet("/with-metadata", async (
            int page,
            int size,
            IQuoteMetadataQueryService metadataQueries,
            CancellationToken ct) =>
        {
            using var activity = Source.StartActivity("list-quotes-metadata");
            activity?.SetTag("page", page);
            activity?.SetTag("size", size);

            var quotes = await metadataQueries.GetPagedAsync(page, size, ct);
            return Results.Ok(quotes);
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

        group.MapPost("/{id:int}/metadata", async (
            int id,
            AssignMetadataRequest request,
            AppDbContext db,
            CancellationToken ct) =>
        {
            using var activity = Source.StartActivity("assign-quote-metadata");
            activity?.SetTag("quote.id", id);

            var quote = await db.Quotes
                .Include(q => q.Tags)
                .Include(q => q.Categories)
                .FirstOrDefaultAsync(q => q.Id == id, ct);

            if (quote is null)
            {
                activity?.SetTag("found", false);
                return Results.NotFound();
            }
            activity?.SetTag("found", true);

            var validTags = request.Tags
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var validCategories = request.Categories
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var errors = new Dictionary<string, string[]>();
            var tooLongTags = validTags.Where(t => t.Length > 50).ToList();
            var tooLongCats = validCategories.Where(c => c.Length > 50).ToList();
            if (tooLongTags.Count > 0)
                errors["tags"] = [$"Tag names must be 50 characters or fewer: {string.Join(", ", tooLongTags)}"];
            if (tooLongCats.Count > 0)
                errors["categories"] = [$"Category names must be 50 characters or fewer: {string.Join(", ", tooLongCats)}"];
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            foreach (var name in validTags)
            {
                if (quote.Tags.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
                    continue;
                var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == name, ct)
                          ?? db.Tags.Add(new Tag { Name = name }).Entity;
                quote.Tags.Add(tag);
            }

            foreach (var name in validCategories)
            {
                if (quote.Categories.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
                    continue;
                var category = await db.Categories.FirstOrDefaultAsync(c => c.Name == name, ct)
                               ?? db.Categories.Add(new Category { Name = name }).Entity;
                quote.Categories.Add(category);
            }

            await db.SaveChangesAsync(ct);

            activity?.SetTag("tags.count", validTags.Count);
            activity?.SetTag("categories.count", validCategories.Count);
            return Results.NoContent();
        });

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
