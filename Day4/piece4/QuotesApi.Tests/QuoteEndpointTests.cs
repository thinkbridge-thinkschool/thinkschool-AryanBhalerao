using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using QuotesApi.Data;
using QuotesApi.Models;

namespace QuotesApi.Tests;

public class QuoteEndpointTests : IClassFixture<QuotesApiFactory>
{
    private readonly QuotesApiFactory _factory;
    private readonly HttpClient _anonClient;

    public QuoteEndpointTests(QuotesApiFactory factory)
    {
        _factory = factory;
        _anonClient = factory.CreateClient();
    }

    private int GetSeedUserId()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return db.Users.First().Id;
    }

    private async Task<int> SeedQuoteForUser(int ownerId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var quote = new Quote
        {
            Author = "Seed Author",
            Text = "Seed text for test",
            OwnerId = ownerId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Quotes.Add(quote);
        await db.SaveChangesAsync();
        return quote.Id;
    }

    // ── GET /api/quotes ───────────────────────────────────────────────────

    [Fact]
    public async Task GetQuotes_Anonymous_Returns200()
    {
        var resp = await _anonClient.GetAsync("/api/quotes?page=1&size=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── GET /api/quotes/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task GetQuoteById_Exists_Returns200()
    {
        var id = await SeedQuoteForUser(GetSeedUserId());
        var resp = await _anonClient.GetAsync($"/api/quotes/{id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetQuoteById_NotFound_Returns404()
    {
        var resp = await _anonClient.GetAsync("/api/quotes/999999");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── POST /api/quotes ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateQuote_WithoutAuth_Returns401()
    {
        var resp = await _anonClient.PostAsJsonAsync("/api/quotes",
            new { author = "A", text = "T" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task CreateQuote_WithAuthButNoWriteScope_Returns403()
    {
        var client = _factory.CreateUserClient(1, "user@test.com", withWriteScope: false);
        var resp = await client.PostAsJsonAsync("/api/quotes", new { author = "A", text = "T" });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task CreateQuote_WithWriteScope_Returns201()
    {
        var client = _factory.CreateUserClient(GetSeedUserId(), "test@example.com");
        var resp = await client.PostAsJsonAsync("/api/quotes",
            new { author = "Confucius", text = "Life is simple." });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task CreateQuote_EmptyAuthor_Returns400()
    {
        var client = _factory.CreateUserClient(GetSeedUserId(), "test@example.com");
        var resp = await client.PostAsJsonAsync("/api/quotes",
            new { author = "", text = "Some text" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateQuote_EmptyText_Returns400()
    {
        var client = _factory.CreateUserClient(GetSeedUserId(), "test@example.com");
        var resp = await client.PostAsJsonAsync("/api/quotes",
            new { author = "Author", text = "" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── DELETE /api/quotes/{id} ───────────────────────────────────────────

    [Fact]
    public async Task DeleteQuote_WithoutAuth_Returns401()
    {
        var resp = await _anonClient.DeleteAsync("/api/quotes/1");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteQuote_NotFound_Returns404()
    {
        var client = _factory.CreateUserClient(GetSeedUserId(), "test@example.com");
        var resp = await client.DeleteAsync("/api/quotes/999998");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteQuote_AsOwner_Returns204()
    {
        var userId = GetSeedUserId();
        var quoteId = await SeedQuoteForUser(userId);
        var client = _factory.CreateUserClient(userId, "test@example.com");
        var resp = await client.DeleteAsync($"/api/quotes/{quoteId}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteQuote_AsNonOwner_Returns403()
    {
        var ownerId = GetSeedUserId();
        var quoteId = await SeedQuoteForUser(ownerId);
        // Different userId (99999) ≠ ownerId
        var attackerClient = _factory.CreateUserClient(99999, "attacker@test.com");
        var resp = await attackerClient.DeleteAsync($"/api/quotes/{quoteId}");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }
}
