# Entra ID Token Test — QuotesApi

## Token retrieved in Azure Cloud Shell

```bash
TOKEN=$(az account get-access-token \
  --resource api://625aa1cf-d69d-4267-a1cb-8849773ac822 \
  --query accessToken \
  --output tsv)
```

Token issuer (v1.0): `https://sts.windows.net/7e394fc8-4b86-4cfe-810e-43f86f8bec47/`

---

## Request

```
POST http://localhost:5051/api/quotes
Authorization: Bearer <entra-token>
Content-Type: application/json

{"author":"Richard Feynman","text":"The first principle is that you must not fool yourself and you are the easiest person to fool."}
```

---

## Actual output

```
HTTP 201
```

```json
{
  "id": 4,
  "author": "Richard Feynman",
  "text": "The first principle is that you must not fool yourself and you are the easiest person to fool.",
  "isDeleted": false,
  "createdAt": "2026-05-21T05:19:08.5576065Z"
}
```

---

## Notes

The token from `az account get-access-token --resource` is a **v1.0** Entra token.  
Its issuer is `https://sts.windows.net/{tenant}/` rather than `https://login.microsoftonline.com/{tenant}/v2.0`.

Two fixes were applied to `InfrastructureExtensions.cs` to handle this:

1. `MultiScheme` forwarder checks for **both** `login.microsoftonline.com` and `sts.windows.net` so v1.0 tokens are routed to `EntraJwt` instead of falling back to `InternalJwt`.
2. `EntraJwt` scheme has explicit `ValidIssuers` containing both the v1.0 and v2.0 issuer URLs for the tenant.
