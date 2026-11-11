<!-- _class: lead -->

# S05 - Rendimiento, accesibilidad e idiomas

### React en producción con TypeScript

**Rogelio Arriaga - Raptor Dev Services**

<!--
Bienvenida. Antes de empezar, cada quien levanta el proyecto de la sesión anterior (Mesa de ayuda, un ejemplo ilustrativo):
npm ci && npm run build && node server/server.mjs. Si alguien no lo tiene, se resuelve en la apertura, no a la mitad de la práctica.
Esta sesión se trabaja con un navegador real (Chromium). Quien no lo tenga instalado: npx playwright install chromium.
-->

---

## Agenda del día

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, objetivos, el proyecto | 20 min |
| Parte 1 | Medir antes de tocar nada | 45 min |
| Parte 2 | El peso del bundle y el render que sobra | 55 min |
| Parte 3 | Accesibilidad AA: teclado, foco, semántica y axe | 55 min |
| Parte 4 | Idiomas: i18next, plurales y claves tipadas | 25 min |
| Parte 5 | Práctica: medir, arreglar tres problemas y volver a medir | 75 min |
| Cierre | Errores típicos, tarea y quiz | 25 min |

<!-- Hay un descanso de 10 minutos entre la Parte 2 y la Parte 3, y otro a la mitad de la práctica. -->

---

## El lema de la sesión

> Optimizar sin medir antes es adivinar con más trabajo.

- Una optimización sin número antes y después es una opinión con código.
- La accesibilidad tampoco se adivina: una herramienta encuentra una parte, y el resto se recorre con el teclado.
- Hoy cada afirmación sale con la medición que la sostiene, hecha en un navegador real.

<!-- Pregunta al grupo: ¿quién ha envuelto algo en useMemo "por si acaso"? Casi todos. Hoy vemos cuánto costaba y cuánto ganó. -->

---

## Al terminar puedes

- Medir una pantalla con Lighthouse, con la API de rendimiento del navegador y con el tamaño del build, y repetir la medición.
- Decir qué pesa en tu bundle y dividirlo por rutas.
- Encontrar un render que sobra, corregirlo con la herramienta más barata y comprobar que ayudó.
- Recorrer una app solo con teclado y arreglar foco, título y etiquetas.
- Escribir una prueba que falle cuando alguien rompa la accesibilidad.
- Traducir una pantalla con claves tipadas, plurales y fechas por idioma.

---

## Cómo trabajamos en esta sesión

- **Ejemplo ilustrativo:** "Mesa de ayuda", una app de tickets inventada para enseñar. Sus datos son ficticios.
- Todo lo que se muestra se corrió en el proyecto, con estas versiones a noviembre de 2026: React 19.3.1, Vite 8.3.8, React Router 8.5.1, TypeScript 6.0.3, Playwright 1.65.0, Lighthouse 13.6.0.
- Los números de tiempo salen de **una máquina concreta y cargada**. Lo que importa es la comparación entre versiones, no el valor absoluto.
- Se reporta el rango de varias corridas, no la mejor.

```bash
node -v                        # v22.22.1 o superior (lo pide React Router 8.5.1)
npm ci && npm run build        # compila el proyecto
PORT=3100 node server/server.mjs
```

Fuente: `engines` de `react-router@8.5.1` en npm: `node >=22.22.0`.

---

<!-- _class: lead -->

# Parte 1
## Medir antes de tocar nada

---

## Qué es "rápido": tres números

Google define tres métricas de experiencia en la página, los Core Web Vitals, y un umbral de "bueno" para cada una:

| Métrica | Qué mide | Bueno |
|---|---|---|
| LCP (Largest Contentful Paint) | cuándo aparece el contenido principal | 2.5 s o menos |
| INP (Interaction to Next Paint) | cuánto tarda la página en responder a lo que haces | 200 ms o menos |
| CLS (Cumulative Layout Shift) | cuánto se mueve el diseño mientras carga | 0.1 o menos |

El criterio se evalúa en el **percentil 75** de las cargas reales de página, separando móvil y escritorio.

Fuente: web.dev, "Web Vitals" (web.dev/articles/vitals), última actualización 31 de octubre de 2024. Umbrales vigentes a noviembre de 2026.

<!-- Insistir en el percentil 75: tres de cada cuatro visitas deben cumplir. Una sola medición en tu laptop no es esto. -->

---

## INP: lo que esta sesión puede mover

- INP observa la latencia de las interacciones durante toda la visita y reporta un solo valor, el de la más larga sin contar valores atípicos.
- Cuentan los clics, los toques y las teclas. No cuentan pasar el cursor, hacer scroll ni hacer zoom.
- Umbrales: bueno hasta 200 ms, "necesita mejorar" hasta 500 ms, malo por encima.

> Escribir en un cuadro de búsqueda que filtra una lista es la interacción de INP que más seguido se rompe en un front de React.

Fuente: web.dev, "Interaction to Next Paint" (web.dev/articles/inp), última actualización 2 de septiembre de 2025.

---

## Laboratorio y campo

| | Laboratorio | Campo |
|---|---|---|
| Qué es | una corrida controlada (Lighthouse, tu navegador) | datos de visitas reales |
| Sirve para | comparar antes y después, depurar | saber cómo le va a la gente |
| Límite | una máquina, una red, una vez | llega tarde y sin causa |

Lighthouse es **laboratorio**: simula una red y un procesador, y devuelve una puntuación. No es el percentil 75 de nadie.

En esta sesión medimos en laboratorio: es lo que sirve para saber si **tu cambio** ayudó.

Fuente: la configuración del propio reporte de Lighthouse 13.6.0 (`throttlingMethod: simulate`, `formFactor: mobile`).

---

## Cuatro instrumentos, cuatro preguntas

| Instrumento | Contesta |
|---|---|
| `vite build` y el peso por paquete | ¿cuánto JavaScript descarga el usuario? |
| Lighthouse | ¿cómo se ve la carga inicial con una red simulada? |
| Event Timing (`PerformanceObserver`) | ¿cuánto tarda el navegador en procesar lo que escribo? |
| React Profiler | ¿qué componentes se renderizaron y cuánto costó cada uno? |

Ninguno sustituye a los otros. Cada uno contesta una pregunta distinta.

---

## El Profiler de React

`<Profiler>` mide el costo de renderizar un árbol. React llama a su `onRender` en cada actualización confirmada:

```tsx
function onRender(id, phase, actualDuration, baseDuration, startTime, commitTime) {
  console.log(id, phase, actualDuration, baseDuration);
}

<Profiler id="Volumen" onRender={onRender}>
  <VolumenPage />
</Profiler>
```

- `actualDuration`: milisegundos gastados renderizando ese árbol en esta actualización. Muestra qué tan bien funciona la memoización.
- `baseDuration`: estimación de lo que costaría renderizar todo sin optimizaciones.
- **Está desactivado en el build de producción** por omisión: para medir producción se necesita un build con profiling.

Fuente: react.dev, referencia de `<Profiler>`. Fragmento: se muestra la firma de la documentación.

<!-- En el navegador, la pestaña Profiler de las React Developer Tools hace lo mismo con una interfaz. Mostrarla en vivo con la pantalla de volumen. -->
---

## Demostración: Lighthouse sobre la pantalla de entrada

```bash
PORT=3100 node server/server.mjs &
npx lighthouse http://127.0.0.1:3100/entrar \
  --only-categories=performance,accessibility \
  --output=json --output-path=./reporte.json --quiet \
  --chrome-flags="--headless"
```

Resultado real (Lighthouse 13.6.0, Chromium 158 en modo headless, red simulada de móvil, a noviembre de 2026):

| Métrica | Valor |
|---|---|
| Performance / Accessibility | 1 / 1 |
| First Contentful Paint | 0.6 s |
| Largest Contentful Paint | 0.6 s |
| Total Blocking Time | 0 ms |
| Peso total | 487 KiB |
| Auditoría "JavaScript sin usar" | ahorro estimado de 273 KiB |

Este reporte se corrió sobre el proyecto **ya terminado de la sesión** (con idiomas y la pantalla de volumen): por eso pesa 487 KiB y no 478 kB. En la práctica corres el tuyo antes de empezar.

<!-- En el laboratorio de redacción se corrió con --no-sandbox y el chrome-headless-shell de Playwright porque el servidor no tiene Chrome instalado. En su laptop basta --chrome-flags="--headless". Un 1 en performance no significa "no hay nada que mejorar": mira la auditoría de JavaScript sin usar. -->

---

## Lo que dice el reporte y lo que no

- Una puntuación de 1 en una pantalla de entrada vacía **no prueba** que la app vaya bien: la pantalla casi no hace nada.
- Lo útil es la auditoría: 273 KiB estimados de JavaScript que esta pantalla descarga y no usa.
- Esa pista apunta a la siguiente parte: dividir el código.

Antes de arreglar nada, anotamos la línea base:

| Línea base (versión del curso, sesión 4) | Valor |
|---|---|
| JavaScript al abrir `/entrar` | 478.12 kB, un solo archivo |

Fuente: medición propia con Playwright (`e2e/peso.spec.ts`), tamaño sin comprimir.

---

## Medir lo que escribes: Event Timing

```ts {1-2|3-8|9-11|*}
// En la prueba E2E: registra cuánto tarda el navegador en procesar cada evento.
await page.evaluate(() => {
  const w = window as unknown as { __eventos: number[] };
  w.__eventos = [];
  new PerformanceObserver((lista) => {
    for (const e of lista.getEntries()) w.__eventos.push(e.duration);
  }).observe({ type: 'event', durationThreshold: 16 });
});
// Después de teclear: el peor evento es la "latencia de interacción" de esta prueba.
const eventos = await page.evaluate(() => (window as unknown as { __eventos: number[] }).__eventos);
console.log(Math.max(0, ...eventos));
```

- `duration` es el tiempo desde que se inicia el evento hasta el siguiente pintado, **redondeado a múltiplos de 8 ms**.
- El umbral por omisión es 104 ms; el mínimo posible es 16 ms.

Fuente: MDN, `PerformanceEventTiming` y `PerformanceObserver.observe()`.

---

## Error típico: medir mal

| Cómo se mide mal | Por qué engaña |
|---|---|
| Una sola corrida | La máquina varía: en esta sesión, la misma prueba osciló entre 4.6 s y 8.2 s |
| Con el servidor de desarrollo | `vite dev` no minifica ni divide como el build |
| Con las DevTools abiertas y otras pestañas | Compites contigo mismo |
| Cambiar tres cosas y medir una vez | No sabes cuál ayudó |
| Medir después sin haber medido antes | No hay con qué comparar |

**La regla:** una variable a la vez, el mismo comando antes y después, varias corridas, y se reporta el rango.

<!-- Si alguien mide en dev, que lo haga una vez y compare con el build: la diferencia suele sorprender. No la afirmamos con cifra porque no la medimos hoy. -->

---

<!-- _class: lead -->

# Parte 2
## El peso del bundle y el render que sobra

---

## ¿Qué pesa en tu bundle?

```bash
npx vite build --sourcemap
node scripts/peso-por-paquete.mjs
```

```text
react-dom                          202.0 kB
react-router                        93.3 kB
zod                                 81.2 kB
i18next                             47.4 kB
react-hook-form                     33.8 kB
@tanstack/query-core                32.1 kB
@tanstack/virtual-core              31.2 kB
src (código propio)                 12.9 kB
react                                8.0 kB
react-i18next                        4.8 kB
```

Es una **estimación** a partir del sourcemap, útil para comparar. No es un valor al byte.

<!-- Mostrar el script: es corto y está en el proyecto. La conclusión importante es la última proporción: el código propio casi no pesa. -->

---

## Lo que se ve

- Tu código propio pesa 12.9 kB de ~560. **El peso son las dependencias.**
- Cada librería nueva tiene un costo en kB que se paga en cada primera visita.
- `i18next` agregó 47.4 kB de golpe: lo veremos en la parte 4.

> Antes de agregar una dependencia, mide cuánto pesa. Después de agregarla, mide cuánto pesó.

Fuente: medición propia con `scripts/peso-por-paquete.mjs`, a noviembre de 2026 y con las versiones del proyecto.

---

## Dividir por rutas con `lazy`

React Router permite declarar la ruta con una función que importa la pantalla **cuando se visita**:

```tsx {1-2|4-8|9-13|*}
import { createBrowserRouter, type RouteObject } from 'react-router';
import { RutaProtegida } from '../features/auth';

export const rutas: RouteObject[] = [
  // eslint-disable-next-line boundaries/dependencies -- excepción de la sesión 5: lazy apunta al archivo de la pantalla
  { path: '/entrar', lazy: async () => ({ Component: (await import('../features/auth/login-page')).LoginPage }) },
  {
    element: <RutaProtegida />,
    children: [{ path: '/', lazy: async () => ({ Component: (await import('../features/tickets')).TicketsPage }) }],
  },
];

export const crearRouter = () => createBrowserRouter(rutas);
```

Fuente: React Router, "Route Object", propiedad `lazy` (reactrouter.com): "Most properties can be lazily imported to reduce the initial bundle size".

---

## Antes y después: el número honesto

| | JavaScript al abrir `/entrar` | Archivos |
|---|---|---|
| Antes (un solo archivo) | 478.12 kB | 1 |
| Después de `lazy` en las rutas | 438.91 kB | 1 |
| Chunk de tickets (se baja al entrar a esa pantalla) | 41.03 kB | 1 |

- Se ahorraron **39.2 kB (8.2 %)** en la primera pantalla. Menos de lo que promete la idea de "dividir el código": las librerías compartidas siguen en el chunk principal.
- La pantalla de entrada no se separó: Vite avisa por qué.

```text
[INEFFECTIVE_DYNAMIC_IMPORT] src/features/auth/login-page.tsx is dynamically imported by
src/app/router.tsx but also statically imported by src/features/auth/index.ts, dynamic import
will not move module into another chunk.
```

<!-- La enseñanza: un import estático desde el barril (index.ts) anula el import dinámico. Con lazy, nadie debe importar esa pantalla de forma estática. Aquí no lo arreglamos a propósito: la pantalla de entrada es chica y el aviso es una buena lección. -->

---

## El aviso de Vite es una prueba gratis

- Lee los avisos del build. Este explica **por qué** una división no pasó.
- El arreglo es importar la pantalla desde un solo lugar, no desde el barril de la feature.
- La regla de fronteras de la sesión 2 sigue valiendo: el `lazy` apunta a la feature, no a sus archivos internos. La excepción es esta pantalla (y la de volumen): el lint la marca y el proyecto la deja con un `eslint-disable-next-line` que dice el motivo. Una excepción sin motivo escrito es una regla rota.

| Decisión | Cuándo |
|---|---|
| `lazy` en una ruta | la pantalla es pesada o se visita poco |
| Dejarla en el chunk principal | es la primera pantalla de casi todos |

---

## Por qué un componente se vuelve a renderizar

Según react.dev, un componente se renderiza por dos razones:

1. Es su **render inicial**.
2. **El estado del componente, o el de uno de sus ancestros, cambió.**

Y el proceso es recursivo: si el componente actualizado devuelve otros componentes, React renderiza esos también. Por eso escribir en un `<input>` cuyo estado vive en la página vuelve a renderizar **todas las filas** de la lista, aunque cada fila reciba las mismas props.

`memo` rompe esa cadena **solo si las props no cambian**: un objeto o una función nuevos en cada render cuentan como props nuevas.

Fuente: react.dev, "Render and Commit" y la referencia de `memo`.

---

## La pantalla de volumen

5 000 tickets sintéticos (Ejemplo ilustrativo) y un cuadro de búsqueda. Cada fila formatea su fecha con `Intl.DateTimeFormat`, un trabajo caro que se repite en cada render.

```tsx {1-9|11-13|15-21|*}
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
  const [texto, setTexto] = useState('');
  const todos = useMemo(() => ticketsSinteticos(5000), []);
  const visibles = useMemo(() => todos.filter((t) => t.titulo.toLowerCase().includes(texto.toLowerCase())), [todos, texto]);
  // ...
  return (
    <ul>
      {visibles.map((t) => <FilaMemo key={t.id} ticket={t} />)}
    </ul>
  );
}
```

<!-- El código completo está en la práctica, paso 2. Aquí se muestra solo lo que importa. Es un fragmento. -->

---

## Medir antes de arreglar

Una prueba de Playwright escribe `incidencia 1` en el buscador (12 teclas) y mide con Event Timing. Resultados reales en Chromium 158, **rango de 7 corridas** en un servidor cargado:

| Versión | Tiempo total de teclear | Peor evento | Nodos `<li>` en el DOM |
|---|---|---|---|
| Sin optimizar (filas se renderizan siempre) | 4.6 a 8.2 s | 456 a 1 408 ms | 1 111 |
| `memo` en la fila | 0.24 a 0.64 s | 48 a 88 ms | 1 111 |
| Virtualizada con `@tanstack/react-virtual` | 0.09 a 0.23 s | 24 a 40 ms | 28 |

Umbral de INP "bueno": 200 ms. **La versión sin optimizar lo supera por mucho; las otras dos quedan por debajo.**

<!-- Cuidado con la lectura: el tiempo total incluye la espera de Playwright. Compara entre filas, no con la realidad de un usuario. Los eventos de 8 ms de resolución explican que los valores sean múltiplos de 8. -->

---

## `memo`: una herramienta de medición, no un hábito

> You should only rely on `memo` as a performance optimization. If your code doesn't work without it, find the underlying problem and fix it first.

(react.dev, referencia de `memo`)

- `memo` ayuda cuando el componente se renderiza seguido con **las mismas props** y renderizar cuesta.
- No ayuda si las props son nuevas en cada render (un objeto o una función creados en el cuerpo).
- Ponlo después de medir, y mide otra vez.

La misma página aclara que `memo` es una optimización, no una garantía: React puede renderizar un componente memoizado de todos modos.

---

## React Compiler: memoizar sin escribirlo

React Compiler memoiza por ti. Según react.dev, hace innecesarios `useMemo`, `useCallback` y `React.memo` en la mayoría de los casos.

```ts
// vite.compiler.config.ts (fragmento de la doc oficial, probado en el proyecto)
import { defineConfig } from 'vitest/config';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import babel from '@rolldown/plugin-babel';

export default defineConfig({
  plugins: [react(), babel({ presets: [reactCompilerPreset()] })],
  build: { outDir: 'dist-compiler' },
});
```

Paquete: `babel-plugin-react-compiler` 1.0.0 (npm, a noviembre de 2026). Con `@vitejs/plugin-react` 6 o superior se usa `reactCompilerPreset` con `@rolldown/plugin-babel`.

Fuente: react.dev, "React Compiler" e "Installation".

---

## Probamos el Compiler sobre la fila lenta

La misma `FilaLenta`, **sin** `memo`, compilada con React Compiler:

| Versión | Tiempo total de teclear | Peor evento |
|---|---|---|
| Sin optimizar, sin Compiler | 4.6 a 8.2 s | 456 a 1 408 ms |
| Sin optimizar, con Compiler (2 corridas) | 0.39 a 0.74 s | 56 a 128 ms |
| `memo` a mano | 0.24 a 0.64 s | 48 a 88 ms |

- El resultado queda en el mismo orden de magnitud que el `memo` manual.
- El Compiler no es gratis en bytes: el chunk principal pasó de 233.90 kB a 235.79 kB y el de tickets de 41.97 kB a 46.07 kB (build con `vite.compiler.config.ts`).
- Dos corridas no es una muestra: lo repites en tu proyecto antes de decidir.

<!-- Honestidad: son solo 2 corridas con Compiler. Lo que sí se puede afirmar: el Compiler quitó el render de las filas que no cambiaron, y el efecto se ve en el peor evento. -->

---

## Virtualizar: no dibujar lo que no se ve

```tsx {1-6|8-16|*}
const virtualizador = useVirtualizer({
  count: visibles.length,
  getScrollElement: () => contenedor.current,
  estimateSize: () => 24,
  overscan: 8,
});

<div ref={contenedor} style={{ height: 480, overflow: 'auto' }} tabIndex={0} aria-label="Resultados">
  <ul style={{ height: virtualizador.getTotalSize(), position: 'relative' }}>
    {virtualizador.getVirtualItems().map((v) => (
      <li key={visibles[v.index]!.id} style={{ position: 'absolute', transform: `translateY(${v.start}px)` }}>
        {visibles[v.index]!.titulo}
      </li>
    ))}
  </ul>
</div>
```

- Fragmento simplificado: el código completo, con la guarda del índice, está en la práctica.
- 28 nodos en el DOM en lugar de 1 111, con el mismo resultado visible.
- Costo: una dependencia (`@tanstack/virtual-core`, 31.2 kB sin comprimir) y trabajo de accesibilidad: el contenedor con scroll necesita `tabIndex={0}` y nombre.

Fuente: `@tanstack/react-virtual` 3.14.26 en npm; código probado en el proyecto.

---

## Paginar o virtualizar

| Opción | Cuándo | Qué cuesta |
|---|---|---|
| Paginación en el servidor | el conjunto es enorme o se filtra por servidor | un viaje por página; el usuario "pasa página" |
| Virtualización | todo el conjunto ya está en el cliente | complejidad y accesibilidad |
| Ninguna | decenas de filas | nada |

La mejor optimización de una lista de 5 000 filas suele ser **no traer 5 000 filas**.

Libro: *React Application Architecture for Production* (Alickovic y Alicea, 2026), cap. 8, secciones de paginación y virtualización.

---

## Debounce: menos eventos que procesar

Un `debounce` retrasa la acción hasta que el usuario deja de teclear unos milisegundos.

```ts
// Fragmento: no se ejecutó en el laboratorio de redacción.
import { useEffect, useState } from 'react';

export function useValorRetrasado<T>(valor: T, ms = 300): T {
  const [retrasado, setRetrasado] = useState(valor);
  useEffect(() => {
    const id = setTimeout(() => setRetrasado(valor), ms);
    return () => clearTimeout(id);
  }, [valor, ms]);
  return retrasado;
}
```

- Sirve para **llamadas a la API** mientras se escribe. El filtro local de la pantalla de volumen no lo necesita si las filas no se renderizan de más.
- Ponlo después de medir, no antes.

---

## Colocar el estado y componer

Dos arreglos sin dependencias:

1. **Colocar el estado donde se usa.** Si solo el cuadro de búsqueda necesita el texto mientras se teclea, ese estado vive en el cuadro, no en la página.
2. **Componer.** Un componente que recibe `children` no se vuelve a renderizar cuando cambia su estado interno: los hijos llegan ya creados.

Son gratis y no necesitan `memo`. Se prueban igual: se mide antes y después.

Libro: *React Application Architecture for Production* (Alickovic y Alicea, 2026), cap. 8, secciones "Colocating state" y "Using component composition".

---

## Error típico: optimizar a ciegas

| Síntoma | Lo que se hace | Lo que pasa |
|---|---|---|
| "La app se siente lenta" | `useMemo` en todo | más código, el mismo cuello de botella |
| Lista lenta | virtualizar sin medir | complejidad extra donde bastaba `memo` |
| Bundle grande | `lazy` en todas las rutas | peticiones extra por pantalla chica |
| "El Compiler lo arregla" | activarlo sin medir | puede ayudar; puede no cambiar nada |

La pregunta antes de cada cambio: **¿qué número voy a comparar?**

---

<!-- _class: lead -->

# Parte 3
## Accesibilidad AA: teclado, foco, semántica y axe

---

## El estándar: WCAG 2.2

- WCAG 2.2 es **Recomendación del W3C** desde el 12 de diciembre de 2024.
- Los criterios se agrupan en niveles A, AA y AAA. Un front serio apunta a **AA**.
- Cuatro principios: perceptible, operable, comprensible y robusto.

Fuente: W3C, "Web Content Accessibility Guidelines (WCAG) 2.2" (w3.org/TR/WCAG22/).

---

## Los criterios que esta sesión toca

| Criterio | Nombre | Nivel |
|---|---|---|
| 1.1.1 | Non-text Content | A |
| 1.3.1 | Info and Relationships | A |
| 1.4.3 | Contrast (Minimum): 4.5:1 en texto normal, 3:1 en texto grande | AA |
| 2.1.1 | Keyboard | A |
| 2.4.1 | Bypass Blocks | A |
| 2.4.2 | Page Titled | A |
| 2.4.3 | Focus Order | A |
| 2.4.7 | Focus Visible | AA |
| 2.5.8 | Target Size (Minimum): 24 por 24 píxeles CSS | AA |
| 3.1.1 | Language of Page | A |
| 3.3.2 | Labels or Instructions | A |
| 4.1.3 | Status Messages | AA |

Fuente: W3C, páginas "Understanding" de cada criterio (w3.org/WAI/WCAG22/Understanding/).

---

## Semántica primero

- `<main>`, `<nav>`, `<header>` y `<footer>` son **landmarks**: el lector de pantalla salta entre ellos.
- Un `<button>` ya es enfocable, activable con Espacio y Enter y se anuncia como botón. Un `<div onClick>` no es nada de eso.
- Todo campo tiene una `<label>` asociada con `htmlFor` (3.3.2, 1.3.1). Un `placeholder` no es etiqueta.
- Un mensaje que aparece sin mover el foco necesita `role="status"` o `role="alert"` (4.1.3).

```tsx
{errors.titulo && <p id="titulo-error" role="alert">{errors.titulo.message}</p>}
<input id="titulo" aria-invalid={errors.titulo ? 'true' : 'false'} aria-describedby={errors.titulo ? 'titulo-error' : undefined} />
```

Libro: *React Application Architecture for Production* (Alickovic y Alicea, 2026), cap. 10.

---

## El problema propio de una SPA

En una página normal, cambiar de ruta recarga el documento: el navegador anuncia el título nuevo y el foco vuelve al inicio.

En una SPA, **no pasa nada de eso**. El foco se queda en el botón que ya no existe y el título sigue siendo el de la pantalla anterior.

- Criterio 2.4.2: la página debe tener un título que describa su propósito.
- Criterio 2.4.3: el orden del foco debe ser coherente.

Se comprueba en la práctica: entrar con el teclado y mirar dónde queda el foco.

---

## Arreglarlo: un layout que sigue la ruta

```tsx {1-2|4-6|7-14|15-17|*}
import { useEffect, useRef } from 'react';
import { Outlet, useLocation } from 'react-router';

export function Layout() {
  const { pathname } = useLocation();
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
  return <Outlet />;
}
```

- El título sigue al `h1` (2.4.2) y el foco pasa al `h1` al cambiar de ruta (2.4.3). En la carga inicial solo se pone el título.
- `tabIndex = -1` permite enfocar el `h1` desde código sin meterlo en el orden de `Tab`.

<!-- Es una solución simple. Hay librerías y patrones más completos, y los lectores de pantalla se comportan distinto: se prueba con uno. La prueba de Playwright solo verifica foco y título. -->

---

## Navegar solo con teclado

| Tecla | Debe hacer |
|---|---|
| `Tab` / `Shift+Tab` | pasar por todo lo interactivo, en orden visual |
| `Enter` / `Espacio` | activar botones y enlaces |
| `Esc` | cerrar un modal y devolver el foco |
| Flechas | moverse dentro de un grupo (menú, pestañas) |

Tres preguntas al recorrer la app:

1. ¿Veo siempre dónde está el foco? (2.4.7: nunca `outline: none` sin reemplazo).
2. ¿Puedo llegar a todo y salir de todo? (2.1.1 y 2.1.2: sin trampas de teclado).
3. ¿El foco queda en algún lugar razonable tras cada acción?

---

## El enlace "saltar al contenido"

Cuando una página repite una barra de navegación, el teclado pasa por todos sus enlaces en cada pantalla. El criterio 2.4.1 pide un mecanismo para saltarlos.

```tsx
// Fragmento: la Mesa de ayuda no tiene barra de navegación todavía.
<a href="#contenido" className="saltar">Saltar al contenido</a>
<main id="contenido" tabIndex={-1}>...</main>
```

La técnica "añadir un enlace al inicio de cada página que lleve directo al contenido principal" es una de las suficientes que lista W3C.

Fuente: W3C, "Understanding SC 2.4.1: Bypass Blocks" (técnica G1).

---

## axe: una parte, no todo

axe-core automatiza una parte de las reglas de WCAG. La documentación de Playwright lo dice así: las pruebas automatizadas detectan algunos problemas comunes, y **muchos solo se descubren a mano**.

```ts {1-4|6-10|*}
import AxeBuilder from '@axe-core/playwright';

const REGLAS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

test('la pantalla de entrada no tiene violaciones detectables por axe', async ({ page }) => {
  await page.goto('/entrar');
  await expect(page.getByRole('heading', { name: 'Entrar' })).toBeVisible();
  const resultado = await new AxeBuilder({ page }).withTags(REGLAS).analyze();
  expect(resultado.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
});
```

- Se espera a que la pantalla exista antes de analizar: axe analiza lo que hay en ese momento.
- Se devuelve una lista legible (`id: nodos`), no el objeto completo.

Fuente: playwright.dev, "Accessibility testing"; `@axe-core/playwright` 4.14.0 en npm. La etiqueta `wcag22aa` es de axe-core.

---

## Verlo fallar: rompemos la pantalla a propósito

Quitamos la `<label>` del correo, agregamos una imagen sin `alt` y un botón gris claro sobre fondo blanco. Salida real de la prueba:

```text
Error: expect(received).toEqual(expected) // deep equality

- Expected  - 1
+ Received  + 5

- Array []
+ Array [
+   "color-contrast: 1",
+   "image-alt: 1",
+   "label: 1",
+ ]
```

Y Lighthouse, sobre la misma pantalla rota: **Accessibility 0.68** en lugar de 1, con las mismas tres auditorías en cero: `color-contrast`, `image-alt` y `label`.

<!-- Que el grupo vea el error: son tres criterios (1.4.3, 1.1.1, 3.3.2/1.3.1) detectados sin abrir un lector de pantalla. Lo que axe NO vio en la versión buena: el foco perdido al cambiar de ruta. Eso lo encontró el recorrido con teclado. -->

---

## Lo que axe no vio

La versión "buena" de la pantalla pasó axe y pasó Lighthouse 1. Y aun así:

- Al entrar, el foco quedaba en `<body>` y el título decía "Mesa de ayuda" en todas las pantallas.
- Lo detectó una **prueba de teclado** que escribimos nosotros.

Salida real al quitar el `Layout`:

```text
Error: expect(locator).toBeFocused() failed
Locator:  getByRole('heading', { name: 'Tickets' })
Expected: focused
Received: inactive
```

> Una herramienta automática encuentra una parte. El resto se recorre con teclado y, cuando importa, con un lector de pantalla.

---

## Error típico: accesibilidad como adorno

| Error | Por qué falla | Arreglo |
|---|---|---|
| `outline: none` sin reemplazo | no se ve el foco (2.4.7) | `:focus-visible` con un contorno claro |
| `<div onClick>` | no es enfocable ni se anuncia | `<button>` |
| `placeholder` en lugar de label | desaparece al escribir | `<label htmlFor>` |
| `aria-label` por todos lados | oculta el texto visible y se desincroniza | texto visible primero |
| Color como único indicador | no lo ve quien no distingue colores | texto o icono junto al color |

---

<!-- _class: lead -->

# Parte 4
## Idiomas: i18next, plurales y claves tipadas

---

## i18n: lo que cambia y lo que no

- **Textos** (los más obvios), **plurales**, **fechas, números y monedas** y el atributo `lang` del documento.
- No cambia la lógica del negocio: no se traduce con `if (idioma === 'en')` repartido por el código.
- Una librería (aquí `i18next` con `react-i18next`) guarda los textos por idioma y los entrega por **clave**.

Versiones a noviembre de 2026: `i18next` 27.1.1 y `react-i18next` 18.0.1 (npm).

Libro: *React Application Architecture for Production* (Alickovic y Alicea, 2026), cap. 9.

---

## Los recursos: una sola fuente de claves

```ts {1-2|3-8|9-14|16-17|*}
export const es = {
  comun: { cargando: 'Cargando...', reintentar: 'Reintentar', idioma: 'Idioma' },
  login: { titulo: 'Entrar', correo: 'Correo', contrasena: 'Contraseña', entrar: 'Entrar' },
  tickets: {
    resumen_one: 'Hay {{count}} ticket.',
    resumen_other: 'Hay {{count}} tickets.',
  },
  // ...
} as const;

type Estructura<T> = { -readonly [K in keyof T]: T[K] extends string ? string : Estructura<T[K]> };

// El inglés debe tener exactamente las mismas claves: si falta una, no compila.
export const en: Estructura<typeof es> = {
  comun: { cargando: 'Loading...', reintentar: 'Retry', idioma: 'Language' },
  login: { titulo: 'Sign in', correo: 'Email', contrasena: 'Password', entrar: 'Sign in' },
  tickets: {
    resumen_one: 'There is {{count}} ticket.',
    resumen_other: 'There are {{count}} tickets.',
  },
};
```

---

## Plurales: el idioma decide

En i18next un plural se declara con **sufijos**: `clave_one` y `clave_other` (y los que el idioma pida). La variable se llama `count` y es obligatoria.

```ts
i18n.t('tickets.resumen', { count: 1 }); // "Hay 1 ticket."
i18n.t('tickets.resumen', { count: 3 }); // "Hay 3 tickets."
```

- Las reglas siguen la API `Intl.PluralRules` del navegador.
- Sin `count`, no hay respaldo hacia la clave sin sufijo.
- Nunca se arma un plural con `count === 1 ? 'ticket' : 'tickets'`: el inglés y otros idiomas tienen reglas propias.

Fuente: i18next, "Plurals" (i18next.com/translation-function/plurals).

---

## Claves tipadas

```ts
declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'translation';
    resources: { translation: typeof es };
  }
}
```

Con esto, una clave mal escrita **no compila**. Salida real de `tsc` (abreviada) al escribir `t('login.titulos')`:

```text
src/features/auth/login-page.tsx(20,14): error TS2345: Argument of type '["login.titulos"]' is not
assignable to parameter of type ...
    Type '"login.titulos"' is not assignable to type '"comun.cargando" | "comun.reintentar" | ...'.
    Did you mean '"login.titulo"'?
```

Y si al inglés le falta una clave del español:

```text
src/shared/i18n/recursos.ts(44,3): error TS2741: Property 'idioma' is missing in type
'{ cargando: string; reintentar: string; errorCarga: string; }' but required in type ...
```

Fuente: documentación de react-i18next, "TypeScript" (react.i18next.com/latest/typescript); la salida es de `tsc` 6.0.3 en el proyecto.

---

## Fechas y números: `Intl`, no a mano

```tsx
new Intl.DateTimeFormat(i18n.language, { dateStyle: 'medium', timeZone: 'UTC' }).format(new Date(ticket.creadoEn))
```

- `Intl.DateTimeFormat` y `Intl.NumberFormat` están en el navegador: no hace falta una librería de fechas para mostrar una fecha.
- Se le pasa el idioma activo. `timeZone: 'UTC'` aquí hace que la fecha se vea igual en todas las máquinas.
- El selector de idioma muestra cada idioma en su propio nombre con `Intl.DisplayNames`.

```tsx
{IDIOMAS.map((i) => (
  <option key={i} value={i} lang={i}>{new Intl.DisplayNames([i], { type: 'language' }).of(i)}</option>
))}
```

---

## `lang` del documento y de las opciones

Al cambiar de idioma, el documento debe decirlo:

```ts
i18n.on('languageChanged', (idioma) => {
  document.documentElement.lang = idioma;
});
```

- Criterio 3.1.1: el idioma por omisión de la página debe poder determinarse por programa. Los lectores de pantalla cargan las reglas de pronunciación correctas a partir de él.
- Cada opción del selector lleva su propio `lang`.

Fuente: W3C, "Understanding SC 3.1.1: Language of Page".

---

## Dos trampas que solo aparecen al correr

1. **jsdom declara `navigator.language` como `en-US`.** Las pruebas de Vitest arrancaban en inglés y buscaban texto en español:

```text
TestingLibraryElementError: Unable to find an element with the text: No hay tickets con ese filtro..
```

Arreglo: en el `setup` de las pruebas, `await i18n.changeLanguage('es')` antes de cada prueba.

2. **Playwright usa `en-US` por omisión.** Las pruebas E2E no encontraban el encabezado "Entrar". Arreglo: `locale: 'es-MX'` en `playwright.config.ts`.

Las dos pasaron porque el idioma depende del **entorno**, y el entorno de prueba no es el tuyo.

<!-- Estas dos salieron al correr de verdad, no las inventamos. Preguntar al grupo: ¿qué otra cosa del entorno cambia el resultado? Zona horaria, por ejemplo. -->

---

## El costo y el límite

- Costo medido: `i18next` sumó 47.4 kB (sin comprimir) al bundle y `react-i18next` 4.8 kB. El JavaScript de `/entrar` pasó de 438.91 kB a **496.81 kB** (con la pantalla de volumen, el selector y todo lo agregado hoy).
- Límite: los mensajes de error que manda **el servidor** (como "Ya existe un ticket con ese título") llegan en el idioma del servidor. Traducirlos exige que la API mande códigos de error y no texto, o que reciba el idioma.
- Cargar un idioma solo cuando se necesita (namespaces o recursos dinámicos) es lo que sigue cuando hay muchos idiomas. El libro lo trata en su capítulo 9.

---

<!-- _class: lead -->

# Parte 5
## Práctica: medir, arreglar tres problemas y volver a medir

---

## Qué vamos a hacer

Con el proyecto de la sesión anterior:

1. **Medir** la línea base: bundle, Lighthouse y la pantalla de volumen.
2. **Arreglar tres problemas**, uno a la vez: dividir por rutas, resolver el render de la lista, y el foco y título al cambiar de ruta.
3. **Verlo fallar**: romper la accesibilidad a propósito y leer lo que axe dice.
4. **Recorrer la app solo con teclado.**
5. **Volver a medir** y llenar la tabla antes y después.

La guía paso a paso está en `practica.md`.

<!-- Sugerir parejas. Uno teclea, el otro anota los números. El que anota es el que entrega la tabla. -->

---

## Cierre: lo que se llevan

| Pregunta | Respuesta de hoy |
|---|---|
| ¿Qué es rápido? | LCP 2.5 s, INP 200 ms y CLS 0.1 en el percentil 75, medidos |
| ¿Qué pesa? | Las dependencias, no tu código |
| ¿Cómo arreglo un render que sobra? | Primero mides; luego colocas estado, `memo`, Compiler o virtualizas |
| ¿Es accesible? | axe encuentra una parte; el teclado, el resto |
| ¿Y los idiomas? | Claves tipadas, plurales por idioma y `Intl` |

---

## Tarea

**Reporte antes y después, con números**, de una app propia (o de la Mesa de ayuda) con:

- Peso de JavaScript de la primera pantalla antes y después de un cambio.
- Una interacción medida con Event Timing, antes y después.
- Un reporte de Lighthouse y uno de axe, con lo que arreglaste.
- Un recorrido con teclado documentado.

Detalle y criterios en `tarea.md`. Lecturas:

- `dev-notes/06-frontend/02-react-produccion.md`.
- *React Application Architecture for Production* (Alickovic y Alicea, 2026), caps. 8 a 10.

---

## Siguiente sesión

### S06 - Pruebas y producción

- Pruebas con Vitest, Testing Library y MSW: qué probar y qué no.
- Una prueba de punta a punta con Playwright.
- Build, versión en los assets y caché: por qué un navegador sigue con el JavaScript viejo.
- Un pipeline para el front y un contenedor.

> Un navegador con la versión vieja en caché hace que el arreglo exista solo en el servidor.

---

## Quiz: rendimiento, accesibilidad e idiomas

- 10 preguntas por intento, sacadas de un banco de 12.
- Individual, sin el proyecto abierto: se contesta con lo que entendiste, no con lo que copias.
- Lo que falles, búscalo en la nota `02-react-produccion` y en las láminas de esta sesión.

<!-- Últimos 15 minutos. Revisa en voz alta la pregunta que más se falle. -->
