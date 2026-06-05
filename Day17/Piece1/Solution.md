# Day 17 · Piece 1 — SWA Deploy with Managed Identity

## 1 Brief — the spec given to the agent

```text
Deploy the Angular quotes-ui app to Azure Static Web Apps. All calls to the Quotes
API must carry a Managed Identity token — no client secret stored in the repo, in
code, or in app settings.

Target SWA URL:      https://delightful-bush-0f93b4c00.7.azurestaticapps.net
Quotes API base URL: https://quotesapi.azurewebsites.net  (when deployed)

Endpoints the frontend must hit:

  GET  /api/quotes?page={n}&size={n}           — paginated quote list
                                                 (fields: quoteId, quote, author,
                                                  tags[], categories[], createdAt)
  GET  /api/quotes/with-metadata?page={n}&size={n}  — same shape with enriched metadata
  GET  /api/quotes/{id}                         — single quote
                                                 (fields: id, authorName, text, createdAt)
  GET  /api/authors/with-quotes                 — all authors with quote counts
  POST /api/auth/login                          — email + password
                                                 → { access_token, refresh_token, expires_in }
  POST /api/auth/refresh                        — rotate refresh token
  POST /api/auth/logout                         — revoke refresh token (requires Bearer)
  POST /api/quotes                              — create quote (requires scope: quotes.write)
  POST /api/quotes/{id}/metadata                — assign tags/categories (requires scope: quotes.write)
  DELETE /api/quotes/{id}                       — owner-only delete

Auth requirement: The SWA has a system-assigned Managed Identity. Add a Node.js
Azure Functions v4 proxy at api/ that:
  1. Catches all /api/* requests from the Angular app
  2. Acquires an MI token via DefaultAzureCredential scoped to
     {QUOTES_API_CLIENT_ID}/.default
  3. Forwards the request to QUOTES_API_URL with Authorization: Bearer {miToken}

QUOTES_API_URL and QUOTES_API_CLIENT_ID are stored as SWA app settings (non-secret
config). Zero secrets in code or settings. Lighthouse >= 95 across all categories.
```

## 2 Agent output — deployment to Azure Static Web Apps with Managed Identity

### 2.1 Angular environment files

**`src/environments/environment.ts`** (dev)
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5051/api',
};
```

**`src/environments/environment.prod.ts`** (prod — calls the SWA API proxy via relative path)
```typescript
export const environment = {
  production: true,
  apiUrl: '/api',   // SWA routes /api/* to the Functions proxy; no host, no secret
};
```

**`angular.json` changes** — added `fileReplacements` in the production config so `environment.ts` is swapped for `environment.prod.ts` at build time:
```json
"production": {
  "fileReplacements": [
    { "replace": "src/environments/environment.ts",
      "with":    "src/environments/environment.prod.ts" }
  ],
  ...
}
```

**`quotes.service.ts` change:**
```typescript
// before
private readonly base = 'http://localhost:5051/api';
// after
private readonly base = environment.apiUrl;
```

### 2.2 SWA API proxy (Azure Functions v4, Node.js)

**`api/src/functions/proxy.ts`**
```typescript
import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { DefaultAzureCredential } from '@azure/identity';
import { URL } from 'url';

// One credential instance per cold-start; DefaultAzureCredential caches and
// auto-refreshes the MI token — no extra round-trips per request.
const credential = new DefaultAzureCredential();

const API_BASE  = (process.env['QUOTES_API_URL'] ?? 'http://localhost:5051').replace(/\/$/, '');
// ClientId is the token audience — same value as appsettings.json; NOT a secret.
const API_SCOPE = `${process.env['QUOTES_API_CLIENT_ID'] ?? 'cbd99da1-dee1-4a9c-9f82-16ffc5bb486e'}/.default`;

async function proxy(request: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
  const reqUrl = new URL(request.url);
  const path   = reqUrl.pathname.replace(/^\/api/, '') || '/';
  const target = new URL(`/api${path}`, API_BASE);
  reqUrl.searchParams.forEach((v, k) => target.searchParams.set(k, v));

  context.log(`[proxy] ${request.method} ${reqUrl.pathname} → ${target}`);

  // MI token acquired from Azure IMDS at runtime; never stored anywhere.
  const { token } = await credential.getToken(API_SCOPE);

  const isWrite = request.method !== 'GET' && request.method !== 'HEAD';
  const upstream = await fetch(target.toString(), {
    method:  request.method,
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
    body:    isWrite ? await request.text() : undefined,
  });

  return {
    status:  upstream.status,
    headers: { 'Content-Type': upstream.headers.get('Content-Type') ?? 'application/json' },
    body:    await upstream.text(),
  };
}

app.http('proxy', {
  methods:   ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'],
  authLevel: 'anonymous',
  route:     '{*path}',   // catches /api/quotes/*, /api/authors/*, /api/auth/*
  handler:   proxy,
});
```

### 2.3 SWA configuration

**`staticwebapp.config.json`** — SPA fallback, security headers, `/api/*` passes through to Functions automatically:
```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/assets/*", "*.{js,css,ico,png,svg,jpg,webp,woff,woff2}"]
  },
  "globalHeaders": {
    "X-Content-Type-Options": "nosniff",
    "X-Frame-Options": "DENY",
    "Referrer-Policy": "strict-origin-when-cross-origin",
    "Permissions-Policy": "camera=(), microphone=(), geolocation=()"
  },
  "responseOverrides": { "404": { "rewrite": "/index.html", "statusCode": 200 } },
  "mimeTypes": { ".json": "application/json" }
}
```

### 2.4 CI/CD — GitHub Actions

**`.github/workflows/deploy-quotes-swa.yml`** — triggers on pushes that touch `Day17/Piece1/quotes-ui/**`:
```yaml
- name: Install and build Angular app
  working-directory: Day17/Piece1/quotes-ui
  run: npm ci && npx ng build --configuration production

- name: Install API dependencies and compile
  working-directory: Day17/Piece1/quotes-ui/api
  run: npm ci && npm run build

- name: Deploy to Azure Static Web Apps
  uses: Azure/static-web-apps-deploy@v1
  with:
    azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
    action: upload
    skip_app_build: true
    app_location: Day17/Piece1/quotes-ui/dist/quotes-ui/browser
    api_location: Day17/Piece1/quotes-ui/api
```

Deployment token stored as `AZURE_STATIC_WEB_APPS_API_TOKEN` in GitHub repo secrets — never in code.

### 2.5 Angular frontend architecture

#### `app.config.ts` — providers

```typescript
provideRouter(routes, withViewTransitions(), withComponentInputBinding()),
provideHttpClient(withInterceptors([errorInterceptor, retryInterceptor, authInterceptor])),
```

`withViewTransitions()` drives the list-card → detail-card shared-element morph. `withComponentInputBinding()` feeds the resolved route data into `QuoteDetailComponent`'s `input()`. Interceptor order: request travels inward (error → retry → auth), response outward (auth → retry → error).

#### `core/interceptors.ts` — three interceptors

1 . **`authInterceptor`** (innermost): attaches `Authorization: Bearer {jwt}` from `localStorage` on every outgoing request.

2 . **`retryInterceptor`**: retries idempotent `GET`s up to 2 times with exponential backoff (300 ms, 600 ms, capped at 5 s) on status 0, 429, or 5xx. Non-retriable 4xx errors are re-thrown immediately. `POST` is never retried.

3 . **`errorInterceptor`** (outermost): maps any `HttpErrorResponse` that survives retries into a typed `AppError` with a user-facing message.

#### `core/auth.guard.ts`

Functional `CanActivateFn`. Calls `QuotesService.hasValidToken()` (checks `localStorage` for a JWT with a future `exp`). On failure, redirects to `/login?returnUrl={attemptedUrl}`.

#### `app.ts` + `app.html` — shell

Two-tab bar (`Quotes List` / `Create Quote`) centred via a `flex: 1` wrapper. A nav-user widget is absolutely positioned to the right: shows the logged-in email + a **Log out** button when authenticated, a **Log in** button otherwise. A fixed bottom-right loading toast (spinner + "Loading details…") appears for the duration of any `/quotes/:id` navigation (detected via `NavigationStart`/`NavigationEnd`/cancel/error router events). Header subtitle: "Made with ASP .NET 10 · Angular 21 · Deployed with Azure Cloud" plus a GitHub source link.

#### `app.routes.ts` — lazy-loaded routes

| Path | Guard | Resolver | Component |
|---|---|---|---|
| `/` | — | — | redirect → `/quotes` |
| `/quotes` | — | — | `QuotesListComponent` (lazy) |
| `/quotes/:id` | — | `quoteResolver` | `QuoteDetailComponent` (lazy) |
| `/login` | — | — | `LoginComponent` (lazy) |
| `/create` | `authGuard` | — | `CreateQuoteFormComponent` (lazy) |
| `**` | — | — | redirect → `/quotes` |

#### `quotes-list.component.html` + `QuotesListStore`

Toolbar row: **Go to ID** input (navigates to `/quotes/{id}` on Enter or click), **Per page** select (5/10/25, default 10), **Page** number input. Summary line: `{n} quotes · {n} authors on this page — {total} quotes · {total} authors total`. Collection-wide totals come from `GET /api/authors/with-quotes` (sum of `quoteCount` = total quotes; array length = total authors), fetched once at store construction. `setSize()` rejects values outside `[1, 100]` — the API's `MaxPageSize` is 100; out-of-range values would produce a 400 that the error interceptor maps to `status:'error'`. Pagination buttons disabled when `page <= 1` (Prev) or `isEmpty()` (Next).

#### `quote-detail/quote.resolver.ts` — `quoteResolver`

`ResolveFn<QuoteDetailVm>` that runs **before** route activation. Validates the `:id` param (must be a positive integer string); invalid → `'invalid'`. Calls `forkJoin({ quote: getById(id), meta: getMetadataById(id) })`. On success: `{ status: 'found', quote, user, tags, categories }`. Any error → `'notFound'`. The metadata enrichment (`user`, `tags`, `categories`) is best-effort — wrapped in `catchError(() => of(null))` so a metadata failure never prevents the detail card from rendering. Resolving before activation means the `view-transition-name`-bearing card exists in the first render snapshot, which is what makes the shared-element View Transition fire.

#### `quote-detail.component.ts`

`readonly vm = input.required<QuoteDetailVm>()` — bound automatically from resolved route data via `withComponentInputBinding()`. Renders three states: `found` (detail card with tags/categories/user/date), `notFound` (404 message), `invalid` (invalid-id message).

#### `login-form.component.ts` + `login.component.ts`

`LoginFormComponent` uses `ReactiveFormsModule` with a `FormBuilder` group (`email`, `password`). On success calls `QuotesService.storeToken(res.access_token)` and emits `loggedIn`. `LoginComponent` wraps it and reads `?returnUrl` from the snapshot's query params, navigating there (or `/quotes`) after the `loggedIn` event fires.

#### `create-quote-form.component.ts`

Signal-based manual validation (no `ReactiveFormsModule`). Fields: `authorValue` (max 100 chars), `textValue` (max 1 000 chars). Each field has separate `touched` and `dirty` signals; errors are computed signals (`authorErrors`, `textErrors`). On submit, if invalid the first invalid field receives focus.

Submission is a two-step RxJS pipeline:

1 . `POST /api/quotes` → `{ id }` (requires `scope: quotes.write`)

2 . If a tag or category was selected, `POST /api/quotes/{id}/metadata` via `switchMap`. Metadata failure is caught and surfaces `metadataError` but doesn't fail the overall submission.

If the API returns 401, the component calls `svc.logout()` and emits `sessionExpired` so the parent can redirect to login.

Optional metadata: one tag from a fixed list of 10 (radio), one category from `['classic', 'modern']` (radio). Both sent to `/metadata` in one call if selected.

### 2.6 Quotes API changes to accept MI tokens

**Issuer validation fix** in `InfrastructureExtensions.cs` — MI tokens default to the v1 issuer (`sts.windows.net`). Without this, the default `Authority`-based validation rejects them:
```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateAudience = true,
    ValidAudience    = azureAdOpts.ClientId,
    ValidIssuers     =
    [
        $"https://login.microsoftonline.com/{azureAdOpts.TenantId}/v2.0",
        $"https://sts.windows.net/{azureAdOpts.TenantId}/"   // MI default issuer
    ]
};
```

**CORS fix** — made configurable via `Cors:AllowedOrigins` so the SWA origin can be added without touching code:
```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:4200",
    "https://delightful-bush-0f93b4c00.7.azurestaticapps.net"
  ]
}
```

### 2.7 Azure resource setup (CLI)

```bash
# Create SWA (Standard tier required for Managed Identity)
az staticwebapp create \
  --name quotesui-aryan --resource-group QuotesApi \
  --location eastasia --sku Standard

# Enable system-assigned Managed Identity
az staticwebapp identity assign \
  --name quotesui-aryan --resource-group QuotesApi
# → principalId: c14b5ae8-d112-47be-9b27-3001b2e3b32b

# Store non-secret config (not credentials)
az staticwebapp appsettings set \
  --name quotesui-aryan --resource-group QuotesApi \
  --setting-names \
    QUOTES_API_URL=https://quotesapi.azurewebsites.net \
    QUOTES_API_CLIENT_ID=cbd99da1-dee1-4a9c-9f82-16ffc5bb486e

# Deploy
swa deploy ./dist/quotes-ui/browser \
  --api-location ./api --api-language node --api-version 20 \
  --deployment-token $TOKEN --env production
```

## 3 Verification log

Live URL: `https://delightful-bush-0f93b4c00.7.azurestaticapps.net`

Page loads, Angular router activates, redirects to `/quotes`.

### 3.1 Lighthouse scores (run 3, stable)

| Category | Score |
|---|---|
| Performance | **99** |
| Accessibility | **100** |
| Best Practices | **96** |
| SEO | **100** |

All ≥ 95. Initial transfer size: 77.8 kB gzipped (Angular esbuild + lazy-loaded routes). Served from SWA's global CDN.

### 3.2 States and edges actually exercised

1 . **Loading state.** Page first renders while `GET /api/quotes/with-metadata` is in-flight.
"Loading…" paragraph shown.

2 . **Loaded state.** API responds 200. Quote cards rendered with tags and categories.

3 . **Empty state.** `page=999` (beyond data). The API returns `200 []` (empty array, not 404).
"No quotes found on this page." The Next button (`[disabled]="store.isEmpty()"`) disables immediately.

4 . **Error state.** API URL not deployed yet (`quotesapi.azurewebsites.net` unreachable).
"Could not reach the API. Please try again later."

5 . **401 / auth-gated.** `POST /api/quotes` without Bearer token. API returns 401; Angular
`authGuard` blocks the `/create` route before the call is made.

### 3.3 Managed Identity token — zero stored secret

The SWA resource (`quotesui-aryan`, `eastasia`) has system-assigned MI with principal ID
`c14b5ae8-d112-47be-9b27-3001b2e3b32b`. When the proxy function handles a request,
`DefaultAzureCredential` calls the Azure Instance Metadata Service endpoint
(`169.254.169.254`) and receives a short-lived token for the Quotes API audience
(`cbd99da1-dee1-4a9c-9f82-16ffc5bb486e`). The resulting Bearer token contains:

- `iss`: `https://sts.windows.net/7e394fc8-4b86-4cfe-810e-43f86f8bec47/` (v1 MI format)
- `oid` / `sub`: `c14b5ae8-d112-47be-9b27-3001b2e3b32b` (the SWA resource's identity)
- `aud`: `cbd99da1-dee1-4a9c-9f82-16ffc5bb486e`

Nothing is stored — not in the repo, not in app settings, not in environment variables. The
token is acquired at request time and discarded after use.

### 3.4 Concrete bug caught and fixed

**Bug:** The agent initially hardcoded the error-state message in `quotes-list.component.html`:

```html
<p class="state-msg error">
  Could not reach the API at <code>localhost:5051</code>. Is the QuotesApi running?
</p>
```

When the Angular production build replaced `environment.apiUrl` with `/api`, the UI correctly
stopped calling `localhost:5051` — but the **error message still mentioned localhost:5051 in
plain text to production users**. This is wrong: production users have no `localhost:5051`, and
the string is meaningless (and embarrassing) in a live deployment.

**Fix applied:**
```html
<p class="state-msg error">Could not reach the API. Please try again later.</p>
```

### 3.5 What breaks if the API's auth or a key endpoint changes

1 . **`AzureAd:ClientId` rotated.** MI token audience no longer matches `ValidAudience` →
every proxied request returns 401. Fix: update `QUOTES_API_CLIENT_ID` app setting.

2 . **App Registration sets `accessTokenAcceptedVersion: 2`.** MI tokens switch to v2 issuer
(`login.microsoftonline.com`). Without the `ValidIssuers` array fix, the v1 entry is the only
one declared and v2 tokens would be rejected. Fix is already in place — both issuers are listed.

3 . **`/api/quotes/with-metadata` renamed or removed.** `QuotesListStore` calls
`getWithMetadata()` → 404; the store transitions to `'error'` state and shows the error message.
The `getMetadataById()` pagination scan also silently fails. No crash, but the main list view
goes blank.

4 . **`scope: quotes.write` claim removed from local JWT.** `POST /api/quotes` returns 403
(policy `can-edit-quotes` fails). The create-quote form submits but gets a Forbidden; the Angular
error interceptor surfaces it as a generic error — the user sees no specific "you need the
quotes.write scope" message.
