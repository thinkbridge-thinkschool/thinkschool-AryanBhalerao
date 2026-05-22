# Correlated Log Output

Five lines from a single `POST /api/quotes` request. Every line carries the same
`TraceId` (`36d2c32ce30d7925896456349f69f6c0`), linking them across EF Core, the
application layer, and the request-summary middleware.

```
[14:07:19 DBG] 36d2c32ce30d7925896456349f69f6c0 Microsoft.EntityFrameworkCore.Database.Command: Creating DbCommand for 'ExecuteReader' (0ms).
[14:07:19 DBG] 36d2c32ce30d7925896456349f69f6c0 Microsoft.EntityFrameworkCore.Database.Command: Executing DbCommand [Parameters=[@p0='?' (Size = 15), @p1='?' (DbType = DateTimeOffset), @p2='?' (DbType = Int32), @p3='?' (Size = 50)], CommandType='Text', CommandTimeout='30']
[14:07:19 INF] 36d2c32ce30d7925896456349f69f6c0 Microsoft.EntityFrameworkCore.Database.Command: Executed DbCommand (0ms) [Parameters=[@p0='?' (Size = 15), @p1='?' (DbType = DateTimeOffset), @p2='?' (DbType = Int32), @p3='?' (Size = 50)], CommandType='Text', CommandTimeout='30']
[14:07:19 INF] 36d2c32ce30d7925896456349f69f6c0 QuotesApi.Quotes: Created quote 1 for user 1
[14:07:19 INF] 36d2c32ce30d7925896456349f69f6c0 Serilog.AspNetCore.RequestLoggingMiddleware: HTTP POST /api/quotes responded 201 in 154.5043 ms
```

The adjacent login request (`543e14a5...`) and list request (`935bdeac...`) carry
*different* TraceIds, proving the scope is per-request and does not bleed across
concurrent connections.

```
[14:07:19 INF] 543e14a5ec636230dedb959e3de67a81 QuotesApi.Auth: Login succeeded for user 1 (test@example.com)
[14:07:19 INF] 543e14a5ec636230dedb959e3de67a81 Serilog.AspNetCore.RequestLoggingMiddleware: HTTP POST /api/auth/login responded 200 in 671.2027 ms
[14:07:19 INF] 935bdeac443d9c62e6aa8deb78d50cfd Serilog.AspNetCore.RequestLoggingMiddleware: HTTP GET /api/quotes responded 200 in 22.8905 ms
```