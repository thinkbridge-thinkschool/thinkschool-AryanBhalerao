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

        // Repositories (scoped)
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuthorRepository, AuthorRepository>();

        services.AddSingleton<IClock, SystemClock>();

        // Background work queue
        services.AddSingleton<IBackgroundTaskQueue>(_ => new BackgroundTaskQueue(capacity: 100));
        services.AddHostedService<QueuedHostedService>();

        // CQRS-lite write + read services
        services.AddScoped<CreateQuoteCommandHandler>();
        services.AddScoped<IQuoteMetadataQueryService, EfCoreQuoteMetadataQueryService>();

        // Cached hot read
        services.Configure<QuoteCacheOptions>(configuration.GetSection("QuoteCache"));
        var cacheOpts = configuration.GetSection("QuoteCache").Get<QuoteCacheOptions>()
            ?? new QuoteCacheOptions();

        services.AddSingleton<ReadStats>();

        // L2: Redis-backed distributed cache
        services.AddStackExchangeRedisCache(o =>
        {
            o.Configuration = cacheOpts.RedisConnection;
            o.InstanceName = "quotes:";
        });
        services.AddHybridCache();

        // EF service + caching decorator
        services.AddScoped<EfCoreQuoteQueryService>();
        services.AddScoped<IQuoteQueryService, CachedQuoteQueryService>();

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

        // JWT options
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        var jwtOpts = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");
        if (string.IsNullOrEmpty(jwtOpts.SigningKey))
            throw new InvalidOperationException(
                "Jwt:SigningKey is not configured. Run: dotnet user-secrets set Jwt:SigningKey <value>");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.SigningKey));

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

        services.AddSingleton<IAuthorizationHandler, OwnQuoteHandler>();

        services.AddAuthorization(options =>
        {
            // Claim-based policy
            options.AddPolicy("can-edit-quotes", p => p.RequireClaim("scope", "quotes.write"));

            // Resource-based policy
            options.AddPolicy("can-delete-own-quote", p => p.AddRequirements(new OwnQuoteRequirement()));
        });
    }
}
