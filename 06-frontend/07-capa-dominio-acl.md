# 07 · Capa de dominio y ACL — sacar el negocio de la vista

## Problema que resuelve

React no impone dónde va la lógica de negocio, así que por defecto acaba escrita donde se necesitó primero: dentro del componente. Las reglas quedan repartidas entre JSX y hooks, no se pueden testear sin renderizar, y cuando el backend cambia un contrato hay que buscar todas las copias de la traducción a mano.

La solución es una capa propia, en JavaScript plano, que no sabe que React existe.

## Reconocer la fuga

### Síntoma 1: transformación de datos dentro de la vista o del hook

El componente conoce los nombres de campo del backend:

```js
// MAL - la traduccion vive donde se consumio primero
setUser({
  id: data.user_identification,
  name: data.user_full_name,
  isPremium: data.is_premium_user,
})
```

Esa traducción se repite en cada pantalla que consume el endpoint. Cuando el backend renombra un campo, la copia que se te pase no da error: da `undefined` en pantalla.

### Síntoma 2: programación defensiva en el JSX

```jsx
// MAL - "sin plan es Basic" es una regla de negocio escondida en el markup
const level = user && user.subscription ? user.subscription : 'Basic'
const expiry = user && user.expire ? user.expire : 'Nunca'
```

Cada `??` en una vista es una decisión del negocio sin dueño, sin nombre y sin test.

### Síntoma 3: la máquina de estados disfrazada de configuración de UI

Este es el que más se escapa, porque parece configuración de botones:

```jsx
// MAL - visto en sociofit-webclient, SubscriptionSection.jsx
const ALLOWED = {
  renew: ['Active', 'Expired'],
  pause: ['Active'],
  resume: ['Paused'],
  cancel: ['Active', 'Paused', 'Pending'],
}
const can = (key) => canWrite && current && ALLOWED[key].includes(current.status)
```

No es configuración: es la máquina de estados de la suscripción. Cuando el backend agregue `Trial`, hay que encontrar esa tabla dentro de un `.jsx`, y si la misma regla se dibuja en otra pantalla ya hay dos tablas que no coinciden.

> **El olor de fondo es siempre el mismo:** para cambiar una regla de negocio tienes que abrir un archivo `.jsx`.

## El ACL (Anti-Corruption Layer)

Un ACL traduce entre dos sistemas que no hablan el mismo idioma. En el frontend se sitúa entre lo que responde el backend y lo que consume la aplicación.

**Regla:** la forma remota nunca cruza hacia arriba. Solo el ACL conoce el contrato del API; de ahí para arriba circulan tipos propios.

```js
// src/domain/member/transformer.js
const PLAN_LEVELS = ['basic', 'standard', 'premium', 'enterprise']

export function toMember(remote) {
  const level = remote.subscriptionDetails?.level?.toLowerCase()

  return {
    id: remote.id,
    name: remote.fullName?.trim() || 'Sin nombre',
    // Regla de negocio explicita, con nombre y testeable:
    // sin plan reconocido, es basic.
    plan: PLAN_LEVELS.includes(level) ? level : 'basic',
    expiresAt: remote.expiryUtc ? new Date(remote.expiryUtc) : null,
  }
}
```

Con eso el componente se queda sin traducción y sin defensivos:

```jsx
<h1>{member.name}</h1>
<PlanBadge level={member.plan} />
```

### Qué absorbe el ACL

- **Renombrado y aplanado** de campos.
- **Defaults** cuando el backend manda `null`, vacío o un valor no reconocido.
- **Normalización de formatos:** fechas a `Date`, dinero de centavos a unidades, enums a un casing único.
- **Descarte** de lo que la app no usa.
- **Varios orígenes hacia un mismo tipo:** dos transformers, un solo tipo de dominio.

### Si tu backend ya manda camelCase

Un backend propio bien portado (.NET, Spring, NestJS) manda los campos con nombres razonables, así que el renombrado deja de ser el motivo. **La fuga sigue existiendo** con otras caras:

- **Defensivos de forma repetidos en la capa de API.** En `sociofit-webclient`, `Array.isArray(data) ? data : (data?.items ?? [])` aparece cuatro veces solo en `api/members.js`, porque el endpoint a veces pagina y a veces no. Es una decisión sobre el contrato tomada cuatro veces.
- **Unidades crudas.** El importe llega en centavos y la fecha en UTC. Convertir es dominio; hacerlo en cada vista garantiza que una lo haga distinto.
- **Estados como cadenas sueltas.** `status === 'Paused'` regado por componentes, sin un lugar que diga qué estados existen.

El ACL vale por **aislar del contrato**, no por renombrar.

### Dónde encaja

```
red -> interceptor (desenvuelve el envelope) -> servicio -> ACL -> dominio -> hook -> vista
```

El envelope es transporte; el ACL es traducción. No los mezcles en la misma función: el servicio de `src/api/` resuelve el envelope y llama al transformer, y devuelve tipos de dominio.

## El modelo de dominio

Un objeto plano basta mientras el dato sea solo dato. Cuando aparece **comportamiento**, sube a modelo: un módulo de funciones puras o una clase.

La señal para promoverlo: el mismo cálculo sobre la misma entidad aparece en dos lugares, o tiene reglas propias (redondeos, topes, vigencias) que quieres testear sin montar un componente.

```js
// src/domain/member/Member.js
export class Member {
  constructor(remote) {
    this._plan = normalizePlan(remote.subscriptionDetails?.level)
    this._expiresAt = remote.expiryUtc ? new Date(remote.expiryUtc) : null
  }

  get isActive() {
    return this._expiresAt === null || this._expiresAt > new Date()
  }

  canAccess(feature) {
    return this.isActive && PLAN_FEATURES[this._plan].includes(feature)
  }
}
```

`canAccess` se testea con un `new Member(...)` y cero infraestructura de React.

### Las tres prohibiciones

1. **Cero imports de React.** Ni hooks, ni JSX. Si el modelo necesita un hook, la lógica está mal repartida.
2. **Cero acceso a la red.** El modelo recibe datos ya traídos; quien trae es el servicio.
3. **Cero estado de UI.** "Está abierto el modal" no es dominio.

Con las tres, el modelo se testea con pruebas unitarias normales y sobrevive a un cambio de framework.

> **Ojo con las instancias y el estado de React.** Un modelo con métodos es un objeto mutable; React compara por referencia. Si guardas instancias en `useState`, nunca mutes la instancia en su sitio: crea una nueva. Cuando el modelo solo calcula, prefiere funciones puras sobre objetos planos y evitas el problema entero.

## Reglas que varían: Strategy

Cuando una operación tiene varias versiones que se eligen en runtime (descuentos por temporada, tarifas por tipo de cliente), no la escribas como escalera de `if` dentro del modelo.

```js
// src/domain/pricing/strategies.js
export const noDiscount = { calculate: () => 0 }
export const seasonalDiscount = (rate) => ({ calculate: (price) => price * rate })

// La decision de CUAL aplicar es una funcion pura, no un if en el onClick
export function resolveStrategy(item, today) {
  if (isPromoDay(today)) return seasonalDiscount(0.15)
  if (item.category === 'bundle') return bundleDiscount
  return noDiscount
}
```

Quitar la promoción el lunes es borrar una línea de una función pura, no auditar componentes.

**No lo apliques por defecto.** Con una sola variante, un `if` está bien. La señal es la tercera variante, o que la regla tenga fecha de caducidad.

## Las capas y el sentido único

```
views/     JSX. Pinta. Recibe props ya calculadas.
   |
   v
hooks/     Estado, efectos, orquestacion. Traduce dominio -> props.
   |
   v
domain/    Reglas, calculos, ACL. JS puro, sin React.
```

**La dependencia va en un solo sentido: hacia abajo.**

| Capa | Sí | No |
|------|----|----|
| Vista (`.jsx`) | Layout, composición, eventos, estados de carga | Cálculos de negocio, nombres de campo del API, fallbacks de datos |
| Hook | Estado, efectos, llamar al servicio, exponer props listas | Reglas de negocio propias, traducción del contrato |
| Dominio / ACL | Traducción, defaults, cálculos, invariantes, estrategias | React, red, estado de UI |

**El nombre de la carpeta sigue la convención del repo.** Si tus módulos puros y testeados ya viven en `utils/`, esa carpeta ya es la capa de dominio y renombrarla es churn. Lo innegociable son las tres prohibiciones y que tenga prueba propia.

## Caso real: por qué la duplicación es cara

La conversión de centavos a unidades vivía copiada en seis features de `sociofit-webclient`, con **ocho nombres** para dos operaciones. Cinco copias eran idénticas y la sexta devolvía `"0.00"` donde las demás devolvían cadena vacía, **bajo el mismo nombre de función**.

Consecuencia: mover un componente de un feature a otro cambiaba lo que se veía en pantalla. No había error, no había test rojo, y el diagnóstico costó más que el arreglo.

**Las copias no se rompen, divergen en silencio.** Por eso la señal de "súbelo a compartido" es la **segunda** copia, no la quinta: mientras haya dos, todavía se pueden comparar.

## Cómo migrar un componente que ya tiene la fuga

No reescribas la pantalla. Ve por capas, de abajo hacia arriba:

1. **Cubre con prueba lo que ya funciona.** Sin red, el refactor es a ciegas.
2. **Extrae el transformer.** Corta el mapeo a `transformer.js` y llámalo desde el servicio.
3. **Absorbe los defensivos.** Cada `??` de la vista que rellena un dato ausente se muda al transformer.
4. **Extrae los cálculos.** Cada función pura que no toque React sale a `domain/`.
5. **Promueve a modelo solo si aparece comportamiento** compartido o con reglas propias.
6. **Deja el hook orquestando y la vista pintando.**

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| El backend cambia contratos y rompe pantallas | CRUD trivial que solo lista y muestra lo que llega |
| Hay reglas de negocio en el front (vigencias, elegibilidad, totales) | Toda la lógica vive en el backend y el front solo pinta |
| El mismo concepto lo consumen dos o más features | Un solo feature, sin cálculos, sin previsión de crecer |
| Quieres testear reglas sin renderizar componentes | Prototipo desechable |

> **TypeScript no es requisito.** Los ejemplos van en JS porque `sociofit-webclient` es JS puro. Lo que TS agregaría es que el cambio de contrato falle en el build; sin TS esa red la pone la prueba del transformer, que pasa de recomendable a obligatoria.

> Fuente: *React Anti-Patterns* (Juntao Qiu) — Ch.8 Exploring Data Management in React, Ch.11 Introducing Layered Architecture in React. Ejemplos reales de `sociofit-webclient`.

---

*Rogelio Arriaga Gonzalez*
