using QuotesApi.Repositories;

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

        return app;
    }
}
