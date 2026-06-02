# Verification Log

### 1. Loading state (initial render)
When the app loads, `status` signal is `'loading'` and `page = 1`, `size = 10`.
The data-loading `effect()` fires immediately because `page`, `size`, and `view` are all read inside it.
Template shows "Loading…" via `@if (status() === 'loading')`.

### 2. Loaded state — `GET /api/quotes?page=1&size=10`
Response: `[ { id, authorName, text, createdAt }, … ]`
- `quotes.set(data)` fires → `isEmpty` computed updates to `false`
- `pageLabel` computed updates: `"Page 1 · 10 of 10 requested"`
- `firstAuthor` computed updates to the authorName of `quotes()[0]`
- The logging effect fires: `[QuotesList] state: Page 1 · 10 of 10 requested | status: loaded`
- `@for (quote of quotes(); track quote.id)` renders 10 `<li>` cards

### 3. Empty list state — page beyond last record
Clicking "Next →" increments `page` signal past the last page.
Effect re-fires, `GET /api/quotes?page=N&size=10` returns `[]`.
- `quotes.set([])` → `isEmpty` becomes `true`
- `status.set('empty')` → `@if (isEmpty())` renders "No quotes found on this page."
- "Next →" button has `[disabled]="isEmpty()"` — it disables itself, preventing an infinite loop of empty fetches
- `firstAuthor` computes to `'—'` (the `??` fallback)

### 4. Error state — API unreachable
With the API stopped, any fetch returns a network error.
- Effect's `error` callback: `status.set('error')`
- Template shows: "Could not reach the API at localhost:5051. Is the QuotesApi running?"

### 5. List → metadata view switch (Bug 1 fixed)
Clicking "Metadata" calls `switchView('metadata')`:
- `view.set('metadata')` fires — `view` is now a **tracked** dependency of the effect
- Effect re-runs automatically: `GET /api/quotes/with-metadata?page=1&size=10` fires
- Stale list data is no longer shown — the fix reads `v = this.view()` before `untracked()`
- `switchView()` now only clears and updates signals; no duplicate fetch logic

### 6. Next page in metadata view (Bug 4 fixed)
Switching to Metadata view and clicking "Next →":
- `isEmpty` computed now checks `this.metadata().length === 0` when in metadata view
- "Next →" button enables correctly when metadata results are present
- Previously `isEmpty` always read `this.quotes().length` — was always `0` in metadata view

### 7. Detail panel — `GET /api/quotes/{id}` (Bug 3 fixed)
Effect in `App` root watches `selectedId`.
- Typing `1` → `selectedId.set(1)` → effect fires → `detailStatus = 'loading'`
- Response arrives: guard checks `this.selectedId() === id` before writing
- `detailStatus = 'found'`, `@case ('found')` shows the blockquote
- Rapid typing `1` → `12` → `123`: earlier responses are discarded by the stale-response guard
- Only the last committed response renders — no stale overwrite

### 8. Detail 404
Typing `99999` → 404 → stale-response guard passes (id still matches) → `detailStatus = 'notFound'` → "404 — no quote with that ID."

### 9. detail signal type (Bug 2 fixed)
`detail` is typed as `signal<QuoteReadModel | null>(null)` — `id` is present on the type.
Accessing `detail()!.id` no longer requires a cast.

# Bugs Caught and Fixed

## Bug Caught: `@` in plain template text parsed as control flow

**What the agent initially produced:**

```html
<p class="subtitle">signal · computed · effect · @if · @for · @switch · inject()</p>
```

**Build error:**

```
NG5002: Incomplete block "if". If you meant to write the @ character,
you should use the "&#64;" HTML entity instead.
```

Angular 17+ parses `@if`, `@for`, `@switch` as control flow keywords everywhere in the template — even in static display text. The fix:

```html
<p class="subtitle">signal · computed · effect · &#64;if · &#64;for · &#64;switch · inject()</p>
```

Similarly, `{id}` in template text triggers ICU message parsing. Fixed with `&#123;id&#125;`.
