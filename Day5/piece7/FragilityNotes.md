# Fragility Notes

Things observed during the smoke test that feel fragile or are likely to cause future pain.

---

## 1. `POST /api/auth/refresh` silently 500s on wrong field name

**Severity: High**

`RefreshRequest` binds `refresh_token` (snake_case via `[JsonPropertyName]`), but a consumer sending the camelCase `refreshToken` gets a **500** instead of a 400. The field deserialization silently produces `null`, which then reaches `TokenHasher.Hash(null)` → `Encoding.UTF8.GetBytes(null)` → `ArgumentNullException`. The exception is unhandled and bubbles up as a 500 with `"detail":"Value cannot be null. (Parameter 's')"`.

The same applies to `POST /api/auth/logout`.

**Fix:** Null-guard `RefreshRequest.RefreshToken` in the endpoint (return 400) or add `[Required]` validation to the record.

---

## 2. Naming inconsistency: request vs. response field casing

**Severity: Medium**

| Surface | Field convention | Example |
|---------|-----------------|---------|
| `LoginRequest` body | lowercase (`email`, `password`) | `{"email":"…"}` |
| `RefreshRequest` body | snake_case (`refresh_token`) | `{"refresh_token":"…"}` |
| `LoginResponse` body | snake_case (`access_token`, `refresh_token`, `expires_in`) | `{"access_token":"…"}` |
| `CreateQuoteRequest` body | camelCase (`author`, `text`) | inferred from binding defaults |

No single naming convention is applied consistently. A client SDK author or documentation reader has to discover the correct field names by trial and error.

---

## 3. `GET /api/quotes/` — `page` and `size` are mandatory with no defaults

**Severity: Medium**

```csharp
group.MapGet("/", async (int page, int size, …) => …);
```

Omitting either query parameter returns a 400. There is no documented default. Callers who "try the URL" first without parameters will get an error that is indistinguishable from a permanent API problem. Conventional REST list endpoints default to `page=1&size=20`.

---

## 4. Pagination response is a flat array — no total count

**Severity: Medium**

`GET /api/quotes/?page=1&size=10` returns a raw JSON array. There is no envelope providing `total`, `totalPages`, or `hasNextPage`. A client cannot know whether they are on the last page without requesting an empty page and checking for an empty array.

---

## 5. SQLite in-container with no persistent volume

**Severity: High (for production use)**

The database file lives inside the container filesystem. Any container restart, redeploy, or scale-out event wipes all quotes and refresh tokens. The seeded `test@example.com` user is re-created on each cold start, but anything written at runtime is ephemeral.

---

## 6. Only one hardcoded user — no registration endpoint

**Severity: Medium**

The only way to get credentials is the seed in `Program.cs` (`test@example.com` / `password123`). There is no `POST /api/auth/register`. Any real consumer needs to create users out-of-band (direct DB access). The `can-delete-own-quote` policy is never falsified in practice because all quotes share the same single owner.

---

## 7. Internal error details leak in 500 responses

**Severity: Low–Medium**

500 responses currently include the exception message:
```json
{"title":"Server Error","status":500,"detail":"Value cannot be null. (Parameter 's')"}
```
This exposes internal parameter names. In production the `detail` field should be suppressed or replaced with a generic message.

---

## 8. No rate limiting on auth endpoints

**Severity: Low**

`POST /api/auth/login` has no rate limiting. An attacker can submit unlimited credential attempts. Brute-force protection (e.g., `Microsoft.AspNetCore.RateLimiting`) is absent.

---

## 9. `GET /health` returns plain text, not JSON

**Severity: Low**

The `/health` endpoint returns the string `Healthy` (content-type: `text/plain`). Clients that parse all responses as JSON will fail. If the health check is consumed by a monitoring tool expecting JSON, it may misinterpret the response.

---

## 10. `DELETE /api/quotes/{id}` — not-own path not exercised

**Severity: Low (test gap)**

Because only one user exists, the 403-Forbidden path in `OwnQuoteHandler` was never hit during smoke testing. The custom `can-delete-own-quote` policy is untested end-to-end against the deployed instance.
