import { execFileSync } from 'node:child_process';
import { expect, test } from '@playwright/test';

// Este archivo NO corre con el resto (usa su propio servidor): npx playwright test --config=playwright.cache.config.ts
const desplegar = (...args: string[]) => execFileSync('node', ['scripts/desplegar-demo.mjs', ...args], { encoding: 'utf8' }).trim();

test('sin huella: el navegador sigue con el JavaScript viejo hasta que vence su caché', async ({ page }) => {
  console.log(desplegar('1'));
  await page.goto('/');
  await expect(page.locator('#version')).toHaveText('JavaScript versión 1');

  console.log(desplegar('2')); // el "despliegue": app.js ahora dice 2, pero la URL es la misma
  await page.goto('/');
  console.log('VISTO sin huella:', await page.locator('#version').textContent());
  await expect(page.locator('#version')).toHaveText('JavaScript versión 1'); // el navegador usó su copia
});

test('con huella en ?v=: el HTML nuevo apunta a una URL nueva y el navegador la pide', async ({ page }) => {
  console.log(desplegar('1', '--huella'));
  await page.goto('/');
  await expect(page.locator('#version')).toHaveText('JavaScript versión 1');

  console.log(desplegar('2', '--huella'));
  await page.goto('/');
  console.log('VISTO con huella:', await page.locator('#version').textContent());
  await expect(page.locator('#version')).toHaveText('JavaScript versión 2');
});
