using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using QuotesApi.Data;
using QuotesApi.IntegrationTests.Infrastructure;
using QuotesApi.Models;
using Xunit;

namespace QuotesApi.IntegrationTests.Tests;

[Collection("Integration")]
public sealed class QuotesEndpointTests : IAsyncLifetime
{
    private readonly QuotesApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public QuotesEndpointTests(QuotesApiFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.CleanAsync();
    public Task DisposeAsync()    => Task.CompletedTask;

    // ── GET /api/quotes ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetQuotes_WithSeededData_ReturnsAll()
    {
        await _factory.SeedAsync(async db =>
        {
            db.Quotes.AddRange(
                Quote.Create("Einstein", "Imagination is more important than knowledge.", "u1").Value!,
                Quote.Create("Twain",    "The secret of getting ahead is getting started.",  "u1").Value!
            );
            return 0;
        });

        var response = await _client.GetAsync("/api/quotes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ParseAsync(response);
        Assert.Equal(2, body.GetProperty("total").GetInt32());
        Assert.Equal(2, body.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task GetQuotes_EmptyDatabase_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/quotes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ParseAsync(response);
        Assert.Equal(0, body.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task GetQuotes_Pagination_RespectsPageSize()
    {
        await _factory.SeedAsync(async db =>
        {
            for (int i = 1; i <= 5; i++)
                db.Quotes.Add(Quote.Create($"Author{i}", $"Quote number {i}", "u1").Value!);
            return 0;
        });

        var response = await _client.GetAsync("/api/quotes?page=1&size=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ParseAsync(response);
        Assert.Equal(5, body.GetProperty("total").GetInt32());
        Assert.Equal(2, body.GetProperty("data").GetArrayLength());
    }

    // ── GET /api/quotes/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task GetQuoteById_ExistingId_ReturnsQuote()
    {
        var id = await _factory.SeedAsync(async db =>
        {
            var quote = Quote.Create("Seneca", "We suffer more in imagination than in reality.", "u1").Value!;
            db.Quotes.Add(quote);
            await db.SaveChangesAsync();
            return quote.Id;
        });

        var response = await _client.GetAsync($"/api/quotes/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ParseAsync(response);
        Assert.Equal("Seneca", body.GetProperty("author").GetString());
    }

    [Fact]
    public async Task GetQuoteById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/quotes/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetQuoteById_SoftDeletedQuote_Returns404()
    {
        // Soft-deleted quotes are hidden by EF global filter — SQL Server must honour it.
        var id = await _factory.SeedAsync(async db =>
        {
            var quote = Quote.Create("Ghost", "You should not see me.", "u1").Value!;
            quote.SoftDelete();
            db.Quotes.Add(quote);
            await db.SaveChangesAsync();
            return quote.Id;
        });

        var response = await _client.GetAsync($"/api/quotes/{id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── POST /api/quotes ─────────────────────────────────────────────────────

    [Fact]
    public async Task PostQuote_ValidTokenWithWriteScope_Returns201()
    {
        var token = _factory.GenerateBearerToken("user-abc", "quotes.write");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new StringContent(
            """{"author":"Marcus Aurelius","text":"You have power over your mind, not outside events."}""",
            Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/quotes", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await ParseAsync(response);
        Assert.Equal("Marcus Aurelius", body.GetProperty("author").GetString());
        Assert.True(body.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task PostQuote_MissingScope_Returns403()
    {
        // Token exists but lacks quotes.write scope.
        var token = _factory.GenerateBearerToken("user-xyz");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new StringContent(
            """{"author":"Plato","text":"Wise men speak because they have something to say."}""",
            Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/quotes", payload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostQuote_Unauthenticated_Returns401()
    {
        var payload = new StringContent(
            """{"author":"Aristotle","text":"Excellence is never an accident."}""",
            Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/quotes", payload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── DELETE /api/quotes/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteQuote_ByOwner_Returns204()
    {
        const string ownerId = "owner-delete-test";

        var id = await _factory.SeedAsync(async db =>
        {
            var quote = Quote.Create("Epictetus", "Make the best use of what is in your power.", ownerId).Value!;
            db.Quotes.Add(quote);
            await db.SaveChangesAsync();
            return quote.Id;
        });

        var token = _factory.GenerateBearerToken(ownerId, "quotes.write");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.DeleteAsync($"/api/quotes/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's gone (soft-deleted).
        var getResponse = await _client.GetAsync($"/api/quotes/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteQuote_ByNonOwner_Returns403()
    {
        var id = await _factory.SeedAsync(async db =>
        {
            var quote = Quote.Create("Heraclitus", "Change is the only constant.", "real-owner").Value!;
            db.Quotes.Add(quote);
            await db.SaveChangesAsync();
            return quote.Id;
        });

        var token = _factory.GenerateBearerToken("different-user", "quotes.write");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.DeleteAsync($"/api/quotes/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static async Task<JsonElement> ParseAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
    }
}
