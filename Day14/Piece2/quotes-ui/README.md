# QuotesUi

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 21.2.11.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.

---

## Create-a-Quote Form — Agent Prompt, Output, and Verification

### (1) Brief — the spec given to the agent

> Build a reactive Angular 21 form component (`CreateQuoteFormComponent`) that posts to the real backend endpoint:
>
> **`POST http://localhost:5051/api/quotes`**
>
> **Request body (JSON):**
> | Field    | Type   | Required | Max length |
> |----------|--------|----------|-----------|
> | `author` | string | yes      | 100 chars  |
> | `text`   | string | yes      | 1000 chars |
>
> **Successful response:** `201 Created`, body `{ "id": number }`. The `id` is the auto-assigned database ID (SQL Server identity column); the form should display it after creation so the user can look the quote up via the detail panel.
>
> **Error response:** `400 Bad Request` with an RFC 7807 `ValidationProblem` body — structure is `{ title, errors: { fieldName: string[] } }`.
>
> The endpoint no longer requires a JWT bearer token (auth was relaxed for this piece so the UI exercise is self-contained).
>
> **Form requirements:**
> - Angular reactive form (`FormBuilder`, `FormGroup`) with two controls: `author` (required, maxLength 100) and `text` (required, maxLength 1000).
> - Client-side validators matching the API constraints exactly.
> - On submit with a client-side error: mark all controls as touched, move keyboard focus to the first invalid field.
> - Status signal: `idle → submitting → success | error`. Disable and mark `aria-busy` on the submit button while `submitting`.
> - Full a11y: every input has an associated `<label for>`. Error spans are always rendered (so `aria-describedby` IDs are never dangling); `aria-live="polite"` announces new error text. `aria-invalid="true"` set only when the control is invalid *and* touched. Server errors in a `role="alert"` paragraph. Success message in `role="status"`. `focus-visible` ring on the submit button.

---

### (2) Agent output — the form component and template

**`src/app/create-quote-form/create-quote-form.component.ts`**

```typescript
import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { QuotesService } from '../services/quotes.service';

type SubmitStatus = 'idle' | 'submitting' | 'success' | 'error';

@Component({
  selector: 'app-create-quote-form',
  imports: [ReactiveFormsModule],
  templateUrl: './create-quote-form.component.html',
  styleUrl: './create-quote-form.component.css',
})
export class CreateQuoteFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly svc = inject(QuotesService);

  readonly form = this.fb.group({
    author: ['', [Validators.required, Validators.maxLength(100)]],
    text: ['', [Validators.required, Validators.maxLength(1000)]],
  });

  readonly submitStatus = signal<SubmitStatus>('idle');
  readonly newQuoteId = signal<number | null>(null);
  readonly serverError = signal<string | null>(null);

  readonly authorInput = viewChild<ElementRef<HTMLInputElement>>('authorInput');
  readonly textInput = viewChild<ElementRef<HTMLTextAreaElement>>('textInput');

  get author() { return this.form.controls.author; }
  get text() { return this.form.controls.text; }

  submit() {
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      if (this.author.invalid) {
        this.authorInput()?.nativeElement.focus();
      } else {
        this.textInput()?.nativeElement.focus();
      }
      return;
    }

    this.submitStatus.set('submitting');
    this.serverError.set(null);
    this.newQuoteId.set(null);

    this.svc.create(this.author.value!, this.text.value!).subscribe({
      next: (res) => {
        this.newQuoteId.set(res.id);
        this.submitStatus.set('success');
        this.form.reset();
      },
      error: (err) => {
        const detail = err?.error?.errors
          ? Object.values(err.error.errors as Record<string, string[]>).flat().join(' ')
          : (err?.error?.title ?? 'Failed to create quote. Please try again.');
        this.serverError.set(detail);
        this.submitStatus.set('error');
      },
    });
  }
}
```

**`src/app/create-quote-form/create-quote-form.component.html`**

```html
<section class="create-form" aria-labelledby="create-form-title">
  <h2 id="create-form-title">Create a quote</h2>

  <form [formGroup]="form" (ngSubmit)="submit()" novalidate>

    <div class="field">
      <label for="cf-author">Author</label>
      <input
        id="cf-author"
        #authorInput
        type="text"
        formControlName="author"
        autocomplete="off"
        [attr.aria-invalid]="author.invalid && author.touched ? 'true' : null"
        aria-describedby="cf-author-error"
      />
      <span id="cf-author-error" class="field-error" aria-live="polite">
        @if (author.invalid && author.touched) {
          @if (author.errors?.['required']) { Author is required. }
          @else if (author.errors?.['maxlength']) { Author must be 100 characters or fewer. }
        }
      </span>
    </div>

    <div class="field">
      <label for="cf-text">Quote text</label>
      <textarea
        id="cf-text"
        #textInput
        formControlName="text"
        rows="4"
        [attr.aria-invalid]="text.invalid && text.touched ? 'true' : null"
        aria-describedby="cf-text-error"
      ></textarea>
      <span id="cf-text-error" class="field-error" aria-live="polite">
        @if (text.invalid && text.touched) {
          @if (text.errors?.['required']) { Quote text is required. }
          @else if (text.errors?.['maxlength']) { Quote text must be 1000 characters or fewer. }
        }
      </span>
    </div>

    @if (submitStatus() === 'error') {
      <p class="server-error" role="alert">{{ serverError() }}</p>
    }

    @if (submitStatus() === 'success') {
      <p class="success-msg" role="status">
        Quote #{{ newQuoteId() }} created — paste that ID into the lookup panel to preview it.
      </p>
    }

    <button
      type="submit"
      [disabled]="submitStatus() === 'submitting'"
      [attr.aria-busy]="submitStatus() === 'submitting' ? 'true' : null"
    >
      @if (submitStatus() === 'submitting') { Saving… } @else { Create quote }
    </button>

  </form>
</section>
```

---

### (3) Verification log

**States and edges exercised**

| State | How exercised | Observed |
|---|---|---|
| **Empty submit** | Clicked "Create quote" with both fields blank | Both error spans populated ("required"), focus moved to Author input, `aria-invalid="true"` set on both controls |
| **Author too long** | Pasted 101-character string into Author, tabbed away | "Author must be 100 characters or fewer" appeared immediately via `aria-live`; `aria-invalid` set |
| **Text too long** | Typed past 1000 chars in Quote text | Maxlength validator fired; error text announced |
| **Submitting** | Submitted a valid form while throttling network in DevTools | Button showed "Saving…", `aria-busy="true"`, button disabled |
| **Success** | Submitted author="Seneca" + a valid quote to the live API | `role="status"` paragraph appeared with the new quote ID; form reset; pasted ID into lookup panel and confirmed the quote was retrievable via `GET /api/quotes/{id}` |
| **Server error** | Stopped the API, submitted a valid form | `role="alert"` paragraph showed "Failed to create quote. Please try again." |
| **API validation error** | Sent a request that bypassed client validators | `errors` dictionary from `ValidationProblem` body was flattened and displayed inline |

**A11y checks**

- **Keyboard path:** Tab → Author input → Tab → Quote text → Tab → "Create quote" button → Enter. All controls reachable, submit fires, focus moves to Author on empty submit.
- **axe DevTools (browser extension):** Zero violations on the form in both idle and error states.
- **`aria-describedby` pairing:** Verified with browser accessibility tree — Author input correctly references `cf-author-error` span; the span is always in the DOM so the reference is never dangling.

**One concrete bug caught and fixed**

The agent initially set `aria-invalid` using a ternary that emitted `'false'` when the control was valid:

```html
[attr.aria-invalid]="author.invalid && author.touched ? 'true' : 'false'"
```

`aria-invalid="false"` is technically valid HTML but screen readers (NVDA, VoiceOver) announce it on every field, making the form noisy. The correct pattern is to omit the attribute entirely when the field is valid. Fixed by returning `null` when the condition is false, which causes Angular to remove the attribute from the DOM:

```html
[attr.aria-invalid]="author.invalid && author.touched ? 'true' : null"
```

**What breaks if the quote contract changes**

| Change | What breaks |
|---|---|
| `author` renamed to `authorName` in POST body | The service sends `{ author, text }` — field silently ignored, API gets `""`, returns validation error |
| `text` renamed to `content` | Same silent-mismatch failure; client validators fire on a field the API no longer reads |
| New required field added (e.g. `category`) | POST returns 400 `ValidationProblem`; the form surfaces the server error via the `errors` flattener, but there's no client-side control for the field, so the form can never succeed |
| `author` maxLength tightened to 50 | Client validator still allows up to 100 chars; submitting a 60-char author succeeds client-side, fails server-side with a `ValidationProblem` that the error handler displays |
