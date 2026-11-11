# Glosario - S02 - Arquitectura por feature

> Definiciones de los términos técnicos de la sesión 2.
> Ordenado alfabéticamente.

---

## A

### ACL (capa anticorrupción)
**Categoría:** Arquitectura
**Definición:** Capa que traduce el modelo de un sistema externo al modelo propio, para que los cambios del externo no se filtren por toda la aplicación. En el front, suele ser una función que convierte el JSON de la API en el modelo que usa la pantalla.
**En el curso:** `aTicketVista(dto)` en `features/tickets/domain.ts`.



---

### Antipatrón
**Categoría:** Arquitectura
**Definición:** Solución que parece razonable y produce más problemas de los que resuelve cuando se repite. Se reconoce por síntomas recurrentes.
**En el curso:** La carpeta "común", el import profundo y la lógica de negocio dentro del JSX.



---

### API pública de una feature
**Categoría:** Arquitectura
**Definición:** Lo que una feature ofrece al resto de la aplicación, declarado en su `index.ts`. Todo lo demás es interno y puede cambiar sin avisar.
**En el curso:** `features/auth/index.ts` exporta `LoginPage`, `RutaProtegida`, `useSesion` y `useSalir`.



---

### app (capa)
**Categoría:** Arquitectura
**Definición:** Carpeta de nivel más alto: rutas, layout y proveedores. Compone las features y puede importar de `features` y de `shared`; nadie importa de ella.
**En el curso:** `src/app/router.tsx` y `src/app/layout.tsx`.



---

## B

### Barrel (archivo índice)
**Categoría:** Arquitectura
**Definición:** Archivo `index.ts` que reexporta lo que una carpeta quiere mostrar. En una feature, funciona como su API pública.
**En el curso:** Cada carpeta de `features/` trae el suyo.



---

## C

### Capturas (`capture`)
**Categoría:** Herramientas
**Definición:** Opción de `eslint-plugin-boundaries` que guarda fragmentos de la ruta de un elemento (por ejemplo el nombre de la carpeta de una feature) para usarlos en las políticas.
**En el curso:** `capture: ['nombre']` permite escribir "una feature solo puede importar de sí misma".



---

### Cohesión
**Categoría:** Arquitectura
**Definición:** Medida de cuánto pertenecen entre sí las cosas que están juntas. Alta cohesión: lo que cambia junto vive junto.
**En el curso:** En la estructura por feature, agregar un campo a los tickets abre una carpeta, no cinco.



---

### Composición
**Categoría:** Patrones
**Definición:** Construir algo uniendo piezas independientes, en lugar de que una pieza conozca a la otra. En React, pasar datos y funciones por props o hijos.
**En el curso:** `Layout` conecta `Cabecera` (perfil) con la sesión (auth) sin que se importen.



---

## D

### Dependencia
**Categoría:** Arquitectura
**Definición:** Relación en la que un archivo necesita a otro para funcionar; en el código, un `import`. Un grafo de dependencias sano tiene un solo sentido.
**En el curso:** `app` → `features` → `shared`.



---

### Dirección de dependencias
**Categoría:** Arquitectura
**Definición:** Regla de que las dependencias van en un solo sentido, de lo más específico a lo más general, y nunca al revés.
**En el curso:** `shared` no importa de `features`; una feature no importa de `app`.



---

### Dominio (modelo de dominio)
**Categoría:** Arquitectura
**Definición:** Representación de los conceptos del negocio y sus reglas, independiente de cómo llegan los datos (HTTP, base de datos) y de cómo se muestran (React).
**En el curso:** `TicketVista` y la regla de qué es urgente.



---

### DTO (Data Transfer Object)
**Categoría:** Arquitectura
**Definición:** Objeto cuya forma está dictada por el contrato de comunicación (la respuesta de la API), no por las necesidades de la aplicación.
**En el curso:** `Ticket` de `contract.ts` es el DTO; `TicketVista` es el modelo.



---

## E

### ESLint
**Categoría:** Herramientas
**Definición:** Analizador de código estático para JavaScript y TypeScript. Corre reglas y falla cuando se incumplen. Versión 10.14.0 a noviembre de 2026.
**En el curso:** `npm run lint` ejecuta también la regla de fronteras.



---

### Estructura por feature
**Categoría:** Arquitectura
**Definición:** Organizar el código en carpetas por tema del negocio (tickets, auth), cada una con sus pantallas, hooks, llamadas a la API y modelo.
**En el curso:** `src/features/tickets/`.



---

### Estructura por tipo de archivo
**Categoría:** Arquitectura
**Definición:** Organizar el código en carpetas por clase de archivo (`components/`, `hooks/`, `services/`). Funciona en aplicaciones pequeñas y dispersa los cambios al crecer.
**En el curso:** La aplicación `mesa-plana` de la práctica.



---

## F

### Feature
**Categoría:** Arquitectura
**Definición:** Módulo de la aplicación que corresponde a un tema del negocio. Tiene sus propias pantallas, datos y reglas, y una API pública.
**En el curso:** `auth`, `perfil` y `tickets`.



---

### `fileInternalPath`
**Categoría:** Herramientas
**Definición:** Campo de `eslint-plugin-boundaries` con la ruta de un archivo dentro de su elemento. Permite escribir que solo se puede importar `index.ts`.
**En el curso:** La política de `app` permite `fileInternalPath: 'index.ts'` en las features.



---

## I

### Import profundo
**Categoría:** Antipatrones
**Definición:** Importar un archivo interno de otra feature en lugar de su API pública. Congela la estructura interna de esa feature.
**En el curso:** `from '../tickets/ticket-form'` en lugar de `from '../tickets'`.



---

### `import/resolver`
**Categoría:** Herramientas
**Definición:** Ajuste que le dice a ciertos plugins de ESLint cómo resolver una ruta de import a un archivo real (por ejemplo, agregar `.ts` y `.tsx`).
**En el curso:** Sin él, `eslint-plugin-boundaries` no detecta imports como `'../auth'` y no avisa.



---

## M

### `--max-warnings`
**Categoría:** Herramientas
**Definición:** Opción de ESLint que hace fallar el comando si hay más de cierto número de advertencias. Sirve para impedir que el número crezca mientras se adopta una regla.
**En el curso:** `npx eslint . --max-warnings 0` termina con código 1 si hay una advertencia.



---

## N

### `no-restricted-imports`
**Categoría:** Herramientas
**Definición:** Regla de ESLint que prohíbe imports por patrón de texto. Sin dependencias, pero mira el texto del import, no a qué elemento apunta.
**En el curso:** Alternativa para pocas reglas simples sobre `shared`.



---

## P

### Pantalla (page)
**Categoría:** React
**Definición:** Componente asociado a una ruta. Orquesta hooks y componentes de una feature; no debería contener reglas de negocio.
**En el curso:** `TicketsPage`.



---

### Políticas (`policies`)
**Categoría:** Herramientas
**Definición:** Lista de reglas de `boundaries/dependencies`: quién puede importar a quién. Con `default: 'disallow'`, todo lo no permitido está prohibido.
**En el curso:** Cinco políticas definen la estructura `app` → `features` → `shared`.



---

## R

### Reexportar
**Categoría:** Patrones
**Definición:** Exportar desde un archivo lo que se importó de otro: `export { X } from './x'`. Base de los barrels.
**En el curso:** `export { TicketsPage } from './tickets-page'`.



---

### Refactorizar
**Categoría:** Procesos
**Definición:** Cambiar la estructura interna del código sin cambiar su comportamiento, con pruebas que lo comprueben.
**En el curso:** Mover archivos a `features/` con `tsc` y `vitest` después de cada paso.



---

### Responsabilidad única (SRP)
**Categoría:** Principios
**Definición:** Principio de que una unidad de código (función, componente, módulo) tenga una sola razón para cambiar.
**En el curso:** `aTicketVista` solo convierte; `useTickets` solo conecta la caché.



---

## S

### `select` (TanStack Query)
**Categoría:** Herramientas
**Definición:** Opción de `useQuery` que transforma los datos de la caché al leerlos. Se vuelve a ejecutar solo si cambian los datos o la función; una función en línea se ejecuta en cada render.
**En el curso:** `select: aPaginaVista` convierte DTO en `TicketVista`.



---

### `shared`
**Categoría:** Arquitectura
**Definición:** Capa de código que no conoce el negocio: cliente de API, componentes de UI base, utilidades. Solo puede importar de sí misma.
**En el curso:** `src/shared/api` y `src/shared/ui`.



---

## V

### Vista (modelo de vista)
**Categoría:** Patrones
**Definición:** Modelo ya preparado para la pantalla, con los campos derivados que esta necesita. Se deriva del DTO en una frontera.
**En el curso:** `TicketVista` con `abierto` y `urgente`.



---

*Versiones a noviembre de 2026; consulta el registro de npm para las vigentes.*
