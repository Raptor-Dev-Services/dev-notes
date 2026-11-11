import { defineConfig } from '@playwright/test';

const puerto = process.env.PORT ?? '3100';

export default defineConfig({
  testDir: './e2e',
  testIgnore: 'cache.spec.ts',
  forbidOnly: !!process.env.CI, // un test.only olvidado rompe la CI en lugar de saltarse todo lo demás
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: { baseURL: `http://127.0.0.1:${puerto}`, locale: 'es-MX', trace: 'retain-on-failure' },
  webServer: {
    command: 'node server/server.mjs',
    url: `http://127.0.0.1:${puerto}/api/me`,
    reuseExistingServer: false,
  },
});
