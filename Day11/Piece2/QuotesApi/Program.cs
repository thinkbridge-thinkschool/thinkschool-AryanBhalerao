using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;
using QuotesApi.Endpoints;
using QuotesApi.Extensions;
using QuotesApi.Models;
using Serilog;
using Serilog.Context;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var keyVaultUri = builder.Configuration["KeyVault:Uri"];
    if (!string.IsNullOrEmpty(keyVaultUri))
        builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());

    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    // Push ASP.NET Core's TraceIdentifier into every log event for this request.
    app.Use(async (ctx, next) =>
    {
        using (LogContext.PushProperty("TraceId", ctx.TraceIdentifier))
            await next(ctx);
    });

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        // Recreate as a covering index (AuthorId key + Text/CreatedAt included).
        // Idempotent: drops the old narrow index if present, then creates the covering one.
        // EnsureCreated() is a no-op on existing DBs, so we manage this explicitly.
        db.Database.ExecuteSqlRaw("""
            IF EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'IX_Quotes_AuthorId'
                  AND object_id = OBJECT_ID(N'[dbo].[Quotes]'))
                DROP INDEX [IX_Quotes_AuthorId] ON [dbo].[Quotes];
            CREATE NONCLUSTERED INDEX [IX_Quotes_AuthorId]
                ON [dbo].[Quotes] ([AuthorId] ASC)
                INCLUDE ([Text], [CreatedAt]);
            """);

        if (!db.Users.Any())
        {
            db.Users.Add(new User
            {
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
            });
            db.SaveChanges();
        }

        if (!db.Authors.Any())
        {
            var authorNames = new[]
            {
                "Marcus Aurelius", "Seneca", "Epictetus", "Aristotle", "Plato",
                "Socrates", "Friedrich Nietzsche", "Immanuel Kant", "René Descartes", "John Locke",
                "David Hume", "Voltaire", "Jean-Paul Sartre", "Albert Camus", "Simone de Beauvoir",
                "Bertrand Russell", "Ludwig Wittgenstein", "Francis Bacon", "Thomas Hobbes", "John Stuart Mill"
            };

            var authors = authorNames.Select(name => new Author { Name = name }).ToList();
            db.Authors.AddRange(authors);
            db.SaveChanges();

            var quoteTemplates = new[]
            {
                "The impediment to action advances action. What stands in the way becomes the way.",
                "Waste no more time arguing what a good man should be. Be one.",
                "You have power over your mind, not outside events. Realize this, and you will find strength.",
                "The first rule is to keep an untroubled spirit. The second is to look things in the face and know them for what they are.",
                "Everything we hear is an opinion, not a fact. Everything we see is a perspective, not the truth.",
                "Luck is what happens when preparation meets opportunity.",
                "We suffer more in imagination than in reality.",
                "It is not that I am so smart, it is just that I stay with problems longer.",
                "The unexamined life is not worth living.",
                "Happiness is the meaning and the purpose of life, the whole aim and end of human existence.",
                "The roots of education are bitter, but the fruit is sweet.",
                "Knowing yourself is the beginning of all wisdom.",
                "The only true wisdom is in knowing you know nothing.",
                "Excellence is never an accident; it is the result of high intention and sincere effort.",
                "He who is not a good servant will not be a good master.",
            };

            var rng = new Random(42);
            var quotes = Enumerable.Range(1, 500).Select(i =>
            {
                var author = authors[i % authors.Count];
                var template = quoteTemplates[i % quoteTemplates.Length];
                return new Quote
                {
                    Author = author.Name,
                    AuthorId = author.Id,
                    Text = $"[{i}] {template}",
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-i),
                    OwnerId = null
                };
            }).ToList();

            db.Quotes.AddRange(quotes);
            db.SaveChanges();
        }
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health");
    app.MapAuthEndpoints();
    app.MapQuoteEndpoints();
    app.MapAuthorEndpoints();

    // Needed so WebApplicationFactory<Program> in integration tests can reference this type.
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
