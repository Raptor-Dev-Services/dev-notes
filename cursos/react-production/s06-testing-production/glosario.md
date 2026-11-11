# Glosario - S06 - Pruebas y producción

> Definiciones de los términos técnicos de la sesión 6.
> Ordenado alfabéticamente.

---

## A

### Aserción
**Categoría:** Pruebas
**Definición:** La línea de una prueba que compara lo que pasó con lo que debía pasar (`expect(...)`). Una prueba sin aserciones que puedan fallar no protege nada.
**En el curso:** La prueba vacua tenía una aserción que no podía fallar en ese instante.

### Asset
**Categoría:** Producción
**Definición:** Archivo estático que la página descarga: JavaScript, CSS, imágenes, fuentes. En Vite, los de `dist/assets/` llevan una huella del contenido en el nombre.
**En el curso:** `index-WeLmJLk9.js` es un asset con huella.

---

## B

### Build
**Categoría:** Producción
**Definición:** El paso que convierte el código fuente en los archivos que se sirven (`dist/`): compila, minifica y reparte en chunks. Las variables `VITE_` se incrustan aquí.
**En el curso:** `npx vite build`.

---

## C

### Cache-Control
**Categoría:** HTTP
**Definición:** Cabecera que dice cómo y cuánto puede guardarse una respuesta. Directivas vistas: `max-age`, `no-cache`, `no-store`, `public` e `immutable`.
**En el curso:** El HTML lleva `no-cache` y los assets con huella `public, max-age=31536000, immutable`.

### Cache busting
**Categoría:** Producción
**Definición:** Hacer que la URL de un archivo cambie cuando cambia su contenido, para que ninguna caché sirva la versión vieja. Se hace con una huella en el nombre o en la cadena de consulta (`?v=`).
**En el curso:** La solución al caso del JavaScript viejo en la academia.

### CI (integración continua)
**Categoría:** Producción
**Definición:** Correr automáticamente, en cada cambio, las puertas del proyecto: tipos, lint, pruebas y build.
**En el curso:** El workflow `front.yml` de GitHub Actions.

### Cleanup
**Categoría:** Pruebas
**Definición:** Desmontar lo que una prueba renderizó. Sin `globals`, Vitest no lo hace solo y hay que llamar a `cleanup()` de Testing Library en un `afterEach`.
**En el curso:** Quitarlo hizo fallar 4 de 6 pruebas con "Found multiple elements".

---

## E

### E2E (punta a punta)
**Categoría:** Pruebas
**Definición:** Prueba que recorre un flujo completo en un navegador real contra el sistema real. Es lenta y se reserva para los flujos críticos.
**En el curso:** `e2e/tickets.spec.ts` con Playwright.

### ETag
**Categoría:** HTTP
**Definición:** Cabecera que identifica una versión de un recurso. El cliente la devuelve en `If-None-Match` y, si no cambió, el servidor contesta 304 sin cuerpo.
**En el curso:** nginx devolvió `304 Not Modified` al revalidar el HTML.

---

## F

### Fragmento
**Categoría:** Curso
**Definición:** Código o configuración que se muestra pero no se ejecutó en el laboratorio de redacción. Siempre va marcado.
**En el curso:** El workflow de GitHub y el `docker build`.

---

## H

### Huella (hash de contenido)
**Categoría:** Producción
**Definición:** Cadena derivada del contenido de un archivo (por ejemplo, los primeros caracteres de su SHA-256). Si el contenido cambia, la huella cambia.
**En el curso:** Va en el nombre que genera Vite y en el `?v=` del `desplegar-demo.mjs`.

---

## I

### immutable
**Categoría:** HTTP
**Definición:** Directiva de `Cache-Control` (RFC 8246) que indica que la respuesta no cambiará mientras sea fresca, así que el cliente no necesita revalidarla ni al recargar.
**En el curso:** Va en los assets con huella.

### Prueba de integración
**Categoría:** Pruebas
**Definición:** Prueba que junta varios módulos (una pantalla, su hook, su cliente de API) con la red simulada, y verifica lo que ve el usuario.
**En el curso:** Las de `tickets-page.test.tsx`.

---

## J

### jsdom
**Categoría:** Pruebas
**Definición:** Implementación de un navegador en Node, sin layout, sin red real y sin caché. Declara `navigator.language` como `en-US`.
**En el curso:** El entorno de Vitest.

---

## L

### Locator
**Categoría:** Pruebas
**Definición:** En Playwright, la forma de encontrar un elemento, con espera y reintento automáticos. Se prefieren los que usan rol, etiqueta o texto visible.
**En el curso:** `page.getByRole('button', { name: 'Entrar' })`.

---

## M

### MSW (Mock Service Worker)
**Categoría:** Pruebas
**Definición:** Librería que intercepta las peticiones de red y las contesta con manejadores que tú defines, de modo que el código corre igual que en producción.
**En el curso:** Versión 3.0.5; `onUnhandledFrame: 'error'` hace fallar las peticiones sin manejador.

---

## N

### no-cache
**Categoría:** HTTP
**Definición:** Directiva de `Cache-Control` que permite guardar la respuesta pero obliga a revalidarla con el servidor antes de cada uso. No significa "no guardar" (eso es `no-store`).
**En el curso:** Va en `index.html`.

---

## P

### Prueba vacua
**Categoría:** Pruebas
**Definición:** Prueba que pasa aunque el código esté roto, por ejemplo una aserción negativa evaluada antes de que la pantalla termine de cargar.
**En el curso:** `queryByText(...)).not.toBeInTheDocument()` sin esperar nada antes.

---

## R

### Revalidación
**Categoría:** HTTP
**Definición:** Preguntar al servidor si una copia guardada sigue vigente (con `If-None-Match` o `If-Modified-Since`). Si sí, 304 sin cuerpo.
**En el curso:** Lo que hace el navegador con el HTML por el `no-cache`.

---

## T

### Trace (traza)
**Categoría:** Pruebas
**Definición:** Archivo que Playwright guarda con el paso a paso de una prueba: el DOM en cada momento, la red y la consola. Se abre con `npx playwright show-trace`.
**En el curso:** `trace: 'retain-on-failure'`.

### Trofeo de pruebas
**Categoría:** Pruebas
**Definición:** Modelo de Kent C. Dodds para repartir el esfuerzo de pruebas: estática, unitaria, integración y punta a punta, con el mayor peso en integración.
**En el curso:** Guía qué se prueba en cada capa de la Mesa de ayuda.

---

## V

### Vitest
**Categoría:** Pruebas
**Definición:** Ejecutor de pruebas que comparte la configuración de Vite. Versión 5.1.0 a noviembre de 2026; pide Node `^22.12.0`, `^24.0.0` o `>=26.0.0`.
**En el curso:** Corre las pruebas de integración y unitarias.

---

## W

### webServer (Playwright)
**Categoría:** Pruebas
**Definición:** Opción de `playwright.config.ts` que arranca un servidor antes de las pruebas y espera a que responda en una URL.
**En el curso:** Levanta `node server/server.mjs`.

### Web-first assertion
**Categoría:** Pruebas
**Definición:** Aserción de Playwright que espera hasta que la condición se cumple (`expect(locator).toBeVisible()`), en lugar de evaluarla una sola vez.
**En el curso:** Reemplaza las esperas fijas.
