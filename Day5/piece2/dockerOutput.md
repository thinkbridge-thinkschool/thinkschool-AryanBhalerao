# docker run output

```
docker run -p 8080:8080 \
  -e "KeyVault__Uri=" \
  -e "Jwt__SigningKey=super-secret-dev-key-for-container-test-32chars!!" \
  -e "ConnectionStrings__Default=Data Source=/tmp/quotes.db" \
  quotes-api:0.1.0
```

```
[06:02:53 WRN]  Microsoft.AspNetCore.DataProtection.Repositories.FileSystemXmlRepository: Storing keys in a directory '/home/app/.aspnet/DataProtection-Keys' that may not be persisted outside of the container. Protected data will be unavailable when container is destroyed.
[06:02:53 WRN]  Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager: No XML encryptor configured. Key {ab1d7e14-d6ca-4752-85de-665ca2a63e47} may be persisted to storage in unencrypted form.
[06:02:53 INF]  Microsoft.Hosting.Lifetime: Now listening on: http://[::]:8080
[06:02:53 INF]  Microsoft.Hosting.Lifetime: Application started. Press Ctrl+C to shut down.
[06:02:53 INF]  Microsoft.Hosting.Lifetime: Hosting environment: Production
[06:02:53 INF]  Microsoft.Hosting.Lifetime: Content root path: /app
```

## Runtime env vars required

| Variable | Value |
|---|---|
| `KeyVault__Uri` | _(empty)_ | 
| `Jwt__SigningKey` | any 32+ char secret | 
| `ConnectionStrings__Default` | `Data Source=/tmp/quotes.db` |