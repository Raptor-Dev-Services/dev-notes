# React en producción con TypeScript

Curso de **Raptor Dev Services Academy**. Las clases como presentación, el quiz y el avance están en [academy.raptorcloud.dev/react-production/](https://academy.raptorcloud.dev/react-production/); aquí está todo el material en Markdown, y las lecturas son notas de este mismo repositorio. Los libros se citan por capítulo: cada quien consigue su copia.

**Prerrequisito:** JavaScript. Recomendable: la unidad [Frontend con React](../react-frontend/README.md) de Dev Notes.
**Objetivo:** construir un front de React con TypeScript listo para producción: arquitectura por feature, datos y formularios, seguridad en el navegador, rendimiento, accesibilidad, pruebas y despliegue.
**Formato:** 6 sesiones de 5 horas (30 h), con clase, quiz, práctica, tarea y glosario.
**Código:** un solo proyecto de ejemplo, la *Mesa de ayuda* (ejemplo ilustrativo), en [`referencia/mesa-de-ayuda`](referencia/mesa-de-ayuda/): React 19.3.1, TypeScript 6.0.3, Vite 8.3.8, React Router 8.5.1, TanStack Query 5.107.1, Zod 4.6.7, Vitest 5.1.0 y Playwright 1.65.0 (versiones a noviembre de 2026, de la que cada unidad cita su fuente). La sesión 2 parte de [`referencia/mesa-plana`](referencia/mesa-plana/), la misma aplicación organizada por tipo de archivo. Necesita Node 22.22 o superior.
**Para quién:** quien ya hace pantallas en React y quiere que aguanten usuarios reales.

---

## Sesión 1. TypeScript para React (5 h)

> Los tipos del contrato de la API son la primera prueba del front.

**Material:** [clase](s01-typescript-for-react/slides.md), [práctica](s01-typescript-for-react/practica.md), [tarea](s01-typescript-for-react/tarea.md), [glosario](s01-typescript-for-react/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** qué hace TypeScript y qué no (los tipos se borran), el esquema como única fuente del contrato, props, hooks y eventos tipados, uniones discriminadas, genéricos y `satisfies`.
- [ ] **Lectura:** [`06-frontend/01-variables-entorno-vite.md`](../../06-frontend/01-variables-entorno-vite.md). [`06-frontend/12-typescript-para-react.md`](../../06-frontend/12-typescript-para-react.md).
- [ ] **Práctica:** tipar un componente y un cliente de API con los tipos del contrato. Verlo fallar: cambiar un campo del contrato y que el compilador lo marque; cambiar el servidor y que el esquema lo marque.
- [ ] **Tarea:** quitar todos los `any` de un módulo, con el antes y el después medidos.
- **Libros:** *Learn React with TypeScript* (Rippon, 2025), caps. 1 a 3. *TypeScript 5 Design Patterns and Best Practices* (Despoudis, 2025), caps. 1 y 2.

**Al terminar puedes:** hacer que un cambio en la API rompa el build y no la pantalla del usuario.

---

## Sesión 2. Arquitectura por feature (5 h)

> Una regla de arquitectura que nadie hace cumplir es una sugerencia.

**Material:** [clase](s02-feature-architecture/slides.md), [práctica](s02-feature-architecture/practica.md), [tarea](s02-feature-architecture/tarea.md), [glosario](s02-feature-architecture/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** estructura por feature, API pública, capa de dominio, fronteras de importación y antipatrones comunes.
- [ ] **Lectura:** [`06-frontend/06-feature-hook.md`](../../06-frontend/06-feature-hook.md), [`07-capa-dominio-acl.md`](../../06-frontend/07-capa-dominio-acl.md) y [`10-fronteras-import.md`](../../06-frontend/10-fronteras-import.md).
- [ ] **Práctica:** reorganizar una app por feature y poner una regla de importación que falle en lint. Verlo fallar: cuatro violaciones, y una regla que calla por falta de resolutor.
- [ ] **Tarea:** el mapa de features de su proyecto, con la regla y el plan de adopción.
- **Libros:** *React Application Architecture for Production* (Alickovic, 2026), caps. 1 a 3. *React Anti-Patterns* (Qiu, 2023), caps. 3, 4 y 11.

**Al terminar puedes:** crecer una app sin que cada pantalla dependa de todas las demás.

---

## Sesión 3. Datos, estado y formularios (5 h)

> La mitad de los errores de un front es estado guardado en el lugar equivocado.

**Material:** [clase](s03-data-state-forms/slides.md), [práctica](s03-data-state-forms/practica.md), [tarea](s03-data-state-forms/tarea.md), [glosario](s03-data-state-forms/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** dónde vive cada estado, traer datos con caché, formularios con validación y errores del servidor.
- [ ] **Lectura:** [`06-frontend/08-estado-donde-vive.md`](../../06-frontend/08-estado-donde-vive.md).
- [ ] **Práctica:** un formulario que valida en el cliente, muestra los errores del servidor y no pierde lo escrito cuando falla la red.
- [ ] **Tarea:** los cuatro estados (cargando, vacío, error y con datos) en todas sus pantallas.
- **Libros:** *React Application Architecture for Production*, caps. 5 y 6. *Learn React with TypeScript*, caps. 8 a 10. *Learn React Hooks* (Bugl, 2025), caps. 5 a 8.

**Al terminar puedes:** decidir dónde vive cada dato y que ninguna pantalla se quede en blanco.

---

## Sesión 4. Autenticación y seguridad en el navegador (5 h)

> Un token en localStorage es de cualquier script que corra en la página.

**Material:** [clase](s04-auth-browser-security/slides.md), [práctica](s04-auth-browser-security/practica.md), [tarea](s04-auth-browser-security/tarea.md), [glosario](s04-auth-browser-security/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** sesión con cookie HttpOnly frente a token en el navegador, XSS, CSRF y Content Security Policy.
- [ ] **Lectura:** [`10-cloud/07-seguridad-web.md`](../../10-cloud/07-seguridad-web.md). [`06-frontend/13-seguridad-en-el-navegador.md`](../../06-frontend/13-seguridad-en-el-navegador.md).
- [ ] **Práctica:** robar un token de localStorage con un XSS sembrado, en un laboratorio propio y con autorización, y ver cómo la cookie HttpOnly y la CSP lo bloquean.
- [ ] **Tarea:** la CSP de su aplicación, sin `unsafe-inline`.
- **Libros:** *React Application Architecture for Production*, cap. 7. *Web Application Security*, 2.ª ed. (Hoffman, 2023), caps. 10, 11, 28 y 29.

**Al terminar puedes:** elegir dónde guardar la sesión y demostrar que la defensa funciona.

---

## Sesión 5. Rendimiento, accesibilidad e idiomas (5 h)

> Optimizar sin medir antes es adivinar con más trabajo.

**Material:** [clase](s05-performance-a11y-i18n/slides.md), [práctica](s05-performance-a11y-i18n/practica.md), [tarea](s05-performance-a11y-i18n/tarea.md), [glosario](s05-performance-a11y-i18n/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** medir con Profiler y Lighthouse, dividir el código, accesibilidad AA con teclado y lector de pantalla, e internacionalización.
- [ ] **Lectura:** [`06-frontend/02-react-produccion.md`](../../06-frontend/02-react-produccion.md).
- [ ] **Práctica:** medir, arreglar tres problemas y volver a medir. Recorrer la app solo con teclado.
- [ ] **Tarea:** el reporte antes y después, con números.
- **Libros:** *React Application Architecture for Production*, caps. 8 a 10.

**Al terminar puedes:** mejorar el rendimiento con datos y garantizar que la app se usa sin ratón.

---

## Sesión 6. Pruebas y producción (5 h)

> Un navegador con la versión vieja en caché hace que el arreglo exista solo en el servidor.

**Material:** [clase](s06-testing-production/slides.md), [práctica](s06-testing-production/practica.md), [tarea](s06-testing-production/tarea.md), [glosario](s06-testing-production/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** pruebas con Vitest y Testing Library, una prueba de punta a punta, build, versión en los assets y caché.
- [ ] **Lectura:** [`06-frontend/14-pruebas-front.md`](../../06-frontend/14-pruebas-front.md) y [`06-frontend/15-cache-y-versionado-de-assets.md`](../../06-frontend/15-cache-y-versionado-de-assets.md).
- [ ] **Práctica:** pruebas de una feature y una prueba E2E con Playwright. Verlo fallar: un despliegue sin versión en los assets y un navegador que sigue con el JavaScript viejo.
- [ ] **Tarea:** el pipeline del front con pruebas y despliegue en contenedor.
- **Libros:** *React Application Architecture for Production*, caps. 11 y 12. *React Anti-Patterns*, caps. 5 a 7. *Modern Full-Stack React Projects* (Bugl, 2026), cap. 9.
- **Caso real:** en la propia academia, Cloudflare guardaba el JavaScript viejo hasta 4 horas. Se resolvió con una huella del contenido en `?v=`.

**Al terminar puedes:** publicar un front que los usuarios reciben de verdad, probado antes de salir.

---

## Proyecto final

Un front completo contra una API real: arquitectura por feature, formularios con errores del servidor, CSP, accesibilidad AA medida, pruebas y despliegue en contenedor.

## Código de referencia

En [`referencia/`](referencia/) está el proyecto del instructor: [`mesa-de-ayuda`](referencia/mesa-de-ayuda/) con todo lo que se construye en las seis sesiones, y [`mesa-plana`](referencia/mesa-plana/), el punto de partida de la sesión 2. Para comprobarlo: `npm ci`, `npm run typecheck`, `npm run lint` y `npm test` dentro de la carpeta. Úsenlo para comparar después de intentarlo, no antes. Todo en él es un ejemplo ilustrativo: la persona, los tickets y las credenciales no existen y solo valen contra el servidor de laboratorio que viene en la carpeta.

## Notas nuevas de este curso

| Nota | Sesión |
|---|---|
| [`06-frontend/12-typescript-para-react.md`](../../06-frontend/12-typescript-para-react.md) | 1 |
| [`06-frontend/13-seguridad-en-el-navegador.md`](../../06-frontend/13-seguridad-en-el-navegador.md) | 4 |
| [`06-frontend/14-pruebas-front.md`](../../06-frontend/14-pruebas-front.md) | 6 |
| [`06-frontend/15-cache-y-versionado-de-assets.md`](../../06-frontend/15-cache-y-versionado-de-assets.md) | 6 |

## Libros base

| Libro | Autor | Año | Para qué |
|---|---|---|---|
| React Application Architecture for Production | Alan Alickovic y Anthony Alicea | 2026 | Columna del curso |
| Learn React with TypeScript | Carl Rippon | 2025 | TypeScript y datos |
| React Anti-Patterns | Juntao Qiu | 2023 | Lo que no hacer, y pruebas |
| Learn React Hooks | Daniel Bugl | 2025 | Hooks a fondo |
| TypeScript 5 Design Patterns and Best Practices | Theofanis Despoudis | 2025 | Patrones en TypeScript |
| Modern Full-Stack React Projects | Daniel Bugl | 2026 | E2E y despliegue |
| Web Application Security | Andrew Hoffman | 2023 | Seguridad en el navegador |

El curso usa Vite con SPA, que es lo que usan la academia y los proyectos del instructor. Los libros de 2025 y 2026 se van a Server Components y Next.js: se leen por los conceptos de arquitectura, estado, seguridad y pruebas, que no dependen de ello.

---

*Rogelio Arriaga Gonzalez*
