# App Insights Setup

## QuotesApi.csproj

```xml
<PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" Version="1.3.2" />
<PackageReference Include="Azure.Identity" Version="1.13.2" />
<PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" Version="1.3.0" />
```

## Program.cs

```csharp
var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrEmpty(keyVaultUri))
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
```

## InfrastructureExtensions.cs

```csharp
services.AddOpenTelemetry()
    .WithTracing(t => t
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("QuotesApi"))
        .AddSource(QuoteEndpoints.ActivitySourceName)
        .AddSource(AuthEndpoints.ActivitySourceName)
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .UseAzureMonitor(options =>
        options.ConnectionString = configuration["ApplicationInsights:ConnectionString"]);
```

## appsettings.json

```json
"KeyVault": {
  "Uri": "https://quotesapi-kv.vault.azure.net/"
},
"ApplicationInsights": {
  "ConnectionString": ""
}
```
