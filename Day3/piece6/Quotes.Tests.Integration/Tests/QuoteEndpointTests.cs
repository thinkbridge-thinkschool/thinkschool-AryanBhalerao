using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuotesApi.Data;
using Xunit;

namespace Quotes.Tests.Integration.Tests;

// ── GET /api/quotes ───────────────────────────────────────────────────────────

public class GetQuotes_WhenEmpty : IntegrationTestBase
{
    [Fact]
    public async Task Returns200WithEmptyList()
    {
        var response = await Client.GetAsync("/api/quotes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(0);
        body.GetProperty("total").GetInt32().Should().Be(0);
    }
}

public class GetQuotes_AfterCreating : IntegrationTestBase
{
    [Fact]
    public async Task ReturnsQuoteInList()
    {
        var create = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        create.Headers.Add("X-Test-Claims", "sub=1,scope=quotes.write");
        create.Content = JsonContent.Create(new { Author = "Plato", Text = "Wise men speak because they have something to say." });
        (await Client.SendAsync(create)).EnsureSuccessStatusCode();

        var response = await Client.GetAsync("/api/quotes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("total").GetInt32().Should().Be(1);
        body.GetProperty("data").GetArrayLength().Should().Be(1);
    }
}

// ── GET /api/quotes/{id} ──────────────────────────────────────────────────────

public class GetQuoteById_WhenExists : IntegrationTestBase
{
    [Fact]
    public async Task Returns200WithQuote()
    {
        var create = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        create.Headers.Add("X-Test-Claims", "sub=5,scope=quotes.write");
        create.Content = JsonContent.Create(new { Author = "Aristotle", Text = "Excellence is not a gift, but a skill." });
        var createResp = await Client.SendAsync(create);
        var created    = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        int id = created.GetProperty("id").GetInt32();

        var response = await Client.GetAsync($"/api/quotes/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt32().Should().Be(id);
        body.GetProperty("author").GetString().Should().Be("Aristotle");
    }
}

public class GetQuoteById_WhenNotFound : IntegrationTestBase
{
    [Fact]
    public async Task Returns404()
    {
        var response = await Client.GetAsync("/api/quotes/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

// ── POST /api/quotes ──────────────────────────────────────────────────────────

public class PostQuote_Anonymous : IntegrationTestBase
{
    [Fact]
    public async Task Returns401()
    {
        // No X-Test-Claims header → TestAuthHandler returns NoResult → unauthenticated → 401
        var response = await Client.PostAsJsonAsync("/api/quotes",
            new { Author = "Seneca", Text = "Per aspera ad astra." });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public class PostQuote_AuthenticatedWithoutScopeClaim : IntegrationTestBase
{
    [Fact]
    public async Task Returns403()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        request.Headers.Add("X-Test-Claims", "sub=1"); // authenticated, but no scope=quotes.write
        request.Content = JsonContent.Create(new { Author = "Seneca", Text = "Dum spiro spero." });

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

public class PostQuote_WithScopeClaim : IntegrationTestBase
{
    [Fact]
    public async Task Returns201WithCreatedQuote()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        request.Headers.Add("X-Test-Claims", "sub=42,scope=quotes.write");
        request.Content = JsonContent.Create(new
        {
            Author = "Marcus Aurelius",
            Text   = "The impediment to action advances action."
        });

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("author").GetString().Should().Be("Marcus Aurelius");
        body.GetProperty("ownerId").GetString().Should().Be("42");
        response.Headers.Location.Should().NotBeNull();
    }
}

public class PostQuote_WithEmptyAuthor : IntegrationTestBase
{
    [Fact]
    public async Task ReturnsProblemDetails()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        request.Headers.Add("X-Test-Claims", "sub=1,scope=quotes.write");
        request.Content = JsonContent.Create(new { Author = "", Text = "Some text here." });

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("problem+json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("detail").GetString().Should()
            .Contain("Author must be between 1 and 200 characters");
    }
}

// ── DELETE /api/quotes/{id} ───────────────────────────────────────────────────

public class DeleteQuote_Anonymous : IntegrationTestBase
{
    [Fact]
    public async Task Returns401()
    {
        // RequireAuthorization() with no header → 401 before handler body runs
        var response = await Client.DeleteAsync("/api/quotes/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public class DeleteQuote_ByNonOwner : IntegrationTestBase
{
    [Fact]
    public async Task Returns403()
    {
        // Create as user 1
        var create = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        create.Headers.Add("X-Test-Claims", "sub=1,scope=quotes.write");
        create.Content = JsonContent.Create(new { Author = "Twain", Text = "Get your facts first." });
        var created = await (await Client.SendAsync(create)).Content.ReadFromJsonAsync<JsonElement>();
        int id = created.GetProperty("id").GetInt32();

        // Attempt delete as user 99 — resource-based ownership check must deny
        var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/quotes/{id}");
        delete.Headers.Add("X-Test-Claims", "sub=99,scope=quotes.write");
        var response = await Client.SendAsync(delete);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

public class DeleteQuote_ByOwner : IntegrationTestBase
{
    [Fact]
    public async Task Returns204()
    {
        var create = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        create.Headers.Add("X-Test-Claims", "sub=7,scope=quotes.write");
        create.Content = JsonContent.Create(new { Author = "Epictetus", Text = "Make the best use of what is in your power." });
        var created = await (await Client.SendAsync(create)).Content.ReadFromJsonAsync<JsonElement>();
        int id = created.GetProperty("id").GetInt32();

        var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/quotes/{id}");
        delete.Headers.Add("X-Test-Claims", "sub=7,scope=quotes.write"); // same user → 204
        var response = await Client.SendAsync(delete);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

public class DeleteQuote_WhenNotFound : IntegrationTestBase
{
    [Fact]
    public async Task Returns404()
    {
        var delete = new HttpRequestMessage(HttpMethod.Delete, "/api/quotes/99999");
        delete.Headers.Add("X-Test-Claims", "sub=1,scope=quotes.write");
        var response = await Client.SendAsync(delete);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

// ── EF migrations ─────────────────────────────────────────────────────────────

public class Migrations_AreApplied : IntegrationTestBase
{
    [Fact]
    public async Task NoPendingMigrations()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuoteDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync();

        pending.Should().BeEmpty();
    }
}
