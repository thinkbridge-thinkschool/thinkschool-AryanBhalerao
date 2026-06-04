import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { AuthorWithQuotes, QuoteMetadataReadModel, QuoteReadModel } from '../models/quote.model';
import { EMPTY, Observable } from 'rxjs';
import { expand, first, map } from 'rxjs/operators';

export interface LoginResponse {
  access_token: string;
  refresh_token: string;
  expires_in: number;
}

@Injectable({ providedIn: 'root' })
export class QuotesService {
  // inject() replaces constructor injection — no constructor body needed
  private readonly http = inject(HttpClient);
  private readonly base = 'http://localhost:5051/api';

  getPage(page: number, size: number) {
    const params = new HttpParams()
      .set('page', page)
      .set('size', size);
    return this.http.get<QuoteReadModel[]>(`${this.base}/quotes`, { params });
  }

  getById(id: number) {
    return this.http.get<QuoteReadModel>(`${this.base}/quotes/${id}`);
  }

  getWithMetadata(page: number, size: number) {
    const params = new HttpParams()
      .set('page', page)
      .set('size', size);
    return this.http.get<QuoteMetadataReadModel[]>(`${this.base}/quotes/with-metadata`, { params });
  }

  // Every author with their quote count — used to compute collection-wide totals
  // (Σ quoteCount = total quotes, list length = total authors).
  getAuthorsWithQuotes() {
    return this.http.get<AuthorWithQuotes[]>(`${this.base}/authors/with-quotes`);
  }

  // The by-id endpoint omits tags/categories, so to enrich a single searched
  // quote we page through /with-metadata and match client-side. Stops at the
  // first match or when a short page signals the end of the data.
  // NOTE: only finds quotes within the pages it scans (page size capped at 100
  // by the API); a hit far past the first pages still requires walking to it.
  getMetadataById(id: number, pageSize = 100): Observable<QuoteMetadataReadModel | null> {
    return this.getWithMetadata(1, pageSize).pipe(
      // index is 0 for the seed page, so the next page number is index + 2
      expand((batch, index) =>
        batch.length === pageSize ? this.getWithMetadata(index + 2, pageSize) : EMPTY
      ),
      map((batch) => batch.find((q) => q.quoteId === id) ?? null),
      // emit the first page-hit; if none of the pages matched, emit null
      first((m) => m !== null, null)
    );
  }

  // Both POST endpoints require the can-edit-quotes policy, so the stored JWT
  // must accompany them — without it the API answers 401 and nothing is saved.
  private authHeaders(): HttpHeaders | undefined {
    const token = localStorage.getItem('jwt');
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : undefined;
  }

  create(author: string, text: string): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(
      `${this.base}/quotes`,
      { author, text },
      { headers: this.authHeaders() },
    );
  }

  assignMetadata(id: number, tags: string[], categories: string[]): Observable<void> {
    return this.http.post<void>(
      `${this.base}/quotes/${id}/metadata`,
      { tags, categories },
      { headers: this.authHeaders() },
    );
  }

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/auth/login`, { email, password });
  }

  // True only when a stored token exists and its `exp` claim is still in the
  // future — presence alone isn't enough, access tokens expire after 15 min.
  hasValidToken(): boolean {
    const token = localStorage.getItem('jwt');
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      if (typeof payload.exp === 'number' && payload.exp * 1000 > Date.now()) {
        return true;
      }
    } catch {
      // malformed token — fall through and discard it
    }
    this.logout();
    return false;
  }

  logout(): void {
    localStorage.removeItem('jwt');
  }
}
