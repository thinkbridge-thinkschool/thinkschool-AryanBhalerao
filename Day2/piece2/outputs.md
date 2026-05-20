# Outputs

## Build

```
dotnet build --no-restore
```

```
  QuotesApi -> ...\QuotesApi\bin\Debug\net10.0\QuotesApi.dll
  QuotesApi.Tests -> ...\QuotesApi.Tests\bin\Debug\net10.0\QuotesApi.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.19
```

---

## Test

```
dotnet test --no-build --logger "console;verbosity=normal"
```

```
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v2.8.2+699d445a1a (64-bit .NET 10.0.8)
[xUnit.net 00:00:00.08]   Discovering: QuotesApi.Tests
[xUnit.net 00:00:00.13]   Discovered:  QuotesApi.Tests
[xUnit.net 00:00:00.13]   Starting:    QuotesApi.Tests

warn: Microsoft.EntityFrameworkCore.Model.Validation[30002]
      The entity type 'CollectionItem' has composite key '{'CollectionId', 'QuoteId'}' which is
      configured to use generated values. SQLite does not support generated values on composite keys.

info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260519044357_InitialCreate'.
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260519122734_AddCollections'.

info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: ...\QuotesApi

  Passed  GlobalExceptionHandlerTests.TryHandleAsync_OperationCancelled_Returns499   [17 ms]
  Passed  GlobalExceptionHandlerTests.TryHandleAsync_OtherException_Returns500        [35 ms]
  Passed  CollectionCancellationTests.GetCollection_TokenCancelledDuringSlowIo_OperationDoesNotCompleteOr499   [1 s]
  Passed  CollectionCancellationTests.DeleteCollection_TokenCancelledDuringSlowIo_OperationDoesNotCompleteOr499 [204 ms]
  Passed  CollectionCancellationTests.PostCollection_TokenCancelledDuringSlowIo_OperationDoesNotCompleteOr499   [217 ms]

info: Microsoft.Hosting.Lifetime[0]
      Application is shutting down...
[xUnit.net 00:00:01.66]   Finished:    QuotesApi.Tests

Test Run Successful.
Total tests: 5
     Passed: 5
 Total time: 2.1337 Seconds
```
