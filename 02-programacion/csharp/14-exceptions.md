# 14 — Excepciones en C#

Las excepciones son errores que ocurren en tiempo de ejecución. C# tiene un sistema estructurado para lanzarlas, capturarlas y propagarlas.

> Fuente: *C# 13 and .NET 9: Modern Cross-Platform Development* (Mark J. Price) — Ch.3 Controlling Flow, Converting Types, and Handling Exceptions

---

## ¿Qué es una excepción?

Cuando algo sale mal en el código, el runtime crea un objeto `Exception` con información del error y lo "lanza" (`throw`). Si nadie lo captura, la aplicación falla.

```csharp
int[] numeros = { 1, 2, 3 };
Console.WriteLine(numeros[5]);  // IndexOutOfRangeException — índice fuera de rango
```

---

## `try/catch` — capturar excepciones

```csharp
try
{
    // Código que puede lanzar excepción
    var user = await repo.GetByPublicIdAsync(id, ct);
    return Ok(user);
}
catch (Exception ex)
{
    // Se ejecuta si ocurre cualquier excepción en el bloque try
    _logger.LogError(ex, "Error obteniendo usuario {UserId}", id);
    return StatusCode(500, "Error interno");
}
```

### Capturar tipos específicos

```csharp
try
{
    await _db.ExecuteAsync(sql, param);
}
catch (NpgsqlException ex) when (ex.SqlState == "23505")
{
    // Violación de constraint UNIQUE (código 23505 de PostgreSQL)
    return new InsertUserConflictFailure("El email ya está registrado.");
}
catch (NpgsqlException ex)
{
    // Otro error de PostgreSQL
    _logger.LogError(ex, "Error de base de datos");
    throw;  // re-lanzar — no sabemos manejar esto
}
catch (OperationCanceledException)
{
    // La petición fue cancelada — no es un error, solo retornar
    return new InsertUserCancelledFailure("Operación cancelada.");
}
catch (Exception ex)
{
    // Cualquier otra excepción
    _logger.LogError(ex, "Error inesperado");
    throw;
}
```

**El orden importa:** los catch más específicos deben ir primero.

### `when` — filtro de excepción

```csharp
catch (SqlException ex) when (ex.Number == 2627)
{
    // Solo captura si el número del error es 2627 (unique constraint)
    // Si el número es otro, la excepción sigue propagándose
}
```

---

## `finally` — siempre se ejecuta

El bloque `finally` se ejecuta sin importar si hubo excepción o no. Ideal para liberar recursos.

```csharp
NpgsqlConnection connection = null;
try
{
    connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync(ct);
    // ... usar la conexión
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error en la BD");
    throw;
}
finally
{
    // Siempre se ejecuta — con o sin excepción
    connection?.Dispose();  // ?.  por si connection es null (excepción en new)
}
```

**En la práctica, usa `using` en vez de `try/finally` para recursos:**

```csharp
// Equivalente al anterior pero más limpio:
using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync(ct);
// Al salir del scope (incluso por excepción), connection.Dispose() se llama automáticamente
```

---

## `throw` — lanzar excepciones

### Lanzar nueva excepción

```csharp
if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("Jwt:Key no está configurado.");

if (email is null)
    throw new ArgumentNullException(nameof(email), "El email no puede ser null.");

if (precio < 0)
    throw new ArgumentOutOfRangeException(nameof(precio), "El precio debe ser positivo.");
```

### Re-lanzar la misma excepción — preserva el stack trace

```csharp
try
{
    await _repo.InsertAsync(user, ct);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error insertando usuario");
    throw;       // ✓ re-lanza preservando el stack trace original
    // throw ex; // ❌ re-lanza pero PIERDE el stack trace — no uses esto
}
```

### Lanzar con inner exception

```csharp
try
{
    await _db.ExecuteAsync(sql, param);
}
catch (NpgsqlException dbEx)
{
    // Envolver en una excepción de dominio, preservando la original como inner
    throw new RepositoryException("Error guardando el usuario", dbEx);
    //                                                           ↑ innerException
}
```

---

## Excepciones de .NET más comunes

| Excepción | Cuándo ocurre |
|-----------|--------------|
| `NullReferenceException` | Acceder a miembro de un objeto null |
| `ArgumentNullException` | Argumento null cuando no se permite |
| `ArgumentException` | Argumento con valor inválido |
| `ArgumentOutOfRangeException` | Argumento fuera del rango permitido |
| `InvalidOperationException` | Operación no válida en el estado actual |
| `IndexOutOfRangeException` | Índice fuera de los límites del array |
| `KeyNotFoundException` | Clave no encontrada en Dictionary |
| `OperationCanceledException` | Operación cancelada via CancellationToken |
| `TaskCanceledException` | Task cancelado (hereda de OperationCanceledException) |
| `NotImplementedException` | Método no implementado (placeholder) |
| `NotSupportedException` | Operación no soportada en este contexto |
| `UnauthorizedAccessException` | Falta de permisos |
| `TimeoutException` | Operación excedió el tiempo límite |
| `FormatException` | String no tiene el formato esperado (Guid.Parse, int.Parse) |
| `OverflowException` | Resultado demasiado grande para el tipo |
| `DivideByZeroException` | División por cero |

---

## Excepciones personalizadas

Crear excepciones propias para errores de dominio:

```csharp
// Excepción base del dominio
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) 
        : base(message, innerException) { }
}

// Excepciones específicas
public sealed class RepositoryException : DomainException
{
    public RepositoryException(string message) : base(message) { }
    public RepositoryException(string message, Exception inner) : base(message, inner) { }
}

public sealed class ConfigurationException : DomainException
{
    public string ConfigKey { get; }
    
    public ConfigurationException(string configKey) 
        : base($"La configuración '{configKey}' es requerida pero no está definida.")
    {
        ConfigKey = configKey;
    }
}

// Uso
throw new ConfigurationException("Jwt:Key");
// "La configuración 'Jwt:Key' es requerida pero no está definida."
```

---

## Patrón del proyecto — manejo en controllers

```csharp
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
{
    try
    {
        _ = await Mediator.Send(new GetExampleUserRequest(id), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }
    catch (Exception ex)
    {
        // Log con el contexto de la operación
        _logger.LogError(ex, "Error en GetById ExampleUser id={UserId}", id);
        
        // Buscar la excepción raíz (la más profunda — la causa real)
        var innerEx = ex;
        while (innerEx.InnerException != null) innerEx = innerEx.InnerException!;
        
        // Devolver el mensaje al cliente
        return StatusCode(500, _viewModel.Fail(innerEx.Message));
    }
}
```

**Por qué buscar la inner exception:**
```
InvalidOperationException: "Unable to resolve service..."
    → PostgresException: "FATAL: role 'postgres' does not exist"
        → NpgsqlException: "Connection refused"
```
El mensaje útil está en la excepción más profunda. Las externas son wrappers.

---

## `OperationCanceledException` — no es un error

Cuando se cancela una operación (cliente desconectado, timeout), se lanza `OperationCanceledException`. **No debes loguear esto como error** — es comportamiento esperado.

```csharp
try
{
    _ = await Mediator.Send(request, ct);
    return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
}
catch (OperationCanceledException)
{
    // No loguear como error — el cliente se desconectó, es normal
    return StatusCode(499, "Client Closed Request");
    // 499 es el código no-oficial para "cliente cerró la conexión"
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error en GetById id={UserId}", id);
    var inner = ex;
    while (inner.InnerException != null) inner = inner.InnerException!;
    return StatusCode(500, _viewModel.Fail(inner.Message));
}
```

---

## Global exception handling — `ProblemDetailsMiddleware`

En el proyecto, `Common.Web` registra un middleware que captura excepciones no manejadas y las convierte en respuestas estándar (RFC 7807 ProblemDetails):

```csharp
// Program.cs — registrar el middleware
app.UseProblemDetailsMiddleware();  // de Common.Web

// Si un handler lanza excepción no capturada:
// → ProblemDetailsMiddleware la captura
// → Retorna JSON estándar:
// {
//   "type": "https://tools.ietf.org/html/rfc7807",
//   "title": "An error occurred while processing your request.",
//   "status": 500,
//   "traceId": "00-abc123..."
// }
```

El middleware de `Common.Web` captura:
- `ArgumentException` → 400 Bad Request
- `UnauthorizedAccessException` → 401 Unauthorized
- `KeyNotFoundException` → 404 Not Found
- `InvalidOperationException` → 422 Unprocessable Entity
- Cualquier otra `Exception` → 500 Internal Server Error

---

## Errores comunes

### Error 1 — Catch vacío (silenciar errores)

```csharp
// ❌ Captura la excepción y la ignora — el error desaparece silenciosamente
try
{
    await _repo.InsertAsync(user, ct);
}
catch (Exception)
{
    // Sin log, sin rethrow — el error se pierde
}

// ✓ Siempre al menos loguea
try
{
    await _repo.InsertAsync(user, ct);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error insertando usuario");
    throw;
}
```

### Error 2 — throw ex (pierde stack trace)

```csharp
// ❌ throw ex — resetea el stack trace, pierdes dónde ocurrió el error
catch (Exception ex)
{
    throw ex;  // el stack trace ahora empieza aquí, no en el origen real
}

// ✓ throw solo — preserva el stack trace original
catch (Exception ex)
{
    _logger.LogError(ex, "...");
    throw;  // re-lanza sin modificar
}
```

### Error 3 — Catch demasiado amplio sin re-throw

```csharp
// ❌ Capturar todo y retornar error genérico — esconde problemas de configuración
catch (Exception)
{
    return StatusCode(500, "Error");
}

// ✓ Loguear el detalle y retornar mensaje controlado al cliente
catch (Exception ex)
{
    _logger.LogError(ex, "Error en {Operacion}", nameof(GetById));
    var inner = ex;
    while (inner.InnerException != null) inner = inner.InnerException!;
    return StatusCode(500, _viewModel.Fail(inner.Message));
}
```


---

*Rogelio Arriaga Gonzalez*
