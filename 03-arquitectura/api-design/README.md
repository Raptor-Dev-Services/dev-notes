# API Design — Índice

Diseño de APIs REST: semántica HTTP, recursos, paginación, errores y evolución del contrato.

📁 Carpeta: [`api-design/`](api-design/)

---

## Documentos

| # | Documento | Temas cubiertos |
|---|-----------|-----------------|
| 1 | [HTTP](api-design/01-http.md) | Verbos, idempotencia, status codes, 401 vs 403, headers request/response |
| 2 | [REST](api-design/02-rest.md) | Recursos, URLs, constraints REST, verbos como sub-recursos, Richardson Maturity Model |
| 3 | [Paginación](api-design/03-paginacion.md) | Offset pagination, cursor/keyset, filtros, ordenamiento, problemas de OFFSET |
| 4 | [Errores y Contratos](api-design/04-errores-contratos.md) | 4xx vs 5xx, error codes, mensajes seguros, validación por campo, cliente-side handling |
| 5 | [Buenas Prácticas](api-design/05-buenas-practicas.md) | Consistencia, UUIDs públicos, backwards compatibility, idempotency keys, checklist |

---

## Mapa de relación con el proyecto

| Documento | Dónde se aplica en el proyecto |
|-----------|-------------------------------|
| HTTP | Verbos en los Controllers, status codes en `ResultViewModel`, `[Authorize]` |
| REST | Convenciones de rutas `/api/example/users`, nombres de endpoints |
| Paginación | `GetExampleUsersRequest` con page/pageSize, `GetExampleUsersSuccess` con total |
| Errores | `ResultViewModel`, `GlobalExceptionHandler`, `ProblemDetails` |
| Buenas Prácticas | `PublicId` UUID en responses, `ResultViewModel` consistente, Swagger |

---

## Orden de lectura sugerido

1. [HTTP](api-design/01-http.md) — semántica base
2. [REST](api-design/02-rest.md) — diseño de URLs y recursos
3. [Errores y Contratos](api-design/04-errores-contratos.md) — qué comunica la API cuando falla
4. [Paginación](api-design/03-paginacion.md) — colecciones grandes
5. [Buenas Prácticas](api-design/05-buenas-practicas.md) — evolución y mantenimiento


---

*Rogelio Arriaga Gonzalez*
