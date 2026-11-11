import { expect, test } from '@playwright/test';

test('la CSP bloquea un script en línea inyectado', async ({ page }) => {
  const respuesta = await page.goto('/entrar');
  const csp = respuesta?.headers()['content-security-policy'];
  test.skip(!csp, 'Corre el servidor con la variable CSP para esta prueba');
  expect(csp).not.toContain('unsafe-inline');

  const mensajes: string[] = [];
  page.on('console', (m) => mensajes.push(m.text()));
  await page.evaluate(() => {
    const s = document.createElement('script');
    s.textContent = 'window.__inyectado = true';
    document.body.append(s); // lo mismo que haría un XSS exitoso al insertar un script
  });
  expect(await page.evaluate(() => '__inyectado' in window)).toBe(false);
  expect(mensajes.join('\n')).toContain('Content Security Policy');
});
