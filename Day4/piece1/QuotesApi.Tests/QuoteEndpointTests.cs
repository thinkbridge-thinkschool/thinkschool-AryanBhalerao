using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace QuotesApi.Tests;

public class QuoteEndpointTests : IClassFixture<QuotesApiFactory>
{
    private readonly HttpClient _client;

    public QuoteEndpointTests(QuotesApiFactory factory)
        => _client = factory.CreateClient();

    // ── GET /api/quotes ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetQuotes_ReturnsOkWithList()
    {
        var response = await _client.GetAsync("/api/quotes?page=1&size=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── GET /api/quotes/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task GetQuoteById_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/quotes/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetQuoteById_Existing_Returns200()
    {
        var create = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        create.Headers.Add("X-Test-Claims", "sub=5,scope=quotes.write");
        create.Content = JsonContent.Create(new { Author = "Aristotle", Text = "The whole is greater than the sum of its parts." });
        var createResp = await _client.SendAsync(create);
        createResp.EnsureSuccessStatusCode();
        var body = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        int id = body.GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/quotes/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── POST /api/quotes ──────────────────────────────────────────────────────

    [Fact]
    public async Task PostQuote_Anonymous_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/quotes",
            new { Author = "Seneca", Text = "Per aspera ad astra." });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostQuote_AuthenticatedWithoutScope_Returns403()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        request.Headers.Add("X-Test-Claims", "sub=1");
        request.Content = JsonContent.Create(new { Author = "Seneca", Text = "Per aspera ad astra." });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostQuote_AuthenticatedWithScope_Returns201()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        request.Headers.Add("X-Test-Claims", "sub=10,scope=quotes.write");
        request.Content = JsonContent.Create(new
        {
            Author = "Epictetus",
            Text = "Make the best use of what is in your power."
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostQuote_MissingAuthor_Returns400()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        request.Headers.Add("X-Test-Claims", "sub=10,scope=quotes.write");
        request.Content = JsonContent.Create(new { Author = "", Text = "Some text." });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── DELETE /api/quotes/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteQuote_Anonymous_Returns401()
    {
        var response = await _client.DeleteAsync("/api/quotes/999");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuote_ByNonOwner_Returns403()
    {
        var create = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        create.Headers.Add("X-Test-Claims", "sub=1,scope=quotes.write");
        create.Content = JsonContent.Create(new { Author = "Twain", Text = "Classic quote." });
        var createResp = await _client.SendAsync(create);
        createResp.EnsureSuccessStatusCode();
        var body = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        int id = body.GetProperty("id").GetInt32();

        var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/quotes/{id}");
        delete.Headers.Add("X-Test-Claims", "sub=2,scope=quotes.write");
        var response = await _client.SendAsync(delete);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuote_ByOwner_Returns204()
    {
        var create = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        create.Headers.Add("X-Test-Claims", "sub=42,scope=quotes.write");
        create.Content = JsonContent.Create(new
        {
            Author = "Marcus Aurelius",
            Text = "You have power over your mind, not outside events."
        });
        var createResp = await _client.SendAsync(create);
        createResp.EnsureSuccessStatusCode();
        var body = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        int id = body.GetProperty("id").GetInt32();

        var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/quotes/{id}");
        delete.Headers.Add("X-Test-Claims", "sub=42");
        var response = await _client.SendAsync(delete);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuote_NonExistent_Returns404()
    {
        var delete = new HttpRequestMessage(HttpMethod.Delete, "/api/quotes/99999");
        delete.Headers.Add("X-Test-Claims", "sub=1");

        var response = await _client.SendAsync(delete);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
