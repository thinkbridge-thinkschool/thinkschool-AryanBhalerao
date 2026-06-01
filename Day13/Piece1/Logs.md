# Verification Log

### 1. Loading state (initial render)
When the app loads, `status` signal is `'loading'` and `page = 1`, `size = 10`.
The data-loading `effect()` fires immediately because `page` and `size` are read inside it.
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

### 5. Computed updating when signal changes
Changing page size via the `<select>` dropdown calls `setSize(25)`:
- `size.set(25)` fires
- `page.set(1)` fires (reset)
- Effect re-runs because `page` changed (or `size` changed, whichever tracks first)
- `pageLabel` updates from `"Page 1 · 10 of 10 requested"` → `"Page 1 · 25 of 25 requested"`
Both computed re-evaluations happened without any explicit subscribe or change detection call.

### 6. `@switch` on `view` signal
Clicking "Metadata" tab calls `switchView('metadata')`:
- `view.set('metadata')` 
- `@switch (view())` transitions from `@case ('list')` to `@case ('metadata')`
- `GET /api/quotes/with-metadata?page=1&size=10` returns `{ quoteId, quote, user, tags, categories }[]`
- `@for (m of metadata(); track m.quoteId)` renders cards with tag/category pills

### 7. Detail panel — `GET /api/quotes/{id}`
Effect in `App` root watches `selectedId`.
- Typing `1` → `selectedId.set(1)` → effect fires → `detailStatus = 'loading'`
- Response arrives: `detailStatus = 'found'`, `@case ('found')` shows the blockquote
- Typing `99999` → 404 → `detailStatus = 'notFound'` → `@case ('notFound')` shows "404 — no quote with that ID."

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

Angular 17+ parses `@if`, `@for`, `@switch` as control flow keywords everywhere in the template — even in static display text. The compiler saw `@if` in the subtitle and opened a block it could never close, which then made every subsequent `@switch` in the real control flow look like an "Unclosed block". The fix:

```html
<p class="subtitle">signal · computed · effect · &#64;if · &#64;for · &#64;switch · inject()</p>
```

Similarly, `{id}` in template text triggers ICU message parsing (`{` opens an ICU expression). Fixed with `&#123;id&#125;`.
