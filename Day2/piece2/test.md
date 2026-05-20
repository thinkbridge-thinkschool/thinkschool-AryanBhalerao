# CancellationTests.cs

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using QuotesApi.Data;
using QuotesApi.Middleware;
using QuotesApi.Models;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace QuotesApi.Tests;

file sealed class SlowCollectionRepository : ICollectionRepository
{
    private readonly TimeSpan _delay;

    public SlowCollectionRepository(TimeSpan delay) => _delay = delay;

    public async Task<Collection?> GetByIdAsync(int id, CancellationToken ct)
    {
        await Task.Delay(_delay, ct);
        return null;
    }

    public async Task<Collection> AddAsync(Collection collection, CancellationToken ct)
    {
        await Task.Delay(_delay, ct);
        return collection;
    }

    public async Task UpdateAsync(Collection collection, CancellationToken ct)
        => await Task.Delay(_delay, ct);

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        await Task.Delay(_delay, ct);
        return false;
    }
}

public sealed class SlowCollectionFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public SlowCollectionFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace on-disk SQLite with in-memory so migrations run cleanly.
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<QuoteDbContext>));
            if (dbDescriptor is not null) services.Remove(dbDescriptor);

            services.AddDbContext<QuoteDbContext>(
                o => o.UseSqlite(_connection));

            // Replace the real repository with the slow stub.
            var repoDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ICollectionRepository));
            if (repoDescriptor is not null) services.Remove(repoDescriptor);

            services.AddScoped<ICollectionRepository>(
                _ => new SlowCollectionRepository(TimeSpan.FromSeconds(5)));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _connection.Dispose();
        base.Dispose(disposing);
    }
}

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_OperationCancelled_Returns499()
    {
        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new OperationCanceledException(cts.Token),
            cts.Token);

        Assert.True(handled);
        Assert.Equal(499, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_OtherException_Returns500()
    {
        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new InvalidOperationException("boom"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(500, httpContext.Response.StatusCode);
    }
}

public class CollectionCancellationTests : IClassFixture<SlowCollectionFactory>
{
    private readonly HttpClient _client;

    public CollectionCancellationTests(SlowCollectionFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task GetCollection_TokenCancelledDuringSlowIo_OperationDoesNotCompleteOr499()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        HttpResponseMessage? response = null;
        try
        {
            response = await _client.GetAsync("/api/collections/1", cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        Assert.Equal(499, (int)response.StatusCode);
    }

    [Fact]
    public async Task PostCollection_TokenCancelledDuringSlowIo_OperationDoesNotCompleteOr499()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var content = JsonContent.Create(new { Name = "Reading", OwnerId = "user42" });

        HttpResponseMessage? response = null;
        try
        {
            response = await _client.PostAsync("/api/collections", content, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        Assert.Equal(499, (int)response.StatusCode);
    }

    [Fact]
    public async Task DeleteCollection_TokenCancelledDuringSlowIo_OperationDoesNotCompleteOr499()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        HttpResponseMessage? response = null;
        try
        {
            response = await _client.DeleteAsync("/api/collections/1", cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        Assert.Equal(499, (int)response.StatusCode);
    }
}
```
