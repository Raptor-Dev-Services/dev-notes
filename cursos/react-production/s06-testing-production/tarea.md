# Tarea - S06 - Pruebas y producción

**React en producción con TypeScript**
**Tiempo estimado:** 4 a 5 horas

---

## Parte 1 - Lecturas (1.5 a 2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/06-frontend/14-pruebas-front.md` | Completa | 30 min |
| `dev-notes/06-frontend/15-cache-y-versionado-de-assets.md` | Completa | 30 min |
| MDN, "Cache-Control" | `no-cache`, `max-age`, `immutable` y "Caching static assets with cache busting" | 20 min |
| playwright.dev, "Best Practices" | Completa | 20 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *React Application Architecture for Production* (Alan Alickovic y Anthony Alicea, Packt, 2026) | Cap. 11 - Testing the Application; Cap. 12 - Going to Production |
| *React Anti-Patterns* (Juntao Qiu, Packt, 2023) | Cap. 5 - Testing in React; Cap. 6 - Exploring Common Refactoring Techniques; Cap. 7 - Introducing Test-Driven Development with React |
| *Modern Full-Stack React Projects* (Daniel Bugl, Packt, 2026) | Cap. 9 - Implementing End-to-End Tests Using Playwright |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: el pipeline del front (2.5 a 3 horas)

Un pipeline que otra persona pueda clonar y correr, con **pruebas** y **despliegue en contenedor**, sobre una app propia o sobre la Mesa de ayuda (Ejemplo ilustrativo).

### Qué lleva

| Pieza | Requisito |
|-------|-----------|
| Pruebas de integración | Al menos 8, con Vitest, Testing Library y MSW, que cubran los cuatro estados de una pantalla, un error 422 y una red caída |
| Prueba E2E | Al menos una con Playwright, con `locale` fijo y `trace: 'retain-on-failure'` |
| Workflow de CI | `.github/workflows/front.yml`: `npm ci`, tipos, lint, pruebas, build y E2E; el reporte de Playwright se sube aunque falle |
| Imagen | `Dockerfile` de dos etapas, `.dockerignore` y `nginx.conf` |
| Caché | `index.html` con `no-cache`; `/assets/` con `max-age` largo e `immutable`; evidencia con `curl -I` |
| Fallo visto | Para **cada** tipo de prueba, una captura de cómo la viste fallar por la razón correcta |

### Reglas

- **Cada prueba se rompe a propósito al menos una vez.** El reporte incluye la salida de esa falla.
- **Nada de esperas fijas** (`setTimeout`, `waitForTimeout`): se usan `findBy`, `waitFor` o las aserciones de Playwright que esperan.
- **No se mockea un componente propio.** Se simula la red con MSW.
- Lo que no puedas ejecutar (por ejemplo, el workflow sin un repositorio en GitHub) **se marca como no ejecutado** y se valida lo que sí se puede: el YAML con un parser, la sintaxis de nginx con `nginx -t`.

### Formato del reporte

Un archivo `README-pipeline.md` con:

````markdown
## Pruebas
| Archivo | Qué comportamiento protege | La vi fallar con |
|---|---|---|
| tickets-page.test.tsx | el estado vacío | romper EstadoDeDatos (salida abajo) |

## Cabeceras
Salida de `curl -I` de `/` y de un asset.

## Lo que no ejecuté
- El workflow en GitHub (no tengo repositorio remoto), validado con PyYAML.
````

---

## Parte 3 - Caché del navegador (45 a 60 minutos)

1. Reproduce el caso del JavaScript viejo con tu propio servidor: un archivo `app.js` sin huella con `Cache-Control: public, max-age=14400`.
2. Muestra con Chromium (Playwright) que un navegador sigue con la versión 1 tras desplegar la 2.
3. Arréglalo de **dos** formas: huella en `?v=` y huella en el nombre del archivo. Entrega la salida de ambas y una frase sobre cuándo preferirías cada una.

---

## Criterios de evaluación

| Criterio | Peso |
|---|---|
| Las pruebas protegen comportamiento visible y cada una se vio fallar por la razón correcta | 25 % |
| El pipeline corre en el orden correcto (barato primero) y sube el reporte aunque falle | 20 % |
| El contenedor sirve `dist/` con las cabeceras de caché correctas, con evidencia | 20 % |
| Se reproduce y se arregla el caso del JavaScript viejo con salida real | 20 % |
| Lo no ejecutado está marcado, y lo que sí se pudo validar se validó | 15 % |

---

## Qué entregar

- El repositorio (o un zip) con las pruebas, el workflow, el `Dockerfile` y `nginx.conf`.
- `README-pipeline.md` con la evidencia.

---

## Proyecto final

Con las seis sesiones, entregas **un front completo contra una API real**: arquitectura por feature con una regla de importación en lint, formularios con errores del servidor, sesión y una CSP sin `unsafe-inline`, accesibilidad AA medida, reporte de rendimiento, pruebas y despliegue en contenedor. Criterios y fechas en el README del curso.
