QuotesApi.Tests/RefreshTokenReuseTests.cs

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using QuotesApi.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace QuotesApi.Tests;

public sealed class AuthFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public AuthFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
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

file record TokenResponse(
    [property: JsonPropertyName("access_token")]  string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")]    int ExpiresIn);

public class RefreshTokenReuseTests : IClassFixture<AuthFactory>
{
    private readonly HttpClient _client;

    public RefreshTokenReuseTests(AuthFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task Reuse_Of_Rotated_Token_RevokesEntireChain()
    {
        // 1. Login
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        var tokens1 = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens1);

        // 2. First legitimate refresh — token1 consumed, token2 issued
        var refresh1Resp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = tokens1.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh1Resp.StatusCode);
        var tokens2 = await refresh1Resp.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens2);
        Assert.NotEqual(tokens1.RefreshToken, tokens2.RefreshToken);

        // 3. Replay token1 — reuse attack → 401, chain revoked
        var reuseResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = tokens1.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResp.StatusCode);

        // 4. token2 must now be dead (caught in chain revocation)
        var token2Resp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = tokens2.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, token2Resp.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewPair()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "Password123!" });
        var tokens1 = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();

        var refreshResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = tokens1!.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshResp.StatusCode);

        var tokens2 = await refreshResp.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens2?.AccessToken);
        Assert.NotNull(tokens2?.RefreshToken);
    }

    [Fact]
    public async Task Logout_RevokesToken_SubsequentRefreshFails()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "test@example.com", Password = "Password123!" });
        var tokens = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();

        var logoutResp = await _client.PostAsJsonAsync("/api/auth/logout",
            new { RefreshToken = tokens!.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logoutResp.StatusCode);

        var refreshResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResp.StatusCode);
    }
}
```

```
dotnet test QuotesApi.Tests/QuotesApi.Tests.csproj --logger "console;verbosity=normal"
```

```
  Determining projects to restore...
  All projects are up-to-date for restore.
  QuotesApi -> C:\Users\aryan\Desktop\thinkschool-AryanBhalerao\Day2\piece7\QuotesApi\bin\Debug\net10.0\QuotesApi.dll
  QuotesApi.Tests -> C:\Users\aryan\Desktop\thinkschool-AryanBhalerao\Day2\piece7\QuotesApi.Tests\bin\Debug\net10.0\QuotesApi.Tests.dll
Test run for C:\Users\aryan\Desktop\thinkschool-AryanBhalerao\Day2\piece7\QuotesApi.Tests\bin\Debug\net10.0\QuotesApi.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v2.8.2+699d445a1a (64-bit .NET 10.0.8)
[xUnit.net 00:00:00.10]   Discovering: QuotesApi.Tests
[xUnit.net 00:00:00.14]   Discovered:  QuotesApi.Tests
[xUnit.net 00:00:00.14]   Starting:    QuotesApi.Tests
warn: Microsoft.EntityFrameworkCore.Model.Validation[30002]
      The entity type 'CollectionItem' has composite key '{'CollectionId', 'QuoteId'}' which is configured to use generated values. SQLite does not support generated values on composite keys.
info: Microsoft.EntityFrameworkCore.Migrations[20411]
      Acquiring an exclusive lock for migration application. See https://aka.ms/efcore-docs-migrations-lock for more information if this takes too long.
info: Microsoft.EntityFrameworkCore.Migrations[20411]
      Acquiring an exclusive lock for migration application. See https://aka.ms/efcore-docs-migrations-lock for more information if this takes too long.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (9ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT COUNT(*) FROM "sqlite_master" WHERE "name" = '__EFMigrationsLock' AND "type" = 'table';
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (9ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT COUNT(*) FROM "sqlite_master" WHERE "name" = '__EFMigrationsLock' AND "type" = 'table';
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (1ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE IF NOT EXISTS "__EFMigrationsLock" (
          "Id" INTEGER NOT NULL CONSTRAINT "PK___EFMigrationsLock" PRIMARY KEY,
          "Timestamp" TEXT NOT NULL
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (1ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE IF NOT EXISTS "__EFMigrationsLock" (
          "Id" INTEGER NOT NULL CONSTRAINT "PK___EFMigrationsLock" PRIMARY KEY,
          "Timestamp" TEXT NOT NULL
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT OR IGNORE INTO "__EFMigrationsLock"("Id", "Timestamp") VALUES(1, '2026-05-20 12:34:04.4340175+00:00');
      SELECT changes();
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT OR IGNORE INTO "__EFMigrationsLock"("Id", "Timestamp") VALUES(1, '2026-05-20 12:34:04.4339847+00:00');
      SELECT changes();
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
          "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
          "ProductVersion" TEXT NOT NULL
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
          "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
          "ProductVersion" TEXT NOT NULL
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT COUNT(*) FROM "sqlite_master" WHERE "name" = '__EFMigrationsHistory' AND "type" = 'table';
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT COUNT(*) FROM "sqlite_master" WHERE "name" = '__EFMigrationsHistory' AND "type" = 'table';
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT "MigrationId", "ProductVersion"
      FROM "__EFMigrationsHistory"
      ORDER BY "MigrationId";
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT "MigrationId", "ProductVersion"
      FROM "__EFMigrationsHistory"
      ORDER BY "MigrationId";
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260519044357_InitialCreate'.
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260519044357_InitialCreate'.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE "Quotes" (
          "Id" INTEGER NOT NULL CONSTRAINT "PK_Quotes" PRIMARY KEY AUTOINCREMENT,
          "Author" TEXT NOT NULL,
          "Text" TEXT NOT NULL
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE "Quotes" (
          "Id" INTEGER NOT NULL CONSTRAINT "PK_Quotes" PRIMARY KEY AUTOINCREMENT,
          "Author" TEXT NOT NULL,
          "Text" TEXT NOT NULL
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      VALUES ('20260519044357_InitialCreate', '10.0.8');
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      VALUES ('20260519044357_InitialCreate', '10.0.8');
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260519122734_AddCollections'.
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260519122734_AddCollections'.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE "Collections" (
          "Id" INTEGER NOT NULL CONSTRAINT "PK_Collections" PRIMARY KEY AUTOINCREMENT,
          "Name" TEXT NOT NULL,
          "OwnerId" TEXT NOT NULL
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE "Collections" (
          "Id" INTEGER NOT NULL CONSTRAINT "PK_Collections" PRIMARY KEY AUTOINCREMENT,
          "Name" TEXT NOT NULL,
          "OwnerId" TEXT NOT NULL
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE "CollectionItems" (
          "QuoteId" INTEGER NOT NULL,
          "CollectionId" INTEGER NOT NULL,
          "AddedAt" TEXT NOT NULL,
          CONSTRAINT "PK_CollectionItems" PRIMARY KEY ("CollectionId", "QuoteId"),
          CONSTRAINT "FK_CollectionItems_Collections_CollectionId" FOREIGN KEY ("CollectionId") REFERENCES "Collections" ("Id") ON DELETE CASCADE
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE "CollectionItems" (
          "QuoteId" INTEGER NOT NULL,
          "CollectionId" INTEGER NOT NULL,
          "AddedAt" TEXT NOT NULL,
          CONSTRAINT "PK_CollectionItems" PRIMARY KEY ("CollectionId", "QuoteId"),
          CONSTRAINT "FK_CollectionItems_Collections_CollectionId" FOREIGN KEY ("CollectionId") REFERENCES "Collections" ("Id") ON DELETE CASCADE
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      VALUES ('20260519122734_AddCollections', '10.0.8');
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      VALUES ('20260519122734_AddCollections', '10.0.8');
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260520083319_RichQuoteEntity'.
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260520083319_RichQuoteEntity'.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      ALTER TABLE "Quotes" ADD "CreatedAt" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00';
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      ALTER TABLE "Quotes" ADD "CreatedAt" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00';
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      ALTER TABLE "Quotes" ADD "IsDeleted" INTEGER NOT NULL DEFAULT 0;
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      VALUES ('20260520083319_RichQuoteEntity', '10.0.8');
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      ALTER TABLE "Quotes" ADD "IsDeleted" INTEGER NOT NULL DEFAULT 0;
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260520111739_AddUsers'.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      VALUES ('20260520083319_RichQuoteEntity', '10.0.8');
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260520111739_AddUsers'.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE "Users" (
          "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
          "Email" TEXT NOT NULL,
          "PasswordHash" TEXT NOT NULL
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE "Users" (
          "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
          "Email" TEXT NOT NULL,
          "PasswordHash" TEXT NOT NULL
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      VALUES ('20260520111739_AddUsers', '10.0.8');
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260520120334_AddRefreshTokenRotation'.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      VALUES ('20260520111739_AddUsers', '10.0.8');
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260520120334_AddRefreshTokenRotation'.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE "RefreshTokens" (
          "Id" INTEGER NOT NULL CONSTRAINT "PK_RefreshTokens" PRIMARY KEY AUTOINCREMENT,
          "Token" TEXT NOT NULL,
          "UserId" INTEGER NOT NULL,
          "ExpiresAt" TEXT NOT NULL,
          "RevokedAt" TEXT NULL,
          "ReplacedByToken" TEXT NULL,
          CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE "RefreshTokens" (
          "Id" INTEGER NOT NULL CONSTRAINT "PK_RefreshTokens" PRIMARY KEY AUTOINCREMENT,
          "Token" TEXT NOT NULL,
          "UserId" INTEGER NOT NULL,
          "ExpiresAt" TEXT NOT NULL,
          "RevokedAt" TEXT NULL,
          "ReplacedByToken" TEXT NULL,
          CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON "RefreshTokens" ("Token");
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON "RefreshTokens" ("Token");
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      VALUES ('20260520120334_AddRefreshTokenRotation', '10.0.8');
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      VALUES ('20260520120334_AddRefreshTokenRotation', '10.0.8');
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260520140000_AddRefreshTokenFamily'.
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260520140000_AddRefreshTokenFamily'.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      ALTER TABLE "RefreshTokens" ADD "Family" TEXT NOT NULL DEFAULT '';
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      ALTER TABLE "RefreshTokens" ADD "Family" TEXT NOT NULL DEFAULT '';
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE INDEX "IX_RefreshTokens_Family" ON "RefreshTokens" ("Family");
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE INDEX "IX_RefreshTokens_Family" ON "RefreshTokens" ("Family");
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      VALUES ('20260520140000_AddRefreshTokenFamily', '10.0.8');
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      VALUES ('20260520140000_AddRefreshTokenFamily', '10.0.8');
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      DELETE FROM "__EFMigrationsLock";
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      DELETE FROM "__EFMigrationsLock";
  Passed QuotesApi.Tests.GlobalExceptionHandlerTests.TryHandleAsync_OperationCancelled_Returns499 [18 ms]
  Passed QuotesApi.Tests.GlobalExceptionHandlerTests.TryHandleAsync_OtherException_Returns500 [41 ms]
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT EXISTS (
          SELECT 1
          FROM "Users" AS "u")
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT EXISTS (
          SELECT 1
          FROM "Users" AS "u")
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (3ms) [Parameters=[@p0='?' (Size = 16), @p1='?' (Size = 60)], CommandType='Text', CommandTimeout='30']
      INSERT INTO "Users" ("Email", "PasswordHash")
      VALUES (@p0, @p1)
      RETURNING "Id";
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (3ms) [Parameters=[@p0='?' (Size = 16), @p1='?' (Size = 60)], CommandType='Text', CommandTimeout='30']
      INSERT INTO "Users" ("Email", "PasswordHash")
      VALUES (@p0, @p1)
      RETURNING "Id";
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\Users\aryan\Desktop\thinkschool-AryanBhalerao\Day2\piece7\QuotesApi
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\Users\aryan\Desktop\thinkschool-AryanBhalerao\Day2\piece7\QuotesApi
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (2ms) [Parameters=[@dto_Email='?' (Size = 16)], CommandType='Text', CommandTimeout='30']
      SELECT "u"."Id", "u"."Email", "u"."PasswordHash"
      FROM "Users" AS "u"
      WHERE "u"."Email" = @dto_Email
      LIMIT 1
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (1ms) [Parameters=[@p0='?' (DbType = DateTime), @p1='?' (Size = 32), @p2='?', @p3='?' (DbType = DateTime), @p4='?' (Size = 64), @p5='?' (DbType = Int32)], CommandType='Text', CommandTimeout='30']
      INSERT INTO "RefreshTokens" ("ExpiresAt", "Family", "ReplacedByToken", "RevokedAt", "Token", "UserId")
      VALUES (@p0, @p1, @p2, @p3, @p4, @p5)
      RETURNING "Id";
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@tokenHash='?' (Size = 64)], CommandType='Text', CommandTimeout='30']
      SELECT "r"."Id", "r"."ExpiresAt", "r"."Family", "r"."ReplacedByToken", "r"."RevokedAt", "r"."Token", "r"."UserId"
      FROM "RefreshTokens" AS "r"
      WHERE "r"."Token" = @tokenHash
      LIMIT 1
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@p='?' (DbType = Int32)], CommandType='Text', CommandTimeout='30']
      SELECT "u"."Id", "u"."Email", "u"."PasswordHash"
      FROM "Users" AS "u"
      WHERE "u"."Id" = @p
      LIMIT 1
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@p2='?' (DbType = Int32), @p0='?' (Size = 64), @p1='?' (DbType = DateTime)], CommandType='Text', CommandTimeout='30']
      UPDATE "RefreshTokens" SET "ReplacedByToken" = @p0, "RevokedAt" = @p1
      WHERE "Id" = @p2
      RETURNING 1;
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@p0='?' (DbType = DateTime), @p1='?' (Size = 32), @p2='?', @p3='?' (DbType = DateTime), @p4='?' (Size = 64), @p5='?' (DbType = Int32)], CommandType='Text', CommandTimeout='30']
      INSERT INTO "RefreshTokens" ("ExpiresAt", "Family", "ReplacedByToken", "RevokedAt", "Token", "UserId")
      VALUES (@p0, @p1, @p2, @p3, @p4, @p5)
      RETURNING "Id";
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@dto_Email='?' (Size = 16)], CommandType='Text', CommandTimeout='30']
      SELECT "u"."Id", "u"."Email", "u"."PasswordHash"
      FROM "Users" AS "u"
      WHERE "u"."Email" = @dto_Email
      LIMIT 1
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@p0='?' (DbType = DateTime), @p1='?' (Size = 32), @p2='?', @p3='?' (DbType = DateTime), @p4='?' (Size = 64), @p5='?' (DbType = Int32)], CommandType='Text', CommandTimeout='30']
      INSERT INTO "RefreshTokens" ("ExpiresAt", "Family", "ReplacedByToken", "RevokedAt", "Token", "UserId")
      VALUES (@p0, @p1, @p2, @p3, @p4, @p5)
      RETURNING "Id";
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@tokenHash='?' (Size = 64)], CommandType='Text', CommandTimeout='30']
      SELECT "r"."Id", "r"."ExpiresAt", "r"."Family", "r"."ReplacedByToken", "r"."RevokedAt", "r"."Token", "r"."UserId"
      FROM "RefreshTokens" AS "r"
      WHERE "r"."Token" = @tokenHash
      LIMIT 1
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@p1='?' (DbType = Int32), @p0='?' (DbType = DateTime)], CommandType='Text', CommandTimeout='30']
      UPDATE "RefreshTokens" SET "RevokedAt" = @p0
      WHERE "Id" = @p1
      RETURNING 1;
info: Microsoft.Hosting.Lifetime[0]
      Application is shutting down...
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@tokenHash='?' (Size = 64)], CommandType='Text', CommandTimeout='30']
      SELECT "r"."Id", "r"."ExpiresAt", "r"."Family", "r"."ReplacedByToken", "r"."RevokedAt", "r"."Token", "r"."UserId"
      FROM "RefreshTokens" AS "r"
      WHERE "r"."Token" = @tokenHash
      LIMIT 1
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@dto_Email='?' (Size = 16)], CommandType='Text', CommandTimeout='30']
      SELECT "u"."Id", "u"."Email", "u"."PasswordHash"
      FROM "Users" AS "u"
      WHERE "u"."Email" = @dto_Email
      LIMIT 1
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@p0='?' (DbType = DateTime), @p1='?' (Size = 32), @p2='?', @p3='?' (DbType = DateTime), @p4='?' (Size = 64), @p5='?' (DbType = Int32)], CommandType='Text', CommandTimeout='30']
      INSERT INTO "RefreshTokens" ("ExpiresAt", "Family", "ReplacedByToken", "RevokedAt", "Token", "UserId")
      VALUES (@p0, @p1, @p2, @p3, @p4, @p5)
      RETURNING "Id";
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@tokenHash='?' (Size = 64)], CommandType='Text', CommandTimeout='30']
      SELECT "r"."Id", "r"."ExpiresAt", "r"."Family", "r"."ReplacedByToken", "r"."RevokedAt", "r"."Token", "r"."UserId"
      FROM "RefreshTokens" AS "r"
      WHERE "r"."Token" = @tokenHash
      LIMIT 1
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@p='?' (DbType = Int32)], CommandType='Text', CommandTimeout='30']
      SELECT "u"."Id", "u"."Email", "u"."PasswordHash"
      FROM "Users" AS "u"
      WHERE "u"."Id" = @p
      LIMIT 1
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@p2='?' (DbType = Int32), @p0='?' (Size = 64), @p1='?' (DbType = DateTime)], CommandType='Text', CommandTimeout='30']
      UPDATE "RefreshTokens" SET "ReplacedByToken" = @p0, "RevokedAt" = @p1
      WHERE "Id" = @p2
      RETURNING 1;
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@p0='?' (DbType = DateTime), @p1='?' (Size = 32), @p2='?', @p3='?' (DbType = DateTime), @p4='?' (Size = 64), @p5='?' (DbType = Int32)], CommandType='Text', CommandTimeout='30']
      INSERT INTO "RefreshTokens" ("ExpiresAt", "Family", "ReplacedByToken", "RevokedAt", "Token", "UserId")
      VALUES (@p0, @p1, @p2, @p3, @p4, @p5)
      RETURNING "Id";
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@tokenHash='?' (Size = 64)], CommandType='Text', CommandTimeout='30']
      SELECT "r"."Id", "r"."ExpiresAt", "r"."Family", "r"."ReplacedByToken", "r"."RevokedAt", "r"."Token", "r"."UserId"
      FROM "RefreshTokens" AS "r"
      WHERE "r"."Token" = @tokenHash
      LIMIT 1
warn: Security[0]
      Refresh token reuse detected for family d7dd8773691f4acca61b27d0ef7aa9d3. Revoking entire chain.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@existing_Family='?' (Size = 32)], CommandType='Text', CommandTimeout='30']
      SELECT "r"."Id", "r"."ExpiresAt", "r"."Family", "r"."ReplacedByToken", "r"."RevokedAt", "r"."Token", "r"."UserId"
      FROM "RefreshTokens" AS "r"
      WHERE "r"."Family" = @existing_Family
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@p1='?' (DbType = Int32), @p0='?' (DbType = DateTime)], CommandType='Text', CommandTimeout='30']
      UPDATE "RefreshTokens" SET "RevokedAt" = @p0
      WHERE "Id" = @p1
      RETURNING 1;
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[@tokenHash='?' (Size = 64)], CommandType='Text', CommandTimeout='30']
      SELECT "r"."Id", "r"."ExpiresAt", "r"."Family", "r"."ReplacedByToken", "r"."RevokedAt", "r"."Token", "r"."UserId"
      FROM "RefreshTokens" AS "r"
      WHERE "r"."Token" = @tokenHash
      LIMIT 1
info: Microsoft.Hosting.Lifetime[0]
      Application is shutting down...
[xUnit.net 00:00:02.88]   Finished:    QuotesApi.Tests
  Passed QuotesApi.Tests.CollectionCancellationTests.GetCollection_TokenCancelledDuringSlowIo_OperationDoesNotCompleteOr499 [2 s]
  Passed QuotesApi.Tests.CollectionCancellationTests.DeleteCollection_TokenCancelledDuringSlowIo_OperationDoesNotCompleteOr499 [213 ms]
  Passed QuotesApi.Tests.RefreshTokenReuseTests.Refresh_WithValidToken_ReturnsNewPair [2 s]
  Passed QuotesApi.Tests.CollectionCancellationTests.PostCollection_TokenCancelledDuringSlowIo_OperationDoesNotCompleteOr499 [210 ms]
  Passed QuotesApi.Tests.RefreshTokenReuseTests.Logout_RevokesToken_SubsequentRefreshFails [150 ms]
  Passed QuotesApi.Tests.RefreshTokenReuseTests.Reuse_Of_Rotated_Token_RevokesEntireChain [157 ms]

Test Run Successful.
Total tests: 8
     Passed: 8
 Total time: 3.4115 Seconds
```
