using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuotesApi.Data;
using QuotesApi.Models;
using QuotesApi.Repositories;
using QuotesApi.Services;

namespace QuotesApi.Endpoints;

public static class AuthEndpoints
{
    public const string ActivitySourceName = "QuotesApi.Auth";
    private static readonly ActivitySource Source = new(ActivitySourceName);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            AppDbContext db,
            IRefreshTokenRepository tokenRepo,
            IClock clock,
            IConfiguration config,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("QuotesApi.Auth");

            using var activity = Source.StartActivity("login");

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                logger.LogWarning("Login failed for email {Email}", request.Email);
                activity?.SetTag("result", "failure");
                return Results.Unauthorized();
            }

            using var mintActivity = Source.StartActivity("mint-tokens");
            mintActivity?.SetTag("user.id", user.Id);

            var accessToken = MintAccessToken(user, config);
            var (rawRefresh, refreshEntity) = MintRefreshToken(user.Id, Guid.NewGuid().ToString(), clock, config);
            await tokenRepo.AddAsync(refreshEntity);
            mintActivity?.SetTag("family.id", refreshEntity.FamilyId);

            activity?.SetTag("result", "success");
            activity?.SetTag("user.id", user.Id);
            logger.LogInformation("Login succeeded for user {UserId} ({Email})", user.Id, user.Email);
            return Results.Ok(new LoginResponse(accessToken, rawRefresh, AccessExpirySeconds(config)));
        });

        app.MapPost("/api/auth/refresh", async (
            RefreshRequest request,
            IRefreshTokenRepository tokenRepo,
            IClock clock,
            IConfiguration config,
            ILoggerFactory loggerFactory,
            AppDbContext db) =>
        {
            var logger = loggerFactory.CreateLogger("QuotesApi.Auth");

            using var activity = Source.StartActivity("refresh-token");

            var incomingHash = TokenHasher.Hash(request.RefreshToken);
            var stored = await tokenRepo.FindByHashAsync(incomingHash);

            if (stored is null)
            {
                activity?.SetTag("result", "not_found");
                return Results.Unauthorized();
            }

            activity?.SetTag("user.id", stored.UserId);
            activity?.SetTag("family.id", stored.FamilyId);

            // Reuse detected: token was already rotated away — revoke entire family.
            if (stored.RevokedAt is not null)
            {
                logger.LogWarning(
                    "Refresh token reuse detected for family {FamilyId}, UserId {UserId}. Revoking entire chain.",
                    stored.FamilyId, stored.UserId);
                activity?.SetTag("result", "reuse_detected");
                await tokenRepo.RevokeFamilyAsync(stored.FamilyId);
                return Results.Unauthorized();
            }

            if (stored.ExpiresAt < clock.UtcNow)
            {
                activity?.SetTag("result", "expired");
                return Results.Unauthorized();
            }

            var user = await db.Users.FindAsync(stored.UserId);
            if (user is null)
            {
                activity?.SetTag("result", "user_not_found");
                return Results.Unauthorized();
            }

            using var rotateActivity = Source.StartActivity("rotate-refresh-token");
            rotateActivity?.SetTag("user.id", user.Id);
            rotateActivity?.SetTag("family.id", stored.FamilyId);

            var newAccessToken = MintAccessToken(user, config);
            var (newRawRefresh, newRefreshEntity) = MintRefreshToken(user.Id, stored.FamilyId, clock, config);

            // Atomic: revoke old token (pointing at its replacement) and persist the new one.
            await tokenRepo.RevokeTokenAsync(stored, newRefreshEntity.TokenHash);
            await tokenRepo.AddAsync(newRefreshEntity);

            activity?.SetTag("result", "success");
            logger.LogInformation("Refresh token rotated for user {UserId}, family {FamilyId}", user.Id, stored.FamilyId);
            return Results.Ok(new LoginResponse(newAccessToken, newRawRefresh, AccessExpirySeconds(config)));
        });

        app.MapPost("/api/auth/logout", async (
            RefreshRequest request,
            IRefreshTokenRepository tokenRepo) =>
        {
            using var activity = Source.StartActivity("logout");
            var hash = TokenHasher.Hash(request.RefreshToken);
            var stored = await tokenRepo.FindByHashAsync(hash);

            if (stored is not null && stored.RevokedAt is null)
            {
                await tokenRepo.RevokeTokenAsync(stored, replacedByHash: null);
                activity?.SetTag("revoked", true);
            }
            else
            {
                activity?.SetTag("revoked", false);
            }

            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }

    private static string MintAccessToken(User user, IConfiguration config)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("scope", "quotes.write")
            ],
            expires: DateTime.UtcNow.AddMinutes(config.GetValue<int>("Jwt:ExpiresInMinutes", 15)),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static (string rawToken, RefreshToken entity) MintRefreshToken(
        int userId, string familyId, IClock clock, IConfiguration config)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var entity = new RefreshToken
        {
            TokenHash = TokenHasher.Hash(raw),
            UserId = userId,
            FamilyId = familyId,
            ExpiresAt = clock.UtcNow.AddDays(config.GetValue<int>("Jwt:RefreshTokenExpiryDays", 7))
        };
        return (raw, entity);
    }

    private static int AccessExpirySeconds(IConfiguration config)
        => config.GetValue<int>("Jwt:ExpiresInMinutes", 15) * 60;
}
