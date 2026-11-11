import type { Page } from '@playwright/test';

export async function entrar(page: Page) {
  await page.goto('/entrar');
  await page.getByLabel('Correo').fill('ana@example.com');
  await page.getByLabel('Contraseña').fill('clave-de-ejemplo');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await page.getByRole('heading', { name: 'Tickets' }).waitFor();
}
