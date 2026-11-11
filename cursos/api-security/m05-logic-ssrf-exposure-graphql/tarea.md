# Tarea - M05 - Lógica de negocio, destinos y exposición de datos; GraphQL

**Curso:** Seguridad de APIs: atacar y defender
**Tiempo estimado:** 2 a 3 horas

> Toda la tarea se hace sobre la aplicación de práctica o sobre una API **tuya**, en tu máquina, con datos ficticios. Nada contra sistemas ajenos.

---

## Parte 1 - Lecturas (1 hora)

| Recurso | Sección | Tiempo est. |
|---|---|---|
| `dev-notes/04-backend/45-idempotency-keys.md` | La solución y el middleware | 20 min |
| OWASP, *API Security Top 10 2023* | API3, API6, API7 y API10 | 20 min |
| `dev-notes/12-seguridad/09-graphql-seguridad.md` | Completa | 20 min |

### Recomendadas

| Libro | Capítulos |
|---|---|
| *Pentesting APIs* (Maurício Harley, 2024) | Cap. 8, Data Exposure and Sensitive Information Leakage; cap. 9, API Abuse and Business Logic Testing |
| *Web Application Security* (Andrew Hoffman, 2.ª ed.) | Cap. 18, Business Logic Vulnerabilities; cap. 36, Mitigating Business Logic Vulnerabilities |
| *Black Hat GraphQL* (Nick Aleks y Dolev Farhi, 2023) | Caps. 3 a 7, leídos como lista de lo que tu servidor debe resistir |
| *Bug Bounty Bootcamp* (Vickie Li, 2021) | Cap. 12, Race Conditions; cap. 13, Server-Side Request Forgery; cap. 17, Application Logic Errors and Broken Access Control |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Entregable: mapa de flujos y revisión de destinos

Un documento `m05-revision.md` sobre **una API tuya** (o la aplicación de práctica) con:

1. **Mapa de flujos de negocio.** Una tabla de **cinco** flujos con dinero o confianza de por medio: acción, regla de negocio, **dónde vive la regla hoy** (servidor, cliente, ninguna), y qué pasa si se repite 20 veces (por escrito o con una prueba).
2. **Una regla "una sola vez" probada.** Elige un flujo del mapa, implementa o localiza su regla, y entrega dos pruebas: una secuencial y una con peticiones simultáneas, **ambas vistas fallar** al quitar la regla.
3. **Contrato de respuesta.** Para tres endpoints: qué propiedades salen y cuáles necesita el negocio. Una prueba de contrato (lo que **no** debe estar en el cuerpo) vista fallar.
4. **Destinos.** ¿Tu API visita URLs que le da el cliente (webhooks, vistas previas, importaciones)? Si sí, lista de permitidos propuesta, casos que debe rechazar y la prueba. Si no, explica cómo lo verificaste.
5. **Terceros.** Una API externa que consumes: qué validas de lo que devuelve (estado, tipo, tamaño, forma, valores) y qué falta.
6. **Una hoja GraphQL.** Si tu proyecto usa GraphQL, la tabla de cinco frentes llena con tu configuración real. Si no, la tabla llena sobre la nota `09-graphql-seguridad.md`, con tus propias palabras y sin consultas de ataque.

### Reglas

- Cada afirmación sobre un estándar cita su fuente (OWASP, RFC, documentación).
- **No** incluyas consultas, cargas o instrucciones para explotar nada; el informe no las necesita.
- Una prueba que no cae al quitar el arreglo no cuenta.
- Lo que no verificaste lo marcas como "sin verificar".

---

## Criterios de evaluación

| Criterio | Puntos |
|---|---|
| Mapa de flujos completo, con la ubicación real de cada regla | 20 |
| Regla "una vez" con prueba secuencial y simultánea, ambas vistas fallar | 25 |
| Contrato de respuesta y su prueba | 15 |
| Revisión de destinos y de consumo de terceros | 20 |
| Hoja de los cinco frentes de GraphQL | 15 |
| Fuentes citadas y redacción | 5 |
| **Total** | **100** |

**Descuentos:** consultas o cargas de ataque en el documento (-30 y se repite la entrega); una prueba que no cae (-5 por cada una); fuentes inventadas (-20).

---

## Cómo entregar

Sube `m05-revision.md` y tus archivos de prueba a la plataforma. El quiz del módulo se resuelve en la plataforma.
