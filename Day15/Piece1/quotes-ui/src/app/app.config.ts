import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor, errorInterceptor, retryInterceptor } from './core/interceptors';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
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
