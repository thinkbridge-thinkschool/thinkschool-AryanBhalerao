using QuotesApi.Data;
using QuotesApi.Models;

namespace QuotesApi.Extensions;

public static class EndpointExtensions
{
    public static void MapCollectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/collections");

        group.MapGet("/{id:int}", async (int id, ICollectionRepository repo, CancellationToken ct) =>
        {
            var collection = await repo.GetByIdAsync(id, ct);
            return collection is not null ? Results.Ok(collection) : Results.NotFound();
        });

        group.MapPost("/", async (CreateCollectionDto dto, ICollectionRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.OwnerId))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["Name and OwnerId are required."]
                });

            try
            {
                var collection = Collection.Create(dto.Name, dto.OwnerId);
                await repo.AddAsync(collection, ct);
                return Results.Created($"/api/collections/{collection.Id}", collection);
            }
            catch (ArgumentException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        });

        group.MapPost("/{id:int}/items", async (int id, AddItemDto dto, ICollectionRepository repo, CancellationToken ct) =>
        {
            var collection = await repo.GetByIdAsync(id, ct);
            if (collection is null) return Results.NotFound();

            try
            {
                collection.AddItem(dto.QuoteId);
                await repo.UpdateAsync(collection, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        });

        group.MapDelete("/{id:int}/items/{quoteId:int}", async (int id, int quoteId, ICollectionRepository repo, CancellationToken ct) =>
        {
            var collection = await repo.GetByIdAsync(id, ct);
            if (collection is null) return Results.NotFound();

            try
            {
                collection.RemoveItem(quoteId);
                await repo.UpdateAsync(collection, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        });

        group.MapDelete("/{id:int}", async (int id, ICollectionRepository repo, CancellationToken ct) =>
        {
            var success = await repo.DeleteAsync(id, ct);
            return success ? Results.NoContent() : Results.NotFound();
        });
    }

    public static void MapQuoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/quotes");

        group.MapGet("/", async (int? page, int? size, IQuoteRepository repo, CancellationToken ct) =>
        {
            var p = page ?? 1;
            var s = size ?? 10;
            var (quotes, total) = await repo.GetPaginatedAsync(p, s, ct);
            return Results.Ok(new { Data = quotes, Total = total, Page = p, Size = s });
        });

        group.MapGet("/{id:int}", async (int id, IQuoteRepository repo, CancellationToken ct) =>
        {
            var quote = await repo.GetByIdAsync(id, ct);
            return quote is not null ? Results.Ok(quote) : Results.NotFound();
        });

        group.MapPost("/", async (CreateQuoteDto dto, IQuoteRepository repo, CancellationToken ct) =>
        {
            var result = Quote.Create(dto.Author, dto.Text);
            if (!result.IsSuccess)
                return Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);

            var created = await repo.AddAsync(result.Value!, ct);
            return Results.Created($"/api/quotes/{created.Id}", created);
        });

        group.MapDelete("/{id:int}", async (int id, IQuoteRepository repo, CancellationToken ct) =>
        {
            var success = await repo.DeleteAsync(id, ct);
            return success ? Results.NoContent() : Results.NotFound();
        });
    }
}
