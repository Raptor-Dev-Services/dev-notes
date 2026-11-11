# Práctica - S06 - Pruebas y producción

**React en producción con TypeScript**
**Duración:** 60 minutos, en parejas, con el proyecto Mesa de ayuda (Ejemplo ilustrativo).

---

## Objetivo

Escribir pruebas de una feature y **verlas fallar por la razón correcta**, correr y romper una prueba E2E de Playwright, reproducir un despliegue que deja al navegador con el JavaScript viejo y arreglarlo con una huella en la URL, y armar el pipeline y el contenedor del front.

---

## Antes de empezar

```bash
node -v                       # v22.22.1 o superior
cd mesa-de-ayuda
export npm_config_cache=$HOME/.cache/npm-a06
npm ci
npx playwright install chromium
```

Los archivos de esta práctica están completos en `referencia/mesa-de-ayuda/` del curso; aquí se muestran para que los leas antes de copiarlos. Versiones a noviembre de 2026: Vitest 5.1.0, Testing Library React 16.3.3, MSW 3.0.5, Playwright 1.65.0.

---

## Paso 1 - Las puertas, una por una

```bash
npx tsc --noEmit && echo TSC-OK
npm run lint
npm test
npx vite build
```

Salida de `npm test` en el laboratorio de redacción:

```text
 Test Files  4 passed (4)
      Tests  14 passed (14)
```

Anota cuántas pruebas tiene **tu** proyecto y cuánto tarda `npm test`.

---

## Paso 2 - Pruebas de una feature

Escribe dos archivos de prueba nuevos.

La sesión (feature `auth`), con un router en memoria:

```tsx
// src/features/auth/ruta-protegida.test.tsx
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

El contrato del cliente de la API:

```ts
// src/shared/api/client.test.ts
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

```bash
npm test
```

Deben pasar. Ahora lee cada prueba y contesta por escrito: **¿qué comportamiento del usuario protege?** Si no puedes decirlo, la prueba sobra.

---

## Paso 3 - Verlo fallar (cuatro maneras)

Una prueba que no has visto fallar no es una prueba. Haz estos cuatro experimentos y **deshaz cada cambio** antes del siguiente.

### 3a. Romper el código y que la prueba falle por la razón correcta

En `src/shared/ui/estado-de-datos.tsx`, cambia `if (vacio(consulta.data)) return` por `if (false && vacio(consulta.data)) return`.

```bash
npx vitest run -t 'estado vacío'
```

Salida real (abreviada):

```text
FAIL  src/features/tickets/tickets-page.test.tsx > TicketsPage > muestra el estado vacío
TestingLibraryElementError: Unable to find an element with the text: No hay tickets con ese filtro..
```

¿Falló **por la razón correcta**? Sí: no encuentra el texto del estado vacío.

### 3b. Quitar la limpieza

En `src/test/setup.ts`, borra la línea `cleanup();`. Corre `npx vitest run src/features/tickets`. En el laboratorio fallaron **4 pruebas** con:

```text
TestingLibraryElementError: Found multiple elements with the text: /La impresora no responde/
```

Explica por escrito por qué ocurre.

### 3c. Una prueba que pasa cuando no debe

Crea `src/test/vacia.test.tsx`:

```tsx
import { expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import { renderConProveedores } from './render';
import { TicketsPage } from '../features/tickets/tickets-page';

it('prueba vacua: el estado vacío no aparece', () => {
  renderConProveedores(<TicketsPage />);
  expect(screen.queryByText('No hay tickets con ese filtro.')).not.toBeInTheDocument();
});
```

Pasa (`Tests  1 passed (1)`). Ahora haz el experimento 3a **otra vez** y corre solo esta prueba: **sigue pasando**. Arréglala: antes de la aserción negativa, espera algo que sí debe aparecer (`await screen.findByText(/La impresora/)`). Después bórrala.

### 3d. Una petición sin manejador

Crea `src/test/sinmanejador.test.tsx` con una prueba que llame a `pedir('/ruta-sin-manejador', { esquema: paginaDeTicketsSchema })`. Salida real:

```text
[MSW] Error: intercepted a request without a matching request handler:
  • GET /api/ruta-sin-manejador
...
Caused by: InternalError: [MSW] Cannot bypass a request when using the "error" strategy for the "onUnhandledFrame" option.
```

Explica por qué conviene que `setup.ts` use `onUnhandledFrame: 'error'`. Bórrala.

---

## Paso 4 - La prueba E2E

Configuración y la prueba que ya trae el proyecto:

```ts
// playwright.config.ts
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

```ts
// e2e/tickets.spec.ts
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

```bash
npx vite build
PORT=3120 npx playwright test tickets
```

Deben pasar las dos pruebas.

**Verlo fallar:** en `src/shared/i18n/recursos.ts` (o en el botón de `login-page.tsx` si todavía no tienes idiomas) cambia el texto del botón de "Entrar" a "Acceder", compila (`npx vite build`) y corre `PORT=3120 npx playwright test tickets -g 'crear un ticket'`. Salida real:

```text
Test timeout of 30000ms exceeded.
Error: locator.click: Test timeout of 30000ms exceeded.
Call log:
  - waiting for getByRole('button', { name: 'Entrar' })
...
    test-results/tickets-entrar-crear-un-ticket-y-verlo-en-la-lista/trace.zip
```

Cuestiones:

1. ¿Cuánto tardó en fallar? (Anota el tiempo: es el costo de una prueba rota en CI.)
2. Abre la traza: `npx playwright show-trace test-results/<carpeta>/trace.zip`. ¿Qué viste en el paso donde falló? Descríbelo.
3. Deshaz el cambio y `rm -rf test-results`.

---

## Paso 5 - Lo que el build produce y las cabeceras

```bash
npx vite build
PORT=3120 node server/server.mjs &
echo $! > /tmp/servidor.pid
curl -sI http://127.0.0.1:3120/ | grep -i -E 'HTTP|cache|content-type'
ls dist/assets
curl -sI http://127.0.0.1:3120/assets/$(ls dist/assets | grep '^index-' | head -1) | grep -i -E 'HTTP|cache|content-type'
kill $(cat /tmp/servidor.pid)
```

Salida real:

```text
content-type: text/html; charset=utf-8
cache-control: no-cache
content-type: text/javascript; charset=utf-8
cache-control: public, max-age=31536000, immutable
```

Contesta: ¿por qué el HTML lleva `no-cache` y el asset `max-age=31536000, immutable`? ¿Qué pasaría si el HTML llevara `immutable`?

---

## Paso 6 - Verlo fallar: el navegador con el JavaScript viejo

El "despliegue" lo simula este script, que escribe `demo-cache-dist/` con la versión que le pidas:

```js
// scripts/desplegar-demo.mjs
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

Y esta prueba, que corre contra su propio servidor (el que manda `max-age=14400`, cuatro horas, en todo lo que no es HTML):

```ts
// e2e/cache.spec.ts
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

```ts
// playwright.cache.config.ts
import { defineConfig } from '@playwright/test';

const puerto = process.env.PORT ?? '3100';

// Servidor aparte, con la caché "de red de entrega" de 4 horas para todo lo que no es HTML.
export default defineConfig({
  testDir: './e2e',
  testMatch: 'cache.spec.ts',
  workers: 1,
  fullyParallel: false,
  use: { baseURL: `http://127.0.0.1:${puerto}` },
  webServer: {
    command: `DIST_DIR=demo-cache-dist CACHE_ASSETS='public, max-age=14400' node server/server.mjs`,
    url: `http://127.0.0.1:${puerto}/api/me`,
    reuseExistingServer: false,
  },
});
```

Agrega al servidor de ejemplo la variable `DIST_DIR` (carpeta a servir) si tu copia no la trae: `const DIST = process.env.DIST_DIR ?? join(RAIZ, 'dist');`. La configuración del paso 4 ya excluye `cache.spec.ts` con `testIgnore`, para que no corra con las demás. Agrega `demo-cache-dist` a `.gitignore`.

```bash
PORT=3120 npx playwright test --config=playwright.cache.config.ts -g 'sin huella'
```

Salida real:

```text
desplegada la versión 1: /app.js
desplegada la versión 2: /app.js
VISTO sin huella: JavaScript versión 1
  ✓  1 e2e/cache.spec.ts:7:1 › sin huella: el navegador sigue con el JavaScript viejo hasta que vence su caché
```

Desplegaste la versión 2 y el navegador siguió corriendo la 1. **La prueba pasa porque el error existe.** Escribe en tu reporte, con tus palabras, qué cabecera lo causó y por qué el HTML nuevo no bastó.

---

## Paso 7 - El arreglo: una huella en la URL

```bash
PORT=3120 npx playwright test --config=playwright.cache.config.ts -g 'con huella'
```

```text
desplegada la versión 1: /app.js?v=0f3a6a8b
desplegada la versión 2: /app.js?v=ed916442
VISTO con huella: JavaScript versión 2
```

(Las huellas son los 8 primeros caracteres del SHA-256 del contenido: las tuyas pueden coincidir o no según el texto exacto que tenga tu `app.js`.)

Contesta: ¿qué cambió entre las dos pruebas, el JavaScript o su URL? ¿Por qué el HTML sí llegó fresco en las dos? (Pista: `Cache-Control` del HTML.)

Y compara con lo que Vite hace por ti: nombres como `index-WeLmJLk9.js`. ¿Qué ventaja tiene poner la huella en el nombre frente a ponerla en `?v=`?

---

## Paso 8 - nginx con las cabeceras (necesita Docker)

Los archivos:

```nginx
// nginx.conf
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

Valida y sirve `dist/` **con un solo contenedor**, y bórralo al terminar:

```bash
docker run --rm --add-host api:127.0.0.1 -v "$PWD/nginx.conf:/etc/nginx/conf.d/default.conf:ro" nginx:1.31.8-alpine nginx -t
docker run -d --rm --name nginx-front --add-host api:127.0.0.1 -p 127.0.0.1:3125:80 \
  -v "$PWD/nginx.conf:/etc/nginx/conf.d/default.conf:ro" -v "$PWD/dist:/usr/share/nginx/html:ro" nginx:1.31.8-alpine
curl -sI http://127.0.0.1:3125/ | grep -i -E 'HTTP|cache|etag'
E=$(curl -sI http://127.0.0.1:3125/ | grep -i '^etag' | tr -d '\r' | cut -d' ' -f2)
curl -sI -H "If-None-Match: $E" http://127.0.0.1:3125/ | grep -i -E 'HTTP|cache'
docker stop nginx-front
```

Salida real del laboratorio (el ETag y la fecha serán otros):

```text
nginx: configuration file /etc/nginx/nginx.conf test is successful
ETag: "6af4789d-236"
Cache-Control: no-cache
HTTP/1.1 304 Not Modified
Cache-Control: no-cache
```

**Verlo fallar:** corre el primer comando **sin** `--add-host api:127.0.0.1`:

```text
nginx: [emerg] host not found in upstream "api" in /etc/nginx/conf.d/default.conf:23
nginx: configuration file /etc/nginx/nginx.conf test failed
```

Explica por qué falla y cómo lo resolverías en un despliegue real. Si no tienes Docker, marca este paso como "no ejecutado" y explica qué esperas ver.

---

## Paso 9 - El pipeline y la imagen

Escribe `.github/workflows/front.yml`:

```yaml
// .github/workflows/front.yml
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

Valida que el YAML se pueda leer (esto **no** corre el workflow):

```bash
python3 -c "import yaml; d=yaml.safe_load(open('.github/workflows/front.yml')); print(list(d['jobs']))"
# ['calidad', 'imagen']
```

> PyYAML lee la clave `on` como `True` (así lo hace YAML 1.1); GitHub la lee bien. No lo "corrijas".

Y el `Dockerfile` de dos etapas:

```dockerfile
// Dockerfile
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

En un `.dockerignore` deja fuera `node_modules`, `dist`, `test-results`, `playwright-report` y `.git`. **Fragmento:** el `docker build` y el workflow no se ejecutaron en el laboratorio de redacción; si tu equipo tiene Docker y un repositorio en GitHub, corre ambos y pega el resultado.

---

## Entregable

Un archivo `evidencia-s06.md` con:

1. La salida de `npm test` y de las E2E.
2. Para cada experimento "verlo fallar" (3a, 3b, 3c, 3d, 4, 6, 8): la salida **real** que obtuviste y una frase con la razón.
3. Las cabeceras del paso 5 y las respuestas de los pasos 5 y 7.
4. El workflow y el `Dockerfile`, con una nota de lo que ejecutaste y lo que no.

---

## Limpieza

```bash
kill $(cat /tmp/servidor.pid) 2>/dev/null
docker ps --format '{{.Names}}' | grep nginx-front || echo "sin contenedor"
rm -rf test-results playwright-report demo-cache-dist dist-compiler
```

No dejes servidores ni contenedores corriendo.
