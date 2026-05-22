# Configurations

## JwtOptions class

```csharp
namespace QuotesApi.Options;

public record JwtOptions
{
    public string SigningKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(7);
}
```

`SigningKey` is a secret. it never goes in `appsettings.json`.

- **Local dev**: `dotnet user-secrets set Jwt:SigningKey "your-dev-key-min-32-bytes-here"`
- **Production**: set `Jwt__SigningKey` stored it in Azure Key Vault and referenced via Key Vault reference in the app configuration.

---

## appsettings.json section

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=quotes.db"
  },
  "Jwt": {
    "Issuer": "QuotesApi",
    "Audience": "QuotesApi",
    "AccessTokenLifetime": "00:15:00",
    "RefreshTokenLifetime": "7.00:00:00"
  },
  "AzureAd": {
    "TenantId": "0a0aa63d-82d0-4ba1-b909-d7986ece4c4c",
    "ClientId": "cbd99da1-dee1-4a9c-9f82-16ffc5bb486e"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "Enrich": [ "FromLogContext" ],
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {TraceId} {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "KeyVault": {
    "Uri": "https://quotesapi-kv.vault.azure.net/"
  },
  "ApplicationInsights": {
    "ConnectionString": ""
  },
  "AllowedHosts": "*"
}

```

## DI registration (`InfrastructureExtensions.cs`)

```csharp
services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

var jwtOpts = configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");
if (string.IsNullOrEmpty(jwtOpts.SigningKey))
    throw new InvalidOperationException(
        "Jwt:SigningKey is not configured. Run: dotnet user-secrets set Jwt:SigningKey <value>");
```

## Injecting in a service (endpoint handler)

```csharp
app.MapPost("/api/auth/login", async (
    LoginRequest request,
    AppDbContext db,
    IOptions<JwtOptions> jwtOptions,   // <-- injected here
    IClock clock) =>
{
    var opts = jwtOptions.Value;       // unwrap once per handler call

    var token = new JwtSecurityToken(
        issuer: opts.Issuer,
        audience: opts.Audience,
        expires: DateTime.UtcNow.Add(opts.AccessTokenLifetime),
        signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.SigningKey)),
            SecurityAlgorithms.HmacSha256));

    return Results.Ok(new JwtSecurityTokenHandler().WriteToken(token));
});
```
