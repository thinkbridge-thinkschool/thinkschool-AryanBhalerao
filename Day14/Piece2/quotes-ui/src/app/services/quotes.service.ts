import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { QuoteMetadataReadModel, QuoteReadModel } from '../models/quote.model';
import { Observable } from 'rxjs';

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

  create(author: string, text: string): Observable<{ id: number }> {
    const token = localStorage.getItem('jwt');
    const headers = token
      ? new HttpHeaders({ Authorization: `Bearer ${token}` })
      : undefined;
    return this.http.post<{ id: number }>(`${this.base}/quotes`, { author, text }, { headers });
  }

  assignMetadata(id: number, tags: string[], categories: string[]): Observable<void> {
    return this.http.post<void>(`${this.base}/quotes/${id}/metadata`, { tags, categories });
  }

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/auth/login`, { email, password });
  }

  logout(): void {
    localStorage.removeItem('jwt');
  }
}
