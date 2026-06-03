import { Component, inject, signal, computed, effect } from '@angular/core';
import { DatePipe } from '@angular/common';
import { QuotesListComponent } from './quotes-list/quotes-list.component';
import { CreateQuoteFormComponent } from './create-quote-form/create-quote-form.component';
import { LoginFormComponent } from './login-form/login-form.component';
import { QuotesService } from './services/quotes.service';
import { QuoteMetadataReadModel, QuoteReadModel } from './models/quote.model';

type Tab = 'explorer' | 'search' | 'create';

@Component({
  selector: 'app-root',
  imports: [QuotesListComponent, CreateQuoteFormComponent, LoginFormComponent, DatePipe],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly svc = inject(QuotesService);

  readonly activeTab = signal<Tab>('explorer');
  readonly isLoggedIn = signal<boolean>(this.svc.hasValidToken());

  readonly selectedId = signal<number | null>(null);
  readonly detail = signal<QuoteReadModel | null>(null);
  // Tags/categories for the looked-up quote, fetched separately because the
  // by-id endpoint omits them. null = not yet loaded / not found in scanned pages.
  readonly detailMeta = signal<QuoteMetadataReadModel | null>(null);
  readonly detailStatus = signal<'idle' | 'loading' | 'found' | 'notFound'>('idle');

  readonly detailHeader = computed(() =>
    this.selectedId() !== null ? `Quote #${this.selectedId()}` : 'Look up a quote by ID'
  );

  constructor() {
    effect(
      () => {
        const id = this.selectedId();
        if (id === null) {
          this.detailStatus.set('idle');
          return;
        }
        this.detailStatus.set('loading');
        this.detail.set(null);
        this.detailMeta.set(null);

        this.svc.getById(id).subscribe({
          next: (q) => {
            if (this.selectedId() !== id) return;
            this.detail.set(q);
            this.detailStatus.set('found');
          },
          error: () => {
            if (this.selectedId() !== id) return;
            this.detail.set(null);
            this.detailStatus.set('notFound');
          },
        });

        // Best-effort enrichment with tags/categories; arrives independently of
        // the core lookup. Stays null (no pills) if the quote isn't in the
        // scanned metadata pages.
        this.svc.getMetadataById(id).subscribe({
          next: (meta) => {
            if (this.selectedId() === id) this.detailMeta.set(meta);
          },
          error: () => {},
        });
      },
      { allowSignalWrites: true }
    );
  }

  setTab(tab: Tab) {
    // Re-check on every visit to Create — the token may have expired since
    // login, and the form must not show without a valid session.
    if (tab === 'create') {
      this.isLoggedIn.set(this.svc.hasValidToken());
    }
    this.activeTab.set(tab);
  }

  onLoggedIn() {
    this.isLoggedIn.set(true);
  }

  onSessionExpired() {
    this.isLoggedIn.set(false);
  }

  logout() {
    this.svc.logout();
    this.isLoggedIn.set(false);
  }

  lookupId(raw: string) {
    const n = parseInt(raw, 10);
    this.selectedId.set(isNaN(n) ? null : n);
  }
}
