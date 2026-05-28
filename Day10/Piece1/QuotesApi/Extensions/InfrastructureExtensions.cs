using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using QuotesApi.Authorization;
using QuotesApi.Data;
using QuotesApi.Endpoints;
using QuotesApi.Middleware;
using QuotesApi.Options;
using QuotesApi.Repositories;
using QuotesApi.Services;

namespace QuotesApi.Extensions;

public static class InfrastructureExtensions
{
    private const string LocalScheme = "LocalJwt";
    private const string EntraScheme = "EntraId";
    private const string MultiScheme = "MultiScheme";

    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Default") ?? "Data Source=quotes.db"));

        // Scoped: one instance per HTTP request — shares the open DbContext transaction
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Singleton: stateless time source, safe to share across all requests and threads
        services.AddSingleton<IClock, SystemClock>();

        // Transient: new instance per injection — validation is stateless and cheap to allocate
        services.AddTransient<IQuoteValidator, QuoteValidator>();

        var otel = services.AddOpenTelemetry()
            .WithTracing(t => t
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("QuotesApi"))
                .AddSource(QuoteEndpoints.ActivitySourceName)
                .AddSource(AuthEndpoints.ActivitySourceName)
                .AddAspNetCoreInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());

        var aiConnectionString = configuration["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrEmpty(aiConnectionString))
            otel.UseAzureMonitor(options => options.ConnectionString = aiConnectionString);

        services.AddHealthChecks();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Bind typed options for IOptions<> injection across the app
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<AzureAdOptions>(configuration.GetSection("AzureAd"));

        // Read values at startup for authentication middleware setup
        var jwtOpts = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");
        if (string.IsNullOrEmpty(jwtOpts.SigningKey))
            throw new InvalidOperationException(
                "Jwt:SigningKey is not configured. Run: dotnet user-secrets set Jwt:SigningKey <value>");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.SigningKey));

        var azureAdOpts = configuration.GetSection("AzureAd").Get<AzureAdOptions>()
            ?? throw new InvalidOperationException("AzureAd configuration section is missing.");
        if (string.IsNullOrEmpty(azureAdOpts.TenantId))
            throw new InvalidOperationException("AzureAd:TenantId is not configured.");
        if (string.IsNullOrEmpty(azureAdOpts.ClientId))
            throw new InvalidOperationException("AzureAd:ClientId is not configured.");

        // Named HttpClient for Entra ID OIDC discovery with Polly resilience:
        // retries, circuit breaker, and timeout wrap every call to
        // https://login.microsoftonline.com/{tenant}/v2.0/.well-known/openid-configuration
        services.AddHttpClient("entra-id")
            .AddResilienceHandler("default", (builder, ctx) =>
            {
                var logger = ctx.ServiceProvider
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Resilience.EntraId");

                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "Entra ID retry {Attempt} of 3 after {Delay:g}. " +
                            "Status: {Status}. Exception: {Exception}",
                            args.AttemptNumber + 1,
                            args.RetryDelay,
                            args.Outcome.Result?.StatusCode.ToString() ?? "n/a",
                            args.Outcome.Exception?.Message ?? "n/a");
                        return ValueTask.CompletedTask;
                    }
                });

                builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(30),
                    OnOpened = args =>
                    {
                        logger.LogError(
                            "Entra ID circuit breaker opened for {Duration:g}. " +
                            "Failure ratio exceeded 50%% over the last 30 s.",
                            args.BreakDuration);
                        return ValueTask.CompletedTask;
                    }
                });

                builder.AddTimeout(TimeSpan.FromSeconds(10));
            });

        // Plug the resilience-wrapped handler chain into JwtBearer's OIDC backchannel.
        // IHttpMessageHandlerFactory returns the full pipeline (Polly + HttpClientHandler)
        // without creating a managed HttpClient, which avoids double-lifetime conflicts.
        services.AddOptions<JwtBearerOptions>(EntraScheme)
            .Configure<IHttpMessageHandlerFactory>((opts, handlerFactory) =>
            {
                opts.BackchannelHttpHandler = handlerFactory.CreateHandler("entra-id");
            });

        services.AddAuthentication(MultiScheme)
            // Route to LocalJwt or EntraId based on the issuer claim in the incoming token.
            .AddPolicyScheme(MultiScheme, "Local or Entra JWT", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var auth = context.Request.Headers.Authorization.FirstOrDefault();
                    if (auth?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var raw = auth["Bearer ".Length..].Trim();
                        var handler = new JwtSecurityTokenHandler();
                        if (handler.CanReadToken(raw))
                        {
                            var issuer = handler.ReadJwtToken(raw).Issuer;
                            if (issuer.StartsWith("https://login.microsoftonline.com/", StringComparison.OrdinalIgnoreCase) ||
                                issuer.StartsWith("https://sts.windows.net/", StringComparison.OrdinalIgnoreCase))
                                return EntraScheme;
                        }
                    }
                    return LocalScheme;
                };
            })
            .AddJwtBearer(LocalScheme, options =>
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
            })
            .AddJwtBearer(EntraScheme, options =>
            {
                // OIDC discovery at {Authority}/.well-known/openid-configuration fetches
                // Entra's public signing keys automatically — no manual key management needed.
                options.Authority = $"https://login.microsoftonline.com/{azureAdOpts.TenantId}/v2.0";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = azureAdOpts.ClientId
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
