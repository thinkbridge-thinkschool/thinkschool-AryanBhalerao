import {
  Component,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { QuoteMetadataReadModel, QuoteReadModel } from '../models/quote.model';
import { QuotesService } from '../services/quotes.service';

type ViewMode = 'list' | 'metadata';
type LoadStatus = 'loading' | 'loaded' | 'empty' | 'error';

@Component({
  selector: 'app-quotes-list',
  templateUrl: './quotes-list.component.html',
  styleUrl: './quotes-list.component.css',
  // standalone: true is the default in Angular 17+; no NgModule needed
})
export class QuotesListComponent {
  private readonly svc = inject(QuotesService);

  // ── Writable signals ──────────────────────────────────────────────────────
  readonly page = signal(1);
  readonly size = signal(10);
  readonly view = signal<ViewMode>('list');
  readonly status = signal<LoadStatus>('loading');
  readonly quotes = signal<QuoteReadModel[]>([]);
  readonly metadata = signal<QuoteMetadataReadModel[]>([]);

  // ── Computed signals ──────────────────────────────────────────────────────
  // Checks the active collection so pagination works correctly in both views.
  readonly isEmpty = computed(() =>
    this.view() === 'list' ? this.quotes().length === 0 : this.metadata().length === 0
  );
  readonly pageLabel = computed(() => {
    const count = this.view() === 'list'
      ? this.quotes().length
      : this.metadata().length;
    return `Page ${this.page()} · ${count} of ${this.size()} requested`;
  });
  readonly firstAuthor = computed(() =>
    this.view() === 'list'
      ? (this.quotes()[0]?.authorName ?? '—')
      : (this.metadata()[0]?.user ?? '—')
  );

  constructor() {
    // Effect 1: data-loading.
    // Reads page, size, AND view — all three are tracked dependencies so any
    // change re-fetches automatically. allowSignalWrites is required because we
    // write status synchronously inside the effect body.
    effect(
      () => {
        const p = this.page();
        const s = this.size();
        const v = this.view(); // read HERE so view changes re-trigger this effect

        this.status.set('loading');

        // untracked so the subscribe callbacks don't accidentally register
        // quotes/status as dependencies of this effect.
        untracked(() => {
          if (v === 'list') {
            this.svc.getPage(p, s).subscribe({
              next: (data) => {
                this.quotes.set(data);
                this.status.set(data.length === 0 ? 'empty' : 'loaded');
              },
              error: () => this.status.set('error'),
            });
          } else {
            this.svc.getWithMetadata(p, s).subscribe({
              next: (data) => {
                this.metadata.set(data);
                this.status.set(data.length === 0 ? 'empty' : 'loaded');
              },
              error: () => this.status.set('error'),
            });
          }
        });
      },
      { allowSignalWrites: true }
    );

    // Effect 2: pure side-effect — logs the computed summary whenever it changes.
    // No signal writes; demonstrates a read-only effect.
    effect(() => {
      console.log('[QuotesList] state:', this.pageLabel(), '| status:', this.status());
    });
  }

  // ── Actions ───────────────────────────────────────────────────────────────
  prevPage() {
    if (this.page() > 1) this.page.update((p) => p - 1);
  }

  nextPage() {
    this.page.update((p) => p + 1);
  }

  setSize(value: number) {
    this.size.set(value);
    this.page.set(1); // reset to page 1 when page size changes
  }

  switchView(v: ViewMode) {
    // Updating view + page triggers the tracked effect above — no manual fetch needed.
    this.quotes.set([]);
    this.metadata.set([]);
    this.view.set(v);
    this.page.set(1);
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-AU', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }
}
