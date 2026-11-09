# Glosario Técnico  -  Frontend  -  React 19

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### `@headlessui/react`
**Categoría:** Librería / React
**Definición:** Paquete de componentes UI completamente sin estilos propios que implementan interactividad y accesibilidad correctas por defecto (focus trap, ARIA, tecla Escape). Diseñado para usarse junto con Tailwind CSS, dejando el control visual completo al desarrollador.
**En el proyecto:** Se usa para el componente `Modal` del sistema de mensajería. El `Dialog` de Headless UI gestiona automáticamente el foco al abrir el modal de "Nuevo contacto" o "Editar conversación".

### `@hookform/resolvers`
**Categoría:** Librería / Formularios
**Definición:** Paquete adaptador que conecta esquemas de validación externos (Zod, Yup, Joi, etc.) con React Hook Form. Exporta la función `zodResolver` que traduce los errores de Zod al formato que RHF espera.
**En el proyecto:** Se instala junto con `react-hook-form` y `zod`. El `ContactForm` lo usa para conectar `contactSchema` con `useForm`.

### `@tailwindcss/vite`
**Categoría:** Plugin / Build
**Definición:** Plugin oficial de Tailwind CSS v4 para Vite. Reemplaza la configuración basada en PostCSS de v3. Al agregarlo a `vite.config.ts`, Tailwind se integra directamente en el pipeline de Vite sin necesidad de `tailwind.config.js` ni `postcss.config.js`.
**En el proyecto:** Declarado en `vite.config.ts` junto al plugin de React. El CSS del sistema de mensajería arranca solo con `@import "tailwindcss"` en `src/index.css`.

### `AppShell`
**Categoría:** Componente / React
**Definición:** Componente de layout que actúa como contenedor principal de la aplicación. Renderiza la navegación lateral o superior y el área de contenido principal. Detecta rutas "wide" (pantalla completa, sin padding máximo) para módulos como la bandeja de conversaciones o el chat.
**En el proyecto:** Envuelve todas las páginas del sistema de mensajería. Las rutas de chat y bandeja de entrada reciben `flex-1 p-4` sin `max-w` para aprovechar el ancho completo de pantalla.

### `aria-modal`
**Categoría:** Accesibilidad / HTML
**Definición:** Atributo ARIA que indica a los lectores de pantalla que el elemento es un modal activo. Cuando está presente, el lector de pantalla restringe la navegación al contenido dentro del modal e ignora el resto de la página.
**En el proyecto:** Headless UI lo aplica automáticamente en el `Dialog` del componente `Modal`, garantizando accesibilidad en los formularios de contactos y conversaciones sin configuración manual.

### `AppNavbar`
**Categoría:** Componente / React
**Definición:** Componente de navegación principal de la aplicación. Forma parte de la capa `layout/` del front-template y es responsable de renderizar los enlaces de primer nivel del sistema.
**En el proyecto:** Vive en `src/components/layout/AppNavbar` y se renderiza dentro del `AppShell` del sistema de mensajería.

### `AppRoutes`
**Categoría:** Componente / React Router
**Definición:** Componente centralizador de todas las rutas de la aplicación. Define el árbol de `<Route>` con lazy loading de páginas y envuelve cada página en `<Suspense>` para mostrar un spinner mientras el chunk JS carga.
**En el proyecto:** Importa `lazy(() => import('../pages/Conversations'))`, `Contacts` y `Agents`. Cada página del CRM es un chunk JS separado que solo descarga el navegador cuando el usuario navega a esa ruta.

### `as const`
**Categoría:** TypeScript
**Definición:** Aserción de TypeScript que congela el tipo inferido de un objeto o array al tipo literal más estrecho posible. Impide que TypeScript amplíe los tipos a `string`, `number`, etc.
**En el proyecto:** Se usa en `src/config/env.ts` con `} as const` para que el objeto `config` sea de solo lectura y los valores sean tipos literales precisos.

### `Authorization` (header)
**Categoría:** HTTP / Autenticación
**Definición:** Header HTTP estándar que transporta credenciales de autenticación. En APIs REST con JWT se usa el esquema Bearer: `Authorization: Bearer <token>`. El servidor lo valida en cada petición protegida.
**En el proyecto:** El request interceptor de `apiClient` agrega automáticamente `Authorization: Bearer <token>` a cada petición saliente del sistema de mensajería que requiera autenticación.

### Axios
**Categoría:** Librería / HTTP
**Definición:** Cliente HTTP basado en Promesas para el navegador y Node.js. Permite crear instancias configuradas con `baseURL`, `timeout` e interceptors. Es ampliamente preferido sobre `fetch` nativo por su soporte de interceptors, cancelación y manejo de errores tipado.
**En el proyecto:** Se instancia en `src/api/clients.ts` como `apiClient`. Todos los servicios del CRM (conversaciones, contactos, agentes) hacen sus llamadas HTTP a través de esta instancia.

---

## B

### `baseURL`
**Categoría:** Axios / HTTP
**Definición:** Propiedad de configuración de una instancia Axios que define el prefijo que se antepone a todas las URLs relativas de las peticiones. Permite que el mismo código funcione en diferentes ambientes cambiando solo la variable de entorno.
**En el proyecto:** Viene de `config.apiBaseUrl`, que lee `VITE_API_BASE_URL`. En desarrollo apunta a `localhost:5000`; en producción, a `api.crmessenger.com`.

### Bearer token
**Categoría:** Autenticación / HTTP
**Definición:** Esquema de autenticación HTTP en el que el cliente envía un token opaco (generalmente un JWT) en el header `Authorization`. El servidor verifica el token sin necesidad de consultar una sesión almacenada.
**En el proyecto:** El `apiClient` del sistema de mensajería lee el access token del almacenamiento local y lo adjunta como Bearer token a cada petición hacia la Web API de .NET 10.

### Breakpoint
**Categoría:** CSS / Tailwind
**Definición:** Valor de ancho de pantalla a partir del cual se aplican estilos diferentes. Tailwind usa breakpoints predefinidos: `sm` (640px), `md` (768px), `lg` (1024px), `xl` (1280px), `2xl` (1536px). Tailwind es mobile-first: sin prefijo = estilos base mobile.
**En el proyecto:** La bandeja de conversaciones usa `lg:flex-row` para pasar de columna única en mobile a dos columnas (lista + chat) en desktop.

### Bundle
**Categoría:** Build / Vite
**Definición:** Archivo JavaScript resultante del proceso de construcción (build) que empaqueta todos los módulos de la aplicación en uno o varios archivos optimizados para el navegador. Vite aplica tree-shaking, minificación y code splitting.
**En el proyecto:** `npm run build` genera el bundle del sistema de mensajería en la carpeta `dist/`. Con lazy loading, cada página se convierte en un chunk separado.

---

## C

### camelCase
**Categoría:** Convención de naming
**Definición:** Estilo de nomenclatura en que la primera palabra es minúscula y cada palabra subsiguiente comienza con mayúscula. Ejemplo: `useContacts`, `conversationsService`.
**En el proyecto:** Se aplica a custom hooks (`useContacts`), servicios HTTP (`conversationsService`), archivos de estilos (`designSystem.js`) y carpetas de feature (`conversations/`).

### `clearTokens`
**Categoría:** Autenticación / Frontend
**Definición:** Función utilitaria que elimina el access token y el refresh token del almacenamiento local del navegador. Se llama cuando el refresh token ha expirado o es inválido, forzando al usuario a volver a autenticarse.
**En el proyecto:** El response interceptor del `apiClient` la invoca cuando el intento de refresh falla, y luego redirige al usuario a `/login`.

### Chunk
**Categoría:** Build / Vite
**Definición:** Fragmento del bundle JavaScript que se genera de forma separada. Con code splitting y lazy loading, cada página o feature puede ser un chunk independiente que el navegador descarga solo cuando se necesita, reduciendo el tiempo de carga inicial.
**En el proyecto:** Las páginas `Conversations`, `Contacts` y `Agents` del CRM son chunks separados definidos con `lazy()` en `AppRoutes`.

### Code splitting
**Categoría:** Build / Performance
**Definición:** Técnica de empaquetado que divide el bundle JavaScript en múltiples chunks más pequeños. Solo el chunk necesario para la ruta actual se descarga al inicio; los demás se cargan bajo demanda.
**En el proyecto:** Habilitado automáticamente por Vite al usar `lazy()` con `import()` dinámico en `AppRoutes.tsx`.

### Componente controlado
**Categoría:** React
**Definición:** Input de formulario cuyo valor está vinculado a un estado React (`useState`). Cada cambio en el input dispara un re-render del componente. Causa problemas de performance en formularios grandes.
**En el proyecto:** React Hook Form evita componentes controlados usando refs nativas del DOM, lo que es especialmente relevante en formularios con muchos campos como los de importación de contactos.

### Componente no controlado
**Categoría:** React
**Definición:** Input de formulario cuyo valor es gestionado por el DOM nativo, no por React. Se accede a él con refs. React Hook Form usa este modelo por defecto para evitar re-renders innecesarios.
**En el proyecto:** Al usar `{...register('name')}` en el `ContactForm`, RHF registra el input sin convertirlo en un componente controlado, manteniendo la performance del formulario.

### `config` (objeto de entorno)
**Categoría:** Frontend / Configuración
**Definición:** Objeto centralizado exportado desde `src/config/env.ts` que expone todas las variables de entorno de la aplicación. Evita el uso disperso de `import.meta.env` en componentes individuales.
**En el proyecto:** Contiene `appName`, `apiBaseUrl`, `wsUrl`, `isDev`, `isProd` y `mode`. El `apiClient` lo importa para configurar `baseURL`. Si se renombra una variable de entorno, solo se actualiza en este archivo.

### Custom hook
**Categoría:** React
**Definición:** Función JavaScript cuyo nombre comienza con `use` y que puede llamar otros hooks de React. Permite extraer lógica de estado y efectos de los componentes hacia funciones reutilizables y testeables independientemente.
**En el proyecto:** El patrón central del front-template. Ejemplos: `useContacts`, `useConversations`, `useMediaQuery`. Cada módulo del CRM encapsula toda su lógica en un hook de feature.

### `cx()`
**Categoría:** Utilidad / Tailwind
**Definición:** Función utilitaria que concatena clases CSS filtrando valores falsy (`false`, `null`, `undefined`). Permite construir cadenas de clases condicionalmente sin concatenaciones manuales que producen espacios o strings vacíos.
**En el proyecto:** Definida en `src/styles/designSystem.ts`. Se usa para aplicar estilos condicionales en badges de estado de conversación (`open`, `closed`, `pending`) y en nav items activos/inactivos.

---

## D

### Design system
**Categoría:** Frontend / UI
**Definición:** Conjunto de componentes, tokens de estilo y convenciones reutilizables que garantizan consistencia visual en toda la aplicación. En el front-template se implementa como el objeto `ui` con tokens de clases Tailwind y componentes primitivos.
**En el proyecto:** El archivo `src/styles/designSystem.ts` centraliza los tokens visuales del sistema de mensajería: colores, tipografía, botones, badges. Cualquier cambio de marca se propaga actualizando este archivo.

### Design tokens
**Categoría:** Frontend / Diseño
**Definición:** Variables que representan decisiones de diseño (color, espaciado, tipografía, sombras). En el front-template, los tokens son cadenas de clases Tailwind agrupadas en el objeto `ui`.
**En el proyecto:** `ui.controls.primaryButton`, `ui.surface.panel`, `ui.typography.pageTitle` son tokens que todos los componentes del CRM usan en lugar de escribir clases Tailwind repetidamente.

### `Dialog`
**Categoría:** Headless UI / React
**Definición:** Componente de Headless UI que implementa el patrón de diálogo modal accesible. Gestiona automáticamente focus trap, cierre con tecla Escape y el atributo `aria-modal`.
**En el proyecto:** Es la base del componente `Modal` en `src/components/primitives/Modal.tsx`. Se usa para los formularios de creación y edición de contactos y conversaciones.

### `DialogPanel`
**Categoría:** Headless UI / React
**Definición:** Componente hijo de `Dialog` que representa el panel visible del modal. Es el contenedor que recibe los estilos de posicionamiento y fondo del diálogo.
**En el proyecto:** Dentro del `Modal` primitivo del CRM, `DialogPanel` recibe las clases Tailwind de tamaño (`max-w-sm` a `max-w-xl`) según la prop `size`.

### `.env` / `.env.*`
**Categoría:** Vite / Configuración
**Definición:** Archivos de texto que definen variables de entorno en formato `CLAVE=valor`. Vite reconoce varios: `.env` (todas las ejecuciones), `.env.development` (solo dev), `.env.production` (solo build), `.env.local` (personal, no se commitea).
**En el proyecto:** El sistema de mensajería usa `.env.development` para apuntar a `localhost:5000` y `.env.production` para la URL de la API en AWS. Los archivos `.local` están en `.gitignore`.

### `DEV` (variable built-in)
**Categoría:** Vite
**Definición:** Variable booleana built-in de Vite (`import.meta.env.DEV`) que vale `true` cuando la aplicación se ejecuta en modo desarrollo con `npm run dev`. No requiere prefijo `VITE_`.
**En el proyecto:** Expuesta como `config.isDev` en `src/config/env.ts`. Permite condicionar logs de diagnóstico que no deben aparecer en producción.

---

## E

### `editingRow`
**Categoría:** Estado / Feature Hook
**Definición:** Estado del feature hook que almacena el registro actualmente en edición. Vale `null` cuando el formulario está en modo creación, o contiene el objeto completo cuando está en modo edición.
**En el proyecto:** En `useContacts`, determina el título del `Modal`: "Nuevo contacto" cuando es `null`, "Editar contacto" cuando contiene un `Contact`.

### `.env.local`
**Categoría:** Vite / Configuración
**Definición:** Archivo de variables de entorno para overrides locales del desarrollador. Vite lo carga en todos los modos excepto CI, pero nunca debe commitearse al repositorio porque contiene datos personales de cada desarrollador.
**En el proyecto:** Cada miembro del equipo tiene su propio `.env.local` con su URL local de la API. El `.gitignore` del front-template incluye `*.local`.

---

## F

### `fallback`
**Categoría:** React / Suspense
**Definición:** Prop de `<Suspense>` que recibe el elemento JSX que se muestra mientras los hijos están suspendidos (esperando datos o código). Reemplaza el contenido principal hasta que la promesa o el chunk JS resuelvan.
**En el proyecto:** En `AppRoutes.tsx` se usa `<Suspense fallback={<Spinner />}>` para mostrar un spinner mientras Vite descarga el chunk de `Conversations`, `Contacts` o `Agents`.

### Feature hook
**Categoría:** Patrón / React
**Definición:** Custom hook que encapsula toda la lógica de un módulo o feature: estado de datos, filtrado, paginación, estado de formulario y acciones CRUD. Los componentes de vista solo reciben el objeto retornado por el hook como props.
**En el proyecto:** Es el patrón central del front-template. `useContacts` y `useConversations` son los feature hooks de sus respectivos módulos del CRM. La página orquestadora llama al hook y pasa el resultado (`vm`) a las vistas Desktop y Mobile.

### `filteredRows`
**Categoría:** Estado / Feature Hook
**Definición:** Valor derivado (calculado con `useMemo`) que representa la lista de registros filtrados según el estado de búsqueda y filtros activos. No es un estado independiente, se recalcula automáticamente cuando cambian los datos o los filtros.
**En el proyecto:** En `useContacts`, filtra los contactos por texto de búsqueda y etiqueta seleccionada antes de paginar. Evita llamadas adicionales a la API para filtros en cliente.

### Focus trap
**Categoría:** Accesibilidad / UX
**Definición:** Comportamiento que restringe el foco del teclado al contenido de un modal o dialog mientras está abierto. Garantiza que Tab y Shift+Tab ciclen únicamente entre los elementos interactivos del modal, sin saltar al contenido de fondo.
**En el proyecto:** Headless UI aplica focus trap automáticamente en el `Modal` del CRM. El usuario no puede acceder accidentalmente a la bandeja de conversaciones mientras edita un contacto.

### `formOpen`
**Categoría:** Estado / Feature Hook
**Definición:** Estado booleano del feature hook que controla la visibilidad del formulario modal. Se establece en `true` al llamar `handleOpenCreate` o `handleOpenEdit`, y en `false` al cerrar el modal.
**En el proyecto:** Controla el `Modal` de contactos y conversaciones en el CRM. El componente orquestador lo pasa directamente: `<Modal open={vm.formOpen} onClose={() => vm.setFormOpen(false)}>`.

### `formState`
**Categoría:** React Hook Form
**Definición:** Objeto retornado por `useForm` que contiene el estado actual del formulario: `errors` (errores de validación por campo), `isSubmitting` (true durante el submit async), `isDirty`, `isValid`, entre otros.
**En el proyecto:** En `ContactForm.tsx` se desestructura `{ errors, isSubmitting }` de `formState`. `errors.name?.message` muestra el mensaje de error de Zod en el `FormField`.

### `FormField`
**Categoría:** Componente primitivo / React
**Definición:** Componente wrapper para campos de formulario que agrupa visualmente el label, el input y el mensaje de error. Acepta `label`, `error`, `required` y `children`. Elimina la repetición de estructura HTML en cada campo.
**En el proyecto:** Definido en `src/components/primitives/FormField.tsx`. Se usa en `ContactForm` para todos los campos (nombre, teléfono, email). El asterisco rojo se agrega automáticamente con la prop `required`.

---

## G

### `getAccessToken`
**Categoría:** Autenticación / Frontend
**Definición:** Función utilitaria que recupera el JWT de acceso almacenado en el navegador (localStorage o sessionStorage). El request interceptor la llama antes de cada petición para adjuntar el token al header Authorization.
**En el proyecto:** Importada en `src/api/clients.ts` desde `@/auth/tokens`. Si el token existe, el interceptor lo agrega; si no, la petición se envía sin autenticación.

### `.gitignore`
**Categoría:** Git / Configuración
**Definición:** Archivo de texto en la raíz del repositorio que lista patrones de archivos y carpetas que Git debe ignorar. Los archivos ignorados nunca se incluyen en commits ni se sincronizan con el repositorio remoto.
**En el proyecto:** El front-template incluye `*.local` en `.gitignore` para que ningún miembro del equipo suba accidentalmente sus overrides personales de variables de entorno.

---

## H

### `handleSubmit`
**Categoría:** React Hook Form
**Definición:** Función de orden superior de RHF que envuelve el callback de submit del formulario. Ejecuta la validación del schema antes de llamar al callback; si hay errores, los muestra en `formState.errors` sin llamar al callback.
**En el proyecto:** En `ContactForm.tsx`: `<form onSubmit={handleSubmit(onSubmit)}>`. Si la validación Zod falla (nombre vacío, teléfono con formato incorrecto), `onSubmit` nunca se ejecuta.

### `handleOpenCreate`
**Categoría:** Acción / Feature Hook
**Definición:** Función del feature hook que abre el formulario modal en modo creación. Resetea el formulario a valores vacíos, limpia `editingRow` y establece `formOpen` en `true`.
**En el proyecto:** El botón "Nuevo contacto" en `Contacts.tsx` llama `vm.handleOpenCreate()`. El `Modal` renderiza el título "Nuevo contacto".

### `handleOpenEdit`
**Categoría:** Acción / Feature Hook
**Definición:** Función del feature hook que abre el formulario modal en modo edición. Recibe el registro a editar, lo carga en el estado `form`, lo guarda en `editingRow` y abre el modal.
**En el proyecto:** El botón de editar de cada fila/tarjeta en `ContactsTableDesktop` o `ContactsCardsMobile` llama `vm.handleOpenEdit(contact)`.

### `handleSave`
**Categoría:** Acción / Feature Hook
**Definición:** Función async del feature hook que maneja el submit del formulario. Determina si se trata de una creación o actualización basándose en `editingRow`, llama al servicio correspondiente, recarga los datos y cierra el modal.
**En el proyecto:** En `useContacts`, llama a `createContact` o `updateContact` según el caso, y luego ejecuta `loadData()` para refrescar la tabla del CRM.

### Headless UI
**Categoría:** Librería / UI / Accesibilidad
**Definición:** Colección de componentes UI completamente sin estilos visuales que implementan comportamientos complejos y accesibles (modals, dropdowns, comboboxes, transitions). El diseño visual es responsabilidad total del desarrollador con Tailwind u otro sistema de estilos.
**En el proyecto:** Se usa para el componente `Modal`. Su ventaja en el CRM es que el equipo puede personalizar completamente la apariencia sin luchar contra estilos predefinidos.

### HMR (Hot Module Replacement)
**Categoría:** Vite / Dev Server
**Definición:** Mecanismo del servidor de desarrollo de Vite que reemplaza módulos JavaScript modificados en el navegador sin recargar la página completa. Mantiene el estado de la aplicación mientras se edita código.
**En el proyecto:** Durante el desarrollo del CRM en `npm run dev`, los cambios en componentes React se reflejan instantáneamente en el navegador sin perder el estado actual (conversación abierta, filtros aplicados, etc.).

### Hook
**Categoría:** React
**Definición:** Función especial de React que permite a componentes de función usar estado, efectos y otras capacidades de React. Los hooks solo pueden llamarse en el nivel superior de componentes o custom hooks, nunca dentro de condiciones o bucles.
**En el proyecto:** El front-template se construye íntegramente con hooks: `useState`, `useEffect`, `useCallback`, `useMemo`, `useOptimistic`, `use()`, y custom hooks de feature y utilidad.

---

## I

### `import.meta.env`
**Categoría:** Vite / Variables de entorno
**Definición:** Objeto global que Vite inyecta en el código fuente durante el build y el dev server, reemplazando estáticamente las referencias a variables de entorno. Solo las variables con prefijo `VITE_` están disponibles en el bundle del navegador.
**En el proyecto:** Accedido únicamente en `src/config/env.ts`. Los componentes del CRM no usan `import.meta.env` directamente, sino el objeto `config`.

### `import()` dinámico
**Categoría:** JavaScript / Code Splitting
**Definición:** Sintaxis de ES Modules que carga un módulo de forma asíncrona bajo demanda, en lugar de incluirlo en el bundle principal. Devuelve una Promise que resuelve al módulo. Es la base del code splitting con `lazy()`.
**En el proyecto:** `lazy(() => import('../pages/Conversations'))`  -  Vite genera un chunk separado para la página de conversaciones que solo descarga cuando el usuario navega a esa ruta.

### Interceptor
**Categoría:** Axios / HTTP
**Definición:** Función registrada en Axios que se ejecuta automáticamente antes de cada request (request interceptor) o después de cada response (response interceptor). Permite transformar datos, agregar headers o manejar errores de forma centralizada.
**En el proyecto:** El CRM usa dos interceptors: uno de request para agregar el Bearer token, y uno de response para manejar errores 401 y ejecutar el refresh automático del token.

### `isSubmitting`
**Categoría:** React Hook Form
**Definición:** Booleano de `formState` que vale `true` mientras el callback de submit async está en ejecución. Permite deshabilitar el botón de submit durante la operación para evitar doble envío.
**En el proyecto:** En `ContactForm.tsx`: `disabled={isSubmitting}` deshabilita el botón "Guardar" mientras la llamada a la API del CRM está en curso.

---

## J

### JSX
**Categoría:** React / JavaScript
**Definición:** Extensión de sintaxis de JavaScript que permite escribir markup similar a HTML dentro de código JavaScript. Babel o SWC lo transpilan a llamadas `React.createElement`. Los archivos JSX/TSX son los que contienen componentes React.
**En el proyecto:** Todos los componentes del sistema de mensajería usan TSX (JSX con TypeScript). El front-template usa `.tsx` para componentes y `.ts` para lógica pura (hooks, servicios).

### JWT (JSON Web Token)
**Categoría:** Autenticación / Seguridad
**Definición:** Estándar de token compacto y autofirmado que codifica claims en formato JSON. Tiene tres partes: header, payload y firma. El backend lo genera al autenticar y el frontend lo almacena para incluirlo en peticiones subsiguientes.
**En el proyecto:** El back-end .NET 10 del CRM genera JWTs HS256. El frontend los almacena localmente, los agrega como Bearer token, y los renueva automáticamente via refresh token cuando expiran.

---

## L

### Lazy loading
**Categoría:** React / Performance
**Definición:** Técnica que retrasa la carga de un componente o recurso hasta que realmente se necesita. Con `React.lazy()`, un componente se importa de forma dinámica y su código JS solo se descarga cuando se renderiza por primera vez.
**En el proyecto:** Las páginas del CRM (`Conversations`, `Contacts`, `Agents`) usan lazy loading. El bundle inicial es significativamente más pequeño, reduciendo el tiempo hasta la primera interacción.

### `loadData`
**Categoría:** Función / Feature Hook
**Definición:** Función async del feature hook envuelta con `useCallback` que obtiene los datos del servidor y actualiza el estado local. Se llama en `useEffect` al montar el componente y después de operaciones CRUD para refrescar la vista.
**En el proyecto:** En `useContacts`, ejecuta `setRows(await getContacts())`. Al estar en `useCallback` con `[]` de dependencias, no se recrea en cada render y es segura como dependencia de `useEffect`.

---

## M

### `media query`
**Categoría:** CSS
**Definición:** Regla CSS que aplica estilos condicionalmente según características del dispositivo, principalmente el ancho de la ventana (`min-width`, `max-width`). Tailwind genera media queries automáticamente para sus prefijos de breakpoint.
**En el proyecto:** La función `useMediaQuery('(min-width: 768px)')` en la página `Contacts.tsx` determina si renderizar `ContactsTableDesktop` o `ContactsCardsMobile`.

### `MODE`
**Categoría:** Vite
**Definición:** Variable built-in de Vite (`import.meta.env.MODE`) que contiene el modo de ejecución actual como string: `"development"`, `"production"` u otro modo personalizado. No requiere prefijo `VITE_`.
**En el proyecto:** Expuesta como `config.mode` en `src/config/env.ts`. Útil para logs condicionales o comportamientos específicos por ambiente.

### Mobile-first
**Categoría:** CSS / Diseño Responsivo
**Definición:** Estrategia de diseño en la que los estilos base se definen para pantallas pequeñas (móvil) y los estilos para pantallas más grandes se agregan con media queries. Es la filosofía de Tailwind CSS: las clases sin prefijo son el estilo mobile.
**En el proyecto:** El sistema de mensajería está construido mobile-first. La vista base es de columna única; en `lg:` se activa el layout de dos columnas para la bandeja de conversaciones.

### `Modal`
**Categoría:** Componente primitivo / React
**Definición:** Componente reutilizable que muestra contenido en una capa superpuesta sobre la página principal. Bloquea la interacción con el fondo, gestiona el foco y se cierra con Escape o haciendo clic fuera.
**En el proyecto:** Definido en `src/components/primitives/Modal.tsx` usando Headless UI. Acepta props `open`, `onClose`, `title` y `size`. Se usa para formularios de creación y edición en todo el CRM.

---

## N

### Naming convention
**Categoría:** Desarrollo / Convenciones
**Definición:** Reglas que estandarizan cómo se nombran archivos, componentes, funciones y variables. Una convención consistente reduce la carga cognitiva al navegar el código y hace el code review más predecible.
**En el proyecto:** El front-template define: PascalCase para componentes, `use` + camelCase para hooks, camelCase + `Service` para servicios HTTP, plural camelCase para carpetas de feature.

---

## O

### `onBlur`
**Categoría:** React Hook Form
**Definición:** Modo de validación de RHF en el que la validación del campo se dispara cuando el input pierde el foco, en lugar de en cada tecla (`onChange`) o al hacer submit (`onSubmit`). Equilibra feedback rápido con performance.
**En el proyecto:** Se puede configurar en `useForm({ mode: 'onBlur' })` para formularios del CRM donde se quiere dar feedback al salir de cada campo sin validar en cada tecla.

### Orquestador (página)
**Categoría:** Patrón / React
**Definición:** Componente de página que actúa como coordinador: llama al feature hook para obtener el estado y las acciones, y los distribuye a las vistas Desktop y Mobile. No contiene lógica de negocio propia, solo composición.
**En el proyecto:** `src/pages/Contacts.tsx` es el orquestador del módulo de contactos. Llama a `useContacts()`, llama a `useMediaQuery()` y renderiza la vista apropiada con `vm` como props.

---

## P

### `pagedRows`
**Categoría:** Estado / Feature Hook
**Definición:** Subconjunto de `filteredRows` correspondiente a la página actual. Se calcula con `useMemo` aplicando slice según `pageNumber` y el tamaño de página configurado. Es lo que se muestra en la tabla o tarjetas.
**En el proyecto:** En `useContacts`, `pagedRows` alimenta tanto `ContactsTableDesktop` como `ContactsCardsMobile`. La paginación es en cliente para los módulos de datos pequeños del CRM.

### PascalCase
**Categoría:** Convención de naming
**Definición:** Estilo de nomenclatura donde cada palabra comienza con mayúscula, sin separadores. Convención estándar para componentes React y tipos TypeScript.
**En el proyecto:** Todos los componentes del CRM usan PascalCase: `ConversationList`, `ContactForm`, `AppShell`, `FormField`. Los tipos TypeScript también: `type Contact = { id: number; name: string }`.

### Pipeline Vite
**Categoría:** Build / Vite
**Definición:** Cadena de transformaciones que Vite aplica al código fuente: resolución de imports, transpilación TypeScript/JSX, reemplazo de variables de entorno, aplicación de plugins, minificación y generación del bundle final.
**En el proyecto:** El pipeline incluye `@vitejs/plugin-react-swc` (transpilación con SWC) y `@tailwindcss/vite` (generación del CSS de Tailwind). Ambos se declaran en `vite.config.ts`.

### PostCSS
**Categoría:** CSS / Build
**Definición:** Herramienta de transformación de CSS que aplica plugins para autoprefixing, minificación y otras transformaciones. Tailwind v3 lo requería obligatoriamente; Tailwind v4 con el plugin de Vite lo hace innecesario.
**En el proyecto:** En el front-template con Tailwind v4, `postcss.config.js` ya no existe. El plugin `@tailwindcss/vite` maneja la integración directamente.

### `PROD` (variable built-in)
**Categoría:** Vite
**Definición:** Variable booleana built-in de Vite (`import.meta.env.PROD`) que vale `true` cuando el bundle fue generado con `npm run build` y se ejecuta en modo producción. No requiere prefijo `VITE_`.
**En el proyecto:** Expuesta como `config.isProd` en `src/config/env.ts`. Permite deshabilitar herramientas de diagnóstico o activar analytics solo en producción.

### `ProtectedLayout`
**Categoría:** Componente / React Router
**Definición:** Componente de layout de React Router que verifica la autenticación del usuario antes de renderizar las rutas hijas. Si el usuario no está autenticado, redirige a `/login`.
**En el proyecto:** Vive en `src/routes/ProtectedLayout.tsx` del front-template. Todas las rutas del sistema de mensajería (bandeja, contactos, agentes, bots) están anidadas dentro de `ProtectedLayout`.

---

## R

### React 19
**Categoría:** Framework / Frontend
**Definición:** Versión mayor de React lanzada en 2024 que introduce `useOptimistic`, el hook `use()`, mejoras en Suspense y soporte estable de React Server Components. Mantiene compatibilidad con el modelo de hooks de versiones anteriores.
**En el proyecto:** Framework base del sistema de mensajería. El front-template usa React 19 con Vite y TypeScript. Se aprovechan `useOptimistic` para la asignación de conversaciones y `use()` para fetch declarativo de datos.

### React Hook Form (RHF)
**Categoría:** Librería / Formularios
**Definición:** Librería de formularios para React que usa inputs no controlados (refs nativas del DOM) para minimizar re-renders. Provee `useForm`, `register`, `handleSubmit` y `formState`. Se integra con resolvers de validación como Zod.
**En el proyecto:** Usado en todos los formularios del CRM: `ContactForm`, formulario de agentes, formulario de configuración de canales. Elimina el problema de re-renders por campo que tendría un enfoque con `useState`.

### React Router v7
**Categoría:** Librería / Routing
**Definición:** Librería de routing para aplicaciones React SPA. Maneja la navegación del lado del cliente actualizando la URL sin recargar la página. v7 introduce mejoras en el sistema de loaders, acciones y data routers.
**En el proyecto:** Define todas las rutas del CRM en `AppRoutes.tsx`. Gestiona las rutas `/conversations`, `/contacts`, `/agents` con lazy loading y la ruta `/login` fuera del `ProtectedLayout`.

### React Server Components (RSC)
**Categoría:** React / Arquitectura
**Definición:** Modelo de componentes que corren exclusivamente en el servidor y nunca envían su código JavaScript al navegador. Permiten acceso directo a bases de datos y variables secretas. Son nativos de Next.js 14+.
**En el proyecto:** No se usan en el sistema de mensajería porque el stack es Vite + SPA con back-end .NET 10. El equipo los conoce como cultura React moderna, relevante si en el futuro se adopta Next.js.

### `refreshAccessToken`
**Categoría:** Autenticación / Frontend
**Definición:** Función async que llama al endpoint `/auth/refresh` del back-end con el refresh token actual para obtener un nuevo access token. Si tiene éxito, persiste el nuevo token y lo retorna.
**En el proyecto:** El response interceptor del `apiClient` la llama cuando recibe un 401. Se protege con un singleton (`refreshPromise`) para que múltiples peticiones simultáneas que expiran compartan un solo refresh.

### Refresh token
**Categoría:** Autenticación / Seguridad
**Definición:** Token de larga duración que se usa exclusivamente para obtener nuevos access tokens cuando estos expiran. Nunca se envía en peticiones normales de la API, solo al endpoint de refresh.
**En el proyecto:** El back-end .NET 10 del CRM emite access tokens (15 min) y refresh tokens (7 días). El interceptor de Axios maneja el ciclo de renovación de forma transparente al usuario.

### `register`
**Categoría:** React Hook Form
**Definición:** Función de RHF que conecta un input del DOM con el sistema de seguimiento del formulario. Al usar spread (`{...register('fieldName')}`), agrega los atributos `name`, `ref`, `onChange` y `onBlur` al input.
**En el proyecto:** Cada `<input>` del `ContactForm` usa `{...register('name')}`, `{...register('phone')}`, etc. RHF sabe qué campo es cuál por el nombre registrado.

### `resolveApiEnvelope`
**Categoría:** Utilidad / API
**Definición:** Función que normaliza la respuesta del back-end .NET (que sigue el Result Pattern con `isSuccess`, `data`, `message`) al tipo de dato esperado. Lanza un error si `isSuccess` es `false`, con el mensaje del servidor.
**En el proyecto:** Todos los servicios del CRM la usan después de la llamada Axios: `return resolveApiEnvelope<Conversation[]>(data)`. Centraliza el manejo del envelope de respuesta del back-template.

### Result Pattern
**Categoría:** Arquitectura / Backend
**Definición:** Patrón de diseño del back-end que envuelve las respuestas de la API en un objeto estándar `{ isSuccess: bool, data: T, message: string }` en lugar de lanzar excepciones HTTP directamente. Hace explícito el resultado en el contrato de la API.
**En el proyecto:** El back-template .NET 10 retorna `Result<T>` en todos los endpoints. El frontend lo desenvuelve con `resolveApiEnvelope` para obtener el dato tipado o el mensaje de error.

### `rows`
**Categoría:** Estado / Feature Hook
**Definición:** Array de registros obtenidos del servidor y almacenados en el estado del feature hook. Representa la fuente de datos completa antes de aplicar filtros o paginación.
**En el proyecto:** En `useContacts`, `rows` contiene todos los contactos del workspace. `filteredRows` y `pagedRows` se derivan de él con `useMemo` sin llamadas adicionales a la API.

---

## S

### `saving`
**Categoría:** Estado / Feature Hook
**Definición:** Booleano del feature hook que indica si una operación de guardado está en curso. Se establece en `true` antes de llamar al servicio y en `false` en el bloque `finally`. Permite deshabilitar controles durante la operación.
**En el proyecto:** En `useContacts`, mientras `saving` es `true`, el botón de submit del formulario de contacto se deshabilita y muestra "Guardando...".

### `singleton`
**Categoría:** Patrón de diseño / JavaScript
**Definición:** Patrón que garantiza que solo existe una instancia de algo. En el contexto del interceptor de Axios, se usa para asegurar que múltiples peticiones que expiran simultáneamente compartan un único refresh en lugar de generar N llamadas de refresh paralelas.
**En el proyecto:** `refreshPromise` en `clients.ts` implementa el singleton: `refreshPromise = refreshPromise ?? refreshAccessToken()`. Si ya hay un refresh en curso, las demás peticiones esperan al mismo.

### SPA (Single Page Application)
**Categoría:** Arquitectura / Frontend
**Definición:** Aplicación web que carga una sola página HTML y actualiza el contenido dinámicamente usando JavaScript, sin recargas completas de página. El routing es manejado en el cliente por librerías como React Router.
**En el proyecto:** El sistema de mensajería es una SPA construida con React 19 + Vite. El servidor de .NET 10 provee la API; el cliente React maneja toda la navegación entre módulos (conversaciones, contactos, agentes).

### `Spinner`
**Categoría:** Componente / UI
**Definición:** Componente visual que muestra un indicador de carga animado (generalmente un círculo giratorio). Se usa como `fallback` de `<Suspense>` y en estados de carga de datos.
**En el proyecto:** Se usa como fallback en `AppRoutes.tsx` mientras los chunks de página cargados con `lazy()` descargan. También aparece en los estados `loading` de los feature hooks.

### `Suspense`
**Categoría:** React
**Definición:** Componente de React que permite mostrar un estado de carga mientras sus hijos están "suspendidos" (esperando un chunk de código o la resolución de una promesa con `use()`). Reemplaza patrones manuales de `isLoading`.
**En el proyecto:** Envuelve cada página lazy en `AppRoutes.tsx`. Con React 19 y `use()`, también puede usarse en `ContactsTable` para mostrar un skeleton mientras los datos del servidor cargan.

### SWC
**Categoría:** Build / Compilador
**Definición:** Compilador/transpilador de JavaScript y TypeScript escrito en Rust. Es significativamente más rápido que Babel para transpilar JSX y TypeScript. El plugin `@vitejs/plugin-react-swc` lo usa en lugar de Babel.
**En el proyecto:** El front-template usa `@vitejs/plugin-react-swc` en `vite.config.ts`. El build del sistema de mensajería es más rápido gracias a SWC sin sacrificar compatibilidad.

---

## T

### Tailwind CSS v4
**Categoría:** Framework CSS
**Definición:** Versión mayor de Tailwind CSS que adopta un enfoque CSS-first: se configura directamente en CSS con `@import "tailwindcss"` sin necesidad de `tailwind.config.js`. Integra con Vite mediante un plugin nativo.
**En el proyecto:** Sistema de estilos del sistema de mensajería. Todas las clases utilitarias, breakpoints, estados hover/focus y el objeto `ui` del design system están construidos sobre Tailwind v4.

### Tree-shaking
**Categoría:** Build / Vite
**Definición:** Proceso de eliminación de código muerto del bundle final. Vite analiza las importaciones y excluye del bundle el código que nunca se usa. Requiere que el código use módulos ES (import/export) en lugar de CommonJS.
**En el proyecto:** Al importar solo lo necesario de Headless UI, Zod, RHF y Axios, Vite elimina el código de esas librerías que no se usa, reduciendo el tamaño del bundle del CRM.

### `TransitionChild`
**Categoría:** Headless UI / React
**Definición:** Componente de Headless UI que aplica clases CSS en las fases de entrada y salida de una animación. Se usa dentro de `Transition` para animar elementos individuales con diferentes timings.
**En el proyecto:** Se usa dentro del `Modal` primitivo del CRM para animar el panel del dialog al abrirse y cerrarse con clases Tailwind de transición.

### TypeScript
**Categoría:** Lenguaje / Frontend
**Definición:** Superset tipado de JavaScript que agrega tipos estáticos, interfaces, enums y genéricos. Detecta errores en tiempo de compilación y provee autocompletado en el editor. Vite lo transpila nativamente sin configuración adicional.
**En el proyecto:** Todos los archivos del front-template usan TypeScript (`.ts` y `.tsx`). Los tipos `Contact`, `Conversation`, `Agent` son tipos TypeScript que garantizan consistencia entre los servicios HTTP y los componentes.

---

## U

### `ui` (objeto design system)
**Categoría:** Design System / Frontend
**Definición:** Objeto JavaScript exportado desde `designSystem.ts` que agrupa los tokens de clases Tailwind en categorías: `layout`, `surface`, `typography`, `controls`, `badge`. Cada propiedad es una cadena de clases Tailwind predefinidas.
**En el proyecto:** `ui.controls.primaryButton` aplica el estilo del botón principal en toda la app. `ui.badge.success` aplica el badge verde de conversación activa. Un solo cambio en `designSystem.ts` actualiza el estilo en todo el CRM.

### `use()` (hook de React 19)
**Categoría:** React 19
**Definición:** Nuevo hook primitivo de React 19 que puede leer el valor de una `Promise` o de un `Context`. A diferencia de otros hooks, puede usarse dentro de sentencias condicionales. Cuando recibe una Promise no resuelta, suspende el componente.
**En el proyecto:** Permite fetch declarativo de datos en `ContactsTable` sin `useEffect` + `useState` manual. El componente simplemente se suspende hasta que los datos están listos, y `<Suspense>` muestra el spinner.

### `useCallback`
**Categoría:** React
**Definición:** Hook que memoriza una función entre renders. Solo recrea la función cuando sus dependencias cambian. Es esencial para funciones que se pasan a `useEffect` como dependencias o a componentes hijo memorizados.
**En el proyecto:** `loadData` en `useContacts` está envuelta en `useCallback([])` para que `useEffect` no entre en un bucle infinito al usar `loadData` como dependencia del efecto de carga inicial.

### `useEffect`
**Categoría:** React
**Definición:** Hook que ejecuta efectos secundarios (llamadas a API, suscripciones, timers) después de que React renderiza el componente. El array de dependencias controla cuándo se re-ejecuta el efecto.
**En el proyecto:** En los feature hooks del CRM, `useEffect(() => { loadData() }, [loadData])` dispara la carga inicial de datos cuando el componente monta por primera vez.

### `useForm`
**Categoría:** React Hook Form
**Definición:** Hook principal de RHF que inicializa el sistema de gestión del formulario. Retorna `register`, `handleSubmit`, `formState`, `reset`, `setValue` y más. Acepta configuración como `resolver`, `mode` y `defaultValues`.
**En el proyecto:** En `ContactForm.tsx`: `useForm<ContactFormValues>({ resolver: zodResolver(contactSchema) })` conecta el formulario con la validación Zod y obtiene las utilidades de RHF.

### `useMediaQuery`
**Categoría:** Custom Hook / React
**Definición:** Hook genérico que evalúa una media query CSS y retorna un booleano que se actualiza automáticamente cuando el tamaño de ventana cambia. Permite renderizar componentes diferentes según el tipo de dispositivo.
**En el proyecto:** Vive en `src/hooks/useMediaQuery.ts`. La página `Contacts.tsx` lo usa con `'(min-width: 768px)'` para elegir entre `ContactsTableDesktop` y `ContactsCardsMobile`.

### `useMemo`
**Categoría:** React
**Definición:** Hook que memoriza el resultado de un cálculo costoso entre renders. Solo recalcula cuando sus dependencias cambian. Se usa para derivar datos de estado sin repetir la computación en cada render.
**En el proyecto:** En `useContacts`, `filteredRows` y `pagedRows` se calculan con `useMemo`. El filtrado de contactos por texto y etiqueta no se recalcula en cada render, solo cuando cambian `rows` o `filters`.

### `useOptimistic`
**Categoría:** React 19
**Definición:** Hook de React 19 que permite aplicar una actualización temporal al estado de la UI antes de que el servidor confirme la operación. Si el servidor retorna un error, el estado revierte automáticamente.
**En el proyecto:** Se usa en `ConversationList` para asignar un agente a una conversación. El agente aparece asignado al instante (0ms de latencia percibida) mientras la petición HTTP está en curso.

---

## V

### `VITE_*` (prefijo de variable de entorno)
**Categoría:** Vite / Variables de entorno
**Definición:** Prefijo obligatorio para que una variable de entorno sea incluida en el bundle de Vite y sea accesible en el navegador vía `import.meta.env.VITE_NOMBRE`. Variables sin este prefijo son privadas de Node.js/build y no llegan al cliente.
**En el proyecto:** `VITE_API_BASE_URL`, `VITE_WS_URL`, `VITE_APP_NAME` son las variables del sistema de mensajería. Nunca se pone una API key privada o password con este prefijo porque cualquier usuario puede verla en el código fuente.

### `vite.config.ts`
**Categoría:** Vite / Configuración
**Definición:** Archivo de configuración principal de Vite. Define los plugins, alias de imports, configuración del servidor de desarrollo, opciones de build y más. Es el equivalente de `webpack.config.js` para proyectos Vite.
**En el proyecto:** Declara `@vitejs/plugin-react-swc` y `@tailwindcss/vite`. También define el alias `@/` -> `src/` para importaciones absolutas en los componentes del CRM.

### Vertical Slice
**Categoría:** Arquitectura
**Definición:** Organización del código por feature completa (frontend + backend) en lugar de por capa técnica. Cada feature contiene todo lo que necesita: UI, lógica, acceso a datos.
**En el proyecto:** El front-template aplica Vertical Slice en el frontend: `src/components/contacts/` contiene el hook, los componentes Desktop y Mobile. Todo lo relacionado a contactos está junto, no disperso por capas.

### `vm` (view model)
**Categoría:** Patrón / React
**Definición:** Convención de naming para el objeto retornado por un feature hook cuando se pasa como prop a las vistas. Agrupa estado y acciones bajo una sola variable, haciendo explícito que la vista recibe un "modelo de vista".
**En el proyecto:** En `Contacts.tsx`: `const vm = useContacts()`. Luego: `<ContactsTableDesktop vm={vm} />` y `<ContactsCardsMobile vm={vm} />`. Las vistas solo necesitan desestructurar las props que usan de `vm`.

---

## W

### White-labeling
**Categoría:** SaaS / Frontend
**Definición:** Capacidad de personalizar la apariencia (marca, colores, logo) de una aplicación para diferentes clientes sin modificar el código fuente. Se implementa con variables CSS dinámicas o tokens de diseño configurables por tenant.
**En el proyecto:** El design system del sistema de mensajería con tokens en `designSystem.ts` y variables CSS de Tailwind está preparado para white-labeling futuro: La empresa podría ofrecer el CRM bajo la marca de sus clientes enterprise.

---

## Z

### Zod
**Categoría:** Librería / Validación
**Definición:** Librería de validación y parsing de esquemas para TypeScript. Permite definir esquemas con reglas de validación y derivar tipos TypeScript automáticamente con `z.infer<typeof schema>`. No requiere duplicar tipos y validaciones.
**En el proyecto:** Define `contactSchema` con validaciones de nombre (min 2 caracteres), teléfono (regex E.164) y email. El tipo `ContactFormValues` se infiere del schema: un único punto de verdad para tipo y validación.

### `zodResolver`
**Categoría:** React Hook Form / Zod
**Definición:** Función de `@hookform/resolvers/zod` que adapta un esquema Zod para usarlo como validador en React Hook Form. Ejecuta `schema.safeParse()` en cada validación y transforma los errores Zod al formato de RHF.
**En el proyecto:** En `ContactForm.tsx`: `useForm<ContactFormValues>({ resolver: zodResolver(contactSchema) })`. Cuando el usuario intenta guardar un contacto con teléfono inválido, Zod genera el error y RHF lo muestra en el `FormField` correspondiente.
