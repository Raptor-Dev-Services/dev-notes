import { defineConfig } from '@playwright/test';

const puerto = process.env.PORT ?? '3100';

// Servidor aparte, con la caché "de red de entrega" de 4 horas para todo lo que no es HTML.
export default defineConfig({
  testDir: './e2e',
  testMatch: 'cache.spec.ts',
  workers: 1,
  fullyParallel: false,
  use: { baseURL: `http://127.0.0.1:${puerto}` },
  webServer: {
    command: `DIST_DIR=demo-cache-dist CACHE_ASSETS='public, max-age=14400' node server/server.mjs`,
    url: `http://127.0.0.1:${puerto}/api/me`,
    reuseExistingServer: false,
  },
});
