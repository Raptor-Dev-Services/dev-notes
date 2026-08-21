# 08 · Estado — dónde vive cada cosa

## Problema que resuelve

La mayoría de los problemas de "estado global" no son problemas de librería: son estado colocado en el nivel equivocado. Un filtro que debería estar en la URL acaba en un store, un tema que cambia una vez al día comparte Provider con datos que cambian al escribir, y media app re-renderiza sin motivo.

Antes de elegir herramienta, elige **lugar**.

> **Estado de servidor primero.** Listas y detalles que vienen de la API no son estado de cliente: tienen caché y revalidación propias (ver `02-react-produccion.md`, TanStack Query). Meter la respuesta de un endpoint en un store global es la causa número uno de datos rancios y de dos pantallas mostrando cosas distintas. Este doc cubre el estado que **nace en el cliente**.

## La escalera

Recorre de arriba abajo y **quédate en el primer nivel que funcione**. Subir es fácil; bajar, no.

| Nivel | Cuándo | Herramienta |
|-------|--------|-------------|
| 1. Local al componente | Solo ese componente lo lee | `useState` / `useReducer` |
| 2. Elevado al padre común | Dos o más hermanos lo comparten de verdad | `useState` en el ancestro más cercano |
| 3. En la URL | Filtros, orden, página, tab activo, id seleccionado | query params del router |
| 4. Context | Config de ambiente que casi no cambia: tema, locale, sesión, branding | Context memoizado, dividido por dominio |
| 5. Store con selectores | Estado de dominio que cambia seguido y leen muchos componentes lejanos | Zustand |

Tres preguntas resuelven casi todo:

1. **¿Quién lo lee?** Un componente, nivel 1. Hermanos, nivel 2. Media app, nivel 4 o 5.
2. **¿Debe sobrevivir a un refresh o ser compartible por link?** Sí, nivel 3.
3. **¿Cada cuánto cambia?** Si cambia por interacción y lo leen muchos, Context es el lugar equivocado.

### Nivel 1 vs nivel 3: no lo resuelve la escalera

El estado local no compite con la URL en jerarquía sino en propósito.

Filtros y paginación en `useState` dentro del hook de la pantalla están **bien** mientras la vista sea de operación interna y nadie necesite compartir ni volver a ese resultado exacto. Es más simple y el hook queda autocontenido. Así lo hace `useMembers` en `sociofit-webclient`.

Súbelos a la URL cuando aparezca una de estas:

- Alguien manda por chat "mira este listado".
- El usuario espera que "atrás" deshaga el filtro sin salir de la pantalla.
- El refresh tras una acción no debe perder la posición.

Lo que **no** es defendible es el punto medio: filtros en un store global o en Context. Paga el precio de globalizar sin ganar ni el link compartible ni la simplicidad del estado local.

## Por qué Context no escala para estado que cambia

Un cambio en el `value` re-renderiza a **todos** los consumidores, lean o no el campo que cambió. Con un tema o un locale da igual: cambian una vez al día. Con estado de dominio que cambia al escribir, es una cascada.

Context tampoco permite suscribirse a **una parte** del valor: es todo o nada. Ese es el límite estructural y no se arregla memoizando mejor.

`sociofit-webclient` tiene exactamente cuatro contextos y los cuatro son de nivel 4: `branding`, `entitlements`, `theme`, `i18n`. Ninguno cambia por interacción del usuario. Ese es el uso correcto.

**Señales de que Context se quedó corto:**

- Memoizaste el `value` y el Profiler sigue mostrando re-renders en componentes que no usan lo que cambió.
- Vas por el tercer Provider anidado del mismo dominio para partir el `value`.
- Necesitas leer estado fuera de React (un interceptor, un helper).
- Necesitas coordinar dos slices que se afectan entre sí.

## El patrón de store

Un store vive **fuera del árbol de React** y cada componente se suscribe solo a lo que lee. Eso es lo que Context no puede dar.

```js
// src/stores/cartStore.js
export const useCartStore = create((set) => ({
  items: [],
  couponCode: null,

  // Objeto creado una sola vez: seleccionarlo nunca provoca re-render.
  actions: {
    addItem: (item) => set((state) => ({ items: [...state.items, item] })),
    removeItem: (id) => set((state) => ({ items: state.items.filter((i) => i.id !== id) })),
    clear: () => set({ items: [], couponCode: null }),
  },
}))
```

Fíjate en lo que **no** está: no hay `total`, no hay `itemCount`, no hay `isEmpty`.

### Selectores granulares

**Nunca consumas el store completo.** `const store = useCartStore()` se suscribe a todo y tira por la borda la única ventaja que tenía sobre Context.

```js
// La API publica del store: un hook por lectura
export const useCartItems = () => useCartStore((s) => s.items)
export const useCouponCode = () => useCartStore((s) => s.couponCode)
export const useCartActions = () => useCartStore((s) => s.actions)
```

Un componente que solo pinta el contador se re-renderiza cuando cambian los ítems, y no cuando cambia el cupón.

### Separa estado de acciones

Las acciones nunca cambian de identidad. Si viven en el mismo objeto que el estado, un componente que solo dispara acciones (un botón "Vaciar carrito") se re-renderiza cada vez que el carrito cambia, sin necesitarlo.

Si tu store no las agrupa, un selector que devuelve un objeto nuevo (`s => ({ add: s.add, clear: s.clear })`) crea una referencia distinta en cada render y provoca justo el re-render que querías evitar. O las agrupas bajo `actions`, o seleccionas cada una por separado.

## El estado derivado no vive en el store

Guardar `total` junto a `items` crea dos fuentes de verdad: cada acción que toque `items` tiene que acordarse de recalcular las otras, y la que se olvide deja la UI mintiendo.

```js
// Selector puro: se testea sin React y sin store
export const selectCartSummary = (state) => {
  const subtotal = state.items.reduce((sum, i) => sum + i.price * i.quantity, 0)
  const shipping = subtotal > FREE_SHIPPING_THRESHOLD ? 0 : FLAT_SHIPPING
  return { subtotal, shipping, total: subtotal + shipping }
}
```

Si el derivado es una **regla de negocio** (umbral de envío gratis, escalones de descuento), el cálculo pertenece a la capa de dominio y el selector solo lo invoca. Ver `07-capa-dominio-acl.md`.

**Cuidado con la identidad:** un selector que arma un objeto nuevo devuelve una referencia distinta en cada evaluación. Usa el comparador superficial que ofrezca el store, o selecciona los campos por separado.

## Persistencia: selectiva, nunca completa

Persistir el store entero deja la app en una posición imposible al recargar: un modal marcado como abierto, un `isLoading` congelado en `true`, un formulario a medias que ya no corresponde a nada.

```js
export const useSettingsStore = create(
  persist(
    (set) => ({ theme: 'light', language: 'es', sidebarOpen: true, lastError: null }),
    {
      name: 'app-settings',
      // Solo esto sobrevive al refresh. El resto arranca limpio.
      partialize: (state) => ({ theme: state.theme, language: state.language }),
    },
  ),
)
```

| Guardar | Dónde | Por qué |
|---------|-------|---------|
| Preferencias (tema, idioma, densidad) | `localStorage` | Deben sobrevivir al cierre del navegador |
| Borrador de formulario, paso de un wizard | `sessionStorage` | Sirve al volver de otra pestaña, no dentro de un mes |
| Filtros, orden, página, tab activo | **La URL** | Compartible y con botón "atrás"; no es persistencia |
| Datos de la API | En ningún lado | Es estado de servidor: tiene caché propia |
| Tokens, credenciales, datos personales | **Nunca** | Cualquier XSS los lee |

### Rehidratación

El estado persistido llega **después** del primer render. Si la app pinta antes de leerlo, el usuario ve un parpadeo (el tema claro un instante antes del oscuro). Lee la preferencia antes de pintar o mantén un estado "hidratando" explícito; no lo tapes con un `useEffect` que corrige el valor a destiempo.

## Cuándo subir a algo más pesado

- **Context** para config que casi no cambia.
- **Store con selectores** para el 90% del estado de cliente compartido que sobra.
- **Redux Toolkit** justificado por **necesidades concretas**, no por tamaño: transiciones auditables, middleware de efectos, viaje en el tiempo, o un equipo grande que necesita una sola forma impuesta de cambiar estado.

Migrar de Context a store es un cambio local **si desde el principio consumiste el estado a través de un hook propio** y no de `useContext` crudo en cada componente. Ese es el motivo real de encapsular cada Context en un hook: te deja cambiar de nivel sin tocar los consumidores.

## Cuándo usar / no usar

| Usar un store | No usar un store |
|---------------|------------------|
| Estado de dominio que cambia seguido y leen componentes lejanos | Config de ambiente (tema, locale): eso es Context |
| Necesitas leer o escribir estado fuera de React | Filtros y paginación de una tabla: eso es la URL o estado local |
| Ya partiste el Context en tres Providers y sigue re-renderizando | Datos de la API: eso es estado de servidor |
| Dos slices se afectan y necesitas coordinarlas | Estado que solo lee un componente |

> Fuente: *React 19 Design Patterns and Best Practices* (Carlos Santana Roldán) — Ch.4 Advanced State Management Techniques. Ejemplos de contextos y filtros de `sociofit-webclient`.

---

*Rogelio Arriaga Gonzalez*
