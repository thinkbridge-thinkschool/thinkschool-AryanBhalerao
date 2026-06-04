// RFC 9457 problem-details shapes as the QuotesApi actually emits them.
//
// ASP.NET Core minimal APIs return two flavours, both with content-type
// `application/problem+json`:
//
//   Results.ValidationProblem(errors)  -> ValidationProblemDetails (has `errors`)
//   new ProblemDetails { ... }         -> ProblemDetails (no `errors`)
//
// Confirmed against QuotesApi/Endpoints/QuoteEndpoints.cs (ValidatePaging) and
// QuotesApi/Middleware/GlobalExceptionHandler.cs.

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
}

export interface ValidationProblemDetails extends ProblemDetails {
  // Field name -> list of human-readable messages, e.g. { page: ["Page must be 1 or greater"] }
  errors: Record<string, string[]>;
}

export function isValidationProblem(body: unknown): body is ValidationProblemDetails {
  return (
    typeof body === 'object' &&
    body !== null &&
    'errors' in body &&
    typeof (body as { errors: unknown }).errors === 'object' &&
    (body as { errors: unknown }).errors !== null
  );
}

export function isProblemDetails(body: unknown): body is ProblemDetails {
  return (
    typeof body === 'object' &&
    body !== null &&
    ('title' in body || 'status' in body || 'detail' in body)
  );
}
