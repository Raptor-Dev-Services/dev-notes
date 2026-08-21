# 10 · Fronteras de import — que la arquitectura falle sola

## Problema que resuelve

Separar por feature es fácil de decidir y difícil de sostener. Nadie escribe un import cruzado a propósito: se escribe con prisa, no rompe nada visible, y pasa la revisión porque el diff se ve razonable. Un año después mover un archivo rompe media app y nadie sabe cuándo empezó.

El import de más no falla. Solo **acopla**. Por eso la restricción tiene que verificarse en CI, no recordarse.

## El flujo: un solo sentido

```
app        (rutas, layouts, composicion de pantallas)
  |
  v
features   (modulos de negocio)
  |
  v
shared     (componentes, hooks, lib, utils, config, tipos)
```

Tres restricciones, sin excepciones:

1. **`shared` no importa de `features` ni de `app`.** Si un componente compartido necesita lógica de un feature, la recibe por parámetro o prop. Si no puede, es que no era compartido y pertenece al feature.
2. **`features` no importa de `app`.** Un feature no sabe en qué ruta lo montaron. Así se puede reusar y probar aislado.
3. **Un feature no importa de otro feature salvo dependencia declarada.** Se declara una lista explícita: `members` puede usar `auth`; `members` no puede usar `billing`. Lo que dos features necesiten compartir sube a `shared`.

```js
// PERMITIDO: app importa de feature
// src/app/routes/members.jsx
import { MembersPage } from '@/features/members/MembersPage'

// PERMITIDO: feature importa de auth (dependencia declarada)
// src/features/members/components/MembersTable.jsx
import { useSession } from '@/features/auth/hooks/useSession'

// PERMITIDO: feature importa de compartido
import { Modal } from '@/ui/primitives/Modal'
import { centsToUnits } from '@/utils/money'

// PROHIBIDO: feature importa de app
import { loader } from '@/app/routes/members'

// PROHIBIDO: feature importa de un feature no declarado
import { InvoiceCard } from '@/features/billing/components/InvoiceCard'

// PROHIBIDO: compartido importa de feature
// src/ui/primitives/UserBadge.jsx
import { useSession } from '@/features/auth/hooks/useSession'
```

## Cómo se verifica

Con `import/no-restricted-paths` de `eslint-plugin-import`, declarando zonas: `target` es lo que se protege, `from` es de dónde no puede importar.

```js
// eslint.config.js
import { importRules } from './infra/eslint-import-rules.js'

export default [
  {
    rules: {
      'import/no-restricted-paths': ['error', { zones: importRules }],
    },
  },
]
```

```js
// infra/eslint-import-rules.js
const zones = []

// 1. Lo compartido se mantiene independiente
zones.push({
  target: [
    './src/ui/**', './src/utils/**', './src/config/**',
    './src/hooks/**', './src/lib/**', './src/stores/**',
  ],
  from: ['./src/features/**', './src/app/**'],
  message: 'Lo compartido no puede importar de features ni de app.',
})

// 2. Los features no conocen la capa de rutas
zones.push({
  target: './src/features/**',
  from: './src/app/**',
  message: 'Un feature no puede importar de app.',
})

// 3. Dependencias entre features: explicitas y controladas
const features = [
  { name: 'auth',    allowedFeatures: [] },
  { name: 'members', allowedFeatures: ['auth'] },
  { name: 'billing', allowedFeatures: ['auth'] },
  { name: 'pos',     allowedFeatures: ['auth', 'products'] },
]

features.forEach(({ name, allowedFeatures }) => {
  const forbidden = features
    .filter((f) => f.name !== name && !allowedFeatures.includes(f.name))
    .map((f) => `./src/features/${f.name}/**`)

  if (forbidden.length) {
    zones.push({
      target: `./src/features/${name}/**`,
      from: forbidden,
      message: `El feature "${name}" solo puede importar de: ${allowedFeatures.join(', ') || 'ninguno'}.`,
    })
  }
})

export const importRules = zones
```

`auth` es la base y no importa de nadie. El resto puede usar `auth` y nada más entre pares. Lo que dos features de un mismo nivel necesiten compartir sube a `shared`.

## Qué evita

- **Dependencias circulares.** Con jerarquía estricta son imposibles, no solo improbables.
- **Acoplamiento que nadie decidió.** El import cruzado se ve en el PR como una línea más; en CI se ve como un error con nombre.
- **Deriva arquitectónica.** La violación se atrapa el día que se escribe, no el día que duele.
- **Dependencias ocultas en lo compartido.** Un primitivo que importa de un feature deja de ser reusable sin que su nombre lo diga.
- **Features intestables.** Si un feature no importa de `app`, se puede montar en una prueba sin el router entero.

## Cómo adoptarlo en un repo que ya tiene violaciones

Encenderlo de golpe en un repo grande produce cientos de errores y termina en `eslint-disable`. Por capas:

1. **Enciende la regla en modo `warn`** y cuenta las violaciones por zona.
2. **Empieza por la restricción 1** (`shared` no importa de features). Suele ser la que menos violaciones tiene y la que más valor da: deja lo compartido realmente compartido.
3. **Sube esa zona a `error`** cuando llegue a cero. Ya no puede volver a romperse.
4. **Repite con la 2 y la 3.** La 3 es la más costosa: aprovecha para decidir qué sube a `shared`.
5. **Declara las dependencias entre features** con lo que ya existe, no con lo que te gustaría. La lista describe la realidad; apretarla es un refactor aparte.

## Nota sobre la convención de carpetas

El nombre de las capas importa menos que el sentido único. `sociofit-webclient` usa `src/features/` para los módulos, `src/api/` para el acceso HTTP, y `src/ui/`, `src/utils/`, `src/auth/`, `src/config/` como compartidos. Es la misma jerarquía con otros nombres: ajusta los globs, no la estructura.

Si tu proyecto separa por `src/pages/` + `src/components/<modulo>/` en vez de carpeta por feature, la restricción sigue aplicando: lo importante es la separación por capa, no la ruta exacta.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Más de un desarrollador toca el repo | Prototipo o proyecto de una sola persona y corta vida |
| El proyecto pasó de ~10 features | App pequeña donde todo cabe en la cabeza |
| Ya hubo un "mover esto rompió aquello" | El equipo aún está definiendo la estructura |
| Quieres que features se prueben aislados | No hay CI que corra el lint |

> Fuente: *React Application Architecture for Production*, 2ª ed. (Alan Alickovic, Anthony Alicea) — Ch.2 Setup and Project Structure Overview.

---

*Rogelio Arriaga Gonzalez*
