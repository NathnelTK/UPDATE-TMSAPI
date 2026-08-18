import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors, withXsrfConfiguration } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { routes } from './app.routes';
import { credentialsInterceptor } from './interceptors/credentials.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    // Batch rapid change-detection events into a single check — reduces
    // unnecessary re-renders when multiple signals fire in quick succession.
    provideZoneChangeDetection({ eventCoalescing: true }),
    // withComponentInputBinding() lets URL params flow directly into @input()
    // fields on routed components (used in Exercise 4 / course-detail).
    provideRouter(routes, withComponentInputBinding()),
    // M10 Session 2 — credentialsInterceptor attaches withCredentials:true so the
    // HttpOnly auth cookie flows; withXsrfConfiguration makes Angular read the
    // XSRF-TOKEN cookie and echo it as X-XSRF-TOKEN on POST/PUT/DELETE.
    provideHttpClient(
      withInterceptors([credentialsInterceptor]),
      withXsrfConfiguration({
        cookieName: 'XSRF-TOKEN', // Cookie name set by the .NET server
        headerName: 'X-XSRF-TOKEN', // Header name the .NET antiforgery service expects
      }),
    ),
    // M9 Session 2 — Angular Material needs the animations package for sort
    // arrows, paginator transitions, etc. The async provider lazy-loads it.
    provideAnimationsAsync(),
  ],
};
