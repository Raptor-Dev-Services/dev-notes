# 02 — React de Producción

Patrones y librerías del stack frontend para apps SaaS: routing protegido, cliente HTTP con refresh automático, formularios con validación, fechas, i18n y actualizaciones optimistas.

> Fuente: *Full Stack React, TypeScript, and Node* (David Choi) — Ch.7 Advanced React Patterns

---

## React Router — routing con lazy loading y layout protegido

```jsx
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { lazy, Suspense } from 'react';

// Lazy loading — cada página se carga solo cuando se navega a ella
const Login     = lazy(() => import('./pages/Login'));
const Dashboard = lazy(() => import('./pages/Dashboard'));
const Tasks     = lazy(() => import('./pages/Tasks'));

const router = createBrowserRouter([
  {
    path: '/login',
    element: (
      <Suspense fallback={<PageLoader />}>
        <Login />
      </Suspense>
    ),
  },
  {
    path: '/',
    element: <ProtectedLayout />,   // verifica autenticación antes de renderizar hijos
    children: [
      { index: true, element: <Dashboard /> },
      { path: 'tasks',   element: <Tasks /> },
      { path: 'settings', element: <Settings /> },
    ],
  },
]);

// Layout protegido — redirige al login si no hay sesión
export function ProtectedLayout() {
  const { isAuthenticated, loading } = useAuth();

  if (loading)          return <PageLoader />;
  if (!isAuthenticated) return <Navigate to="/login" replace />;

  return <Outlet />;   // renderiza el child route
}
```

---

## Axios — cliente HTTP con interceptors JWT y refresh automático

El interceptor de respuesta captura los 401 y hace refresh del token automáticamente, transparente para todos los componentes:

```js
// src/api/client.js
import axios from 'axios';
import { config } from '@/config/env';
import { getAccessToken, refreshAccessToken, clearTokens } from '@/auth/tokens';

export const api = axios.create({
  baseURL: config.apiBaseUrl,
  timeout: 30_000,
});

// Adjuntar token en cada request
api.interceptors.request.use((req) => {
  const token = getAccessToken();
  if (token) req.headers.Authorization = `Bearer ${token}`;
  return req;
});

// Manejo de 401 — refresh automático
let refreshPromise = null;   // singleton: múltiples requests concurrent solo hacen 1 refresh

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config;

    if (error.response?.status === 401 && !original._retry) {
      original._retry = true;

      // Si ya hay un refresh en progreso, esperar al mismo promise
      refreshPromise = refreshPromise || refreshAccessToken();

      try {
        const newToken = await refreshPromise;
        refreshPromise = null;
        original.headers.Authorization = `Bearer ${newToken}`;
        return api(original);   // reintentar el request original
      } catch (e) {
        refreshPromise = null;
        clearTokens();
        window.location.href = '/login';   // sesión expirada — redirigir
        return Promise.reject(e);
      }
    }

    return Promise.reject(error);
  }
);
```

---

## React Hook Form + Zod — formularios con validación tipada

```tsx
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const schema = z.object({
  email:    z.string().email('Email inválido'),
  password: z.string().min(8, 'Mínimo 8 caracteres'),
});

type LoginFormData = z.infer<typeof schema>;

export function LoginForm() {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<LoginFormData>({ resolver: zodResolver(schema) });

  const onSubmit = async (data: LoginFormData) => {
    try {
      await api.post('/auth/login', data);
    } catch (e) {
      // Error del servidor → mostrar en el campo o en el form
      setError('root', { message: 'Credenciales inválidas' });
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <input {...register('email')} type="email" placeholder="Email" />
      {errors.email && <p className="text-red-500">{errors.email.message}</p>}

      <input {...register('password')} type="password" placeholder="Contraseña" />
      {errors.password && <p className="text-red-500">{errors.password.message}</p>}

      {errors.root && <p className="text-red-500">{errors.root.message}</p>}

      <button disabled={isSubmitting}>
        {isSubmitting ? 'Entrando...' : 'Iniciar sesión'}
      </button>
    </form>
  );
}
```

---

## date-fns — fechas sin Moment.js

`date-fns` es modular: solo importar las funciones que se usan. `dayjs` es una alternativa más pequeña (2 KB vs 15 KB gzipped).

```js
import { format, formatDistance, parseISO, addDays, isAfter } from 'date-fns';
import { es } from 'date-fns/locale';

// Formatear fecha con locale español
format(new Date(), 'dd/MM/yyyy HH:mm', { locale: es });
// → '08/05/2026 14:30'

// Tiempo relativo ("hace 3 horas")
formatDistance(parseISO(task.createdAt), new Date(), { locale: es, addSuffix: true });
// → 'hace 3 horas'

// Calcular fecha de vencimiento de trial
const trialEndsAt = addDays(new Date(), 14);

// Verificar si una suscripción expiró
if (isAfter(new Date(), parseISO(subscription.expiresAt))) {
  // suscripción vencida
}
```

---

## i18n con react-i18next

```js
// src/i18n/index.js
import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import esMX from './locales/es-MX/common.json';
import enUS from './locales/en-US/common.json';

i18n.use(initReactI18next).init({
  resources: {
    'es-MX': { common: esMX },
    'en-US': { common: enUS },
  },
  lng:          'es-MX',
  fallbackLng:  'en-US',
  ns:           ['common'],
  defaultNS:    'common',
  interpolation: { escapeValue: false },
});

export default i18n;
```

```tsx
// Uso en componentes
import { useTranslation } from 'react-i18next';

export function Header() {
  const { t, i18n } = useTranslation();

  return (
    <header>
      <h1>{t('welcome', { name: user.name })}</h1>
      <button onClick={() => i18n.changeLanguage('en-US')}>EN</button>
      <button onClick={() => i18n.changeLanguage('es-MX')}>ES</button>
    </header>
  );
}
```

```json
// locales/es-MX/common.json
{
  "welcome": "Bienvenido, {{name}}",
  "tasks": "Tareas",
  "save": "Guardar"
}
```

---

## Optimistic updates con TanStack Query

El update optimista actualiza la UI inmediatamente sin esperar la respuesta del servidor — se revierte si el request falla:

```js
import { useMutation, useQueryClient } from '@tanstack/react-query';

function useToggleTask() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (taskId) => api.patch(`/tasks/${taskId}/toggle`),

    // 1. Actualizar UI inmediatamente (sin esperar la API)
    onMutate: async (taskId) => {
      await queryClient.cancelQueries({ queryKey: ['tasks'] });

      const previous = queryClient.getQueryData(['tasks']);

      queryClient.setQueryData(['tasks'], (old) =>
        old.map(t => t.id === taskId ? { ...t, completed: !t.completed } : t)
      );

      return { previous };   // guardar estado para rollback
    },

    // 2. Si falla, revertir al estado anterior
    onError: (err, taskId, context) => {
      queryClient.setQueryData(['tasks'], context.previous);
    },

    // 3. Siempre sincronizar con el servidor al terminar
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
    },
  });
}
```

---

## TanStack Query — patrones clave

```js
// Query con stale time y paginación
const { data, isLoading, error } = useQuery({
  queryKey:  ['tasks', { page, filters }],   // key incluye todos los parámetros
  queryFn:   () => api.get('/tasks', { params: { page, ...filters } }),
  staleTime: 60_000,   // 1 minuto — no refetch si el dato tiene < 1 min de antigüedad
  select:    (data) => data.data,   // extraer solo el data del response de Axios
});

// Prefetch en hover para UX más rápida
const onHover = () => {
  queryClient.prefetchQuery({
    queryKey: ['task', taskId],
    queryFn:  () => api.get(`/tasks/${taskId}`),
  });
};
```

---

*Rogelio Arriaga Gonzalez*
