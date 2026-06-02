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
  @ViewChild('textInput') private textInput!: ElementRef<HTMLTextAreaElement>;

  readonly form = this.fb.group({
    author: ['', [Validators.required, Validators.maxLength(100)]],
    text: ['', [Validators.required, Validators.maxLength(1000)]],
  });

  readonly submitting = signal(false);
  readonly serverError = signal<string | null>(null);
  readonly successId = signal<number | null>(null);

  get author() { return this.form.controls.author; }
  get text() { return this.form.controls.text; }

  submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      // Move focus to the first invalid field so keyboard/SR users know where the errors are.
      if (this.author.invalid) {
        this.authorInput.nativeElement.focus();
      } else {
        this.textInput.nativeElement.focus();
      }
      return;
    }

    this.submitting.set(true);
    this.serverError.set(null);
    this.successId.set(null);

    const { author, text } = this.form.getRawValue();
    this.svc.createQuote(author!, text!).subscribe({
      next: (res) => {
        this.successId.set(res.id);
        this.form.reset();
        this.submitting.set(false);
      },
      error: (err) => {
        const title = err.error?.title ?? 'Server error — could not create quote.';
        this.serverError.set(title);
        this.submitting.set(false);
      },
    });
  }
}
