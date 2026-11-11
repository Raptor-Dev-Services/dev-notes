# Tarea - M01 - Superficie e inventario

**Curso:** Seguridad de APIs: atacar y defender
**Tiempo estimado:** 2 a 3 horas

> Toda la tarea se hace sobre **tu** código o sobre la API de referencia del curso. No se prueba nada contra sistemas de otras personas.

---

## Parte 1 - Lecturas (1 hora)

| Recurso | Sección | Tiempo est. |
|---|---|---|
| OWASP, *API Security Top 10 2023* | Las diez fichas, solo "Description" y "How to prevent" de API1, API2 y API9 | 30 min |
| `dev-notes/04-backend/15-openapi.md` | Cómo se genera y publica el documento | 15 min |
| Microsoft, *Policy-based authorization in ASP.NET Core* | "Require global user authentication" y "Default and fallback policies" | 15 min |

### Recomendadas

| Libro | Capítulos |
|---|---|
| *Hacking APIs* (Corey J. Ball, 2022) | Caps. 1 a 3: cómo funcionan las aplicaciones web, la anatomía de las APIs y sus vulnerabilidades comunes |
| *Pentesting APIs* (Maurício Harley, 2024) | Caps. 1 y 3: el panorama de seguridad de APIs y el reconocimiento |
| *Attack Surface Management* (Ron Eddings y MJ Kaufmann, 2025) | Caps. 2 y 4: tipos de superficie y clasificación de activos |
| *Web Application Security* (Andrew Hoffman, 2.ª ed.) | Caps. 3 a 5: estructura de una aplicación moderna y análisis de la API |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Entregable: inventario de una API

Un documento `m01-inventario.md` y el código de su prueba, sobre **una API tuya** (de un proyecto, de Titulación o de trabajo propio) o, si no tienes una, sobre la API de referencia del curso **ampliada** con dos rutas que inventes (por ejemplo, `GET /api/pedidos/{id}/factura` y `DELETE /api/pedidos/{id}`).

1. **Alcance.** Qué API es, de quién es y qué entornos existen. Una frase: "Es mía / tengo autorización por escrito de...".
2. **Contrato de rutas.** El archivo `rutas-documentadas.txt` completo (una línea `VERBO /ruta`) y, junto a cada ruta, tres datos: quién puede llamarla (anónimo, cualquier usuario, rol), si recibe un identificador de objeto en la ruta y qué datos devuelve.
3. **La prueba.** El código de la prueba que compara las rutas reales con el contrato y falla si hay una sin documentar, una fantasma o una pública inesperada.
4. **Verlo fallar.** La salida de la prueba cuando agregas, a propósito, una ruta sin su línea. Copia el mensaje real y explica qué nombra.
5. **Ficha de inventario** de dos entornos (por ejemplo, producción y pruebas), con la tabla de la práctica. Si un dato falta, escribe "no sé" y lo que harías para averiguarlo.
6. **Tres rutas para el módulo 3.** De tu contrato, elige las tres rutas que reciben un identificador de objeto y explica en una frase por qué podrían ser vulnerables a BOLA (API1:2023) si faltara la comprobación de dueño. No las pruebes todavía: es la agenda del siguiente módulo.

### Reglas del documento

- Cada afirmación sobre un estándar cita su fuente (Top 10, ASVS, documentación de Microsoft).
- No incluyas direcciones, nombres de personas ni datos reales de un sistema de terceros.
- La prueba debe poder correrse con un solo comando y volverse a correr.

---

## Criterios de evaluación

| Criterio | Puntos |
|---|---|
| Alcance claro y propio | 10 |
| Contrato de rutas completo, con quién llama cada ruta | 20 |
| Prueba que compara rutas reales y contrato, y funciona | 25 |
| "Verlo fallar" con salida real y explicación correcta | 20 |
| Ficha de inventario de dos entornos, con los "no sé" honestos | 15 |
| Tres rutas candidatas a BOLA bien elegidas y justificadas | 10 |
| **Total** | **100** |

**Descuentos:** salida de prueba inventada o copiada de otra máquina (-20); información de un sistema que no es tuyo (-30 y se repite la entrega).

---

## Cómo entregar

Sube `m01-inventario.md` y el código de la prueba a la plataforma. El quiz del módulo se resuelve en la plataforma.
