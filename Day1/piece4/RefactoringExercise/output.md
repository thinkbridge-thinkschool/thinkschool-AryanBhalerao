# dotnet run

PS C:\Users\aryan\Desktop\thinkschool-AryanBhalerao\Day1\piece4\RefactoringExercise\src\OrderApi> dotnet run
Using launch settings from C:\Users\aryan\Desktop\thinkschool-AryanBhalerao\Day1\piece4\RefactoringExercise\src\OrderApi\Properties\launchSettings.json...
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5147
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\Users\aryan\Desktop\thinkschool-AryanBhalerao\Day1\piece4\RefactoringExercise\src\OrderApi

# dotnet test
PS C:\Users\aryan\Desktop\thinkschool-AryanBhalerao\Day1\piece4\RefactoringExercise\tests\OrderApi.Tests> dotnet test
Restore complete (0.5s)
  OrderApi net10.0 succeeded (0.4s) → C:\Users\aryan\Desktop\thinkschool-AryanBhalerao\Day1\piece4\RefactoringExercise\src\OrderApi\bin\Debug\net10.0\OrderApi.dll
  OrderApi.Tests net10.0 succeeded (0.9s) → bin\Debug\net10.0\OrderApi.Tests.dll
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.4+50e68bbb8b (64-bit .NET 10.0.8)
[xUnit.net 00:00:01.24]   Discovering: OrderApi.Tests
[xUnit.net 00:00:01.29]   Discovered:  OrderApi.Tests
[xUnit.net 00:00:01.33]   Starting:    OrderApi.Tests
info: Microsoft.EntityFrameworkCore.Update[30100]
      Saved 1 entities to in-memory store.
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\Users\aryan\Desktop\thinkschool-AryanBhalerao\Day1\piece4\RefactoringExercise\src\OrderApi
info: OrderApi.Services.OrderService[0]
      Order started for Jane Doe
info: Microsoft.EntityFrameworkCore.Update[30100]
      Saved 3 entities to in-memory store.
info: OrderApi.Services.OrderService[0]
      Sending email for order 1 to jane@example.com
info: Microsoft.Hosting.Lifetime[0]
      Application is shutting down...
[xUnit.net 00:00:03.10]   Finished:    OrderApi.Tests
  OrderApi.Tests test net10.0 succeeded (4.1s)

Test summary: total: 4, failed: 0, succeeded: 4, skipped: 0, duration: 4.1s
Build succeeded in 6.6s