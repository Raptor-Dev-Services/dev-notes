# Tarea - S03 - Datos, estado y formularios

**React en producción con TypeScript**
**Tiempo estimado:** 4 a 5 horas

---

## Parte 1 - Lecturas (1.5 a 2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/06-frontend/08-estado-donde-vive.md` | Completa: la escalera, Context frente a store, estado derivado y persistencia | 40 min |
| `dev-notes/06-frontend/02-react-produccion.md` | React Hook Form + Zod; TanStack Query - patrones clave; Optimistic updates | 40 min |
| Documentación de TanStack Query v5 | *Important Defaults*, *Query Keys*, *Optimistic Updates* | 30 min |

Lee con el proyecto abierto: cada ejemplo se busca en el código y se corre.

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *React Application Architecture for Production* (Alan Alickovic, Anthony Alicea, 2026) | Cap. 5 - Communicating with the API; Cap. 6 - Managing Application State |
| *Learn React with TypeScript*, 3.ª ed. (Carl Rippon, 2025) | Cap. 8 - Client Component Data Fetching and Mutations with TanStack Query; Cap. 9 - Working with Forms; Cap. 10 - State Management |
| *Learn React Hooks*, 2.ª ed. (Daniel Bugl, 2025) | Cap. 5 - Implementing React Contexts; Cap. 6 - Using Hooks and React Suspense for Data Fetching; Cap. 7 - Using Hooks for Handling Forms; Cap. 8 - Using Hooks for Routing |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: los cuatro estados en tu proyecto (2.5 a 3 horas)

Trabaja sobre **tu propio proyecto** (el de tu equipo, o el del curso si aún no tienes uno). Si no tienes uno con API, usa la Mesa de ayuda del curso y agrega una pantalla nueva, por ejemplo el detalle de un ticket.

### Qué entregas

1. **El inventario de estado** (`estado-s03.md`): una tabla con **todos** los datos que guardan tus pantallas.

| Dato | Familia (servidor, UI, URL, derivado) | Dónde vive hoy | Dónde debería vivir | ¿Cambia? |
|---|---|---|---|---|

   - Mínimo 12 filas, de al menos tres pantallas distintas.
   - Marca cada dato que está **duplicado** (la misma información en dos lugares) y cada par de variables que pueden **contradecirse**.
   - Para cada fila donde "hoy" y "debería" difieren, haz el cambio o explica por qué no.

2. **Los cuatro estados** en todas las pantallas que traen datos del servidor. Para cada pantalla, una línea en `estado-s03.md` con la evidencia de que probaste los cuatro:

| Pantalla | Cargando | Error | Vacío | Con datos | Cómo lo provocaste |
|---|---|---|---|---|---|

   - Un estado "vacío" distinto del de "error", con un texto que diga qué hacer.
   - El error con un botón de reintentar.
   - Una captura de pantalla de cada uno.

3. **Un formulario real** de tu proyecto con:
   - validación en el cliente con un esquema (Zod u otro, justificado);
   - el error 422 del servidor pegado al campo;
   - botón deshabilitado y con texto distinto mientras guarda;
   - **lo escrito se conserva** ante un 500 y ante una red caída;
   - el campo y su error ligados para el lector de pantalla (`aria-invalid`, `aria-describedby`, `role="alert"`).

4. **Una prueba por cada defensa**, y para **cada una**, la demostración de que cae si quitas la defensa (como en el paso 4 de la práctica). Mínimo cuatro pruebas:
   - los cuatro estados (puede ser una prueba por estado);
   - el error 422 pegado al campo;
   - lo escrito se conserva ante una falla;
   - una mutación con rollback, o el filtro en la URL si no tienes mutaciones.

   La evidencia es el mensaje de error real al quitar la defensa, pegado en `estado-s03.md`.

### Condiciones

- Ningún dato del servidor copiado a `useState` si una consulta con caché lo resuelve.
- Toda `queryKey` incluye lo que cambia el resultado, y se genera desde una fábrica de claves.
- Los datos que llegan de la API se validan en un solo punto (la frontera), no en cada componente.
- Las pruebas no mockean `fetch` a mano: usan MSW o una alternativa equivalente que intercepte la red.

---

## Criterios de evaluación

| Criterio | Peso | Cómo se evalúa |
|---|---|---|
| Inventario de estado | 20 % | 12 filas o más, familias bien asignadas, duplicados y contradicciones encontrados |
| Los cuatro estados | 25 % | Todas las pantallas con datos, evidencia de cada estado |
| Formulario | 25 % | Validación cliente y servidor, no pierde lo escrito, accesible |
| Pruebas con demostración de caída | 20 % | Cada prueba cae de verdad al quitar su defensa |
| Claridad de la evidencia | 10 % | Salida real, no descrita |

**Entrega:** un enlace al repositorio con `estado-s03.md` y los cambios, o un `.zip`. Las capturas, dentro del documento.

---

## Siguiente sesión

### S04 - Autenticación y seguridad en el navegador

- Cookie `HttpOnly` frente a token en el navegador.
- Un XSS sembrado en tu propio laboratorio, y cómo lo frenan la cookie y la CSP.
- Tu CSP sin `unsafe-inline`.

> Un token en localStorage es de cualquier script que corra en la página.
