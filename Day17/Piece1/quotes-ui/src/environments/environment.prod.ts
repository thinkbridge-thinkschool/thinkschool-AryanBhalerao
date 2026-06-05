export const environment = {
  production: true,
  // In production the Angular app is served from SWA; all /api/* requests are
  // handled by the SWA built-in API (Azure Functions proxy) so a relative path
  // is correct — no host, no stored URL, no secret.
  apiUrl: '/api',
};
