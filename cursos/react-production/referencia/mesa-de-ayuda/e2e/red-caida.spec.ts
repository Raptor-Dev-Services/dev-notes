import { expect, test } from '@playwright/test';

test('con la red caída el formulario conserva lo escrito y, al recargar, el borrador', async ({ page }) => {
  await page.goto('/entrar');
  await page.getByLabel('Correo').fill('ana@example.com');
  await page.getByLabel('Contraseña').fill('clave-de-ejemplo');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page.getByRole('heading', { name: 'Tickets' })).toBeVisible();

  await page.route('**/api/tickets', (ruta) => (ruta.request().method() === 'POST' ? ruta.abort('connectionfailed') : ruta.continue()));
  await page.getByLabel('Título').fill('Escrito justo antes de que cayera la red');
  await page.getByRole('button', { name: 'Crear ticket' }).click();
  await expect(page.getByText(/Lo que escribiste sigue aquí/)).toBeVisible();
  await expect(page.getByLabel('Título')).toHaveValue('Escrito justo antes de que cayera la red');

  await page.reload();
  await expect(page.getByLabel('Título')).toHaveValue('Escrito justo antes de que cayera la red');
});
