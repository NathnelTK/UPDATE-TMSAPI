import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';
import { errorInterceptor } from './interceptors/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    // Batch rapid change-detection events into a single check — reduces
    // unnecessary re-renders when multiple signals fire in quick succession.
    provideZoneChangeDetection({ eventCoalescing: true }),
    // withComponentInputBinding() lets URL params flow directly into @input()
    // fields on routed components (used in course-detail / enrollment-form).
    provideRouter(routes, withComponentInputBinding()),
    // M11 — JWT bearer auth. authInterceptor attaches `Authorization: Bearer`
    // and transparently refreshes on 401; errorInterceptor centralises RFC 7807
    // error surfacing. Order matters: auth runs first so a refreshed retry still
    // passes back through error handling. (The old cookie/XSRF interceptors were
    // retired when the API moved from HttpOnly cookies to bearer tokens.)
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    // Angular Material needs the animations package for sort arrows, paginator
    // transitions, etc. The async provider lazy-loads it.
    provideAnimationsAsync(),
  ],
};
