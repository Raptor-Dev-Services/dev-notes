# Práctica - S02 - Arquitectura por feature

**React en producción con TypeScript**
**Duración:** 85 minutos, por parejas, en su máquina.

---

## Objetivo

Tomar una aplicación organizada por **tipo de archivo**, reorganizarla por **feature** sin romper nada (con `tsc` y las pruebas como red), poner una **regla de importación que falle en el lint** y verla fallar. Al final agregas la capa de dominio y compones dos features en `app/`.

> **Ejemplo ilustrativo.** La *Mesa de ayuda* es una aplicación de ejemplo; sus datos no existen. Todo corre en tu máquina.

---

## Antes de empezar

La carpeta `referencia/mesa-plana` trae la Mesa de ayuda **organizada por tipo de archivo**, con las mismas pruebas de las sesiones anteriores.

```bash
cp -r referencia/mesa-plana ~/mesa-plana
cd ~/mesa-plana
npm ci
git init && git add -A && git commit -m "base: mesa plana"
npm run typecheck && npm run lint && npm test
```

Los tres deben terminar sin errores (`Tests  6 passed (6)`). El `git init` es parte de la práctica: **sin un commit por movimiento no hay red**.

---

## Paso 1 - Medir cuánto cuesta un cambio

```bash
find src -type f | sort
```

```console
src/components/estado-de-datos.tsx
src/components/ticket-form.tsx
src/hooks/use-sesion.ts
src/hooks/use-tickets.ts
src/main.tsx
src/pages/login-page.tsx
src/pages/tickets-page.test.tsx
src/pages/tickets-page.tsx
src/routes/router.tsx
src/routes/ruta-protegida.tsx
src/services/auth-api.ts
src/services/client.ts
src/services/contract.ts
src/services/tickets-api.ts
src/test/render.tsx
src/test/servidor.ts
src/test/setup.ts
```

¿Cuántas carpetas toca el tema "tickets"?

```bash
grep -rlE "ticket|Ticket" src | grep -v test | xargs -n1 dirname | sort -u
```

```console
src/components
src/hooks
src/pages
src/routes
src/services
```

Y qué importa la pantalla de tickets:

```bash
grep -n "^import" src/pages/tickets-page.tsx
```

```console
1:import { useState } from 'react';
2:import type { EstadoTicket } from '../services/contract';
3:import { EstadoDeDatos } from '../components/estado-de-datos';
4:import { TicketForm } from '../components/ticket-form';
5:import { useTickets } from '../hooks/use-tickets';
```

**Anota** (para la evidencia): cuántas carpetas toca "tickets", y cuántos archivos importan `contract.ts`:

```bash
grep -rln "/contract'" src | sort
```

---

## Paso 2 - Reorganizar por feature, un movimiento a la vez

El destino:

```console
src/
  app/        router.tsx
  features/
    auth/     api.ts  index.ts  login-page.tsx  ruta-protegida.tsx  use-sesion.ts
    tickets/  api.ts  index.ts  ticket-form.tsx  tickets-page.tsx  tickets-page.test.tsx  use-tickets.ts
  shared/
    api/      client.ts  contract.ts
    ui/       estado-de-datos.tsx
  test/       render.tsx  servidor.ts  setup.ts
  main.tsx
```

| Origen | Destino |
|---|---|
| `services/client.ts`, `services/contract.ts` | `shared/api/` |
| `components/estado-de-datos.tsx` | `shared/ui/` |
| `services/auth-api.ts` | `features/auth/api.ts` |
| `hooks/use-sesion.ts`, `pages/login-page.tsx`, `routes/ruta-protegida.tsx` | `features/auth/` |
| `services/tickets-api.ts` | `features/tickets/api.ts` |
| `hooks/use-tickets.ts`, `components/ticket-form.tsx`, `pages/tickets-page.tsx`, `pages/tickets-page.test.tsx` | `features/tickets/` |
| `routes/router.tsx` | `app/router.tsx` |

### El ritmo: un movimiento, una verificación

Empieza por lo compartido, que no depende de nadie:

```bash
mkdir -p src/shared/api src/shared/ui
git mv src/services/client.ts src/shared/api/client.ts
git mv src/services/contract.ts src/shared/api/contract.ts
npx tsc --noEmit
```

`tsc` lista **cada import que se rompió**, con archivo y línea. Arréglalos uno por uno (son rutas relativas) hasta que compile, corre las pruebas y haz commit:

```bash
npx vitest run
git commit -am "refactor: el cliente y el contrato pasan a shared/api"
```

Repite con `estado-de-datos.tsx` hacia `shared/ui/`. Luego las features:

```bash
mkdir -p src/features/auth src/features/tickets src/app
git mv src/services/auth-api.ts src/features/auth/api.ts
git mv src/hooks/use-sesion.ts src/features/auth/use-sesion.ts
git mv src/pages/login-page.tsx src/features/auth/login-page.tsx
git mv src/routes/ruta-protegida.tsx src/features/auth/ruta-protegida.tsx
# tsc, pruebas, commit...
git mv src/services/tickets-api.ts src/features/tickets/api.ts
git mv src/hooks/use-tickets.ts src/features/tickets/use-tickets.ts
git mv src/components/ticket-form.tsx src/features/tickets/ticket-form.tsx
git mv src/pages/tickets-page.tsx src/features/tickets/tickets-page.tsx
git mv src/pages/tickets-page.test.tsx src/features/tickets/tickets-page.test.tsx
git mv src/routes/router.tsx src/app/router.tsx
# tsc, pruebas, commit...
```

### Las APIs públicas

Crea los `index.ts`:

```ts
// src/features/tickets/index.ts
export { TicketsPage } from './tickets-page';
```

```ts
// src/features/auth/index.ts
export { LoginPage } from './login-page';
export { RutaProtegida } from './ruta-protegida';
export { useSesion, useSalir } from './use-sesion';
```

Y `app/router.tsx` importa **solo** por ahí:

```tsx
import { LoginPage, RutaProtegida } from '../features/auth';
import { TicketsPage } from '../features/tickets';
```

Cuando `tsc`, el lint y `npm test` estén en verde, borra las carpetas vacías (`src/components`, `src/hooks`, `src/pages`, `src/routes`, `src/services`) y haz el commit final:

```bash
npm run typecheck && npm run lint && npm test
git add -A && git commit -m "refactor: la aplicación se organiza por feature"
```

**Pregunta para la pareja:** ¿cuántos archivos tuvieron que cambiar de contenido (no solo de lugar)? ¿Qué te dice eso de la estructura anterior?

---

## Paso 3 - Poner la regla

Instala el plugin y el resolutor (ya vienen en el `package.json` de la referencia; en tu proyecto real hay que instalarlos):

```bash
npm i -D eslint-plugin-boundaries@7.4.2 eslint-import-resolver-node
```

Reemplaza `eslint.config.js`:

```js
import tseslint from 'typescript-eslint';
import boundaries from 'eslint-plugin-boundaries';

export default tseslint.config(
  { ignores: ['dist', 'node_modules'] },
  ...tseslint.configs.recommended,
  {
    files: ['src/**/*.{ts,tsx}'],
    plugins: { boundaries },
    settings: {
      'import/resolver': { node: { extensions: ['.ts', '.tsx', '.js'] } },
      'boundaries/elements': [
        { type: 'app', pattern: 'src/app' },
        { type: 'feature', pattern: 'src/features/*', capture: ['nombre'] },
        { type: 'shared', pattern: 'src/shared' },
      ],
    },
    rules: {
      'boundaries/dependencies': [
        'error',
        {
          default: 'disallow',
          policies: [
            { from: { element: { type: 'app' } }, allow: { to: { element: { type: 'feature', fileInternalPath: 'index.ts' } } } },
            { from: { element: { type: 'app' } }, allow: { to: { element: { type: 'shared' } } } },
            { from: { element: { type: 'feature' } }, allow: { to: { element: { type: 'shared' } } } },
            { from: { element: { type: 'feature' } }, allow: { to: { element: { type: 'feature', captured: { nombre: '{{ from.element.captured.nombre }}' } } } } },
            { from: { element: { type: 'shared' } }, allow: { to: { element: { type: 'shared' } } } },
          ],
        },
      ],
    },
  },
);
```

```bash
npm run lint
```

Debe terminar **sin errores**: la estructura que acabas de armar cumple la regla. Haz commit.

---

## Paso 4 - Verlo fallar

Una regla que nunca viste fallar no sabes si funciona. Para cada caso: crea el archivo, corre `npx eslint .`, **lee el mensaje**, borra el archivo.

### 4.1 Una feature que importa a otra

```bash
cat > src/features/tickets/malo.ts <<'E'
import { useSesion } from '../auth';
export const x = useSesion;
E
npx eslint .
```

```console
src/features/tickets/malo.ts
  1:27  error  There is no policy allowing dependencies from elements of type "feature" and captured values: nombre="tickets" to elements of type "feature" and captured values: nombre="auth"  boundaries/dependencies
```

### 4.2 `shared` que importa una feature

```bash
cat > src/shared/malo2.ts <<'E'
import { TicketsPage } from '../features/tickets';
export const y = TicketsPage;
E
```

```console
src/shared/malo2.ts
  1:29  error  There is no policy allowing dependencies from elements of type "shared" to elements of type "feature" and captured values: nombre="tickets"  boundaries/dependencies
```

### 4.3 Una feature que importa a `app`

```bash
cat > src/features/auth/malo3.ts <<'E'
import { crearRouter } from '../../app/router';
export const z = crearRouter;
E
```

```console
src/features/auth/malo3.ts
  1:29  error  There is no policy allowing dependencies from elements of type "feature" and captured values: nombre="auth" to elements of type "app"  boundaries/dependencies
```

### 4.4 `app` que importa por dentro de una feature

```bash
cat > src/app/malo.ts <<'E'
import { LoginPage } from '../features/auth/login-page';
export const q = LoginPage;
E
```

```console
src/app/malo.ts
  1:27  error  There is no policy allowing dependencies from elements of type "app" to elements of type "feature" and captured values: nombre="auth"  boundaries/dependencies
```

Borra los cuatro archivos y confirma `npm run lint` limpio.

### 4.5 El resolutor: la regla que calla

Ahora el caso más traicionero. Quita (comenta) la línea `'import/resolver'` del `eslint.config.js`, recrea el archivo del caso 4.1 y corre `npx eslint .`:

```console
(sin salida)
```

**Sin el resolutor, el plugin no sabe a qué archivo apunta `'../auth'`** (no tiene extensión) y no marca nada: el import malo pasa. Si el lint "pasa" en tu proyecto, comprueba que la regla **de verdad** se ejecuta con un archivo como el del caso 4.1. Restaura la línea y borra el archivo.

---

## Paso 5 - Componer en `app/`

`perfil` muestra el nombre y un botón "Salir"; el nombre lo tiene `auth`.

**Primero, hazlo mal** y mira cómo lo detiene el lint:

```tsx
// src/features/perfil/malo.tsx
import { useSesion } from '../auth';

export function CabeceraConSesion() {
  const sesion = useSesion();
  return <header>Hola, {sesion.data?.nombre}</header>;
}
```

```console
src/features/perfil/malo.tsx
  1:27  error  There is no policy allowing dependencies from elements of type "feature" and captured values: nombre="perfil" to elements of type "feature" and captured values: nombre="auth"  boundaries/dependencies
```

**Ahora hazlo bien.** Borra `malo.tsx` y crea:

```tsx
// src/features/perfil/cabecera.tsx
interface Props {
  nombre: string;
  alSalir: () => void;
}

export function Cabecera({ nombre, alSalir }: Props) {
  return (
    <header>
      <span>Hola, {nombre}</span>
      <button type="button" onClick={alSalir}>
        Salir
      </button>
    </header>
  );
}
```

```ts
// src/features/perfil/index.ts
export { Cabecera } from './cabecera';
```

```tsx
// src/app/layout.tsx
import { Outlet } from 'react-router';
import { useSalir, useSesion } from '../features/auth';
import { Cabecera } from '../features/perfil';

/** La composición entre features vive en app/: aquí se conectan, ninguna importa a la otra. */
export function Layout() {
  const sesion = useSesion();
  const salir = useSalir();
  return (
    <>
      {sesion.data && <Cabecera nombre={sesion.data.nombre} alSalir={() => salir.mutate()} />}
      <Outlet />
    </>
  );
}
```

Y en `app/router.tsx` envuelve la ruta protegida con el `Layout`:

```tsx
import { createBrowserRouter, type RouteObject } from 'react-router';
import { LoginPage, RutaProtegida } from '../features/auth';
import { TicketsPage } from '../features/tickets';
import { Layout } from './layout';

export const rutas: RouteObject[] = [
  { path: '/entrar', element: <LoginPage /> },
  {
    element: <RutaProtegida />,
    children: [{ element: <Layout />, children: [{ path: '/', element: <TicketsPage /> }] }],
  },
];

export const crearRouter = () => createBrowserRouter(rutas);
```

```bash
npm run typecheck && npm run lint && npm test
```

Los tres en verde. `Cabecera` se prueba sola (sin sesión, sin red) y `app/` es el único que sabe de las dos features.

---

## Paso 6 - La capa de dominio

Crea `src/features/tickets/domain.ts`:

```ts
import type { Prioridad, Ticket } from '../../shared/api/contract';

/** El ticket tal como lo necesita la pantalla: no es el JSON de la API, es el modelo de la aplicación. */
export interface TicketVista {
  id: number;
  titulo: string;
  prioridad: Prioridad;
  abierto: boolean;
  creadoEn: Date;
  urgente: boolean;
}

/** Frontera entre la API y la aplicación (capa anticorrupción): el único lugar que conoce la forma del JSON. */
export function aTicketVista(dto: Ticket): TicketVista {
  const abierto = dto.estado === 'abierto';
  return {
    id: dto.id,
    titulo: dto.titulo,
    prioridad: dto.prioridad,
    abierto,
    creadoEn: new Date(dto.creadoEn),
    urgente: abierto && dto.prioridad === 'alta',
  };
}
```

Y su prueba, `domain.test.ts`:

```ts
import { describe, expect, it } from 'vitest';
import type { Ticket } from '../../shared/api/contract';
import { aTicketVista } from './domain';

const base: Ticket = { id: 7, titulo: 'Impresora', prioridad: 'alta', estado: 'abierto', creadoEn: '2026-11-02T15:00:00.000Z' };

describe('aTicketVista', () => {
  it('un ticket abierto de prioridad alta es urgente', () => {
    expect(aTicketVista(base).urgente).toBe(true);
  });
  it('un ticket cerrado ya no es urgente, aunque su prioridad sea alta', () => {
    expect(aTicketVista({ ...base, estado: 'cerrado' }).urgente).toBe(false);
  });
  it('convierte la fecha ISO en Date', () => {
    expect(aTicketVista(base).creadoEn).toEqual(new Date('2026-11-02T15:00:00.000Z'));
  });
});
```

```bash
npx vitest run src/features/tickets/domain.test.ts
#  Test Files  1 passed (1)
#       Tests  3 passed (3)
```

### Conectarlo con `select`

En `use-tickets.ts`, la conversión se hace al **leer** la caché. La función va fuera del hook (una función en línea se ejecuta en cada render):

```ts
import { aTicketVista } from './domain';
import type { PaginaDeTickets } from '../../shared/api/contract';

const aPaginaVista = (pagina: PaginaDeTickets) => ({
  total: pagina.total,
  datos: pagina.datos.map(aTicketVista),
});

export function useTickets(estado: EstadoTicket | 'todos') {
  return useQuery({
    queryKey: ticketsKeys.lista(estado),
    queryFn: ({ signal }) => ticketsApi.listar(estado, signal),
    select: aPaginaVista,
  });
}
```

`tickets-page.tsx` ahora recibe `TicketVista`. `tsc` te dice qué cambiar:

```console
src/features/tickets/tickets-page.tsx(23,45): error TS2339: Property 'estado' does not exist on type 'TicketVista'.
```

Cámbialo para que use `t.abierto` y muestre ` - urgente` cuando `t.urgente`:

```tsx
<li key={t.id}>{t.titulo} ({t.prioridad}, {t.abierto ? 'abierto' : 'cerrado'}){t.urgente && ' - urgente'}</li>
```

Corre todo: `npm run typecheck && npm run lint && npm test`.

---

## Paso 7 - Adoptarlo en un repo con violaciones

Simula un repo que ya tiene una violación. En `eslint.config.js` cambia `'error'` por `'warn'`, crea de nuevo `src/features/tickets/malo.ts` (caso 4.1) y compara:

```bash
npx eslint .                      # advertencia, pero termina con 0
echo $?
npx eslint . --max-warnings 0     # con tope: falla
echo $?
```

```console
x 1 problem (0 errors, 1 warning)
exit sin tope: 0
ESLint found too many warnings (maximum: 0).
exit con tope 0: 1
```

Con `--max-warnings` igual al número **actual** de advertencias, el pipeline se rompe en cuanto alguien agrega una más. Restaura `'error'` y borra el archivo.

---

## Evidencia a entregar

Un `evidencia-s02.md` con:

1. El árbol `src/` antes y después, y las dos respuestas del paso 1 (carpetas que toca "tickets"; archivos que importan `contract.ts`).
2. El historial de commits (`git log --oneline`) de la reorganización.
3. La salida de `npm run typecheck`, `npm run lint` y `npm test` en verde.
4. Los cuatro mensajes del paso 4 y lo que observaste en el caso 4.5.
5. El código de `Cabecera` y `Layout`, y el mensaje del lint que se evitó.
6. Las pruebas de `aTicketVista` y la salida de `vitest`.
7. La salida del paso 7 con los dos códigos de salida.

---

## Si algo no sale

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| `Cannot find module '../features/auth'` | El `index.ts` no existe o no exporta lo que importas | Revisa el `index.ts` de la feature |
| El lint pasa y no debería | Falta `import/resolver`, o el patrón de `boundaries/elements` no coincide | Haz el caso 4.5; revisa `pattern: 'src/features/*'` |
| Pruebas rotas tras mover `tickets-page.test.tsx` | Imports de `../test/...` con ruta vieja | Ajusta a `../../test/render` y `../../test/servidor` |
| `git mv` dice que el destino no existe | Falta `mkdir -p` | Crea la carpeta destino |
