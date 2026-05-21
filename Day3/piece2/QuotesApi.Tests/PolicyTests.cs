using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuotesApi.Data;

namespace QuotesApi.Tests;

// ── Test auth handler ────────────────────────────────────────────────────────
// Reads "X-Test-Claims: sub=1,scope=quotes.write" and builds a ClaimsPrincipal.
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Claims", out var raw))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = raw.ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(kv => kv.Length == 2)
            .Select(kv => kv[0].Trim() == "sub"
                ? new Claim(ClaimTypes.NameIdentifier, kv[1].Trim())
                : new Claim(kv[0].Trim(), kv[1].Trim()))
            .ToList();

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

// ── WebApplicationFactory ────────────────────────────────────────────────────
public sealed class QuotesApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"quotes-test-{Guid.NewGuid()}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Replace production SQLite with an isolated temp database.
            var existing = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<QuoteDbContext>));
            if (existing is not null) services.Remove(existing);

            services.AddDbContext<QuoteDbContext>(opt =>
                opt.UseSqlite($"DataSource={_dbPath}"));

            // Override the default auth scheme so the test handler runs first.
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultScheme               = TestAuthHandler.SchemeName;
                o.DefaultAuthenticateScheme   = TestAuthHandler.SchemeName;
                o.DefaultChallengeScheme      = TestAuthHandler.SchemeName;
                o.DefaultForbidScheme         = TestAuthHandler.SchemeName;
            });

            services
                .AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}

// ── Tests ────────────────────────────────────────────────────────────────────
public class PolicyTests : IClassFixture<QuotesApiFactory>
{
    private readonly HttpClient _client;

    public PolicyTests(QuotesApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // Policy: "can-edit-quotes" — requires claim scope=quotes.write
    // Authenticated user WITHOUT the scope claim must receive 403.
    [Fact]
    public async Task PostQuote_AuthenticatedWithoutScopeClaim_Returns403()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        request.Headers.Add("X-Test-Claims", "sub=1"); // no scope claim
        request.Content = JsonContent.Create(new { Author = "Seneca", Text = "Per aspera ad astra." });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Policy: "quote-owner" (QuoteOwnerRequirement)
    // A user who did NOT create the quote must receive 403 on DELETE.
    [Fact]
    public async Task DeleteQuote_ByNonOwner_Returns403()
    {
        // Create a quote as user 1 (has the write scope).
        var create = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        create.Headers.Add("X-Test-Claims", "sub=1,scope=quotes.write");
        create.Content = JsonContent.Create(new { Author = "Twain", Text = "Classic quote." });
        var createResp = await _client.SendAsync(create);
        createResp.EnsureSuccessStatusCode();

        var body = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        int id = body.GetProperty("id").GetInt32();

        // Attempt delete as user 2 — different sub, ownership check must fail.
        var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/quotes/{id}");
        delete.Headers.Add("X-Test-Claims", "sub=2,scope=quotes.write");
        var deleteResp = await _client.SendAsync(delete);

        Assert.Equal(HttpStatusCode.Forbidden, deleteResp.StatusCode);
    }
}
