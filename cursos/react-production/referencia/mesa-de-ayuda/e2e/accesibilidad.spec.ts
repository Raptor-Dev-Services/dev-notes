import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';
import { entrar } from './ayudas';

const REGLAS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

test('la pantalla de entrada no tiene violaciones WCAG 2.2 AA detectables por axe', async ({ page }) => {
  await page.goto('/entrar');
  await expect(page.getByRole('heading', { name: 'Entrar' })).toBeVisible();
  const resultado = await new AxeBuilder({ page }).withTags(REGLAS).analyze();
  expect(resultado.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
});

test('la lista de tickets no tiene violaciones detectables por axe', async ({ page }) => {
  await entrar(page);
  await expect(page.getByRole('list', { name: 'Lista de tickets' })).toBeVisible();
  const resultado = await new AxeBuilder({ page }).withTags(REGLAS).analyze();
  expect(resultado.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
});
