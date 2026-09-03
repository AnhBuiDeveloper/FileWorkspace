import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests/ui',
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'http://127.0.0.1:5090',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure'
  },
  webServer: {
    command: 'node tests/ui/start-server.mjs',
    url: 'http://127.0.0.1:5090',
    reuseExistingServer: false,
    timeout: 60000,
    env: {
      UPLOAD_ACCESS_TOKEN: 'playwright-e2e-token',
      ASPNETCORE_ENVIRONMENT: 'Testing'
    }
  }
});
