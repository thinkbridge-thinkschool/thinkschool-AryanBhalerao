import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { QuotesService } from '../services/quotes.service';

// Functional guard: lets the navigation through when a live JWT is present,
// otherwise redirects to /login carrying the attempted url as ?returnUrl so the
// login route can bounce the user straight back after authenticating.
export const authGuard: CanActivateFn = (_route, state) => {
  const svc = inject(QuotesService);
  const router = inject(Router);

  if (svc.hasValidToken()) {
    return true;
  }

  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};
