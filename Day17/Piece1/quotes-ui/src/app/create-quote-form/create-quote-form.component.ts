import { Component, ElementRef, computed, inject, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { catchError, map, of, switchMap } from 'rxjs';
import { QuotesService } from '../services/quotes.service';

type SubmitStatus = 'idle' | 'submitting' | 'success' | 'error';

@Component({
  selector: 'app-create-quote-form',
  // FormsModule provides NgForm on <form>, which intercepts native submit
  // and emits (ngSubmit) — without it the page reloads and submit() never runs.
  imports: [FormsModule],
  templateUrl: './create-quote-form.component.html',
  styleUrl: './create-quote-form.component.css',
})
export class CreateQuoteFormComponent {
  private readonly svc = inject(QuotesService);

  // Emitted when the API rejects the stored token (401) — the access token
  // only lives 15 minutes, so the parent must send the user back to login.
  readonly sessionExpired = output<void>();

  // Signal-backed field values
  readonly authorValue = signal('');
  readonly textValue = signal('');

  // Touch / dirty state per field — no FormGroup to own this
  readonly authorTouched = signal(false);
  readonly textTouched = signal(false);
  readonly authorDirty = signal(false);
  readonly textDirty = signal(false);

  // Computed errors — inline logic; Validators.required from @angular/forms expects
  // an AbstractControl and can't be used here (see bug note in ReadMe)
  readonly authorErrors = computed(() => {
    const v = this.authorValue();
    const errs: Record<string, true> = {};
    if (!v.trim()) errs['required'] = true;
    if (v.length > 100) errs['maxlength'] = true;
    return Object.keys(errs).length ? errs : null;
  });

  readonly textErrors = computed(() => {
    const v = this.textValue();
    const errs: Record<string, true> = {};
    if (!v.trim()) errs['required'] = true;
    if (v.length > 1000) errs['maxlength'] = true;
    return Object.keys(errs).length ? errs : null;
  });

  readonly formValid = computed(
    () => this.authorErrors() === null && this.textErrors() === null,
  );

  // Optional metadata — plain signals, not form fields; POST /api/quotes only
  // takes { author, text }. Tags/categories go to POST /api/quotes/{id}/metadata.
  readonly availableTags = [
    'wisdom', 'motivation', 'humor', 'philosophy', 'perseverance',
    'success', 'life', 'education', 'truth', 'change',
  ];
  readonly availableCategories = ['classic', 'modern'];
  readonly selectedTag = signal<string | null>(null);
  readonly selectedCategory = signal<string | null>(null);

  // Submit state
  readonly submitStatus = signal<SubmitStatus>('idle');
  readonly newQuoteId = signal<number | null>(null);
  readonly serverError = signal<string | null>(null);
  readonly metadataError = signal<string | null>(null);

  // DOM refs for focus-on-error (viewChild is already signal-based in Angular 17+)
  readonly authorInput = viewChild<ElementRef<HTMLInputElement>>('authorInput');
  readonly textInput = viewChild<ElementRef<HTMLTextAreaElement>>('textInput');

  setAuthor(v: string) {
    this.authorValue.set(v);
    this.authorDirty.set(true);
  }

  touchAuthor() {
    this.authorTouched.set(true);
  }

  setText(v: string) {
    this.textValue.set(v);
    this.textDirty.set(true);
  }

  touchText() {
    this.textTouched.set(true);
  }

  selectTag(tag: string) { this.selectedTag.set(tag); }
  selectCategory(cat: string) { this.selectedCategory.set(cat); }

  submit() {
    // Signal Forms has no markAllAsTouched() — set each field's touched signal manually
    this.authorTouched.set(true);
    this.textTouched.set(true);

    if (!this.formValid()) {
      if (this.authorErrors()) this.authorInput()?.nativeElement.focus();
      else if (this.textErrors()) this.textInput()?.nativeElement.focus();
      return;
    }

    this.submitStatus.set('submitting');
    this.serverError.set(null);
    this.metadataError.set(null);
    this.newQuoteId.set(null);

    const parsedTags = this.selectedTag() ? [this.selectedTag()!] : [];
    const parsedCategories = this.selectedCategory() ? [this.selectedCategory()!] : [];
    const hasMetadata = parsedTags.length > 0 || parsedCategories.length > 0;

    this.svc
      .create(this.authorValue(), this.textValue())
      .pipe(
        switchMap((res) => {
          if (!hasMetadata) return of({ id: res.id, metaOk: true });
          return this.svc.assignMetadata(res.id, parsedTags, parsedCategories).pipe(
            map(() => ({ id: res.id, metaOk: true })),
            catchError(() => of({ id: res.id, metaOk: false })),
          );
        }),
      )
      .subscribe({
        next: ({ id, metaOk }) => {
          this.newQuoteId.set(id);
          this.submitStatus.set('success');
          if (!metaOk) {
            this.metadataError.set(
              'Quote created but metadata could not be saved — you can retry by re-submitting with the same ID.',
            );
          }
          this.authorValue.set('');
          this.textValue.set('');
          this.authorTouched.set(false);
          this.textTouched.set(false);
          this.authorDirty.set(false);
          this.textDirty.set(false);
          this.selectedTag.set(null);
          this.selectedCategory.set(null);
        },
        error: (err) => {
          if (err?.status === 401) {
            // Token expired or invalid — drop it and bounce back to the login form.
            this.svc.logout();
            this.submitStatus.set('idle');
            this.sessionExpired.emit();
            return;
          }
          const detail = err?.error?.errors
            ? Object.values(err.error.errors as Record<string, string[]>)
                .flat()
                .join(' ')
            : (err?.error?.title ?? 'Failed to create quote. Please try again.');
          this.serverError.set(detail);
          this.submitStatus.set('error');
        },
      });
  }
}
