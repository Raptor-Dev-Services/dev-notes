# Roadmap — Frontend: React 19 + Vite + Tailwind v4

**Prerequisito:** HTML/CSS/JavaScript básico. TypeScript básico recomendado.
**Objetivo:** construir el front-template — SPA production-ready con React 19, Vite, Tailwind v4 y diseño accesible.
**Duración estimada:** 3-4 semanas.

---

## Fase 1 — Configuración del entorno (semana 1)

> Antes del primer componente, el entorno debe estar correcto.

- [ ] [Variables de Entorno Vite](../06-frontend/01-variables-entorno-vite.md) — .env, archivos por ambiente, tipos TypeScript, seguridad

**Al terminar esta fase puedes:** configurar la app para distintos entornos (dev, staging, prod) sin exponer secretos en el bundle.

---

## Fase 2 — React 19 en producción (semana 1-2)

> Los patrones que hacen que una app React escale bien en producción.

- [ ] [React en Producción](../06-frontend/02-react-produccion.md) — React Router, Axios interceptors, React Hook Form + Zod, TanStack Query, manejo de errores global

**Temas clave de este doc:**
- **React Router** — layouts anidados, rutas protegidas
- **Axios interceptors** — JWT refresh automático, 401 redirect
- **RHF + Zod** — formularios tipados con validación en el cliente
- **TanStack Query** — caché de datos del servidor, invalidación, stale-while-revalidate

**Al terminar esta fase puedes:** conectar la SPA al backend con autenticación JWT transparente y formularios validados.

---

## Fase 3 — Estilos: Tailwind v4 (semana 2)

> El sistema de estilos del front-template — utility-first con soporte multi-tenant.

- [ ] [Tailwind v4](../06-frontend/03-tailwind.md) — dark mode con CSS variables, tenant theming, plugins custom, animaciones

**Conceptos clave:**
- **CSS Variables + Tailwind** — cambiar el tema del tenant sin recargar
- **Dark mode** — `class` strategy con CSS variables
- **Custom plugins** — utilidades que no existen en Tailwind por defecto

**Al terminar esta fase puedes:** construir interfaces que cambian de tema según el tenant con dark mode incluido.

---

## Fase 4 — Design System (semana 2-3)

> La capa que da coherencia visual a toda la app.

- [ ] [Design System](../06-frontend/04-design-system.md) — semantic tokens, escala tipográfica, ARIA (Modal, FormField, skip links)
- [ ] [Componentes Primitivos](../06-frontend/05-componentes-primitivos.md) — Button, Input, Table — la base reutilizable

**Al terminar esta fase puedes:** construir cualquier pantalla usando los primitivos del design system sin CSS ad hoc.

**Principios de accesibilidad aplicados:**
- `aria-label`, `aria-describedby`, `aria-live` en los primitivos
- Skip link para navegación por teclado
- Focus visible en todos los componentes interactivos
- Roles ARIA correctos (dialog, alert, form)

---

## Fase 5 — Patrones de feature (semana 3)

> Cómo organizar una feature completa en el frontend.

- [ ] [Feature Hook](../06-frontend/06-feature-hook.md) — patrón `useExampleUser` — lógica separada de la vista

**El patrón:**
```
feature/
├── useExampleUser.ts      ← lógica: TanStack Query + mutaciones + estado local
├── ExampleUserForm.tsx    ← formulario: RHF + Zod, llama al hook
├── ExampleUserTable.tsx   ← presentación: datos del hook, sin lógica
└── index.ts               ← exporta componentes públicos de la feature
```

**Al terminar esta fase puedes:** organizar features complejas con separación clara entre lógica y presentación.

---

## Fase 6 — Docker del frontend (semana 3-4)

> Empaquetar la SPA para producción.

- [ ] [Proyecto Real Docker](../08-contenedores/04-proyecto-real.md) — ver sección del frontend: Dockerfile multi-stage + nginx.conf

**Al terminar esta fase puedes:** publicar el frontend como una imagen Docker con nginx optimizado para SPA.

---

## Stack de dependencias del front-template

| Categoría | Librería | Propósito |
|-----------|---------|-----------|
| Build | Vite 6 | Bundler ultra-rápido con HMR |
| Framework | React 19 | UI con Server Components y Concurrent Mode |
| Routing | React Router 7 | Rutas anidadas, layouts, lazy loading |
| Estilos | Tailwind v4 | Utility-first CSS con CSS variables |
| Forms | React Hook Form + Zod | Formularios tipados con validación |
| Data fetching | TanStack Query v5 | Cache del servidor, sync, optimistic updates |
| HTTP | Axios | Cliente HTTP con interceptors para JWT |
| State | Zustand | Estado global ligero (cuando TanStack no alcanza) |
| Testing | Vitest + Testing Library | Tests unitarios de componentes |

---

## Qué puedes construir al terminar

Una SPA completa con:
- Autenticación JWT con refresh automático
- Formularios validados con RHF + Zod
- Tabla paginada con TanStack Query
- Dark mode y theming por tenant
- Diseño accesible (ARIA, focus, skip links)
- Docker multi-stage listo para producción

---

## Siguiente paso

- [Roadmap DevOps](roadmap-devops.md) — desplegar el frontend en producción
- [Roadmap SaaS Multi-Tenant](roadmap-saas-multitenant.md) — conectar el frontend al backend SaaS

---

*Duración total estimada: 3-4 semanas — Nivel objetivo: frontend developer mid*
