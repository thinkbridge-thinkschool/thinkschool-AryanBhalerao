# Cancellation-Aware Service

## Endpoints — `EndpointExtensions.cs`
```csharp
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
```

---

## Repository interface — `ICollectionRepository.cs`

```csharp
public interface ICollectionRepository
{
    Task<Collection?> GetByIdAsync(int id, CancellationToken ct);
    Task<Collection>  AddAsync(Collection collection, CancellationToken ct);
    Task              UpdateAsync(Collection collection, CancellationToken ct);
    Task<bool>        DeleteAsync(int id, CancellationToken ct);
}
```

Every async method that touches I/O takes `CancellationToken ct` as its last parameter.

---

## Repository implementation — `CollectionRepository.cs`

```csharp
public class CollectionRepository : ICollectionRepository
{
    private readonly QuoteDbContext _dbContext;
    private readonly ILogger<CollectionRepository> _logger;

    public CollectionRepository(QuoteDbContext dbContext, ILogger<CollectionRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Collection?> GetByIdAsync(int id, CancellationToken ct)
    {
        _logger.LogInformation("Fetching collection {Id}", id);
        return await _dbContext.Collections
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct);  // EF respects ct
    }

    public async Task<Collection> AddAsync(Collection collection, CancellationToken ct)
    {
        _logger.LogInformation("Adding collection '{Name}'", collection.Name);
        _dbContext.Collections.Add(collection);
        await _dbContext.SaveChangesAsync(ct);           // EF respects ct
        return collection;
    }

    public async Task UpdateAsync(Collection collection, CancellationToken ct)
    {
        _logger.LogInformation("Updating collection {Id}", collection.Id);
        await _dbContext.SaveChangesAsync(ct);           // EF respects ct
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        _logger.LogInformation("Deleting collection {Id}", id);
        var collection = await _dbContext.Collections.FindAsync(new object[] { id }, ct);
        if (collection is null) return false;
        _dbContext.Collections.Remove(collection);
        await _dbContext.SaveChangesAsync(ct);           // EF respects ct
        return true;
    }
}
```

---

## Exception handler — `GlobalExceptionHandler.cs`

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client.");
            httpContext.Response.StatusCode = 499;
            return true;
        }

        _logger.LogError(exception, "An unhandled exception occurred.");

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title  = "Server Error",
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, CancellationToken.None);

        return true;
    }
}
```