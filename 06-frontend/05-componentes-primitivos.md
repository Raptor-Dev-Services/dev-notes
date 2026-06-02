# 05 · Componentes primitivos

## Problema que resuelve

Los proyectos acumulan código repetido de formularios y modales en cada feature. Extraer primitivos reutilizables (`FormField`, `Modal`, `Pagination`) reduce el código de cada módulo y garantiza consistencia visual en toda la app.

## FormField

Wrapper de campo de formulario: label, input slot y mensaje de error.

```jsx
// src/components/primitives/FormField.jsx
export default function FormField({ label, children, error, required }) {
  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label className="text-sm font-medium text-slate-700">
          {label}
          {required && <span className="ml-0.5 text-red-500">*</span>}
        </label>
      )}
      {children}
      {error && <p className="text-xs text-red-600">{error}</p>}
    </div>
  )
}
```

```jsx
// uso
<FormField label="Correo electrónico" required error={errors.email}>
  <input
    type="email"
    value={form.email}
    onChange={(e) => setForm({ ...form, email: e.target.value })}
    className={ui.controls.input}
  />
</FormField>
```

El `children` puede ser cualquier control: `<input>`, `<select>`, `<textarea>`, o un componente personalizado.

## Modal (Headless UI)

Modal accesible construido con `@headlessui/react`. Soporta tamaños variables y animación de entrada/salida.

```jsx
// src/components/primitives/Modal.jsx
import { Dialog, DialogPanel, DialogTitle, Transition, TransitionChild } from '@headlessui/react'
import { Fragment } from 'react'
import { XMarkIcon } from '@heroicons/react/24/outline'

const SIZE_CLASS = { sm: 'max-w-sm', md: 'max-w-md', lg: 'max-w-lg', xl: 'max-w-xl' }

export default function Modal({ open, onClose, title, children, size = 'md' }) {
  return (
    <Transition appear show={open} as={Fragment}>
      <Dialog as="div" className="relative z-[60]" onClose={onClose}>
        <TransitionChild
          as={Fragment}
          enter="ease-out duration-200" enterFrom="opacity-0" enterTo="opacity-100"
          leave="ease-in duration-150" leaveFrom="opacity-100" leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-black/45" />
        </TransitionChild>

        <div className="fixed inset-0 z-10 flex items-center justify-center p-4">
          <TransitionChild
            as={Fragment}
            enter="ease-out duration-200" enterFrom="opacity-0 scale-95" enterTo="opacity-100 scale-100"
            leave="ease-in duration-150" leaveFrom="opacity-100 scale-100" leaveTo="opacity-0 scale-95"
          >
            <DialogPanel className={`w-full ${SIZE_CLASS[size] ?? SIZE_CLASS.md} rounded-xl bg-white shadow-xl ring-1 ring-slate-900/10`}>
              <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
                {title && <DialogTitle className="text-sm font-semibold text-slate-900">{title}</DialogTitle>}
                <button type="button" onClick={onClose} className="ml-auto rounded-md p-1 text-slate-400 hover:bg-slate-100">
                  <XMarkIcon className="size-4" />
                </button>
              </div>
              <div className="p-4">{children}</div>
            </DialogPanel>
          </TransitionChild>
        </div>
      </Dialog>
    </Transition>
  )
}
```

```jsx
// uso
<Modal open={formOpen} onClose={() => setFormOpen(false)} title="Editar usuario" size="lg">
  <form onSubmit={handleSave}>
    <FormField label="Nombre" required>
      <input value={form.fullName} onChange={...} className={ui.controls.input} />
    </FormField>
    <div className="mt-4 flex justify-end gap-2">
      <button type="button" onClick={() => setFormOpen(false)} className={ui.controls.secondaryButton}>
        Cancelar
      </button>
      <button type="submit" className={ui.controls.primaryButton}>
        Guardar
      </button>
    </div>
  </form>
</Modal>
```

### Por qué Headless UI

- `Dialog` maneja focus trap, `Escape` para cerrar, y `aria-modal` automáticamente
- `Transition` / `TransitionChild` aplican animaciones CSS por estado (enter/leave) sin JavaScript de animación

## Instalación de dependencias

```bash
npm install @headlessui/react @heroicons/react
```

## AppNavbar — navegación principal

```jsx
// src/components/layout/AppNavbar.jsx
const NAV_ITEMS = [
  { label: 'Inicio', to: '/app' },
  { label: 'Usuarios', to: '/app/example/users' },
]

export default function AppNavbar() {
  const { pathname } = useLocation()
  return (
    <header className={ui.layout.stickyTopBar}>
      <div className="mx-auto flex h-14 max-w-screen-xl items-center gap-6 px-4 sm:px-6">
        <Link to="/app" className="text-sm font-black tracking-tight text-slate-900">
          App
        </Link>
        <nav className="flex items-center gap-1">
          {NAV_ITEMS.map((item) => {
            const active = item.to === '/app'
              ? pathname === '/app'
              : pathname.startsWith(item.to)
            return (
              <Link key={item.to} to={item.to}
                className={cx(ui.controls.navItem, active ? ui.controls.navItemActive : ui.controls.navItemIdle)}>
                {item.label}
              </Link>
            )
          })}
        </nav>
      </div>
    </header>
  )
}
```

`ui.layout.stickyTopBar` aplica `sticky top-0 backdrop-blur` para que la barra quede fija con efecto glassmorphism al hacer scroll.

## AppShell — layout de página

```jsx
// src/routes/AppRoutes.jsx
function AppShell({ children }) {
  const { pathname } = useLocation()
  const isWide = wideRoutes.some((r) => pathname.startsWith(r))
  return (
    <div className="flex min-h-screen flex-col bg-slate-50">
      <AppNavbar />
      <main className={isWide
        ? 'flex-1 p-3 sm:p-4 lg:p-5'
        : 'mx-auto w-full max-w-screen-xl flex-1 p-3 sm:p-4 lg:p-5'
      }>
        {children}
      </main>
    </div>
  )
}
```

`wideRoutes` es un array de prefijos de rutas que necesitan ancho completo (ej. dashboards, mapas). El resto tiene `max-w-screen-xl` centrado.

## Lazy loading de páginas

```jsx
const Home         = lazy(() => import('../pages/Home'))
const ExampleUsers = lazy(() => import('../pages/ExampleUsers'))

function renderLazy(element) {
  return <Suspense fallback={<Spinner />}>{element}</Suspense>
}

<Route index element={renderLazy(<Home />)} />
<Route path="example/users" element={renderLazy(<ExampleUsers />)} />
```

Cada página es un chunk separado. El `<Suspense>` muestra un spinner mientras carga el JS del módulo.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| `FormField` para cualquier campo de formulario | cuando el label y error van integrados en el control (ej. floating labels) |
| `Modal` para confirmaciones y formularios cortos | drawers/sidebars (usar Headless UI `Dialog` con panel lateral) |
| lazy routing para páginas pesadas | lazy en componentes pequeños que no justifican el overhead de chunk |


> Fuente: *Full Stack React, TypeScript, and Node* (David Choi) — Ch.5 React Component Patterns

---

## Glosario

| Término | Definición |
|---------|-----------|
| Componente primitivo | bloque de construcción reutilizable de bajo nivel (Button, FormField, Modal) sin lógica de negocio |
| Composición de componentes | patrón donde un componente complejo se construye combinando primitivos en lugar de heredar de ellos |
| Prop drilling | pasar props a través de múltiples niveles de componentes intermedios que no las usan; señal de que se necesita contexto o composición |
| `children` prop | mecanismo de React para pasar contenido arbitrario a un componente como si fuera un slot |
| Portal | mecanismo de React (`createPortal`) que renderiza un hijo en un nodo del DOM fuera de la jerarquía del componente padre |
| Suspense | componente de React que muestra un fallback mientras un hijo cargado de forma asíncrona no está listo |
| Lazy loading | carga diferida con `React.lazy` que convierte una página en un chunk separado cargado solo cuando se navega a ella |
| Headless UI | librería de componentes accesibles sin estilos predefinidos, diseñada para usarse con Tailwind CSS |
| FormField | componente primitivo que agrupa un label, un input y un mensaje de error con accesibilidad correcta |
| Toast | notificación temporal que aparece brevemente en la pantalla para confirmar una acción o mostrar un error |

---

*Rogelio Arriaga Gonzalez*
