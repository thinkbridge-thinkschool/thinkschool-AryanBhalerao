using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace QuotesApi.Tests;

/// <summary>
/// Integration test factory. Boots the real app against an isolated per-run SQLite DB.
/// Authentication is replaced with TestAuthHandler so tests control claims via headers.
/// PostConfigure overrides the default scheme after InfrastructureExtensions sets it.
/// </summary>
public class QuotesApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"quotesapi-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}",
                ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000001",
                ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000002",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Register the test handler scheme
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            // PostConfigure runs after ALL Configure<AuthenticationOptions> calls —
            // this guarantees we win over InfrastructureExtensions' MultiScheme setup.
            services.PostConfigure<AuthenticationOptions>(opts =>
            {
                opts.DefaultScheme = TestAuthHandler.SchemeName;
                opts.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                opts.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                opts.DefaultForbidScheme = TestAuthHandler.SchemeName;
            });
        });
    }

    /// <summary>Helper: authenticated client with the given userId and optional scope.</summary>
    public HttpClient CreateUserClient(int userId, string email, bool withWriteScope = true)
    {
        var claims = new List<object>
        {
            new { type = System.Security.Claims.ClaimTypes.NameIdentifier, value = userId.ToString() },
            new { type = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, value = email },
        };
        if (withWriteScope)
            claims.Add(new { type = "scope", value = "quotes.write" });

        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", JsonSerializer.Serialize(claims));
        return client;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
