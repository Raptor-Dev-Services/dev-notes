<!-- _class: lead -->

# S01 - TypeScript para React

### React en producción con TypeScript

**Rogelio Arriaga - Raptor Dev Services**

<!--
Bienvenida. Antes de empezar, cada quien confirma Node 22.12 o superior (node -v) y clona o copia el proyecto de la carpeta referencia/mesa-de-ayuda.
Si alguien no instala en la apertura, se resuelve ahí y no a la mitad de la práctica.
-->

---

## Agenda del día

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, objetivos, el proyecto del curso | 25 min |
| Parte 1 | Lo que TypeScript hace, y lo que no, con tu API | 45 min |
| Parte 2 | Props, hooks y eventos bien tipados | 55 min |
| Parte 3 | Uniones discriminadas y genéricos | 60 min |
| Parte 4 | Práctica: el contrato de la API manda | 80 min |
| Cierre | Errores típicos, tarea y quiz | 35 min |

<!-- Hay un descanso de 10 minutos entre la Parte 2 y la Parte 3, y otro a la mitad de la práctica. -->

---

## El lema de la sesión

> Los tipos del contrato de la API son la primera prueba del front.

- Una prueba que corre en cada guardado, sin que nadie la escriba dos veces.
- Si el servidor cambia un campo, lo ideal es que falle **tu build**, no la pantalla del usuario.
- Hoy aprendemos a lograrlo, **y a ver cuándo TypeScript solo aparenta protegerte**.

<!-- Pregunta al grupo: ¿quién ha visto "undefined" impreso en una pantalla porque el backend renombró un campo? Casi todos. Esa es la razón de la sesión. -->

---

## Al terminar puedes

- Explicar qué se borra de tu código al compilar y por qué eso limita lo que TypeScript puede prometer.
- Tipar componentes, hooks, eventos y reducers sin `any`.
- Modelar los estados de una pantalla con **uniones discriminadas** y comprobar que no falta ninguno.
- Escribir una función o un componente **genérico** con una restricción útil.
- Derivar los tipos del contrato desde un solo esquema y validarlo en la frontera con la API.
- Hacer que un cambio en el contrato rompa el build.

---

## Cómo trabajamos en este curso

- **Un proyecto de ejemplo** durante las seis sesiones: la *Mesa de ayuda*, una aplicación pequeña de tickets. *Ejemplo ilustrativo*: sus datos y personas no existen.
- Todo el código que se ve en clase se corre; el que es solo un fragmento lo dice.
- **La regla del curso:** si no lo vieron fallar, no lo probaron. Cada práctica tiene un paso "Verlo fallar".
- Las versiones son las de **noviembre de 2026**. Cambian: cada vez que se cita una, lleva su fuente.

```bash
node -v        # v22.12 o superior (Vitest 5 y Vite 8 lo piden)
npm -v
```

<!-- Vite 8.3.8 declara node ^20.19.0 || >=22.12.0; Vitest 5.1.0 declara ^22.12.0 || ^24.0.0 || >=26.0.0; react-router 8.5.1 declara >=22.22.0 (metadatos de npm, 2026-11-11). Por eso pedimos Node 22.22 o superior. -->

---

## Las versiones del curso (a noviembre de 2026)

| Pieza | Versión | De dónde sale |
|---|---|---|
| React y React DOM | 19.3.1 | `npm view react version` |
| TypeScript | 6.0.3 | ver la lámina siguiente |
| Vite | 8.3.8 | `npm view vite version` |
| React Router | 8.5.1 | `npm view react-router version` |
| TanStack Query | 5.107.1 | `npm view @tanstack/react-query version` |
| Zod | 4.6.7 | `npm view zod version` |
| Vitest | 5.1.0 | `npm view vitest version` |
| ESLint y typescript-eslint | 10.14.0 y 8.72.2 | `npm view` de cada paquete |

Fuente: registro de npm, consultado el 11 de noviembre de 2026.

<!-- No hace falta memorizar la tabla: el package.json del proyecto de referencia las fija. Lo que sí hay que aprender es consultarlas, porque este documento envejece. -->

---

## ¿Por qué TypeScript 6.0 y no el 7?

- **TypeScript 6.0** salió el 23 de marzo de 2026. Sus autores dijeron que sería el último basado en el código JavaScript del compilador.
- **TypeScript 7.0** es estable desde el 8 de julio de 2026: es un port nativo, escrito en Go, y el comando sigue siendo `tsc`.
- La 7.0 salió **sin API programática** (se espera una nueva en la 7.1). Las herramientas que importan `typescript` por dentro, como typescript-eslint, siguen necesitando la 6.
- `typescript-eslint` 8.72.2 declara el rango `typescript >=4.8.4 <6.1.0`: con la 7.0.2 `npm install` se niega.

Fuente: *Announcing TypeScript 6.0* y *Announcing TypeScript 7.0* (devblogs.microsoft.com/typescript); metadatos de `typescript-eslint@8.72.2` en npm, 11 de noviembre de 2026.

<!-- Esto es parte de la lección: una herramienta nueva puede ser mejor y aun así no ser la que tu cadena soporta. Se prueba con npm install, no con el titular. En el proyecto de referencia, tsc de la 7.0.2 también compiló el código sin errores. -->

---

<!-- _class: lead -->

# Parte 1
## Lo que TypeScript hace, y lo que no, con tu API

---

## Los tipos se borran

TypeScript revisa tu código al **compilar** y después lo borra. En el navegador no hay tipos:

```ts {1-4|6-8}
interface Ticket {
  id: number;
  titulo: string;
}

// Lo que llega al navegador:
// (nada). Solo JavaScript.
const t = { id: 1, titulo: 'Hola' };
```

- Un `interface` no genera código. No hay nada que ejecutar en cada petición.
- Por eso los tipos prueban que **tu código es coherente consigo mismo**, no que **el servidor te dijo la verdad**.

<!-- Esta es la idea central de la sesión. Si hay un solo mensaje que se lleven: la frontera entre tu programa y el mundo exterior es donde los tipos no alcanzan. -->

---

## Dónde la mentira entra: la frontera

`Response.json()` está declarado así en la librería de TypeScript (`lib.dom.d.ts`, TS 6.0.3):

```ts
json(): Promise<any>;
```

Y `JSON.parse` (`lib.es5.d.ts`):

```ts
parse(text: string, reviver?: ...): any;
```

- `any` desactiva la revisión: se puede hacer **cualquier cosa** con el valor.
- Un `any` que entra por `fetch` se queda en tu programa y se contagia.

Fuente: archivos `lib.dom.d.ts` y `lib.es5.d.ts` del paquete `typescript@6.0.3`.

<!-- Se pueden abrir en node_modules/typescript/lib para verlos con los propios ojos. -->

---

## Demostración: un cambio de contrato que no rompe nada

Tenemos un componente con `any`, como lo escribe quien tiene prisa:

```tsx {1-4|6-16}
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

El servidor manda `prioridad`, no `urgencia`.

---

## Resultado: el compilador no dice nada

```console
$ npx tsc --noEmit
$ echo $?
0
```

Y en pantalla sale `La impresora del piso 2 no responde (undefined)`.

- Cero errores, y el campo está mal desde la primera línea.
- ESLint sí protesta, pero por otra razón:

```console
$ npx eslint src/practica/lista-any.tsx
  1:48  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any
  6:56  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any
  9:30  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any
```

Fuente: salida real del proyecto de referencia (TypeScript 6.0.3, typescript-eslint 8.72.2).

<!-- La regla no-explicit-any está en la configuración recommended de typescript-eslint. Es una red de seguridad para el equipo, no para el contrato. -->

---

## La solución: una sola fuente para el contrato

```ts {1|3-9|11-12|14-16}
import { z } from 'zod';

export const ticketSchema = z.object({
  id: z.number().int(),
  titulo: z.string(),
  prioridad: z.enum(['baja', 'media', 'alta']),
  estado: z.enum(['abierto', 'cerrado']),
  creadoEn: z.iso.datetime(),
});

// El tipo sale del esquema: no se escribe dos veces
export type Ticket = z.infer<typeof ticketSchema>;

// En la frontera, el esquema valida lo que llegó
const datos: unknown = await respuesta.json();
const ticket = ticketSchema.parse(datos);
```

- `unknown` obliga a comprobar antes de usar. `any` no.
- `z.infer` evita que el tipo y la validación se separen con el tiempo.

Fuente: documentación de Zod 4 (zod.dev/basics), secciones *Defining a schema*, *Parsing data* e *Inferring types*.

---

## Lo que `parse` hace cuando el servidor cambia

```console
ZodError
[
  {
    "code": "invalid_value",
    "values": ["baja", "media", "alta"],
    "path": ["urgencia"],
    "message": "Invalid option: expected one of \"baja\"|\"media\"|\"alta\""
  }
]
```

- El error dice **qué campo** y **qué se esperaba**, en la capa de datos y no tres componentes más abajo.
- Es un error en ejecución: ocurre cuando alguien usa la pantalla. El compilador lo detecta antes; Zod lo detecta cuando el compilador ya no puede.
- Las dos defensas se complementan.

<!-- Salida real con zod 4.6.7 al validar {id:1, titulo:'Hola', prioridad:'alta'} contra un esquema que pide 'urgencia'. -->

---

## Los dos candados

| Candado | Cuándo actúa | Qué atrapa | Qué no atrapa |
|---|---|---|---|
| **Tipos** (compilador) | Al escribir y en CI | Un campo renombrado en el contrato que tu código todavía usa | Que el servidor mande algo distinto al contrato |
| **Esquema** (Zod en la frontera) | En ejecución, por cada respuesta | El servidor que se salió del contrato | Un uso incorrecto del dato ya validado |

- Con **solo tipos**: pantalla con `undefined`.
- Con **solo esquema**: error en tiempo de ejecución en cada uso, aunque el equipo ya sepa del cambio.
- Con **los dos**: el cambio se ve al compilar y, si el servidor se desvía, en el log.

---

## Configurar el compilador para que sirva

```json {4-5|6-8|9-12}
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

- `strict` activa la familia estricta: `strictNullChecks`, `noImplicitAny`, `useUnknownInCatchVariables` y otras. En TypeScript 6.0 ya es `true` por omisión.
- `noUncheckedIndexedAccess` **no** entra en `strict` y vale la pena activarlo.

Fuente: *TSConfig Reference* (typescriptlang.org/tsconfig); *Announcing TypeScript 6.0*.

---

## `noUncheckedIndexedAccess`: el primer elemento no siempre existe

```ts
export const primero = (p: PaginaDeTickets) => p.datos[0].titulo;
```

```console
n.ts(2,42): error TS2532: Object is possibly 'undefined'.
```

- Con la opción activa, `lista[0]` es `Ticket | undefined`.
- Te obliga a decidir qué pasa con una lista vacía, que es justo el caso que se olvida.

```ts
export const primero = (p: PaginaDeTickets) => p.datos[0]?.titulo ?? 'Sin tickets';
```

---

## `verbatimModuleSyntax`: separa los tipos de los valores

```ts
import { Ticket } from '../shared/api/contract';
```

```console
error TS1484: 'Ticket' is a type and must be imported using a type-only import
when 'verbatimModuleSyntax' is enabled.
```

```ts
import type { Ticket } from '../shared/api/contract';
// o, mezclando valores y tipos:
import { ticketSchema, type Ticket } from '../shared/api/contract';
```

- Lo que lleva `type` se borra por completo; lo que no, se conserva tal cual.
- Evita imports que solo existen para los tipos y que arrastran código al bundle sin que nadie lo note.

<!-- Salida real, TypeScript 6.0.3. -->

---

## Error típico 1: el `as` que miente

El código de `tickets-page.tsx` del proyecto, tal como está:

```tsx
<select
  id="filtro"
  value={estado}
  onChange={(e) => setEstado(e.target.value as EstadoTicket | 'todos')}
>
```

- `e.target.value` es un `string` cualquiera. El `as` le dice al compilador "confía en mí" y **no comprueba nada**.
- Si alguien agrega una opción `<option value="pendiente">` y se olvida del tipo, compila, y el estado queda con un valor imposible.
- Una aserción no convierte, no valida y no protege: solo calla al compilador.

---

## La guarda de tipo: comprobar de verdad

```ts
type Filtro = EstadoTicket | 'todos';
const FILTROS = ['todos', 'abierto', 'cerrado'] as const;

export const esFiltro = (valor: string): valor is Filtro =>
  (FILTROS as readonly string[]).includes(valor);
```

```tsx
onChange={(e) => {
  if (esFiltro(e.target.value)) setEstado(e.target.value);
}}
```

- `valor is Filtro` es un **predicado de tipo**: si devuelve `true`, TypeScript estrecha el tipo.
- La comprobación existe en ejecución; el tipo se apoya en ella.

Fuente: *TypeScript Handbook, Narrowing*, sección *Using type predicates* (typescriptlang.org).

---

## Resumen de la Parte 1

- Los tipos se borran: no protegen la **frontera** con el servidor.
- `fetch().json()` y `JSON.parse` devuelven `any`: se reciben como `unknown` y se validan.
- Un esquema da **tipo y validación** desde una sola definición (`z.infer`).
- `strict`, `noUncheckedIndexedAccess` y `verbatimModuleSyntax` convierten descuidos en errores de compilación.
- Un `as` no comprueba. Una guarda de tipo sí.

---

<!-- _class: lead -->

# Parte 2
## Props, hooks y eventos bien tipados

---

## Props: una `interface` y `children` con `ReactNode`

```tsx {1-4|6-8}
interface InsigniaProps {
  prioridad: Prioridad;
  children?: ReactNode;
}

export function Insignia({ prioridad, children }: InsigniaProps) {
  return <span data-prioridad={prioridad}>{children ?? prioridad}</span>;
}
```

- La documentación de React tipa las props directamente en el parámetro de la función, sin un tipo especial de componente.
- `ReactNode` es la unión de todo lo que puede ir de hijo: texto, números, elementos, `null`, listas.
- `ReactElement` solo acepta elementos JSX. TypeScript no puede exigir que los hijos sean de un tipo concreto.

Fuente: react.dev, *Using TypeScript*, secciones *Typing props* y *Children*.

---

## Extender las props de un elemento nativo

```tsx
import type { ComponentProps } from 'react';

type BotonProps = ComponentProps<'button'> & { variante?: 'primario' | 'peligro' };

export function Boton({ variante = 'primario', ...resto }: BotonProps) {
  return <button data-variante={variante} {...resto} />;
}
```

- `ComponentProps<'button'>` trae `onClick`, `disabled`, `type` y todas las props de un `<button>`.
- Un botón del design system queda con **las mismas props** que el nativo, más las suyas.
- En React 19 `ref` es una prop más de los componentes de función: ya no hace falta `forwardRef` para componentes nuevos.

Fuente: react.dev, *React 19* (5 de diciembre de 2024): "Starting in React 19, you can now access `ref` as a prop for function components".

---

## Hooks: la inferencia hace casi todo

```ts
const [abierto, setAbierto] = useState(false);           // boolean
const [filtro, setFiltro] = useState<Filtro>('todos');   // sin <Filtro> sería string
const [ticket, setTicket] = useState<Ticket | null>(null);
```

- Con un valor inicial, el tipo se infiere. Con una unión o con `null`, hay que decirlo.
- Con `useState<Ticket | null>(null)` el compilador te obliga a manejar el `null` antes de leer `ticket.titulo`.

Fuente: react.dev, *Using TypeScript*, sección *useState*.

---

## `useReducer`: las acciones como unión discriminada

```ts {1-5|6-9|11-20}
type Filtro = EstadoTicket | 'todos';
type Accion =
  | { tipo: 'filtrar'; filtro: Filtro }
  | { tipo: 'pagina'; numero: number }
  | { tipo: 'reiniciar' };
interface EstadoTabla {
  filtro: Filtro;
  pagina: number;
}

export function reductor(estado: EstadoTabla, accion: Accion): EstadoTabla {
  switch (accion.tipo) {
    case 'filtrar':
      return { filtro: accion.filtro, pagina: 1 };
    case 'pagina':
      return { ...estado, pagina: accion.numero };
    case 'reiniciar':
      return { filtro: 'todos', pagina: 1 };
  }
}
```

- Dentro de cada `case`, `accion` ya sabe qué campos trae: `accion.numero` solo existe en `'pagina'`.
- Si agregas una acción nueva y olvidas el `case`, la función deja de devolver `EstadoTabla` y el compilador lo marca.

Fuente: react.dev, *Using TypeScript*, sección *useReducer*.

---

## Eventos: el tipo lo da el elemento

```tsx
import type { ChangeEvent } from 'react';

function manejarCambio(evento: ChangeEvent<HTMLSelectElement>) {
  const valor = evento.currentTarget.value;
  // ...
}
```

- Si el manejador va **dentro** del JSX, TypeScript infiere el tipo. Si lo sacas a una función, hay que escribirlo.
- `React.SyntheticEvent` es la base de todos los eventos, por si no encuentras el específico.
- El tipo exacto se descubre con el cursor sobre el `onChange` de un elemento en el editor.

Fuente: react.dev, *Using TypeScript*, sección *DOM Events*.

---

## `useContext`: el contexto sin valor por omisión útil

```tsx
const SesionContext = createContext<Usuario | null>(null);

export function useUsuario(): Usuario {
  const usuario = useContext(SesionContext);
  if (!usuario) throw new Error('useUsuario se usa dentro de <SesionProvider>');
  return usuario;
}
```

- El contexto vale `null` fuera de su proveedor; el hook lo convierte en un error que dice qué pasó.
- Quien consume `useUsuario()` recibe un `Usuario`, nunca `null`: se acabaron los `?.` por todas partes.
- *Fragmento ilustrativo*: la Mesa de ayuda no usa este contexto; su sesión vive en la caché de TanStack Query (sesión 4).

Fuente: react.dev, *Using TypeScript*, sección *useContext*.

---

## Un hook propio con retorno claro

```ts
export function useTickets(estado: EstadoTicket | 'todos') {
  return useQuery({
    queryKey: ticketsKeys.lista(estado),
    queryFn: ({ signal }) => ticketsApi.listar(estado, signal),
  });
}
```

- No se anotó el tipo de retorno: viene de `ticketsApi.listar`, que viene del esquema.
- `consulta.data` es `PaginaDeTickets | undefined`; tras `consulta.isPending === false` y sin error, `PaginaDeTickets`.
- **Un cambio en el esquema llega hasta el componente sin que nadie lo escriba.**

<!-- Este es el hilo que une la sesión: esquema -> api -> hook -> componente. Nadie repitió el tipo en cuatro lugares. -->

---

## Resumen de la Parte 2

- Props con `interface`, `ReactNode` para hijos, `ComponentProps<'elemento'>` para extender un nativo.
- `useState` infiere; con uniones o `null` se escribe el genérico.
- `useReducer` con acciones como unión discriminada: el `switch` conoce los campos de cada caso.
- Eventos con `ChangeEvent<HTMLElemento>` cuando el manejador sale del JSX.
- Los tipos fluyen del esquema al componente: no se repiten.

<!-- Descanso de 10 minutos. -->

---

<!-- _class: lead -->

# Parte 3
## Uniones discriminadas y genéricos

---

## El problema de los booleanos sueltos

```tsx
const [cargando, setCargando] = useState(true);
const [error, setError] = useState<string | null>(null);
const [datos, setDatos] = useState<Ticket[] | null>(null);
```

Tres estados independientes permiten **ocho combinaciones**; solo tres tienen sentido:

| `cargando` | `error` | `datos` | ¿Existe? |
|---|---|---|---|
| true | null | null | Sí, cargando |
| false | texto | null | Sí, error |
| false | null | lista | Sí, listo |
| true | texto | lista | **No**, pero el tipo lo permite |
| false | null | null | **No**: ¿vacío o todavía no pidió? |

- Cada componente inventa su forma de desempatar. Ahí nacen las pantallas en blanco.

---

## Unión discriminada: solo lo que existe puede existir

```ts
export type Vista<T> =
  | { estado: 'cargando' }
  | { estado: 'error'; mensaje: string }
  | { estado: 'listo'; datos: T };
```

- Un campo común (`estado`) con valores literales es el **discriminante**.
- Al comprobarlo, TypeScript sabe qué más hay: dentro de `'listo'`, `datos` existe; dentro de `'error'`, no.
- Los estados imposibles **no se pueden construir**.

Fuente: *TypeScript Handbook, Narrowing*: "When every type in a union contains a common property with literal types, TypeScript considers that to be a *discriminated union*".

---

## Exhaustividad: que el compilador cuente los casos

```ts {1-3|5-16}
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

- En el `default` ya no queda ningún caso posible: `v` es de tipo `never`.
- Si agregas `{ estado: 'vacio' }` a la unión y no lo manejas, `v` **ya no es `never`** y el compilador lo marca.

Fuente: *TypeScript Handbook, Narrowing*, sección *The never type and exhaustiveness checking*.

---

## Verlo fallar: quitar un caso

Quitamos el `case 'listo'` y compilamos:

```console
ejemplos.tsx(38,26): error TS2345: Argument of type
'{ estado: "listo"; datos: { id: number; titulo: string; ... }[]; }'
is not assignable to parameter of type 'never'.
```

- El mensaje señala **el caso que falta**: `estado: "listo"`.
- Sin `assertNever`, la función devolvería `undefined` sin avisar. Con `strict`, el error de "no todas las rutas devuelven valor" aparece solo si el tipo de retorno lo exige.

<!-- Salida real, TypeScript 6.0.3. -->

---

## Genéricos: una función para muchos tipos

Un componente de lista que no sabe nada de tickets:

```tsx {1-5|7-15|17}
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

- `T` se **infiere** de `elementos`: en `clave` y `render`, `t` es un `Ticket`, sin escribirlo.
- Se usa con tickets, usuarios o cualquier cosa, y nunca pierde el tipo.

---

## Restricciones: `extends` pone el piso

La función de la capa de API del proyecto:

```ts {1-4|5-7|9-10}
export async function pedir<S extends z.ZodType>(
  ruta: string,
  { esquema, metodo = 'GET', cuerpo, signal }: Opciones<S>,
): Promise<z.infer<S>> {
  // ...
  return esquema.parse(datos);
}

const pagina = await pedir('/tickets', { esquema: paginaDeTicketsSchema });
//    ^? PaginaDeTickets
```

- `S extends z.ZodType` dice "acepto cualquier esquema de Zod, y nada más".
- El tipo de retorno depende del esquema que pasaste: **un cliente de API que no sabe qué endpoints existen y aun así devuelve el tipo correcto**.

---

## `satisfies`: validar la forma sin perder el detalle

```ts
export const ETIQUETAS = {
  baja: 'Baja',
  media: 'Media',
  alta: 'Alta',
} satisfies Record<Prioridad, string>;
```

Si falta una prioridad:

```console
error TS1360: Type '{ baja: string; media: string; }' does not satisfy the expected type
'Record<"baja" | "media" | "alta", string>'.
  Property 'alta' is missing in type '{ baja: string; media: string; }' but required ...
```

- Agregas `'critica'` al contrato: este objeto **deja de compilar** hasta que lo completas.
- `satisfies` apareció en TypeScript 4.9 (15 de noviembre de 2022).

Fuente: *Announcing TypeScript 4.9*, devblogs.microsoft.com/typescript.

---

## Tipos de utilidad: derivar en lugar de copiar

```ts
export type ResumenDeTicket = Pick<Ticket, 'id' | 'titulo'>;
export type CambioDeTicket = Partial<Omit<Ticket, 'id' | 'creadoEn'>>;
```

| Utilidad | Hace |
|---|---|
| `Pick<T, K>` | Se queda solo con las claves `K` |
| `Omit<T, K>` | Quita las claves `K` |
| `Partial<T>` | Vuelve opcionales todas las claves |
| `Record<K, V>` | Un objeto con las claves `K` y valores `V` |
| `ReturnType<F>` | El tipo que devuelve la función `F` |
| `Awaited<T>` | El tipo que entrega una promesa |

- Si `Ticket` cambia, estos tipos cambian con él. Copiarlos a mano es otra forma de repetir el contrato.

Fuente: *TypeScript Handbook, Utility Types* (typescriptlang.org/docs/handbook/utility-types.html).

---

## Tipos opacos: dos números que no se confunden

```ts
type Marca<T, M extends string> = T & { readonly __marca: M };
export type TicketId = Marca<number, 'TicketId'>;
export type UsuarioId = Marca<number, 'UsuarioId'>;

export const buscarTicket = (id: TicketId) => `/tickets/${id}`;
```

```console
m.ts(3,14): error TS2345: Argument of type 'UsuarioId' is not assignable to parameter of type 'TicketId'.
m.ts(4,14): error TS2345: Argument of type 'number' is not assignable to parameter of type 'TicketId'.
```

- Para el compilador ambos son `number`, pero con una marca que solo existe en tiempo de compilación.
- Útil cuando mezclar dos ids sería un bug serio. **No lo pongas en todo**: cada marca exige un punto donde se crea, con `as`, y ahí vuelve el acto de fe.

Es un patrón de la comunidad, no una característica del lenguaje. Fuente: ejemplo compilado con TypeScript 6.0.3 (los errores son la salida real del compilador).

<!-- Aquí podría hablarse de "nominal typing". TypeScript es estructural: dos tipos con la misma forma son intercambiables. La marca es el truco para simular lo contrario. Hay que decir que el curso no lo usa en el proyecto. -->

---

## Error típico 2: el genérico que no restringe nada

```ts
function primero<T>(lista: T[]): T { return lista[0]; }      // sin noUncheckedIndexedAccess compila
function leer<T>(dato: unknown): T { return dato as T; }     // el as miente, de nuevo
```

- `leer<Ticket>(...)` suena a "leer un `Ticket`" y no comprueba nada: es un `as` con otra cara.
- Un genérico que **solo aparece en el retorno** casi siempre es esto.
- La alternativa honesta recibe la prueba: `leer(dato, esquema)`.

---

## Error típico 3: `!` y `@ts-ignore`

```ts
const raiz = document.getElementById('root')!;   // "te prometo que existe"
// @ts-ignore
renderizar(raiz);
```

- `!` borra el `null`. A veces es razonable: `index.html` es tuyo y `root` existe. Que sea una **decisión**, no un hábito.
- `@ts-ignore` calla los errores de la línea siguiente y **nunca avisa** cuando ya no hace falta.
- Si hay que callar uno, mejor `@ts-expect-error` con un motivo: si el error desaparece, TypeScript reporta que la directiva sobra (`Unused '@ts-expect-error' directive`).

Fuente: *TypeScript 3.9 release notes* (typescriptlang.org/docs/handbook/release-notes/typescript-3-9.html), donde apareció `// @ts-expect-error`.

---

## Del contrato a mano al contrato generado

Hasta aquí el esquema lo escribimos nosotros. Si la API publica **OpenAPI**, los tipos se pueden generar:

```bash
npx openapi-typescript contract/openapi.yaml -o src/shared/api/schema.d.ts
```

```ts
export interface components {
    schemas: {
        Ticket: {
            id: number;
            titulo: string;
            prioridad: components["schemas"]["Prioridad"];
            // ...
```

- Con la versión 7.13.0 de `openapi-typescript` (febrero de 2026) se generó el archivo del proyecto a partir de `contract/openapi.yaml`.
- *React Application Architecture for Production* (Alickovic, 2026), cap. 5, usa esta idea: tipos y esquemas de validación **generados** desde la especificación.

---

## Que los dos contratos no se separen

```ts
import type { components } from '../shared/api/schema';
import type { Ticket } from '../shared/api/contract';

type Igual<A, B> = (<T>() => T extends A ? 1 : 2) extends <T>() => T extends B ? 1 : 2 ? true : false;
const mismoTicket: Igual<Ticket, components['schemas']['Ticket']> = true;
```

Si el esquema de Zod y el OpenAPI describen cosas distintas:

```console
compatibilidad.ts(6,7): error TS2322: Type 'true' is not assignable to type 'false'.
```

- Una prueba de **tipos**: no corre nada, solo compila.
- Es un truco conocido y algo críptico; sirve para ver la idea. Con un contrato generado, el truco ya no hace falta.

---

## Resumen de la Parte 3

- Una unión discriminada modela los estados **sin combinaciones imposibles**.
- `assertNever` y `satisfies` hacen que olvidar un caso sea error de compilación.
- Un genérico con restricción (`extends`) da funciones reutilizables que no pierden el tipo.
- Los tipos de utilidad derivan; no copian.
- Un genérico que solo aparece en el retorno, `as` y `!` son actos de fe: úsalos como decisión.

---

<!-- _class: lead -->

# Parte 4
## Práctica: el contrato de la API manda

---

## Qué vamos a hacer (80 minutos)

1. **Ver que no falla.** Cambiar un campo del servidor con el componente lleno de `any`.
2. **Tipar** el componente y el cliente de API con el contrato.
3. **Verlo fallar.** Renombrar un campo en el contrato y leer la lista de errores del compilador.
4. **Verlo fallar en ejecución.** Hacer que el servidor se salga del contrato y leer el error de Zod.
5. **Quitar** los `any` de un módulo con la regla de lint como guía.

El enunciado completo, con los comandos y las salidas esperadas, está en `practica.md`.

<!-- Reparte a los equipos por parejas. Sugiere que uno escriba y otro lea el error en voz alta: leer el mensaje de TypeScript completo es la habilidad que más cuesta. -->

---

## Cómo se lee un error de TypeScript

```console
lista-tipada.tsx(14,26): error TS2339: Property 'urgencia' does not exist on type
'{ id: number; titulo: string; prioridad: "baja" | "media" | "alta"; ... }'.
```

1. **Dónde:** archivo, línea y columna.
2. **Código:** `TS2339` (propiedad que no existe); sirve para buscar en la documentación.
3. **Qué esperaba:** el tipo del objeto, con sus campos reales. Lo que buscas suele estar en esa lista.
4. Si el mensaje es largo, **lee primero la última línea**: ahí suele estar la causa.

---

## Al terminar la práctica debes poder mostrar

- El compilador con `0` errores cuando el componente tenía `any` y el campo estaba mal.
- La misma pantalla, tipada, con el error en la línea exacta.
- La lista de errores al renombrar `prioridad` en el contrato, y los archivos que toca.
- El mensaje de `ZodError` cuando el servidor devuelve un campo distinto.
- `npm run lint` sin `no-explicit-any` en tu módulo.

---

<!-- _class: lead -->

# Cierre
## Errores típicos, tarea y quiz

---

## Qué guardar de la sesión

| Si ves... | Piensa... |
|---|---|
| `any` | Un hueco por donde entra cualquier cosa. ¿`unknown` y una comprobación? |
| `as Algo` | ¿Dónde se comprueba? Si en ningún lado, es una promesa sin garantía. |
| Tres booleanos de estado | ¿Una unión discriminada con un `estado`? |
| El mismo tipo escrito dos veces | ¿Se puede derivar con `z.infer`, `Pick` u `Omit`? |
| Un `switch` sobre una unión sin `default` | ¿`assertNever` para que olvidar un caso no compile? |
| `lista[0].algo` | ¿Y si la lista está vacía? |

---

## La tarea

**Quitar todos los `any` de un módulo de tu propio proyecto** y entregar el antes y el después:

- El módulo elegido, con un conteo de `any` (y de `as`) antes y después.
- Un caso real en el que un tipo nuevo **encontró un error** que ya existía.
- Un cambio de contrato que ahora rompe el build, con la salida del compilador.
- Una decisión por escrito: dónde dejaste un `as` o un `!` a propósito y por qué.

Entrega en `tarea.md`: criterios de evaluación y formato.

---

## Lecturas y libros de la sesión

**Lecturas del repositorio**

- `dev-notes/06-frontend/01-variables-entorno-vite.md` (la tipificación de `import.meta.env`).
- `dev-notes/06-frontend/12-typescript-para-react.md` (nota nueva).

**Libros (por capítulo)**

- *Learn React with TypeScript* (Rippon, 2025), caps. 1 a 3: *Getting Started with React*, *Getting Started with TypeScript* y *Using React Hooks*.
- *TypeScript 5 Design Patterns and Best Practices* (Despoudis, 2025), caps. 1 y 2: *Getting Started with TypeScript 5* y *TypeScript Core Principles*.

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Siguiente sesión

### S02 - Arquitectura por feature

- Cómo ordenar una aplicación para que crezca sin que cada pantalla dependa de todas las demás.
- La capa de dominio y las fronteras de importación.
- Una regla que **falla en el lint** cuando alguien la rompe.

> Una regla de arquitectura que nadie hace cumplir es una sugerencia.

---

## Quiz: TypeScript para React

- 10 preguntas por intento, sacadas de un banco de 12.
- Individual, sin el editor abierto: se contesta con lo que entendiste, no con lo que copias.
- Lo que falles, búscalo en la nota `12-typescript-para-react` antes del siguiente intento.

<!-- Últimos 15 minutos. Revisa en voz alta la pregunta que más se falle. -->
