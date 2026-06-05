import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { AuthorWithQuotes, QuoteMetadataReadModel, QuoteReadModel } from '../models/quote.model';
import { EMPTY, Observable } from 'rxjs';
import { expand, first, map } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface LoginResponse {
  access_token: string;
  refresh_token: string;
  expires_in: number;
}

@Injectable({ providedIn: 'root' })
export class QuotesService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  private readonly _loggedIn = signal(false);
  readonly loggedIn = this._loggedIn.asReadonly();

  readonly currentUserEmail = computed((): string | null => {
    if (!this._loggedIn()) return null;
    const token = localStorage.getItem('jwt');
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.email ?? payload.sub ?? null;
    } catch {
      return null;
    }
  });

  constructor() {
    if (this.hasValidToken()) {
      this._loggedIn.set(true);
    }
  }

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

  getAuthorsWithQuotes() {
    return this.http.get<AuthorWithQuotes[]>(`${this.base}/authors/with-quotes`);
  }

  getMetadataById(id: number, pageSize = 100): Observable<QuoteMetadataReadModel | null> {
    return this.getWithMetadata(1, pageSize).pipe(
      expand((batch, index) =>
        batch.length === pageSize ? this.getWithMetadata(index + 2, pageSize) : EMPTY
      ),
      map((batch) => batch.find((q) => q.quoteId === id) ?? null),
      first((m) => m !== null, null)
    );
  }

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

  storeToken(token: string): void {
    localStorage.setItem('jwt', token);
    this._loggedIn.set(true);
  }

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
    localStorage.removeItem('jwt');
    this._loggedIn.set(false);
    return false;
  }

  logout(): void {
    localStorage.removeItem('jwt');
    this._loggedIn.set(false);
  }
}
