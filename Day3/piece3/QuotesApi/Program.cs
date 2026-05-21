using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using QuotesApi.Authorization;
using QuotesApi.Data;
using QuotesApi.Extensions;
using QuotesApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

// ── Authentication ───────────────────────────────────────────────────────────
const string InternalScheme = "InternalJwt";
const string EntraScheme    = "EntraJwt";
const string MultiScheme    = "MultiScheme";

var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyBytes    = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
var azSettings  = builder.Configuration.GetSection("AzureAd");
var tenantId    = azSettings["TenantId"]!;

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme          = MultiScheme;
        options.DefaultChallengeScheme = MultiScheme;
    })
    .AddJwtBearer(InternalScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer           = true,
            ValidIssuer              = jwtSettings["Issuer"],
            ValidateAudience         = true,
            ValidAudience            = jwtSettings["Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    })
    .AddJwtBearer(EntraScheme, options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.Audience  = azSettings["Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuers = [
                $"https://login.microsoftonline.com/{tenantId}/v2.0",
                $"https://sts.windows.net/{tenantId}/"
            ]
        };
    })
    .AddPolicyScheme(MultiScheme, MultiScheme, options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
            {
                var token   = authHeader["Bearer ".Length..];
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var issuer = handler.ReadJwtToken(token).Issuer;
                    if (issuer.Contains("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase) ||
                        issuer.Contains("sts.windows.net", StringComparison.OrdinalIgnoreCase))
                        return EntraScheme;
                }
            }
            return InternalScheme;
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Policy 1 — claim-based: token must carry scope=quotes.write
    options.AddPolicy("can-edit-quotes", p =>
        p.RequireClaim("scope", "quotes.write"));

    // Policy 2 — resource-based: caller's sub must match Quote.OwnerId
    options.AddPolicy("quote-owner", p =>
        p.AddRequirements(new QuoteOwnerRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, QuoteOwnerHandler>();

// ── App pipeline ─────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QuoteDbContext>();
    db.Database.Migrate();

    if (!db.Users.Any())
    {
        db.Users.Add(User.Create("test@example.com", BCrypt.Net.BCrypt.HashPassword("Password123!")));
        db.SaveChanges();
    }
}

app.MapAuthEndpoints();
app.MapQuoteEndpoints();
app.MapCollectionEndpoints();

app.Run();

public partial class Program { }
