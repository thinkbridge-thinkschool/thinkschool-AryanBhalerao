using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Quotes.Tests.Integration.Tests;

// ── POST /api/auth/login ──────────────────────────────────────────────────────

public class Login_WithValidCredentials : IntegrationTestBase
{
    [Fact]
    public async Task Returns200WithTokens()
    {
        // The factory runs db.Database.Migrate() and seeds test@example.com on startup.
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "Password123!" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("access_token").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refresh_token").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("expires_in").GetInt32().Should().BePositive();
    }
}

public class Login_WithWrongPassword : IntegrationTestBase
{
    [Fact]
    public async Task Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "WrongPassword!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public class Login_WithUnknownEmail : IntegrationTestBase
{
    [Fact]
    public async Task Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new { Email = "nobody@example.com", Password = "Password123!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

// ── POST /api/auth/logout ─────────────────────────────────────────────────────

public class Logout_WithValidToken : IntegrationTestBase
{
    [Fact]
    public async Task Returns204()
    {
        var loginResp = await Client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "Password123!" });
        var loginBody    = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginBody.GetProperty("refresh_token").GetString()!;

        var response = await Client.PostAsJsonAsync("/api/auth/logout",
            new { RefreshToken = refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

// ── POST /api/auth/refresh ────────────────────────────────────────────────────

public class Refresh_WithValidToken : IntegrationTestBase
{
    [Fact]
    public async Task Returns200WithNewTokens()
    {
        var loginResp = await Client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "Password123!" });
        var loginBody = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var rt1 = loginBody.GetProperty("refresh_token").GetString()!;

        var response = await Client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = rt1 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var rt2  = body.GetProperty("refresh_token").GetString()!;
        // Rotated token must be different from the consumed one.
        rt2.Should().NotBe(rt1);
        body.GetProperty("access_token").GetString().Should().NotBeNullOrEmpty();
    }
}

public class Refresh_WithRevokedToken : IntegrationTestBase
{
    [Fact]
    public async Task Returns401()
    {
        var loginResp = await Client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "Password123!" });
        var loginBody = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var rt1 = loginBody.GetProperty("refresh_token").GetString()!;

        // Rotate rt1 → it becomes revoked
        (await Client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = rt1 }))
            .EnsureSuccessStatusCode();

        // Re-using the already-rotated token must be rejected
        var response = await Client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = rt1 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
