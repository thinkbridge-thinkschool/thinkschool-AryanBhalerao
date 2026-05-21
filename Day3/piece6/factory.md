# QuotesApiFactory

```csharp
// Infrastructure/QuotesApiFactory.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuotesApi.Data;

namespace Quotes.Tests.Integration.Infrastructure;
public sealed class QuotesApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"quotes-integration-{Guid.NewGuid():N}.db");

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
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}
```
