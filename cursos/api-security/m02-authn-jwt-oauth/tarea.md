# Tarea - M02 - Autenticación, JWT y OAuth

**Curso:** Seguridad de APIs: atacar y defender
**Tiempo estimado:** 2 a 3 horas

> Toda la tarea se hace sobre **tu** código o sobre la API de referencia. No se prueba nada contra sistemas de otras personas ni se adivinan llaves.

---

## Parte 1 - Lecturas (1 hora)

| Recurso | Sección | Tiempo est. |
|---|---|---|
| `dev-notes/04-backend/21-jwt.md` | Emisión y validación | 20 min |
| `dev-notes/03-arquitectura/08-keycloak.md` | Cómo un servidor de identidad emite los tokens | 15 min |
| IETF, *RFC 8725* (JSON Web Token Best Current Practices) | Secciones 2 y 3 | 25 min |

### Recomendadas

| Libro | Capítulos |
|---|---|
| *Pentesting APIs* (Maurício Harley, 2024) | Cap. 4: autenticación y autorización |
| *The JWT Handbook* (Sebastián Peyrott, 2018) | "JSON Web Tokens in Detail" y "JSON Web Signatures" |
| *Keycloak* (Stian Thorgersen y Pedro Igor Silva, 2.ª ed.) | Caps. 3 a 5: estándares, OpenID Connect y OAuth 2.0 |
| *Hacking APIs* (Corey J. Ball, 2022) | Cap. 8: autenticación |
| *Zero Trust Networks* (Rais et al., 2024) | Cap. 6: confiar en las identidades |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Entregable: auditoría de autenticación de una API

Un documento `m02-auditoria-autenticacion.md` y el código de las pruebas nuevas, sobre **tu API** (o, si no tienes, la de referencia con una **segunda** API imaginaria que comparta emisor).

1. **Alcance.** Qué API y qué mecanismo usa (JWT propio, servidor de identidad, llaves de API).
2. **Tabla de la práctica (paso 6)** completa: 12 comprobaciones, qué hace tu API, si hay prueba y la brecha.
3. **Dos pruebas nuevas.** Escribe dos pruebas de "token malo" que **no existan** aún en `Modulo2Tests.cs` o en tu proyecto. Ejemplos: token con `nbf` en el futuro (`NotBefore`), token con el claim `sub` vacío o con un `iss` con diferencia de mayúsculas. Cada una debe esperar 401.
4. **Verlo fallar.** Para cada prueba nueva: el cambio que haces al código para quitar su control, la salida real donde la prueba cae y la restauración. Sin esta evidencia, la prueba no cuenta.
5. **Decisiones de tiempo.** Vida del token de acceso, `ClockSkew` y qué pasa cuando cambia el rol de una persona. Justifica cada número y di qué compromiso aceptas.
6. **Plan de rotación de la llave.** En un párrafo: dónde vive la llave, quién la puede leer, cómo se cambia sin una caída larga y qué ocurre con los tokens emitidos con la anterior.
7. **OAuth 2.0 / OpenID Connect.** Si tu API o su cliente usa un servidor de autorización, indica el flujo y verifica contra RFC 9700 que no usa el implícito ni la contraseña directa. Si no usa ninguno, explica cuándo convendría adoptarlo.

### Reglas del documento

- Cada afirmación sobre un estándar cita su fuente (RFC y sección).
- No incluyas llaves, tokens ni contraseñas reales; usa valores ficticios.
- Las pruebas deben correrse con un solo comando.

---

## Criterios de evaluación

| Criterio | Puntos |
|---|---|
| Alcance y tabla de 12 comprobaciones, con brechas honestas | 20 |
| Dos pruebas nuevas, bien planteadas (token realmente malo) | 25 |
| "Verlo fallar" con salida real para cada una | 20 |
| Decisiones de tiempo justificadas | 10 |
| Plan de rotación de la llave | 10 |
| OAuth/OpenID Connect contrastado con RFC 9700 | 10 |
| Claridad y fuentes | 5 |
| **Total** | **100** |

**Descuentos:** tokens, llaves o claves reales en el documento (-30 y se repite la entrega); salidas de pruebas copiadas o inventadas (-20).

---

## Cómo entregar

Sube `m02-auditoria-autenticacion.md` y las pruebas a la plataforma. El quiz del módulo se resuelve en la plataforma.
