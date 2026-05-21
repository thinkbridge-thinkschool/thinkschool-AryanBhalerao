# Testcontainers Fixture

## SqlServerFixture

```csharp
// QuotesApi.IntegrationTests/Infrastructure/SqlServerFixture.cs
using Testcontainers.MsSql;

namespace QuotesApi.IntegrationTests.Infrastructure;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
```

`MsSqlBuilder` defaults to a random host port and the `sa` password embedded in the image; `GetConnectionString()` returns the full ADO.NET connection string once the container is healthy.

---

## QuotesApiFactory (WebApplicationFactory)

```csharp
// QuotesApi.IntegrationTests/Infrastructure/QuotesApiFactory.cs
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

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<QuotesApiFactory> { }

public sealed class QuotesApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestJwtKey   = "test-super-secret-key-at-least-32-characters!!";
    private const string TestIssuer   = "quotes-api-test";
    private const string TestAudience = "quotes-api-test";

    private readonly SqlServerFixture _db = new();

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();   // start container

        _ = Services;                  // build app (Program.cs runs; migrations skipped in "Testing" env)

        // EnsureCreated() derives the schema from the EF Core model with SQL Server types.
        // We don't call Migrate() because the existing migrations were scaffolded against
        // SQLite (TEXT / INTEGER column types) and would produce invalid DDL on SQL Server.
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
            // Remove the SQLite registration added by InfrastructureExtensions.
            var opts = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<QuoteDbContext>));
            if (opts is not null) services.Remove(opts);

            // Replace with SQL Server pointed at the Testcontainers instance.
            services.AddDbContext<QuoteDbContext>(o =>
                o.UseSqlServer(_db.ConnectionString));
        });
    }

    public async Task CleanAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuoteDbContext>();

        await db.Database.ExecuteSqlRawAsync("DELETE FROM CollectionItems");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Collections");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM RefreshTokens");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Users");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Quotes");
    }

    public async Task<T> SeedAsync<T>(Func<QuoteDbContext, Task<T>> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuoteDbContext>();
        var result = await seed(db);
        await db.SaveChangesAsync();
        return result;
    }

    public string GenerateBearerToken(string userId, params string[] scopes)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),   // covers both MapInboundClaims modes
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
```

---

## Test class pattern

```csharp
[Collection("Integration")]
public sealed class QuotesEndpointTests : IAsyncLifetime
{
    private readonly QuotesApiFactory _factory;
    private readonly HttpClient _client;

    public QuotesEndpointTests(QuotesApiFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.CleanAsync();   // fresh slate per test
    public Task DisposeAsync()    => Task.CompletedTask;

    [Fact]
    public async Task GetQuotes_WithSeededData_ReturnsAll()
    {
        await _factory.SeedAsync(async db =>
        {
            db.Quotes.Add(Quote.Create("Einstein", "Imagination over knowledge.", "u1").Value!);
            return 0;
        });

        var response = await _client.GetAsync("/api/quotes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```