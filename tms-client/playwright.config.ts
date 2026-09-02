import { defineConfig, devices } from '@playwright/test';

/**
 * M12 Session 2 — End-to-end tests (Playwright).
 *
 * Prerequisites to run locally:
 *   1. Backend API running on http://localhost:5003  (dotnet run in TmsApi.Api)
 *   2. `npm start` serves the client on http://localhost:4200 — Playwright starts
 *      it automatically via `webServer` below (or reuses one you already have up).
 *
 * The `setup` project logs in once and saves the JWT session to
 * playwright/.auth/admin.json; the `chromium` project reuses it as storageState so
 * every test starts already authenticated. That auth file is gitignored — it holds
 * a real token and must never be committed.
 */
const AUTH_FILE = 'playwright/.auth/admin.json';

export default defineConfig({
  testDir: './e2e',
  tsconfig: './e2e/tsconfig.json',
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
  },
  projects: [
    // Runs auth.setup.ts first to produce the storageState file.
    { name: 'setup', testMatch: /.*\.setup\.ts/ },
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], storageState: AUTH_FILE },
      dependencies: ['setup'],
    },
  ],
  webServer: {
    command: 'npm start',
    url: 'http://localhost:4200',
    reuseExistingServer: !process.env['CI'],
    timeout: 120_000,
  },
});
