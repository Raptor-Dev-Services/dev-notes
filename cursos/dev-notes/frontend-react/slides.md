<!-- _class: lead -->

# Frontend  -  React 19  -  Vite  -  Tailwind v4
## Design System  -  Feature Hooks  -  Formularios  -  HTTP

Dev Notes  -  Material extra

---

<!-- _class: lead -->

# Parte 1
## Arquitectura del front-template

---

## Estructura de carpetas del front-template

```
src/
  api/                    <- clientes HTTP y servicios por feature
    clients.js            <- apiClient (axios) + interceptors
    exampleUsersService.js
  components/
    layout/               <- AppNavbar, AppShell
    primitives/           <- FormField, Modal, Pagination
    example/
      users/
        useExampleUsers.js          <- toda la lógica del módulo
        Desktop/ExampleUsersTableDesktop.jsx
        Mobile/ExampleUsersCardsMobile.jsx
  config/
    env.js                <- acceso centralizado a import.meta.env
  hooks/                  <- hooks genéricos (useMediaQuery, etc.)
  pages/                  <- orquestadores de módulo (sin lógica)
  routes/                 <- AppRoutes, ProtectedLayout
  styles/
    designSystem.js       <- objeto ui + función cx()
```

---

## Convenciones de naming

| Elemento | Convención | Ejemplo |
|----------|-----------|---------|
| Componente React | PascalCase | `ConversationList.tsx` |
| Custom hook | camelCase con prefijo `use` | `useConversations.ts` |
| Servicio HTTP | camelCase + sufijo `Service` | `conversationsService.ts` |
| Archivo de estilos | camelCase | `designSystem.js` |
| Carpeta de feature | camelCase plural | `conversations/` |
| Tipo TypeScript | PascalCase | `type Contact = { id: number; name: string }` |

**Regla de oro:** el archivo del hook y los componentes que lo consumen viven en la misma carpeta de feature.

---

<!-- _class: lead -->

# Parte 2
## Vite + Variables de entorno

---

## Archivos .env reconocidos por Vite

| Archivo | Cuándo se carga |
|---------|----------------|
| `.env` | Siempre  -  variables compartidas por todos los modos |
| `.env.local` | Siempre, salvo en CI  -  overrides personales (NUNCA al repo) |
| `.env.development` | Solo en `npm run dev` |
| `.env.production` | Solo en `npm run build` |
| `.env.development.local` | Override local de development |
| `.env.production.local` | Override local de production |

**Precedencia (menor -> mayor):** `.env` -> `.env.{mode}` -> `.env.local` -> `.env.{mode}.local`

Los archivos `.local` **NUNCA** se comitean. Agregar `*.local` al `.gitignore`.

---

## Por qué `VITE_*` y no `process.env`

Vite **no usa Node.js en el navegador**. En tiempo de build, Vite reemplaza estáticamente las referencias a `import.meta.env.VITE_*` en el bundle final.

```
  process.env.REACT_APP_API_URL   <- Create React App (obsoleto)
  import.meta.env.VITE_API_BASE_URL  <- Vite estándar
```

**Solo variables con prefijo `VITE_` llegan al bundle.** Todo lo que pongas con ese prefijo es **público**  -  cualquier usuario puede verlo en el código fuente. Jamás pongas API keys secretas o passwords con prefijo `VITE_`.

---

## Centralizar el acceso: `src/config/env.ts`

```ts
// src/config/env.ts  -  NO usar import.meta.env disperso en componentes
export const config = {
  appName:    import.meta.env.VITE_APP_NAME ?? 'Sistema de mensajería',
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL,
  wsUrl:      import.meta.env.VITE_WS_URL,
  isDev:      import.meta.env.DEV,
  isProd:     import.meta.env.PROD,
  mode:       import.meta.env.MODE,
} as const

if (!config.apiBaseUrl) {
  throw new Error('VITE_API_BASE_URL es requerido')
}
```

Todos los componentes importan `config`  -  si el nombre de la variable cambia, se actualiza en un solo lugar.

`import.meta.env.DEV` y `import.meta.env.MODE` son **built-in** de Vite, no necesitan prefijo `VITE_`.

---

<!-- _class: lead -->

# Parte 3
## React 19  -  Novedades clave

---

## `useOptimistic`  -  actualizaciones optimistas

Muestra el estado final **antes** de que el servidor confirme. Si el servidor falla, revierte automáticamente.

```tsx
import { useOptimistic } from 'react'

function ConversationList({ conversations, onAssign }: Props) {
  const [optimisticList, addOptimistic] = useOptimistic(
    conversations,
    (current, assigned: { id: number; agentName: string }) =>
      current.map(c =>
        c.id === assigned.id ? { ...c, agent: assigned.agentName } : c
      )
  )

  async function handleAssign(id: number, agentName: string) {
    addOptimistic({ id, agentName })   // actualiza UI al instante
    await assignConversation(id, agentName)  // llama al servidor
  }
  // ...
}
```

Sin `useOptimistic`: el usuario espera ~300ms viendo la UI sin cambios. Con él: 0ms de latencia percibida.

---

## `use()`  -  leer promesas y contextos

`use()` es un hook que puede leer el valor de una `Promise` o de un `Context` dentro de cualquier componente, incluyendo sentencias condicionales.

```tsx
import { use, Suspense } from 'react'

// promesa creada fuera del componente
const contactsPromise = fetchContacts()

function ContactsTable() {
  // use() suspende el componente hasta que la promesa resuelve
  const contacts = use(contactsPromise)
  return <table>{contacts.map(c => <tr key={c.id}><td>{c.name}</td></tr>)}</table>
}

// en el padre
<Suspense fallback={<Spinner />}>
  <ContactsTable />
</Suspense>
```

A diferencia de `useEffect` + `useState`, no hay estado de loading manual. El componente simplemente se suspende.

---

## Mejoras en Suspense y Server Components (conceptual)

**Suspense en React 19**
- `<Suspense>` ahora funciona de forma más predecible con datos asíncronos
- Compatible con `use()` para fetch de datos sin `useEffect`
- `fallback` se muestra mientras cualquier hijo suspendido resuelve

**React Server Components (RSC)**
- Corren solo en el servidor  -  nunca envían su código JS al navegador
- Permiten acceso directo a DB, sistema de archivos, variables de entorno secretas
- **En nuestro stack (Vite + SPA):** no usamos RSC. El back-end es .NET 10 Web API
- RSC es relevante en Next.js 14+; conocerlo es parte de la cultura React moderna

---

<!-- _class: lead -->

# Parte 4
## Tailwind CSS v4

---

## Tailwind v4: CSS-first, sin `tailwind.config.js`

**v3** requería tres archivos de configuración:
```
tailwind.config.js   <- tema, contenido, plugins
postcss.config.js    <- integración PostCSS
src/index.css        <- @tailwind base; @tailwind components; @tailwind utilities;
```

**v4** con Vite: un plugin, una línea en CSS:

```ts
// vite.config.ts
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

---

## Breakpoints y clases de estado

Tailwind es **mobile-first**: sin prefijo = mobile base, los prefijos añaden estilos hacia arriba.

```tsx
// oculto en mobile, visible desde lg (1024px+)
<aside className="hidden lg:block w-64" />

// padding crece por breakpoint
<main className="p-3 sm:p-4 lg:p-5" />

// panel de conversación  -  dos columnas en desktop
<div className="flex flex-col lg:flex-row gap-4">
  <ConversationList className="w-full lg:w-80" />
  <ChatPanel className="flex-1" />
</div>
```

**Clases de estado:** `hover:`, `focus-visible:`, `disabled:`, `active:`

```tsx
<button className="bg-slate-900 hover:bg-slate-700 focus-visible:outline-2 disabled:opacity-50">
  Enviar mensaje
</button>
```

---

## Helper `cx()`  -  clases condicionales

```ts
// src/styles/designSystem.ts
export function cx(...classes: (string | false | null | undefined)[]) {
  return classes.filter(Boolean).join(' ')
}
```

```tsx
// nav item activo vs inactivo
<Link
  to="/conversations"
  className={cx(
    ui.controls.navItem,
    active ? ui.controls.navItemActive : ui.controls.navItemIdle
  )}
>
  Conversaciones
</Link>

// badge de estado de conversación
<span className={cx(
  'inline-flex rounded-full px-2 py-0.5 text-xs font-semibold',
  status === 'open'   && 'bg-emerald-100 text-emerald-700',
  status === 'closed' && 'bg-slate-200 text-slate-600',
  status === 'pending'&& 'bg-amber-100 text-amber-700',
)}>
  {status}
</span>
```

---

<!-- _class: lead -->

# Parte 5
## Design System + Componentes Primitivos

---

## El objeto `ui`  -  tokens de clases Tailwind

```ts
// src/styles/designSystem.ts
export const ui = {
  layout: {
    moduleShell:  'flex min-h-[calc(100vh-6.5rem)] rounded-xl border bg-slate-50',
    stickyTopBar: 'sticky top-0 z-50 border-b border-gray-200 bg-white/95 backdrop-blur',
  },
  surface: {
    panel:      'rounded-xl border border-slate-200 bg-white shadow-sm',
    tablePanel: 'overflow-hidden rounded-lg border border-slate-200 bg-white',
  },
  typography: {
    pageTitle:  'text-lg font-semibold text-slate-900 sm:text-xl',
    body:       'text-sm leading-6 text-slate-600',
  },
  controls: {
    primaryButton:   'inline-flex min-h-11 items-center gap-2 rounded-lg bg-slate-900 px-4 text-sm font-semibold text-white hover:bg-slate-700',
    secondaryButton: 'inline-flex min-h-11 items-center gap-2 rounded-lg border border-slate-300 bg-white px-4 text-sm font-semibold text-slate-700 hover:bg-slate-50',
    input:           'min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 text-sm',
  },
  badge: {
    success: 'inline-flex rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-700',
    neutral: 'inline-flex rounded-full bg-slate-200 px-2 py-0.5 text-xs font-semibold text-slate-600',
  },
}
```

---

## `FormField`  -  wrapper de campo de formulario

```tsx
// src/components/primitives/FormField.tsx
interface FormFieldProps {
  label?: string
  error?: string
  required?: boolean
  children: React.ReactNode
}

export default function FormField({ label, error, required, children }: FormFieldProps) {
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

---

## Uso de `FormField` en formulario de contacto

```tsx
// ContactForm.tsx
import FormField from '@/components/primitives/FormField'
import { ui } from '@/styles/designSystem'

function ContactForm({ errors, register }: Props) {
  return (
    <div className="flex flex-col gap-4">
      <FormField label="Nombre completo" required error={errors.name?.message}>
        <input
          {...register('name')}
          type="text"
          className={ui.controls.input}
          placeholder="Ej. María García"
        />
      </FormField>

      <FormField label="Teléfono (WhatsApp)" error={errors.phone?.message}>
        <input
          {...register('phone')}
          type="tel"
          className={ui.controls.input}
          placeholder="+52 55 1234 5678"
        />
      </FormField>
    </div>
  )
}
```

---

## `Modal` con Headless UI  -  accesible por defecto

```tsx
// Modal.tsx  -  estructura simplificada
import { Dialog, DialogPanel, Transition, TransitionChild } from '@headlessui/react'

interface ModalProps {
  open: boolean
  onClose: () => void
  title?: string
  size?: 'sm' | 'md' | 'lg' | 'xl'
  children: React.ReactNode
}

const SIZE: Record<string, string> = {
  sm: 'max-w-sm', md: 'max-w-md', lg: 'max-w-lg', xl: 'max-w-xl'
}
```

**Por qué Headless UI:**
- `Dialog` gestiona automáticamente: focus trap, tecla `Escape`, `aria-modal`
- `Transition` / `TransitionChild` aplican clases CSS en enter/leave  -  sin JS de animación
- Sin estilos propios  -  el styling lo pones tú con Tailwind

---

## `AppShell` y lazy routing

```tsx
// AppRoutes.tsx  -  lazy loading de páginas
const Conversations = lazy(() => import('../pages/Conversations'))
const Contacts      = lazy(() => import('../pages/Contacts'))
const Agents        = lazy(() => import('../pages/Agents'))

function renderLazy(el: React.ReactElement) {
  return <Suspense fallback={<Spinner />}>{el}</Suspense>
}

// cada página es un chunk JS separado
<Route path="conversations" element={renderLazy(<Conversations />)} />
<Route path="contacts"      element={renderLazy(<Contacts />)} />
<Route path="agents"        element={renderLazy(<Agents />)} />
```

**AppShell** detecta rutas "wide" (bandeja de entrada, chat) y aplica `flex-1 p-4` sin `max-w` para aprovechar el ancho completo de pantalla.

---

<!-- _class: lead -->

# Parte 6
## Feature Hook Pattern

---

## El problema sin feature hook

```tsx
//  ContactsPage.tsx sin feature hook  -  150+ líneas mezcladas
export default function ContactsPage() {
  const [contacts, setContacts] = useState<Contact[]>([])
  const [loading, setLoading]   = useState(false)
  const [error, setError]       = useState('')
  const [search, setSearch]     = useState('')
  const [page, setPage]         = useState(1)
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing]   = useState<Contact | null>(null)
  const [saving, setSaving]     = useState(false)
  // ... 8 funciones async, useMemo para filtrado, useEffect para fetch
  // ... todo mezclado con el JSX del render
}
```

**Consecuencias:** imposible testear la lógica sola, no se puede reutilizar entre Desktop/Mobile, el código review se vuelve una pesadilla.

---

## Estructura del feature hook

```
src/components/contacts/
  useContacts.ts                    <- TODA la lógica
  Desktop/
    ContactsTableDesktop.tsx        <- vista desktop (solo props)
  Mobile/
    ContactsCardsMobile.tsx         <- vista mobile (solo props)

src/pages/
  Contacts.tsx                      <- orquestador: hook + vistas
```

**Responsabilidades de `useContacts`:**
- Estado de datos: `rows`, `loading`, `error`
- Filtrado en cliente: `filteredRows` (useMemo)
- Paginación: `pagedRows`, `totalPages`, `pageNumber`
- Formulario CRUD: `formOpen`, `form`, `saving`, `editingRow`
- Acciones: `handleOpenCreate`, `handleOpenEdit`, `handleSave`, `handleDelete`

---

## `useContacts`  -  esqueleto

```ts
// src/components/contacts/useContacts.ts
const EMPTY_FORM = { name: '', phone: '', email: '', tags: [] as string[] }

export default function useContacts() {
  const [rows, setRows]       = useState<Contact[]>([])
  const [loading, setLoading] = useState(false)
  const [filters, setFilters] = useState({ search: '', tag: 'all' })
  const [pageNumber, setPageNumber] = useState(1)
  const [formOpen, setFormOpen]     = useState(false)
  const [editingRow, setEditingRow] = useState<Contact | null>(null)
  const [form, setForm]             = useState(EMPTY_FORM)
  const [saving, setSaving]         = useState(false)

  const loadData = useCallback(async () => {
    setLoading(true)
    try { setRows(await getContacts()) }
    catch (err) { /* setError */ }
    finally { setLoading(false) }
  }, [])

  useEffect(() => { loadData() }, [loadData])
  // filteredRows, pagedRows, handleSave, handleDelete ...
  return { rows, loading, filters, setFilters, pagedRows, /* ... */ }
}
```

---

## `Contacts.tsx`  -  orquestador (vista pura)

```tsx
// src/pages/Contacts.tsx
import useContacts from '@/components/contacts/useContacts'
import ContactsTableDesktop from '@/components/contacts/Desktop/ContactsTableDesktop'
import ContactsCardsMobile  from '@/components/contacts/Mobile/ContactsCardsMobile'
import useMediaQuery from '@/hooks/useMediaQuery'
import Modal from '@/components/primitives/Modal'
import { ui } from '@/styles/designSystem'

export default function Contacts() {
  const vm = useContacts()
  const isDesktop = useMediaQuery('(min-width: 768px)')

  return (
    <div className={ui.layout.moduleShell}>
      <header className="flex items-center justify-between p-4">
        <h1 className={ui.typography.pageTitle}>Contactos</h1>
        <button onClick={vm.handleOpenCreate} className={ui.controls.primaryButton}>
          Nuevo contacto
        </button>
      </header>

      {isDesktop
        ? <ContactsTableDesktop vm={vm} />
        : <ContactsCardsMobile  vm={vm} />}

      <Modal open={vm.formOpen} onClose={() => vm.setFormOpen(false)}
        title={vm.editingRow ? 'Editar contacto' : 'Nuevo contacto'}>
        {/* ContactForm aquí */}
      </Modal>
    </div>
  )
}
```

---

<!-- _class: lead -->

# Parte 7
## Formularios con React Hook Form + Zod

---

## Por qué React Hook Form + Zod

| Alternativa | Problema |
|-------------|---------|
| `useState` por campo | re-render en cada tecla, validación manual |
| Formik | re-renders costosos, API verbosa |
| **RHF + Zod** | inputs no controlados (performance), validación con tipos TypeScript |

```bash
npm install react-hook-form @hookform/resolvers zod
```

**Flujo:** Zod define el schema con tipos -> `zodResolver` conecta el schema con RHF -> RHF maneja registro, validación y submit.

---

## Schema Zod para formulario de contacto

```ts
// src/components/contacts/contactSchema.ts
import { z } from 'zod'

export const contactSchema = z.object({
  name:  z.string().min(2, 'El nombre requiere al menos 2 caracteres'),
  phone: z.string()
    .regex(/^\+?\d{10,15}$/, 'Formato inválido  -  ej. +525512345678')
    .optional()
    .or(z.literal('')),
  email: z.string().email('Email inválido').optional().or(z.literal('')),
  tags:  z.array(z.string()).default([]),
})

export type ContactFormValues = z.infer<typeof contactSchema>
```

La validación se ejecuta **al hacer submit** (modo `onSubmit` por defecto) o al salir del campo (`onBlur`). Nunca en cada tecla salvo que se configure `mode: 'onChange'`.

---

## Formulario conectado con RHF

```tsx
// ContactForm.tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { contactSchema, type ContactFormValues } from './contactSchema'
import FormField from '@/components/primitives/FormField'
import { ui } from '@/styles/designSystem'

interface Props { onSubmit: (data: ContactFormValues) => Promise<void> }

export default function ContactForm({ onSubmit }: Props) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<ContactFormValues>({ resolver: zodResolver(contactSchema) })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
      <FormField label="Nombre completo" required error={errors.name?.message}>
        <input {...register('name')} className={ui.controls.input} />
      </FormField>
      <FormField label="Teléfono" error={errors.phone?.message}>
        <input {...register('phone')} type="tel" className={ui.controls.input} />
      </FormField>
      <button type="submit" disabled={isSubmitting} className={ui.controls.primaryButton}>
        {isSubmitting ? 'Guardando...' : 'Guardar'}
      </button>
    </form>
  )
}
```

---

<!-- _class: lead -->

# Parte 8
## Axios Interceptors  -  Cliente HTTP

---

## Configuración del `apiClient`

```ts
// src/api/clients.ts
import axios from 'axios'
import { config } from '@/config/env'
import { getAccessToken, refreshAccessToken, clearTokens } from '@/auth/tokens'

export const apiClient = axios.create({
  baseURL: config.apiBaseUrl,
  timeout: 30_000,
})

// request interceptor  -  añade Bearer token a cada petición
apiClient.interceptors.request.use((req) => {
  const token = getAccessToken()
  if (token) req.headers.Authorization = `Bearer ${token}`
  return req
})
```

**`baseURL`** viene de `config.apiBaseUrl` (que lee `VITE_API_BASE_URL`), no de un string hardcodeado. Así development apunta a `localhost:5000` y production a `api.crmessenger.com` sin cambiar código.

---

## Response interceptor  -  manejo de 401

```ts
// continuación de src/api/clients.ts
let refreshPromise: Promise<string> | null = null

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config
    if (error.response?.status === 401 && !original._retry) {
      original._retry = true
      // singleton: múltiples requests 401 simultáneas comparten un solo refresh
      refreshPromise = refreshPromise ?? refreshAccessToken()
      try {
        const newToken = await refreshPromise
        refreshPromise = null
        original.headers.Authorization = `Bearer ${newToken}`
        return apiClient(original)           // reintenta la request original
      } catch {
        clearTokens()
        window.location.href = '/login'      // sesión expirada: redirige
      }
    }
    return Promise.reject(error)
  }
)
```

---

## `resolveApiEnvelope`  -  desenvuelve la respuesta del back

El back-template retorna `Result<T>`: `{ isSuccess: true, data: [...], message: "" }`.

```ts
// src/api/clients.ts
export function resolveApiEnvelope<T>(
  payload: unknown,
  fallbackMessage = 'Request failed'
): T {
  if (Array.isArray(payload)) return payload as T

  const p = payload as Record<string, unknown>
  const isSuccess = p?.isSuccess ?? p?.IsSuccess

  if ('isSuccess' in p && !isSuccess) {
    throw new Error((p.message as string) ?? fallbackMessage)
  }
  return (p?.data ?? p?.Data ?? payload) as T
}
```

```ts
// conversationsService.ts
export async function getConversations(): Promise<Conversation[]> {
  const { data } = await apiClient.get('/conversations')
  return resolveApiEnvelope<Conversation[]>(data)
}
```

---

## Resumen del stack frontend

| Capa | Tecnología | Rol |
|------|-----------|-----|
| Build | Vite 6 + `@tailwindcss/vite` | Dev server, HMR, bundle |
| UI Framework | React 19 | Componentes, hooks, Suspense |
| Estilos | Tailwind CSS v4 | Utilidades CSS, design tokens |
| Routing | React Router v7 | SPA routing, lazy loading |
| HTTP | Axios + interceptors | Auth, refresh, error norm. |
| Formularios | React Hook Form + Zod | Validación tipada, performance |
| Design System | `ui` object + `cx()` | Tokens de clases centralizados |
| Primitivos | FormField, Modal, AppShell | Componentes base reutilizables |
| Feature Hook | `useContacts`, `useConversations` | Lógica separada de la vista |

**El test de arquitectura:** si el componente de página tiene más de 20 líneas de lógica (states, effects, handlers), necesita un feature hook.
