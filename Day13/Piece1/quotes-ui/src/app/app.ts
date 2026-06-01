import { Component, inject, signal, computed, effect } from '@angular/core';
import { QuotesListComponent } from './quotes-list/quotes-list.component';
import { QuotesService } from './services/quotes.service';

@Component({
  selector: 'app-root',
  imports: [QuotesListComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  // inject() at field level — no constructor needed
  private readonly svc = inject(QuotesService);

  readonly selectedId = signal<number | null>(null);
  readonly detail = signal<{ authorName: string; text: string; createdAt: string } | null>(null);
  readonly detailStatus = signal<'idle' | 'loading' | 'found' | 'notFound'>('idle');

  // computed: label shown in the detail panel header
  readonly detailHeader = computed(() =>
    this.selectedId() !== null ? `Quote #${this.selectedId()}` : 'Select a quote to preview'
  );

  constructor() {
    // effect: whenever selectedId changes, fetch the detail from /api/quotes/{id}
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
            this.detail.set(q);
            this.detailStatus.set('found');
          },
          error: () => {
            this.detail.set(null);
            this.detailStatus.set('notFound');
          },
        });
      },
      { allowSignalWrites: true }
    );
  }

  lookupId(raw: string) {
    const n = parseInt(raw, 10);
    this.selectedId.set(isNaN(n) ? null : n);
  }
}
