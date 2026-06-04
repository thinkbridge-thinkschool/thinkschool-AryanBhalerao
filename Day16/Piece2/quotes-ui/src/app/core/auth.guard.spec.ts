import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { authGuard } from './auth.guard';
import { QuotesService } from '../services/quotes.service';

// Exercises the two edges of the functional guard wired on the `create` route:
//   valid token  -> activation allowed (true)
//   no token     -> redirected to /login?returnUrl=<attempted url>
describe('authGuard (CanActivateFn)', () => {
  function setup(hasValidToken: boolean) {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: QuotesService, useValue: { hasValidToken: () => hasValidToken } },
      ],
    });
  }

  const stateFor = (url: string) => ({ url }) as RouterStateSnapshot;

  it('allows activation when the token is valid', () => {
    setup(true);
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as any, stateFor('/create')),
    );
    expect(result).toBe(true);
  });

  it('redirects to /login with the attempted url as returnUrl when there is no valid token', () => {
    setup(false);
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as any, stateFor('/create')),
    );

    expect(result).toBeInstanceOf(UrlTree);
    const tree = result as UrlTree;
    const router = TestBed.inject(Router);
    // Serialize to assert both the target path and the preserved returnUrl.
    expect(router.serializeUrl(tree)).toBe('/login?returnUrl=%2Fcreate');
  });
});
