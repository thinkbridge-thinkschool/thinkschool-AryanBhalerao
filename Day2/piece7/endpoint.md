# POST /api/auth/refresh

Exchanges a valid refresh token for a new access + refresh token pair (rotation).  
Old token is immediately invalidated. If the old token is replayed, the entire family is revoked.

## Request

```
POST /api/auth/refresh
Content-Type: application/json

{ "refreshToken": "<raw-refresh-token>" }
```

## Responses

| Status | Meaning |
|--------|---------|
| 200 | New pair issued |
| 401 | Token not found, expired, revoked, or reuse detected |

```json
{
  "access_token":  "<jwt>",
  "refresh_token": "<new-raw-token>",
  "expires_in":    900
}
```

## Implementation

```csharp
// Extensions/AuthEndpointExtensions.cs

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
```

## Supporting helpers

```csharp
// Token is never stored raw — only its SHA-256 hex digest hits the DB.
internal static string HashToken(string token)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
    return Convert.ToHexString(bytes).ToLowerInvariant();
}

private static (string raw, string hash) GenerateRefreshToken()
{
    var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    return (raw, HashToken(raw));
}
```

## RefreshToken model (Models/RefreshToken.cs)

```csharp
public class RefreshToken
{
    public int Id { get; private set; }
    public string Token { get; private set; } = null!;      // SHA-256 hash
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string Family { get; private set; } = null!;     // rotation-chain ID
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByToken { get; private set; }    // hash of successor

    private RefreshToken() { }

    public static RefreshToken Create(string tokenHash, int userId, string family, DateTime expiresAt) =>
        new() { Token = tokenHash, UserId = userId, Family = family, ExpiresAt = expiresAt };

    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive  => !IsRevoked && DateTime.UtcNow < ExpiresAt;

    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByToken = replacedByTokenHash;
    }
}
```

## Security properties

- Raw token is never persisted; only its SHA-256 digest is stored.  
  A database dump cannot be used to refresh sessions.
- Every rotation produces a new `Family`-scoped token; `Family` is set once at login and inherited by all successors.
- Presenting a rotated (already-replaced) token triggers **chain revocation**: every token sharing the same `Family` is immediately marked revoked, forcing re-authentication.
- Expired tokens are rejected without chain revocation (no evidence of theft).
- Logout (`POST /api/auth/logout`) revokes the active token without affecting the chain.
