// Karma test entry point (M12 Session 2).
//
// `polyfills` (src/polyfills.ts) loads zone.js first; this file then loads the
// Zone.js testing patch and boots the Angular testing environment exactly once.
// The @angular-devkit karma builder discovers and injects every *.spec.ts in the
// tsconfig.spec.json program automatically, so no require.context wiring is needed.
import 'zone.js/testing';
import { getTestBed } from '@angular/core/testing';
import {
  BrowserDynamicTestingModule,
  platformBrowserDynamicTesting,
} from '@angular/platform-browser-dynamic/testing';

getTestBed().initTestEnvironment(
  BrowserDynamicTestingModule,
  platformBrowserDynamicTesting(),
);
