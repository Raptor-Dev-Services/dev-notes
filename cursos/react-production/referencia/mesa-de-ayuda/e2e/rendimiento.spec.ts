import { expect, test } from '@playwright/test';
import { entrar } from './ayudas';

// Mide en un Chromium real cuánto tarda el navegador en procesar lo que escribes (Event Timing API)
// y cuántos nodos de lista hay en el DOM. Los números dependen de la máquina: compara modos entre sí, no contra un valor fijo.
for (const modo of ['lenta', 'memo', 'virtual'] as const) {
  test(`volumen: modo ${modo}`, async ({ page }) => {
    await entrar(page);
    await page.goto(`/volumen?modo=${modo}`);
    await expect(page.getByRole('status')).toHaveText('5000 resultados');
    await page.evaluate(() => {
      const w = window as unknown as { __eventos: number[] };
      w.__eventos = [];
      new PerformanceObserver((lista) => {
        for (const e of lista.getEntries()) w.__eventos.push(e.duration);
      }).observe({ type: 'event', durationThreshold: 16 });
    });
    const t0 = Date.now();
    await page.getByLabel('Buscar').pressSequentially('incidencia 1', { delay: 0 });
    await expect(page.getByRole('status')).not.toHaveText('5000 resultados');
    await page.evaluate(() => new Promise((r) => requestAnimationFrame(() => requestAnimationFrame(r))));
    const total = Date.now() - t0;
    const eventos = await page.evaluate(() => (window as unknown as { __eventos: number[] }).__eventos);
    const nodos = await page.locator('main li').count();
    console.log(`MEDIDA modo=${modo} nodos_li=${nodos} total_ms=${total} eventos_lentos=${eventos.length} peor_evento_ms=${Math.max(0, ...eventos)}`);
    if (modo === 'virtual') expect(nodos).toBeLessThan(100);
  });
}
