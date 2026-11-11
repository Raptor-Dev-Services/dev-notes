# 14 - Pruebas del front - qué probar y cómo saber que la prueba sirve

## Problema que resuelve

Un front sin pruebas se rompe en silencio: alguien renombra un campo de la API, mueve un componente o cambia un texto, y el error aparece en producción. Un front con pruebas que prueban la implementación se rompe en voz alta y por nada: cada refactorización pone la suite en rojo aunque la pantalla siga igual.

Esta nota da el criterio (qué probar y qué no), la configuración mínima (Vitest, Testing Library y MSW), y la regla que separa una prueba de un deseo: **hay que verla fallar por la razón correcta**.

> Todo el código de esta nota corre en el proyecto de ejemplo del curso "React en producción con TypeScript" (la "Mesa de ayuda", un **Ejemplo ilustrativo**), con las versiones de noviembre de 2026: Vitest 5.1.0, Testing Library React 16.3.3, user-event 14.6.9, jest-dom 7.0.1, MSW 3.0.5, jsdom 31.0.0 y Playwright 1.65.0. Vitest 5.1.0 pide Node `^22.12.0`, `^24.0.0` o `>=26.0.0`.

---

## Dónde poner el esfuerzo

El "trofeo de pruebas" de Kent C. Dodds reparte el esfuerzo en cuatro capas:

| Capa | Qué es | Ejemplo en la Mesa de ayuda |
|------|--------|-----------------------------|
| Estática | el compilador y el linter, sin correr nada | `tsc --noEmit`, ESLint y la regla de fronteras |
| Unitaria | una función o módulo aislado | el cliente `pedir`, los esquemas de `zod` |
| Integración | varias piezas juntas, con la red simulada | una pantalla con su formulario contra MSW |
| Punta a punta | navegador real contra el sistema real | entrar y crear un ticket con Playwright |

La frase que se asocia a la idea, "Write tests. Not too many. Mostly integration.", la publicó Guillermo Rauch en un tuit del 10 de diciembre de 2016. Dodds escribió después un artículo con ese título (octubre de 2017) y presentó el trofeo en un tuit del 6 de febrero de 2018.

---

## Qué probar y qué no

> The more your tests resemble the way your software is used, the more confidence they can give you. (Testing Library, "Guiding Principles")

| Se prueba | No se prueba |
|-----------|--------------|
| lo que ve y hace la persona: textos, roles, estados | el nombre de un estado interno o de un hook |
| la frontera con el servidor: éxito, 422, 500 y red caída | que `useState` funciona |
| los caminos de error y de vacío | lo que ya prueba la librería que usas |
| el contrato: qué pasa si el servidor cambia un campo | el CSS |

La prueba de fuego: si una refactorización que **no cambia el comportamiento** rompe la prueba, la prueba estaba atada a la implementación.

---

## La configuración mínima

Vitest comparte la configuración de Vite: el mismo `plugins` sirve al build y a las pruebas.

```ts
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: { proxy: { '/api': 'http://127.0.0.1:3100' } },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
  },
});
```

El archivo `setup` corre antes de cada archivo de prueba:

```ts
import '@testing-library/jest-dom/vitest';
import { afterAll, afterEach, beforeAll, beforeEach } from 'vitest';
import { cleanup } from '@testing-library/react';
import { servidor } from './servidor';
import i18n from '../shared/i18n';

beforeAll(() => servidor.listen({ onUnhandledFrame: 'error' }));
// jsdom declara navigator.language = 'en-US': sin esto la app arrancaría en inglés y las pruebas buscarían texto en español.
beforeEach(async () => {
  await i18n.changeLanguage('es');
});
afterEach(() => {
  cleanup();
  servidor.resetHandlers();
});
afterAll(() => servidor.close());
```

Tres decisiones que salieron al correr, no de un libro:

- **`onUnhandledFrame: 'error'`.** Una petición sin manejador falla la prueba. En MSW 3.0.5 la opción anterior, `onUnhandledRequest`, ya no existe en sus tipos y TypeScript lo marca.
- **`cleanup()` explícito.** Sin `globals` en Vitest, Testing Library no limpia sola entre pruebas. Al quitarlo, 4 de las 6 pruebas de la pantalla fallaron con `Found multiple elements with the text: /La impresora no responde/`.
- **El idioma.** jsdom declara `navigator.language` como `en-US`: sin el `changeLanguage('es')` la app arrancaba en inglés y las pruebas buscaban texto en español. (Playwright tiene el mismo comportamiento: se arregla con `locale: 'es-MX'` en su configuración.)

---

## Simular la red, no tus componentes

MSW intercepta `fetch`: tu código corre igual que en producción, desde la petición hasta la pantalla.

```ts
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

export const ticketsDePrueba = [
  { id: 1, titulo: 'La impresora no responde', prioridad: 'media', estado: 'abierto', creadoEn: '2026-11-02T15:00:00.000Z' },
];

export const servidor = setupServer(
  http.get('/api/tickets', () => HttpResponse.json({ datos: ticketsDePrueba, total: 1 })),
);
```

Y un `render` con los proveedores que la app necesita:

```tsx
import type { ReactElement } from 'react';
import { render } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

/** Un QueryClient nuevo por prueba y sin reintentos: un error debe verse ya, no tras tres intentos. */
export function renderConProveedores(ui: ReactElement) {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(<QueryClientProvider client={cliente}>{ui}</QueryClientProvider>);
}
```

- Un `QueryClient` **nuevo por prueba**: una caché compartida contaminaría las pruebas siguientes.
- `retry: false`: TanStack Query reintenta tres veces con retroceso exponencial por omisión, y una prueba de error tardaría en verlo.

Se mockea el **borde** del sistema, no un componente propio. Si sustituyes `useTickets` por un doble, ya no pruebas que `useTickets` funcione con la API.

---

## Las consultas, por rol primero

Testing Library recomienda este orden: `getByRole` ("tu primera preferencia para casi todo") y `getByLabelText` para campos; luego `getByText`, `getByPlaceholderText` y `getByDisplayValue`; después `getByAltText` y `getByTitle`; y `getByTestId` solo cuando no hay otra forma, porque el usuario no ve los ids.

| Familia | Qué hace |
|---------|----------|
| `getBy...` | devuelve el elemento o lanza error si no hay (o si hay varios) |
| `queryBy...` | devuelve `null` si no hay: sirve para afirmar que algo **no** está |
| `findBy...` | asíncrona: espera hasta encontrarlo (1 000 ms por omisión) |

Buscar por rol y nombre también prueba, de paso, que la pantalla es accesible.

---

## Una pantalla con sus cuatro estados

```tsx
it('muestra los tickets que devuelve la API', async () => {
  renderConProveedores(<TicketsPage />);
  expect(screen.getByRole('status')).toHaveTextContent('Cargando');
  expect(await screen.findByText(/La impresora no responde/)).toBeInTheDocument();
});

it('muestra el estado vacío', async () => {
  servidor.use(http.get('/api/tickets', () => HttpResponse.json({ datos: [], total: 0 })));
  renderConProveedores(<TicketsPage />);
  expect(await screen.findByText('No hay tickets con ese filtro.')).toBeInTheDocument();
});

it('muestra el error y permite reintentar', async () => {
  servidor.use(http.get('/api/tickets', () => HttpResponse.json({ codigo: 'interno', mensaje: 'Falla provocada' }, { status: 500 }), { once: true }));
  renderConProveedores(<TicketsPage />);
  expect(await screen.findByRole('alert')).toHaveTextContent('Falla provocada');
  await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }));
  expect(await screen.findByText(/La impresora no responde/)).toBeInTheDocument();
});
```

Fragmento de `src/features/tickets/tickets-page.test.tsx` (seis pruebas en total: también cubre el error 422 pegado al campo y la red caída con `HttpResponse.error()`, y en ambos casos verifica que lo escrito se conserva). `{ once: true }` hace que el primer intento falle y el segundo pase.

---

## El contrato y la sesión

```ts
import { describe, expect, it } from 'vitest';
import { http, HttpResponse } from 'msw';
import { servidor } from '../../test/servidor';
import { ApiError, pedir } from './client';
import { paginaDeTicketsSchema } from './contract';

describe('pedir', () => {
  it('falla con nombre cuando el servidor rompe el contrato', async () => {
    // El servidor renombró "prioridad" a "urgencia": el tipo ya no coincide con la realidad.
    servidor.use(http.get('/api/tickets', () => HttpResponse.json({ datos: [{ id: 1, titulo: 'x', urgencia: 'alta', estado: 'abierto', creadoEn: '2026-11-02T15:00:00.000Z' }], total: 1 })));
    await expect(pedir('/tickets', { esquema: paginaDeTicketsSchema })).rejects.toThrow(/prioridad/);
  });

  it('convierte un 422 en ApiError con errores por campo', async () => {
    servidor.use(http.get('/api/tickets', () => HttpResponse.json({ codigo: 'validacion', mensaje: 'Revisa los campos.', errores: { titulo: 'Muy corto' } }, { status: 422 })));
    const error = await pedir('/tickets', { esquema: paginaDeTicketsSchema }).catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).errores).toEqual({ titulo: 'Muy corto' });
  });

  it('manda la cabecera anti-CSRF en cada petición', async () => {
    let cabecera: string | null = null;
    servidor.use(
      http.get('/api/tickets', ({ request }) => {
        cabecera = request.headers.get('x-requested-by');
        return HttpResponse.json({ datos: [], total: 0 });
      }),
    );
    await pedir('/tickets', { esquema: paginaDeTicketsSchema });
    expect(cabecera).toBe('mesa-de-ayuda');
  });
});
```

El primer caso es la prueba de la sesión 1 del curso: el servidor renombra `prioridad` a `urgencia` y el cliente **falla con el nombre del campo** en vez de pintar una pantalla rota. El tercero verifica la cabecera que el servidor exige contra CSRF.

Una ruta protegida se prueba con un router en memoria, y con **tres** casos, no dos: sin sesión (401), con sesión, y con fallo del servidor (500). Un 500 no es "no hay sesión"; mezclarlos manda al usuario a entrar cuando el servidor está caído.

```tsx
import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { http, HttpResponse } from 'msw';
import { servidor } from '../../test/servidor';
import { RutaProtegida } from './ruta-protegida';

function montar(rutaInicial: string) {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const router = createMemoryRouter(
    [
      { path: '/entrar', element: <p>Pantalla de entrada</p> },
      { element: <RutaProtegida />, children: [{ path: '/', element: <p>Contenido privado</p> }] },
    ],
    { initialEntries: [rutaInicial] },
  );
  return render(
    <QueryClientProvider client={cliente}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
}

describe('RutaProtegida', () => {
  it('manda a /entrar cuando la API contesta 401', async () => {
    servidor.use(http.get('/api/me', () => HttpResponse.json({ codigo: 'sin-sesion', mensaje: 'Inicia sesión.' }, { status: 401 })));
    montar('/');
    expect(await screen.findByText('Pantalla de entrada')).toBeInTheDocument();
    expect(screen.queryByText('Contenido privado')).not.toBeInTheDocument();
  });

  it('muestra el contenido cuando hay sesión', async () => {
    servidor.use(http.get('/api/me', () => HttpResponse.json({ id: 1, nombre: 'Ana Ejemplo', email: 'ana@example.com' })));
    montar('/');
    expect(await screen.findByText('Contenido privado')).toBeInTheDocument();
  });

  it('distingue un fallo del servidor de "no hay sesión"', async () => {
    servidor.use(http.get('/api/me', () => HttpResponse.json({ codigo: 'interno', mensaje: 'Falla' }, { status: 500 })));
    montar('/');
    expect(await screen.findByRole('alert')).toHaveTextContent('No pudimos comprobar tu sesión.');
  });
});
```

---

## Verlo fallar por la razón correcta

Una prueba que nunca viste fallar no demuestra nada. El procedimiento: **rompe el código a propósito** y lee el fallo.

Se rompió `EstadoDeDatos` para que nunca mostrara el estado vacío (`if (false && vacio(consulta.data))`). Salida real:

```text
FAIL  src/features/tickets/tickets-page.test.tsx > TicketsPage > muestra el estado vacío
TestingLibraryElementError: Unable to find an element with the text: No hay tickets con ese filtro..
This could be because the text is broken up by multiple elements. In this case, you can provide a
function for your text matcher to make your matcher more flexible.
Ignored nodes: comments, script, style
<body>
  <div>
    <main>
      <h1>Tickets</h1>
```

Falla por la razón correcta: no encuentra el texto del estado vacío. Y Testing Library imprime el DOM, que es la mitad del diagnóstico.

### La prueba vacua

```tsx
it('prueba vacua: el estado vacío no aparece', () => {
  renderConProveedores(<TicketsPage />);
  expect(screen.queryByText('No hay tickets con ese filtro.')).not.toBeInTheDocument();
});
```

Pasa (`Tests  1 passed (1)`) **y seguiría pasando con el estado vacío roto**: en ese instante la pantalla todavía dice "Cargando...". Arreglo: antes de la aserción negativa, esperar algo que sí debe aparecer (`await screen.findByText(...)`).

### Una petición sin manejador

```text
[MSW] Error: intercepted a request without a matching request handler:
  • GET /api/ruta-sin-manejador
...
Caused by: InternalError: [MSW] Cannot bypass a request when using the "error" strategy for the "onUnhandledFrame" option.
```

Es lo que se quiere: una prueba que llega a una ruta olvidada o a la red real **debe fallar**.

---

## Punta a punta: lo que jsdom no ve

jsdom no tiene cookies reales, ni layout, ni caché. Una E2E de Playwright en Chromium sí. Playwright recomienda probar lo que ve el usuario final, aislar cada prueba, usar localizadores con espera automática, preferir atributos visibles al usuario sobre XPath o CSS, y usar aserciones que esperan.

```ts
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
```

La segunda prueba comprueba algo que jsdom no puede: que `document.cookie` está vacío y la cookie `sid` es `HttpOnly`.

```ts
import { defineConfig } from '@playwright/test';

const puerto = process.env.PORT ?? '3100';

export default defineConfig({
  testDir: './e2e',
  testIgnore: 'cache.spec.ts',
  forbidOnly: !!process.env.CI, // un test.only olvidado rompe la CI en lugar de saltarse todo lo demás
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: { baseURL: `http://127.0.0.1:${puerto}`, locale: 'es-MX', trace: 'retain-on-failure' },
  webServer: {
    command: 'node server/server.mjs',
    url: `http://127.0.0.1:${puerto}/api/me`,
    reuseExistingServer: false,
  },
});
```

Verlo fallar: se cambió el texto del botón de "Entrar" a "Acceder". La prueba tardó **30 segundos** en fallar (el timeout por omisión) con `waiting for getByRole('button', { name: 'Entrar' })`, y dejó una traza: `npx playwright show-trace test-results/<carpeta>/trace.zip`. Con `retries` y `forbidOnly` en CI (arriba), un `test.only` olvidado rompe el pipeline en vez de saltarse todo lo demás.

---

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Pruebas de integración con MSW para cada pantalla con datos | Mockear tus propios hooks o componentes |
| Una E2E por flujo que no puede romperse (entrar, pagar, crear) | Una E2E por cada pantalla: lenta y frágil |
| Probar el contrato con la API: qué pasa si cambia un campo | Probar el nombre de un estado interno |
| Romper el código a propósito para ver la prueba fallar | Confiar en una prueba que nunca viste en rojo |
| Esperas con `findBy` y aserciones de Playwright | `setTimeout` o `waitForTimeout` fijos |

> Fuente: documentación de Testing Library ("Guiding Principles", "About Queries"); documentación de Playwright ("Best Practices", "Trace viewer"); documentación de TanStack Query (guía de pruebas); Kent C. Dodds, "The Testing Trophy and Testing Classifications". Libros: *React Application Architecture for Production* (Alickovic y Alicea, 2026), cap. 11; *React Anti-Patterns* (Qiu, 2023), caps. 5 a 7; *Modern Full-Stack React Projects* (Bugl, 2026), cap. 9.

---

## Glosario

| Término | Significado |
|---------|-------------|
| Prueba de integración | varias piezas juntas (pantalla, hook, cliente) con la red simulada |
| MSW | librería que intercepta `fetch` y lo contesta con manejadores propios |
| Prueba vacua | prueba que pasa aunque el código esté roto |
| Locator | forma de Playwright de encontrar un elemento, con espera y reintento |
| Traza (trace) | archivo con el paso a paso de una prueba E2E: DOM, red y consola |

---

*Rogelio Arriaga Gonzalez*
