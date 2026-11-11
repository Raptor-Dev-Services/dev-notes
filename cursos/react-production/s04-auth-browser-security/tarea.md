# Tarea - S04 - Autenticación y seguridad en el navegador

**React en producción con TypeScript**
**Tiempo estimado:** 4 a 5 horas

---

## Marco ético de la tarea

Todo ataque que pruebes en esta tarea es **contra tu propia aplicación, en tu máquina o en un entorno tuyo**. No pruebes tu carga de XSS en ningún sistema ajeno ni en uno que no te haya autorizado por escrito su dueño.

---

## Parte 1 - Lecturas (1.5 a 2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/10-cloud/07-seguridad-web.md` | A02 (cabeceras de seguridad), A05 (inyección), A07 (manejo de tokens), CORS | 40 min |
| `dev-notes/06-frontend/13-seguridad-en-el-navegador.md` | Completa | 40 min |
| MDN, *Content Security Policy (CSP)* | Completa | 30 min |

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *React Application Architecture for Production* (Alan Alickovic, Anthony Alicea, 2026) | Cap. 7 - Implementing Authentication and Securing the Application |
| *Web Application Security*, 2.ª ed. (Andrew Hoffman, 2023) | Cap. 10 - Cross-Site Scripting; Cap. 11 - Cross-Site Request Forgery; Cap. 28 - Defending Against XSS Attacks; Cap. 29 - Defending Against CSRF Attacks |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: la CSP de tu aplicación (2.5 a 3 horas)

Trabaja sobre **tu propio proyecto** (el de tu equipo, o la Mesa de ayuda si aún no tienes uno).

### Qué entregas

1. **La decisión de sesión** (`seguridad-s04.md`, sección 1): dónde vive la sesión de tu aplicación hoy (cookie, `localStorage`, memoria), qué defecto tiene y qué cambiarías. Si ya es una cookie `HttpOnly`, lista sus atributos (`Secure`, `SameSite`, `Path`, `Max-Age`, ¿prefijo `__Host-`?) y por qué cada uno.

2. **Una CSP sin `unsafe-inline`** para tu aplicación, con estas condiciones:
   - La cabecera completa, escrita en `seguridad-s04.md`, **directiva por directiva** con el motivo de cada una. Mínimo: `default-src`, `script-src`, `style-src`, `img-src`, `connect-src`, `object-src`, `base-uri`, `form-action`, `frame-ancestors`.
   - **Sin `unsafe-inline`** en `script-src`. Para `style-src`, si tu biblioteca de componentes inyecta estilos en línea y no hay otra opción, justifica la excepción y propón el plan para quitarla; no se acepta "porque si no, no funciona".
   - Cada origen de terceros que agregues (una tipografía, un analítico) se justifica: qué pasa si cae o si lo comprometen.
   - Tu aplicación **sigue funcionando** con ella: recorre todas las pantallas con la consola abierta y pega la consola **vacía de violaciones**.

3. **El camino**, en dos pasos, con evidencia:
   - Primero con `Content-Security-Policy-Report-Only`: lista cada violación que apareció y cómo la resolviste (mover un script en línea a un archivo, agregar un origen, cambiar una biblioteca).
   - Luego con `Content-Security-Policy` en modo bloqueo.

4. **La demostración del bloqueo**: una prueba que inyecta un script en línea en tu aplicación (como `e2e/csp.spec.ts` del proyecto de referencia) y demuestra que **no corre**; la salida de la prueba y el mensaje de la consola. Muestra también qué pasa si quitas la CSP: la prueba debe **caer**.

5. **Una revisión de sinks**: busca en tu proyecto `dangerouslySetInnerHTML`, `innerHTML`, `href={` y `src={` con datos del usuario, `eval`, `new Function`. Tabla: archivo, línea, de dónde viene el dato, ¿es seguro?, y el cambio (`HtmlSeguro`, texto con llaves, validar la URL). Si no hay ninguno, demuéstralo con la búsqueda que hiciste.

6. **CSRF**: explica qué defensa usa tu aplicación contra CSRF y demuéstralo con un experimento (un formulario de otro origen, como en el paso 5 de la práctica), con la salida real. Si no tienes ninguna, agrega la cabecera personalizada y repite.

### Condiciones

- Las cabeceras de seguridad (`X-Content-Type-Options: nosniff`, `Referrer-Policy`) también van, con una línea de justificación cada una.
- Nada de secretos en variables `VITE_` ni en `localStorage`: haz el inventario y anota qué hay en cada uno.
- Todo lo que muestres como salida es **real**, de tu máquina. Lo que no puedas ejecutar, márcalo como "no ejecutado" y di por qué.

---

## Criterios de evaluación

| Criterio | Peso | Cómo se evalúa |
|---|---|---|
| La CSP | 30 % | Sin `unsafe-inline` en scripts, cada directiva justificada, la aplicación funciona |
| El camino (reporte y luego bloqueo) | 15 % | Violaciones listadas y resueltas |
| La demostración del bloqueo | 20 % | La prueba pasa con la CSP y **cae** sin ella |
| La decisión de sesión | 10 % | Justificada con lo que un XSS puede y no puede hacer |
| Revisión de sinks | 10 % | Tabla completa o búsqueda demostrada |
| CSRF con experimento | 10 % | Salida real, defensa explicada |
| Marco ético | 5 % | Solo tu entorno |

**Entrega:** un enlace al repositorio con `seguridad-s04.md` y los cambios, o un `.zip`.

---

## Siguiente sesión

### S05 - Rendimiento, accesibilidad e idiomas

- Medir con el Profiler y Lighthouse antes de optimizar.
- Dividir el código; accesibilidad AA con teclado y lector de pantalla; internacionalización.

> Optimizar sin medir antes es adivinar con más trabajo.
