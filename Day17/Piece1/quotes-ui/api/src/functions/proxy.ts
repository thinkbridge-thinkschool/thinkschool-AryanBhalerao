import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { DefaultAzureCredential } from '@azure/identity';
import { URL } from 'url';

// Reuse the credential object across invocations — DefaultAzureCredential caches
// the token internally and refreshes it before expiry, so creating it once at
// module load avoids repeated MSI endpoint round-trips.
const credential = new DefaultAzureCredential();

const API_BASE = (process.env['QUOTES_API_URL'] ?? 'http://localhost:5051').replace(/\/$/, '');

// The App Registration client ID is the audience for the MI token.
// It is NOT a secret — it is the same value already published in appsettings.json.
const API_SCOPE = `${process.env['QUOTES_API_CLIENT_ID'] ?? 'abb9a212-0298-4302-985a-f5be1676d00d'}/.default`;

async function proxy(request: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
  const reqUrl = new URL(request.url);

  // SWA routes /api/* here; strip the leading /api so we can forward to
  // the Quotes API which also mounts its routes under /api.
  const path = reqUrl.pathname.replace(/^\/api/, '') || '/';
  const targetUrl = new URL(`/api${path}`, API_BASE);
  reqUrl.searchParams.forEach((val, key) => targetUrl.searchParams.set(key, val));

  context.log(`[proxy] ${request.method} ${reqUrl.pathname}${reqUrl.search} → ${targetUrl}`);

  // Azure SWA strips the Authorization header before forwarding to managed functions.
  // The Angular authInterceptor also sends the JWT in X-User-Authorization so it
  // survives the SWA proxy. Prefer Authorization (local dev / direct calls) and
  // fall back to X-User-Authorization (production SWA path).
  // If neither is present, fall back to a Managed Identity token for anonymous GETs.
  const clientAuth = request.headers.get('Authorization')
    ?? request.headers.get('X-User-Authorization');

  let authHeader: string | undefined;
  if (clientAuth) {
    authHeader = clientAuth;
  } else {
    try {
      const { token } = await credential.getToken(API_SCOPE);
      authHeader = `Bearer ${token}`;
    } catch {
      // MI token unavailable (e.g. app role not yet assigned) — proceed without auth.
      // Anonymous GET endpoints will still succeed; protected endpoints will 401.
      context.log('[proxy] MI token unavailable, forwarding without Authorization header');
    }
  }

  const isWrite = request.method !== 'GET' && request.method !== 'HEAD';
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  if (authHeader) headers['Authorization'] = authHeader;

  const upstream = await fetch(targetUrl.toString(), {
    method: request.method,
    headers,
    body: isWrite ? await request.text() : undefined,
  });

  const body = await upstream.text();
  const contentType = upstream.headers.get('Content-Type') ?? 'application/json';

  return {
    status: upstream.status,
    headers: { 'Content-Type': contentType },
    body,
  };
}

app.http('proxy', {
  methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'],
  authLevel: 'anonymous',
  // Catch-all route: handles /api/quotes/*, /api/authors/*, /api/auth/*
  route: '{*path}',
  handler: proxy,
});
