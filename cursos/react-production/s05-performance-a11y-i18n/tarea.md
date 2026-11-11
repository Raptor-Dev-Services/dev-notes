# Tarea - S05 - Rendimiento, accesibilidad e idiomas

**React en producción con TypeScript**
**Tiempo estimado:** 3 a 4 horas

---

## Parte 1 - Lecturas (1 a 1.5 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/06-frontend/02-react-produccion.md` | i18n con react-i18next; TanStack Query, patrones clave | 25 min |
| web.dev, "Web Vitals" (web.dev/articles/vitals) | Los tres umbrales y el percentil 75 | 15 min |
| web.dev, "Interaction to Next Paint" (web.dev/articles/inp) | Qué interacciones cuentan | 15 min |
| W3C, WCAG 2.2 (w3.org/TR/WCAG22/) | Los criterios de la tabla de la clase | 30 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *React Application Architecture for Production* (Alan Alickovic y Anthony Alicea, Packt, 2026) | Cap. 8 - Improving Application Performance; Cap. 9 - Going International; Cap. 10 - Making the Application Accessible |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: el reporte antes y después (2 a 2.5 horas)

Un reporte que un compañero pueda **repetir** y obtener números parecidos. Se hace sobre una app propia o sobre la Mesa de ayuda (Ejemplo ilustrativo), pero con **datos que tú puedas explicar**.

### Qué lleva

Un archivo `reporte-rendimiento.md` con estas secciones:

| Sección | Qué contiene |
|---------|--------------|
| Entorno | Versión de Node, del navegador, de Lighthouse y de las librerías que importan; máquina y carga aproximada |
| Bundle | JavaScript de la primera pantalla antes y después de **un** cambio, en kB sin comprimir y comprimidos, y el comando que lo midió |
| Peso por paquete | Los cinco paquetes que más pesan, con el script de la práctica |
| Interacción | Una interacción real (escribir en un filtro, abrir un menú) medida con Event Timing, antes y después, en tres corridas |
| Lighthouse | Los números de performance y accesibilidad, y la auditoría de mayor impacto |
| axe | Las violaciones que encontró y las que arreglaste, con el criterio de WCAG 2.2 de cada una |
| Teclado | El recorrido con las seis preguntas de la práctica, por pantalla |
| Lo que no valió la pena | Una optimización que probaste y descartaste, con el número que la descartó |

### Formato de cada medición

````markdown
### JavaScript de la primera pantalla
Qué cambié: dividí las rutas con `lazy`.
Comando: `npx vite build` y `PORT=3120 npx playwright test peso`.

| | Antes | Después |
|---|---|---|
| kB al abrir /entrar | 478.12 | 438.91 |
| Archivos | 1 | 1 |

Lectura: bajó 8.2 %. El resto lo pesan react-dom, react-router y zod.
````

Cada tabla lleva su comando y la lectura en una frase. Los números sin comando no cuentan.

### Reglas

- **Una variable por medición.** Si cambiaste tres cosas a la vez, el reporte no sirve.
- **Tres corridas por interacción** y se reporta el rango, no la mejor.
- **Nada de "se siente más rápido".** O hay número, o no hay afirmación.
- Si una optimización no cambió nada, **va al reporte igual**: es información.

---

## Parte 3 - Idiomas y teclado (45 a 60 minutos)

1. Agrega un segundo idioma a **una pantalla** de tu app con claves tipadas. Entrega la salida de `tsc` al escribir una clave inexistente.
2. Mide cuánto sumó la librería de i18n al JavaScript de la primera pantalla.
3. Escribe **una prueba de Playwright con teclado** que recorra un flujo (entrar y hacer una acción) sin usar el ratón y que verifique dónde queda el foco al terminar.

---

## Criterios de evaluación

| Criterio | Peso |
|---|---|
| Cada medición tiene comando, entorno y rango de corridas | 30 % |
| Se cambia una variable a la vez y el antes y el después son comparables | 20 % |
| Los hallazgos de axe y de teclado llevan criterio de WCAG 2.2 y arreglo | 20 % |
| El idioma agregado tiene claves tipadas y plurales | 15 % |
| Se reporta al menos una optimización descartada con su número | 15 % |

---

## Qué entregar

- El repositorio (o un zip) con el reporte, las pruebas de Playwright y los scripts de medición.
- El archivo `reporte-rendimiento.md` en la raíz.

---

## Siguiente sesión

### S06 - Pruebas y producción

- Vitest, Testing Library y MSW: qué probar y qué no.
- Una prueba E2E con Playwright, con trazas.
- Build, versión en los assets y caché del navegador.
- El pipeline del front y un contenedor.

> Un navegador con la versión vieja en caché hace que el arreglo exista solo en el servidor.
