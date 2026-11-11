# Tarea - S01 - TypeScript para React

**React en producción con TypeScript**
**Tiempo estimado:** 3 a 4 horas

---

## Parte 1 - Lecturas (1.5 a 2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/06-frontend/01-variables-entorno-vite.md` | Completa; fíjate en cómo se tipa `import.meta.env` | 15 min |
| `dev-notes/06-frontend/12-typescript-para-react.md` | Los tipos se borran; Uniones discriminadas; Genéricos con restricción | 45 min |
| `dev-notes/06-frontend/12-typescript-para-react.md` | Errores típicos; Configuración del compilador | 30 min |
| react.dev, *Using TypeScript* | Typing props; Example hooks; Useful types | 20 min |

Lee la nota 12 **con el proyecto abierto**: cada ejemplo se compila, no se mira.

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Learn React with TypeScript* (Carl Rippon, Packt, 2025) | Cap. 1 - *Getting Started with React*; Cap. 2 - *Getting Started with TypeScript*; Cap. 3 - *Using React Hooks* |
| *TypeScript 5 Design Patterns and Best Practices* (Theofanis Despoudis, Packt, 2025) | Cap. 1 - *Getting Started with TypeScript 5*; Cap. 2 - *TypeScript Core Principles* |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: quitar los `any` de un módulo (2 a 2.5 horas)

Elige **un módulo de tu propio proyecto** (o, si no tienes uno, de la *Mesa de ayuda*, la parte de `src/features/auth`) con al menos **8 usos** de `any`, `as` o `!` entre todos. Debe tener lógica propia: un cliente de API, un hook de datos, un reductor, un formateador.

### Qué hacer

1. **Mide el antes.** Cuenta los `any`, los `as` y los `!` con un comando que puedas repetir, por ejemplo:
   ```bash
   grep -rnE ':\s*any\b|\bas any\b|<any>' src/mi-modulo | wc -l
   npx eslint src/mi-modulo --rule '{"@typescript-eslint/no-explicit-any":"error"}'
   ```
2. **Tipa la frontera primero**: lo que viene de `fetch`, de `localStorage` o de un `JSON.parse` entra como `unknown` y se valida (Zod o una guarda de tipo).
3. **Deriva los tipos del contrato** con `z.infer` o con los tipos de utilidad. Nada de copiar un `interface` a mano.
4. **Modela los estados** con una unión discriminada si el módulo tiene más de dos booleanos sueltos.
5. **Quita los `any`.** Cada uno que no se pueda quitar queda con un comentario `// any: <motivo>`.
6. **Mide el después** con el mismo comando.
7. **Rompe el contrato a propósito** (renombra un campo) y comprueba que el build falla. Guarda la salida.

### Qué entregas

Un `entrega-s01.md` y el enlace al commit o la carpeta del módulo, con:

| Sección | Contenido |
|---------|-----------|
| **Antes y después** | La tabla de conteos (`any`, `as`, `!`) y el comando que usaste |
| **Un error encontrado** | Un caso real en el que el tipo nuevo **marcó un error que ya existía**, con el mensaje del compilador |
| **Cambio de contrato** | El cambio que hiciste (qué campo) y la salida de `tsc` que muestra el build roto |
| **Frontera** | Dónde validas lo que llega del servidor y qué pasa si no cumple |
| **Decisiones** | Cada `as`, `!` o `any` que dejaste, con su motivo |
| **Configuración** | Tu `tsconfig.json` (o la diferencia con el del curso) y la versión de TypeScript |

### Criterios de evaluación

| Criterio | Puntos |
|----------|--------|
| Conteo antes/después reproducible y con reducción real | 20 |
| La frontera con el servidor valida (no solo tipa) | 20 |
| Tipos derivados del contrato, sin copias a mano | 15 |
| Un error previo encontrado y explicado | 15 |
| El build rompe con un cambio de contrato (evidencia) | 15 |
| Las decisiones sobre lo que se queda están justificadas | 10 |
| Claridad de la entrega | 5 |

**Mínimo para aprobar:** 70 puntos y la frontera validada.

### Errores que bajan la nota

- Reemplazar `any` por `as Algo`: el compilador se calla y la promesa sigue sin comprobarse.
- Tipos duplicados: un `interface` a mano junto a un esquema que ya describe lo mismo.
- Un `catch (e: any)` en lugar de `unknown` con una comprobación.
- Entregar solo el "después" sin demostrar que el build rompe.

---

## Parte 3 - Preparación para la sesión 2 (15 minutos)

Dibuja el árbol de carpetas de tu proyecto en `src/` hasta dos niveles y marca, con una flecha, **tres imports** de un archivo hacia otro que te parezcan "demasiado lejos". La sesión 2 parte de ese dibujo.

---

## Material

**Obligatorio**

- `dev-notes/06-frontend/01-variables-entorno-vite.md`.
- `dev-notes/06-frontend/12-typescript-para-react.md`, completa.

**Recomendado**

- *Learn React with TypeScript* (Rippon, 2025), caps. 1 a 3.
- *TypeScript 5 Design Patterns and Best Practices* (Despoudis, 2025), caps. 1 y 2.
