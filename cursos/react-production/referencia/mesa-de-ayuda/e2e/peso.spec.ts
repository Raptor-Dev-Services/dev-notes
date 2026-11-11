import { test } from '@playwright/test';

// Suma el JavaScript que el navegador descarga al abrir /entrar (tamaños sin comprimir: este servidor de ejemplo no comprime).
test('JavaScript descargado al abrir /entrar', async ({ page }) => {
  await page.goto('/entrar');
  await page.getByRole('heading', { name: 'Entrar' }).waitFor();
  const archivos = await page.evaluate(() =>
    performance
      .getEntriesByType('resource')
      .filter((r) => new URL(r.name).pathname.endsWith('.js'))
      .map((r) => ({ nombre: new URL(r.name).pathname, bytes: (r as PerformanceResourceTiming).decodedBodySize })),
  );
  const total = archivos.reduce((s, a) => s + a.bytes, 0);
  console.log(`MEDIDA js_en_entrar archivos=${archivos.length} total_kB=${(total / 1000).toFixed(2)}`);
});
