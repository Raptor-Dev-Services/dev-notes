# 11 · React de producción

## 11.1 React Router

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>JSX</em></td>
</tr>
<tr>
<td><p>import { createBrowserRouter, RouterProvider } from 'react-router-dom';</p>
<p>import { lazy, Suspense } from 'react';</p>
<p>const Login = lazy(() =&gt; import('./pages/Login'));</p>
<p>const Dashboard = lazy(() =&gt; import('./pages/Dashboard'));</p>
<p>const router = createBrowserRouter([</p>
<p>{ path: '/login', element: &lt;Suspense fallback={&lt;PageLoader /&gt;}&gt;&lt;Login /&gt;&lt;/Suspense&gt; },</p>
<p>{ path: '/', element: &lt;ProtectedLayout /&gt;,</p>
<p>children: [</p>
<p>{ index: true, element: &lt;Dashboard /&gt; },</p>
<p>{ path: 'tasks', element: &lt;Tasks /&gt; },</p>
<p>],</p>
<p>},</p>
<p>]);</p>
<p>export function ProtectedLayout() {</p>
<p>const { isAuthenticated, loading } = useAuth();</p>
<p>if (loading) return &lt;PageLoader /&gt;;</p>
<p>if (!isAuthenticated) return &lt;Navigate to="/login" replace /&gt;;</p>
<p>return &lt;Outlet /&gt;;</p>
<p>}</p></td>
</tr>
</tbody>
</table>

## 11.2 Axios con interceptors para JWT y refresh

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>JavaScript</em></td>
</tr>
<tr>
<td><p>import axios from 'axios';</p>
<p>import { config } from '@/config/env';</p>
<p>import { getAccessToken, refreshAccessToken, clearTokens } from '@/auth/tokens';</p>
<p>export const api = axios.create({</p>
<p>baseURL: config.apiBaseUrl,</p>
<p>timeout: 30000,</p>
<p>});</p>
<p>api.interceptors.request.use((req) =&gt; {</p>
<p>const token = getAccessToken();</p>
<p>if (token) req.headers.Authorization = `Bearer ${token}`;</p>
<p>return req;</p>
<p>});</p>
<p>let refreshPromise = null;</p>
<p>api.interceptors.response.use(</p>
<p>(response) =&gt; response,</p>
<p>async (error) =&gt; {</p>
<p>const original = error.config;</p>
<p>if (error.response?.status === 401 &amp;&amp; !original._retry) {</p>
<p>original._retry = true;</p>
<p>refreshPromise = refreshPromise || refreshAccessToken();</p>
<p>try {</p>
<p>const newToken = await refreshPromise;</p>
<p>refreshPromise = null;</p>
<p>original.headers.Authorization = `Bearer ${newToken}`;</p>
<p>return api(original);</p>
<p>} catch (e) {</p>
<p>clearTokens();</p>
<p>window.location.href = '/login';</p>
<p>return Promise.reject(e);</p>
<p>}</p>
<p>}</p>
<p>return Promise.reject(error);</p>
<p>}</p>
<p>);</p></td>
</tr>
</tbody>
</table>

## 11.3 React Hook Form + Zod

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>TSX</em></td>
</tr>
<tr>
<td><p>import { useForm } from 'react-hook-form';</p>
<p>import { zodResolver } from '@hookform/resolvers/zod';</p>
<p>import { z } from 'zod';</p>
<p>const schema = z.object({</p>
<p>email: z.string().email('Email inválido'),</p>
<p>password: z.string().min(8, 'Mínimo 8 caracteres'),</p>
<p>});</p>
<p>export function LoginForm() {</p>
<p>const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm({</p>
<p>resolver: zodResolver(schema),</p>
<p>});</p>
<p>const onSubmit = async (data) =&gt; {</p>
<p>await api.post('/auth/login', data);</p>
<p>};</p>
<p>return (</p>
<p>&lt;form onSubmit={handleSubmit(onSubmit)}&gt;</p>
<p>&lt;input {...register('email')} type="email" /&gt;</p>
<p>{errors.email &amp;&amp; &lt;p&gt;{errors.email.message}&lt;/p&gt;}</p>
<p>&lt;input {...register('password')} type="password" /&gt;</p>
<p>{errors.password &amp;&amp; &lt;p&gt;{errors.password.message}&lt;/p&gt;}</p>
<p>&lt;button disabled={isSubmitting}&gt;Login&lt;/button&gt;</p>
<p>&lt;/form&gt;</p>
<p>);</p>
<p>}</p></td>
</tr>
</tbody>
</table>

## 11.4 date-fns

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>JavaScript</em></td>
</tr>
<tr>
<td><p>import { format, formatDistance, parseISO, addDays, isAfter } from 'date-fns';</p>
<p>import { es } from 'date-fns/locale';</p>
<p>format(new Date(), 'dd/MM/yyyy HH:mm', { locale: es });</p>
<p>// '08/05/2026 14:30'</p>
<p>formatDistance(parseISO(task.createdAt), new Date(), { locale: es, addSuffix: true });</p>
<p>// 'hace 3 horas'</p>
<p>const trialEndsAt = addDays(new Date(), 14);</p>
<p>if (isAfter(new Date(), parseISO(subscription.expiresAt))) { /* vencida */ }</p></td>
</tr>
</tbody>
</table>

## 11.5 i18n

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>JavaScript</em></td>
</tr>
<tr>
<td><p>import i18n from 'i18next';</p>
<p>import { initReactI18next } from 'react-i18next';</p>
<p>import esMX from './locales/es-MX/common.json';</p>
<p>import enUS from './locales/en-US/common.json';</p>
<p>i18n.use(initReactI18next).init({</p>
<p>resources: {</p>
<p>'es-MX': { common: esMX },</p>
<p>'en-US': { common: enUS },</p>
<p>},</p>
<p>lng: 'es-MX',</p>
<p>fallbackLng: 'en-US',</p>
<p>interpolation: { escapeValue: false },</p>
<p>});</p>
<p>import { useTranslation } from 'react-i18next';</p>
<p>export function Header() {</p>
<p>const { t, i18n } = useTranslation();</p>
<p>return (</p>
<p>&lt;header&gt;</p>
<p>&lt;h1&gt;{t('welcome', { name: user.name })}&lt;/h1&gt;</p>
<p>&lt;button onClick={() =&gt; i18n.changeLanguage('en-US')}&gt;EN&lt;/button&gt;</p>
<p>&lt;/header&gt;</p>
<p>);</p>
<p>}</p></td>
</tr>
</tbody>
</table>

## 11.6 Optimistic updates con TanStack Query

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>JavaScript</em></td>
</tr>
<tr>
<td><p>import { useMutation, useQueryClient } from '@tanstack/react-query';</p>
<p>function useToggleTask() {</p>
<p>const queryClient = useQueryClient();</p>
<p>return useMutation({</p>
<p>mutationFn: (taskId) =&gt; api.patch(`/tasks/${taskId}/toggle`),</p>
<p>onMutate: async (taskId) =&gt; {</p>
<p>await queryClient.cancelQueries({ queryKey: ['tasks'] });</p>
<p>const previous = queryClient.getQueryData(['tasks']);</p>
<p>queryClient.setQueryData(['tasks'], (old) =&gt;</p>
<p>old.map(t =&gt; t.id === taskId ? { ...t, completed: !t.completed } : t));</p>
<p>return { previous };</p>
<p>},</p>
<p>onError: (err, taskId, context) =&gt; {</p>
<p>queryClient.setQueryData(['tasks'], context.previous);</p>
<p>},</p>
<p>onSettled: () =&gt; {</p>
<p>queryClient.invalidateQueries({ queryKey: ['tasks'] });</p>
<p>},</p>
<p>});</p>
<p>}</p></td>
</tr>
</tbody>
</table>



---

*Rogelio Arriaga Gonzalez*
