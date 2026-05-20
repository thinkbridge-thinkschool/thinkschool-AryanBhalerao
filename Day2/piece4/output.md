# Outputs

## Build

```
dotnet build --no-restore
```

```
  QuotesApi net10.0 succeeded (0.5s) → bin\Debug\net10.0\QuotesApi.dll

Build succeeded in 1.0s
```

## Test

```
dotnet test --no-build --logger "console;verbosity=normal"
```

```
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v2.8.2+699d445a1a (64-bit .NET 10.0.8)
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v2.8.2+699d445a1a (64-bit .NET 10.0.8)
[xUnit.net 00:00:00.48]   Discovering: QuotesApi.Tests
[xUnit.net 00:00:00.48]   Discovering: QuotesApi.Tests
[xUnit.net 00:00:00.64]   Discovered:  QuotesApi.Tests
[xUnit.net 00:00:00.64]   Discovered:  QuotesApi.Tests
[xUnit.net 00:00:00.65]   Starting:    QuotesApi.Tests
[xUnit.net 00:00:00.65]   Starting:    QuotesApi.Tests
  Passed QuotesApi.Tests.GlobalExceptionHandlerTests.TryHandleAsync_OperationCancelled_Returns499 [198 ms]
  Passed QuotesApi.Tests.GlobalExceptionHandlerTests.TryHandleAsync_OtherException_Returns500 [43 ms]
warn: Microsoft.EntityFrameworkCore.Model.Validation[30002]
warn: Microsoft.EntityFrameworkCore.Model.Validation[30002]
      The entity type 'CollectionItem' has composite key '{'CollectionId', 'QuoteId'}' which is configured to use generated values. SQLite does not support generated values on composite keys.
      The entity type 'CollectionItem' has composite key '{'CollectionId', 'QuoteId'}' which is configured to use generated values. SQLite does not support generated values on composite keys.
info: Microsoft.EntityFrameworkCore.Migrations[20411]
info: Microsoft.EntityFrameworkCore.Migrations[20411]
      Acquiring an exclusive lock for migration application. See https://aka.ms/efcore-docs-migrations-lock for more information if this takes too long.
      Acquiring an exclusive lock for migration application. See https://aka.ms/efcore-docs-migrations-lock for more information if this takes too long.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (9ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      Executed DbCommand (9ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT COUNT(*) FROM "sqlite_master" WHERE "name" = '__EFMigrationsLock' AND "type" = 'table';
      SELECT COUNT(*) FROM "sqlite_master" WHERE "name" = '__EFMigrationsLock' AND "type" = 'table';
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (1ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      Executed DbCommand (1ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE IF NOT EXISTS "__EFMigrationsLock" (
      CREATE TABLE IF NOT EXISTS "__EFMigrationsLock" (
          "Id" INTEGER NOT NULL CONSTRAINT "PK___EFMigrationsLock" PRIMARY KEY,
          "Id" INTEGER NOT NULL CONSTRAINT "PK___EFMigrationsLock" PRIMARY KEY,
          "Timestamp" TEXT NOT NULL
          "Timestamp" TEXT NOT NULL
      );
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT OR IGNORE INTO "__EFMigrationsLock"("Id", "Timestamp") VALUES(1, '2026-05-20 09:42:49.2849348+00:00');
      INSERT OR IGNORE INTO "__EFMigrationsLock"("Id", "Timestamp") VALUES(1, '2026-05-20 09:42:49.2849348+00:00');
      SELECT changes();
      SELECT changes();
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
      CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
          "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
          "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
          "ProductVersion" TEXT NOT NULL
          "ProductVersion" TEXT NOT NULL
      );
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT COUNT(*) FROM "sqlite_master" WHERE "name" = '__EFMigrationsHistory' AND "type" = 'table';
      SELECT COUNT(*) FROM "sqlite_master" WHERE "name" = '__EFMigrationsHistory' AND "type" = 'table';
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT "MigrationId", "ProductVersion"
      SELECT "MigrationId", "ProductVersion"
      FROM "__EFMigrationsHistory"
      FROM "__EFMigrationsHistory"
      ORDER BY "MigrationId";
      ORDER BY "MigrationId";
info: Microsoft.EntityFrameworkCore.Migrations[20402]
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260519044357_InitialCreate'.
      Applying migration '20260519044357_InitialCreate'.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE "Quotes" (
      CREATE TABLE "Quotes" (
          "Id" INTEGER NOT NULL CONSTRAINT "PK_Quotes" PRIMARY KEY AUTOINCREMENT,
          "Id" INTEGER NOT NULL CONSTRAINT "PK_Quotes" PRIMARY KEY AUTOINCREMENT,
          "Author" TEXT NOT NULL,
          "Author" TEXT NOT NULL,
          "Text" TEXT NOT NULL
          "Text" TEXT NOT NULL
      );
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      VALUES ('20260519044357_InitialCreate', '10.0.8');
      VALUES ('20260519044357_InitialCreate', '10.0.8');
info: Microsoft.EntityFrameworkCore.Migrations[20402]
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260519122734_AddCollections'.
      Applying migration '20260519122734_AddCollections'.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE "Collections" (
      CREATE TABLE "Collections" (
          "Id" INTEGER NOT NULL CONSTRAINT "PK_Collections" PRIMARY KEY AUTOINCREMENT,
          "Id" INTEGER NOT NULL CONSTRAINT "PK_Collections" PRIMARY KEY AUTOINCREMENT,
          "Name" TEXT NOT NULL,
          "Name" TEXT NOT NULL,
          "OwnerId" TEXT NOT NULL
          "OwnerId" TEXT NOT NULL
      );
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE "CollectionItems" (
      CREATE TABLE "CollectionItems" (
          "QuoteId" INTEGER NOT NULL,
          "QuoteId" INTEGER NOT NULL,
          "CollectionId" INTEGER NOT NULL,
          "CollectionId" INTEGER NOT NULL,
          "AddedAt" TEXT NOT NULL,
          "AddedAt" TEXT NOT NULL,
          CONSTRAINT "PK_CollectionItems" PRIMARY KEY ("CollectionId", "QuoteId"),
          CONSTRAINT "PK_CollectionItems" PRIMARY KEY ("CollectionId", "QuoteId"),
          CONSTRAINT "FK_CollectionItems_Collections_CollectionId" FOREIGN KEY ("CollectionId") REFERENCES "Collections" ("Id") ON DELETE CASCADE
          CONSTRAINT "FK_CollectionItems_Collections_CollectionId" FOREIGN KEY ("CollectionId") REFERENCES "Collections" ("Id") ON DELETE CASCADE
      );
      );
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
      VALUES ('20260519122734_AddCollections', '10.0.8');
      VALUES ('20260519122734_AddCollections', '10.0.8');
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      DELETE FROM "__EFMigrationsLock";
      DELETE FROM "__EFMigrationsLock";
info: Microsoft.Hosting.Lifetime[0]
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\Users\aryan\Desktop\thinkschool-AryanBhalerao\Day2\piece2\QuotesApi
      Content root path: C:\Users\aryan\Desktop\thinkschool-AryanBhalerao\Day2\piece2\QuotesApi
  Passed QuotesApi.Tests.CollectionCancellationTests.GetCollection_TokenCancelledDuringSlowIo_OperationDoesNotCompleteOr499 [1 s]
info: Microsoft.Hosting.Lifetime[0]
info: Microsoft.Hosting.Lifetime[0]
      Application is shutting down...
      Application is shutting down...
[xUnit.net 00:00:03.26]   Finished:    QuotesApi.Tests
[xUnit.net 00:00:03.26]   Finished:    QuotesApi.Tests
  Passed QuotesApi.Tests.CollectionCancellationTests.DeleteCollection_TokenCancelledDuringSlowIo_OperationDoesNotCompleteOr499 [213 ms]
  Passed QuotesApi.Tests.CollectionCancellationTests.PostCollection_TokenCancelledDuringSlowIo_OperationDoesNotCompleteOr499 [204 ms]

Test Run Successful.
Total tests: 5
     Passed: 5
 Total time: 4.8643 Seconds
  QuotesApi.Tests test net10.0 succeeded (5.2s)

Test summary: total: 5, failed: 0, succeeded: 5, skipped: 0, duration: 5.1s
Build succeeded in 5.5s
```