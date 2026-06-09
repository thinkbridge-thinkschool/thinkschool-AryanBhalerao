import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { QuotesService } from '../services/quotes.service';
import { QuoteReadModel } from '../models/quote.model';

export type DetailStatus = 'found' | 'notFound' | 'invalid';

// The view model the detail route resolves to. It is produced BEFORE the route
// activates, so QuoteDetailComponent renders its final state (and the
// `view-transition-name` element) synchronously — which is what lets the
// list-card -> detail-card shared-element View Transition actually morph.
export interface QuoteDetailVm {
  status: DetailStatus;
  quote: QuoteReadModel | null;
  // `user`, tags and categories are NOT on the by-id row (QuoteReadModel =
  // Id/AuthorName/Text/CreatedAt). They live only on GET /api/quotes/with-metadata,
  // so we enrich the detail from there. Best-effort: null/empty if that lookup
  // fails. `user` is the account that posted the quote (the API returns
  // "anonymous" when there is no owner); the quote's author is quote.authorName.
  user: string | null;
  tags: string[];
  categories: string[];
}

const empty = (status: DetailStatus): QuoteDetailVm => ({
  status,
  quote: null,
  user: null,
  tags: [],
  categories: [],
});

// Resolves GET /api/quotes/{id} for the :id route param. A non-integer/<=0 param
// never matches the server's `{id:int}` route, so we short-circuit to 'invalid'
// without a request; a real 404 (or any error) becomes 'notFound'. On success
// the quote is enriched with tags/categories from /with-metadata.
export const quoteResolver: ResolveFn<QuoteDetailVm> = (route) => {
  const svc = inject(QuotesService);
  const raw = route.paramMap.get('id');

  if (raw == null || !/^\d+$/.test(raw)) {
    return of(empty('invalid'));
  }
  const id = Number(raw);
  if (!Number.isSafeInteger(id) || id <= 0) {
    return of(empty('invalid'));
  }

  return forkJoin({
    quote: svc.getById(id),
    // Metadata is best-effort enrichment — never let it fail the detail.
    meta: svc.getMetadataById(id).pipe(catchError(() => of(null))),
  }).pipe(
    map(({ quote, meta }): QuoteDetailVm => ({
      status: 'found',
      quote,
      user: meta?.user ?? null,
      tags: meta?.tags ?? [],
      categories: meta?.categories ?? [],
    })),
    catchError(() => of(empty('notFound'))),
  );
};
