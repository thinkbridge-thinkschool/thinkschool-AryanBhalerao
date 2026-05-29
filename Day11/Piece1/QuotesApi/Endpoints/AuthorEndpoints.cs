using QuotesApi.Repositories;

namespace QuotesApi.Endpoints;

public static class AuthorEndpoints
{
    public static IEndpointRouteBuilder MapAuthorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/authors");

        // Deliberately slow endpoint: N+1 queries with no index on Quotes.AuthorId.
        // Used for profiling — measure with k6, inspect SQL via Serilog, plan via EXPLAIN QUERY PLAN.
        group.MapGet("/with-quotes", async (IAuthorRepository repository, CancellationToken ct) =>
        {
            var result = await repository.GetAllWithQuotesSlowAsync(ct);
            return Results.Ok(result);
        });

        return app;
    }
}
