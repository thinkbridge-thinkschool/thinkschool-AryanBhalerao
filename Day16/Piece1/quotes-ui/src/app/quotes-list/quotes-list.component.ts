import {
  Component,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { QuoteMetadataReadModel } from '../models/quote.model';
import { QuotesService } from '../services/quotes.service';

type LoadStatus = 'loading' | 'loaded' | 'empty' | 'error';

@Component({
  selector: 'app-quotes-list',
  imports: [RouterLink],
  templateUrl: './quotes-list.component.html',
  styleUrl: './quotes-list.component.css',
  // standalone: true is the default in Angular 17+; no NgModule needed
})
export class QuotesListComponent {
  private readonly svc = inject(QuotesService);
  private readonly router = inject(Router);

  // ── Writable signals ──────────────────────────────────────────────────────
  readonly page = signal(1);
  readonly size = signal(10);
  readonly status = signal<LoadStatus>('loading');
  readonly quotes = signal<QuoteMetadataReadModel[]>([]);

  // Collection-wide totals (across all pages), derived once from
  // GET /api/authors/with-quotes. null until that request resolves.
  readonly totalQuotes = signal<number | null>(null);
  readonly totalAuthors = signal<number | null>(null);

  // ── Computed signals ──────────────────────────────────────────────────────
  readonly isEmpty = computed(() => this.quotes().length === 0);
  readonly quoteCount = computed(() => this.quotes().length);
  readonly authorCount = computed(
    () => new Set(this.quotes().map((q) => q.author)).size
  );
  // Per-page summary.
  readonly summary = computed(
    () => `${this.quoteCount()} quotes · ${this.authorCount()} authors on this page`
  );
  // Collection-wide summary — shows a placeholder until totals load.
  readonly totalSummary = computed(() => {
    const q = this.totalQuotes();
    const a = this.totalAuthors();
    return q === null || a === null
      ? '… total'
      : `${q} quotes · ${a} authors total`;
  });

  constructor() {
    // Effect 1: data-loading.
    // Reads page AND size — both are tracked dependencies so any change
    // re-fetches automatically. allowSignalWrites is required because we
    // write status synchronously inside the effect body.
    effect(
      () => {
        const p = this.page();
        const s = this.size();

        this.status.set('loading');

        // untracked so the subscribe callbacks don't accidentally register
        // quotes/status as dependencies of this effect.
        untracked(() => {
          this.svc.getWithMetadata(p, s).subscribe({
            next: (data) => {
              this.quotes.set(data);
              this.status.set(data.length === 0 ? 'empty' : 'loaded');
            },
            error: () => this.status.set('error'),
          });
        });
      },
      { allowSignalWrites: true }
    );

    // Effect 2: pure side-effect — logs the computed summary whenever it changes.
    // No signal writes; demonstrates a read-only effect.
    effect(() => {
      console.log('[QuotesList] state:', this.summary(), '| status:', this.status());
    });

    // Collection-wide totals are independent of paging, so fetch them once.
    this.svc.getAuthorsWithQuotes().subscribe({
      next: (authors) => {
        this.totalAuthors.set(authors.length);
        this.totalQuotes.set(authors.reduce((sum, a) => sum + a.quoteCount, 0));
      },
      // Leave totals as null on failure — the template shows a neutral placeholder.
      error: () => {},
    });
  }

  // ── Actions ───────────────────────────────────────────────────────────────
  // Search-by-id: navigate to the detail route rather than fetching inline, so
  // the :id resolver, 404/invalid handling, and the View Transition all apply.
  // A non-numeric entry still routes through (the resolver shows 'invalid').
  goToQuote(raw: string) {
    const trimmed = raw.trim();
    if (trimmed) this.router.navigate(['/quotes', trimmed]);
  }

  prevPage() {
    if (this.page() > 1) this.page.update((p) => p - 1);
  }

  nextPage() {
    this.page.update((p) => p + 1);
  }

  setPage(value: number) {
    if (Number.isFinite(value) && value >= 1) this.page.set(Math.floor(value));
  }

  setSize(value: number) {
    this.size.set(value);
    this.page.set(1); // reset to page 1 when page size changes
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-AU', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }
}
