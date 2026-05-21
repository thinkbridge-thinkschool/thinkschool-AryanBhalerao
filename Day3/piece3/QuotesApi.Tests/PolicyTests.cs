using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QuotesApi.Data;
using Xunit;

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

// ── Factory: test handler replaces real JWT schemes ──────────────────────────
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
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}

// ── Factory: real JWT validation, no test-handler override ───────────────────
// Used to exercise actual token lifetime validation (expired-token tests).
public sealed class RealJwtFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"quotes-realjwt-{Guid.NewGuid()}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            var existing = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<QuoteDbContext>));
            if (existing is not null) services.Remove(existing);

            services.AddDbContext<QuoteDbContext>(opt =>
                opt.UseSqlite($"DataSource={_dbPath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}

// ── Policy & authorization tests ─────────────────────────────────────────────
public class PolicyTests : IClassFixture<QuotesApiFactory>
{
    private readonly HttpClient _client;

    public PolicyTests(QuotesApiFactory factory)
        => _client = factory.CreateClient();

    // ── Anonymous → 401 ──────────────────────────────────────────────────────

    [Fact]
    public async Task PostQuote_Anonymous_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/quotes",
            new { Author = "Seneca", Text = "Per aspera ad astra." });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuote_Anonymous_Returns401()
    {
        // Authorization middleware rejects unauthenticated requests before the
        // handler body runs, so the quote need not exist.
        var response = await _client.DeleteAsync("/api/quotes/999");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Authenticated but wrong policy → 403 ─────────────────────────────────

    [Fact]
    public async Task PostQuote_AuthenticatedWithoutScopeClaim_Returns403()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        request.Headers.Add("X-Test-Claims", "sub=1"); // no scope=quotes.write
        request.Content = JsonContent.Create(new { Author = "Seneca", Text = "Per aspera ad astra." });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuote_ByNonOwner_Returns403()
    {
        // Create a quote as user 1.
        var create = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        create.Headers.Add("X-Test-Claims", "sub=1,scope=quotes.write");
        create.Content = JsonContent.Create(new { Author = "Twain", Text = "Classic quote." });
        var createResp = await _client.SendAsync(create);
        createResp.EnsureSuccessStatusCode();

        var body = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        int id = body.GetProperty("id").GetInt32();

        // Attempt delete as user 2 — ownership check must deny.
        var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/quotes/{id}");
        delete.Headers.Add("X-Test-Claims", "sub=2,scope=quotes.write");
        var deleteResp = await _client.SendAsync(delete);

        Assert.Equal(HttpStatusCode.Forbidden, deleteResp.StatusCode);
    }

    // ── Authenticated + correct policy → 2xx ─────────────────────────────────

    [Fact]
    public async Task PostQuote_AuthenticatedWithScopeClaim_Returns201()
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
    public async Task DeleteQuote_ByOwner_Returns204()
    {
        // Create as user 42.
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

        // Delete as same user — must succeed.
        var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/quotes/{id}");
        delete.Headers.Add("X-Test-Claims", "sub=42,scope=quotes.write");
        var deleteResp = await _client.SendAsync(delete);

        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }
}

// ── Expired-token tests ───────────────────────────────────────────────────────
// Uses RealJwtFactory so the real InternalJwt bearer validator runs.
public class ExpiredTokenTests : IClassFixture<RealJwtFactory>
{
    private readonly HttpClient _client;

    // Must match appsettings.json exactly.
    private const string JwtKey      = "super-secret-jwt-key-for-development-only-256-bits";
    private const string JwtIssuer   = "QuotesApi";
    private const string JwtAudience = "QuotesApiUsers";

    public ExpiredTokenTests(RealJwtFactory factory)
        => _client = factory.CreateClient();

    private static string MintExpiredJwt()
    {
        var keyBytes = Encoding.UTF8.GetBytes(JwtKey);
        var past = DateTime.UtcNow.AddHours(-1);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, "1"),
                new Claim("scope", "quotes.write"),
            ]),
            NotBefore         = past,
            IssuedAt          = past,
            Expires           = past.AddMinutes(1),   // expired ~59 minutes ago
            Issuer            = JwtIssuer,
            Audience          = JwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256)
        };

        return new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityTokenHandler().CreateToken(descriptor));
    }

    [Fact]
    public async Task PostQuote_WithExpiredToken_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", MintExpiredJwt());
        request.Content = JsonContent.Create(new { Author = "Seneca", Text = "Dum spiro spero." });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

// ── Refresh-token rotation & reuse-detection tests ───────────────────────────
public class RefreshTokenTests : IClassFixture<QuotesApiFactory>
{
    private readonly HttpClient _client;

    public RefreshTokenTests(QuotesApiFactory factory)
        => _client = factory.CreateClient();

    private async Task<(string AccessToken, string RefreshToken)> LoginAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "Password123!" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return (
            body.GetProperty("access_token").GetString()!,
            body.GetProperty("refresh_token").GetString()!
        );
    }

    // A rotated (revoked) refresh token must be rejected with 401.
    [Fact]
    public async Task Refresh_WithRevokedToken_Returns401()
    {
        var (_, rt1) = await LoginAsync();

        // First rotation: rt1 becomes revoked, rt2 is issued.
        var rotateResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = rt1 });
        rotateResp.EnsureSuccessStatusCode();

        // Attempt to reuse the now-revoked rt1.
        var reuseResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = rt1 });

        Assert.Equal(HttpStatusCode.Unauthorized, reuseResp.StatusCode);
    }

    // Replaying a rotated token triggers reuse detection: the entire family is
    // revoked, so the current valid token in the same chain is also rejected.
    [Fact]
    public async Task Refresh_ReuseDetected_RevokesEntireFamily_Returns401()
    {
        var (_, rt1) = await LoginAsync();

        // First rotation: rt1 → rt2.
        var rotate1 = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = rt1 });
        rotate1.EnsureSuccessStatusCode();
        var body2 = await rotate1.Content.ReadFromJsonAsync<JsonElement>();
        var rt2 = body2.GetProperty("refresh_token").GetString()!;

        // Replay rt1 (already revoked) — triggers family-wide revocation.
        var replayResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = rt1 });
        Assert.Equal(HttpStatusCode.Unauthorized, replayResp.StatusCode);

        // rt2, though previously active, is now revoked as part of the same family.
        var rt2Resp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = rt2 });
        Assert.Equal(HttpStatusCode.Unauthorized, rt2Resp.StatusCode);
    }
}
