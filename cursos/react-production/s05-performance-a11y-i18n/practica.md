# Práctica - S05 - Rendimiento, accesibilidad e idiomas

**React en producción con TypeScript**
**Duración:** 75 minutos, en parejas, con el proyecto Mesa de ayuda (Ejemplo ilustrativo).

---

## Objetivo

Medir una app, arreglar **tres problemas** (el JavaScript de la primera pantalla, un render que sobra y el foco al cambiar de ruta), ver fallar la accesibilidad a propósito y volver a medir. Se entrega una tabla antes y después **con números**.

Los tres problemas:

1. El JavaScript que baja la pantalla de entrada (línea base: 478.12 kB, un archivo).
2. Una lista de 5 000 filas que tarda segundos en responder cuando escribes.
3. Al entrar, el foco se queda en `<body>` y el título nunca cambia.

---

## Antes de empezar

```bash
node -v                       # v22.22.1 o superior
cd mesa-de-ayuda
export npm_config_cache=$HOME/.cache/npm-a05
npm ci
npm i @tanstack/react-virtual@3.14.26 i18next@27.1.1 react-i18next@18.0.1
npm i -D @axe-core/playwright@4.14.0
npx playwright install chromium
```

Los archivos de esta práctica están completos en `referencia/mesa-de-ayuda/` de este curso; aquí se muestran para que los leas antes de copiarlos. El proyecto parte del estado en que dejaste la sesión 4, con `playwright.config.ts` leyendo `PORT` y `locale: 'es-MX'` (el paso 8 explica por qué).

> **Cómo medir:** una variable a la vez, el mismo comando antes y después, **tres corridas** y se anota el rango. Cierra lo que tengas abierto y no midas con `vite dev`.

---

## Paso 1 - Línea base del bundle

```bash
npx vite build
```

Anota el tamaño del archivo `dist/assets/index-*.js`. Con el proyecto de la sesión 4 salió así (Vite 8.3.8):

```text
dist/assets/index-Cz5em8n-.js  478.11 kB │ gzip: 147.91 kB
```

Ahora mide lo que baja **un navegador real** al abrir la pantalla de entrada. Crea la prueba:

```ts
// e2e/peso.spec.ts
import { test } from '@playwright/test';

// Suma el JavaScript que el navegador descarga al abrir /entrar (tamaños sin comprimir: este servidor de ejemplo no comprime).
test('JavaScript descargado al abrir /entrar', async ({ page }) => {
  await page.goto('/entrar');
  await page.getByRole('heading', { name: 'Entrar' }).waitFor();
  const archivos = await page.evaluate(() =>
    performance
      .getEntriesByType('resource')
      .filter((r) => new URL(r.name).pathname.endsWith('.js'))
      .map((r) => ({ nombre: new URL(r.name).pathname, bytes: (r as PerformanceResourceTiming).decodedBodySize })),
  );
  const total = archivos.reduce((s, a) => s + a.bytes, 0);
  console.log(`MEDIDA js_en_entrar archivos=${archivos.length} total_kB=${(total / 1000).toFixed(2)}`);
});
```

```bash
PORT=3120 npx playwright test peso
# MEDIDA js_en_entrar archivos=1 total_kB=478.12
```

Anota: **478.12 kB, 1 archivo** (los números exactos cambian con tus dependencias; lo que importa es tu línea base).

---

## Paso 2 - ¿Qué pesa?

Crea el script del paquete:

```js
// scripts/peso-por-paquete.mjs
// Peso aproximado por paquete de todo el JavaScript de dist/assets, a partir de sus sourcemaps.
//   npx vite build --sourcemap && node scripts/peso-por-paquete.mjs
// Es una estimación (reparte los bytes del archivo entre las fuentes que dicen los mapeos), útil para comparar, no para citar al byte.
import { readdirSync, readFileSync } from 'node:fs';

const B64 = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
function vlq(texto) {
  const salida = [];
  let desplazamiento = 0;
  let valor = 0;
  for (const c of texto) {
    const d = B64.indexOf(c);
    valor += (d & 31) << desplazamiento;
    if (d & 32) desplazamiento += 5;
    else {
      salida.push(valor & 1 ? -(valor >> 1) : valor >> 1);
      valor = desplazamiento = 0;
    }
  }
  return salida;
}

const archivos = readdirSync('dist/assets').filter((f) => f.endsWith('.js'));
if (archivos.length === 0) throw new Error('No hay dist/assets/*.js: corre npx vite build --sourcemap');
const porFuente = new Map();
for (const archivo of archivos) {
  const mapa = JSON.parse(readFileSync(`dist/assets/${archivo}.map`, 'utf8'));
  const lineas = readFileSync(`dist/assets/${archivo}`, 'utf8').split('\n');
  let fuente = 0;
  mapa.mappings.split(';').forEach((linea, n) => {
    if (!linea) return;
    let columna = 0;
    const trozos = [];
    for (const seg of linea.split(',')) {
      const v = vlq(seg);
      columna += v[0];
      if (v.length > 1) {
        fuente += v[1];
        trozos.push([columna, fuente]);
      }
    }
    const largo = lineas[n]?.length ?? 0;
    trozos.forEach(([col, f], i) => {
      const fin = trozos[i + 1]?.[0] ?? largo;
      const nombre = mapa.sources[f];
      porFuente.set(nombre, (porFuente.get(nombre) ?? 0) + fin - col);
    });
  });
}
const porPaquete = new Map();
for (const [nombre, bytes] of porFuente) {
  const m = [...nombre.matchAll(/node_modules\/((?:@[^/]+\/)?[^/]+)/g)].at(-1);
  const clave = m ? m[1] : 'src (código propio)';
  porPaquete.set(clave, (porPaquete.get(clave) ?? 0) + bytes);
}
for (const [paquete, bytes] of [...porPaquete].sort((a, b) => b[1] - a[1]).slice(0, 10)) {
  console.log(`${paquete.padEnd(32)}${(bytes / 1024).toFixed(1).padStart(8)} kB`);
}
```

```bash
npx vite build --sourcemap
node scripts/peso-por-paquete.mjs
```

En el proyecto del laboratorio de redacción, medido sobre el chunk principal **ya dividido por rutas** (paso 5), salió (aproximado, por sourcemap): `react-dom` 202.2 kB, `react-router` 92.2 kB, `zod` 81.4 kB, `@tanstack/query-core` 32.1 kB y **código propio 4.6 kB**.

**Pregunta para la pareja:** con estos números, ¿qué conviene recortar primero: tu código o una dependencia? Anota la respuesta.

> Borra los `.map` antes de servir producción: `rm dist/assets/*.map` o vuelve a correr `npx vite build` sin `--sourcemap`.

---

## Paso 3 - Lighthouse sobre la pantalla de entrada

```bash
PORT=3120 node server/server.mjs &
echo $! > /tmp/servidor.pid
npx --yes lighthouse@13.6.0 http://127.0.0.1:3120/entrar \
  --only-categories=performance,accessibility \
  --output=json --output-path=./reporte-antes.json --quiet \
  --chrome-flags="--headless"
kill $(cat /tmp/servidor.pid)
```

Lee del JSON `categories.performance.score`, `categories.accessibility.score` y las auditorías `first-contentful-paint`, `largest-contentful-paint`, `total-blocking-time`, `cumulative-layout-shift` y `unused-javascript`. Es **laboratorio con red simulada**, no el percentil 75 de usuarios reales.

> Si `lighthouse` no encuentra Chrome en tu máquina, instala Chrome o apunta `CHROME_PATH` a un Chromium. En el laboratorio de redacción se corrió con el `chrome-headless-shell` de Playwright y `--chrome-flags="--headless --no-sandbox"`.

---

## Paso 4 - La pantalla de volumen (el problema 2)

Agrega los datos sintéticos (Ejemplo ilustrativo) y la pantalla:

```ts
// src/features/volumen/datos.ts
import type { Ticket } from '../../shared/api/contract';

const TEMAS = ['impresora', 'correo', 'acceso', 'monitor', 'red', 'teléfono', 'licencia', 'contraseña'];
const PRIORIDADES = ['baja', 'media', 'alta'] as const;

/** Datos sintéticos y deterministas (Ejemplo ilustrativo): n tickets para probar listas grandes. */
export function ticketsSinteticos(n: number): Ticket[] {
  return Array.from({ length: n }, (_, i) => ({
    id: i + 1,
    titulo: `Incidencia ${i + 1}: ${TEMAS[i % TEMAS.length]} del piso ${(i % 7) + 1}`,
    prioridad: PRIORIDADES[i % 3] ?? 'baja',
    estado: i % 5 === 0 ? 'cerrado' : 'abierto',
    creadoEn: new Date(Date.UTC(2026, 10, 1 + (i % 28), i % 24)).toISOString(),
  }));
}
```

```tsx
// src/features/volumen/volumen-page.tsx
import { memo, useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router';
import { useVirtualizer } from '@tanstack/react-virtual';
import type { Ticket } from '../../shared/api/contract';
import { ticketsSinteticos } from './datos';

type Modo = 'lenta' | 'memo' | 'virtual';

function FilaLenta({ ticket }: { ticket: Ticket }) {
  // Crear un Intl.DateTimeFormat en cada render es caro a propósito: es el trabajo que se repite.
  const fecha = new Intl.DateTimeFormat('es-MX', { dateStyle: 'long', timeStyle: 'short', timeZone: 'UTC' }).format(new Date(ticket.creadoEn));
  return (
    <li>
      {ticket.titulo} - {ticket.prioridad} - {fecha}
    </li>
  );
}
const FilaMemo = memo(FilaLenta);

export function VolumenPage() {
  const [params] = useSearchParams();
  const modo: Modo = params.get('modo') === 'memo' ? 'memo' : params.get('modo') === 'virtual' ? 'virtual' : 'lenta';
  const [texto, setTexto] = useState('');
  const todos = useMemo(() => ticketsSinteticos(5000), []);
  const visibles = useMemo(() => todos.filter((t) => t.titulo.toLowerCase().includes(texto.toLowerCase())), [todos, texto]);
  const contenedor = useRef<HTMLDivElement>(null);
  const virtualizador = useVirtualizer({ count: visibles.length, getScrollElement: () => contenedor.current, estimateSize: () => 24, overscan: 8 });

  return (
    <main>
      <h1>Volumen ({modo})</h1>
      <label htmlFor="buscar">Buscar</label>
      <input id="buscar" value={texto} onChange={(e) => setTexto(e.target.value)} />
      <p role="status">{visibles.length} resultados</p>
      {modo === 'virtual' ? (
        <div ref={contenedor} style={{ height: 480, overflow: 'auto' }} tabIndex={0} aria-label="Resultados">
          <ul style={{ height: virtualizador.getTotalSize(), position: 'relative', margin: 0, padding: 0, listStyle: 'none' }}>
            {virtualizador.getVirtualItems().map((v) => {
              const t = visibles[v.index];
              if (!t) return null;
              return (
                <li key={t.id} style={{ position: 'absolute', top: 0, left: 0, height: v.size, transform: `translateY(${v.start}px)` }}>
                  {t.titulo} - {t.prioridad}
                </li>
              );
            })}
          </ul>
        </div>
      ) : (
        <ul>
          {visibles.map((t) => (modo === 'memo' ? <FilaMemo key={t.id} ticket={t} /> : <FilaLenta key={t.id} ticket={t} />))}
        </ul>
      )}
    </main>
  );
}
```

Registra la ruta (dentro de las rutas protegidas) en `src/app/router.tsx`:

```tsx
// eslint-disable-next-line boundaries/dependencies -- excepción de la sesión 5: lazy apunta al archivo de la pantalla
{ path: '/volumen', lazy: async () => ({ Component: (await import('../features/volumen/volumen-page')).VolumenPage }) },
```

El comentario es necesario: la regla de fronteras de la sesión 2 prohíbe importar por dentro de una feature, y `lazy` necesita apuntar al archivo de la pantalla para que Vite la separe. Una excepción lleva siempre su motivo escrito.

Y la prueba que mide, con Event Timing, qué tan lenta responde cada modo:

```ts
// e2e/ayudas.ts
import type { Page } from '@playwright/test';

export async function entrar(page: Page) {
  await page.goto('/entrar');
  await page.getByLabel('Correo').fill('ana@example.com');
  await page.getByLabel('Contraseña').fill('clave-de-ejemplo');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await page.getByRole('heading', { name: 'Tickets' }).waitFor();
}
```

```ts
// e2e/rendimiento.spec.ts
import { expect, test } from '@playwright/test';
import { entrar } from './ayudas';

// Mide en un Chromium real cuánto tarda el navegador en procesar lo que escribes (Event Timing API)
// y cuántos nodos de lista hay en el DOM. Los números dependen de la máquina: compara modos entre sí, no contra un valor fijo.
for (const modo of ['lenta', 'memo', 'virtual'] as const) {
  test(`volumen: modo ${modo}`, async ({ page }) => {
    await entrar(page);
    await page.goto(`/volumen?modo=${modo}`);
    await expect(page.getByRole('status')).toHaveText('5000 resultados');
    await page.evaluate(() => {
      const w = window as unknown as { __eventos: number[] };
      w.__eventos = [];
      new PerformanceObserver((lista) => {
        for (const e of lista.getEntries()) w.__eventos.push(e.duration);
      }).observe({ type: 'event', durationThreshold: 16 });
    });
    const t0 = Date.now();
    await page.getByLabel('Buscar').pressSequentially('incidencia 1', { delay: 0 });
    await expect(page.getByRole('status')).not.toHaveText('5000 resultados');
    await page.evaluate(() => new Promise((r) => requestAnimationFrame(() => requestAnimationFrame(r))));
    const total = Date.now() - t0;
    const eventos = await page.evaluate(() => (window as unknown as { __eventos: number[] }).__eventos);
    const nodos = await page.locator('main li').count();
    console.log(`MEDIDA modo=${modo} nodos_li=${nodos} total_ms=${total} eventos_lentos=${eventos.length} peor_evento_ms=${Math.max(0, ...eventos)}`);
    if (modo === 'virtual') expect(nodos).toBeLessThan(100);
  });
}
```

```bash
PORT=3120 npx playwright test rendimiento --workers=1
```

Salida de una corrida del laboratorio de redacción (Chromium 158, servidor cargado; los tuyos serán distintos):

```text
MEDIDA modo=lenta nodos_li=1111 total_ms=6072 eventos_lentos=55 peor_evento_ms=640
MEDIDA modo=memo nodos_li=1111 total_ms=410 eventos_lentos=54 peor_evento_ms=80
MEDIDA modo=virtual nodos_li=28 total_ms=210 eventos_lentos=57 peor_evento_ms=32
```

Corre tres veces y anota el rango de `total_ms` y `peor_evento_ms` de cada modo. La columna `eventos_lentos` no importa: cuenta eventos de 16 ms o más, que incluyen las teclas, los clics y el resto.

**Verlo fallar (la prueba del umbral):** el umbral de INP "bueno" es 200 ms. Agrega al final de la prueba `expect(Math.max(0, ...eventos)).toBeLessThan(200)` y corre otra vez. En el modo `lenta` falla. Quítalo después: es un experimento, porque un tiempo de una máquina no debe ser una aserción permanente.

---

## Paso 5 - Arreglo 1: el JavaScript de la primera pantalla

Divide las rutas con `lazy` (si ya lo hiciste en el paso 4 para `/volumen`, falta `/` y `/entrar`):

```tsx
export const rutas: RouteObject[] = [
  // eslint-disable-next-line boundaries/dependencies -- excepción de la sesión 5: lazy apunta al archivo de la pantalla
  { path: '/entrar', lazy: async () => ({ Component: (await import('../features/auth/login-page')).LoginPage }) },
  {
    element: <RutaProtegida />,
    children: [{ path: '/', lazy: async () => ({ Component: (await import('../features/tickets')).TicketsPage }) }],
  },
];
```

```bash
npx vite build
PORT=3120 npx playwright test peso
```

Resultado con el proyecto de la sesión 4 (sin idiomas ni volumen todavía):

```text
dist/assets/tickets-DjSO1ijj.js   41.03 kB │ gzip: 14.52 kB
dist/assets/index-Bz1005Dq.js    438.91 kB │ gzip: 134.66 kB
MEDIDA js_en_entrar archivos=1 total_kB=438.91
```

Baja 39.2 kB (8.2 %). El build además avisa:

```text
[INEFFECTIVE_DYNAMIC_IMPORT] src/features/auth/login-page.tsx is dynamically imported by
src/app/router.tsx but also statically imported by src/features/auth/index.ts, dynamic import
will not move module into another chunk.
```

**Pregunta:** ¿por qué la pantalla de entrada no se separó? (Pista: lee el aviso.) Anótala.

---

## Paso 6 - Arreglo 2: el render que sobra

Ya está en el código del paso 4: los tres modos viven en la misma pantalla. Compara con `/volumen?modo=lenta`, `?modo=memo` y `?modo=virtual` abiertas a mano en el navegador y escribe `incidencia 1` en el buscador. Después decide **cuál se queda** en la app y por qué (usa tus rangos del paso 4):

| Modo | Total | Peor evento | Nodos | Complejidad añadida |
|---|---|---|---|---|
| lenta | | | | ninguna |
| memo | | | | una línea |
| virtual | | | | una dependencia y un contenedor con scroll |

Opcional (React Compiler, solo si te sobra tiempo):

```bash
npm i -D babel-plugin-react-compiler@1.0.0 @rolldown/plugin-babel
```

```ts
// vite.compiler.config.ts
import { defineConfig } from 'vitest/config';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import babel from '@rolldown/plugin-babel';

// Igual que vite.config.ts, pero con React Compiler (doc: react.dev/learn/react-compiler/installation).
export default defineConfig({
  plugins: [react(), babel({ presets: [reactCompilerPreset()] })],
  build: { outDir: 'dist-compiler' },
});
```

```bash
npx vite build -c vite.compiler.config.ts
DIST_DIR=dist-compiler PORT=3120 npx playwright test rendimiento -g lenta --workers=1
```

En el laboratorio de redacción, la versión `lenta` con el Compiler dio **0.39 a 0.74 s** y un peor evento de **56 a 128 ms** (dos corridas), contra 4.6 a 8.2 s y 456 a 1 408 ms sin Compiler (siete corridas).

---

## Paso 7 - Arreglo 3: foco y título (primero verlo fallar)

Escribe **primero la prueba**, con el teclado y sin ratón:

```ts
// e2e/teclado.spec.ts
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
```

```bash
PORT=3120 npx playwright test teclado
```

Falla, y eso es lo correcto. Salida real:

```text
Error: expect(locator).toBeFocused() failed
Locator:  getByRole('heading', { name: 'Tickets' })
Expected: focused
Received: inactive
```

Ahora amplía el `Layout` de la sesión 2 (el que compone la cabecera de `perfil`) para que mueva el foco y ponga el título:

```tsx
// src/app/layout.tsx
import { useEffect, useRef } from 'react';
import { Outlet, useLocation } from 'react-router';
import { useSalir, useSesion } from '../features/auth';
import { Cabecera } from '../features/perfil';

/**
 * Composición entre features (sesión 2): la cabecera de perfil recibe lo que necesita por props, ninguna feature importa a la otra.
 * Accesibilidad (sesión 5): en una SPA el navegador no recarga la página, así que no anuncia el cambio de pantalla ni mueve el foco.
 * Al cambiar de ruta: el título del documento sigue al h1 (WCAG 2.4.2) y el foco pasa al h1 (WCAG 2.4.3).
 */
export function Layout() {
  const { pathname } = useLocation();
  const sesion = useSesion();
  const salir = useSalir();
  const primera = useRef(true);
  useEffect(() => {
    const h1 = document.querySelector('h1');
    if (!h1) return;
    document.title = `${h1.textContent} - Mesa de ayuda`;
    if (primera.current) {
      primera.current = false;
      return;
    }
    h1.tabIndex = -1;
    h1.focus();
  }, [pathname]);
  return (
    <>
      {sesion.data && <Cabecera nombre={sesion.data.nombre} alSalir={() => salir.mutate()} />}
      <Outlet />
    </>
  );
}
```

Úsalo como ruta padre en `router.tsx`:

```tsx
export const rutas: RouteObject[] = [
  {
    element: <Layout />,
    children: [
      // ...las rutas de antes
    ],
  },
];
```

```bash
npx vite build && PORT=3120 npx playwright test teclado
```

Debe pasar.

> **Trampa que apareció al correr:** Playwright usa `en-US` por omisión. Si tu app ya tiene idiomas (paso 9), agrega `locale: 'es-MX'` en `use` de `playwright.config.ts`, o esta prueba no encontrará el encabezado "Entrar".

---

## Paso 8 - Verlo fallar: axe sobre una pantalla rota

Prueba de axe:

```ts
// e2e/accesibilidad.spec.ts
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
```

```bash
PORT=3120 npx playwright test accesibilidad
```

Pasa en la versión buena. Ahora **rompe** `login-page.tsx` a propósito: borra la `<label htmlFor="email">`, agrega `<img src="/logo.svg" />` sin `alt` y dale al botón `style={{ color: '#aaa', background: '#fff' }}`. Corre `npx vite build` y la prueba otra vez:

```text
- Expected  - 1
+ Received  + 5

- Array []
+ Array [
+   "color-contrast: 1",
+   "image-alt: 1",
+   "label: 1",
+ ]
```

Con Lighthouse sobre la pantalla rota, la categoría de accesibilidad dio **0.68** (contra 1 en la buena), con `color-contrast`, `image-alt` y `label` en cero.

Anota qué criterio de WCAG 2.2 corresponde a cada una (1.4.3, 1.1.1 y 3.3.2 o 1.3.1) y **deshaz el cambio**.

---

## Paso 9 - Recorrer la app solo con teclado

Apaga el ratón (o no lo toques) y recorre la app **en el navegador, a mano**. Para cada pantalla (entrada, tickets, volumen) anota sí o no:

| Pregunta | Entrada | Tickets | Volumen |
|---|---|---|---|
| ¿Veo siempre dónde está el foco? (2.4.7) | | | |
| ¿Llego a todo con `Tab` y en orden lógico? (2.4.3) | | | |
| ¿Puedo entrar y salir de todo sin ratón? (2.1.1, 2.1.2) | | | |
| ¿Tras enviar el formulario, el mensaje de error se anuncia sin mover el foco? (4.1.3) | | | |
| ¿Cada campo tiene una etiqueta visible? (3.3.2) | | | |
| ¿El título de la pestaña describe la pantalla? (2.4.2) | | | |

En el modo `virtual` revisa además: ¿puedes desplazar la lista con el teclado? El contenedor lleva `tabIndex={0}` para eso.

Cada "no" es un hallazgo: anótalo con la pantalla y el criterio.

---

## Paso 10 - Idiomas (extensión)

Si sobra tiempo: agrega i18n a **la pantalla de entrada** con `i18next` y `react-i18next`.

1. Crea `src/shared/i18n/recursos.ts` con las claves de `login` y `comun` en español e inglés (ver las láminas de la parte 4), y `src/shared/i18n/index.ts` con `i18n.init({ lng, fallbackLng: 'es', resources })` y el `declare module 'i18next'` de las claves tipadas.
2. Cambia los textos de `login-page.tsx` por `t('login.titulo')`, etc.
3. **Verlo fallar:** escribe `t('login.titulos')`. Corre `npx tsc --noEmit`:

```text
src/features/auth/login-page.tsx(20,14): error TS2345: Argument of type '["login.titulos"]' is not
assignable to parameter of type ...
    Did you mean '"login.titulo"'?
```

4. Borra una clave del inglés y vuelve a correr `tsc`: debe decir `Property '...' is missing`.
5. Corre `npm test`: si tus pruebas buscan texto en español y jsdom declara `en-US`, fallan. Arréglalo con `await i18n.changeLanguage('es')` en un `beforeEach` del `setup`.
6. Mide otra vez el JavaScript de `/entrar`. En el laboratorio de redacción, con idiomas en todas las pantallas, la pantalla de volumen y el selector, pasó de 438.91 kB a **496.81 kB**; `i18next` aportó ~47.4 kB. Anota cuánto sumó **tu** versión.

---

## Paso 11 - Volver a medir y la tabla

Llena con tus números (rango de tres corridas donde aplique):

| Qué | Antes | Después | Cómo se midió |
|---|---|---|---|
| JavaScript al abrir `/entrar` | | | `peso.spec.ts` |
| Tiempo de teclear 12 teclas en `/volumen` | | | `rendimiento.spec.ts` |
| Peor evento (Event Timing) | | | `rendimiento.spec.ts` |
| Accessibility de Lighthouse | | | Lighthouse 13.6.0 |
| Violaciones de axe | | | `accesibilidad.spec.ts` |
| Foco tras entrar | `body` | `h1` | `teclado.spec.ts` |

---

## Entregable

Un archivo `reporte-s05.md` con:

1. La tabla del paso 11, con **los comandos exactos** y la versión de Node, Chromium y Lighthouse.
2. Las respuestas a las preguntas de los pasos 2, 5 y 6.
3. La lista de hallazgos del recorrido con teclado (paso 9), con criterio de WCAG 2.2.
4. Un párrafo: ¿qué optimización **no** valió la pena y por qué?

---

## Limpieza

```bash
kill $(cat /tmp/servidor.pid) 2>/dev/null
rm -rf dist-compiler test-results playwright-report reporte-antes.json
```

No dejes servidores corriendo.
