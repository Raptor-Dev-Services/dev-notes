# Glosario - S03 - Datos, estado y formularios

> Definiciones de los términos técnicos de la sesión 3.
> Ordenado alfabéticamente.

---

## A

### Actualización optimista
**Categoría:** Datos
**Definición:** Mostrar el resultado de una escritura antes de que el servidor la confirme, y deshacerlo si falla. Requiere guardar una copia del estado anterior para poder restaurarlo.
**En el curso:** `useCambiarEstado` cierra un ticket al instante; si el PATCH falla, `onError` restaura la copia.

### ApiError
**Categoría:** Datos
**Definición:** Clase de error propia del proyecto que representa una respuesta HTTP fallida: lleva el estado y el cuerpo de error de la API. Permite distinguir un 422 (datos inválidos) de un 500 o de una red caída.
**En el curso:** `pedir()` lanza `ApiError` cuando `respuesta.ok` es falso; el formulario la reconoce con `instanceof`.

---

## C

### Caché
**Categoría:** Datos
**Definición:** Copia de un dato del servidor guardada en el cliente para no volver a pedirlo en cada uso. Su costo es que puede quedar vieja.
**En el curso:** TanStack Query guarda cada respuesta indexada por su `queryKey`.

### Condición de carrera
**Categoría:** Estado
**Definición:** Error en que el resultado depende de qué respuesta llega primero. Una petición vieja y lenta puede terminar después de una nueva y sobrescribirla.
**En el curso:** El paso 2 de la práctica la provoca con un `useEffect` y un `fetch` sin cancelación.

### Contrato de la API
**Categoría:** Datos
**Definición:** El acuerdo sobre la forma de las peticiones y respuestas entre el servidor y el cliente. En el proyecto vive como esquemas Zod de los que salen también los tipos.
**En el curso:** `src/shared/api/contract.ts`.

### Context
**Categoría:** Estado
**Definición:** Mecanismo de React para repartir un valor a todo un subárbol sin pasar props. No decide cuándo re-renderizar: todo consumidor se vuelve a pintar cuando el valor cambia.
**En el curso:** Sirve para lo que casi no cambia (tema, usuario). Los datos del servidor no van ahí.

---

## D

### Derivado (estado)
**Categoría:** Estado
**Definición:** Valor que se calcula a partir de otro estado o de props durante el render, y por eso no se guarda. Guardarlo crea redundancia y la posibilidad de que dos copias diverjan.
**En el curso:** `seleccionado` se obtiene con `tickets.find(...)` a partir de `seleccionadoId`.

---

## E

### Estado del servidor
**Categoría:** Estado
**Definición:** Datos cuyo dueño es el servidor y que el cliente solo copia. Pueden quedar viejos sin que la interfaz lo note: necesitan caché, renovación e invalidación.
**En el curso:** La lista de tickets; se administra con TanStack Query.

### Estado de la interfaz
**Categoría:** Estado
**Definición:** Datos que solo existen en el navegador y de los que el cliente es dueño: un menú abierto, una pestaña elegida, un texto a medio escribir.
**En el curso:** Se guarda con `useState`, en la URL o, si se comparte, en un ancestro, Context o store.

### Estado vacío
**Categoría:** Interfaz
**Definición:** La pantalla cuando la consulta salió bien y no hay datos que mostrar. No es un error: debe decirle a la persona qué hacer.
**En el curso:** `No hay tickets con ese filtro.`

---

## F

### Frontera (validación en la)
**Categoría:** Datos
**Definición:** Validar los datos en el punto en que entran a la aplicación (la respuesta de la API, la URL, el almacenamiento) en lugar de confiar en los tipos, que desaparecen al compilar.
**En el curso:** `esquema.parse(datos)` en `pedir()`; `estadoTicketSchema.safeParse(...)` para el filtro de la URL.

---

## G

### gcTime
**Categoría:** TanStack Query
**Definición:** Tiempo que una consulta sin observadores permanece en la caché antes de borrarse. Por omisión, 5 minutos (a noviembre de 2026).
**En el curso:** Explica por qué volver a una pantalla reciente la muestra al instante.

---

## I

### Idempotencia
**Categoría:** Datos
**Definición:** Propiedad de una operación que, repetida, produce el mismo resultado que si se hubiera hecho una vez. Un `POST` no lo es por sí solo: un reintento puede duplicar el recurso.
**En el curso:** Razón por la que deshabilitar el botón no basta contra el doble envío.

### Invalidar (una consulta)
**Categoría:** TanStack Query
**Definición:** Marcar una consulta como vieja y, si está en pantalla, volver a pedirla. Se hace después de una mutación para que las lecturas reflejen el cambio.
**En el curso:** `invalidateQueries({ queryKey: ['tickets'] })` alcanza todas las listas de tickets.

### isFetching
**Categoría:** TanStack Query
**Definición:** Bandera verdadera mientras hay una petición en vuelo, con o sin datos en caché. Sirve para un indicador discreto de "actualizando".
**En el curso:** Distinta de `isPending`, que solo es verdadera mientras no hay datos.

### isPending
**Categoría:** TanStack Query
**Definición:** Bandera verdadera mientras la consulta todavía no tiene datos para esa clave. Es la que decide si se muestra "Cargando".
**En el curso:** `EstadoDeDatos` pinta "Cargando..." solo con `isPending`.

---

## M

### MSW (Mock Service Worker)
**Categoría:** Pruebas
**Definición:** Biblioteca que intercepta las peticiones de red reales y responde con respuestas de prueba, sin reemplazar el código de producción.
**En el curso:** `servidor.use(http.post(...))` simula un 422 o una red caída. En la versión 3 la opción para peticiones sin manejador es `onUnhandledFrame`.

### Mutación
**Categoría:** TanStack Query
**Definición:** Una escritura en el servidor (crear, cambiar, borrar). A diferencia de una consulta, no se guarda en caché ni se ejecuta sola: la dispara la persona.
**En el curso:** `useCrearTicket` y `useCambiarEstado`.

---

## P

### Parámetros de búsqueda
**Categoría:** Estado
**Definición:** La parte de la URL después del `?` (`?estado=abierto`). Estado que se puede compartir, recargar y recorrer con Atrás y Adelante. Es entrada de usuario: se valida.
**En el curso:** El filtro de la lista se lee con `useSearchParams`.

---

## Q

### queryFn
**Categoría:** TanStack Query
**Definición:** La función que trae el dato de una consulta. Recibe un objeto con la `signal` de cancelación y la `queryKey`.
**En el curso:** `({ signal }) => ticketsApi.listar(estado, signal)`.

### queryKey
**Categoría:** TanStack Query
**Definición:** Arreglo que identifica una consulta en la caché. Debe incluir todo lo que cambia el resultado. Es jerárquica: invalidar un prefijo alcanza todas las claves que empiezan con él.
**En el curso:** `['tickets', 'lista', 'abierto']`, generada por `ticketsKeys.lista`.

---

## R

### React Hook Form
**Categoría:** Formularios
**Definición:** Biblioteca de formularios que registra los campos sin guardar cada valor en estado de React, de modo que escribir no re-renderiza el formulario completo.
**En el curso:** `useForm` con `register`, `handleSubmit` y `setError`.

### Rollback
**Categoría:** Datos
**Definición:** Deshacer un cambio provisional cuando la operación falla, devolviendo el estado a una copia guardada.
**En el curso:** `onError` de `useCambiarEstado`; su prueba cae si se quita.

---

## S

### safeParse
**Categoría:** Zod
**Definición:** Método de Zod que valida un valor sin lanzar excepciones: devuelve un objeto con `success` y, según el caso, `data` o `error`.
**En el curso:** Valida el filtro que viene de la URL; si es inválido, se usa `'todos'`.

### sessionStorage
**Categoría:** Navegador
**Definición:** Almacenamiento clave-valor de la pestaña, que vive mientras la pestaña esté abierta y sobrevive a recargar. Cualquier script de la página lo puede leer.
**En el curso:** Guarda el borrador del título del ticket.

### setError
**Categoría:** Formularios
**Definición:** Función de React Hook Form que fija a mano el error de un campo. Se le puede dar un `type` propio, como `'server'`, para distinguir el origen.
**En el curso:** Pega el mensaje del 422 al campo que corresponde.

### signal (AbortSignal)
**Categoría:** Navegador
**Definición:** Objeto que avisa a una operación (como `fetch`) que debe cancelarse. TanStack Query lo aborta cuando la consulta queda obsoleta.
**En el curso:** `pedir(ruta, { esquema, signal })`.

### staleTime
**Categoría:** TanStack Query
**Definición:** Tiempo durante el cual un dato se considera fresco y no se vuelve a pedir. Por omisión es 0: el dato nace viejo (a noviembre de 2026).
**En el curso:** `crearQueryClient` lo fija en 30 segundos; es una decisión de producto.

---

## U

### unión discriminada
**Categoría:** TypeScript
**Definición:** Tipo unión cuyos miembros comparten una propiedad literal (`estado: 'cargando' | 'error' | ...`) que permite al compilador saber cuál es cuál. Impide combinaciones imposibles de estado.
**En el curso:** `Vista<T>`, la alternativa a tres booleanos de carga.

---

## Z

### Zod
**Categoría:** Validación
**Definición:** Biblioteca de esquemas para TypeScript: define la forma de un dato, lo valida en ejecución y deriva el tipo estático con `z.infer`.
**En el curso:** El contrato de la API y la validación del formulario salen del mismo esquema.

### zodResolver
**Categoría:** Formularios
**Definición:** Adaptador de `@hookform/resolvers` que conecta un esquema Zod con React Hook Form para validar el formulario con las reglas del esquema.
**En el curso:** `useForm({ resolver: zodResolver(nuevoTicketSchema) })`.
