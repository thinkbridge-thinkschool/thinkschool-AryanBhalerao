# Refresh token reuse-detection test

Full source: [QuotesApi.Tests/RefreshTokenReuseTests.cs](QuotesApi.Tests/RefreshTokenReuseTests.cs)

## What it proves

Replaying a token that has already been rotated causes the server to revoke the **entire family chain** — including tokens the attacker has never seen — and forces re-authentication.

## Test factory

```csharp
public sealed class AuthFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public AuthFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Swap on-disk SQLite for an in-memory connection shared across
            // the lifetime of the factory so state persists between requests.
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<QuoteDbContext>));
            if (dbDescriptor is not null) services.Remove(dbDescriptor);

            services.AddDbContext<QuoteDbContext>(o => o.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _connection.Dispose();
        base.Dispose(disposing);
    }
}
```

## The reuse-detection test

```csharp
[Fact]
public async Task Reuse_Of_Rotated_Token_RevokesEntireChain()
{
    // 1. Login — token1 is minted and stored (family A).
    var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
        new { Email = "test@example.com", Password = "Password123!" });
    Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
    var tokens1 = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();
    Assert.NotNull(tokens1);

    // 2. Legitimate refresh — token1 is revoked (ReplacedByToken = hash(token2))
    //    and token2 is issued in the same family.
    var refresh1Resp = await _client.PostAsJsonAsync("/api/auth/refresh",
        new { RefreshToken = tokens1.RefreshToken });
    Assert.Equal(HttpStatusCode.OK, refresh1Resp.StatusCode);
    var tokens2 = await refresh1Resp.Content.ReadFromJsonAsync<TokenResponse>();
    Assert.NotNull(tokens2);
    Assert.NotEqual(tokens1.RefreshToken, tokens2.RefreshToken);

    // 3. Replay token1 — reuse attack.
    //    Server detects token1.IsRevoked && token1.ReplacedByToken != null
    //    → logs a security warning, revokes ALL tokens in family A (including token2).
    var reuseResp = await _client.PostAsJsonAsync("/api/auth/refresh",
        new { RefreshToken = tokens1.RefreshToken });
    Assert.Equal(HttpStatusCode.Unauthorized, reuseResp.StatusCode);

    // 4. token2 is now also dead because it was caught in the chain revocation.
    //    This means the legitimate client (who holds token2) is forced to
    //    re-authenticate, signalling that their refresh token may have been stolen.
    var token2Resp = await _client.PostAsJsonAsync("/api/auth/refresh",
        new { RefreshToken = tokens2.RefreshToken });
    Assert.Equal(HttpStatusCode.Unauthorized, token2Resp.StatusCode);
}
```

## Helper record used for deserialisation

```csharp
file record TokenResponse(
    [property: JsonPropertyName("access_token")]  string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")]    int ExpiresIn);
```

## How the server-side chain revocation works

```
token1 (revoked, ReplacedByToken = hash(token2))  ─┐
token2 (active)                                     ├── Family = "9ae45a27..."
   ...any further rotations...                      ─┘

On replay of token1:
  existing.IsRevoked       → true
  existing.ReplacedByToken → not null   ← this is the reuse signal

  chain = SELECT * FROM RefreshTokens WHERE Family = '9ae45a27...'
  foreach token in chain where !token.IsRevoked:
      token.Revoke()       ← token2 and any successors are killed here
  SaveChanges()
  return 401
```

## Running the tests

```
dotnet test Day2/piece7/QuotesApi.Tests/QuotesApi.Tests.csproj
```

Expected output:

```
Passed  RefreshTokenReuseTests.Reuse_Of_Rotated_Token_RevokesEntireChain
Passed  RefreshTokenReuseTests.Refresh_WithValidToken_ReturnsNewPair
Passed  RefreshTokenReuseTests.Logout_RevokesToken_SubsequentRefreshFails
```
