import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding, withViewTransitions } from '@angular/router';
import { authInterceptor, errorInterceptor, retryInterceptor } from './core/interceptors';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // withViewTransitions() animates same view-transition-name elements across
    // routes (list card -> detail card). withComponentInputBinding() feeds the
    // :id route param into QuoteDetailComponent's input().
    provideRouter(routes, withViewTransitions(), withComponentInputBinding()),
    // Order = request travels top->bottom toward the backend; the error/response
    // travels bottom->top on the way back. So:
    //   errorInterceptor  (outermost) maps whatever finally fails -> AppError
    //   retryInterceptor             re-issues idempotent GETs on transient errors
    //   authInterceptor   (innermost) stamps the Bearer header on every (re)try
    provideHttpClient(
      withInterceptors([errorInterceptor, retryInterceptor, authInterceptor]),
    ),
  ],
};
