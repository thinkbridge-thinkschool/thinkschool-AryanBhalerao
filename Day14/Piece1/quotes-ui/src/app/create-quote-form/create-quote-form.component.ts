import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, map, of, switchMap } from 'rxjs';
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

  readonly availableTags = ['wisdom', 'motivation', 'humor', 'philosophy', 'perseverance', 'success', 'life', 'education', 'truth', 'change'];
  readonly availableCategories = ['classic', 'modern'];

  readonly selectedTag = signal<string | null>(null);
  readonly selectedCategory = signal<string | null>(null);

  readonly submitStatus = signal<SubmitStatus>('idle');
  readonly newQuoteId = signal<number | null>(null);
  readonly serverError = signal<string | null>(null);
  readonly metadataError = signal<string | null>(null);

  readonly authorInput = viewChild<ElementRef<HTMLInputElement>>('authorInput');
  readonly textInput = viewChild<ElementRef<HTMLTextAreaElement>>('textInput');

  get author() { return this.form.controls.author; }
  get text() { return this.form.controls.text; }

  selectTag(tag: string) { this.selectedTag.set(tag); }
  selectCategory(cat: string) { this.selectedCategory.set(cat); }

  submit() {
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      if (this.author.invalid) {
        this.authorInput()?.nativeElement.focus();
      } else if (this.text.invalid) {
        this.textInput()?.nativeElement.focus();
      }
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
      .create(this.author.value!, this.text.value!)
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
          this.form.reset();
          this.selectedTag.set(null);
          this.selectedCategory.set(null);
        },
        error: (err) => {
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
