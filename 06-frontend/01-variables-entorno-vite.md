# 01 — Variables de Entorno en Vite

Vite tiene un sistema simple pero estricto. Solo expone al cliente las variables con prefijo `VITE_`. Todas las demás están disponibles únicamente en build-time (Node), nunca llegan al navegador.

> Fuente: *Full Stack React, TypeScript, and Node* (David Choi) — Ch.3 Creating React Apps with Vite

---

## Archivos `.env` reconocidos por Vite

| Archivo | Cuándo se carga |
|---------|----------------|
| `.env` | Siempre — variables compartidas por todos los modos |
| `.env.local` | Siempre salvo en CI — overrides personales, **NUNCA al repo** |
| `.env.development` | Solo en modo dev (`npm run dev`) |
| `.env.production` | Solo en build (`npm run build`) |
| `.env.development.local` | Override local de development |
| `.env.production.local` | Override local de production |

**Orden de precedencia** (de menor a mayor):  
`.env` → `.env.{mode}` → `.env.local` → `.env.{mode}.local`

Los archivos `.local` **nunca** se comitean. Deben estar en `.gitignore`.

---

## Ejemplo real — SaaS multi-tenant

```env
# .env (compartido, va al repo)
VITE_APP_NAME=TaskFlow
VITE_DEFAULT_LOCALE=es-MX

# .env.development (va al repo)
VITE_API_BASE_URL=http://localhost:5000/api
VITE_WS_URL=ws://localhost:5000/hubs
VITE_STRIPE_PUBLIC_KEY=pk_test_51AbCd...
VITE_ENABLE_DEVTOOLS=true

# .env.production (va al repo, solo valores no sensibles)
VITE_API_BASE_URL=https://api.taskflow.com/api
VITE_WS_URL=wss://api.taskflow.com/hubs
VITE_ENABLE_DEVTOOLS=false

# .env.local (NUNCA al repo — override personal del dev)
VITE_API_BASE_URL=http://192.168.1.50:5000/api
```

---

## Leer variables en código — centralizar en `config.js`

No dispersar `import.meta.env.VITE_*` por todo el código. Centralizar en un archivo de configuración:

```js
// src/config/env.js — único punto de acceso a variables de entorno
export const config = {
  appName:       import.meta.env.VITE_APP_NAME ?? 'App',
  apiBaseUrl:    import.meta.env.VITE_API_BASE_URL,
  wsUrl:         import.meta.env.VITE_WS_URL,
  stripeKey:     import.meta.env.VITE_STRIPE_PUBLIC_KEY,
  defaultLocale: import.meta.env.VITE_DEFAULT_LOCALE ?? 'en-US',
  enableDevtools: import.meta.env.VITE_ENABLE_DEVTOOLS === 'true',
  isDev:         import.meta.env.DEV,
  isProd:        import.meta.env.PROD,
  mode:          import.meta.env.MODE,
};

// Validación al inicio — falla rápido si falta algo crítico
if (!config.apiBaseUrl) {
  throw new Error('VITE_API_BASE_URL es requerido. Define la variable de entorno.');
}
```

```jsx
// Uso en componentes — importar config, no import.meta.env directamente
import { config } from '../config/env';

const api = axios.create({ baseURL: config.apiBaseUrl });
```

---

## Variables nativas de Vite (sin prefijo)

Estas las provee Vite automáticamente — no requieren definición manual:

| Variable | Valor |
|----------|-------|
| `import.meta.env.DEV` | `true` en dev, `false` en producción |
| `import.meta.env.PROD` | `true` en producción |
| `import.meta.env.MODE` | `"development"` o `"production"` |
| `import.meta.env.BASE_URL` | La base URL del app (configurable en `vite.config.js`) |

---

## TypeScript — tipos para variables de entorno

Para evitar `any` al acceder a `import.meta.env`:

```ts
// src/env.d.ts
/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_APP_NAME: string;
  readonly VITE_API_BASE_URL: string;
  readonly VITE_WS_URL: string;
  readonly VITE_STRIPE_PUBLIC_KEY: string;
  readonly VITE_DEFAULT_LOCALE: string;
  readonly VITE_ENABLE_DEVTOOLS: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
```

---

## Seguridad — qué NO poner con prefijo `VITE_`

> Las variables `VITE_*` se incrustan en el bundle final que el navegador descarga. Cualquier usuario puede leerlas inspeccionando el código fuente.

```
✗ VITE_DB_PASSWORD       — contraseña de base de datos
✗ VITE_STRIPE_SECRET_KEY — sk_live_... (clave secreta de Stripe)
✗ VITE_JWT_SECRET        — clave de firma de tokens
✓ VITE_STRIPE_PUBLIC_KEY — pk_live_... (clave pública — diseñada para el navegador)
✓ VITE_API_BASE_URL      — URL de la API (no es un secreto)
```

Los secretos reales van en variables de entorno sin prefijo `VITE_` — accesibles en build scripts de Node, no en el bundle del navegador.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Variable de entorno | valor de configuración externo al código fuente que cambia según el ambiente de ejecución |
| Prefijo VITE_ | convención de Vite que determina qué variables de entorno se incrustan en el bundle del navegador |
| `import.meta.env` | objeto de Vite que expone las variables de entorno con prefijo `VITE_` en tiempo de compilación |
| `.env.local` | archivo de variables de entorno específico de la máquina del desarrollador; nunca se sube al repositorio |
| Tree shaking | técnica del bundler que elimina código no referenciado; las variables no incrustadas no llegan al bundle |
| `ImportMetaEnv` | interfaz TypeScript que tipifica las variables de entorno de Vite para obtener autocompletar en el editor |
| Bundle | archivo JavaScript comprimido y optimizado que Vite genera para el navegador durante el build de producción |
| Secret leak | filtración accidental de credenciales en el código o en el bundle del navegador |
| Modo de Vite | contexto de ejecución (`development`, `staging`, `production`) que determina qué archivo `.env` se carga |

---

*Rogelio Arriaga Gonzalez*
