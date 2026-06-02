import { Component, inject, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { QuotesService } from '../services/quotes.service';

@Component({
  selector: 'app-login-form',
  imports: [ReactiveFormsModule],
  templateUrl: './login-form.component.html',
  styleUrl: './login-form.component.css',
})
export class LoginFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly svc = inject(QuotesService);

  readonly loggedIn = output<void>();

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  readonly status = signal<'idle' | 'submitting' | 'error'>('idle');
  readonly errorMsg = signal<string | null>(null);

  get email() { return this.form.controls.email; }
  get password() { return this.form.controls.password; }

  submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.status.set('submitting');
    this.errorMsg.set(null);

    this.svc.login(this.email.value!, this.password.value!).subscribe({
      next: (res) => {
        localStorage.setItem('jwt', res.accessToken);
        this.status.set('idle');
        this.loggedIn.emit();
      },
      error: () => {
        this.errorMsg.set('Login failed. Check your credentials and try again.');
        this.status.set('error');
      },
    });
  }
}
