using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuotesApi.Data;

namespace Quotes.Tests.Integration.Infrastructure;

/// <summary>
/// Boots the real app pipeline in-memory with two overrides:
///   1. SQLite DB replaced with a unique temp file per factory instance → full isolation.
///   2. All JWT schemes replaced with TestAuthHandler → tests drive identity via header.
///
/// xUnit creates a new test-class instance per test method, so constructing the factory
/// in the test class constructor (and disposing in Dispose()) gives each test its own DB.
/// </summary>
public sealed class QuotesApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"quotes-integration-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // ── Replace production SQLite with a per-factory temp database ─────
            var existing = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<QuoteDbContext>));
            if (existing is not null) services.Remove(existing);

            services.AddDbContext<QuoteDbContext>(opt =>
                opt.UseSqlite($"DataSource={_dbPath}"));

            // ── Replace all real JWT schemes with TestAuthHandler ──────────────
            // PostConfigure runs after the production AddAuthentication calls,
            // so it wins and sets the default to our scheme.
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultScheme             = TestAuthHandler.SchemeName;
                o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                o.DefaultChallengeScheme    = TestAuthHandler.SchemeName;
                o.DefaultForbidScheme       = TestAuthHandler.SchemeName;
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
        // Must clear SQLite connection pools before deleting the file on Windows;
        // open handles prevent deletion.
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}
