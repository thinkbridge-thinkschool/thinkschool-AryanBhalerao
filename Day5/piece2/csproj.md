# Container properties in QuotesApi.csproj

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <UserSecretsId>b8c5f3a9-2e47-4d61-9f8c-1a3b7e2d5f0c</UserSecretsId>
  <ContainerImageName>quotes-api</ContainerImageName>
  <ContainerImageTag>0.1.0</ContainerImageTag>
  <ContainerBaseImage>mcr.microsoft.com/dotnet/aspnet:10.0</ContainerBaseImage>
</PropertyGroup>
```

## Notes

- `ContainerBaseImage` uses the standard Debian-based runtime instead of Alpine because
  `Microsoft.EntityFrameworkCore.Sqlite` ships a glibc-compiled `libe_sqlite3.so` that fails
  to load on Alpine (musl libc — missing `fcntl64` symbol). Alpine would require a custom
  SQLite provider or a base image with `libc6-compat`.
- Build command: `dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer`
- The SDK tooling warns that `ContainerImageName` is obsolete; the replacement property is
  `ContainerRepository`. Both produce the same local image tag.
