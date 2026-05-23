using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using QuotesApi.Data;
using QuotesApi.Models;
using QuotesApi.Services;

namespace QuotesApi.Tests;

/// <summary>
/// Tests for /api/auth/login, /api/auth/refresh, and /api/auth/logout.
/// Authentication is not required for login/refresh, so those use the plain client.
/// Logout requires auth — uses the TestAuthHandler-backed client.
/// </summary>
public class AuthEndpointTests : IClassFixture<QuotesApiFactory>
{
    private readonly HttpClient _client;
    private readonly QuotesApiFactory _factory;

    public AuthEndpointTests(QuotesApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Login ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "test@example.com", password = "password123" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.AccessToken));
        Assert.False(string.IsNullOrEmpty(body.RefreshToken));
        Assert.True(body.ExpiresIn > 0);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "test@example.com", password = "wrong!" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@example.com", password = "password123" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Refresh ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_Returns200WithNewTokens()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "test@example.com", password = "password123" });
        var login = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        var refreshResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refresh_token = login!.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, refreshResp.StatusCode);
        var body = await refreshResp.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        // New refresh token is issued (rotation)
        Assert.NotEqual(login.RefreshToken, body!.RefreshToken);
    }

    [Fact]
    public async Task Refresh_UnknownToken_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refresh_token = "totally-unknown-token" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Refresh_ExpiredToken_Returns401()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.First();

        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = TokenHasher.Hash(raw),
            UserId = user.Id,
            FamilyId = Guid.NewGuid().ToString(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
        });
        await db.SaveChangesAsync();

        var resp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refresh_token = raw });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Refresh_ReuseDetected_Returns401AndRevokesFamily()
    {
        // Login → token1. Refresh with token1 → token2.
        // Refresh again with token1 (now revoked) → reuse detection → 401.
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "test@example.com", password = "password123" });
        var login = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        var token1 = login!.RefreshToken;

        await _client.PostAsJsonAsync("/api/auth/refresh", new { refresh_token = token1 });

        var reuseResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refresh_token = token1 });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResp.StatusCode);
    }

    [Fact]
    public async Task Refresh_AfterFamilyRevocation_Returns401()
    {
        // After reuse detection revokes the whole family, even a VALID token in that family is rejected.
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "test@example.com", password = "password123" });
        var login = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        var token1 = login!.RefreshToken;

        // Rotate: token1 → token2
        var refreshResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refresh_token = token1 });
        var refresh = await refreshResp.Content.ReadFromJsonAsync<LoginResponse>();
        var token2 = refresh!.RefreshToken;

        // Trigger reuse detection (re-use token1)
        await _client.PostAsJsonAsync("/api/auth/refresh", new { refresh_token = token1 });

        // token2 is also in the same family — should now be revoked
        var resp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refresh_token = token2 });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Logout ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_WithoutAuth_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/logout",
            new { refresh_token = "whatever" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidToken_Returns204()
    {
        // Get a refresh token via login, then log out using an authenticated (test) client.
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "test@example.com", password = "password123" });
        var login = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        // TestAuthHandler-backed client — authenticated without needing a real JWT.
        var authedClient = _factory.CreateClient();
        authedClient.DefaultRequestHeaders.Add("X-Test-Claims", "[]");

        var resp = await authedClient.PostAsJsonAsync("/api/auth/logout",
            new { refresh_token = login!.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task Logout_AlreadyRevokedToken_IsIdempotent()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "test@example.com", password = "password123" });
        var login = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        var refreshToken = login!.RefreshToken;

        var authedClient = _factory.CreateClient();
        authedClient.DefaultRequestHeaders.Add("X-Test-Claims", "[]");

        var first = await authedClient.PostAsJsonAsync("/api/auth/logout",
            new { refresh_token = refreshToken });
        var second = await authedClient.PostAsJsonAsync("/api/auth/logout",
            new { refresh_token = refreshToken });

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }
}
