# 04 · Design System — tokens de clases Tailwind

## Problema que resuelve

Cuando los strings de clases Tailwind están dispersos en cada componente, cambiar el color de los botones primarios requiere buscar y reemplazar en todo el proyecto. Centralizar las clases en un objeto de tokens (`ui`) asegura que todos los componentes consuman el mismo lenguaje visual y que los cambios de estilo sean un cambio en un solo lugar.

## Estructura del objeto ui

```js
// src/styles/designSystem.js
export const ui = {
  layout:     { ... },   // contenedores de página
  surface:    { ... },   // paneles, tarjetas, drawers, modals
  typography: { ... },   // jerarquía tipográfica
  controls:   { ... },   // inputs, botones, nav items
  feedback:   { ... },   // errores, estados vacíos
  table:      { ... },   // tabla y sus partes
  drawer:     { ... },   // panel lateral deslizante
  modal:      { ... },   // ventana modal
  badge:      { ... },   // etiquetas de estado
}
```

## Categorías y tokens principales

### layout — estructura de página

```js
appSection:   'flex min-h-[calc(100vh-6.5rem)] flex-col gap-4 ...',
moduleShell:  'flex min-h-[calc(100vh-6.5rem)] ... rounded-xl border bg-slate-50 ...',
stickyTopBar: 'sticky top-0 z-50 border-b border-gray-200 bg-white/95 backdrop-blur',
```

### surface — contenedores visuales

```js
panel:      'rounded-xl border border-slate-200 bg-white shadow-sm',
panelSoft:  'rounded-xl border border-slate-200 bg-slate-50 shadow-sm',
toolbar:    'rounded-[1.5rem] border border-slate-200 bg-white p-4 shadow-sm ...',
tablePanel: 'overflow-hidden rounded-lg border border-slate-200 bg-white',
modal:      'w-full max-w-md rounded-xl bg-white shadow-xl',
```

### typography — jerarquía de texto

```js
heroTitle:   'text-2xl font-black tracking-tight text-slate-900 sm:text-3xl lg:text-4xl',
pageTitle:   'text-lg font-semibold text-slate-900 sm:text-xl',
sectionTitle:'text-xl font-semibold text-slate-900',
cardTitle:   'text-sm font-semibold text-slate-900 sm:text-base',
body:        'text-sm leading-6 text-slate-600',
tableHead:   'text-xs font-semibold uppercase tracking-wide text-slate-600',
```

### controls — botones e inputs

```js
primaryButton:    'inline-flex min-h-11 ... bg-slate-900 ... text-white ...',
accentButton:     'inline-flex min-h-11 ... bg-[#ff6100] ... text-white ...',
secondaryButton:  'inline-flex min-h-11 ... border border-slate-300 bg-white ...',
destructiveButton:'inline-flex min-h-11 ... bg-red-600 ... text-white ...',
input:            'min-h-11 rounded-md border border-slate-300 bg-white ...',
navItem:          'min-h-11 whitespace-nowrap rounded-md px-3 py-2 text-sm font-semibold ...',
navItemActive:    'bg-slate-900 text-white',
navItemIdle:      'text-slate-700 hover:bg-slate-100',
```

### badge — etiquetas de estado

```js
success: 'inline-flex rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-700',
warning: 'inline-flex rounded-full bg-amber-100 ...',
blocked: 'inline-flex rounded-full bg-rose-100 ...',
info:    'inline-flex rounded-full bg-sky-100 ...',
neutral: 'inline-flex rounded-full bg-slate-200 ...',
```

## Uso en componentes

```jsx
import { cx, ui } from '../../styles/designSystem'

// botón primario
<button className={ui.controls.primaryButton}>
  Guardar
</button>

// título de página
<h1 className={ui.typography.pageTitle}>
  Usuarios
</h1>

// panel con contenido
<div className={ui.surface.panel}>
  ...
</div>

// badge de estado
<span className={row.isActive ? ui.badge.success : ui.badge.neutral}>
  {row.isActive ? 'Activo' : 'Inactivo'}
</span>

// nav item activo/inactivo combinando cx
<Link className={cx(ui.controls.navItem, active ? ui.controls.navItemActive : ui.controls.navItemIdle)}>
  {label}
</Link>
```

## Theming por tenant

Para adaptar el design system a un tenant específico, se puede parametrizar el color de acento. La variable `#ff6100` (naranja Raptor) puede extraerse como CSS custom property:

```css
/* src/index.css */
@import "tailwindcss";

:root {
  --color-accent: #ff6100;
  --color-accent-hover: #ff7b00;
}
```

```js
// controles que usan el acento del tenant
accentButton: 'inline-flex ... bg-[var(--color-accent)] hover:bg-[var(--color-accent-hover)] ...',
```

Al cargar la app, la API del tenant devuelve el color primario y se aplica al `:root`:

```js
document.documentElement.style.setProperty('--color-accent', tenant.primaryColor)
```

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| cualquier componente del proyecto | componentes externos o de librerías (Headless UI, etc.) |
| cuando se quiere cambiar el estilo globalmente desde un punto | cuando se necesita override local puntual (usar `cx()` para extender) |


---

*Rogelio Arriaga Gonzalez*
