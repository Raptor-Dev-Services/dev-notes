# Tarea - M06 - Corregir, probar y reportar

**Curso:** Seguridad de APIs: atacar y defender
**Tiempo estimado:** 3 a 4 horas (es el proyecto final del curso)

> Toda la tarea se hace sobre **tu API propia**, en tu máquina. No se prueba nada contra sistemas ajenos y el informe no incluye datos reales de personas ni cargas de ataque.

---

## Parte 1 - Lecturas (1 hora)

| Recurso | Sección | Tiempo est. |
|---|---|---|
| `12-seguridad/10-pruebas-de-seguridad-de-apis.md` | Completa | 30 min |
| OWASP, *API Security Top 10, 2023 edition* (api-security.owasp.org) | Las diez fichas | 20 min |
| `04-backend/testing/03-integration-tests.md` | Pruebas de integración en memoria | 10 min |

### Recomendadas

| Libro | Capítulos |
|---|---|
| *Pentesting APIs* (Maurício Harley, 2024) | Cap. 10, Secure Coding Practices for APIs |
| *Web Application Security*, 2.ª ed. (Andrew Hoffman, 2023) | Caps. 20 a 22, 24 y 25 |
| *Threat Modeling* (Izar Tarandach y Matthew J. Coles, 2020) | Caps. 1, 2 y 5 |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Entregable: la prueba de seguridad completa de tu API

Un repositorio (o carpeta) con:

1. **Inventario.** Tabla de rutas (verbo, ruta, quién puede llamarla, qué objetos toca) y una prueba que la compare con las rutas reales.
2. **Modelo de amenazas de una hoja.** Las cuatro preguntas de la clase, contestadas para tu API.
3. **Hallazgos priorizados (al menos cuatro).** Cada uno con la ficha: título, clase del Top 10 de APIs 2023, dónde, evidencia mínima, impacto, corrección, prueba y prioridad.
4. **Correcciones con su prueba.** Por cada hallazgo, el cambio en el código y la prueba. Debes mostrar la secuencia **rojo, verde**: la salida de la prueba antes de la corrección y después.
5. **Una mutación.** Para dos de tus pruebas, la evidencia de que cae al quitar el control.
6. **CI.** El flujo que corre las pruebas de seguridad y la captura o descripción de la regla que bloquea el merge.
7. **Informe.** `informe.md` con alcance, método, hallazgos, qué quedó pendiente y qué no se probó.

### Reglas del informe

- Cada afirmación sobre un estándar cita su fuente (Top 10 de APIs, ASVS 5.0.0, documentación de ASP.NET Core).
- La corrección se escribe como la haría tu equipo. **No** incluyas cargas de ataque ni instrucciones para explotar nada.
- Toda la evidencia usa datos de ejemplo, ficticios.
- Si tu API no tiene una clase del Top 10 (por ejemplo, no llama a terceros), dilo y explica por qué no aplica.

---

## Criterios de evaluación

| Criterio | Puntos |
|---|---|
| Inventario con su prueba y coherente con las rutas reales | 15 |
| Modelo de amenazas de una hoja, específico de tu API | 10 |
| Cuatro hallazgos con clase correcta, evidencia mínima y prioridad razonada | 20 |
| Correcciones que atacan la causa y pruebas con secuencia rojo-verde | 25 |
| Dos mutaciones con la caída de la prueba | 10 |
| CI con regla de bloqueo | 10 |
| Informe claro, con lo pendiente y lo no probado | 10 |
| **Total** | **100** |

**Descuentos:** cargas de ataque o procedimientos de explotación (-30 y se repite la entrega); datos reales de personas (-20); prueba sin evidencia de haber fallado antes (-5 por hallazgo).

---

## Cómo entregar

Sube el repositorio comprimido (sin `bin/` ni `obj/`) y `informe.md` a la plataforma. El quiz del módulo se resuelve en la plataforma.
