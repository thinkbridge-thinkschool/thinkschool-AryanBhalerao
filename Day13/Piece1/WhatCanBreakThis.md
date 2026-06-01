# What Would Break if the API Contract Changed

- **`authorName` → `author`** — `{{ quote.authorName }}` renders blank. TypeScript sees `string`, template sees `undefined`. No compile error because `QuoteReadModel.authorName` would still exist in the interface, just never populated at runtime. *Silent data loss.*

- **`quoteId` → `id` on metadata** — `track m.quoteId` tracks `undefined` for every item. Angular uses `undefined` as the same key for all rows, causing the `@for` to destroy and recreate every DOM node on each re-render instead of patching. *Performance regression + possible flicker.*

- **`createdAt` removed** — `new Date(undefined)` → `"Invalid Date"` rendered in the `<time>` element. No crash. *Garbled display.*

- **Pagination query string changes (`?page=1&size=10` → `?offset=0&limit=10`)** — API returns 400 Bad Request; effect's error callback fires; status stays `'error'`; all pages show the error banner. *Complete feature loss.*

- **`GET /api/quotes/with-metadata` removed** — `switchView('metadata')` triggers a 404; `status` stays `'error'`; metadata panel shows error message. *Metadata tab broken.*
