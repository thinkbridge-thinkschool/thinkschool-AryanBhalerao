import { Component, inject, signal, computed, effect } from '@angular/core';
import { DatePipe } from '@angular/common';
import { QuotesListComponent } from './quotes-list/quotes-list.component';
import { CreateQuoteFormComponent } from './create-quote-form/create-quote-form.component';
import { QuotesService } from './services/quotes.service';
import { QuoteReadModel } from './models/quote.model';

type Tab = 'explorer' | 'search' | 'create';

@Component({
  selector: 'app-root',
  imports: [QuotesListComponent, CreateQuoteFormComponent, DatePipe],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly svc = inject(QuotesService);

  readonly activeTab = signal<Tab>('explorer');

  readonly selectedId = signal<number | null>(null);
  readonly detail = signal<QuoteReadModel | null>(null);
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
      },
      { allowSignalWrites: true }
    );
  }

  setTab(tab: Tab) {
    this.activeTab.set(tab);
  }

  lookupId(raw: string) {
    const n = parseInt(raw, 10);
    this.selectedId.set(isNaN(n) ? null : n);
  }
}
