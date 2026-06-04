import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { QuotesListStore } from '../services/quotes-list.store';

@Component({
  selector: 'app-quotes-list',
  imports: [RouterLink],
  templateUrl: './quotes-list.component.html',
  styleUrl: './quotes-list.component.css',
})
export class QuotesListComponent {
  private readonly router = inject(Router);
  readonly store = inject(QuotesListStore);

  // Navigation belongs in the component because it depends on Router.
  goToQuote(raw: string): void {
    const trimmed = raw.trim();
    if (trimmed) this.router.navigate(['/quotes', trimmed]);
  }
}
