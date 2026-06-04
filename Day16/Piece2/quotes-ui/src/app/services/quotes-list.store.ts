import { computed, effect, inject, Injectable, signal, untracked } from '@angular/core';
import { QuoteMetadataReadModel } from '../models/quote.model';
import { QuotesService } from './quotes.service';

export type LoadStatus = 'loading' | 'loaded' | 'empty' | 'error';

@Injectable({ providedIn: 'root' })
export class QuotesListStore {
  private readonly svc = inject(QuotesService);

  // ── Private writable signals ──────────────────────────────────────────────
  private readonly _page = signal<number>(1);
  private readonly _size = signal<number>(10);
  private readonly _status = signal<LoadStatus>('loading');
  private readonly _quotes = signal<QuoteMetadataReadModel[]>([]);
  private readonly _totalQuotes = signal<number | null>(null);
  private readonly _totalAuthors = signal<number | null>(null);

  // ── Public readonly signals ───────────────────────────────────────────────
  readonly page = this._page.asReadonly();
  readonly size = this._size.asReadonly();
  readonly status = this._status.asReadonly();
  readonly quotes = this._quotes.asReadonly();
  readonly totalQuotes = this._totalQuotes.asReadonly();
  readonly totalAuthors = this._totalAuthors.asReadonly();

  // ── Computed signals ──────────────────────────────────────────────────────
  readonly isEmpty = computed(() => this._quotes().length === 0);
  readonly quoteCount = computed(() => this._quotes().length);
  readonly authorCount = computed(
    () => new Set(this._quotes().map((q) => q.author)).size
  );
  readonly summary = computed(
    () => `${this.quoteCount()} quotes · ${this.authorCount()} authors on this page`
  );
  readonly totalSummary = computed(() => {
    const q = this._totalQuotes();
    const a = this._totalAuthors();
    return q === null || a === null
      ? '… total'
      : `${q} quotes · ${a} authors total`;
  });

  constructor() {
    // Re-fetches whenever page or size changes.
    // allowSignalWrites is required because _status is written synchronously
    // inside the effect body (before entering untracked).
    effect(
      () => {
        const p = this._page();
        const s = this._size();

        this._status.set('loading');

        // untracked prevents the subscribe callbacks from registering
        // _quotes / _status as tracked dependencies of this effect,
        // which would otherwise cause an infinite re-fetch loop.
        untracked(() => {
          this.svc.getWithMetadata(p, s).subscribe({
            next: (data) => {
              this._quotes.set(data);
              this._status.set(data.length === 0 ? 'empty' : 'loaded');
            },
            error: () => this._status.set('error'),
          });
        });
      },
      { allowSignalWrites: true }
    );

    // Collection-wide totals — fetched once, independent of paging.
    this.svc.getAuthorsWithQuotes().subscribe({
      next: (authors) => {
        this._totalAuthors.set(authors.length);
        this._totalQuotes.set(authors.reduce((sum, a) => sum + a.quoteCount, 0));
      },
      error: () => {},
    });
  }

  // ── Public actions ────────────────────────────────────────────────────────
  prevPage(): void {
    if (this._page() > 1) this._page.update((p) => p - 1);
  }

  nextPage(): void {
    this._page.update((p) => p + 1);
  }

  setPage(value: number): void {
    if (Number.isFinite(value) && value >= 1) this._page.set(Math.floor(value));
  }

  // MaxPageSize on the API is 100 (QuoteEndpoints.cs); values outside [1, 100]
  // produce a 400 that the error handler maps to status:'error', which the
  // template then misreports as "Could not reach the API."
  setSize(value: number): void {
    if (!Number.isFinite(value) || value < 1 || value > 100) return;
    this._size.set(value);
    this._page.set(1);
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-AU', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }
}
