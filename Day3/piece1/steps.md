# Day 3 — Wire Entra ID into QuotesApi

Goal: keep the existing `/api/auth/login` flow (internal JWT) working **and** also accept Entra ID tokens from a SPA. Both token types hit the same `[Authorize]`-protected endpoints transparently.

---

## Step 1 — Register the API in the Azure Portal

1. Open [portal.azure.com](https://portal.azure.com) → **Azure Active Directory** → **App registrations** → **New registration**.
2. Fill in:
   - **Name**: `QuotesApi`
   - **Supported account types**: *Accounts in this organizational directory only*
   - **Redirect URI**: leave blank (pure API, no interactive login)
3. Click **Register**.
4. On the overview page, copy:
   - **Application (client) ID** — you'll use this as `ClientId`
   - **Directory (tenant) ID** — goes into the Authority URL
5. Go to **Expose an API**:
   - Click **Set** next to the Application ID URI → accept the default `api://{client-id}`.
   - Click **Add a scope** → name it `access_as_user`, admin + user consent, enable it.

---

## Step 2 — Fill in `appsettings.json`

Open [`QuotesApi/appsettings.json`](QuotesApi/appsettings.json).  
The `AzureAd` section was already added — replace the three placeholders:

```json
"AzureAd": {
  "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "ClientId": "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy",
  "Audience": "api://yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy"
}
```

`Audience` must match the **Application ID URI** exactly (`api://{client-id}`).

---

## Step 3 — What changed in `InfrastructureExtensions.cs`

[`QuotesApi/Extensions/InfrastructureExtensions.cs`](QuotesApi/Extensions/InfrastructureExtensions.cs) now registers three authentication handlers instead of one:

| Scheme | Purpose | Key validation |
|---|---|---|
| `InternalJwt` | Tokens from `/api/auth/login` | Symmetric key from `Jwt:Key` |
| `EntraJwt` | Tokens from Entra ID / SPA | OIDC discovery (auto-fetched public keys) |
| `MultiScheme` | Policy scheme — picks the right handler | Peeks at `iss` claim before routing |

The `MultiScheme` policy reads the raw JWT header, checks if the `iss` claim contains `login.microsoftonline.com`, and forwards to `EntraJwt`; everything else falls back to `InternalJwt`.

No changes were needed in `Program.cs`, `EndpointExtensions.cs`, or `AuthEndpointExtensions.cs` — `[Authorize]` / `.RequireAuthorization()` already works because `MultiScheme` is now the default.

---

## Step 4 — Run the API

```bash
cd QuotesApi
dotnet run
```

On startup the API fetches Entra's OIDC discovery document to cache the public keys. You'll see a log line from `Microsoft.AspNetCore.Authentication`.

---

## Step 5 — Test with an Entra token

### Option A — Azure CLI

```bash
# Log in
az login

# Get a token scoped to your API
az account get-access-token \
  --resource api://YOUR_CLIENT_ID \
  --query accessToken -o tsv
```

```bash
# Call a protected endpoint
curl -X POST https://localhost:5001/api/quotes \
  -H "Authorization: Bearer <entra-token>" \
  -H "Content-Type: application/json" \
  -d '{"author":"Feynman","text":"Imagination is more important than knowledge."}'
```

Expected: `201 Created`.

### Option B — MSAL.js (SPA)

```js
const result = await msalInstance.acquireTokenSilent({
    scopes: ["api://YOUR_CLIENT_ID/access_as_user"]
});

await fetch("https://localhost:5001/api/quotes", {
    method: "POST",
    headers: {
        "Authorization": `Bearer ${result.accessToken}`,
        "Content-Type": "application/json"
    },
    body: JSON.stringify({ author: "Feynman", text: "..." })
});
```

---

## Step 6 — Verify the internal JWT still works

1. Get an internal token:
   ```bash
   curl -X POST https://localhost:5001/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com","password":"Password123!"}'
   ```

2. Use the returned `access_token` against a protected endpoint:
   ```bash
   curl -X POST https://localhost:5001/api/quotes \
     -H "Authorization: Bearer <internal-token>" \
     -H "Content-Type: application/json" \
     -d '{"author":"Einstein","text":"Logic will get you from A to Z."}'
   ```

Expected: `201 Created` — the `InternalJwt` scheme handles this path unchanged.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `401` on Entra token | Wrong `Audience` | Must be `api://{client-id}`, not the GUID alone |
| `401` on internal token | Route defaulted to `EntraJwt` | Check the `iss` claim in your token (use jwt.io) — it must not contain `login.microsoftonline.com` |
| `AADSTS70011` | Scope not found | Add `access_as_user` scope under **Expose an API** in the portal |
| `IDX20803` at startup | Can't reach Entra discovery URL | Check internet access / firewall; Entra metadata endpoint: `https://login.microsoftonline.com/{tenant}/v2.0/.well-known/openid-configuration` |
| `IDX10205` (lifetime) | Clock skew > 5 min | Sync machine clock; Entra tokens default to 1 hour lifetime |

---

## How it works — flow diagram

```
Request arrives with Bearer token
         │
         ▼
   MultiScheme.ForwardDefaultSelector()
         │
         ├─ iss contains "login.microsoftonline.com"? ──► EntraJwt handler
         │                                                  (Authority + Audience validation)
         │
         └─ everything else ──────────────────────────► InternalJwt handler
                                                          (symmetric key validation)
```
