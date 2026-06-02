# Agent's Complete Verbatim Output

Angular 21 standalone app (`quotes-ui/`) scaffolded against the Day 13 QuotesApi.
No NgModules anywhere. All modern Angular patterns applied. All four bugs from Piece 1 fixed.

---

## File: `src/app/models/quote.model.ts`

```typescript
// Matches QuoteReadModel record: GET /api/quotes?page=&size=
// and GET /api/quotes/{id}
export interface QuoteReadModel {
  id: number;
  authorName: string;   // mapped from Author column in Dapper query
  text: string;
  createdAt: string;    // DateTimeOffset serialised as ISO-8601 string
}

// Matches QuoteMetadataReadModel record: GET /api/quotes/with-metadata?page=&size=
export interface QuoteMetadataReadModel {
  quoteId: number;
  quote: string;
  user: string;
  tags: string[];
  categories: string[];
}
```

---

## File: `src/app/services/quotes.service.ts`

```typescript
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
}
```

---

## File: `src/app/quotes-list/quotes-list.component.ts`

```typescript
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
  readonly pageLabel = computed(
    () => `Page ${this.page()} · ${this.quotes().length} of ${this.size()} requested`
  );
  readonly firstAuthor = computed(() => this.quotes()[0]?.authorName ?? '—');

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
```

---

## File: `src/app/quotes-list/quotes-list.component.html`

```html
<section class="quotes-shell">
  <header class="toolbar">
    <h2>Quotes</h2>

    <div class="view-toggle">
      <button
        [class.active]="view() === 'list'"
        (click)="switchView('list')"
      >
        List
      </button>
      <button
        [class.active]="view() === 'metadata'"
        (click)="switchView('metadata')"
      >
        Metadata
      </button>
    </div>

    <div class="page-size">
      <label for="size">Per page</label>
      <select id="size" (change)="setSize(+$any($event.target).value)">
        <option value="5">5</option>
        <option value="10" selected>10</option>
        <option value="25">25</option>
      </select>
    </div>
  </header>

  <!-- computed() drives the summary line -->
  <p class="summary">{{ pageLabel() }} · first author: {{ firstAuthor() }}</p>

  <!-- @switch on the view signal — demonstrates new control flow @switch -->
  @switch (view()) {
    @case ('list') {
      @if (status() === 'loading') {
        <p class="state-msg">Loading…</p>
      } @else if (status() === 'error') {
        <p class="state-msg error">
          Could not reach the API at <code>localhost:5051</code>. Is the
          QuotesApi running?
        </p>
      } @else if (isEmpty()) {
        <p class="state-msg">No quotes found on this page.</p>
      } @else {
        <!-- @for with track — track quote.id, not $index -->
        <ul class="quote-list">
          @for (quote of quotes(); track quote.id) {
            <li class="quote-card">
              <blockquote>{{ quote.text }}</blockquote>
              <footer>
                <strong>{{ quote.authorName }}</strong>
                <time>{{ formatDate(quote.createdAt) }}</time>
              </footer>
            </li>
          }
        </ul>
      }
    }

    @case ('metadata') {
      @if (status() === 'loading') {
        <p class="state-msg">Loading metadata…</p>
      } @else if (status() === 'error') {
        <p class="state-msg error">Failed to load metadata.</p>
      } @else if (metadata().length === 0) {
        <p class="state-msg">No metadata on this page.</p>
      } @else {
        <ul class="quote-list">
          @for (m of metadata(); track m.quoteId) {
            <li class="quote-card">
              <blockquote>{{ m.quote }}</blockquote>
              <footer>
                <strong>{{ m.user }}</strong>
                <span class="pills">
                  @for (tag of m.tags; track tag) {
                    <span class="pill tag">{{ tag }}</span>
                  }
                  @for (cat of m.categories; track cat) {
                    <span class="pill cat">{{ cat }}</span>
                  }
                </span>
              </footer>
            </li>
          }
        </ul>
      }
    }
  }

  <nav class="pagination">
    <button (click)="prevPage()" [disabled]="page() <= 1">← Prev</button>
    <span>Page {{ page() }}</span>
    <button (click)="nextPage()" [disabled]="isEmpty()">Next →</button>
  </nav>
</section>
```

---

## File: `src/app/app.ts`

```typescript
import { Component, inject, signal, computed, effect } from '@angular/core';
import { QuotesListComponent } from './quotes-list/quotes-list.component';
import { QuotesService } from './services/quotes.service';
import { QuoteReadModel } from './models/quote.model';

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
  readonly detail = signal<QuoteReadModel | null>(null);
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
            // Stale-response guard: discard if the user changed the ID while
            // this request was in flight.
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

  lookupId(raw: string) {
    const n = parseInt(raw, 10);
    this.selectedId.set(isNaN(n) ? null : n);
  }
}
```

---

## File: `src/app/app.html`

```html
<div class="app-shell">
  <header class="app-header">
    <h1>Quotes UI — Angular 21 Standalone</h1>
    <p class="subtitle">signal · computed · effect · &#64;if · &#64;for · &#64;switch · inject()</p>
  </header>

  <main class="app-body">
    <!-- Quotes list — all reactive state lives inside QuotesListComponent -->
    <app-quotes-list />

    <!-- Detail lookup panel — demonstrates @switch + effect in App root -->
    <aside class="detail-panel">
      <h3>{{ detailHeader() }}</h3>

      <input
        type="number"
        placeholder="Enter quote ID…"
        (input)="lookupId($any($event.target).value)"
        min="1"
      />

      <!-- @switch on detailStatus signal -->
      @switch (detailStatus()) {
        @case ('idle') {
          <p class="hint">Type an ID above to fetch a single quote via GET /api/quotes/&#123;id&#125;</p>
        }
        @case ('loading') {
          <p class="hint">Fetching…</p>
        }
        @case ('found') {
          <div class="detail-card">
            <blockquote>{{ detail()!.text }}</blockquote>
            <p><strong>{{ detail()!.authorName }}</strong></p>
          </div>
        }
        @case ('notFound') {
          <p class="hint error">404 — no quote with that ID.</p>
        }
      }
    </aside>
  </main>
</div>
```

---

## File: `src/app/app.config.ts`

```typescript
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(),
  ],
};
```

---

## Build result

```
✔ Building...
Initial chunk files | Names  | Raw size
main.js             | main   |  1.17 MB
styles.css          | styles | 211 bytes

Application bundle generation complete. [1.469 seconds]
```

Zero errors. Zero NgModules.

---

## Key design decisions

| Pattern | Where | Why |
|---------|-------|-----|
| `signal()` | `page`, `size`, `view`, `status`, `quotes`, `metadata`, `selectedId` | Single source of truth; mutated via `.set()` / `.update()` |
| `computed()` | `isEmpty`, `pageLabel`, `firstAuthor`, `detailHeader` | Derived state — auto-memoised, never stale |
| `view` read before `untracked()` | Data-loading effect | `const v = this.view()` before `untracked(...)` ensures view is a tracked dependency |
| `effect()` data-loading | `QuotesListComponent`, `App` | Re-fetches automatically when `page`, `size`, `view`, or `selectedId` changes |
| `effect()` logging | `QuotesListComponent` | Pure side-effect with no signal writes; shows the read-only effect use-case |
| `untracked()` | Inside data-loading effect | Prevents subscribe callbacks from becoming reactive dependencies of the effect |
| `{ allowSignalWrites: true }` | Both data-loading effects | Required for the synchronous `status.set('loading')` write inside the effect body |
| Stale-response guard | `App` getById callbacks | `if (this.selectedId() !== id) return` discards responses for IDs no longer selected |
| `isEmpty` view-aware | `QuotesListComponent` | Checks `metadata().length` in metadata view so "Next →" is not permanently disabled |
| `detail: signal<QuoteReadModel \| null>` | `App` | Uses the typed model instead of inline object; `id` field is present and accessible |
| `@switch` | `view()` in list, `detailStatus()` in panel | Multi-branch state display without nested `@if` chains |
| `@for … track quote.id` | Both list and metadata | Stable identity key from the API — `id` for quotes, `quoteId` for metadata |
| `inject()` | All services | No constructor parameters; works at field initialiser scope |
| `providedIn: 'root'` | `QuotesService` | Tree-shakeable singleton; no module needed |
