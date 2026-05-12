# 03 · Tailwind CSS v4 con Vite

## Problema que resuelve

Tailwind CSS v4 cambia la forma de instalación y configuración respecto a v3. Ya no usa `tailwind.config.js` ni `postcss.config.js` — se integra directamente como plugin de Vite y la configuración va en el CSS.

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

## Helper cx() — clases condicionales

Función utilitaria para componer clases de forma condicional sin dependencias externas:

```js
// src/styles/designSystem.js
export function cx(...classes) {
  return classes.filter(Boolean).join(' ')
}
```

```jsx
// uso
import { cx, ui } from '../../styles/designSystem'

<Link className={cx(
  ui.controls.navItem,
  active ? ui.controls.navItemActive : ui.controls.navItemIdle
)}>
  {label}
</Link>
```

`cx()` filtra valores falsy (`false`, `null`, `undefined`, `''`) y une el resto con espacio. Equivalente a `clsx` sin necesidad de instalarlo.

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

## Clases de estado

```jsx
// hover, focus-visible, disabled
<button className="bg-slate-900 hover:bg-slate-700 focus-visible:outline-2 disabled:opacity-50">
  Guardar
</button>
```

## Relación con front-template

El front-template usa `@tailwindcss/vite` v4.1.x. No tiene `tailwind.config.js`. Todo el theming está centralizado en `src/styles/designSystem.js` como tokens de strings de clases (ver `04-design-system.md`).

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| cualquier proyecto Vite + React nuevo | proyectos con Webpack (usar PostCSS + v3) |
| componentes que consumen clases directamente | cuando se necesitan temas dinámicos en runtime (usar CSS custom properties) |


---

*Rogelio Arriaga Gonzalez*
