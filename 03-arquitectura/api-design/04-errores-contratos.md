# 04 — Errores y Contratos: Diseñar Respuestas de Error Útiles

Una API bien diseñada comunica los errores de forma que el cliente pueda entenderlos y reaccionar correctamente.

---

## El principio: errores para máquinas, mensajes para humanos

```json
// ❌ Error para humanos — la máquina no puede reaccionar diferente según el error
{ "error": "Something went wrong. Please try again." }

// ✓ Error para máquinas + mensaje para humanos
{
  "type":   "https://httpstatuses.io/409",
  "title":  "Conflict",
  "status": 409,
  "code":   "EMAIL_ALREADY_EXISTS",      ← la máquina reacciona según "code"
  "detail": "El email ya está registrado. Por favor usa otro.",   ← el humano lee esto
  "instance": "/api/users"
}
```

---

## Errores del cliente (4xx) vs del servidor (5xx)

### 4xx — el cliente debe cambiar algo

El cliente envió algo incorrecto. Reintentar la misma request no ayuda — debe corregir primero.

```
400 Bad Request
   - Body JSON malformado
   - Tipos incorrectos (string donde se esperaba int)
   - Parámetro requerido ausente

401 Unauthorized
   - Token ausente
   - Token expirado o con firma inválida
   → Cliente: renovar el token y reintentar

403 Forbidden
   - Token válido pero sin permiso para esta operación
   → Cliente: no reintentar — cambiar el flujo

404 Not Found
   - El recurso no existe
   → Cliente: no reintentar — el recurso no existe

409 Conflict
   - Email duplicado
   - Versión desactualizada (optimistic concurrency)
   → Cliente: cambiar el valor en conflicto

422 Unprocessable Entity
   - Reglas de negocio violadas
   → Cliente: corregir la request

429 Too Many Requests
   - Rate limit excedido
   → Cliente: esperar Retry-After segundos y reintentar
```

### 5xx — el servidor falló

El cliente no puede hacer nada. Reintentar puede funcionar (si es un error transitorio).

```
500 Internal Server Error — error genérico, no expongas el stack trace
503 Service Unavailable   — la app está reiniciando o bajo mantenimiento
504 Gateway Timeout       — servicio externo tardó demasiado
```

---

## Errores de validación — detalle por campo

El error de validación más útil incluye exactamente qué campo falló y por qué:

```json
// ❌ Poco útil — el cliente no sabe qué cambiar
{
  "status": 400,
  "detail": "Validation failed"
}

// ✓ Útil — el cliente sabe exactamente qué campos y por qué
{
  "type":   "https://httpstatuses.io/400",
  "title":  "Validation Error",
  "status": 400,
  "errors": {
    "email":    ["El email no tiene un formato válido."],
    "password": [
      "Mínimo 8 caracteres.",
      "Debe contener al menos una mayúscula."
    ]
  },
  "traceId": "00-abc123..."
}
```

---

## Error codes — máquina-legibles

Además del status HTTP, un `code` string permite al cliente distinguir entre errores del mismo status:

```json
// Ambos son 409, pero el cliente reacciona diferente:
{ "status": 409, "code": "EMAIL_ALREADY_EXISTS", "detail": "..." }
{ "status": 409, "code": "OUTDATED_VERSION",     "detail": "..." }
```

```csharp
// Definir los códigos como constantes
public static class ErrorCodes
{
    public const string EmailAlreadyExists  = "EMAIL_ALREADY_EXISTS";
    public const string OutdatedVersion     = "OUTDATED_VERSION";
    public const string InsufficientBalance = "INSUFFICIENT_BALANCE";
    public const string AccountInactive     = "ACCOUNT_INACTIVE";
}

// En la response del Handler
public sealed record InsertExampleUserConflictFailure(string Message)
    : InsertExampleUserResponse, IConflictFailure
{
    public string Code => ErrorCodes.EmailAlreadyExists;
}
```

---

## Contratos claros — qué puede esperar el cliente

### Documentar qué puede fallar

Para cada endpoint, el cliente necesita saber qué errores puede recibir:

```
POST /api/example/users

200 OK           — usuario creado
400 Bad Request  — body malformado o tipos incorrectos
401 Unauthorized — token ausente o inválido
409 Conflict     — el email ya existe (code: EMAIL_ALREADY_EXISTS)
422 Validation   — campos inválidos (detail incluye errors por campo)
500 Server Error — error interno
```

### Swagger / OpenAPI documenta los contratos

```csharp
// WebApi/EndPoints/ExampleUsers/ExampleUsersController.cs
[HttpPost]
[ProducesResponseType(typeof(ResultViewModel<ExampleUserDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public async Task<IActionResult> Insert([FromBody] InsertExampleUserBody body, CancellationToken ct)
{ ... }
```

---

## Mensajes de error — qué incluir y qué no

### Incluir

```json
{
  "status": 404,
  "code":   "USER_NOT_FOUND",
  "detail": "El usuario con ID abc-123 no fue encontrado.",
  "instance": "/api/users/abc-123",
  "traceId": "00-abc..."
}
```

### No incluir

```json
// ❌ Nunca en producción — expone información interna
{
  "exception": "NpgsqlException",
  "stackTrace": "at Npgsql.NpgsqlConnector...",
  "connectionString": "Host=postgres;Password=...",
  "innerException": "..."
}
```

**Regla:** los errores 5xx nunca deben exponer detalles técnicos en producción. El `traceId` en la response permite al equipo correlacionar con los logs internos sin exponer nada al cliente.

---

## Manejo de errores del lado del cliente

Patrón recomendado para consumir la API:

```typescript
// TypeScript / Frontend
async function getUser(id: string): Promise<User | null> {
  const response = await fetch(`/api/users/${id}`, {
    headers: { Authorization: `Bearer ${getToken()}` }
  });

  if (response.ok) {
    const result = await response.json();
    return result.data;
  }

  switch (response.status) {
    case 401:
      await refreshToken();
      return getUser(id);   // reintentar con token nuevo
    case 403:
      showPermissionError();
      return null;
    case 404:
      return null;
    case 429:
      const retryAfter = response.headers.get('Retry-After');
      await sleep(parseInt(retryAfter ?? '60') * 1000);
      return getUser(id);   // reintentar después del tiempo indicado
    default:
      const error = await response.json();
      logError(error.traceId, error.detail);
      throw new Error(error.detail ?? 'Error inesperado');
  }
}
```

---

## Relación con back-template

El proyecto usa `ResultViewModel<T>` para todas las respuestas:

```json
{
  "isSuccess": true/false,
  "message": "...",
  "data": {...},
  "utcTimeStamp": "..."
}
```

Para errores técnicos (excepciones), `GlobalExceptionHandler` retorna `ProblemDetails` RFC 7807. El equipo puede expandir esto con `code` strings y error codes para tipos de conflicto específicos.


---

*Rogelio Arriaga Gonzalez*
