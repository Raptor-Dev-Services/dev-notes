import { expect, test } from '@playwright/test';

test('se entra y se crea un ticket solo con teclado, y al cambiar de pantalla el foco va al h1', async ({ page }) => {
  await page.goto('/entrar');
  await expect(page.getByRole('heading', { name: 'Entrar' })).toBeVisible();

  await page.keyboard.press('Tab'); // correo
  await page.keyboard.type('ana@example.com');
  await page.keyboard.press('Tab'); // contraseña
  await page.keyboard.type('clave-de-ejemplo');
  await page.keyboard.press('Enter');

  await expect(page.getByRole('heading', { name: 'Tickets' })).toBeFocused();
  await expect(page).toHaveTitle('Tickets - Mesa de ayuda');

  await page.getByLabel('Título').focus();
  await page.keyboard.type('Ticket creado con teclado');
  await page.keyboard.press('Enter');
  await expect(page.getByRole('list', { name: 'Lista de tickets' })).toContainText('Ticket creado con teclado');
});
