# Program Output

## dotnet build — OrderApi

```
Determining projects to restore...
All projects are up-to-date for restore.
OrderApi -> ...\OrderApi\bin\Debug\net10.0\OrderApi.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.46
```

## dotnet test — OrderApi.Tests

```
Determining projects to restore...
All projects are up-to-date for restore.
OrderApi -> ...\OrderApi\bin\Debug\net10.0\OrderApi.dll
OrderApi.Tests -> ...\OrderApi.Tests\bin\Debug\net10.0\OrderApi.Tests.dll

Test run for OrderApi.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v2.8.2+699d445a1a (64-bit .NET 10.0.8)
[xUnit.net 00:00:00.11]   Discovering: OrderApi.Tests
[xUnit.net 00:00:00.16]   Discovered:  OrderApi.Tests
[xUnit.net 00:00:00.16]   Starting:    OrderApi.Tests
[xUnit.net 00:00:00.97]   Finished:    OrderApi.Tests

  Passed  OrderApi.Tests.Services.OrderServiceClockTests.EstimatedDelivery_IsThreeDaysAfterClockTime_NotSystemTime [709 ms]
  Passed  OrderApi.Tests.Services.OrderServiceClockTests.OrderDate_IsSetFromClockNotSystemTime [44 ms]

Test Run Successful.
Total tests: 2
     Passed: 2
 Total time: 1.5406 Seconds
```
