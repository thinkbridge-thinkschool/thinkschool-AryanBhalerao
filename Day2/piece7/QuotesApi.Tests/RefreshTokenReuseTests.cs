using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using QuotesApi.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace QuotesApi.Tests;

// ---------------------------------------------------------------------------
// Factory — in-memory SQLite so the test is hermetic.
// Program.cs startup runs db.Database.Migrate() + seeds the test user.
// ---------------------------------------------------------------------------
public sealed class AuthFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public AuthFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<QuoteDbContext>));
            if (dbDescriptor is not null) services.Remove(dbDescriptor);

            services.AddDbContext<QuoteDbContext>(o => o.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _connection.Dispose();
        base.Dispose(disposing);
    }
}

// ---------------------------------------------------------------------------
// Helper record for deserialising the token response.
// ---------------------------------------------------------------------------
file record TokenResponse(
    [property: JsonPropertyName("access_token")]  string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")]    int ExpiresIn);

// ---------------------------------------------------------------------------
// Reuse-detection tests
// ---------------------------------------------------------------------------
public class RefreshTokenReuseTests : IClassFixture<AuthFactory>
{
    private readonly HttpClient _client;

    public RefreshTokenReuseTests(AuthFactory factory)
        => _client = factory.CreateClient();

    // -----------------------------------------------------------------------
    // Core scenario:
    //   1. Login  → token1
    //   2. Refresh(token1) → token2   (token1 is now revoked + replaced)
    //   3. Refresh(token1) again      → 401, reuse detected, chain revoked
    //   4. Refresh(token2)            → 401 (caught in chain revocation)
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Reuse_Of_Rotated_Token_RevokesEntireChain()
    {
        // 1. Login
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        var tokens1 = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens1);

        // 2. First legitimate refresh — token1 consumed, token2 issued
        var refresh1Resp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = tokens1.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh1Resp.StatusCode);
        var tokens2 = await refresh1Resp.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens2);
        Assert.NotEqual(tokens1.RefreshToken, tokens2.RefreshToken);

        // 3. Replay token1 — this is the reuse attack
        //    Expected: 401 + the entire chain is revoked server-side
        var reuseResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = tokens1.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResp.StatusCode);

        // 4. token2 must now be dead (revoked as part of chain kill)
        var token2Resp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = tokens2.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, token2Resp.StatusCode);
    }

    // Happy-path sanity: a single refresh round-trip works before any reuse.
    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewPair()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "Password123!" });
        var tokens1 = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();

        var refreshResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = tokens1!.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshResp.StatusCode);

        var tokens2 = await refreshResp.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens2?.AccessToken);
        Assert.NotNull(tokens2?.RefreshToken);
    }

    // Logout revokes the token; subsequent refresh must be rejected.
    [Fact]
    public async Task Logout_RevokesToken_SubsequentRefreshFails()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "Password123!" });
        var tokens = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();

        var logoutResp = await _client.PostAsJsonAsync("/api/auth/logout",
            new { RefreshToken = tokens!.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logoutResp.StatusCode);

        var refreshResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResp.StatusCode);
    }
}
