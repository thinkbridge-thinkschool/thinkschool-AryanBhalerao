# Azure Setup — Day 5 / Piece 4

## Live URL

```
https://ca-api-nb3bgcnwnlpwe.lemoncliff-d4727121.southeastasia.azurecontainerapps.io
```

---

## azure.yaml

```yaml
# yaml-language-server: $schema=https://raw.githubusercontent.com/Azure/azure-dev/main/schemas/v1.0/azure.yaml.json

name: quotes-api

services:
  api:
    project: ./QuotesApi
    language: dotnet
    host: containerapp
```

---

## azd up output

```
Initialize bicep provider

Provisioning and deploying (azd up)
Packaging overlaps with provisioning for faster execution.

  api: Packaging
Initialize bicep provider
  api: Packaging (Building Docker image)
Creating a deployment plan
Comparing deployment state
Validating deployment
  api: Packaging (Tagging container image)
Creating/Updating resources
  You can view detailed progress in the Azure Portal:
  https://portal.azure.com/#view/HubsExtension/DeploymentDetailsBlade/~/overview/id/...

  (✓) Done: Resource group: rg-quotesapi-dev (3.089s)
  (✓) Done: Container Registry: acrnb3bgcnwnlpwe (763ms)
  (✓) Done: Log Analytics workspace: log-nb3bgcnwnlpwe (21.382s)
  (✓) Done: Container Apps Environment: cae-nb3bgcnwnlpwe (51.034s)
  (✓) Done: Container App: ca-api-nb3bgcnwnlpwe (16.692s)
  api: Publishing
  api: Publishing (Tagging container image)
  api: Publishing (Logging into container registry)
  api: Publishing (Pushing container image) [1s]
  api: Deploying [25s]
  api: Deploying (Updating container app revision) [25s]
  api: Deploying (Waiting for container revision (15s)) [40s]
  api: Deploying (Fetching endpoints for service) [42s]
  api: Done [42s]
  - Endpoint: https://ca-api-nb3bgcnwnlpwe.lemoncliff-d4727121.southeastasia.azurecontainerapps.io/


SUCCESS: Your application was provisioned and deployed to Azure in 2 minutes 43 seconds.
  Provisioning: 2 minutes
  Deploying:    42 seconds
```

---

## Notes

- Region: `southeastasia` (required by subscription policy — allowed regions: koreacentral, southeastasia, eastasia, austriaeast, malaysiawest)
- Student subscription allows only **1 Container Apps Environment** globally; a fresh CAE (`cae-nb3bgcnwnlpwe`) was created after the previous one finished deleting.
- `Jwt__SigningKey` is injected as a Container Apps secret (never stored in source).
- `KeyVault__Uri` is overridden to empty string so the app skips Key Vault at startup.
- SQLite database is ephemeral (recreated with seed data on each container start) — acceptable for this demo.
