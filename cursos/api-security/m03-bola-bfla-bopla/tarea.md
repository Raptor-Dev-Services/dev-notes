# Tarea - M03 - Autorización rota: BOLA, BOPLA y BFLA

**Curso:** Seguridad de APIs: atacar y defender
**Tiempo estimado:** 2 a 3 horas

> Toda la tarea se hace sobre **tu** código o sobre la API de referencia. No se prueba nada contra sistemas de otras personas.

---

## Parte 1 - Lecturas (1 hora)

| Recurso | Sección | Tiempo est. |
|---|---|---|
| `dev-notes/04-backend/27-rbac.md` | Roles y permisos | 20 min |
| `dev-notes/04-backend/35-tenant-leakage-tests.md` | Cómo se prueba que nada se cruza | 20 min |
| OWASP, *API Security Top 10 2023* | API1, API3 y API5: "Description" y "How to prevent" | 20 min |

### Recomendadas

| Libro | Capítulos |
|---|---|
| *Hacking APIs* (Corey J. Ball, 2022) | Caps. 10 y 11: autorización y asignación masiva |
| *Pentesting APIs* (Maurício Harley, 2024) | Cap. 4: mecanismos de autorización (RBAC, ABAC, ámbitos OAuth) |
| *Web Application Security* (Andrew Hoffman, 2.ª ed.) | Caps. 15 y 33: atacar y defender datos y objetos |
| *Bug Bounty Bootcamp* (Vickie Li, 2021) | Caps. 10 y 17: referencias directas a objetos y control de acceso |
| *Building Secure and Reliable Systems* (Heather Adkins et al., 2020) | Cap. 5: privilegio mínimo |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Entregable: matriz de autorización de una API

Un documento `m03-matriz-autorizacion.md` y el código de las pruebas, sobre **tu API** (o la de referencia ampliada con dos rutas tuyas).

1. **Alcance.** Qué API, qué roles existen y quiénes son tus **dos** usuarios de prueba del mismo rol más un usuario de rol superior.
2. **La matriz.** Todas las rutas que reciben un identificador de objeto **y** todas las funciones de administración, con la respuesta esperada para cada actor (sin sesión, dueño, otro usuario del mismo rol, rol superior). Mínimo **15 filas**.
3. **Decisiones.** Para cada tipo de respuesta de negación (401, 403, 404): cuándo la usa tu API y por qué. Incluye la decisión sobre si el rol superior ve objetos ajenos.
4. **Las pruebas.** La matriz como `[Theory]` (o su equivalente en tu pila) y las pruebas de BOPLA: una de **escritura** (el cliente manda una propiedad que no debe poder cambiar) y una de **lectura** (la respuesta no incluye campos internos).
5. **Verlo fallar.** Para **tres** filas de tu matriz (una de BOLA, una de BFLA y una de BOPLA): el cambio de código que reintroduce la falla, la salida real donde la prueba cae y la restauración.
6. **Un hallazgo real o simulado.** Si encontraste una falla de verdad en tu API, redáctala con la ficha del módulo 6 (título, clase de API Top 10, dónde, evidencia mínima, corrección, prueba). Si no encontraste ninguna, di cómo lo comprobaste y **qué parte no pudo cubrir** tu matriz.
7. **Si tu API es multi-inquilino**, agrega una sección con dos inquilinos y la prueba de fuga. Si no, explica por qué no aplica.

### Reglas del documento

- No incluyas datos personales reales ni ids de sistemas ajenos.
- Cada afirmación sobre un estándar cita su fuente (API Top 10, cheat sheet de OWASP).
- Las pruebas deben correr con un solo comando.

---

## Criterios de evaluación

| Criterio | Puntos |
|---|---|
| Matriz completa (≥ 15 filas), con las rutas correctas | 25 |
| Pruebas de la matriz y de BOPLA que funcionan | 25 |
| "Verlo fallar" de tres filas, con salida real | 20 |
| Decisiones 401/403/404 justificadas | 10 |
| Hallazgo o análisis de lo que no cubre la matriz | 10 |
| Multi-inquilino o su explicación | 5 |
| Claridad y fuentes | 5 |
| **Total** | **100** |

**Descuentos:** pruebas que solo usan el rol con más permisos (-20); salidas copiadas o inventadas (-20); datos reales o de un sistema ajeno (-30 y se repite la entrega).

---

## Cómo entregar

Sube `m03-matriz-autorizacion.md` y las pruebas a la plataforma. El quiz del módulo se resuelve en la plataforma.
