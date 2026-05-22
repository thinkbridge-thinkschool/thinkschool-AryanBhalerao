using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace QuotesApi.Tests;

public class AuthEndpointTests : IClassFixture<QuotesApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(QuotesApiFactory factory)
        => _client = factory.CreateClient();

    private async Task<(string AccessToken, string RefreshToken)> LoginAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "password123" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return (
            body.GetProperty("access_token").GetString()!,
            body.GetProperty("refresh_token").GetString()!
        );
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "wrongpassword" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "nobody@example.com", Password = "password123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "password123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("access_token").GetString()));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("refresh_token").GetString()));
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refresh_token = "not-a-real-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        var (_, rt) = await LoginAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refresh_token = rt });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("access_token").GetString()));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("refresh_token").GetString()));
    }

    [Fact]
    public async Task Refresh_RevokedToken_Returns401()
    {
        var (_, rt) = await LoginAsync();

        // First rotation — rt is now revoked.
        var first = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refresh_token = rt });
        first.EnsureSuccessStatusCode();

        // Reuse the revoked token.
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refresh_token = rt });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ReuseDetected_RevokesEntireFamily()
    {
        var (_, rt1) = await LoginAsync();

        var rotate1 = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refresh_token = rt1 });
        rotate1.EnsureSuccessStatusCode();
        var body2 = await rotate1.Content.ReadFromJsonAsync<JsonElement>();
        var rt2 = body2.GetProperty("refresh_token").GetString()!;

        // Replaying rt1 triggers family-wide revocation.
        var replay = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refresh_token = rt1 });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // rt2 is also revoked as part of the same family.
        var rt2Resp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refresh_token = rt2 });
        Assert.Equal(HttpStatusCode.Unauthorized, rt2Resp.StatusCode);
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_ValidRefreshToken_Returns204()
    {
        var (_, rt) = await LoginAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Add("X-Test-Claims", "sub=1");
        request.Content = JsonContent.Create(new { refresh_token = rt });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
