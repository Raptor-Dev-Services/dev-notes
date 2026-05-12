# 05 — Buenas Prácticas: Diseño, Documentación y Evolución de APIs

Principios y decisiones de diseño que hacen una API predecible, robusta y fácil de consumir.

> Fuente: *Programming APIs with C# and .NET* (David Barkol) — Ch.8 API Design Best Practices

---

## Consistencia por encima de "lo correcto"

Una API inconsistente es más difícil de usar que una API que sigue convenciones no ideales pero consistentes.

```
❌ Inconsistente:
GET /api/users             → { "data": [...], "total": 100 }
GET /api/orders            → { "orders": [...], "count": 50 }
GET /api/products          → [ {...}, {...} ]      ← array directo
POST /api/users/create     → { "success": true }
POST /api/orders           → { "id": 123 }

✓ Consistente:
GET /api/users             → { "isSuccess": true, "data": { "items": [...], "total": 100 } }
GET /api/orders            → { "isSuccess": true, "data": { "items": [...], "total": 50 } }
POST /api/users            → { "isSuccess": true, "data": { "publicId": "...", ... } }
POST /api/orders           → { "isSuccess": true, "data": { "publicId": "...", ... } }
```

---

## IDs — nunca exponer IDs internos

```json
// ❌ Expone IDs secuenciales — enumeration attack
GET /api/users/1
GET /api/users/2
GET /api/users/999   ← fácil iterar todos los usuarios

// ✓ UUID públicos
GET /api/users/550e8400-e29b-41d4-a716-446655440000
// Imposible enumerar — no revela cuántos usuarios hay
```

El proyecto usa `Id` (serial interno) para joins en DB y `PublicId` (UUID) para la API. **Nunca devolver `Id` en las responses.**

---

## Nulls — ser explícito

```json
// ❌ Ausencia de campo — el cliente no sabe si no existe o no aplica
{ "fullName": "John", "deletedAt": null }
{ "fullName": "Jane" }

// ✓ Siempre incluir el campo, aunque sea null — el cliente sabe qué esperar
{ "fullName": "John", "deletedAt": null }
{ "fullName": "Jane", "deletedAt": null }
```

La excepción: campos opcionales que nunca aplican para un tipo de recurso pueden omitirse para simplificar el contrato.

---

## Idempotency Keys — POST seguros para reintentar

Para operaciones costosas o con side-effects (pagos, envío de emails), permitir al cliente reintentar con seguridad:

```
POST /api/payments/charge
Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000

Si el servidor ya procesó esta key → retornar el mismo resultado sin volver a ejecutar
Si el cliente no recibió la respuesta y reintenta → la operación no se duplica
```

```csharp
// WebApi — leer el header
var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].ToString();

// Infrastructure — guardar resultado por key
var cached = await _idempotencyRepo.GetAsync(idempotencyKey);
if (cached is not null) return cached;  // retornar respuesta ya guardada

var result = await ProcessAsync(request);
await _idempotencyRepo.SaveAsync(idempotencyKey, result, TimeSpan.FromHours(24));
return result;
```

---

## Versionado desde el primer día

Agregar versionado desde v1, aunque no se prevean cambios:

```
✓ /api/v1/users      — desde el día 1
✗ /api/users         — sin versión → cuando haya un breaking change, no hay cómo migrar gradualmente
```

Cuando se necesita un breaking change:
1. Crear `/api/v2/users` con el nuevo contrato
2. Mantener `/api/v1/users` funcionando (deprecar pero no apagar)
3. Comunicar a los clientes la fecha de fin de vida de v1
4. Dar tiempo suficiente para migrar (mínimo 6 meses para APIs públicas)

---

## Backwards compatibility — qué es breaking y qué no

```
✓ NO es breaking change (backwards compatible):
   - Agregar campo OPCIONAL en la response
   - Agregar campo OPCIONAL en el request body
   - Agregar un nuevo endpoint
   - Cambiar el orden de los campos en el JSON (los clients deben ignorar el orden)
   - Agregar un nuevo valor a un enum (si el cliente es tolerante a valores desconocidos)

✗ SÍ es breaking change:
   - Renombrar un campo
   - Cambiar el tipo de un campo (string → int)
   - Eliminar un campo
   - Cambiar el status code de una respuesta
   - Cambiar la semántica de un campo (significado distinto con el mismo nombre)
   - Hacer REQUERIDO un campo que era opcional
```

---

## Documentación — OpenAPI / Swagger

Una API sin documentación no existe para quienes no la escribieron.

```csharp
// Host/Extensions/SwaggerExtensions.cs — configuración del proyecto
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Back Template API",
        Version     = "v1",
        Description = "API de ejemplo con Clean Architecture + CQRS",
        Contact     = new OpenApiContact { Name = "Raptor Dev Services" }
    });

    // Incluir XML docs para descripción de endpoints
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});
```

```csharp
// Documentar con XML comments
/// <summary>Obtiene un usuario por su ID público.</summary>
/// <param name="id">UUID público del usuario</param>
/// <response code="200">Usuario encontrado</response>
/// <response code="404">Usuario no encontrado</response>
[HttpGet("{id:guid}")]
[ProducesResponseType(typeof(ResultViewModel<ExampleUserDto>), 200)]
[ProducesResponseType(typeof(ProblemDetails), 404)]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct) { ... }
```

---

## Rate Limiting — comunicarlo claramente

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 60
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1747038000

{
  "type":   "https://httpstatuses.io/429",
  "title":  "Too Many Requests",
  "status": 429,
  "detail": "Límite de 100 requests por minuto excedido. Reintenta en 60 segundos."
}
```

El cliente legítimo puede leer `Retry-After` y esperar el tiempo correcto sin backoff exponencial innecesario.

---

## Timeout y Cancellation

```csharp
// ✓ Respetar el CancellationToken en todos los handlers
public async Task<GetExampleUserResponse> Handle(
    GetExampleUserRequest request, CancellationToken ct)
{
    var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);  // ct se cancela si el cliente se desconecta
    // ...
}
```

Si el cliente cancela la request (navega a otra página, timeout del browser), el `CancellationToken` se cancela y el servidor puede detener el trabajo en progreso en lugar de completarlo innecesariamente.

---

## Checklist de diseño para un nuevo endpoint

```
Antes de implementar:
[ ] ¿El verbo HTTP es correcto? (GET para leer, POST para crear, etc.)
[ ] ¿La URL sigue las convenciones del proyecto?
[ ] ¿Los parámetros de query están documentados?
[ ] ¿Qué status codes puede retornar?
[ ] ¿Qué errores puede retornar el cliente?
[ ] ¿El endpoint requiere autenticación? ¿Qué roles?
[ ] ¿Hay paginación si puede retornar muchos resultados?

Antes de hacer PR:
[ ] ¿El response body es consistente con los otros endpoints?
[ ] ¿Se devuelve PublicId (UUID) en lugar de Id (serial)?
[ ] ¿Los campos de fecha son ISO 8601 UTC?
[ ] ¿Hay ProducesResponseType en el controller?
[ ] ¿dotnet build 0 errores?
```

---

## Relación con back-template

La plantilla ya implementa muchos de estos patrones:
- `ResultViewModel<T>` — respuesta consistente en todos los endpoints
- `PublicId` UUID — nunca se expone el `Id` serial
- JWT Bearer — autenticación stateless
- Rate limiting disponible en `Security.md`
- Swagger con Bearer UI configurado

Los patterns de este documento son extensiones naturales para cuando la API crezca: idempotency keys, error codes, versionado desde v1.


---

*Rogelio Arriaga Gonzalez*
