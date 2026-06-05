import { HttpErrorResponse } from '@angular/common/http';
import { isValidationProblem, isProblemDetails } from './problem-details';

// One typed error the whole app catches on, instead of leaking HttpErrorResponse
// and forcing every caller to re-parse problem+json. `friendlyMessage` is always
// safe to render to a user; `fieldErrors` is populated only for 400 validation
// problems so a form can highlight individual fields.
export type AppErrorKind =
  | 'validation' // 400 ValidationProblemDetails
  | 'unauthorized' // 401
  | 'forbidden' // 403
  | 'notfound' // 404
  | 'server' // 5xx
  | 'network' // status 0: CORS / offline / DNS
  | 'unknown';

export class AppError extends Error {
  constructor(
    readonly kind: AppErrorKind,
    readonly status: number,
    readonly friendlyMessage: string,
    readonly fieldErrors?: Record<string, string[]>,
    override readonly cause?: unknown,
  ) {
    super(friendlyMessage);
    this.name = 'AppError';
  }
}

// Maps a raw HttpErrorResponse from the QuotesApi into an AppError.
// Grounded in the real responses: ValidationProblem on 400, Unauthorized() on
// 401, Forbid() on 403, NotFound() on 404, ProblemDetails on 5xx.
export function toAppError(err: HttpErrorResponse): AppError {
  // Browser/XHR-level failure: CORS rejection, server down, DNS. Angular reports
  // these as status 0 with an ErrorEvent/ProgressEvent body.
  if (err.status === 0) {
    return new AppError('network', 0, 'Cannot reach the server. Check your connection and try again.', undefined, err);
  }

  const body = err.error as unknown;

  if (err.status === 400 && isValidationProblem(body)) {
    const messages = Object.values(body.errors).flat();
    const friendly =
      messages.length > 0
        ? messages.join(' ')
        : (body.title ?? 'The request was invalid.');
    return new AppError('validation', 400, friendly, body.errors, err);
  }

  if (err.status === 401) {
    return new AppError('unauthorized', 401, 'Your session has expired. Please sign in again.', undefined, err);
  }

  if (err.status === 403) {
    return new AppError('forbidden', 403, "You don't have permission to do that.", undefined, err);
  }

  if (err.status === 404) {
    return new AppError('notfound', 404, 'That quote could not be found.', undefined, err);
  }

  if (err.status >= 500) {
    const detail = isProblemDetails(body) ? body.title : undefined;
    return new AppError('server', err.status, detail ?? 'Something went wrong on our end. Please try again shortly.', undefined, err);
  }

  return new AppError('unknown', err.status, 'An unexpected error occurred.', undefined, err);
}
