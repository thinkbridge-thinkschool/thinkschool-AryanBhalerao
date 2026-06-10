import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { quoteResolver } from './quote-detail/quote.resolver';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'quotes' },
  {
    path: 'quotes',
    loadComponent: () =>
      import('./quotes-list/quotes-list.component').then((m) => m.QuotesListComponent),
  },
  {
    path: 'quotes/:id',
    // Resolve the quote BEFORE activation so the detail card is already in the
    // DOM when the router's View Transition captures the new snapshot — that is
    // what makes the list-card -> detail-card shared-element morph actually run.
    resolve: { vm: quoteResolver },
    loadComponent: () =>
      import('./quote-detail/quote-detail.component').then((m) => m.QuoteDetailComponent),
  },
  {
    path: 'login',
    loadComponent: () => import('./login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'create',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./create-quote-form/create-quote-form.component').then(
        (m) => m.CreateQuoteFormComponent,
      ),
  },
  { path: '**', redirectTo: 'quotes' },
];
