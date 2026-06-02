import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { QuoteMetadataReadModel, QuoteReadModel } from '../models/quote.model';

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

  // POST /api/quotes — requires Bearer JWT with the "can-edit-quotes" scope.
  // Token is read from localStorage; a 401 surfaces as a server-error in the form.
  createQuote(author: string, text: string) {
    const token = localStorage.getItem('jwt') ?? '';
    return this.http.post<{ id: number }>(`${this.base}/quotes`, { author, text }, {
      headers: { Authorization: `Bearer ${token}` },
    });
  }
}
