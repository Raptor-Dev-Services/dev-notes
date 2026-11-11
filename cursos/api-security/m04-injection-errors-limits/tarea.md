# Tarea - M04 - Inyección, errores y límites

**Curso:** Seguridad de APIs: atacar y defender
**Tiempo estimado:** 2 a 3 horas

> Toda la tarea se hace sobre la aplicación de práctica del curso o sobre una API **tuya**, en tu máquina. No se prueba nada contra sistemas ajenos.

---

## Parte 1 - Lecturas (1 hora)

| Recurso | Sección | Tiempo est. |
|---|---|---|
| `dev-notes/04-backend/08-problem-details.md` | Estructura RFC 9457 y manejador global | 15 min |
| `dev-notes/04-backend/12-rate-limiting.md` | Estrategias y configuración | 15 min |
| OWASP, *SQL Injection Prevention Cheat Sheet* | Defensas primarias | 15 min |
| OWASP, *Error Handling Cheat Sheet* | Recomendaciones | 15 min |

### Recomendadas

| Libro | Capítulos |
|---|---|
| *Pentesting APIs* (Maurício Harley, 2024) | Caps. 5, 6 y 7, leídos como la lista de lo que tu API debe resistir |
| *Web Application Security* (Andrew Hoffman, 2.ª ed.) | Cap. 13, Injection; cap. 14, Denial of Service; cap. 31, Defending Against Injection; cap. 32, Defending Against DoS |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Entregable: revisión de entradas, errores y límites de una API

Un documento `m04-revision.md` sobre **una API tuya** (la del proyecto de titulación sirve) o, si no tienes una, sobre la aplicación de práctica. Contiene:

1. **Inventario de entradas.** Tabla de **diez** endpoints con: ruta, de dónde llega cada dato (ruta, query, cuerpo, cabecera) y qué validación tiene hoy (tipo, largo, rango, formato).
2. **Consultas.** Cuántos lugares del código arman SQL u otra instrucción. Para cada uno: ¿parametrizado?, ¿cómo lo comprobaste? (búsqueda en el código y la prueba del apóstrofe).
3. **Errores.** Provoca **un** fallo interno controlado en tu API (una excepción que lanzas tú) y pega la respuesta completa. ¿Qué filtra? ¿Qué cambiarías? Si no filtra nada, explica qué lo impide.
4. **Límites.** Tabla de topes: paginación, tamaño del cuerpo, frecuencia de peticiones y tiempo máximo. Para cada uno, el valor y el lugar del código, o "no existe".
5. **Tres hallazgos redactados.** Cada uno con: título, clase (de la lista OWASP API Security Top 10 2023 o Top 10:2025, con su id), por qué importa, **corrección** y **prueba** que la demuestra (el nombre de la prueba y su resultado antes y después).

### Reglas

- Cada afirmación sobre un estándar cita su fuente (RFC, OWASP, documentación de Microsoft).
- La corrección se escribe como la haría el equipo de desarrollo. **No** incluyas entradas de ataque ni instrucciones para explotar nada.
- Una prueba que nunca falló no cuenta: el entregable muestra cada prueba **fallando** al quitar el arreglo, aunque sea con un interruptor o una modificación temporal.
- Si tu API no tiene un hallazgo de alguna clase, lo dices; no se inventan.

---

## Criterios de evaluación

| Criterio | Puntos |
|---|---|
| Inventario de entradas completo y honesto | 15 |
| Revisión de consultas con evidencia | 15 |
| Prueba de error con respuesta real y análisis | 20 |
| Tabla de límites con valores y lugares | 15 |
| Tres hallazgos con clase, corrección y prueba vista fallar | 30 |
| Redacción clara y fuentes citadas | 5 |
| **Total** | **100** |

**Descuentos:** respuestas de error inventadas o copiadas de otra API (-15); cargas de ataque o pasos de explotación en el documento (-30 y se repite la entrega); una prueba que no cae al quitar el arreglo (-5 por cada una).

---

## Cómo entregar

Sube `m04-revision.md` y los archivos de prueba que escribiste a la plataforma. El quiz del módulo se resuelve en la plataforma.
