# Tarea - S02 - Arquitectura por feature

**React en producción con TypeScript**
**Tiempo estimado:** 3 a 4 horas

---

## Parte 1 - Lecturas (1.5 a 2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/06-frontend/06-feature-hook.md` | Completa | 25 min |
| `dev-notes/06-frontend/07-capa-dominio-acl.md` | Completa | 30 min |
| `dev-notes/06-frontend/10-fronteras-import.md` | Completa; después, la lámina "Una trampa de versiones" de la clase | 25 min |
| `dev-notes/06-frontend/08-estado-donde-vive.md` | Solo "La escalera" (se retoma en la sesión 3) | 10 min |

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *React Application Architecture for Production* (Alan Alickovic y Anthony Alicea, Packt, 2026) | Cap. 1 - *Understanding the Architecture of React Applications*; Cap. 2 - *Setup and Project Structure Overview*; Cap. 3 - *Building and Documenting Components* |
| *React Anti-Patterns* (Juntao Qiu, Packt, 2023) | Cap. 3 - *Organizing Your React Application*; Cap. 4 - *Designing Your React Components*; Cap. 11 - *Introducing Layered Architecture in React* |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: el mapa de features de tu proyecto (2 a 2.5 horas)

Aplicas la sesión a **tu propio proyecto de React** (o, si no tienes uno, a una aplicación pública de React de tamaño mediano que elijas y cites). No tienes que reorganizarlo: tienes que **entenderlo, medirlo y planear**.

### Qué hacer

1. **Dibuja el árbol actual** de `src/` hasta tres niveles (`find src -type d | head -50` o `tree -d -L 3 src`).
2. **Identifica las features.** Una feature es un tema del negocio con pantalla, datos y reglas propias (no un tipo de archivo). Lista cada una con: nombre, qué pantallas tiene, qué datos usa, qué otras features necesita hoy.
3. **Mide el cruce.** Cuenta los imports que cruzan fronteras con un comando que puedas repetir. Por ejemplo, los que salen de una carpeta de una feature hacia otra:
   ```bash
   grep -rnE "from '\.\./(\.\./)*(otra-carpeta)" src/mi-feature | wc -l
   ```
   Entrega el comando y el número, no solo el número.
4. **Propón el árbol nuevo**: `app/`, `features/<nombre>/`, `shared/`. Para cada feature, su `index.ts` (qué exporta).
5. **Escribe la regla** de `eslint-plugin-boundaries` adaptada a tu árbol y **comprueba que corre** con un archivo que la viole a propósito. Pega el mensaje.
6. **Elige una dependencia entre features** que hoy exista y descríbela como se resolvería con composición en `app/` (qué pasa por props, qué conecta el layout).
7. **Plan de adopción:** cuántas violaciones hay, en qué orden se arreglan, qué tope pones en `--max-warnings` y en qué fecha el lint pasa de `warn` a `error`.

### Qué entregas

Un `entrega-s02.md`, y el enlace al repositorio o a los archivos de configuración, con:

| Sección | Contenido |
|---------|-----------|
| **Árbol actual y propuesto** | Los dos árboles, lado a lado |
| **Features** | La tabla del paso 2 |
| **Cruces medidos** | El comando, el número y los tres cruces más graves, explicados |
| **La regla** | Tu `eslint.config.js` (la parte del plugin) y el mensaje al violarla |
| **Una composición en `app/`** | El código antes y después de resolver una dependencia entre features |
| **Plan de adopción** | Orden, tope de advertencias y fecha |
| **Versiones** | Las de ESLint, `eslint-plugin-boundaries` y TypeScript con las que probaste |

### Criterios de evaluación

| Criterio | Puntos |
|----------|--------|
| Las features son temas de negocio, no tipos de archivo | 20 |
| Cruces medidos con un comando reproducible | 15 |
| La regla corre y falla con un caso hecho a propósito (evidencia) | 25 |
| Una composición en `app/` bien resuelta | 15 |
| Plan de adopción realista (orden, tope, fecha) | 15 |
| Claridad de la entrega | 10 |

**Mínimo para aprobar:** 70 puntos y la regla demostrada con un caso que falla.

### Errores que bajan la nota

- Entregar una regla que nunca se vio fallar (recuerda el caso 4.5 de la práctica: una regla puede quedar callada).
- Una "feature" llamada `components`, `common` o `utils`.
- Resolver una dependencia entre features con un `import` más: hay que conectarlas en `app/`.
- Un plan que activa la regla en `error` el primer día sobre cientos de violaciones.

---

## Parte 3 - Preparación para la sesión 3 (15 minutos)

Elige **una pantalla** de tu proyecto que traiga datos de una API. Anota qué pasa si la API tarda 10 segundos, si devuelve una lista vacía y si devuelve un error 500. En la sesión 3 la pasamos por esos tres escenarios.

---

## Material

**Obligatorio**

- `dev-notes/06-frontend/06-feature-hook.md`, `07-capa-dominio-acl.md` y `10-fronteras-import.md`.

**Recomendado**

- *React Application Architecture for Production* (Alickovic, 2026), caps. 1 a 3.
- *React Anti-Patterns* (Qiu, 2023), caps. 3, 4 y 11.
