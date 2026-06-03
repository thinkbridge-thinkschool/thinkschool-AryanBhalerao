import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AppError } from './app-error';
import {
  RETRY_BACKOFF,
  authInterceptor,
  errorInterceptor,
  retryInterceptor,
} from './interceptors';

// ---------------------------------------------------------------------------
// CHARACTERIZATION TEST — pins the real QuotesApi HTTP contract.
//
// This runs BEFORE any UI is wired to the new interceptor stack. It encodes,
// as executable fixtures, exactly what the backend returns today:
//
//   GET  /api/quotes?page=N&size=N  -> 200 [{ id, authorName, text, createdAt }]
//                                      (QuoteReadModel record, camelCased)
//   GET  /api/quotes?page=0&...     -> 400 application/problem+json
//                                      ValidationProblemDetails { errors: { page: [...] } }
//                                      (QuoteEndpoints.ValidatePaging)
//
// and the behaviour the interceptors must layer on top: Bearer auth, retry of
// idempotent GETs with backoff, and ProblemDetails -> AppError mapping.
// ---------------------------------------------------------------------------

const BASE = 'http://localhost:5051/api';

// Real row as returned by GET /api/quotes — note `authorName`, not `author`,
// and the trailing ISO `createdAt`. This is the contract, copied from a live
// response, not an invented example.
const QUOTE_ROW = {
  id: 1,
  authorName: 'Marcus Aurelius',
  text: 'You have power over your mind, not outside events.',
  createdAt: '2026-06-01T12:42:18.000+00:00',
};

// Real 400 body from ValidatePaging when page < 1.
const VALIDATION_PROBLEM = {
  type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
  title: 'One or more validation errors occurred.',
  status: 400,
  errors: { page: ['Page must be 1 or greater'] },
};

describe('QuotesApi contract (characterization)', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(
          withInterceptors([errorInterceptor, retryInterceptor, authInterceptor]),
        ),
        provideHttpClientTesting(),
        // Collapse exponential backoff to zero so retries resolve synchronously
        // in the test — the production token keeps the real timer.
        { provide: RETRY_BACKOFF, useValue: () => of(0) },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('GET /api/quotes?page=N&size=N returns rows shaped {id, authorName, text, createdAt}', () => {
    let body: typeof QUOTE_ROW[] | undefined;
    http
      .get<(typeof QUOTE_ROW)[]>(`${BASE}/quotes`, { params: { page: 1, size: 2 } })
      .subscribe((r) => (body = r));

    const req = httpMock.expectOne(
      (r) => r.url === `${BASE}/quotes` && r.params.get('page') === '1' && r.params.get('size') === '2',
    );
    expect(req.request.method).toBe('GET');
    req.flush([QUOTE_ROW]);

    expect(body).toHaveLength(1);
    // The pinned shape — if the read model ever renames AuthorName this breaks.
    expect(body![0]).toEqual(
      expect.objectContaining({
        id: 1,
        authorName: 'Marcus Aurelius',
        text: expect.any(String),
        createdAt: expect.any(String),
      }),
    );
    // Guards the exact assumption the agent got wrong: there is NO `author` key.
    expect('author' in body![0]).toBe(false);
  });

  it('attaches Bearer <jwt> from localStorage on outgoing requests', () => {
    localStorage.setItem('jwt', 'header.payload.sig');
    http.get(`${BASE}/quotes`, { params: { page: 1, size: 2 } }).subscribe();

    const req = httpMock.expectOne((r) => r.url === `${BASE}/quotes`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer header.payload.sig');
    req.flush([QUOTE_ROW]);
  });

  it('sends no Authorization header when there is no stored token', () => {
    http.get(`${BASE}/quotes`, { params: { page: 1, size: 2 } }).subscribe();
    const req = httpMock.expectOne((r) => r.url === `${BASE}/quotes`);
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([QUOTE_ROW]);
  });

  it('retries an idempotent GET on a transient 503 and succeeds on the retry', () => {
    let body: (typeof QUOTE_ROW)[] | undefined;
    http.get<(typeof QUOTE_ROW)[]>(`${BASE}/quotes`, { params: { page: 1, size: 2 } }).subscribe((r) => (body = r));

    // First attempt fails transiently...
    httpMock
      .expectOne((r) => r.url === `${BASE}/quotes`)
      .flush('', { status: 503, statusText: 'Service Unavailable' });

    // ...backoff is of(0), so the retry is issued synchronously. Second attempt succeeds.
    httpMock.expectOne((r) => r.url === `${BASE}/quotes`).flush([QUOTE_ROW]);

    expect(body).toHaveLength(1);
  });

  it('does NOT retry a non-idempotent POST', () => {
    let caught: AppError | undefined;
    localStorage.setItem('jwt', 'header.payload.sig');
    http
      .post(`${BASE}/quotes`, { author: 'X', text: 'Y' })
      .subscribe({ error: (e) => (caught = e) });

    // Exactly one POST is ever made; a 503 surfaces immediately.
    httpMock
      .expectOne((r) => r.method === 'POST' && r.url === `${BASE}/quotes`)
      .flush('', { status: 503, statusText: 'Service Unavailable' });

    httpMock.expectNone((r) => r.url === `${BASE}/quotes`);
    expect(caught).toBeInstanceOf(AppError);
    expect(caught!.kind).toBe('server');
  });

  it('does NOT retry a GET that returns a 4xx — it surfaces at once', () => {
    let caught: AppError | undefined;
    http
      .get(`${BASE}/quotes`, { params: { page: 0, size: 2 } })
      .subscribe({ error: (e) => (caught = e) });

    httpMock
      .expectOne((r) => r.url === `${BASE}/quotes`)
      .flush(VALIDATION_PROBLEM, { status: 400, statusText: 'Bad Request' });

    httpMock.expectNone((r) => r.url === `${BASE}/quotes`); // no retry
    expect(caught).toBeInstanceOf(AppError);
  });

  it('maps a 400 ValidationProblemDetails to a typed AppError with a friendly message', () => {
    let caught: AppError | undefined;
    http
      .get(`${BASE}/quotes`, { params: { page: 0, size: 2 } })
      .subscribe({ error: (e) => (caught = e) });

    httpMock
      .expectOne((r) => r.url === `${BASE}/quotes`)
      .flush(VALIDATION_PROBLEM, { status: 400, statusText: 'Bad Request' });

    expect(caught).toBeInstanceOf(AppError);
    expect(caught!.kind).toBe('validation');
    expect(caught!.status).toBe(400);
    // The friendly message is the flattened, human-readable field message.
    expect(caught!.friendlyMessage).toBe('Page must be 1 or greater');
    expect(caught!.fieldErrors).toEqual({ page: ['Page must be 1 or greater'] });
  });

  it('maps a 401 (no/expired token on a guarded route) to an unauthorized AppError', () => {
    let caught: AppError | undefined;
    http
      .post(`${BASE}/quotes`, { author: 'A', text: 'B' })
      .subscribe({ error: (e) => (caught = e) });

    httpMock
      .expectOne((r) => r.method === 'POST' && r.url === `${BASE}/quotes`)
      .flush('', { status: 401, statusText: 'Unauthorized' });

    expect(caught!.kind).toBe('unauthorized');
    expect(caught!.friendlyMessage).toContain('sign in');
  });

  it('maps a network failure (status 0) to a friendly network AppError', () => {
    let caught: AppError | undefined;
    http
      .get(`${BASE}/quotes`, { params: { page: 1, size: 2 } })
      .subscribe({ error: (e) => (caught = e) });

    // status 0 is retriable, so it is attempted MAX_RETRIES+1 times before surfacing.
    for (let i = 0; i < 3; i++) {
      httpMock
        .expectOne((r) => r.url === `${BASE}/quotes`)
        .error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });
    }

    expect(caught!.kind).toBe('network');
    expect(caught!.friendlyMessage).toContain('Cannot reach the server');
  });
});
