# Glosario - S05 - Rendimiento, accesibilidad e idiomas

> Definiciones de los términos técnicos de la sesión 5.
> Ordenado alfabéticamente.

---

## A

### Accessible name
**Categoría:** Accesibilidad
**Definición:** El nombre con el que una tecnología de apoyo anuncia un control. Sale de su `<label>`, de su texto, de `aria-label` o de `alt`, según el elemento. Un campo sin nombre accesible se anuncia como "campo de edición" y nada más.
**En el curso:** axe reporta `label` cuando un `<input>` no tiene nombre, como al borrar la etiqueta del correo.

### actualDuration
**Categoría:** Rendimiento
**Definición:** Argumento de `onRender` del componente `<Profiler>`: milisegundos gastados renderizando el árbol medido en la actualización actual. Compararlo con `baseDuration` muestra qué tan bien funciona la memoización.
**En el curso:** Es la medición de React; en la sesión se usó además Event Timing en el navegador.

### aria-live
**Categoría:** Accesibilidad
**Definición:** Atributo que marca una región cuyos cambios un lector de pantalla debe anunciar sin mover el foco. `role="status"` y `role="alert"` la incluyen. Responde al criterio 4.1.3 de WCAG 2.2.
**En el curso:** El contador de resultados de la pantalla de volumen y los errores del formulario usan `role="status"` y `role="alert"`.

### axe-core
**Categoría:** Herramientas
**Definición:** Motor de reglas de accesibilidad que se integra en pruebas. `@axe-core/playwright` lo corre sobre la página real y devuelve violaciones con su regla y sus nodos. Detecta una parte de los problemas, no todos.
**En el curso:** Versión 4.14.0 del paquete a noviembre de 2026; detectó `color-contrast`, `image-alt` y `label` en la pantalla rota.

---

## B

### baseDuration
**Categoría:** Rendimiento
**Definición:** Argumento de `onRender`: estimación de lo que costaría renderizar todo el árbol del `<Profiler>` sin ninguna optimización. Es un peor caso.
**En el curso:** Sirve para saber cuánto ahorra `memo` comparándolo con `actualDuration`.

### Bundle
**Categoría:** Rendimiento
**Definición:** El archivo (o los archivos) de JavaScript que el build produce y que el navegador descarga. Su tamaño sin comprimir y comprimido se mide en kB.
**En el curso:** La línea base del proyecto es de 478.12 kB en un archivo; después de `lazy` el principal pesa 438.91 kB.

---

## C

### CLS (Cumulative Layout Shift)
**Categoría:** Rendimiento
**Definición:** Métrica de Core Web Vitals que suma cuánto se mueve el diseño de la página mientras carga. El umbral de "bueno" es 0.1 o menos en el percentil 75.
**En el curso:** Lighthouse dio 0 en la pantalla de entrada.

### Code splitting
**Categoría:** Rendimiento
**Definición:** Partir el JavaScript en varios archivos que se descargan cuando se necesitan, en lugar de uno solo al inicio. En React Router se hace con la propiedad `lazy` de la ruta.
**En el curso:** El chunk de tickets (41.03 kB) solo baja al visitar esa pantalla.

### Core Web Vitals
**Categoría:** Rendimiento
**Definición:** Conjunto de tres métricas de experiencia de usuario definidas por Google: LCP, INP y CLS, cada una con su umbral de "bueno" evaluado en el percentil 75.
**En el curso:** Se usaron sus umbrales (2.5 s, 200 ms y 0.1) para decidir si una medición es aceptable.

---

## D

### Debounce
**Categoría:** Rendimiento
**Definición:** Retrasar una acción hasta que pasan unos milisegundos sin nuevos eventos. Sirve para no lanzar una petición por cada tecla.
**En el curso:** Se mostró un hook de ejemplo; el filtro local de la pantalla de volumen se resolvió con `memo`.

---

## E

### Event Timing
**Categoría:** Rendimiento
**Definición:** API del navegador (`PerformanceObserver` con `type: 'event'`) que expone cuánto tarda el navegador en procesar cada evento hasta el siguiente pintado, redondeado a múltiplos de 8 ms.
**En el curso:** La prueba `rendimiento.spec.ts` la usa para medir el peor evento al escribir en el buscador.

---

## F

### Focus management
**Categoría:** Accesibilidad
**Definición:** Decidir en qué elemento queda el foco tras una acción (abrir un modal, cambiar de ruta). En una SPA el navegador no lo hace por ti.
**En el curso:** El `Layout` mueve el foco al `h1` al cambiar de ruta.

---

## I

### i18n
**Categoría:** Idiomas
**Definición:** Internacionalización: preparar una app para varios idiomas y regiones (textos, plurales, fechas, números). Se abrevia así porque entre la `i` y la `n` hay 18 letras.
**En el curso:** Se implementó con `i18next` y `react-i18next`.

### INP (Interaction to Next Paint)
**Categoría:** Rendimiento
**Definición:** Métrica de Core Web Vitals que reporta la latencia de la interacción más larga de la visita (sin contar valores atípicos). Cuentan clics, toques y teclas. Bueno: 200 ms o menos.
**En el curso:** El filtro de la lista con 5 000 filas superaba el umbral en la versión sin optimizar.

### Intl
**Categoría:** Idiomas
**Definición:** Objeto global de JavaScript con formateo sensible al idioma: `Intl.DateTimeFormat`, `Intl.NumberFormat`, `Intl.PluralRules`, `Intl.DisplayNames`.
**En el curso:** Se usa para fechas y para mostrar el nombre de cada idioma en el selector.

---

## L

### Landmark
**Categoría:** Accesibilidad
**Definición:** Región de la página con rol propio (`main`, `nav`, `header`, `footer`) entre las que un lector de pantalla permite saltar.
**En el curso:** Cada pantalla tiene un `<main>`.

### Lazy loading
**Categoría:** Rendimiento
**Definición:** Descargar código (o imágenes) solo cuando se necesita. En el curso, se hace con `import()` dinámico dentro de `lazy` en las rutas.
**En el curso:** El build avisa con `INEFFECTIVE_DYNAMIC_IMPORT` cuando un import estático anula la división.

### LCP (Largest Contentful Paint)
**Categoría:** Rendimiento
**Definición:** Métrica de Core Web Vitals: cuándo se pinta el elemento de contenido más grande. Bueno: 2.5 s o menos, medido desde que la página empieza a cargar.
**En el curso:** Lighthouse dio 0.6 s en la pantalla de entrada.

### Lighthouse
**Categoría:** Herramientas
**Definición:** Herramienta de auditoría que corre en laboratorio (red y procesador simulados) y devuelve puntuaciones de rendimiento, accesibilidad y otras.
**En el curso:** Versión 13.6.0 (a noviembre de 2026), sobre la pantalla de entrada correcta (1) y la rota (0.68 en accesibilidad).

---

## M

### memo
**Categoría:** React
**Definición:** `React.memo` envuelve un componente para que React se salte su render cuando las props no cambiaron. Es una optimización, no una garantía.
**En el curso:** Bajó el tiempo de la lista de 4.6-8.2 s a 0.24-0.64 s en el laboratorio.

---

## P

### Percentil 75
**Categoría:** Rendimiento
**Definición:** Valor por debajo del cual está el 75 % de las mediciones. Los Core Web Vitals se evalúan ahí, no en el promedio.
**En el curso:** Una sola corrida local no equivale a este percentil.

### Profiler
**Categoría:** React
**Definición:** Componente de React (y pestaña de las React Developer Tools) que mide el costo de renderizar un árbol. Está desactivado en el build de producción por omisión.
**En el curso:** Se explicó su firma `onRender`.

---

## R

### React Compiler
**Categoría:** React
**Definición:** Compilador que memoiza componentes y valores automáticamente, y reduce la necesidad de `useMemo`, `useCallback` y `memo`. Paquete `babel-plugin-react-compiler`, versión 1.0.0 a noviembre de 2026.
**En el curso:** Se probó sobre la fila sin `memo` y quedó en 0.39-0.74 s en dos corridas.

---

## S

### Skip link
**Categoría:** Accesibilidad
**Definición:** Enlace al inicio de la página que lleva directo al contenido principal para que el teclado se salte la navegación repetida (criterio 2.4.1).
**En el curso:** Se mostró como fragmento.

### Sourcemap
**Categoría:** Herramientas
**Definición:** Archivo que relaciona el JavaScript compilado con sus fuentes. Con él se estima cuánto aporta cada paquete al bundle.
**En el curso:** `scripts/peso-por-paquete.mjs` lo usa.

---

## V

### Virtualización
**Categoría:** Rendimiento
**Definición:** Renderizar solo las filas visibles de una lista larga y simular el resto con espacio. Reduce los nodos del DOM.
**En el curso:** Con `@tanstack/react-virtual` la lista pasó de 1 111 a 28 nodos `<li>`.

---

## W

### WCAG 2.2
**Categoría:** Accesibilidad
**Definición:** Web Content Accessibility Guidelines, versión 2.2: Recomendación del W3C desde el 12 de diciembre de 2024. Define criterios de éxito en niveles A, AA y AAA.
**En el curso:** Se trabaja el nivel AA, con criterios por número (2.4.3, 4.1.3, etc.).
