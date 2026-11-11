<!-- _class: lead -->

# S06 - Pruebas y producción

### React en producción con TypeScript

**Rogelio Arriaga - Raptor Dev Services**

<!--
Última sesión del curso. Cada quien levanta el proyecto Mesa de ayuda (ejemplo ilustrativo): npm ci && npm test.
Para la parte de contenedores se necesita Docker; quien no lo tenga, sigue la práctica hasta el paso de nginx y lo marca como no ejecutado.
-->

---

## Agenda del día

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, objetivos, el caso real | 20 min |
| Parte 1 | Pruebas con criterio: Vitest, Testing Library y MSW | 55 min |
| Parte 2 | Una prueba de punta a punta con Playwright | 45 min |
| Parte 3 | Build, versión en los assets y caché | 55 min |
| Parte 4 | Pipeline y contenedor | 40 min |
| Parte 5 | Práctica: pruebas, E2E y verlo fallar | 60 min |
| Cierre | Proyecto final, tarea y quiz | 25 min |

<!-- Hay un descanso de 10 minutos entre la Parte 2 y la Parte 3, y otro a la mitad de la práctica. -->

---

## El lema de la sesión

> Un navegador con la versión vieja en caché hace que el arreglo exista solo en el servidor.

- Arreglaste el error, el pipeline salió en verde y lo desplegaste. Y el usuario sigue viendo el error.
- No es magia: es una cabecera que dijo "guarda esto cuatro horas".
- Hoy probamos el código antes de que salga **y** comprobamos que lo que sale es lo que el usuario recibe.

<!-- Pregunta al grupo: ¿alguien ha desplegado un arreglo y tuvo que decir "limpia la caché"? Esa frase es el síntoma de lo que vemos en la parte 3. -->

---

## Al terminar puedes

- Elegir qué se prueba con Vitest y Testing Library, qué con Playwright y qué no se prueba.
- Escribir una prueba que falle **por la razón correcta**, y reconocer una que pasa siempre.
- Simular la API con MSW sin mockear tus propios componentes.
- Escribir una prueba de punta a punta con Playwright y leer su traza cuando falle.
- Explicar por qué un asset con huella en el nombre se puede guardar un año y el HTML no.
- Reproducir un despliegue que deja al navegador con el JavaScript viejo, y arreglarlo.
- Armar el pipeline de un front (pruebas, build, imagen) y un contenedor que sirva `dist/`.

---

## Cómo trabajamos en esta sesión

- **Ejemplo ilustrativo:** "Mesa de ayuda", una app de tickets inventada para enseñar.
- Todo lo que se muestra se corrió. Lo que **no** se pudo correr en el laboratorio de redacción (el pipeline en GitHub y la construcción de la imagen) está marcado como fragmento.
- Versiones a noviembre de 2026: Vitest 5.1.0, Testing Library React 16.3.3, user-event 14.6.9, jest-dom 7.0.1, MSW 3.0.5, jsdom 31.0.0, Playwright 1.65.0, Vite 8.3.8, nginx 1.31.8.
- Vitest 5.1.0 pide Node `^22.12.0`, `^24.0.0` o `>=26.0.0`.

Fuente: `engines` y `peerDependencies` de `vitest@5.1.0` en npm.

---

## El caso real: el JavaScript viejo de la academia

En la propia academia (academy.raptorcloud.dev), Cloudflare guardaba el JavaScript viejo **hasta 4 horas**. Se resolvió con una huella del contenido en `?v=`.

- Un arreglo desplegado no llegaba al navegador de quien ya había visitado el sitio.
- Cloudflare documenta que el Browser Cache TTL por omisión es de **4 horas** en todos los planes, y que con la opción "Respect Existing Headers" no sobrescribe ni inserta `Cache-Control`.
- La huella cambia la URL; una URL nueva no está en ninguna caché.

Fuente: el caso, como lo registra el instructor; Cloudflare, "Edge Cache TTL and Browser Cache TTL" (developers.cloudflare.com/cache/how-to/edge-browser-cache-ttl/), a noviembre de 2026.

<!-- Contar el caso como está. No inventar más detalle: no sabemos qué otras capas intervinieron. Lo reproducimos en pequeño en la parte 3. -->

---

<!-- _class: lead -->

# Parte 1
## Pruebas con criterio

---

## El trofeo de pruebas

Una forma de decidir dónde poner el esfuerzo. De abajo hacia arriba:

| Capa | Qué es | En la Mesa de ayuda |
|---|---|---|
| Estática | el compilador y el linter encuentran errores sin correr nada | `tsc`, ESLint y la regla de fronteras |
| Unitaria | una función o un módulo aislado | el cliente `pedir` y los esquemas |
| Integración | varios módulos juntos, con la red simulada | una pantalla con su formulario, contra MSW |
| Punta a punta | el navegador real contra el sistema real | entrar y crear un ticket |

La frase que se asocia a esta idea, "Write tests. Not too many. Mostly integration.", la publicó Guillermo Rauch en un tuit del 10 de diciembre de 2016; Kent C. Dodds escribió después un artículo con ese título (octubre de 2017) y presentó el trofeo en un tuit del 6 de febrero de 2018.

Fuente: Kent C. Dodds, "The Testing Trophy and Testing Classifications" (kentcdodds.com).

---

## Qué probar y qué no

> The more your tests resemble the way your software is used, the more confidence they can give you.

(Testing Library, "Guiding Principles")

| Se prueba | No se prueba |
|---|---|
| lo que ve y hace la persona: texto, roles, estados | el nombre de un estado interno o de un hook |
| la frontera con el servidor: éxito, 422, 500, red caída | que `useState` funciona |
| los caminos de error | que una librería hace lo que su documentación promete |
| el contrato: qué pasa si el servidor cambia un campo | el CSS |

Si una refactorización que no cambia el comportamiento rompe la prueba, la prueba estaba probando la implementación.

Libros: *React Anti-Patterns* (Qiu, 2023), cap. 5; *React Application Architecture for Production* (Alickovic y Alicea, 2026), cap. 11.

---

## La configuración de Vitest

```ts {4-6|7-11|*}
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

- `vitest/config` extiende la configuración de Vite: el mismo `plugins` sirve al build y a las pruebas.
- `jsdom` simula un navegador en Node: sin red real, sin layout, sin CSS.
- `setupFiles` corre antes de cada archivo de prueba.

Fuente: documentación de Vitest (vitest.dev), `test.environment` y `test.setupFiles`.

---

## El `setup`: lo que toda prueba necesita

```ts {1-5|7|8-11|12-16|*}
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

- `onUnhandledFrame: 'error'`: una petición sin manejador **falla la prueba**. MSW 3.0.5 renombró la opción anterior (`onUnhandledRequest`) a `onUnhandledFrame` (está en sus tipos); con el nombre viejo, TypeScript marca el error: `'onUnhandledRequest' does not exist in type 'PartialObjectDeep<SharedOptions...'`.
- `cleanup()` se llama a mano: sin `globals` en Vitest, Testing Library no se limpia sola entre pruebas. Al quitarla, **4 de las 6 pruebas de la pantalla fallaron** con `Found multiple elements with the text: /La impresora no responde/`: los renders de las pruebas anteriores seguían en el documento.
- `i18n.changeLanguage('es')`: jsdom declara `en-US` (sesión 5).

---

## MSW: simular la red, no tus componentes

```ts {4-6|8-10|*}
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

export const ticketsDePrueba = [
  { id: 1, titulo: 'La impresora no responde', prioridad: 'media', estado: 'abierto', creadoEn: '2026-11-02T15:00:00.000Z' },
];

export const servidor = setupServer(
  http.get('/api/tickets', () => HttpResponse.json({ datos: ticketsDePrueba, total: 1 })),
);
```

- MSW intercepta `fetch`: tu código corre **igual que en producción**, de la petición a la pantalla.
- Se mockea el borde del sistema (la API), no un componente propio. Si mockeas `useTickets`, ya no pruebas que `useTickets` funcione con la API.
- Cada prueba puede sobrescribir un manejador con `servidor.use(...)`, y `resetHandlers` lo deshace.

---

## Un render con proveedores

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

- Un `QueryClient` **nuevo por prueba**: si se compartiera, la caché de una prueba contaminaría a la siguiente.
- `retry: false`: con los tres reintentos por omisión, un error tardaría segundos en aparecer en la pantalla.

Fuente: TanStack Query, guía de pruebas (tanstack.com/query/latest/docs/framework/react/guides/testing): el valor por omisión son tres reintentos con retroceso exponencial, y apagarlos con `QueryClientProvider` evita que las pruebas de error se pasen del tiempo. Aislar cada prueba con su propio cliente es la práctica que se sigue en el curso.

---

## Las consultas: por rol primero

| Prioridad | Consulta | Cuándo |
|---|---|---|
| 1 | `getByRole` | casi siempre: "tu primera preferencia para casi todo" |
| 1 | `getByLabelText` | campos de formulario |
| 2 | `getByText`, `getByPlaceholderText`, `getByDisplayValue` | cuando no hay rol |
| 3 | `getByAltText`, `getByTitle` | semánticas |
| 4 | `getByTestId` | último recurso: el usuario no lo ve |

| Familia | Qué hace |
|---|---|
| `getBy...` | devuelve el elemento o lanza error si no hay (o si hay varios) |
| `queryBy...` | devuelve `null` si no hay: sirve para afirmar que algo **no** está |
| `findBy...` | asíncrona: espera hasta encontrarlo (1 000 ms por omisión) |

Fuente: Testing Library, "About Queries" (testing-library.com/docs/queries/about/). Si buscas por rol y nombre, tus pruebas también prueban la accesibilidad.

---

## Las pruebas de la pantalla de tickets

```tsx {1-5|7-11|13-19|*}
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

Son **los cuatro estados** de la sesión 3: cargando, con datos, vacío y error. El reintento usa `{ once: true }`: el primer intento falla y el segundo pasa.

<!-- Fragmento de src/features/tickets/tickets-page.test.tsx. El archivo completo tiene 6 pruebas. -->

---

## El formulario: errores del servidor y red caída

```tsx {1-11|13-18|*}
it('pega el error 422 del servidor al campo y conserva lo escrito', async () => {
  servidor.use(
    http.post('/api/tickets', () => HttpResponse.json({ codigo: 'validacion', mensaje: 'Revisa los campos.', errores: { titulo: 'Ya existe un ticket con ese título.' } }, { status: 422 })),
  );
  renderConProveedores(<TicketsPage />);
  await screen.findByText(/La impresora/);
  await userEvent.type(screen.getByLabelText('Título'), 'Un título válido');
  await userEvent.click(screen.getByRole('button', { name: 'Crear ticket' }));
  expect(await screen.findByText('Ya existe un ticket con ese título.')).toBeInTheDocument();
  expect(screen.getByLabelText('Título')).toHaveValue('Un título válido');
});

it('conserva lo escrito cuando la red falla', async () => {
  servidor.use(http.post('/api/tickets', () => HttpResponse.error()));
  // ...mismo flujo: escribe, envía...
  expect(await screen.findByText(/Lo que escribiste sigue aquí/)).toBeInTheDocument();
  expect(screen.getByLabelText('Título')).toHaveValue('Sin red, pero no se pierde');
});
```

`HttpResponse.error()` simula un fallo de red, no una respuesta con error. Fragmento: el flujo completo está en `tickets-page.test.tsx`.

---

## Probar el contrato y la sesión

```ts {8-12|14-19|21-31|*}
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

El primer caso es el de la sesión 1: el servidor renombró `prioridad` a `urgencia`, y el cliente **falla con el nombre del campo** en lugar de pintar una pantalla rota.

---

## Una ruta protegida, con router en memoria

```tsx {10-23|25-31|*}
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

- `createMemoryRouter` prueba las redirecciones sin un navegador.
- Se prueban **los tres casos**: sin sesión (401), con sesión y fallo del servidor (500). Un 500 no es "no hay sesión", y mezclarlos manda al usuario a entrar cuando el servidor está caído.

---

## Verlo fallar: la razón correcta

Rompimos `EstadoDeDatos` para que nunca muestre el estado vacío (`if (false && vacio(...))`). Salida real:

```text
FAIL  src/features/tickets/tickets-page.test.tsx > TicketsPage > muestra el estado vacío
TestingLibraryElementError: Unable to find an element with the text: No hay tickets con ese filtro.. This
could be because the text is broken up by multiple elements. In this case, you can provide a function for
your text matcher to make your matcher more flexible.
Ignored nodes: comments, script, style
<body>
  <div>
    <main>
      <h1>Tickets</h1>
```

- Falla **por la razón correcta**: no encuentra el texto del estado vacío.
- Testing Library imprime el DOM al fallar: es la mitad del diagnóstico.

Una prueba que no has visto fallar no es una prueba: es un deseo.

---

## Verlo pasar cuando no debía: la prueba vacua

```tsx
it('prueba vacua: el estado vacío no aparece', () => {
  renderConProveedores(<TicketsPage />);
  expect(screen.queryByText('No hay tickets con ese filtro.')).not.toBeInTheDocument();
});
```

Esta prueba **pasa** (salida real: `Tests  1 passed (1)`) y pasaría igual con el estado vacío roto: en ese instante la pantalla todavía dice "Cargando...".

| Señal de prueba vacua | Arreglo |
|---|---|
| `not.toBeInTheDocument()` sin esperar nada antes | espera primero algo que **sí** debe aparecer (`await findBy...`) y después afirma lo que no |
| una aserción dentro de un `if` o de un `.then` sin `await` | la aserción siempre corre |
| nunca la viste fallar | rompe el código a propósito y mira |

---

## MSW 3: una petición sin manejador

Una prueba que llama a una ruta sin manejador no pasa inadvertida:

```text
[MSW] Error: intercepted a request without a matching request handler:
  • GET /api/ruta-sin-manejador
If you still wish to intercept this unhandled request, please create a request handler for it.
...
AssertionError: promise rejected "TypeError: fetch failed" instead of resolving
Caused by: InternalError: [MSW] Cannot bypass a request when using the "error" strategy for the "onUnhandledFrame" option.
```

Es lo que se quiere: una prueba que habla con la red real o con una ruta olvidada **debe fallar**, no depender de la suerte.

---

## Error típico: lo que no hay que probar

| Prueba | Problema |
|---|---|
| `expect(componente.state.cargando).toBe(true)` | prueba la implementación, no la pantalla |
| `jest.mock('./use-tickets')` | no pruebas que el hook funciona con la API |
| `data-testid` en todo | el usuario no ve ids: se pierde la prueba de accesibilidad |
| esperar con `setTimeout(500)` | lenta y frágil: usa `findBy` o `waitFor` |
| una prueba por línea de código | cobertura alta, confianza baja |

Libro: *React Anti-Patterns* (Qiu, 2023), caps. 5 a 7 (pruebas, refactorización y TDD).

---

<!-- _class: lead -->

# Parte 2
## Una prueba de punta a punta con Playwright

---

## Cuándo una prueba E2E

- Las pruebas de integración corren en jsdom: **no hay navegador, ni cookie, ni layout, ni caché**.
- Una E2E recorre lo que el usuario recorre, en un navegador real, contra el servidor real.
- Es lenta y cara de mantener: se usa para **los flujos que no pueden romperse** (entrar, pagar, crear), no para cada pantalla.

Playwright recomienda probar lo que ve el usuario final, aislar cada prueba, usar localizadores con espera automática, preferir atributos visibles al usuario sobre XPath o CSS, y usar aserciones que esperan.

Fuente: playwright.dev, "Best Practices". Libro: *Modern Full-Stack React Projects* (Bugl, 2026), cap. 9.

---

## La configuración

```ts {5-10|11|12-16|*}
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

- `webServer` levanta el servidor antes de las pruebas (aquí, la API de ejemplo que también sirve `dist/`).
- `locale: 'es-MX'`: Playwright usa `en-US` por omisión y la app mostraría el inglés.
- `forbidOnly` en CI: un `test.only` olvidado hace fallar el pipeline en lugar de saltarse todo lo demás.
- `trace: 'retain-on-failure'` guarda la traza de lo que falló.

Fuente: playwright.dev, "Test configuration" y "Trace viewer".

---

## La prueba

```ts {3-17|19-28|*}
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

- Se usan localizadores por **rol y etiqueta**, los mismos de Testing Library.
- `expect(...).toBeVisible()` espera hasta que se cumpla: no hay `sleep`.
- La segunda prueba comprueba algo que **jsdom no puede**: que `document.cookie` está vacío y que la cookie `sid` es `HttpOnly`.

---

## Verlo fallar: cambia un texto y lee la traza

Cambiamos el texto del botón de "Entrar" a "Acceder" y corrimos la prueba (salida real, abreviada):

```text
1) e2e/tickets.spec.ts:3:1 › entrar, crear un ticket y verlo en la lista
    Test timeout of 30000ms exceeded.
    Error: locator.click: Test timeout of 30000ms exceeded.
    Call log:
      - waiting for getByRole('button', { name: 'Entrar' })
       9 |   await page.getByRole('button', { name: 'Entrar' }).click();
         |                                                      ^
    attachment #2: trace (application/zip) ───────────────
    test-results/tickets-entrar-crear-un-ticket-y-verlo-en-la-lista/trace.zip
    Usage:
        npx playwright show-trace test-results/.../trace.zip
```

- Tardó **30 s** en fallar: es el timeout por omisión de la prueba. Una prueba rota en CI cuesta medio minuto.
- La traza trae el paso a paso, el DOM en cada momento, la red y la consola.

```bash
npx playwright show-trace test-results/<carpeta>/trace.zip
```

---

## Dónde corre cada tipo de prueba

| | Vitest + Testing Library + MSW | Playwright |
|---|---|---|
| Entorno | jsdom en Node | Chromium real |
| Servidor | simulado | el real |
| Velocidad | milisegundos | segundos |
| Cookie `HttpOnly`, caché, layout | no | sí |
| Cuándo corre | cada guardado | en el pipeline |
| Cuántas | muchas | pocas |

Ninguna sustituye a la otra: la integración encuentra errores de lógica rápido; la E2E encuentra los de entorno.

---

<!-- _class: lead -->

# Parte 3
## Build, versión en los assets y caché

---

## Lo que produce `vite build`

```bash
npx vite build
```

```text
dist/index.html                         0.56 kB
dist/assets/jsx-runtime-Wy9s6hX0.js     8.91 kB
dist/assets/volumen-page-w-fzBuzs.js   35.68 kB
dist/assets/tickets-CbCN6d0C.js        41.97 kB
dist/assets/components-qHOs4wNq.js     86.75 kB
dist/assets/contract-BO96gj8c.js      167.23 kB
dist/assets/index-WeLmJLk9.js         233.90 kB
```

- El nombre de cada archivo lleva una **huella del contenido**. Si el contenido cambia, el nombre cambia.
- `index.html` es el único archivo con nombre fijo, y apunta a los demás:

```html
<script type="module" crossorigin src="/assets/index-WeLmJLk9.js"></script>
<link rel="modulepreload" crossorigin href="/assets/jsx-runtime-Wy9s6hX0.js">
```

---

## Lo que la caché hace con tu JavaScript

Una respuesta HTTP puede traer `Cache-Control` para decir cuánto vive en la caché del navegador.

| Directiva | Significado |
|---|---|
| `max-age=N` | la respuesta es fresca N segundos desde que el origen la generó |
| `no-cache` | se puede guardar, pero hay que **revalidar con el servidor antes de cada uso** (no significa "no guardar") |
| `no-store` | no guardar |
| `immutable` | la respuesta no cambiará mientras sea fresca: ni siquiera se revalida al recargar |

Fuentes: MDN, "Cache-Control"; RFC 8246, "HTTP Immutable Responses" (IETF, septiembre de 2017).

<!-- Lo que sorprende: no-cache NO significa no guardar. La respuesta se guarda y se revalida antes de usarla. -->

---

## La receta: huella en el nombre, cabeceras por tipo

| Archivo | `Cache-Control` | Por qué |
|---|---|---|
| `/assets/*` (con huella) | `public, max-age=31536000, immutable` | su contenido **nunca** cambia en esa URL |
| `index.html` | `no-cache` | siempre se revalida, así que apunta a los assets nuevos |

> Puedes poner un `max-age` largo e `immutable` porque el contenido nunca cambiará. Con `no-cache` el HTML se revalida y el cliente recibe la versión nueva del HTML y de los assets.

(MDN, "Caching static assets with cache busting")

El servidor de ejemplo lo hace así. Salida real:

```text
$ curl -sI http://127.0.0.1:3120/
content-type: text/html; charset=utf-8
cache-control: no-cache
$ curl -sI http://127.0.0.1:3120/assets/index-WeLmJLk9.js
content-type: text/javascript; charset=utf-8
cache-control: public, max-age=31536000, immutable
```

---

## Revalidar: ETag y 304

`no-cache` no obliga a bajar el archivo otra vez: obliga a **preguntar**. Con nginx 1.31.8 (en un contenedor), salida real:

```text
$ curl -sI http://127.0.0.1:3125/
ETag: "6af4789d-236"
Last-Modified: Wed, 11 Nov 2026 14:15:57 GMT
Cache-Control: no-cache
$ curl -sI -H 'If-None-Match: "6af4789d-236"' http://127.0.0.1:3125/
HTTP/1.1 304 Not Modified
Cache-Control: no-cache
```

- Si el HTML no cambió, el servidor contesta **304 sin cuerpo**: la revalidación cuesta una petición pequeña.
- Si cambió, contesta 200 con el HTML nuevo, que apunta a los assets con huella nueva.

---

## Un asset sin huella: la trampa

```html
<script src="/app.js"></script>
```

El nombre nunca cambia. Si el servidor (o una red de entrega delante) manda `Cache-Control: public, max-age=14400` (4 horas):

1. El navegador baja `app.js` y lo guarda.
2. Despliegas una versión nueva. `app.js` ahora dice otra cosa, pero **tiene el mismo nombre**.
3. El navegador no pregunta: su copia sigue fresca hasta que pasen 4 horas.

El HTML nuevo llega, el JavaScript viejo se queda. **El arreglo existe solo en el servidor.**

---

## Verlo fallar en un Chromium real

El "despliegue" se simula con un script; el servidor de la demostración manda `max-age=14400` en todo lo que no es HTML.

```ts {7-16|*}
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
```

Salida real:

```text
desplegada la versión 1: /app.js
desplegada la versión 2: /app.js
VISTO sin huella: JavaScript versión 1
  ok  1 e2e/cache.spec.ts:7:1 › sin huella: el navegador sigue con el JavaScript viejo...
```

La prueba **pasa porque el error existe**: afirma que el navegador sigue con la versión 1 tras desplegar la 2.

---

## El arreglo: una URL nueva por contenido nuevo

```js {9-12|*}
// Simula un "despliegue" del sitio de demostración de caché: escribe demo-cache-dist/ con la versión pedida.
//   node scripts/desplegar-demo.mjs 1            (app.js sin huella: <script src="/app.js">)
//   node scripts/desplegar-demo.mjs 2 --huella   (app.js?v=<hash del contenido>)
import { createHash } from 'node:crypto';
import { mkdirSync, writeFileSync } from 'node:fs';

const version = process.argv[2] ?? '1';
const conHuella = process.argv.includes('--huella');
const js = `document.getElementById('version').textContent = 'JavaScript versión ${version}';\n`;
const huella = createHash('sha256').update(js).digest('hex').slice(0, 8);
const src = conHuella ? `/app.js?v=${huella}` : '/app.js';
const html = `<!doctype html>\n<html lang="es"><head><meta charset="utf-8"><title>Demo de caché</title></head>\n<body><p id="version">sin cargar</p><script src="${src}"></script></body></html>\n`;
mkdirSync('demo-cache-dist', { recursive: true });
writeFileSync('demo-cache-dist/app.js', js);
writeFileSync('demo-cache-dist/index.html', html);
console.log(`desplegada la versión ${version}: ${src}`);
```

La huella son los 8 primeros caracteres del SHA-256 del contenido. Salida real de la segunda prueba:

```text
desplegada la versión 1: /app.js?v=0f3a6a8b
desplegada la versión 2: /app.js?v=ed916442
VISTO con huella: JavaScript versión 2
```

- El HTML (que se revalida) ahora apunta a `/app.js?v=ed916442`: una URL **que la caché nunca vio**.
- Es lo que se hizo en la academia: una huella del contenido en `?v=`.

---

## Huella en el nombre o en `?v=`

| Opción | Ventaja | Límite |
|---|---|---|
| Nombre con huella (`app.3f9a2c.js`) | lo hace Vite solo; casi cualquier caché lo respeta | requiere un build que lo genere |
| Huella en `?v=` | se agrega sin renombrar archivos, a un sitio que ya existe | una caché configurada para ignorar la cadena de consulta no distingue versiones |

La regla de fondo es la misma: **la URL cambia cuando el contenido cambia**.

<!-- La segunda fila es la que hay que verificar en cada red de entrega. No afirmamos que Cloudflare ignore o no la cadena de consulta: depende de su configuración, y en el caso de la academia funcionó. -->

---

## Variables de entorno: se congelan en el build

- En Vite, lo que lleva el prefijo `VITE_` se **incrusta en el JavaScript** al compilar (nota `06-frontend/01-variables-entorno-vite.md`).
- Cambiar la variable en el servidor sin recompilar no cambia nada en el navegador.
- Todo lo que va en el bundle es público: nunca un secreto.

Para producción, el build debe correr **con las variables de producción** y el artefacto es el que se despliega.

---

## Error típico: caché mal puesta

| Error | Síntoma | Arreglo |
|---|---|---|
| `max-age` largo en `index.html` | el usuario no recibe versiones nuevas | `no-cache` en el HTML |
| Assets sin huella con `max-age` | el JavaScript viejo con el HTML nuevo | huella en el nombre o en `?v=` |
| Huella con `no-cache` en los assets | se revalida cada archivo sin necesidad | `immutable` y `max-age` largo |
| Borrar los assets viejos al desplegar | un usuario con el HTML viejo pide un archivo que ya no existe (404) | conservar los assets de la versión anterior un tiempo |

Libro: *React Application Architecture for Production* (Alickovic y Alicea, 2026), cap. 12.

<!-- La última fila es una consecuencia lógica del diseño (un HTML en caché apunta a nombres con huella viejos). No la medimos hoy: es un riesgo que se menciona, no una cifra. -->

---

<!-- _class: lead -->

# Parte 4
## Pipeline y contenedor

---

## El orden del pipeline

De lo más barato a lo más caro, para fallar rápido:

1. `npm ci` (instalación exacta del `package-lock.json`).
2. `tsc --noEmit`.
3. ESLint (con la regla de fronteras).
4. Pruebas de Vitest.
5. `vite build`.
6. Playwright (instala el navegador y corre las E2E).
7. Solo en `main`: construir la imagen y publicarla.

Un error de tipos cuesta segundos; una E2E, minutos. No se corre la E2E si el compilador ya falló.

Libro: *React Application Architecture for Production* (Alickovic y Alicea, 2026), cap. 12 (CI/CD con GitHub Actions).

---

## El workflow de GitHub Actions

```yaml {1-10|11-32|*}
name: front

on:
  pull_request:
  push:
    branches: [main]

permissions:
  contents: read

jobs:
  calidad:
    runs-on: ubuntu-24.04
    steps:
      - uses: actions/checkout@v7
      - uses: actions/setup-node@v7
        with:
          node-version: 22
          cache: npm
      - run: npm ci
      - run: npm run typecheck
      - run: npm run lint
      - run: npm test
      - run: npm run build
      - run: npx playwright install --with-deps chromium
      - run: npm run e2e
      - uses: actions/upload-artifact@v7
        if: ${{ !cancelled() }}
        with:
          name: playwright-report
          path: playwright-report/
          retention-days: 14

  imagen:
    needs: calidad
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-24.04
    permissions:
      contents: read
      packages: write
    steps:
      - uses: actions/checkout@v7
      - uses: docker/setup-buildx-action@v4
      - uses: docker/login-action@v4
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - id: meta
        uses: docker/metadata-action@v6
        with:
          images: ghcr.io/${{ github.repository }}
          tags: |
            type=sha
            type=raw,value=latest
      - uses: docker/build-push-action@v7
        with:
          context: .
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
```

Fragmento: **no se ejecutó en GitHub en el laboratorio de redacción**; el YAML se validó con un parser y los `uses` se comprobaron contra las etiquetas reales del repositorio de cada acción.

- Versiones a noviembre de 2026: `actions/checkout` v7, `actions/setup-node` v7 (corre sobre `node24`), `actions/upload-artifact` v7.
- `permissions: contents: read` por omisión: el job solo recibe lo que necesita.
- `if: ${{ !cancelled() }}`: el reporte de Playwright se sube también cuando las pruebas fallan.

Fuentes: playwright.dev, "Setting up CI"; `action.yml` y etiquetas de los repositorios `actions/*` en GitHub.

---

## Publicar la imagen: el job `imagen`

```yaml {1-6|7-17|*}
  imagen:
    needs: calidad
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-24.04
    permissions:
      contents: read
      packages: write
    steps:
      - uses: actions/checkout@v7
      - uses: docker/setup-buildx-action@v4
      - uses: docker/login-action@v4
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - id: meta
        uses: docker/metadata-action@v6
```

- `needs: calidad`: la imagen solo se construye si todo lo anterior pasó.
- `packages: write` es el permiso para publicar en el registro de contenedores de GitHub con el `GITHUB_TOKEN`.
- La documentación de GitHub recomienda fijar las acciones de terceros a un **SHA de commit** y no a una etiqueta móvil; aquí se usan etiquetas mayores por claridad de clase.

Fuente: docs.github.com, "Publishing Docker images". Versiones: `docker/setup-buildx-action` v4, `docker/login-action` v4, `docker/metadata-action` v6, `docker/build-push-action` v7. Fragmento.

---

## El contenedor: dos etapas

```dockerfile {1-7|9-11|*}
# Etapa 1: compilar. Etapa 2: servir solo dist/ con nginx (sin node, sin código fuente, sin node_modules).
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.31.8-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
```

- Etapa 1: Node compila. Etapa 2: nginx sirve **solo** `dist/`. La imagen final no lleva Node, ni el código fuente, ni `node_modules`.
- Fragmento: el `docker build` no se corrió en el laboratorio de redacción. Los comandos del `RUN` se verificaron por separado (`npm ci` y `npm run build`), y la configuración de nginx sí se probó (siguiente lámina).

---

## nginx: las cabeceras de la receta

```nginx {9-12|15-18|21-23|*}
server {
  listen 80;
  root /usr/share/nginx/html;
  index index.html;

  gzip on;
  gzip_types text/css text/javascript application/javascript application/json image/svg+xml;

  # Assets con huella en el nombre (Vite): el contenido de esa URL nunca cambia.
  location /assets/ {
    add_header Cache-Control "public, max-age=31536000, immutable";
    try_files $uri =404;
  }

  # Todo lo demás (el HTML y las rutas de la SPA): revalidar siempre.
  location / {
    add_header Cache-Control "no-cache";
    try_files $uri /index.html;
  }

  # Que la API vaya al backend (ajusta el destino a tu red).
  location /api/ {
    proxy_pass http://api:3100;
  }
}
```

- `/assets/` con `immutable`; todo lo demás `no-cache` y `try_files ... /index.html` para que las rutas de la SPA (`/entrar`) devuelvan el HTML.
- Probado con `nginx:1.31.8-alpine`: `nginx -t` dice "syntax is ok" y las cabeceras de las láminas anteriores salen de esta configuración.
- **Cuidado:** `add_header` dentro de un `location` **reemplaza** los `add_header` heredados del nivel superior. Si pones una cabecera de seguridad en `server`, repítela en cada `location` que defina las suyas.

---

## Verlo fallar: nginx sin su backend

Con la configuración tal cual, sin la red del backend, nginx **no arranca**:

```text
nginx: [emerg] host not found in upstream "api" in /etc/nginx/conf.d/default.conf:23
nginx: configuration file /etc/nginx/nginx.conf test failed
```

- `proxy_pass http://api:3100;` resuelve el nombre `api` al arrancar. Sin esa red, el contenedor se cae.
- En la prueba del laboratorio se resolvió con `--add-host api:127.0.0.1` solo para validar la sintaxis.
- En producción, el contenedor del front y el de la API comparten una red donde `api` existe.

---

<!-- _class: lead -->

# Parte 5
## Práctica

---

## Qué vamos a hacer

1. Correr la suite completa y mirar cada puerta.
2. Escribir pruebas de una feature, y **verlas fallar** por la razón correcta.
3. Ver una prueba vacua pasar, y arreglarla.
4. Correr la prueba E2E, romperla y leer la traza.
5. Reproducir el despliegue sin versión en los assets: un navegador que sigue con el JavaScript viejo.
6. Arreglarlo con una huella en `?v=`.
7. Escribir el pipeline del front y validar el contenedor.

La guía paso a paso está en `practica.md`.

---

## Proyecto final

Un **front completo contra una API real**:

- arquitectura por feature, con una regla de importación que falla en lint (sesión 2);
- formularios con errores del servidor (sesión 3);
- sesión segura y una CSP sin `unsafe-inline` (sesión 4);
- accesibilidad AA medida y reporte de rendimiento (sesión 5);
- pruebas de integración y una E2E, y despliegue en contenedor con caché correcta (esta sesión).

El detalle y los criterios de evaluación están en el README del curso.

---

## Cierre: lo que se llevan

| Pregunta | Respuesta de hoy |
|---|---|
| ¿Qué pruebo? | Lo que ve y hace el usuario; la frontera con el servidor; el contrato |
| ¿Cómo sé que mi prueba sirve? | La vi fallar por la razón correcta |
| ¿Cuándo una E2E? | En los flujos que no pueden romperse, y para lo que jsdom no ve |
| ¿Qué cachea el navegador? | Lo que le dijo `Cache-Control`; la huella cambia la URL |
| ¿Cómo salgo a producción? | Typecheck, lint, pruebas, build, E2E, imagen, en ese orden |

---

## Tarea

**El pipeline del front, con pruebas y despliegue en contenedor**, sobre una app propia o la Mesa de ayuda:

- Un workflow que corra tipos, lint, pruebas, build y E2E.
- Un `Dockerfile` de dos etapas y una configuración de nginx con las cabeceras de caché correctas.
- Evidencia de las cabeceras (`curl -I`) y de una prueba que viste fallar.

Detalle y criterios en `tarea.md`. Lecturas:

- `dev-notes/06-frontend/14-pruebas-front.md` y `15-cache-y-versionado-de-assets.md`.
- *React Application Architecture for Production* (Alickovic y Alicea, 2026), caps. 11 y 12.

---

## Libros de la sesión

- *React Application Architecture for Production* (Alickovic y Alicea, 2026), caps. 11 y 12.
- *React Anti-Patterns* (Qiu, 2023), caps. 5 a 7.
- *Modern Full-Stack React Projects* (Bugl, 2026), cap. 9.

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Quiz: pruebas y producción

- 10 preguntas por intento, sacadas de un banco de 12.
- Individual, sin el proyecto abierto: se contesta con lo que entendiste, no con lo que copias.
- Lo que falles, búscalo en las notas `14-pruebas-front` y `15-cache-y-versionado-de-assets` antes del siguiente intento.

<!-- Últimos 15 minutos. Es el último quiz del curso: revisa en voz alta la pregunta que más se falle y cierra con el proyecto final. -->
