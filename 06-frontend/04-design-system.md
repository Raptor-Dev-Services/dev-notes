# 04 · Design System — tokens, tipografía y accesibilidad

Cuando los strings de clases Tailwind están dispersos en cada componente, cambiar el color de los botones primarios requiere buscar y reemplazar en todo el proyecto. Centralizar las clases en un objeto de tokens (`ui`) asegura que todos los componentes consuman el mismo lenguaje visual y que los cambios de estilo sean un cambio en un solo lugar.

> Fuentes: *Full-Stack Web Development with TypeScript 5* (Daishi Kato): Ch.5 Building a Component Design System; *Full Stack React, TypeScript, and Node* (David Choi): Ch.4 Styling with Tailwind CSS; WCAG 2.1 (W3C)

---

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

---

## Tokens de color con CSS custom properties

Los colores del design system viven en CSS custom properties, no en clases hardcodeadas. Esto permite que cambien en runtime (dark mode, tema por tenant) sin recompilar Tailwind.

```css
/* src/index.css */
@import "tailwindcss";

/* Paleta semántica — tokens de significado, no de valor */
:root {
  /* fondos */
  --color-bg:          #ffffff;
  --color-bg-subtle:   #f8fafc;
  --color-surface:     #ffffff;
  --color-surface-alt: #f1f5f9;

  /* bordes */
  --color-border:      #e2e8f0;
  --color-border-strong: #cbd5e1;

  /* texto */
  --color-text:        #0f172a;
  --color-text-muted:  #64748b;
  --color-text-subtle: #94a3b8;

  /* acento del tenant (override en runtime) */
  --color-accent:      #ff6100;
  --color-accent-h:    #e55500;   /* hover */
  --color-accent-soft: #fff0e8;   /* fondo suave sobre acento */

  /* estados semánticos */
  --color-success:     #059669;
  --color-success-bg:  #ecfdf5;
  --color-warning:     #d97706;
  --color-warning-bg:  #fffbeb;
  --color-error:       #dc2626;
  --color-error-bg:    #fef2f2;
  --color-info:        #0284c7;
  --color-info-bg:     #f0f9ff;

  /* interactivos */
  --color-focus:       #3b82f6;   /* outline de focus — alto contraste */
}

/* tema oscuro */
.dark {
  --color-bg:          #0f172a;
  --color-bg-subtle:   #1e293b;
  --color-surface:     #1e293b;
  --color-surface-alt: #334155;
  --color-border:      #334155;
  --color-border-strong: #475569;
  --color-text:        #f1f5f9;
  --color-text-muted:  #94a3b8;
  --color-text-subtle: #64748b;
  --color-accent:      #ff7b25;
  --color-accent-h:    #ff9a50;
  --color-accent-soft: #431607;
}
```

Los tokens de **color** (surface, border, text) los consume el design system; los tokens de **acento** se sobreescriben por tenant.

---

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

### typography — sistema de tipografía

La escala tipográfica usa `font-size` relativo con `leading` ajustado para legibilidad óptima en cada nivel:

```js
// Jerarquía de display
heroTitle:    'text-2xl font-black tracking-tight text-slate-900 sm:text-3xl lg:text-4xl',
pageTitle:    'text-lg font-semibold text-slate-900 sm:text-xl',
sectionTitle: 'text-xl font-semibold text-slate-900',
cardTitle:    'text-sm font-semibold text-slate-900 sm:text-base',

// Cuerpo y apoyo
body:         'text-sm leading-6 text-slate-600',
bodyStrong:   'text-sm font-semibold leading-6 text-slate-900',
caption:      'text-xs text-slate-500',
label:        'text-xs font-medium uppercase tracking-wide text-slate-500',

// Tabla
tableHead:    'text-xs font-semibold uppercase tracking-wide text-slate-600',
tableCell:    'text-sm text-slate-700',

// Código
code:         'font-mono text-sm rounded bg-slate-100 px-1.5 py-0.5 text-slate-800',
```

```jsx
// Escala en uso
<h1 className={ui.typography.pageTitle}>Usuarios</h1>
<p  className={ui.typography.body}>Lista de usuarios registrados en el sistema.</p>
<span className={ui.typography.caption}>Actualizado hace 3 minutos</span>
```

### controls — botones e inputs

```js
primaryButton:    'inline-flex min-h-11 items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold text-white ' +
                  'bg-slate-900 hover:bg-slate-700 focus-visible:outline focus-visible:outline-2 ' +
                  'focus-visible:outline-offset-2 focus-visible:outline-slate-900 ' +
                  'disabled:opacity-50 disabled:cursor-not-allowed transition-colors',

accentButton:     'inline-flex min-h-11 items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold text-white ' +
                  'bg-[var(--color-accent)] hover:bg-[var(--color-accent-h)] ' +
                  'focus-visible:outline focus-visible:outline-2 focus-visible:outline-[var(--color-accent)] ' +
                  'disabled:opacity-50 disabled:cursor-not-allowed transition-colors',

secondaryButton:  'inline-flex min-h-11 items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold ' +
                  'border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 ' +
                  'focus-visible:outline focus-visible:outline-2 focus-visible:outline-slate-500 ' +
                  'disabled:opacity-50 disabled:cursor-not-allowed transition-colors',

destructiveButton:'inline-flex min-h-11 items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold text-white ' +
                  'bg-red-600 hover:bg-red-700 focus-visible:outline focus-visible:outline-2 ' +
                  'focus-visible:outline-red-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors',

input:            'min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm ' +
                  'placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-500 ' +
                  'disabled:bg-slate-50 disabled:cursor-not-allowed',

navItem:          'min-h-11 whitespace-nowrap rounded-md px-3 py-2 text-sm font-semibold ' +
                  'focus-visible:outline focus-visible:outline-2 transition-colors',
navItemActive:    'bg-slate-900 text-white',
navItemIdle:      'text-slate-700 hover:bg-slate-100',
```

### badge — etiquetas de estado

```js
success: 'inline-flex rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-700',
warning: 'inline-flex rounded-full bg-amber-100  px-2 py-0.5 text-xs font-semibold text-amber-700',
blocked: 'inline-flex rounded-full bg-rose-100   px-2 py-0.5 text-xs font-semibold text-rose-700',
info:    'inline-flex rounded-full bg-sky-100    px-2 py-0.5 text-xs font-semibold text-sky-700',
neutral: 'inline-flex rounded-full bg-slate-200  px-2 py-0.5 text-xs font-semibold text-slate-600',
```

---

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

---

## Accesibilidad — componentes con ARIA

WCAG 2.1 define cuatro principios (POUR): **Perceptible**, **Operable**, **Comprensible**, **Robusto**. Los roles y atributos ARIA comunican la semántica a lectores de pantalla cuando el HTML nativo no es suficiente.

### Regla de oro de ARIA

> Nunca uses un rol ARIA cuando existe el elemento HTML semántico equivalente.
> `<button>` es mejor que `<div role="button">`. `<nav>` es mejor que `<div role="navigation">`.

### Botón con estado de carga

```jsx
export function LoadingButton({ loading, children, onClick, disabled }) {
  return (
    <button
      className={ui.controls.primaryButton}
      onClick={onClick}
      disabled={disabled || loading}
      aria-busy={loading}        // anuncia al lector que la acción está en proceso
      aria-disabled={disabled}   // distingue disabled semántico del atributo HTML
    >
      {loading && (
        <span
          className="h-4 w-4 animate-spin rounded-full border-2 border-white/30 border-t-white"
          aria-hidden="true"     // el spinner visual es decorativo — el texto ya lo explica
        />
      )}
      <span>{loading ? 'Guardando...' : children}</span>
    </button>
  )
}
```

### Modal accesible

```jsx
export function Modal({ open, onClose, title, children }) {
  const titleId = useId()   // ID único para aria-labelledby

  if (!open) return null

  return (
    // Portal fuera del árbol DOM principal para el foco
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}   // el modal anuncia su título al abrirse
      className="fixed inset-0 z-50 flex items-center justify-center"
    >
      {/* Overlay — click cierra */}
      <div
        className="absolute inset-0 bg-black/40"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Contenido */}
      <div className={ui.modal.content}>
        <h2 id={titleId} className={ui.typography.sectionTitle}>
          {title}
        </h2>

        <div>{children}</div>

        <button
          className="absolute right-4 top-4"
          onClick={onClose}
          aria-label="Cerrar modal"  // sin texto visible → aria-label es obligatorio
        >
          <XMarkIcon aria-hidden="true" className="h-5 w-5" />
        </button>
      </div>
    </div>
  )
}
```

### Tabla accesible

```jsx
export function DataTable({ columns, rows, caption }) {
  return (
    <div role="region" aria-label={caption} className="overflow-x-auto">
      <table className="w-full text-left">
        {caption && (
          <caption className="sr-only">{caption}</caption>  // visible solo para lectores
        )}
        <thead>
          <tr>
            {columns.map(col => (
              <th
                key={col.key}
                scope="col"               // indica que el th encabeza columnas
                className={ui.table.head}
                aria-sort={col.sorted}    // "ascending" | "descending" | "none"
              >
                {col.label}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map(row => (
            <tr key={row.id} className={ui.table.row}>
              {columns.map(col => (
                <td key={col.key} className={ui.table.cell}>
                  {row[col.key]}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
```

### Input con label y error asociados

```jsx
export function FormField({ id, label, error, required, ...inputProps }) {
  const errorId = `${id}-error`

  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={id} className={ui.typography.label}>
        {label}
        {required && (
          <span aria-hidden="true" className="ml-0.5 text-red-500">*</span>
        )}
        {required && (
          <span className="sr-only">(requerido)</span>
        )}
      </label>

      <input
        id={id}
        className={cx(
          ui.controls.input,
          error && 'border-red-500 focus:ring-red-500'
        )}
        aria-describedby={error ? errorId : undefined}  // asocia error al input
        aria-invalid={!!error}                           // anuncia campo inválido
        aria-required={required}
        {...inputProps}
      />

      {error && (
        <p id={errorId} role="alert" className="text-xs text-red-600">
          {error}
        </p>
      )}
    </div>
  )
}
```

### Navegación con skip link

```jsx
// src/components/layout/SkipLink.jsx
// Primer elemento del DOM — permite saltar la nav al contenido principal
export function SkipLink() {
  return (
    <a
      href="#main-content"
      className="sr-only focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:z-[9999]
                 focus:rounded focus:bg-white focus:px-4 focus:py-2 focus:text-sm focus:font-semibold
                 focus:shadow-lg focus:outline focus:outline-2"
    >
      Ir al contenido principal
    </a>
  )
}

// App.jsx
<SkipLink />
<Header />
<main id="main-content" tabIndex={-1}>
  {children}
</main>
```

---

## Contraste de color — WCAG AA

WCAG AA exige una relación de contraste mínima de **4.5:1** para texto normal y **3:1** para texto grande (≥18pt o ≥14pt bold). El design system cumple estos requisitos:

| Token | Color | Sobre fondo | Contraste | WCAG |
|-------|-------|-------------|-----------|------|
| `text-slate-900` | `#0f172a` | blanco `#fff` | 19.6:1 | AAA |
| `text-slate-700` | `#334155` | blanco `#fff` | 8.4:1 | AAA |
| `text-slate-600` | `#475569` | blanco `#fff` | 5.9:1 | AA |
| `text-slate-500` | `#64748b` | blanco `#fff` | 4.6:1 | AA |
| `text-white` | `#ffffff` | `bg-slate-900` | 19.6:1 | AAA |
| `text-white` | `#ffffff` | `bg-red-600` | 4.9:1 | AA |

Verificar con herramienta: `webaim.org/resources/contrastchecker/`

---

## Theming por tenant

Al cargar la app, la API del tenant devuelve el color primario y se aplica al `:root`:

```js
// src/auth/applyTenantTheme.js
export function applyTenantTheme(tenant) {
  const root = document.documentElement
  root.style.setProperty('--color-accent',   tenant.primaryColor ?? '#ff6100')
  root.style.setProperty('--color-accent-h', tenant.primaryColorHover ?? '#e55500')
  root.style.setProperty('--color-accent-soft', tenant.primaryColorSoft ?? '#fff0e8')
}
```

```jsx
// src/auth/SessionProvider.jsx
useEffect(() => {
  if (session?.tenant) {
    applyTenantTheme(session.tenant)
  }
}, [session])
```

---

## Relación con front-template

El front-template usa `@tailwindcss/vite` v4.1.x. El archivo `src/styles/designSystem.js` es la fuente única de verdad para todos los tokens de clases. Los colores del tenant se aplican vía CSS custom properties en el bootstrap de la sesión. Los componentes accesibles (Modal, FormField, SkipLink) están en `src/components/primitives/` (ver `05-componentes-primitivos.md`).

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| tokens del `ui` object para todos los componentes del proyecto | CSS inline con valores mágicos hardcodeados |
| CSS custom properties para colores dinámicos (tenant, dark mode) | clases de color estáticas de Tailwind para colores que cambian |
| `aria-label` cuando el control no tiene texto visible | `aria-label` en elementos con texto legible visible (redundante) |
| `role="alert"` para mensajes de error dinámicos | `role="alert"` en contenido estático que no cambia |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Design token | valor de diseño (color, espacio, tipografía) almacenado como variable reutilizable |
| CSS custom property | variable CSS nativa (`--nombre`) accesible con `var(--nombre)` en cualquier regla |
| ARIA | Accessible Rich Internet Applications — especificación W3C de atributos para accesibilidad |
| WCAG | Web Content Accessibility Guidelines — estándar de accesibilidad web del W3C |
| Contraste | relación entre la luminancia del texto y su fondo (WCAG AA exige 4.5:1 para texto normal) |
| `sr-only` | clase de Tailwind que oculta visualmente pero mantiene accesible para lectores de pantalla |
| `aria-label` | etiqueta de texto alternativa para controles sin texto visible |
| `aria-describedby` | asocia un elemento describiendo al control (útil para mensajes de error) |
| `aria-live` | región que anuncia cambios dinámicos al lector de pantalla sin que el usuario la enfoque |
| `role="dialog"` | semántica de modal — el lector anuncia que se abrió un diálogo al entrar |
| skip link | enlace oculto al inicio del DOM que permite saltar la navegación al contenido principal |
| theming | aplicar un conjunto de tokens de color/estilo diferente en función del contexto (tenant, dark mode) |

---

*Rogelio Arriaga Gonzalez*
