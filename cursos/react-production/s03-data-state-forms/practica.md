# Práctica - S03 - Datos, estado y formularios

**React en producción con TypeScript**
**Duración:** 80 minutos, por equipo.

---

## Objetivo

Ver con tus propios ojos cinco cosas que se rompen cuando el estado vive en el lugar equivocado, y dejar el formulario de la Mesa de ayuda (Ejemplo ilustrativo) resistiendo errores del servidor y una red caída:

1. Una respuesta lenta que pisa a una más nueva.
2. Una clave de consulta que olvida el filtro.
3. Una actualización optimista sin rollback.
4. Un error 422 pegado al campo.
5. Un formulario que no pierde lo escrito.

Cada "verlo fallar" se hace **a propósito, sobre una copia**, y se restaura al terminar. La regla del curso: si no lo viste fallar, no lo probaste.

---

## Antes de empezar

Versiones a noviembre de 2026 (las del `package.json` del proyecto de referencia): Node 22.22 o superior, React 19.3.1, TanStack Query 5.107.1, React Hook Form 7.92.0, Zod 4.6.7, MSW 3.0.5, Vitest 5.1.0.

```bash
cd referencia/mesa-de-ayuda
npm install
npm run typecheck     # sin errores
npm test              # todas las pruebas en verde
```

Si `npm test` no está en verde **antes** de tocar nada, para: no sabrás si lo que rompes lo rompiste tú.

Dos terminales para la parte de navegador:

```bash
npm run api           # API de laboratorio en http://127.0.0.1:3100
npm run dev           # el front con Vite, que manda /api a la API
```

Entra con `ana@example.com` y la contraseña `clave-de-ejemplo` (datos de laboratorio, inventados).

---

## Paso 1 - Los cuatro estados en el navegador

Abre el front y las herramientas de desarrollo (F12, pestaña Network).

| Estado | Cómo provocarlo | Qué debes ver |
|---|---|---|
| Cargando | Network -> Throttling -> una conexión lenta, y recarga | "Cargando..." |
| Con datos | Quita el límite | La lista de tres tickets |
| Vacío | Filtra por "Cerrados" y reabre el único cerrado | "No hay tickets con ese filtro." |
| Error | Network -> Offline, y cambia el filtro | "No pudimos cargar los datos" con el botón Reintentar, tras unos segundos de reintentos |

Anota en `evidencia-s03.md` una línea por estado: qué hiciste y qué viste. Si una pantalla de **tu** proyecto no muestra alguno, ya sabes qué atender en la tarea.

El error tarda unos segundos porque la app reintenta hasta 2 veces los fallos de red (`src/app/query-client.ts`); un 404 o un 422 no se reintenta.

---

## Paso 2 - Verlo fallar: la respuesta vieja pisa a la nueva

Crea `src/practica/ingenua.test.tsx` con la forma "de tutorial": un `fetch` en un `useEffect` y un `useState`.

```tsx
import { useEffect, useState } from 'react';
import { expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { servidor } from '../test/servidor';

function ListaIngenua({ estado }: { estado: string }) {
  const [titulos, setTitulos] = useState<string[]>([]);
  useEffect(() => {
    fetch(`/api/tickets?estado=${estado}`)
      .then((r) => r.json())
      .then((p: { datos: { titulo: string }[] }) => setTitulos(p.datos.map((t) => t.titulo)));
  }, [estado]);
  return <ul>{titulos.map((t) => <li key={t}>{t}</li>)}</ul>;
}

it('la respuesta lenta del filtro viejo pisa a la del filtro nuevo', async () => {
  servidor.use(
    http.get('/api/tickets', async ({ request }) => {
      const estado = new URL(request.url).searchParams.get('estado');
      if (estado === 'abierto') await new Promise((r) => setTimeout(r, 300)); // la respuesta vieja tarda
      const titulo = estado === 'abierto' ? 'Ticket de abiertos' : 'Ticket de cerrados';
      return HttpResponse.json({ datos: [{ titulo }] });
    }),
  );
  const { rerender } = render(<ListaIngenua estado="abierto" />);
  rerender(<ListaIngenua estado="cerrado" />); // la persona cambia el filtro enseguida
  await new Promise((r) => setTimeout(r, 600));
  expect(screen.getByRole('list')).toHaveTextContent('Ticket de cerrados');
});
```

```bash
npx vitest run src/practica
```

La prueba **falla**, y debe fallar. La salida real:

```text
 FAIL  src/practica/ingenua.test.tsx > la respuesta lenta del filtro viejo pisa a la del filtro nuevo
Error: expect(element).toHaveTextContent()
Expected element to have text content:
  Ticket de cerrados
Received:
  Ticket de abiertos
```

La persona eligió "cerrados" y la pantalla le muestra "abiertos": la petición del filtro viejo llegó **después**. Es la condición de carrera que TanStack Query evita con la clave de consulta y la `signal`.

Responde en tu evidencia: ¿en qué orden salieron las peticiones y en qué orden llegaron las respuestas? Borra la carpeta `src/practica/` al terminar.

---

## Paso 3 - Verlo fallar: la clave que olvida el filtro

La prueba que lo detecta ya existe: `al cambiar el filtro se pide y se muestra la lista de ese filtro`, en `src/features/tickets/tickets-estado.test.tsx`. Confírmala en verde, y luego rompe la clave en `src/features/tickets/use-tickets.ts`:

```ts
// antes
queryKey: ticketsKeys.lista(estado),
// después (a propósito)
queryKey: ['tickets'],
```

```bash
npx vitest run src/features/tickets/tickets-estado.test.tsx
```

Resultado real (extracto):

```text
 > src/features/tickets/tickets-estado.test.tsx (5 tests | 1 failed)
     × al cambiar el filtro se pide y se muestra la lista de ese filtro
 FAIL  ... > al cambiar el filtro se pide y se muestra la lista de ese filtro
TestingLibraryElementError: Unable to find an element with the text: /El correo no sincroniza/.
```

La clave es la misma para todos los filtros: TanStack Query responde con lo que ya tiene y **no vuelve a pedir**. Restaura la línea original y confirma que la prueba vuelve a verde.

Regla para la tarea: todo lo que la `queryFn` lee de afuera va en la clave.

---

## Paso 4 - Verlo fallar: quitar el rollback

Lee `src/features/tickets/use-cambiar-estado.ts`. La prueba `cerrar un ticket se ve al instante y vuelve atrás si el servidor falla` simula un PATCH que falla (500) y una relectura que **nunca contesta**, para que el rollback se vea solo, sin ayuda de la relectura.

1. Corre la prueba con el código original: verde.
2. Comenta la línea del rollback en `onError`:

```ts
onError: (_error, _vars, contexto) => {
  // contexto?.antes.forEach(([clave, datos]) => cliente.setQueryData(clave, datos));
},
```

3. Corre de nuevo:

```bash
npx vitest run src/features/tickets/tickets-estado.test.tsx
```

Resultado real (extracto):

```text
 FAIL  ... > cerrar un ticket se ve al instante y vuelve atrás si el servidor falla
TestingLibraryElementError: Unable to find an element with the text: /\(media, abierto\)/.
```

Sin el rollback, la pantalla se queda mostrando "cerrado": un estado que el servidor rechazó. Restaura la línea. Responde en tu evidencia:

- ¿Por qué la prueba hace que la relectura nunca conteste? (Pista: sin eso, la relectura que sigue a toda mutación esconde el problema, porque trae el valor correcto de todos modos.)
- ¿Por qué `onSettled` **devuelve** la promesa de `invalidateQueries`? ¿Qué pasa con `isPending` de la mutación mientras tanto?

---

## Paso 5 - El error 422 pegado al campo

Con la API corriendo, comprueba primero qué responde el servidor ante datos inválidos (el servidor de laboratorio exige la cabecera `X-Requested-By` en las escrituras; la sesión 4 explica por qué):

```bash
curl -s -c cookies.txt -H 'content-type: application/json' \
  -d '{"email":"ana@example.com","password":"clave-de-ejemplo"}' http://127.0.0.1:3100/api/login
curl -s -b cookies.txt -X POST http://127.0.0.1:3100/api/tickets \
  -H 'x-requested-by: curl' -H 'content-type: application/json' \
  -d '{"titulo":"ab","prioridad":"x"}' -w ' [%{http_code}]\n'
```

Salida real:

```text
{"codigo":"validacion","mensaje":"Revisa los campos.","errores":{"titulo":"El título necesita al menos 5 caracteres.","prioridad":"Elige baja, media o alta."}} [422]
```

Y sin la cabecera, el servidor rechaza la escritura aunque los datos sean buenos:

```text
{"codigo":"csrf","mensaje":"Falta la cabecera X-Requested-By."} [403]
```

Ahora, en el navegador, prueba la validación del **cliente**: escribe `abc` y pulsa Crear ticket. Debe salir `El título necesita al menos 5 caracteres.` sin que se haya hecho ninguna petición (míralo en la pestaña Network).

Después, la validación del **servidor**. La prueba `pega el error 422 del servidor al campo y conserva lo escrito` (en `tickets-page.test.tsx`) simula un 422 con `errores: { titulo: 'Ya existe un ticket con ese título.' }`. Léela y contesta:

- ¿Qué línea de `ticket-form.tsx` pega el mensaje al campo? ¿Qué `type` usa y para qué sirve?
- ¿Por qué se compara `campo === 'titulo' || campo === 'prioridad'` antes de llamar a `setError`?

Extiende el formulario: agrega al servidor de laboratorio (`server/server.mjs`) la regla "no se permiten dos tickets abiertos con el mismo título" que responda 422 con `{ errores: { titulo: 'Ya existe un ticket abierto con ese título.' } }`, y comprueba en el navegador que el mensaje aparece junto al campo y que el texto escrito se queda.

---

## Paso 6 - Verlo fallar: la red cae y no se pierde nada

La prueba de punta a punta `e2e/red-caida.spec.ts` hace exactamente esto en un navegador real:

```ts
await page.route('**/api/tickets', (ruta) => (ruta.request().method() === 'POST' ? ruta.abort('connectionfailed') : ruta.continue()));
await page.getByLabel('Título').fill('Escrito justo antes de que cayera la red');
await page.getByRole('button', { name: 'Crear ticket' }).click();
await expect(page.getByText(/Lo que escribiste sigue aquí/)).toBeVisible();
await expect(page.getByLabel('Título')).toHaveValue('Escrito justo antes de que cayera la red');

await page.reload();
await expect(page.getByLabel('Título')).toHaveValue('Escrito justo antes de que cayera la red');
```

```bash
npm run build
PORT=3110 npx playwright test e2e/red-caida.spec.ts
```

(Si es tu primera vez con Playwright: `npx playwright install --with-deps chromium`; en una máquina sin permisos de administrador pide ayuda al instructor.)

Salida real:

```text
Running 1 test using 1 worker
  ok  1 e2e/red-caida.spec.ts:3:1 › con la red caída el formulario conserva lo escrito y, al recargar, el borrador (635ms)
  1 passed
```

Ahora **rómpelo**: en `ticket-form.tsx` mueve `reset()` para que corra al enviar, antes del resultado:

```tsx
const enviar = handleSubmit((valores) => {
  crear.mutate(valores, { /* ... */ });
  reset(); // a propósito: antes de saber el resultado
});
```

Corre la prueba otra vez. Resultado real (extracto):

```text
  fallo  1 e2e/red-caida.spec.ts:3:1 › con la red caída el formulario conserva lo escrito y, al recargar, el borrador
    Error: expect(locator).toHaveValue(expected) failed
    Locator:  getByLabel('Título')
    Expected: "Escrito justo antes de que cayera la red"
    Received: ""
```

El mensaje de error llegó, pero el campo ya estaba vacío: es el error típico de la sesión. Restaura el código.

Sin Playwright puedes verlo a mano: con `npm run dev`, escribe un título, ponte Offline en Network y pulsa Crear ticket. El mensaje dice `No se pudo guardar: Failed to fetch. Lo que escribiste sigue aquí; intenta de nuevo.` (el texto `Failed to fetch` lo escribe Chromium; otros navegadores usan otro).

---

## Paso 7 - El borrador sobrevive a una recarga

1. Escribe medio título, sin enviar.
2. Recarga con F5. El título sigue ahí.
3. Abre las herramientas -> Application -> Session storage y localiza la clave `borrador-ticket`.
4. Cierra la pestaña y vuelve a abrir la aplicación: el borrador ya no está (`sessionStorage` vive mientras la pestaña esté abierta).

La prueba unitaria equivalente es `el borrador sobrevive a una recarga (mismo sessionStorage)`.

Dos preguntas para la evidencia:

- ¿Por qué `borrador.ts` envuelve cada acceso en `try/catch`?
- El borrador guarda **solo el título**. ¿Qué pasaría si guardara una contraseña? (La respuesta es el tema de la sesión 4.)

---

## Paso 8 - Cierre

Corre todas las puertas del proyecto y pega el resultado:

```bash
npm run typecheck
npm run lint
npm test
```

Con el proyecto de referencia al 2026-11-11 el resultado es: `tsc` sin errores, ESLint sin avisos y todas las pruebas de Vitest en verde (las 11 de `src/features/tickets` más las que traigan otras carpetas).

---

## Entrega

`evidencia-s03.md` con:

- Una línea por cada uno de los cuatro estados del paso 1.
- La salida de los tres "verlo fallar" (pasos 2, 3, 4) y la del paso 6.
- Las respuestas a las preguntas de los pasos 4, 5 y 7.
- La regla nueva del servidor del paso 5 y una captura del mensaje junto al campo.
- La salida de `npm run typecheck`, `npm run lint` y `npm test`.

## Criterios de evaluación

| Criterio | Peso |
|---|---|
| Los tres "verlo fallar" muestran el error real, no uno descrito | 30 % |
| Las respuestas explican el porqué, no solo el qué | 25 % |
| La regla nueva del servidor funciona y se ve junto al campo | 20 % |
| El paso de la red caída conserva lo escrito y lo demuestra | 15 % |
| Las puertas quedan en verde | 10 % |

---

## Fuentes

- TanStack Query v5, guías *Important Defaults*, *Optimistic Updates*, *Query Cancellation* y *Query Keys* (tanstack.com/query/latest), leídas el 2026-11-11.
- React Hook Form, `setError` (react-hook-form.com/docs/useform/seterror).
- MSW 3.0.5, tipos de `SharedOptions`: la opción es `onUnhandledFrame`.
- Playwright 1.65, `page.route` y `route.abort` (playwright.dev).
