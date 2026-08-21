# 11 · Prácticas del código real — lo que los libros no traen

## Problema que resuelve

Los ejemplos de libro están escritos para caber en una página: un `useState` para `loading`, otro para `error`, un `fetch` sin cancelar, y una lista que se filtra en memoria. Funcionan como ilustración y se rompen en producción por motivos que el libro no llega a mostrar.

Este doc recoge patrones tomados de `sociofit-webclient` que **mejoran** el ejemplo canónico, con el motivo por el que se adoptaron. Varios nacieron de un incidente real, y ese contexto vale más que el patrón.

## 1. Máquina de estados en vez de booleanos sueltos

**Lo que traen los libros** (y el doc `06-feature-hook.md` de este repo):

```js
const [loading, setLoading] = useState(false)
const [error, setError] = useState('')
const [rows, setRows] = useState([])
```

Tres estados independientes permiten combinaciones que no existen: `loading: true` con `error` lleno, o `loading: false` con `rows` vacío y sin error, que la vista no sabe si es "vacío" o "todavía no cargó". Cada componente inventa su propia forma de desempatar.

**La versión que aguanta:**

```js
// src/utils/loadStatus.js
export const LOAD_STATUS = Object.freeze({
  loading: 'loading',
  success: 'success',
  error: 'error',
})
```

```js
const [status, setStatus] = useState(LOAD_STATUS.loading)
const [error, setError] = useState(null)
```

Un solo valor, tres estados excluyentes. Sobre `success` se decide entre vacío y contenido según si la colección trae elementos, lo que da los **cuatro estados obligatorios de pantalla**: cargando, error, vacío, contenido. Ninguna combinación imposible.

> **El incidente:** ese objeto estaba declarado **38 veces**, idéntico, bajo **cuatro nombres distintos** (`LOAD_STATUS`, `STATUS`, `LOAD`, `PLANS_STATUS`). Había que mirar el import para saber si dos pantallas hablaban de lo mismo. Hablaban de lo mismo. Una palabra por concepto, en un solo archivo.

## 2. Cancelar de verdad, no solo ignorar el resultado

**Lo que traen los libros:** el flag booleano en el cleanup.

```js
useEffect(() => {
  let active = true
  fetchData().then((d) => { if (active) setData(d) })
  return () => { active = false }
}, [id])
```

Evita el `setState` tardío, pero **deja la petición viva** ocupando conexión. Y muchos ejemplos ni eso traen.

**La versión que aguanta:** un hook compartido que aborta de verdad.

```js
// src/features/shared/hooks/useAbortableLoad.js
export function useAbortableLoad(load) {
  useEffect(() => {
    const controller = new AbortController()
    load(controller.signal)
    return () => controller.abort()
  }, [load])
}
```

```js
// en el hook del feature
const load = useCallback(async (signal) => {
  setStatus(LOAD_STATUS.loading)
  try {
    const { items, total } = await listMembers({ page, pageSize, search, signal })
    setItems(items); setTotal(total)
    setStatus(LOAD_STATUS.success)
  } catch (err) {
    // Cancelada porque cambio la pagina o el filtro: no es un fallo.
    if (isAbortError(err)) return
    setError(extractApiErrorMessage(err))
    setStatus(LOAD_STATUS.error)
  }
}, [page, search])

useAbortableLoad(load)
```

**Por qué importa:** sin cancelar, dos peticiones de la misma lista pueden estar en vuelo a la vez y resolver fuera de orden. La más vieja llega al final, sobrescribe el estado, y la vista muestra el resultado de una consulta que el usuario ya reemplazó, **sin ninguna señal de carga**. No hay error: hay datos equivocados en pantalla.

Dos detalles que hacen que funcione:

- El `load` debe venir de un `useCallback` con sus dependencias: el hook usa esa identidad como disparador.
- El `catch` debe ignorar la cancelación con `isAbortError`. Si no, abortar deja la vista en estado de error.

Las recargas explícitas (tras mutar, o el botón de reintento) llaman a `load()` sin señal: no compiten con nada.

## 3. Mutaciones que devuelven resultado, no que lanzan

**Lo que traen los libros:** la mutación lanza y cada llamador pone su `try/catch`.

**La versión que aguanta:**

```js
const saveMember = useCallback(async (id, payload) => {
  try {
    if (id) await updateMember(id, payload)
    else await createMember(payload)
    await load()
    return { ok: true }
  } catch (err) {
    return { ok: false, message: extractApiErrorMessage(err) }
  }
}, [load])
```

El llamador decide cómo mostrarlo, sin repetir el parseo del error:

```js
const result = await vm.saveMember(id, form)
if (!result.ok) setError(result.message)
```

**Por qué importa:** una excepción es para lo excepcional. Que el servidor rechace un alta por reglas de negocio es un resultado esperado del flujo, no un fallo del programa. Modelarlo como valor hace imposible olvidar el manejo: no hay forma de ignorar un `{ ok }` sin que se note en el código.

Es el mismo `Result Pattern` del dominio `04-backend/01-result-pattern.md`, aplicado del lado del cliente.

## 4. Filtrar y paginar en el servidor

**Lo que traen los libros** (y el doc `06-feature-hook.md`): traer todo y filtrar en memoria.

```js
const filteredRows = useMemo(() => rows.filter(matchesFilter), [rows, filters])
const pagedRows = filteredRows.slice((page - 1) * pageSize, page * pageSize)
```

Esto es correcto **solo si el cliente tiene el conjunto completo**: un catálogo corto, las opciones de un desplegable, una lista sin paginar.

**En cuanto hay paginación en servidor, deja de ser un problema de rendimiento y pasa a ser de correctitud.** Si el servidor manda 20 filas por página y el cliente filtra sobre esas 20, el usuario ve una lista vacía y **nunca se entera de que había resultados en la página siguiente**. No hay error, no hay excepción, y el reporte que llega es "no aparece".

**La versión que aguanta:** los tres viajan juntos al servidor.

```js
const { items, total } = await listMembers({ page, pageSize, search, signal })
```

```js
// El filtro vuelve a la pagina 1: si no, se pediria una pagina que el nuevo
// filtro no tiene y la lista saldria vacia sin motivo aparente.
useEffect(() => { setPage(1) }, [search])
```

Y el texto se aplica con debounce, no en cada tecla:

```js
const [searchInput, setSearchInput] = useState('')
const search = useDebouncedValue(searchInput.trim(), 350)
```

Dos valores separados a propósito: `searchInput` es lo que se ve en el campo (responde a cada tecla), `search` es lo que dispara la petición.

**Regla:** filtrar, ordenar y paginar son la misma operación y se resuelven en el mismo lugar. Repartirlas produce resultados incorrectos, no lentos.

## 5. Un helper compartido nace de un incidente, no de una previsión

Tres módulos de `sociofit-webclient` existen porque algo se rompió antes:

| Módulo | Qué consolidó | Qué pasaba |
|--------|---------------|------------|
| `utils/money.js` | Conversión centavos <-> unidades | Seis copias con ocho nombres; la del POS devolvía `"0.00"` donde las demás devolvían cadena vacía, bajo el mismo nombre |
| `utils/loadStatus.js` | Estados de carga | 38 declaraciones idénticas bajo cuatro nombres |
| `features/shared/hooks/useAbortableLoad.js` | Cancelación de peticiones | Respuestas viejas pisando nuevas al cambiar de filtro |

El patrón en los tres: **las copias no se rompen, divergen en silencio.** Ninguna falló con un error. La de `money` cambiaba lo que se veía en pantalla al mover un componente de un feature a otro, y nada avisaba.

**Consecuencia práctica:** la señal para subir algo a compartido es la **segunda** copia, no la quinta. Mientras haya dos, todavía se pueden comparar y decidir cuál es la buena. Con seis, ya nadie sabe cuál era la original.

## 6. El comentario de cabecera explica el porqué, no el qué

La práctica que sostiene a las cinco anteriores. Cada uno de esos módulos abre con un comentario que dice **por qué existe**:

```js
// loadStatus.js - los estados de una carga de datos, en un solo sitio.
//
// Este objeto estaba declarado 38 veces, identico, bajo CUATRO nombres distintos (LOAD_STATUS,
// STATUS, LOAD y PLANS_STATUS), lo que rompe la regla de "una palabra por concepto" y obliga a
// mirar el import para saber si dos pantallas hablan de lo mismo. Hablan de lo mismo.
```

```js
// url.js - saneo de URLs de datos antes de renderizarlas en un href.
//
// Seguridad: solo se permite el esquema http/https. Bloquea vectores como `javascript:` o `data:`
// que un tenant podria poner en un campo de URL de su micrositio y que, al hacer clic un usuario
// logueado, se ejecutarian en el ORIGEN de SocioFit.
```

Un comentario que dice *qué* hace la función es ruido: el código ya lo dice. Un comentario que dice *qué se rompió* y *qué decisión se tomó* es lo único que evita que alguien lo deshaga por parecerle innecesario.

Es la diferencia entre `// convierte centavos a unidades` y "existían seis copias y una divergía".

## Resumen

| Práctica | En vez de | Gana |
|----------|-----------|------|
| `LOAD_STATUS` como máquina de estados | `loading` + `error` booleanos | Sin combinaciones imposibles; cuatro estados de pantalla claros |
| `useAbortableLoad` con `AbortController` | Flag booleano, o nada | Corta la petición de verdad; sin respuestas fuera de orden |
| Mutación devuelve `{ ok, message }` | Lanzar y capturar en cada llamador | El error esperado es un valor; imposible ignorarlo |
| Filtrar y paginar en servidor | Filtrar en cliente sobre datos paginados | Correctitud, no rendimiento |
| Consolidar a la segunda copia | Esperar a que "duela" | Las copias divergen en silencio |
| Comentario que explica el incidente | Comentario que describe el código | Nadie deshace la decisión por creerla innecesaria |

> Fuente: código de producción de `sociofit-webclient` (React 19 · Vite · React Router 7 · Axios). Los puntos 4 y 5 contrastan con el ejemplo de `06-feature-hook.md`, que muestra filtrado en cliente: sigue siendo válido para conjuntos completos sin paginar.

---

*Rogelio Arriaga Gonzalez*
