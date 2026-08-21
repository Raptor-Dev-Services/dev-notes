# 09 · Componentes headless — separar comportamiento de apariencia

## Problema que resuelve

Un componente interactivo tiene dos mitades que envejecen a ritmos distintos: **cómo se comporta** (qué abre, qué cierra, qué hace `Escape`, qué anuncia el lector de pantalla) y **cómo se ve**. La apariencia cambia cada rediseño; el comportamiento casi nunca. Mezclarlos obliga a reescribir lo que funciona cada vez que cambia lo que no.

Un **componente headless** es la mitad de comportamiento sola: un hook que encapsula toda la lógica y no renderiza nada. Quien lo consume decide el markup.

Complementa `05-componentes-primitivos.md`, que cubre los primitivos ya construidos. Este doc cubre cuándo construir el tuyo y cómo.

## Antes de construir: ¿ya existe?

**La respuesta por defecto es no construirlo.** Foco atrapado, retorno de foco al cerrar, `Escape`, navegación con flechas, `aria-activedescendant` y comportamiento en lector de pantalla son caros de hacer bien y más caros de mantener. Ya están resueltos en Headless UI, Radix y React Aria, que son exactamente este patrón empaquetado.

Construye el tuyo solo cuando:

- El comportamiento es **de tu dominio**, no un control estándar (un wizard con tus reglas de avance, una tabla con tu modelo de selección).
- Necesitas la **misma lógica en dos apariencias** que la librería no contempla.
- El primitivo existe pero obliga a un markup incompatible con tu design system, y ya intentaste componer alrededor.

Si es un dropdown, un dialog o un combobox estándar: usa Headless UI y sigue con tu vida.

## El patrón

El hook posee el estado y devuelve lo que hace falta para pintarlo. No devuelve JSX.

```js
// src/hooks/useDropdown.js - todo el comportamiento, cero apariencia
export function useDropdown(items) {
  const [isOpen, setIsOpen] = useState(false)
  const [selectedIndex, setSelectedIndex] = useState(-1)
  const listboxId = useId()

  const toggle = useCallback(() => setIsOpen((o) => !o), [])
  const close = useCallback(() => setIsOpen(false), [])

  const select = useCallback((index) => {
    setSelectedIndex(index)
    setIsOpen(false)
  }, [])

  const handleKeyDown = useCallback((e) => {
    switch (e.key) {
      case 'Enter':
      case ' ':
        e.preventDefault()
        isOpen && selectedIndex >= 0 ? select(selectedIndex) : toggle()
        break
      case 'ArrowDown':
        e.preventDefault()
        setSelectedIndex((i) => (i + 1) % items.length)
        break
      case 'ArrowUp':
        e.preventDefault()
        setSelectedIndex((i) => (i - 1 + items.length) % items.length)
        break
      case 'Escape':
        close()
        break
    }
  }, [isOpen, selectedIndex, items.length, select, toggle, close])

  // Los aria-* son comportamiento, no estilo: los decide el hook.
  const getTriggerProps = useCallback(() => ({
    role: 'combobox',
    'aria-expanded': isOpen,
    'aria-controls': listboxId,
    'aria-haspopup': 'listbox',
    tabIndex: 0,
    onClick: toggle,
    onKeyDown: handleKeyDown,
  }), [isOpen, listboxId, toggle, handleKeyDown])

  const getItemProps = useCallback((index) => ({
    role: 'option',
    'aria-selected': index === selectedIndex,
    onClick: () => select(index),
  }), [selectedIndex, select])

  return { isOpen, selectedIndex, listboxId, close, getTriggerProps, getItemProps }
}
```

El consumidor solo pinta:

```jsx
function PlanPicker({ plans }) {
  const { isOpen, listboxId, getTriggerProps, getItemProps } = useDropdown(plans)

  return (
    <div className="relative">
      <button {...getTriggerProps()} className={ui.controls.secondaryButton}>
        Selecciona un plan
      </button>
      {isOpen && (
        <ul id={listboxId} role="listbox" className={ui.surface.popover}>
          {plans.map((plan, i) => (
            <li key={plan.id} {...getItemProps(i)}>{plan.name}</li>
          ))}
        </ul>
      )}
    </div>
  )
}
```

Una segunda apariencia (compacta, en un drawer, como celda de tabla) es un componente nuevo de 20 líneas que reusa el mismo hook. El comportamiento accesible no se reescribe.

### Las tres piezas que devuelve

| Pieza | Qué es | Ejemplo |
|-------|--------|---------|
| **Estado** | Lo que la vista necesita para decidir qué pintar | `isOpen`, `selectedIndex` |
| **Acciones** | Comandos que la vista dispara | `toggle`, `close`, `select` |
| **Prop getters** | Paquetes de props ya armadas (eventos + `aria-*` + `role`) | `getTriggerProps()` |

Los **prop getters** son lo que separa un headless usable de uno que reparte trabajo. Si devuelves `isOpen` y `handleKeyDown` sueltos, cada consumidor tiene que acordarse de cablear `aria-expanded`, `aria-controls` y `role`, y al tercero se le va a olvidar. Devolviendo `getTriggerProps()`, la accesibilidad viaja con el comportamiento y es imposible olvidarla.

## La regla que no puedes saltarte

Dentro de un componente, una función nueva por render es inofensiva. **Devuelta por un hook, deja de serlo.** El consumidor no puede saber que cambia de identidad en cada render, la mete en las dependencias de un `useEffect`, y el efecto corre en bucle. Como el hook es compartido, el bucle aparece en pantallas que no escribiste tú.

Envuelve en `useCallback` **toda función que salga del hook**, y en `useMemo` los objetos y arrays. Es la excepción explícita a "no memoices por defecto": en un headless compartido es obligatoria.

`useId` para los ids de `aria-controls` y `aria-labelledby`: un id fijo colisiona en cuanto el control se renderiza dos veces en la misma pantalla.

## Elegir la técnica

Hook, HOC y render prop resuelven el mismo problema con distinto costo. **Por defecto: hook.**

| Técnica | Forma | Úsala cuando | Costo |
|---------|-------|--------------|-------|
| **Hook** | `const x = useAlgo(args)` | Casi siempre | Ninguno relevante |
| **Render prop** | `<X>{(estado) => <UI/>}</X>` | El que decide el markup necesita el estado en medio de un árbol que tú controlas (una tabla que gestiona filas y deja pintar cada celda) | Anidamiento; ilegible al combinar dos |
| **HOC** | `withAlgo(Componente)` | Envolver un componente que no controlas, o inyectar lo mismo a muchos por igual | Oculta props, ensucia DevTools |
| **Composición con `children`** | `<Panel header={<H/>}>{body}</Panel>` | El problema es **estructura**, no comportamiento | Ninguno. Prefiérela siempre que aplique |

**Regla práctica:** si el consumidor necesita el estado para pintar y nada más, hook. Si además necesitas controlar el árbol alrededor, render prop. HOC solo para envolver lo ajeno.

```jsx
// HOC legitimo: envolver algo que no controlas, con el mismo criterio en todos lados
export const withAuthorization = (Component) => (props) =>
  useSession().isAuthorized ? <Component {...props} /> : <Login />
```

Para código propio, `useSession()` dentro del componente o un guard de ruta casi siempre gana: menos indirección y las props siguen visibles.

> **No conviertas prop drilling en headless.** Si el problema es que un valor viaja tres niveles, la solución es composición o Context, no un hook nuevo.

## El riesgo real: sobre-abstracción

El patrón tiene un costo que no se ve hasta después: **indirección**. Cada headless que agregas es un salto más entre "veo el componente" y "encuentro por qué hace eso". Un archivo que solo tú usas, con una sola apariencia, no es una abstracción: es un archivo de más.

Extrae **después de la segunda apariencia real**, no antes. La duplicación de un comportamiento en dos sitios es barata de arreglar; la abstracción prematura sobre un caso hipotético es cara de deshacer y suele quedar mal encajada cuando aparece el segundo caso de verdad.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Mismo comportamiento con dos o más apariencias reales | Una sola apariencia: es indirección sin beneficio |
| Control de tu dominio que ninguna librería cubre | Dropdown, dialog, combobox estándar: usa Headless UI |
| Lógica con teclado y `aria-*` que quieres testear aislada | Componente de display sin interacción |
| Quieres que la accesibilidad viaje con el comportamiento | El problema es estructura: usa composición con `children` |

> Fuente: *React Anti-Patterns* (Juntao Qiu) — Ch.9 Applying Design Principles in React, Ch.10 Diving Deep into Composition Patterns.

---

*Rogelio Arriaga Gonzalez*
