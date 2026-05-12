# 01 — HTTP: Verbos, Status Codes e Idempotencia

El protocolo que subyace a todas las APIs REST. Entender HTTP semántico es la base de un buen diseño de API.

---

## Verbos HTTP — cuándo usar cada uno

| Verbo | Semántica | Idempotente | Body |
|-------|-----------|-------------|------|
| `GET` | Leer un recurso | ✓ | No |
| `POST` | Crear un recurso / acción no idempotente | ✗ | Sí |
| `PUT` | Reemplazar un recurso completo | ✓ | Sí |
| `PATCH` | Actualizar parcialmente un recurso | ✗ | Sí |
| `DELETE` | Eliminar un recurso | ✓ | Opcional |
| `HEAD` | Como GET pero sin body — verificar existencia | ✓ | No |
| `OPTIONS` | Descubrir métodos permitidos (CORS preflight) | ✓ | No |

### Idempotencia — la propiedad más importante

Una operación es **idempotente** si ejecutarla N veces produce el mismo resultado que ejecutarla 1 vez:

```
GET  /api/users/123    → mismo resultado siempre  ✓ idempotente
DELETE /api/users/123  → 1ra vez: elimina; 2da vez: 404 (mismo estado final) ✓ idempotente
PUT  /api/users/123    → siempre deja el recurso en el estado enviado ✓ idempotente
POST /api/users        → cada llamada crea un usuario nuevo ✗ no idempotente
```

La idempotencia importa porque los clientes reintentan requests ante fallos de red. Un GET o PUT reintentado es seguro. Un POST reintentado puede crear duplicados.

---

## Status Codes — las categorías

| Rango | Categoría | Qué significa |
|-------|-----------|--------------|
| 2xx | Éxito | La request fue recibida, entendida y aceptada |
| 3xx | Redirección | El cliente necesita tomar más acción |
| 4xx | Error del cliente | La request tiene un problema que el cliente puede corregir |
| 5xx | Error del servidor | El servidor falló al procesar una request válida |

### Los más importantes

```
200 OK             — éxito genérico, retorna body con datos
201 Created        — recurso creado exitosamente
204 No Content     — éxito sin body (DELETE exitoso, UPDATE sin retornar datos)

400 Bad Request    — body malformado, tipos incorrectos
401 Unauthorized   — no autenticado (confusing name: actually "unauthenticated")
403 Forbidden      — autenticado pero sin permiso
404 Not Found      — recurso no existe
409 Conflict       — conflicto con el estado actual (email duplicado, versión desactualizada)
422 Unprocessable Entity — body válido pero con errores de validación semántica
429 Too Many Requests   — rate limit excedido

500 Internal Server Error — error genérico del servidor
502 Bad Gateway         — el servidor upstream falló
503 Service Unavailable — servidor sobrecargado o en mantenimiento
504 Gateway Timeout     — el servidor upstream tardó demasiado
```

### 401 vs 403 — la distinción crítica

```
401 Unauthorized: "No sé quién eres" — falta o es inválido el token de autenticación
403 Forbidden:    "Sé quién eres pero no puedes hacer esto" — autenticado, sin autorización

Request sin token → 401
Request con token válido pero rol insuficiente → 403
```

### 404 vs 403 — privacy through ambiguity

A veces es mejor retornar 404 que 403, para no revelar que el recurso existe:

```
GET /api/invoices/456  (el usuario no es el dueño)
→ 403: "el recurso existe pero no tienes acceso" ← revela que Invoice 456 existe
→ 404: "no encontrado" ← no revela si existe o no

Para admin panels: 403 tiene más sentido (el admin ya sabe que existe)
Para APIs públicas con datos privados: 404 protege más la privacidad
```

---

## Headers HTTP — los más relevantes para APIs

### Request headers

```
Content-Type: application/json       — formato del body enviado
Accept: application/json             — formato esperado en la respuesta
Authorization: Bearer <jwt>          — autenticación
If-None-Match: "etag-value"          — conditional GET (caché)
If-Modified-Since: date              — conditional GET (caché)
X-Request-ID: uuid                   — ID de correlación para tracing
```

### Response headers

```
Content-Type: application/json       — formato del body de respuesta
Location: /api/users/123             — URL del recurso creado (con 201)
ETag: "version-hash"                 — versión del recurso para caché condicional
Cache-Control: max-age=60            — instrucciones de caché
Retry-After: 60                      — segundos a esperar antes de reintentar (429)
X-RateLimit-Limit: 100               — requests permitidos
X-RateLimit-Remaining: 95            — requests restantes
```

---

## GET vs POST para queries complejos

```
GET /api/users?filter=active&sort=name   — simple, cacheable, bookmark-able
POST /api/users/search { "filters": {...}, "sort": {...} }  — para queries complejos con muchos params
```

La regla general: GET para lectura siempre que los parámetros quepan en la URL. POST/search para filtros complejos que no caben o tienen lógica compleja.

---

## Relación con back-template

El proyecto usa los verbos y status codes de este documento:

```csharp
// GET → 200 OK
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
    _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);

// POST → 200 OK (el proyecto retorna 200 en lugar de 201 para simplificar)
// Para 201 + Location header:
return Created($"/api/example/users/{dto.PublicId}", _viewModel);

// DELETE → 204 No Content (si no retorna datos)
return _viewModel.IsSuccess ? NoContent() : StatusCode(500, _viewModel);
```


---

*Rogelio Arriaga Gonzalez*
