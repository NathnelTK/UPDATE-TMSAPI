import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    // Batch rapid change-detection events into a single check — reduces
    // unnecessary re-renders when multiple signals fire in quick succession.
    provideZoneChangeDetection({ eventCoalescing: true }),
    // withComponentInputBinding() lets URL params flow directly into @input()
    // fields on routed components (used in Exercise 4 / course-detail).
    provideRouter(routes, withComponentInputBinding()),
    // Register HttpClient globally — needed from Exercise 6 onwards.
    // Added now to avoid a confusing NullInjectorError later.
    provideHttpClient(),
  ],
};
