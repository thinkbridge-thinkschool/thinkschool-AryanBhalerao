using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using QuotesApi.Data;
using QuotesApi.Models;

namespace QuotesApi.Extensions;

public static class AuthEndpointExtensions
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", async (LoginDto dto, QuoteDbContext db, IConfiguration config) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Results.Unauthorized();

            var (accessToken, expiresIn) = MintAccessToken(user, config);
            var (rawRefresh, tokenHash) = GenerateRefreshToken();
            var family = Guid.NewGuid().ToString("N");
            var refreshExpiry = DateTime.UtcNow.AddDays(RefreshExpiryDays(config));

            db.RefreshTokens.Add(RefreshToken.Create(tokenHash, user.Id, family, refreshExpiry));
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                access_token = accessToken,
                refresh_token = rawRefresh,
                expires_in = expiresIn
            });
        });

        group.MapPost("/refresh", async (
            RefreshDto dto,
            QuoteDbContext db,
            IConfiguration config,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Security");
            var tokenHash = HashToken(dto.RefreshToken);

            var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == tokenHash);
            if (existing is null)
                return Results.Unauthorized();

            if (existing.IsRevoked)
            {
                // Token already rotated — someone is replaying a replaced token.
                if (existing.ReplacedByToken is not null)
                {
                    logger.LogWarning(
                        "Refresh token reuse detected for family {Family}. Revoking entire chain.",
                        existing.Family);

                    var chain = await db.RefreshTokens
                        .Where(t => t.Family == existing.Family)
                        .ToListAsync();
                    foreach (var t in chain)
                        if (!t.IsRevoked) t.Revoke();
                    await db.SaveChangesAsync();
                }
                return Results.Unauthorized();
            }

            if (!existing.IsActive)   // expired
                return Results.Unauthorized();

            var user = await db.Users.FindAsync(existing.UserId);
            if (user is null)
                return Results.Unauthorized();

            var (accessToken, expiresIn) = MintAccessToken(user, config);
            var (rawRefresh, newHash) = GenerateRefreshToken();

            existing.Revoke(newHash);
            db.RefreshTokens.Add(
                RefreshToken.Create(newHash, user.Id, existing.Family,
                    DateTime.UtcNow.AddDays(RefreshExpiryDays(config))));
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                access_token = accessToken,
                refresh_token = rawRefresh,
                expires_in = expiresIn
            });
        });

        group.MapPost("/logout", async (LogoutDto dto, QuoteDbContext db) =>
        {
            var tokenHash = HashToken(dto.RefreshToken);
            var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == tokenHash);
            if (existing?.IsActive == true)
            {
                existing.Revoke();
                await db.SaveChangesAsync();
            }
            return Results.NoContent();
        });
    }

    private static (string token, int expiresIn) MintAccessToken(User user, IConfiguration config)
    {
        var jwtSettings = config.GetSection("Jwt");
        var keyBytes = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
        var expiresInMinutes = int.Parse(jwtSettings["ExpiresInMinutes"] ?? "15");

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            ]),
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return (handler.WriteToken(handler.CreateToken(descriptor)), expiresInMinutes * 60);
    }

    private static (string raw, string hash) GenerateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return (raw, HashToken(raw));
    }

    internal static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static int RefreshExpiryDays(IConfiguration config) =>
        int.Parse(config["Jwt:RefreshExpiryDays"] ?? "7");
}
