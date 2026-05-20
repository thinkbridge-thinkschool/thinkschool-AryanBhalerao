# Test Output

`dotnet test Tests.Domain/Tests.Domain.csproj --logger "console;verbosity=normal"`

```
  Determining projects to restore...
  All projects are up-to-date for restore.
  QuotesApi -> ...\QuotesApi\bin\Debug\net10.0\QuotesApi.dll
  Tests.Domain -> ...\Tests.Domain\bin\Debug\net10.0\Tests.Domain.dll
Test run for Tests.Domain\bin\Debug\net10.0\Tests.Domain.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.5+1b188a7b0a (64-bit .NET 10.0.8)
[xUnit.net 00:00:00.71]   Discovering: Tests.Domain
[xUnit.net 00:00:00.76]   Discovered:  Tests.Domain
[xUnit.net 00:00:00.79]   Starting:    Tests.Domain
[xUnit.net 00:00:02.42]   Finished:    Tests.Domain
  Passed Tests.Domain.CollectionTests.AddItem_DuplicateQuoteId_ThrowsInvalidOperationException [1 s]
  Passed Tests.Domain.CollectionTests.Create_WithEmptyName_ThrowsArgumentException [< 1 ms]
  Passed Tests.Domain.CollectionTests.Create_WithNameOver80Chars_ThrowsArgumentException [< 1 ms]
  Passed Tests.Domain.CollectionTests.AddItem_51st_ThrowsInvalidOperationException [< 1 ms]
  Passed Tests.Domain.CollectionTests.AddThenRemoveItem_LeavesZeroItems [3 ms]
  Passed Tests.Domain.CollectionTests.RemoveItem_NonExistent_ThrowsInvalidOperationException [3 ms]

Test Run Successful.
Total tests: 6
     Passed: 6
 Total time: 4.0633 Seconds
```

