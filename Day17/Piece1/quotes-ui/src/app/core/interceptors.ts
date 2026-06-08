import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { InjectionToken, inject } from '@angular/core';
import { Observable, throwError, timer } from 'rxjs';
import { catchError, retry } from 'rxjs/operators';
import { toAppError } from './app-error';

// ---------------------------------------------------------------------------
// 1. Auth headers — attach the stored JWT as Bearer on every outgoing request.
//    POST /api/quotes and POST /api/quotes/{id}/metadata are guarded by the
//    `can-edit-quotes` policy; without the header the API answers 401.
//    In production the Angular app is served from Azure SWA, which strips the
//    standard Authorization header before forwarding requests to the managed
//    Azure Function proxy. X-User-Authorization is a custom header that SWA
//    passes through unchanged; the proxy reads it and restores Authorization
//    when forwarding to the Container App backend.
// ---------------------------------------------------------------------------
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('jwt');
  if (!token) return next(req);
  // Azure SWA strips the Authorization header before forwarding to managed functions.
  // X-User-Authorization carries the JWT through the SWA proxy so the proxy function
  // can restore it when forwarding to the Container App backend.
  return next(req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`,
      'X-User-Authorization': `Bearer ${token}`,
    },
  }));
};

// ---------------------------------------------------------------------------
// 2. Retry idempotent GETs with exponential backoff.
//    Only GETs are retried (POST is not idempotent — a retried create would
//    duplicate a quote). Only transient failures are retried: status 0
//    (network), 429, and 5xx. A 4xx is a contract error — retrying it is
//    pointless, so the delay notifier rethrows immediately.
//    Backoff is injected so tests can collapse it to zero.
// ---------------------------------------------------------------------------
export const RETRY_BACKOFF = new InjectionToken<(retryCount: number) => Observable<number>>(
  'RETRY_BACKOFF',
  {
    providedIn: 'root',
    // 300ms, 600ms, 1200ms … capped at 5s.
    factory: () => (retryCount: number) => timer(Math.min(2 ** (retryCount - 1) * 300, 5000)),
  },
);

const MAX_RETRIES = 2;

function isRetriable(status: number): boolean {
  return status === 0 || status === 429 || (status >= 500 && status <= 599);
}

export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.method !== 'GET') return next(req);

  const backoff = inject(RETRY_BACKOFF);

  return next(req).pipe(
    retry({
      count: MAX_RETRIES,
      delay: (error: unknown, retryCount: number) => {
        const status = error instanceof HttpErrorResponse ? error.status : -1;
        if (!isRetriable(status)) throw error; // surface non-transient errors at once
        return backoff(retryCount);
      },
    }),
  );
};

// ---------------------------------------------------------------------------
// 3. Error mapping — convert any HttpErrorResponse that survives the retry pass
//    into a typed AppError carrying a friendly message. Outermost interceptor,
//    so it runs after retries are exhausted.
// ---------------------------------------------------------------------------
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((err: unknown) =>
      throwError(() => (err instanceof HttpErrorResponse ? toAppError(err) : err)),
    ),
  );
