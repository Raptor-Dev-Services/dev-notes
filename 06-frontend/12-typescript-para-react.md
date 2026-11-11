# 12 - TypeScript para React - que el contrato de la API rompa el build

## Problema que resuelve

Un front de React recibe datos de una API que no controla. Si el servidor renombra un campo, un componente escrito con `any` sigue compilando y la persona que usa la pantalla ve `undefined`. TypeScript puede convertir ese descubrimiento en un error de compilación, pero solo si los tipos **salen del contrato** y la **frontera** con el servidor se valida. Esta nota reúne lo mínimo para lograrlo: configuración del compilador, tipos para props, hooks y eventos, uniones discriminadas, genéricos y un esquema único que da tipo y validación.

El código sale de un proyecto de ejemplo (la *Mesa de ayuda*, **ejemplo ilustrativo**) que se compiló con las versiones de noviembre de 2026 indicadas más abajo.

## Los tipos se borran

TypeScript revisa el código al compilar y lo borra: el JavaScript que llega al navegador no tiene `interface` ni anotaciones. Los tipos prueban que **tu código es coherente consigo mismo**. No pueden probar que el servidor dijo la verdad.

La librería de tipos de TypeScript 6.0.3 declara:

```ts
// lib.dom.d.ts
json(): Promise<any>;
// lib.es5.d.ts
parse(text: string, reviver?: ...): any;
```

Un `any` que entra por `fetch` se contagia por el programa y desactiva la revisión. La regla: lo que viene de afuera entra como `unknown` y se comprueba.

### Verlo fallar: un componente con `any`

```tsx
export async function cargarTickets(): Promise<any> {
  const respuesta = await fetch('/api/tickets');
  return respuesta.json();
}

export function ListaDeTickets({ tickets }: { tickets: any }) {
  return (
    <ul>
      {tickets.datos.map((t: any) => (
        <li key={t.id}>
          {t.titulo} ({t.urgencia})
        </li>
      ))}
    </ul>
  );
}
```

El servidor manda `prioridad`, no `urgencia`. Resultado:

```console
$ npx tsc --noEmit
$ echo $?
0
```

Cero errores. La pantalla muestra `(undefined)`. ESLint protesta por los `any` (`@typescript-eslint/no-explicit-any`, incluida en la configuración `recommended`), pero no por el campo.

## Un esquema, dos garantías

Con Zod 4 el tipo se **deriva** del esquema:

```ts
import { z } from 'zod';

export const ticketSchema = z.object({
  id: z.number().int(),
  titulo: z.string(),
  prioridad: z.enum(['baja', 'media', 'alta']),
  estado: z.enum(['abierto', 'cerrado']),
  creadoEn: z.iso.datetime(),
});

export type Ticket = z.infer<typeof ticketSchema>;
```

- Al **compilar**, `Ticket` impide usar un campo que no existe.
- En **ejecución**, `ticketSchema.parse(datos)` comprueba lo que llegó.

```tsx
const datos: unknown = await respuesta.json();
const pagina = paginaDeTicketsSchema.parse(datos);
```

| Candado | Cuándo actúa | Qué atrapa | Qué no atrapa |
|---|---|---|---|
| Tipos | Al escribir y en CI | Un campo renombrado en el contrato que el código aún usa | Que el servidor se desvíe del contrato |
| Esquema | En cada respuesta | El servidor fuera de contrato | Un uso incorrecto del dato ya validado |

Si el servidor devuelve `urgencia` donde el contrato pide `prioridad`:

```console
/api/tickets?contrato=roto -> datos.0.prioridad: Invalid option: expected one of "baja"|"media"|"alta"
```

El mensaje trae la **ruta** del campo y lo que se esperaba.

### Un cliente de API genérico

```ts
export async function pedir<S extends z.ZodType>(
  ruta: string,
  { esquema, metodo = 'GET', cuerpo, signal }: Opciones<S>,
): Promise<z.infer<S>> {
  // fetch, errores HTTP convertidos en ApiError...
  return esquema.parse(datos);
}
```

`S extends z.ZodType` acepta cualquier esquema de Zod. El tipo que devuelve depende del esquema que se pasa: `pedir('/tickets', { esquema: paginaDeTicketsSchema })` devuelve `PaginaDeTickets` sin que nadie lo escriba otra vez.

### Si la API publica OpenAPI

Los tipos se pueden **generar** de la especificación en lugar de escribirlos:

```bash
npx openapi-typescript contract/openapi.yaml -o src/shared/api/schema.d.ts
```

Con `openapi-typescript` 7.13.0 el resultado trae `components["schemas"]["Ticket"]` y los tipos de cada operación. Si se mantienen **dos** descripciones (OpenAPI y Zod), una comprobación de igualdad de tipos avisa cuando se separan:

```ts
type Igual<A, B> = (<T>() => T extends A ? 1 : 2) extends <T>() => T extends B ? 1 : 2 ? true : false;
const mismoTicket: Igual<Ticket, components['schemas']['Ticket']> = true;
```

```console
compatibilidad.ts(6,7): error TS2322: Type 'true' is not assignable to type 'false'.
```

## Configurar el compilador

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "noImplicitOverride": true,
    "verbatimModuleSyntax": true,
    "isolatedModules": true,
    "jsx": "react-jsx",
    "moduleResolution": "bundler",
    "noEmit": true,
    "types": ["vite/client"]
  }
}
```

| Opción | Qué hace | Efecto visible |
|---|---|---|
| `strict` | Activa la familia estricta (`strictNullChecks`, `noImplicitAny`, `useUnknownInCatchVariables`, etc.) | En TypeScript 6.0 ya es `true` por omisión |
| `noUncheckedIndexedAccess` | `lista[0]` es `T \| undefined` | Obliga a decidir qué pasa con la lista vacía |
| `verbatimModuleSyntax` | Los imports de solo tipos llevan `type` | Evita imports que existen solo para los tipos |
| `isolatedModules` | Cada archivo se puede transformar por separado | Compatible con Vite y otras herramientas que borran los tipos |

```console
n.ts(2,42): error TS2532: Object is possibly 'undefined'.          # p.datos[0].titulo
error TS1484: 'Ticket' is a type and must be imported using a type-only import when 'verbatimModuleSyntax' is enabled.
```

> **Qué versión de TypeScript.** A noviembre de 2026 la última en npm es la 7.0.2 (estable desde el 8 de julio de 2026, port nativo con el mismo comando `tsc`). Sin embargo salió **sin API programática**: `typescript-eslint` 8.72.2 declara `typescript >=4.8.4 <6.1.0` y `npm install` se niega con `ERESOLVE` si se instala la 7. Un proyecto con ESLint sobre TypeScript fija la **6.0.3**. En el proyecto de ejemplo, `tsc` de la 7.0.2 también compiló el código sin errores. Revisa estas versiones antes de copiar: cambian.

## Props, hijos y elementos nativos

```tsx
import type { ComponentProps, ReactNode } from 'react';

interface InsigniaProps {
  prioridad: Prioridad;
  children?: ReactNode;
}

export function Insignia({ prioridad, children }: InsigniaProps) {
  return <span data-prioridad={prioridad}>{children ?? prioridad}</span>;
}

type BotonProps = ComponentProps<'button'> & { variante?: 'primario' | 'peligro' };

export function Boton({ variante = 'primario', ...resto }: BotonProps) {
  return <button data-variante={variante} {...resto} />;
}
```

- Las props se tipan en el parámetro; React no pide un tipo especial de componente.
- `ReactNode` abarca todo lo que puede ser hijo. `ReactElement` solo acepta elementos.
- `ComponentProps<'button'>` da las props de un `<button>` nativo. Desde React 19, `ref` es una prop más de los componentes de función, así que no hace falta `forwardRef` en los nuevos.

## Hooks y eventos

```ts
const [abierto, setAbierto] = useState(false);            // boolean, por inferencia
const [filtro, setFiltro] = useState<Filtro>('todos');    // sin el genérico sería string
const [ticket, setTicket] = useState<Ticket | null>(null);
```

Un hook propio no necesita anotar su retorno si todo lo que usa ya está tipado:

```ts
export function useTickets(estado: EstadoTicket | 'todos') {
  return useQuery({
    queryKey: ticketsKeys.lista(estado),
    queryFn: ({ signal }) => ticketsApi.listar(estado, signal),
  });
}
```

El tipo de `consulta.data` viene de `ticketsApi.listar`, que viene del esquema. Un cambio en el esquema llega hasta el componente.

Para un contexto sin valor por omisión útil:

```tsx
const SesionContext = createContext<Usuario | null>(null);

export function useUsuario(): Usuario {
  const usuario = useContext(SesionContext);
  if (!usuario) throw new Error('useUsuario se usa dentro de <SesionProvider>');
  return usuario;
}
```

*Fragmento ilustrativo: la Mesa de ayuda no usa este contexto.* Quien consume `useUsuario()` recibe un `Usuario`, nunca `null`.

Los eventos llevan el tipo del elemento (`ChangeEvent<HTMLSelectElement>`). Si el manejador va dentro del JSX, TypeScript lo infiere; si se saca a una función, hay que escribirlo.

## Uniones discriminadas

Tres booleanos sueltos (`cargando`, `error`, `datos`) permiten ocho combinaciones y solo tres tienen sentido. Una unión discriminada solo deja construir las que existen:

```ts
export type Vista<T> =
  | { estado: 'cargando' }
  | { estado: 'error'; mensaje: string }
  | { estado: 'listo'; datos: T };

function assertNever(x: never): never {
  throw new Error(`Caso no previsto: ${JSON.stringify(x)}`);
}

export function describir(v: Vista<Ticket[]>): string {
  switch (v.estado) {
    case 'cargando':
      return 'Cargando...';
    case 'error':
      return `Error: ${v.mensaje}`;
    case 'listo':
      return `${v.datos.length} tickets`;
    default:
      return assertNever(v);
  }
}
```

Al comprobar `estado`, TypeScript sabe qué campos hay en cada rama. Si se quita un caso:

```console
ejemplos.tsx(38,26): error TS2345: Argument of type '{ estado: "listo"; datos: ...[]; }' is not assignable to parameter of type 'never'.
```

El mismo patrón sirve para las acciones de un `useReducer`:

```ts
type Accion =
  | { tipo: 'filtrar'; filtro: Filtro }
  | { tipo: 'pagina'; numero: number }
  | { tipo: 'reiniciar' };
```

## Genéricos con restricción

```tsx
interface ListaProps<T> {
  elementos: readonly T[];
  clave: (elemento: T) => string | number;
  render: (elemento: T) => ReactNode;
}

export function Lista<T>({ elementos, clave, render }: ListaProps<T>) {
  return (
    <ul>
      {elementos.map((e) => (
        <li key={clave(e)}>{render(e)}</li>
      ))}
    </ul>
  );
}

<Lista elementos={tickets} clave={(t) => t.id} render={(t) => t.titulo} />
```

`T` se infiere de `elementos`: en `clave` y `render`, `t` es un `Ticket`. Una restricción (`S extends z.ZodType`) pone un piso sin cerrar la función a un solo tipo.

### `satisfies`, utilidades y tipos opacos

```ts
export const ETIQUETAS = {
  baja: 'Baja',
  media: 'Media',
  alta: 'Alta',
} satisfies Record<Prioridad, string>;

export type ResumenDeTicket = Pick<Ticket, 'id' | 'titulo'>;
export type CambioDeTicket = Partial<Omit<Ticket, 'id' | 'creadoEn'>>;
```

`satisfies` (TypeScript 4.9) comprueba la forma sin ensanchar el tipo. Si se agrega `'critica'` al contrato, `ETIQUETAS` deja de compilar hasta que se complete, y también cualquier `Record<Prioridad, ...>` del código. Los tipos de utilidad **derivan** del contrato; copiarlos a mano es otra forma de repetirlo.

Un **tipo opaco** distingue dos números iguales por dentro:

```ts
type Marca<T, M extends string> = T & { readonly __marca: M };
export type TicketId = Marca<number, 'TicketId'>;
export type UsuarioId = Marca<number, 'UsuarioId'>;
export const buscarTicket = (id: TicketId) => `/tickets/${id}`;
```

```console
m.ts(3,14): error TS2345: Argument of type 'UsuarioId' is not assignable to parameter of type 'TicketId'.
```

Cada marca necesita un punto donde se crea con `as`. Úsalo donde mezclar dos ids sea un riesgo real.

## Errores típicos

| Síntoma | Causa | Arreglo |
|---|---|---|
| El campo cambió y nada falló | Hay `any` en la ruta del dato | `unknown` en la frontera y un esquema |
| `valor as Tipo` por todos lados | El `as` calla al compilador, no comprueba | Una guarda de tipo (`valor is Tipo`) o un esquema |
| `leer<Ticket>(dato)` | Un genérico que solo aparece en el retorno es un `as` disfrazado | Pasar la prueba: `leer(dato, esquema)` |
| `lista[0].titulo` falla con `noUncheckedIndexedAccess` | La lista puede estar vacía | `lista[0]?.titulo ?? ...` |
| `error TS1484` | Import de un tipo sin `type` con `verbatimModuleSyntax` | `import type { Ticket }` |
| Tres booleanos de estado | Combinaciones imposibles | Unión discriminada |
| `// @ts-ignore` | Nunca avisa cuando ya no hace falta | `// @ts-expect-error` con el motivo |
| `npm install` falla con `ERESOLVE` al subir TypeScript | `typescript-eslint` aún no admite esa versión | Fijar TypeScript 6.0.3 |

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| El front consume una API de otro equipo o que cambia | Un prototipo de una tarde que se va a tirar |
| Hay más de una persona en el repo | Un script de una sola vez |
| El contrato ya existe (OpenAPI, esquemas) | Tipos tan elaborados que nadie del equipo los entiende |
| Se quiere que el cambio de la API falle en CI | Marcas y genéricos en todo: cada uno cuesta lectura |

## Versiones de este documento (noviembre de 2026)

React 19.3.1, TypeScript 6.0.3 (la 7.0.2 es la última en npm), Zod 4.6.7, typescript-eslint 8.72.2, `openapi-typescript` 7.13.0, Vite 8.3.8. Fuente: registro de npm, 11 de noviembre de 2026.

> Fuente: *Learn React with TypeScript*, 3.ª ed. (Carl Rippon): caps. 1 a 3. *TypeScript 5 Design Patterns and Best Practices*, 2.ª ed. (Theofanis Despoudis): caps. 1 y 2. Documentación: react.dev, *Using TypeScript*; *TypeScript Handbook*, *Narrowing* y *Utility Types*; *Announcing TypeScript 6.0* y *7.0* (devblogs.microsoft.com/typescript); zod.dev.

---

*Rogelio Arriaga Gonzalez*
