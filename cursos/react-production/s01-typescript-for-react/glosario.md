# Glosario - S01 - TypeScript para React

> Definiciones de los términos técnicos de la sesión 1.
> Ordenado alfabéticamente.

---

## A

### any
**Categoría:** Tipos
**Definición:** Tipo que desactiva la revisión: con un valor `any` se puede leer cualquier propiedad o llamarlo como función sin que el compilador proteste. Se contagia: lo que se calcula a partir de un `any` suele ser `any`.
**En el curso:** `Response.json()` y `JSON.parse` devuelven `any`; la regla `@typescript-eslint/no-explicit-any` marca los que escribes a mano.

---

### Aserción de tipo (`as`)
**Categoría:** Tipos
**Definición:** Instrucción que le dice al compilador "trata este valor como de este otro tipo". No convierte ni comprueba nada en ejecución: si el valor no es de ese tipo, el error aparece más tarde y en otro lugar.
**En el curso:** En `tickets-page.tsx`, `e.target.value as EstadoTicket | 'todos'` es un `as` que no comprueba; se cambia por una guarda de tipo.

---

### assertNever
**Categoría:** Patrones
**Definición:** Función que recibe un valor de tipo `never` y lanza un error. Se llama en el `default` de un `switch` sobre una unión: si se agrega un caso y no se maneja, el valor deja de ser `never` y el compilador falla.
**En el curso:** Se usa en `describir()` para que olvidar un estado de `Vista<T>` no compile.

---

## B

### Borrado de tipos (type erasure)
**Categoría:** Compilador
**Definición:** Los tipos de TypeScript solo existen al compilar: el JavaScript resultante no los trae. Por eso un tipo no puede comprobar lo que llega del servidor en ejecución.
**En el curso:** Es la idea central de la sesión: los tipos protegen tu código, no la frontera con la API.

---

## C

### ComponentProps
**Categoría:** React y tipos
**Definición:** Tipo de utilidad de React que da las props de un elemento nativo o de un componente: `ComponentProps<'button'>` trae `onClick`, `disabled`, `type` y las demás. Sirve para que un componente propio acepte las mismas props que el nativo, más las suyas.
**En el curso:** `Boton` extiende `ComponentProps<'button'>` con una prop `variante`.

---

### Contrato de la API
**Categoría:** Contratos
**Definición:** Descripción de lo que la API recibe y devuelve: rutas, campos, tipos y errores. Puede estar en un OpenAPI, en un esquema de Zod o en ambos. El front depende de él, y un cambio no avisado lo rompe.
**En el curso:** `src/shared/api/contract.ts` define el contrato de la Mesa de ayuda; `contract/openapi.yaml` lo describe como especificación.

---

## D

### Discriminante
**Categoría:** Patrones
**Definición:** Campo con valores literales que comparten todos los miembros de una unión (por ejemplo `estado`) y que permite distinguirlos. Al comprobarlo, TypeScript sabe qué otros campos tiene cada caso.
**En el curso:** `estado: 'cargando' | 'error' | 'listo'` en `Vista<T>`.

---

## E

### Estrechamiento (narrowing)
**Categoría:** Tipos
**Definición:** Proceso por el que TypeScript reduce un tipo amplio a uno más específico dentro de una rama: tras un `if`, un `switch`, un `typeof`, un `instanceof` o una guarda de tipo.
**En el curso:** Dentro de `case 'listo'`, `v.datos` existe; en `case 'error'`, no.

---

### Exhaustividad
**Categoría:** Patrones
**Definición:** Garantía de que se manejaron todos los casos de una unión. Se logra dejando que el valor restante en el `default` sea de tipo `never`.
**En el curso:** Verlo fallar: quitar el `case 'listo'` produce el error `TS2345` con `never`.

---

## F

### Frontera (boundary)
**Categoría:** Contratos
**Definición:** Punto por el que un dato entra a tu programa desde afuera: una respuesta HTTP, `localStorage`, un parámetro de la URL, un mensaje. Ahí el tipo no puede ser una promesa, tiene que ser una comprobación.
**En el curso:** `pedir()` valida cada respuesta con su esquema antes de entregarla al resto de la aplicación.

---

## G

### Genérico
**Categoría:** Tipos
**Definición:** Función, tipo o componente con un parámetro de tipo (`T`) que se decide al usarlo. Permite reutilizar la lógica sin perder el tipo concreto.
**En el curso:** `Lista<T>` sirve para tickets o usuarios; `useState<Ticket | null>` fija `T` explícitamente.

---

### Guarda de tipo (type guard)
**Categoría:** Patrones
**Definición:** Función cuyo retorno es un predicado de tipo (`valor is Filtro`). Si devuelve `true`, TypeScript estrecha el tipo del argumento en esa rama. La comprobación sí ocurre en ejecución.
**En el curso:** `esFiltro(valor)` sustituye al `as` del selector de estado.

---

## I

### Inferencia de tipos
**Categoría:** Tipos
**Definición:** Capacidad del compilador de deducir el tipo de una expresión sin que lo escribas. En React sirve para que `useState(false)` sea `boolean` y para que el retorno de un hook salga de la función que llama.
**En el curso:** `useTickets` no declara su retorno: lo infiere de `ticketsApi.listar` y, a su vez, del esquema.

---

### interface
**Categoría:** Tipos
**Definición:** Declaración de la forma de un objeto. Se usa para props y modelos. Junto con `type`, es la manera de nombrar tipos de objeto; las uniones y los tipos derivados se escriben con `type`.
**En el curso:** `InsigniaProps` y `ListaProps<T>`.

---

### isolatedModules
**Categoría:** Compilador
**Definición:** Opción que asegura que cada archivo se pueda transformar sin mirar a los demás, como hacen Vite y otras herramientas que borran los tipos sin comprobarlos. Prohíbe construcciones que necesitarían información de otros archivos.
**En el curso:** Va activada en el `tsconfig.json` del proyecto.

---

## N

### never
**Categoría:** Tipos
**Definición:** Tipo sin valores posibles: lo que queda cuando se descartaron todos los casos. Nada se le puede asignar, salvo otro `never`.
**En el curso:** Base de `assertNever` y de la comprobación de exhaustividad.

---

### noUncheckedIndexedAccess
**Categoría:** Compilador
**Definición:** Opción que agrega `undefined` al tipo de lo que se lee por índice (`lista[0]`). No forma parte de `strict`. Obliga a decidir qué pasa con una lista vacía.
**En el curso:** `p.datos[0].titulo` falla con `TS2532` hasta que se escribe `p.datos[0]?.titulo`.

---

## O

### OpenAPI
**Categoría:** Contratos
**Definición:** Especificación estándar para describir una API HTTP en YAML o JSON. De ella se pueden generar documentación, clientes y tipos.
**En el curso:** `contract/openapi.yaml`, y `openapi-typescript` 7.13.0 para generar `schema.d.ts`.

---

## R

### ReactNode
**Categoría:** React y tipos
**Definición:** Tipo de React que reúne todo lo que puede ir como hijo de un componente: elementos, texto, números, `null`, `undefined`, booleanos y listas.
**En el curso:** `children?: ReactNode` en `InsigniaProps`.

---

### Record
**Categoría:** Tipos
**Definición:** Tipo de utilidad: `Record<K, V>` es un objeto con las claves `K` y valores `V`. Con una unión de literales como `K`, exige una entrada por cada literal.
**En el curso:** `Record<Prioridad, string>` obliga a que haya una etiqueta por cada prioridad.

---

## S

### satisfies
**Categoría:** Tipos
**Definición:** Operador (TypeScript 4.9) que comprueba que una expresión cumple un tipo sin cambiar el tipo resultante de la expresión.
**En el curso:** `ETIQUETAS satisfies Record<Prioridad, string>` falla si falta una prioridad.

---

### strict
**Categoría:** Compilador
**Definición:** Opción que activa la familia de comprobaciones estrictas: `strictNullChecks`, `noImplicitAny`, `useUnknownInCatchVariables` y otras. En TypeScript 6.0 es `true` por omisión.
**En el curso:** Está activada en el proyecto, junto con `noUncheckedIndexedAccess`.

---

## T

### Tipo de utilidad
**Categoría:** Tipos
**Definición:** Tipo genérico incluido en TypeScript que transforma otros tipos: `Pick`, `Omit`, `Partial`, `Required`, `Record`, `ReturnType`, `Awaited` y más.
**En el curso:** `CambioDeTicket = Partial<Omit<Ticket, 'id' | 'creadoEn'>>`.

---

### Tipo estructural
**Categoría:** Tipos
**Definición:** En TypeScript dos tipos son compatibles si tienen la misma forma, sin importar su nombre. Por eso dos `number` con significados distintos son intercambiables, salvo que se les ponga una marca.
**En el curso:** Motivo de los tipos opacos (`TicketId`, `UsuarioId`).

---

### Tipo opaco (branded type)
**Categoría:** Patrones
**Definición:** Tipo que se construye con una marca que solo existe al compilar, para distinguir valores que por dentro son iguales (por ejemplo dos ids numéricos). Se crea con un `as` en un único punto.
**En el curso:** `TicketId = number & { readonly __marca: 'TicketId' }`.

---

### `@ts-expect-error`
**Categoría:** Compilador
**Definición:** Comentario que suprime el error de la línea siguiente y avisa (`Unused '@ts-expect-error' directive`) cuando ese error ya no existe. Es preferible a `@ts-ignore`, que nunca avisa.
**En el curso:** Se usa en pruebas de tipos, con un comentario del motivo.

---

### TSConfig (`tsconfig.json`)
**Categoría:** Compilador
**Definición:** Archivo que configura el compilador: qué archivos revisa, a qué versión de JavaScript apunta y qué comprobaciones activa.
**En el curso:** El del proyecto activa `strict`, `noUncheckedIndexedAccess`, `verbatimModuleSyntax` y `isolatedModules`.

---

### typescript-eslint
**Categoría:** Herramientas
**Definición:** Conjunto de paquetes que permite a ESLint entender TypeScript. Importa el paquete `typescript` por dentro, por eso depende de una versión que tenga API programática.
**En el curso:** La 8.72.2 declara `typescript >=4.8.4 <6.1.0`; el proyecto usa TypeScript 6.0.3.

---

## U

### Unión discriminada
**Categoría:** Patrones
**Definición:** Unión de tipos de objeto que comparten un campo con valores literales (el discriminante). Modela estados mutuamente excluyentes sin combinaciones imposibles.
**En el curso:** `Vista<T>`: cargando, error o listo.

---

### unknown
**Categoría:** Tipos
**Definición:** Tipo seguro para valores de origen desconocido: se puede asignar cualquier cosa a `unknown`, pero no se puede usar hasta comprobar su tipo. Es el contrario de `any`.
**En el curso:** `const datos: unknown = await respuesta.json()`.

---

## V

### `verbatimModuleSyntax`
**Categoría:** Compilador
**Definición:** Opción que obliga a marcar con `type` los imports que son solo tipos y conserva tal cual los demás. Evita imports que solo existen para los tipos y que quedan en el código final.
**En el curso:** `import { Ticket }` falla con `TS1484`; se escribe `import type { Ticket }`.

---

## Z

### `z.infer`
**Categoría:** Herramientas
**Definición:** Tipo de Zod que obtiene el tipo de TypeScript a partir de un esquema. Es lo que impide que el tipo y la validación se separen.
**En el curso:** `export type Ticket = z.infer<typeof ticketSchema>`.

---

### Zod
**Categoría:** Herramientas
**Definición:** Biblioteca para definir esquemas, validar datos en ejecución y derivar de ellos los tipos de TypeScript. Versión 4.6.7 a noviembre de 2026.
**En el curso:** `ticketSchema` valida y, con `z.infer`, produce el tipo `Ticket`.

---

*Versiones a noviembre de 2026; consulta el registro de npm para las vigentes.*
