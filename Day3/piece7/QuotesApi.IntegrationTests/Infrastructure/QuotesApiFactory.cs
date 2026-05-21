using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using QuotesApi.Data;

namespace QuotesApi.IntegrationTests.Infrastructure;

// Shared across every test class in the "Integration" collection — one container, one factory.
[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<QuotesApiFactory> { }

public sealed class QuotesApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Known key used both here (token generation) and injected via ConfigureAppConfiguration.
    private const string TestJwtKey = "test-super-secret-key-at-least-32-characters!!";
    private const string TestIssuer  = "quotes-api-test";
    private const string TestAudience = "quotes-api-test";

    private readonly SqlServerFixture _db = new();

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();           // 1. Spin up the SQL Server container

        _ = Services;                           // 2. Build the test host (Program.cs runs; migrations
                                                //    are skipped because env == "Testing")

        // 3. Create the schema from the EF Core model using SQL Server types.
        //    We use EnsureCreated() rather than Migrate() because the existing migrations
        //    were scaffolded against SQLite (TEXT / INTEGER column types) and would produce
        //    invalid DDL on SQL Server. EnsureCreated() derives the schema directly from the
        //    model, so SQL Server gets nvarchar / int / datetime2 — the right types.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuoteDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]      = TestJwtKey,
                ["Jwt:Issuer"]   = TestIssuer,
                ["Jwt:Audience"] = TestAudience,
            }));

        builder.ConfigureServices(services =>
        {
            // Remove the SQLite DbContext registration wired in InfrastructureExtensions.
            var opts = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<QuoteDbContext>));
            if (opts is not null) services.Remove(opts);

            // Replace with SQL Server pointed at the Testcontainers instance.
            // The lambda captures _db; the connection string is evaluated lazily
            // when the first DbContext is resolved (after InitializeAsync completes).
            services.AddDbContext<QuoteDbContext>(o =>
                o.UseSqlServer(_db.ConnectionString));
        });
    }

    // Truncate all data between tests so each test seeds only what it needs.
    public async Task CleanAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuoteDbContext>();

        // Order respects FK constraints: children before parents.
        await db.Database.ExecuteSqlRawAsync("DELETE FROM CollectionItems");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Collections");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM RefreshTokens");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Users");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Quotes");
    }

    // Convenience: add rows directly via EF without going through HTTP.
    public async Task<T> SeedAsync<T>(Func<QuoteDbContext, Task<T>> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuoteDbContext>();
        var result = await seed(db);
        await db.SaveChangesAsync();
        return result;
    }

    // Generate a signed JWT accepted by the app's InternalJwt scheme.
    // Includes both "sub" (standard JWT name) and ClaimTypes.NameIdentifier (XML namespace
    // form) so the token works regardless of whether MapInboundClaims is enabled.
    public string GenerateBearerToken(string userId, params string[] scopes)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
        };

        foreach (var scope in scopes)
            claims.Add(new Claim("scope", scope));

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }
}
