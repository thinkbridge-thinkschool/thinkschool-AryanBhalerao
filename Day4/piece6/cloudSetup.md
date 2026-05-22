# Cloud Setup

## App Insights
- Portal → **Application Insights** → Create → Workspace-based, same region as App Service.
- Copy the **Connection String** from the Overview blade.

## Key Vault
- Portal → **Key vaults** → Create → Permission model: **Azure RBAC**.
- **Secrets** → Generate/Import → Name: `ApplicationInsights--ConnectionString`, Value: connection string from above.

## Access
- Key Vault → **IAM** → Add role assignment → **Key Vault Secrets User** → your Azure account (dev).
- App Service → **Identity** → System assigned → On → copy principal ID → Key Vault IAM → **Key Vault Secrets User** → that managed identity (prod).

## App Service config
- Add environment variable `KeyVault__Uri` = `https://quotesapi-kv.vault.azure.net/`.

## Alert
- App Insights → **Alerts** → Create rule → Signal: `requests/duration`, Avg > 500 ms, 5-min window, filter `request/name = POST /api/quotes` → Action group → Email.
