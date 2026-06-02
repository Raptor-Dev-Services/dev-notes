# 03 · Tailwind CSS v4 con Vite

Tailwind CSS v4 cambia radicalmente la forma de instalación y configuración respecto a v3. Ya no usa `tailwind.config.js` ni `postcss.config.js` — se integra directamente como plugin de Vite y toda la configuración va en el CSS mediante `@theme` y CSS custom properties.

> Fuentes: *Full Stack React, TypeScript, and Node* (David Choi) — Ch.4 Styling with Tailwind CSS; documentación oficial Tailwind CSS v4

---

## Instalación (v4 + Vite)

```bash
npm install tailwindcss @tailwindcss/vite
```

```js
// vite.config.js
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
})
```

```css
/* src/index.css */
@import "tailwindcss";
```

```jsx
// src/main.jsx
import './index.css'
```

Una sola línea en el CSS reemplaza las tres directivas de v3 (`@tailwind base/components/utilities`).

---

## Helper cx() — clases condicionales

Función utilitaria para componer clases de forma condicional sin dependencias externas:

```js
// src/styles/designSystem.js
export function cx(...classes) {
  return classes.filter(Boolean).join(' ')
}
```

```jsx
import { cx, ui } from '../../styles/designSystem'

<Link className={cx(
  ui.controls.navItem,
  active ? ui.controls.navItemActive : ui.controls.navItemIdle
)}>
  {label}
</Link>
```

`cx()` filtra valores falsy (`false`, `null`, `undefined`, `''`) y une el resto con espacio. Equivalente a `clsx` sin necesidad de instalarlo.

---

## Breakpoints responsive

Tailwind usa mobile-first. Los prefijos añaden estilos desde ese breakpoint hacia arriba:

| Prefijo | Breakpoint | Uso típico |
|---------|-----------|------------|
| (sin prefijo) | 0px+ | mobile base |
| `sm:` | 640px+ | tablet pequeña |
| `md:` | 768px+ | tablet |
| `lg:` | 1024px+ | desktop |
| `xl:` | 1280px+ | pantalla grande |

```jsx
// oculto en mobile, visible desde lg
<div className="hidden lg:block" />

// padding crece por breakpoint
<main className="p-3 sm:p-4 lg:p-5" />
```

---

## Clases de estado

```jsx
// hover, focus-visible, disabled
<button className="bg-slate-900 hover:bg-slate-700 focus-visible:outline-2 disabled:opacity-50">
  Guardar
</button>
```

---

## Dark mode con CSS custom properties

En v4, `dark:` funciona por defecto con la clase `dark` en el `<html>`. La forma preferida es combinar `dark:` con CSS custom properties para que el cambio de tema sea un cambio de variables.

```css
/* src/index.css */
@import "tailwindcss";

/* tema claro (default) */
:root {
  --color-bg:       #ffffff;
  --color-surface:  #f8fafc;
  --color-border:   #e2e8f0;
  --color-text:     #0f172a;
  --color-muted:    #64748b;
  --color-accent:   #ff6100;
  --color-accent-h: #e55500;
}

/* tema oscuro — reemplaza solo las variables */
.dark {
  --color-bg:       #0f172a;
  --color-surface:  #1e293b;
  --color-border:   #334155;
  --color-text:     #f1f5f9;
  --color-muted:    #94a3b8;
  --color-accent:   #ff7b25;
  --color-accent-h: #ff9a50;
}
```

```jsx
// Componente que usa las variables — las clases dark: no son necesarias
// porque el color proviene de las CSS vars que ya cambiaron
<div style={{ background: 'var(--color-surface)', color: 'var(--color-text)' }}>
  Contenido
</div>

// Alternativa con clases de Tailwind arbitrarias
<div className="bg-[var(--color-surface)] text-[var(--color-text)]">
  Contenido
</div>
```

```jsx
// Toggle de dark mode
export function ThemeToggle() {
  const toggle = () => {
    document.documentElement.classList.toggle('dark')
    // Persistir en localStorage
    const isDark = document.documentElement.classList.contains('dark')
    localStorage.setItem('theme', isDark ? 'dark' : 'light')
  }

  return <button onClick={toggle}>Cambiar tema</button>
}

// Aplicar al cargar la app (evita flash)
// src/main.jsx — antes del ReactDOM.createRoot
const saved = localStorage.getItem('theme')
const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
if (saved === 'dark' || (!saved && prefersDark)) {
  document.documentElement.classList.add('dark')
}
```

---

## Temas por tenant con CSS custom properties

Para adaptar la paleta de color a cada tenant, la API devuelve los colores primarios del tenant y se aplican al `:root` en tiempo de ejecución:

```js
// src/auth/applyTenantTheme.js
export function applyTenantTheme(tenant) {
  const root = document.documentElement

  root.style.setProperty('--color-accent',   tenant.primaryColor   ?? '#ff6100')
  root.style.setProperty('--color-accent-h', tenant.primaryColorHover ?? '#e55500')

  // Calcular variante dark automáticamente (mezcla con negro)
  // o usar colores explícitos del tenant si los provee
  if (tenant.darkPrimaryColor) {
    // .dark también se sobreescribe dinámicamente
    const style = document.getElementById('tenant-dark-theme') ?? createStyleTag()
    style.textContent = `.dark { --color-accent: ${tenant.darkPrimaryColor}; }`
  }
}

function createStyleTag() {
  const el = document.createElement('style')
  el.id = 'tenant-dark-theme'
  document.head.appendChild(el)
  return el
}
```

```js
// Se llama después del login / al inicializar la sesión
applyTenantTheme(session.tenant)
```

Los botones del design system usan `bg-[var(--color-accent)]` en lugar de `bg-orange-500`, por lo que el cambio de color es automático y global:

```js
// src/styles/designSystem.js
accentButton: 'inline-flex min-h-11 rounded-lg px-4 py-2 text-sm font-semibold text-white ' +
              'bg-[var(--color-accent)] hover:bg-[var(--color-accent-h)] ' +
              'focus-visible:outline-2 focus-visible:outline-[var(--color-accent)] ' +
              'disabled:opacity-50 transition-colors',
```

---

## Plugins personalizados en v4

En v4, los plugins se declaran con `@plugin` en el CSS o mediante la API JavaScript `addUtilities`/`addComponents`.

```css
/* src/index.css — plugin inline en CSS */
@import "tailwindcss";
@plugin "tailwindcss/plugin" {
  /* utilidad custom: truncate con n líneas */
  .line-clamp-2 {
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }
}
```

```js
// vite.config.js — plugin vía JavaScript (para lógica dinámica)
import { defineConfig } from 'vite'
import tailwindcss from '@tailwindcss/vite'
import plugin from 'tailwindcss/plugin'

const raptorPlugin = plugin(({ addUtilities, addComponents, theme }) => {
  // Utilidades: clases de una sola propiedad
  addUtilities({
    '.scrollbar-hidden': {
      scrollbarWidth: 'none',
      '&::-webkit-scrollbar': { display: 'none' },
    },
    '.text-balance': { textWrap: 'balance' },
  })

  // Componentes: clases con múltiples propiedades reutilizables
  addComponents({
    '.badge': {
      display: 'inline-flex',
      alignItems: 'center',
      borderRadius: '9999px',
      padding: '0.125rem 0.5rem',
      fontSize: '0.75rem',
      fontWeight: '600',
    },
  })
})

export default defineConfig({
  plugins: [tailwindcss({ plugins: [raptorPlugin] })],
})
```

---

## Animaciones con animate-*

Tailwind v4 incluye las clases `animate-*` sobre CSS `@keyframes` estándar. También permite definir animaciones personalizadas con `@keyframes` en el CSS de configuración.

```jsx
// Clases de animación estándar de Tailwind
<div className="animate-spin">   {/* rotación infinita */}
<div className="animate-ping">   {/* pulso de red/radar */}
<div className="animate-pulse">  {/* fade in/out suave */}
<div className="animate-bounce"> {/* rebote vertical */}
```

```jsx
// Spinner de carga tipico
export function Spinner({ className = '' }) {
  return (
    <div className={`h-5 w-5 animate-spin rounded-full border-2 border-slate-200 border-t-slate-700 ${className}`} />
  )
}

// Skeleton loader — pulso para indicar carga
export function SkeletonRow() {
  return (
    <div className="flex gap-3 animate-pulse">
      <div className="h-4 w-24 rounded bg-slate-200" />
      <div className="h-4 flex-1 rounded bg-slate-200" />
      <div className="h-4 w-16 rounded bg-slate-200" />
    </div>
  )
}
```

```css
/* Animación personalizada en el CSS de configuración */
@import "tailwindcss";

@keyframes slide-in-right {
  from { transform: translateX(100%); opacity: 0; }
  to   { transform: translateX(0);    opacity: 1; }
}

@keyframes fade-out {
  from { opacity: 1; }
  to   { opacity: 0; }
}
```

```js
// Plugin para registrar las animaciones custom como clases de Tailwind
addUtilities({
  '.animate-slide-in-right': {
    animation: 'slide-in-right 0.25s ease-out',
  },
  '.animate-fade-out': {
    animation: 'fade-out 0.2s ease-in forwards',
  },
})
```

```jsx
// Drawer/toast que aparece desde la derecha
<div className="animate-slide-in-right fixed right-0 top-0 h-full w-80 bg-white shadow-xl">
  {/* contenido del drawer */}
</div>
```

---

## Relación con front-template

El front-template usa `@tailwindcss/vite` v4.1.x. No tiene `tailwind.config.js`. Todo el theming está centralizado en `src/styles/designSystem.js` como tokens de strings de clases (ver `04-design-system.md`). Los colores de acento del tenant se aplican vía CSS custom properties con `applyTenantTheme()` en el bootstrap de la sesión.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| cualquier proyecto Vite + React nuevo | proyectos con Webpack (usar PostCSS + v3) |
| CSS custom properties para temas dinámicos en runtime | clases hardcodeadas para colores que deben cambiar por tenant |
| `animate-pulse` y `animate-spin` para feedback de carga | animaciones complejas con múltiples pasos (preferir Framer Motion) |
| plugins para utilidades reutilizables del proyecto | plugins para reemplazar librerías de componentes completas |

---

## Glosario

| Término | Definición |
|---------|-----------|
| utility-first | enfoque de Tailwind donde los estilos se aplican con clases pequeñas y de propósito único |
| mobile-first | diseñar para móvil por defecto y añadir breakpoints para pantallas más grandes |
| CSS custom property | variable CSS nativa (`--nombre: valor`) accesible con `var(--nombre)` |
| `@theme` | bloque de configuración de Tailwind v4 en el CSS para definir tokens de diseño |
| JIT (Just-in-Time) | modo de compilación de Tailwind que genera solo las clases usadas — estándar en v4 |
| dark mode | modo de visualización de alto contraste oscuro activado por clase `dark` o media query |
| `@keyframes` | declaración CSS para definir las etapas de una animación |
| plugin | extensión de Tailwind para agregar utilidades, componentes o variantes personalizadas |

---

*Rogelio Arriaga Gonzalez*
