# Test Run Output

```
$ dotnet test Quotes.Tests.Integration/Quotes.Tests.Integration.csproj --no-build --verbosity normal
```

```
Test run for ...Quotes.Tests.Integration\bin\Debug\net10.0\Quotes.Tests.Integration.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v2.8.2+699d445a1a (64-bit .NET 10.0.8)
[xUnit.net 00:00:00.10]   Discovering: Quotes.Tests.Integration
[xUnit.net 00:00:00.14]   Discovered:  Quotes.Tests.Integration
[xUnit.net 00:00:00.14]   Starting:    Quotes.Tests.Integration

  Passed  Quotes.Tests.Integration.Tests.PostCollection_WithMissingName.ReturnsValidationProblem [3 s]
  Passed  Quotes.Tests.Integration.Tests.PostQuote_Anonymous.Returns401 [3 s]
  Passed  Quotes.Tests.Integration.Tests.PostQuote_WithEmptyAuthor.ReturnsProblemDetails [3 s]
  Passed  Quotes.Tests.Integration.Tests.GetQuotes_WhenEmpty.Returns200WithEmptyList [3 s]
  Passed  Quotes.Tests.Integration.Tests.GetCollection_WhenNotFound.Returns404 [3 s]
  Passed  Quotes.Tests.Integration.Tests.AddItem_ToExistingCollection.Returns204 [3 s]
  Passed  Quotes.Tests.Integration.Tests.Migrations_AreApplied.NoPendingMigrations [706 ms]
  Passed  Quotes.Tests.Integration.Tests.DeleteQuote_WhenNotFound.Returns404 [1 s]
  Passed  Quotes.Tests.Integration.Tests.Login_WithWrongPassword.Returns401 [4 s]
  Passed  Quotes.Tests.Integration.Tests.Login_WithUnknownEmail.Returns401 [1 s]
  Passed  Quotes.Tests.Integration.Tests.GetQuoteById_WhenExists.Returns200WithQuote [1 s]
  Passed  Quotes.Tests.Integration.Tests.DeleteQuote_ByOwner.Returns204 [1 s]
  Passed  Quotes.Tests.Integration.Tests.GetQuotes_AfterCreating.ReturnsQuoteInList [1 s]
  Passed  Quotes.Tests.Integration.Tests.DeleteQuote_Anonymous.Returns401 [862 ms]
  Passed  Quotes.Tests.Integration.Tests.Refresh_WithValidToken.Returns200WithNewTokens [5 s]
  Passed  Quotes.Tests.Integration.Tests.PostCollection_WithValidData.Returns201WithCreatedCollection [1 s]
  Passed  Quotes.Tests.Integration.Tests.PostQuote_WithScopeClaim.Returns201WithCreatedQuote [703 ms]
  Passed  Quotes.Tests.Integration.Tests.DeleteQuote_ByNonOwner.Returns403 [736 ms]
  Passed  Quotes.Tests.Integration.Tests.DeleteCollection_WhenExists.Returns204 [745 ms]
  Passed  Quotes.Tests.Integration.Tests.DeleteCollection_WhenNotFound.Returns404 [634 ms]
  Passed  Quotes.Tests.Integration.Tests.Login_WithValidCredentials.Returns200WithTokens [1 s]
  Passed  Quotes.Tests.Integration.Tests.Logout_WithValidToken.Returns204 [1 s]
  Passed  Quotes.Tests.Integration.Tests.Refresh_WithRevokedToken.Returns401 [1 s]
  Passed  Quotes.Tests.Integration.Tests.GetQuoteById_WhenNotFound.Returns404 [466 ms]
  Passed  Quotes.Tests.Integration.Tests.PostQuote_AuthenticatedWithoutScopeClaim.Returns403 [516 ms]

Test Run Successful.
Total tests: 25
     Passed: 25
 Total time: 7.0891 Seconds
```

