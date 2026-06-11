using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QuotesApi.Data;
using QuotesApi.Models;
using QuotesApi.Options;
using QuotesApi.Repositories;
using QuotesApi.Services;

namespace QuotesApi.Endpoints;

public static class AuthEndpoints
{
    // Timing-equalization dummy hash
    private static readonly string DummyPasswordHash =
        BCrypt.Net.BCrypt.HashPassword("timing-equalization-dummy");

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            AppDbContext db,
            IRefreshTokenRepository tokenRepo,
            IClock clock,
            IOptions<JwtOptions> jwtOptions,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("QuotesApi.Auth");

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user?.PasswordHash ?? DummyPasswordHash);
            if (user is null || !passwordValid)
            {
                logger.LogWarning("Login failed for email {Email}", request.Email);
                return Results.Unauthorized();
            }

            var opts = jwtOptions.Value;
            var accessToken = MintAccessToken(user, opts, clock);
            var (rawRefresh, refreshEntity) = MintRefreshToken(user.Id, Guid.NewGuid().ToString(), clock, opts);
            await tokenRepo.AddAsync(refreshEntity);

            logger.LogInformation("Login succeeded for user {UserId} ({Email})", user.Id, user.Email);
            return Results.Ok(new LoginResponse(accessToken, rawRefresh, AccessExpirySeconds(opts)));
        });

        app.MapPost("/api/auth/refresh", async (
            RefreshRequest request,
            IRefreshTokenRepository tokenRepo,
            IClock clock,
            IOptions<JwtOptions> jwtOptions,
            ILoggerFactory loggerFactory,
            AppDbContext db) =>
        {
            var logger = loggerFactory.CreateLogger("QuotesApi.Auth");

            var incomingHash = TokenHasher.Hash(request.RefreshToken);
            var stored = await tokenRepo.FindByHashAsync(incomingHash);

            if (stored is null)
                return Results.Unauthorized();

            // Reuse detected — revoke family
            if (stored.RevokedAt is not null)
            {
                logger.LogWarning(
                    "Refresh token reuse detected for family {FamilyId}, UserId {UserId}. Revoking entire chain.",
                    stored.FamilyId, stored.UserId);
                await tokenRepo.RevokeFamilyAsync(stored.FamilyId);
                return Results.Unauthorized();
            }

            if (stored.ExpiresAt < clock.UtcNow)
                return Results.Unauthorized();

            var user = await db.Users.FindAsync(stored.UserId);
            if (user is null)
                return Results.Unauthorized();

            var opts = jwtOptions.Value;
            var newAccessToken = MintAccessToken(user, opts, clock);
            var (newRawRefresh, newRefreshEntity) = MintRefreshToken(user.Id, stored.FamilyId, clock, opts);

            // Atomic rotate
            await tokenRepo.RotateAsync(stored, newRefreshEntity);

            logger.LogInformation("Refresh token rotated for user {UserId}, family {FamilyId}", user.Id, stored.FamilyId);
            return Results.Ok(new LoginResponse(newAccessToken, newRawRefresh, AccessExpirySeconds(opts)));
        });

        app.MapPost("/api/auth/logout", async (
            RefreshRequest request,
            IRefreshTokenRepository tokenRepo) =>
        {
            var hash = TokenHasher.Hash(request.RefreshToken);
            var stored = await tokenRepo.FindByHashAsync(hash);

            if (stored is not null && stored.RevokedAt is null)
                await tokenRepo.RevokeTokenAsync(stored, replacedByHash: null);

            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }

    private static string MintAccessToken(User user, JwtOptions opts, IClock clock)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: opts.Issuer,
            audience: opts.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("scope", "quotes.write")
            ],
            expires: clock.UtcNow.Add(opts.AccessTokenLifetime).UtcDateTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static (string rawToken, RefreshToken entity) MintRefreshToken(
        int userId, string familyId, IClock clock, JwtOptions opts)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var entity = new RefreshToken
        {
            TokenHash = TokenHasher.Hash(raw),
            UserId = userId,
            FamilyId = familyId,
            ExpiresAt = clock.UtcNow.Add(opts.RefreshTokenLifetime)
        };
        return (raw, entity);
    }

    private static int AccessExpirySeconds(JwtOptions opts)
        => (int)opts.AccessTokenLifetime.TotalSeconds;
}
