using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QuotesApi.Commands;
using QuotesApi.Data;
using QuotesApi.Models;
using QuotesApi.Queries;
using QuotesApi.Repositories;
using QuotesApi.Services;

namespace QuotesApi.Endpoints;

public static class QuoteEndpoints
{
    private const int MaxPageSize = 100;

    // Returns null when paging params are valid; otherwise a 400 ValidationProblem.
    // page < 1 would produce a negative OFFSET (SQL error), unbounded size invites huge queries.
    private static IResult? ValidatePaging(int page, int size)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
            errors["page"] = ["Page must be 1 or greater"];
        if (size < 1 || size > MaxPageSize)
            errors["size"] = [$"Size must be between 1 and {MaxPageSize}"];
        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }

    public static IEndpointRouteBuilder MapQuoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/quotes");

        group.MapGet("/", async (int page, int size, IQuoteQueryService queries, CancellationToken ct) =>
        {
            if (ValidatePaging(page, size) is { } invalid)
                return invalid;

            var quotes = await queries.GetPagedAsync(page, size, ct);
            return Results.Ok(quotes);
        });

        group.MapGet("/{id:int}", async (int id, IQuoteQueryService queries, CancellationToken ct) =>
        {
            var quote = await queries.GetByIdAsync(id, ct);
            return quote is null ? Results.NotFound() : Results.Ok(quote);
        });

        group.MapGet("/with-metadata", async (
            int page,
            int size,
            IQuoteMetadataQueryService metadataQueries,
            CancellationToken ct) =>
        {
            if (ValidatePaging(page, size) is { } invalid)
                return invalid;

            var quotes = await metadataQueries.GetPagedAsync(page, size, ct);
            return Results.Ok(quotes);
        });

        group.MapPost("/", async (
            CreateQuoteRequest request,
            CreateQuoteCommandHandler handler,
            IBackgroundTaskQueue queue,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("QuotesApi.Quotes");

            var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ownerId = sub is not null && int.TryParse(sub, out var id) ? (int?)id : null;

            var command = new CreateQuoteCommand(request.Author, request.Text, ownerId);

            var (quoteId, errors) = await handler.HandleAsync(command, ct);
            if (errors is not null)
                return Results.ValidationProblem(errors);

            logger.LogInformation("Created quote {QuoteId} for user {UserId}", quoteId, ownerId);

            // Slow post-processing (e.g. indexing, notifications) does not belong on
            // the request thread — enqueue it and return to the caller right away.
            await queue.QueueAsync(async bgCt =>
            {
                logger.LogInformation("Post-processing quote {QuoteId}…", quoteId);
                await Task.Delay(TimeSpan.FromSeconds(2), bgCt);
                logger.LogInformation("Finished post-processing quote {QuoteId}.", quoteId);
            });

            return Results.Created($"/api/quotes/{quoteId}", new { Id = quoteId });
        }).RequireAuthorization("can-edit-quotes");

        group.MapPost("/{id:int}/metadata", async (
            int id,
            AssignMetadataRequest request,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var quote = await db.Quotes
                .Include(q => q.Tags)
                .Include(q => q.Categories)
                .FirstOrDefaultAsync(q => q.Id == id, ct);

            if (quote is null)
                return Results.NotFound();

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
            return Results.NoContent();
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

            var quote = await repository.GetByIdAsync(id, ct);
            if (quote is null)
                return Results.NotFound();

            var result = await authService.AuthorizeAsync(user, quote, "can-delete-own-quote");
            if (!result.Succeeded)
                return Results.Forbid();

            await repository.DeleteAsync(id, ct);
            logger.LogInformation("Deleted quote {QuoteId} by user {UserId}", id, userId);
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }
}
