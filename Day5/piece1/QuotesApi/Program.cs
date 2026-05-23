using Azure.Identity;
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

        if (!db.Users.Any())
        {
            db.Users.Add(new User
            {
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
            });
            db.SaveChanges();
        }

        if (!db.Quotes.Any())
        {
            var ownerId = db.Users.First().Id;
            db.Quotes.AddRange(
                new Quote { Author = "Einstein",    Text = "Imagination is more important than knowledge.",          OwnerId = ownerId, CreatedAt = DateTimeOffset.UtcNow },
                new Quote { Author = "Churchill",   Text = "Success is not final, failure is not fatal.",            OwnerId = ownerId, CreatedAt = DateTimeOffset.UtcNow },
                new Quote { Author = "Twain",       Text = "The secret of getting ahead is getting started.",        OwnerId = ownerId, CreatedAt = DateTimeOffset.UtcNow },
                new Quote { Author = "Aristotle",   Text = "We are what we repeatedly do.",                         OwnerId = ownerId, CreatedAt = DateTimeOffset.UtcNow },
                new Quote { Author = "Seneca",      Text = "It is not that I am brave, it is just that I am busy.", OwnerId = ownerId, CreatedAt = DateTimeOffset.UtcNow },
                new Quote { Author = "Confucius",   Text = "It does not matter how slowly you go as long as you do not stop.", OwnerId = ownerId, CreatedAt = DateTimeOffset.UtcNow },
                new Quote { Author = "Newton",      Text = "If I have seen further it is by standing on the shoulders of giants.", OwnerId = ownerId, CreatedAt = DateTimeOffset.UtcNow },
                new Quote { Author = "Darwin",      Text = "It is not the strongest of the species that survives.", OwnerId = ownerId, CreatedAt = DateTimeOffset.UtcNow }
            );
            db.SaveChanges();
        }
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapAuthEndpoints();
    app.MapQuoteEndpoints();

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
