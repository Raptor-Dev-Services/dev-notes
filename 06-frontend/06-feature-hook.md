# 06 · Patrón feature hook — CRUD + filtros + paginación

## Problema que resuelve

Sin separación de responsabilidades, los componentes de página acumulan decenas de `useState`, lógica de fetch, filtrado, paginación y manejo de modales mezclados con el JSX. El patrón feature hook extrae toda la lógica de estado y efectos a un custom hook, dejando el componente de página como vista pura que solo consume lo que el hook expone.

## Estructura

```
src/
  components/
    example/
      users/
        useExampleUsers.js        ← toda la lógica
        Desktop/
          ExampleUsersTableDesktop.jsx   ← vista desktop
        Mobile/
          ExampleUsersCardsMobile.jsx    ← vista mobile
  pages/
    ExampleUsers.jsx              ← orquestador: usa el hook y renderiza vistas
  api/
    exampleUsersService.js        ← llamadas HTTP
```

## El hook (useExampleUsers)

Responsabilidades del hook:
- estado de datos: `rows`, `loading`, `error`
- filtrado en cliente: `filteredRows` (memo)
- paginación en cliente: `pagedRows`, `totalPages`, `pageNumber`
- formulario CRUD: `formOpen`, `form`, `saving`, `editingRow`
- modal de confirmación: `confirmModal`, `actionForm`
- notificaciones: `notification`
- acciones: `handleOpenCreate`, `handleOpenEdit`, `handleSave`, `handleDeactivate`, `handleReactivate`, `handleDelete`

```js
// src/components/example/users/useExampleUsers.js

const DEFAULT_FILTERS = { search: '', status: 'all' }
const EMPTY_FORM = { fullName: '', email: '', department: '', notes: '', isActive: true }

export default function useExampleUsers() {
  const [rows, setRows]     = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError]   = useState('')
  const [busyRowId, setBusyRowId] = useState(null)
  const [notification, setNotification] = useState(null)

  const [formOpen, setFormOpen]     = useState(false)
  const [editingRow, setEditingRow] = useState(null)
  const [form, setForm]             = useState(EMPTY_FORM)
  const [saving, setSaving]         = useState(false)

  const [confirmModal, setConfirmModal] = useState({
    open: false, title: '', message: '', variant: 'info',
    confirmText: 'Confirmar', requireComment: false, action: null,
  })
  const [actionForm, setActionForm] = useState({ comments: '' })

  const [filters, setFilters]   = useState(DEFAULT_FILTERS)
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize] = useState(50)

  const loadData = useCallback(async () => {
    setLoading(true); setError('')
    try {
      const data = await getExampleUsers()
      setRows(Array.isArray(data) ? data : [])
    } catch (err) {
      setRows([]); setError(extractApiErrorMessage(err))
    } finally { setLoading(false) }
  }, [])

  useEffect(() => { loadData() }, [loadData])

  // filtrado en cliente (memo)
  const filteredRows = useMemo(() => {
    const q = filters.search.trim().toLowerCase()
    return rows.filter((row) => {
      if (q && !`${row.fullName} ${row.email} ${row.department}`.toLowerCase().includes(q)) return false
      if (filters.status === 'active' && !row.isActive) return false
      if (filters.status === 'inactive' && row.isActive) return false
      return true
    })
  }, [rows, filters])

  const totalPages = useMemo(() => Math.max(1, Math.ceil(filteredRows.length / pageSize)), [filteredRows.length, pageSize])
  const pagedRows  = useMemo(() => filteredRows.slice((pageNumber - 1) * pageSize, pageNumber * pageSize), [filteredRows, pageNumber, pageSize])

  // acciones CRUD
  async function handleSave() {
    setSaving(true)
    try {
      if (editingRow) {
        await updateExampleUser(editingRow.userId, form)
        setNotification({ id: crypto.randomUUID(), type: 'success', message: 'Usuario actualizado.' })
      } else {
        await createExampleUser(form)
        setNotification({ id: crypto.randomUUID(), type: 'success', message: 'Usuario creado.' })
      }
      setFormOpen(false)
      await loadData()
    } catch (err) {
      setNotification({ id: crypto.randomUUID(), type: 'error', message: extractApiErrorMessage(err) })
    } finally { setSaving(false) }
  }

  function handleDelete(row) {
    setConfirmModal({
      open: true, title: 'Eliminar usuario',
      message: `¿Eliminar permanentemente a "${row.fullName}"?`,
      confirmText: 'Eliminar', variant: 'danger', requireComment: false,
      action: async () => {
        setBusyRowId(row.userId)
        try {
          await deleteExampleUser(row.userId)
          setNotification({ id: crypto.randomUUID(), type: 'success', message: `"${row.fullName}" eliminado.` })
          await loadData()
        } catch (err) {
          setNotification({ id: crypto.randomUUID(), type: 'error', message: extractApiErrorMessage(err) })
        } finally { setBusyRowId(null) }
      },
    })
  }

  return {
    rows, loading, error,
    filteredRows, pagedRows, totalPages,
    pageNumber, setPageNumber, pageSize,
    filters, setFilters,
    busyRowId, notification, setNotification,
    formOpen, setFormOpen, form, setForm, saving,
    handleOpenCreate, handleOpenEdit, handleSave,
    confirmModal, setConfirmModal, actionForm, setActionForm,
    handleDeactivate, handleReactivate, handleDelete,
    loadData, formatDate,
  }
}
```

## El componente de página (ExampleUsers.jsx)

La página es pura vista: importa el hook y distribuye lo necesario a las vistas Desktop/Mobile.

```jsx
// src/pages/ExampleUsers.jsx
import useExampleUsers from '../components/example/users/useExampleUsers'
import ExampleUsersTableDesktop from '../components/example/users/Desktop/ExampleUsersTableDesktop'
import ExampleUsersCardsMobile from '../components/example/users/Mobile/ExampleUsersCardsMobile'
import useMediaQuery from '../hooks/useMediaQuery'
import Modal from '../components/primitives/Modal'

export default function ExampleUsers() {
  const vm = useExampleUsers()
  const isDesktop = useMediaQuery('(min-width: 768px)')

  return (
    <div className={ui.layout.moduleShell}>
      <header>
        <h1 className={ui.typography.pageTitle}>Usuarios</h1>
        <button onClick={vm.handleOpenCreate} className={ui.controls.primaryButton}>
          Nuevo usuario
        </button>
      </header>

      {isDesktop
        ? <ExampleUsersTableDesktop vm={vm} />
        : <ExampleUsersCardsMobile vm={vm} />
      }

      {/* modal formulario */}
      <Modal open={vm.formOpen} onClose={() => vm.setFormOpen(false)} title={vm.editingRow ? 'Editar' : 'Nuevo'}>
        {/* formulario */}
      </Modal>
    </div>
  )
}
```

## useMediaQuery — vista desktop / mobile

```js
// src/hooks/useMediaQuery.js
export default function useMediaQuery(query) {
  const [matches, setMatches] = useState(() => getMatches(query))

  useEffect(() => {
    const mql = window.matchMedia(query)
    const handleChange = (e) => setMatches(e.matches)
    mql.addEventListener('change', handleChange)
    return () => mql.removeEventListener('change', handleChange)
  }, [query])

  return matches
}
```

```jsx
const isDesktop = useMediaQuery('(min-width: 768px)')

{isDesktop
  ? <ExampleUsersTableDesktop vm={vm} />
  : <ExampleUsersCardsMobile vm={vm} />
}
```

Renderiza vistas completamente distintas por breakpoint, no solo oculta con `hidden md:block`.

## Cliente HTTP (apiClient)

```js
// src/api/clients.js
export const apiClient = axios.create({ baseURL: API_BASE_URL })

// interceptor de auth: añade Bearer token a cada request
apiClient.interceptors.request.use((config) => {
  const token = getToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// interceptor de error: normaliza el mensaje
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const normalizedError = new Error(extractApiErrorMessage(error))
    normalizedError.status = error?.response?.status
    return Promise.reject(normalizedError)
  },
)
```

`resolveApiEnvelope()` desenvuelve respuestas del back-template que pueden venir como `{ isSuccess, data, message }` o como array directo:

```js
export function resolveApiEnvelope(payload, fallbackMessage = 'Request failed') {
  if (Array.isArray(payload)) return payload
  const isSuccess = payload?.isSuccess ?? payload?.IsSuccess
  if ('isSuccess' in payload && !isSuccess) throw new Error(payload.message ?? fallbackMessage)
  return payload?.data ?? payload?.Data ?? payload
}
```

## Relación con back-template

El `resolveApiEnvelope` consume exactamente el `Result<T>` que el back-template retorna desde sus handlers: `{ isSuccess: true, data: [...], message: "" }`. El campo `isSuccess` mapea al `Result.IsSuccess` del `Result Pattern` del dominio 04.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| módulos CRUD completos (lista + formulario + confirmaciones) | componentes simples de display sin estado |
| cuando la página tiene filtros, paginación y múltiples modales | cuando solo se hace un fetch sin acciones (usar `useEffect` + `useState` inline) |
| vistas distintas por breakpoint con lógica diferente | cuando solo se necesita cambiar layout CSS (usar breakpoints Tailwind) |


> Fuente: *Full Stack React, TypeScript, and Node* (David Choi) — Ch.6 React Hooks in Depth

---

*Rogelio Arriaga Gonzalez*
