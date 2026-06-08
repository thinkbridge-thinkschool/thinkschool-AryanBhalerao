using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuotesApi.Authorization;
using QuotesApi.Commands;
using QuotesApi.Data;
using QuotesApi.Middleware;
using QuotesApi.Options;
using QuotesApi.Queries;
using QuotesApi.Repositories;
using QuotesApi.Services;

namespace QuotesApi.Extensions;

public static class InfrastructureExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")
                ?? "Server=.\\SQLEXPRESS;Database=QuotesApi;Trusted_Connection=True;TrustServerCertificate=True"));

        // Scoped: one instance per HTTP request — shares the open DbContext transaction
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuthorRepository, AuthorRepository>();

        // Singleton: stateless time source, safe to share across all requests and threads
        services.AddSingleton<IClock, SystemClock>();

        // Background work queue (singleton, shared by producers + the consumer) and
        // the hosted service that drains it off the request thread.
        services.AddSingleton<IBackgroundTaskQueue>(_ => new BackgroundTaskQueue(capacity: 100));
        services.AddHostedService<QueuedHostedService>();

        // CQRS-lite: separate write handler and read query service.
        services.AddScoped<CreateQuoteCommandHandler>();
        services.AddScoped<IQuoteQueryService, EfCoreQuoteQueryService>();
        services.AddScoped<IQuoteMetadataQueryService, EfCoreQuoteMetadataQueryService>();

        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:4200"];
        services.AddCors(options =>
        {
            options.AddPolicy("QuotesUiDev", policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        services.AddHealthChecks();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Bind typed options for IOptions<> injection across the app
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        // Read values at startup for authentication middleware setup
        var jwtOpts = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");
        if (string.IsNullOrEmpty(jwtOpts.SigningKey))
            throw new InvalidOperationException(
                "Jwt:SigningKey is not configured. Run: dotnet user-secrets set Jwt:SigningKey <value>");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.SigningKey));

        // Locally-issued JWTs (from POST /api/auth/login) are validated against the
        // symmetric signing key configured in user-secrets / appsettings.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOpts.Issuer,
                    ValidAudience = jwtOpts.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.Zero
                };
            });

        // OwnQuoteHandler is singleton: it has no state and handles resource-based auth for Quote deletion.
        services.AddSingleton<IAuthorizationHandler, OwnQuoteHandler>();

        services.AddAuthorization(options =>
        {
            // Policy 1 (claim-based): token must carry scope=quotes.write to mutate quotes.
            options.AddPolicy("can-edit-quotes", p => p.RequireClaim("scope", "quotes.write"));

            // Policy 2 (custom requirement): evaluated against the Quote resource in the endpoint;
            // OwnQuoteHandler succeeds only when quote.OwnerId matches the caller's sub claim.
            options.AddPolicy("can-delete-own-quote", p => p.AddRequirements(new OwnQuoteRequirement()));
        });
    }
}
