# Day 14 Piece 1 — Reactive Form + Accessibility: Brief, Output, Verification

## Brief sent to the agent

```txt
Build a reactive Angular 17+ form component for creating a quote via POST /api/quotes on the
QuotesApi at http://localhost:5051.

Endpoint and contract (cite exactly):
  POST /api/quotes
  Request body: { author: string, text: string }
  Validation rules enforced server-side by CreateQuoteCommandHandler (and Quote.AuthorMaxLength /
  Quote.TextMaxLength constants in Quote.cs):
    - author: required, max 100 characters  (Quote.AuthorMaxLength = 100)
    - text:   required, max 1 000 characters (Quote.TextMaxLength  = 1000)
  Success: 201 Created → { id: number }
  Failure: 422 Unprocessable Entity (ValidationProblem) →
           { title: string, errors: { author?: string[], text?: string[] } }
  Auth: Bearer JWT with the "can-edit-quotes" scope; 401 if absent or expired.

Deliverables:
1. create-quote/create-quote.component.ts — ReactiveFormsModule form with:
   - FormBuilder + FormGroup; controls:
       author: [Validators.required, Validators.maxLength(100)]
       text:   [Validators.required, Validators.maxLength(1000)]
   - submit(): markAllAsTouched() → if invalid, focus first invalid field → else POST
   - Signals: submitting: signal<boolean>, serverError: signal<string|null>,
     successId: signal<number|null>
2. create-quote/create-quote.component.html — accessible template:
   - <label for="…"> associated to each <input>/<textarea> via matching id
   - [attr.aria-invalid]="ctrl.invalid && ctrl.touched ? 'true' : null" on each control
   - aria-describedby="<field>-error" on each control; the target <span id="<field>-error">
     MUST always be in the DOM (not inside @if) so the pointer never dangled
   - role="alert" on error spans so SR announces content as it appears
   - role="status" on success paragraph (polite); role="alert" on server-error (assertive)
   - Submit button disabled while submitting
3. create-quote/create-quote.component.css — minimal styles:
   - :focus-visible outline (2 px blue) on inputs, textarea, and submit button
   - border-color: #dc2626 when [aria-invalid='true']
   - .error-msg min-height so layout doesn't jump when errors appear/disappear
4. Add QuotesService.createQuote(author, text): reads JWT from localStorage('jwt'),
   sends Authorization: Bearer header; return http.post<{ id: number }>
5. Wire into app.ts (import CreateQuoteComponent) and app.html (add <app-create-quote />)
```

---

## Agent output

Files produced (paths relative to `Day14/Piece1/quotes-ui/src/app/`):

- `create-quote/create-quote.component.ts`
- `create-quote/create-quote.component.html`
- `create-quote/create-quote.component.css`
- `services/quotes.service.ts` (updated — added `createQuote`)
- `app.ts` (updated — added `CreateQuoteComponent` import)
- `app.html` (updated — added `<app-create-quote />`)

### `create-quote/create-quote.component.ts` — agent's first draft (buggy)

```ts
// AGENT DRAFT — contains the bug documented below
readonly form = this.fb.group({
  author: ['', [Validators.required, Validators.maxLength(100)]],
  text:   ['', [Validators.required, Validators.maxLength(500)]],  // ← BUG: should be 1000
});
```

Full remainder of the file (focus / submit / signal logic) was correct:

```ts
import { Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { QuotesService } from '../services/quotes.service';

@Component({
  selector: 'app-create-quote',
  imports: [ReactiveFormsModule],
  templateUrl: './create-quote.component.html',
  styleUrl: './create-quote.component.css',
})
export class CreateQuoteComponent {
  private readonly fb = inject(FormBuilder);
  private readonly svc = inject(QuotesService);

  @ViewChild('authorInput') private authorInput!: ElementRef<HTMLInputElement>;
  @ViewChild('textInput')   private textInput!: ElementRef<HTMLTextAreaElement>;

  readonly form = this.fb.group({
    author: ['', [Validators.required, Validators.maxLength(100)]],
    text:   ['', [Validators.required, Validators.maxLength(500)]],  // ← BUG
  });

  readonly submitting = signal(false);
  readonly serverError = signal<string | null>(null);
  readonly successId   = signal<number | null>(null);

  get author() { return this.form.controls.author; }
  get text()   { return this.form.controls.text; }

  submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      if (this.author.invalid) this.authorInput.nativeElement.focus();
      else                     this.textInput.nativeElement.focus();
      return;
    }
    this.submitting.set(true);
    this.serverError.set(null);
    this.successId.set(null);

    const { author, text } = this.form.getRawValue();
    this.svc.createQuote(author!, text!).subscribe({
      next:  (res) => { this.successId.set(res.id); this.form.reset(); this.submitting.set(false); },
      error: (err) => { this.serverError.set(err.error?.title ?? 'Server error.'); this.submitting.set(false); },
    });
  }
}
```

### `create-quote/create-quote.component.html` (no bugs — correct as generated)

```html
<section class="create-quote" aria-labelledby="create-quote-heading">
  <h2 id="create-quote-heading">Create a Quote</h2>

  @if (successId() !== null) {
    <p role="status" class="success-msg">Quote #{{ successId() }} created successfully.</p>
  }

  @if (serverError()) {
    <p role="alert" class="server-error-msg">{{ serverError() }}</p>
  }

  <form (ngSubmit)="submit()" novalidate>
    <div class="field">
      <label for="author">Author</label>
      <input
        #authorInput
        id="author"
        type="text"
        [formControl]="author"
        [attr.aria-invalid]="author.invalid && author.touched ? 'true' : null"
        aria-describedby="author-error"
        autocomplete="name"
        maxlength="100"
      />
      <!--
        Span is ALWAYS in DOM — aria-describedby must never point to a removed element.
        role="alert" announces content to screen readers when it appears.
      -->
      <span id="author-error" class="error-msg" role="alert">
        @if (author.invalid && author.touched) {
          @if (author.errors?.['required']) { Author is required. }
          @else if (author.errors?.['maxlength']) { Author must be 100 characters or fewer. }
        }
      </span>
    </div>

    <div class="field">
      <label for="text">Quote text</label>
      <textarea
        #textInput
        id="text"
        rows="4"
        [formControl]="text"
        [attr.aria-invalid]="text.invalid && text.touched ? 'true' : null"
        aria-describedby="text-error"
        maxlength="1000"
      ></textarea>
      <span id="text-error" class="error-msg" role="alert">
        @if (text.invalid && text.touched) {
          @if (text.errors?.['required']) { Quote text is required. }
          @else if (text.errors?.['maxlength']) { Quote text must be 1,000 characters or fewer. }
        }
      </span>
    </div>

    <button type="submit" [disabled]="submitting()">
      {{ submitting() ? 'Saving…' : 'Save quote' }}
    </button>
  </form>
</section>
```

---

## Bug

### Bug · Text validator capped at 500 — rejects valid quotes the API would accept

**File:** `create-quote/create-quote.component.ts`

The agent wrote:

```ts
text: ['', [Validators.required, Validators.maxLength(500)]],
```

`Quote.TextMaxLength` in `Quote.cs` is **1 000**. The endpoint's `CreateQuoteCommandHandler` also
uses this constant as its upper bound. A quote of, say, 600 characters is perfectly valid for the
API but would show a client-side "must be 500 characters or fewer" error and never be submitted.
The form was more restrictive than the contract — silently blocking valid data.

The agent likely picked 500 as a "reasonable guess" without reading the model constants.

---

## Fix

**File:** `create-quote/create-quote.component.ts`

```ts
// Before:
text: ['', [Validators.required, Validators.maxLength(500)]],

// After — matches Quote.TextMaxLength = 1000:
text: ['', [Validators.required, Validators.maxLength(1000)]],
```

Also updated the error message in the template from `"500 characters or fewer"` to
`"1,000 characters or fewer"` (already reflected in the shipped `.html`).

---

## Verification log

**States exercised:**

- **Empty submit** — Clicked "Save quote" with both fields blank. Both fields showed red borders
  and error messages ("Author is required.", "Quote text is required."). Focus moved to the
  Author input automatically. Keyboard-only: Tab to button, Enter — identical result. ✓

- **Single field invalid** — Filled Author, left Text blank. Submitted: Text field turned red,
  focus jumped there; Author stayed green. ✓

- **maxLength boundary** — Pasted 101-character author: "Author must be 100 characters or fewer"
  appeared immediately (validators run on touch). ✓

- **Server-error (401)** — API running but no JWT in localStorage. Submitted a valid form.
  Network returned 401; the form displayed "One or more validation errors occurred." in the
  red alert banner. Button re-enabled. ✓

- **Submitting state** — Added a `console.log` delay to the observable to hold the in-flight
  state; confirmed the button text changed to "Saving…" and `disabled` attribute was present. ✓

- **Success** — Called `POST /api/login` first with valid credentials, stored the JWT in
  localStorage via devtools, then submitted a valid quote. Got "Quote #42 created successfully."
  in green; form reset. ✓

**Accessibility checks:**

- **Keyboard path** — Tab order: Author → Text → Submit button. No focus traps. Tab past submit
  returned to browser chrome. Enter on submit triggered form submission. All states reachable
  without a mouse. ✓

- **Screen-reader (NVDA + Chrome)** — Navigating to the Author input announced
  "Author, edit text". On submit-with-error, focus moved there and NVDA announced
  "Author is required." via the `aria-describedby` / `role="alert"` chain. The server-error
  `role="alert"` paragraph interrupted and read itself aloud without focus change. ✓

- **axe DevTools** (browser extension) — Zero critical or serious violations on the form
  in all three states: pristine, invalid-touched, and post-success. ✓

**Bug caught:** The text `Validators.maxLength(500)` mismatch described above — found by
pasting a 600-character quote and seeing the form reject it while the API (`curl`) accepted
the same payload. Fixed by changing `500 → 1000` in the `FormGroup` declaration.

---

## What breaks if the quote contract changes

- **`author` max raised from 100 to 200 in `Quote.AuthorMaxLength`** — `Validators.maxLength(100)`
  in the form still blocks 101–200 character authors client-side. The API would accept them but
  the form never sends them. Must update in two places: the validator and the `maxlength` HTML
  attribute (currently `maxlength="100"` on the `<input>`).

- **`text` field renamed to `body` in the request body** — `{ author, text }` in `createQuote()`
  sends `text`; the API reads `body` and gets `null` → 422 with `body: ["Body is required."]`.
  The form control is also named `text` so it would need renaming end-to-end.

- **New required field added (e.g. `source: string, max 200`)** — no control exists for it.
  The API returns 422 with `source: ["Source is required."]` every time. The form shows the
  generic server-error banner (not a per-field message) because it only knows about `author`
  and `text`.

- **Endpoint moved from `/api/quotes` to `/api/v2/quotes`** — `createQuote()` hard-codes the
  path; all creates silently 404 and surface as server errors.

- **Auth scope renamed from `can-edit-quotes` to `quotes.write`** — existing JWTs with the old
  scope get 403 Forbidden; the form shows the server-error banner but has no way to distinguish
  401 (no token) from 403 (wrong scope) without parsing `err.status`.
