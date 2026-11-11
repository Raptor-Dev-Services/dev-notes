import { expect, test } from '@playwright/test';

test('entrar, crear un ticket y verlo en la lista', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveURL(/\/entrar$/); // sin sesión, la ruta protegida manda a entrar

  await page.getByLabel('Correo').fill('ana@example.com');
  await page.getByLabel('Contraseña').fill('clave-de-ejemplo');
  await page.getByRole('button', { name: 'Entrar' }).click();

  await expect(page.getByRole('heading', { name: 'Tickets' })).toBeVisible();
  await page.getByLabel('Título').fill('Reportar un monitor sin señal');
  await page.getByLabel('Prioridad').selectOption('alta');
  await page.getByRole('button', { name: 'Crear ticket' }).click();

  await expect(page.getByRole('list', { name: 'Lista de tickets' })).toContainText('Reportar un monitor sin señal (alta, abierto)');
});

test('la cookie de sesión no es legible desde JavaScript', async ({ page, context }) => {
  await page.goto('/entrar');
  await page.getByLabel('Correo').fill('ana@example.com');
  await page.getByLabel('Contraseña').fill('clave-de-ejemplo');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page.getByRole('heading', { name: 'Tickets' })).toBeVisible();

  expect(await page.evaluate(() => document.cookie)).toBe('');
  const [sid] = (await context.cookies()).filter((c) => c.name === 'sid');
  expect(sid?.httpOnly).toBe(true);
});
