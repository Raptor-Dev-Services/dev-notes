# 08 — Problem Details: RFC 9457 y Manejo de Errores HTTP

Estándar para respuestas de error HTTP. En lugar de formatos ad-hoc, todas las APIs devuelven la misma estructura de error.

> Fuente: *Web API Development with ASP.NET Core 8* (Xiaodi Yan): Ch.9 Error Handling and Problem Details (RFC 7807)
>
> Norma vigente: [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457.html), de julio de 2023, que deja obsoleta a RFC 7807 y conserva los mismos miembros.

---

## El problema que resuelve

Sin un estándar, cada API inventa su propio formato de error:

```json
// ❌ Formato A — información mínima
{ "error": "Not found" }

// ❌ Formato B — estructura inconsistente
{ "success": false, "message": "Usuario no encontrado", "code": 404 }

// ❌ Formato C — detalle técnico expuesto al cliente
{
  "exception": "System.NullReferenceException",
  "stackTrace": "at App.Controllers...",
  "innerException": "..."
}
```

RFC 9457 define una estructura estándar que los clientes pueden procesar de forma uniforme.

---

## Estructura RFC 9457

```json
{
  "type":     "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title":    "Not Found",
  "status":   404,
  "detail":   "Usuario con ID abc-123 no encontrado.",
  "instance": "/api/example/users/abc-123",
  "traceId":  "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

| Campo | Origen | Qué contiene |
|-------|--------|--------------|
| `type` | Estándar | URI que identifica el tipo de problema. Si falta, se asume `about:blank` |
| `title` | Estándar | Descripción corta del problema — constante por tipo de error |
| `status` | Estándar | HTTP status code, solo informativo: manda el de la respuesta |
| `detail` | Estándar | Explicación legible del problema en esta instancia específica |
| `instance` | Estándar | URI que identifica esta ocurrencia del problema |
| `traceId` | Extensión | ID de traza para correlación en logs |

La norma no marca ningún miembro como obligatorio.

---

## ProblemDetails en ASP.NET Core

```csharp
// Host/Program.cs
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        // Agregar traceId a TODOS los ProblemDetails
        ctx.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;

        // En producción, no exponer detalles del entorno
        if (!ctx.HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            ctx.ProblemDetails.Detail = null;  // limpiar detalle técnico en prod
        }
    };
});

app.UseExceptionHandler();   // convierte excepciones no manejadas en ProblemDetails
app.UseStatusCodePages();    // convierte 404/405 bare en ProblemDetails
```

---

## Global Exception Handler

Centraliza el mapeo de excepciones a tipos específicos de ProblemDetails:

```csharp
// WebApi/Exceptions/GlobalExceptionHandler.cs
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext       httpContext,
        Exception         exception,
        CancellationToken ct)
    {
        // Cliente desconectado — no es un error del servidor
        if (exception is OperationCanceledException)
        {
            httpContext.Response.StatusCode = 499;
            return true;
        }

        // Validación — error del cliente (400)
        if (exception is ValidationException validationEx)
        {
            return await HandleValidationAsync(httpContext, validationEx, ct);
        }

        // Error interno — loguear y devolver 500 sin detalles técnicos
        _logger.LogError(exception,
            "Unhandled exception on {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var statusCode = MapToStatusCode(exception);
        var problem = new ProblemDetails
        {
            Status   = statusCode,
            Title    = GetTitle(statusCode),
            // Nunca exception.Message: puede traer cadenas de conexión, SQL o nombres internos.
            // El mensaje real ya quedó en el log, y el traceId lo conecta con esta respuesta.
            Detail   = "No se pudo completar la solicitud. Si el problema continúa, comparte el traceId con soporte.",
            Instance = httpContext.Request.Path,
            Type     = $"https://httpstatuses.io/{statusCode}"
        };
        problem.Extensions["traceId"] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", ct);
        return true;
    }

    private static async ValueTask<bool> HandleValidationAsync(
        HttpContext       context,
        ValidationException ex,
        CancellationToken ct)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Status   = 400,
            Title    = "Validation Error",
            Detail   = "Uno o más campos no son válidos.",
            Instance = context.Request.Path,
            Type     = "https://httpstatuses.io/400"
        };
        problem.Extensions["traceId"] =
            Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", ct);
        return true;
    }

    private static int MapToStatusCode(Exception exception) => exception switch
    {
        ArgumentException           => 400,
        UnauthorizedAccessException => 401,
        KeyNotFoundException        => 404,
        InvalidOperationException   => 422,
        TimeoutException            => 504,
        _                           => 500
    };

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        429 => "Too Many Requests",
        504 => "Gateway Timeout",
        _   => "Internal Server Error"
    };
}
```

```csharp
// WebApi/ServiceCollectionEx.cs
services.AddExceptionHandler<GlobalExceptionHandler>();
services.AddProblemDetails();
```

Dos trampas de este manejador, medidas en .NET 10:

- **`CustomizeProblemDetails` no lo cubre.** Esa personalización solo corre para lo que pasa por `IProblemDetailsService`, y este manejador escribe directo. Con `Detail = exception.Message`, el `ctx.ProblemDetails.Detail = null` de la sección anterior no hizo nada: en Production, una cadena de conexión con su contraseña salió completa en `detail`. Por eso el texto genérico va aquí. Lo mismo vale para las excepciones que se mapean a 4xx, porque una `InvalidOperationException` también puede venir del ORM y nombrar tablas.
- **`WriteAsJsonAsync` pisa el `Content-Type`.** Sin el argumento `contentType`, la respuesta sale como `application/json; charset=utf-8` aunque antes se haya asignado `application/problem+json`.

---

## Tipos de ProblemDetails por escenario

### 404 — Not Found

```csharp
// ✓ Respuesta del Handler usando Result Pattern
return new GetExampleUserNotFoundFailure("Usuario con ID {id} no encontrado.");

// El Presenter llama a _viewModel.Fail(message) → Status 404 en el controller
// O configurar en el GlobalHandler si se prefiere excepción → 404
```

### 409 — Conflict

```json
{
  "type":     "https://httpstatuses.io/409",
  "title":    "Conflict",
  "status":   409,
  "detail":   "El email 'user@example.com' ya está registrado.",
  "instance": "/api/example/users"
}
```

### 400 — Validation Error

```json
{
  "type":   "https://httpstatuses.io/400",
  "title":  "Validation Error",
  "status": 400,
  "errors": {
    "Email":    ["Email con formato inválido."],
    "Password": ["Mínimo 8 caracteres."]
  }
}
```

### 500 — Internal Server Error (producción)

```json
{
  "type":     "https://httpstatuses.io/500",
  "title":    "Internal Server Error",
  "status":   500,
  "traceId":  "00-abc123..."
}
```

En producción **nunca exponer** stack trace, nombre de la excepción ni mensajes internos.

---

## Mapeo desde el Result Pattern

El flujo en back-template combina Result Pattern + ProblemDetails:

```
Handler retorna INotFoundFailure
    ↓
Presenter: _viewModel.Fail(message)  →  IsSuccess = false
    ↓
Controller: StatusCode(500, _viewModel)
    ↓
ResultViewModel { IsSuccess: false, Message: "..." }

// Alternativa más semántica: mapear failures a status codes en el Presenter
private int MapFailureToStatusCode(IResponse response) => response switch
{
    INotFoundFailure   => 404,
    IConflictFailure   => 409,
    IValidationFailure => 400,
    IUnauthorizedFailure => 401,
    _                  => 500
};
```

---

## Relación con back-template

`back-template/docs/Errors.md` documenta `GlobalExceptionHandler` y la estrategia de errores del proyecto. Este documento explica el estándar RFC 9457 en profundidad.

El proyecto tiene `app.UseCoreProblemDetails()` de `Common.Web` que ya configura el middleware. El `GlobalExceptionHandler` en `WebApi/Exceptions/` complementa esto para excepciones específicas.

---

## Cuándo usar / Cuándo no usar

| Decisión | Cuándo |
|----------|--------|
| ✓ RFC 9457 `application/problem+json` | Siempre en APIs REST públicas |
| ✓ `ValidationProblemDetails` | Errores de validación (incluye `errors` por campo) |
| ✓ Custom `type` URI | Cuando el cliente necesita distinguir tipos de error programáticamente |
| ✗ Exponer stack trace | Nunca en producción |
| ✗ Exponer mensajes de excepción internos | Solo en desarrollo |
| ✗ Inventar formato de error propio | Siempre usar RFC 9457 |

**Regla:** `title` es constante para un tipo de error (igual para todos los 404). `detail` es específico de la instancia ("Usuario con ID X no encontrado").


---

## Glosario

| Término | Definición |
|---------|-----------|
| Problem Details | Formato estándar RFC 9457 para respuestas de error HTTP con campos type, title, status, detail e instance |
| RFC 9457 | Estándar IETF vigente para Problem Details en APIs HTTP; deja obsoleta a RFC 7807 |
| IExceptionHandler | Interfaz de ASP.NET Core 8 para manejar excepciones globales de forma tipada |
| ValidationProblemDetails | Extensión de ProblemDetails que incluye un diccionario de errores de validación por campo |
| traceId | Identificador único de una solicitud HTTP, incluido en ProblemDetails para correlacionar logs |
| UseExceptionHandler | Middleware de ASP.NET que captura excepciones y las procesa con IExceptionHandler registrados |
| UseStatusCodePages | Middleware que intercepta respuestas sin cuerpo (404, 405) y las formatea como ProblemDetails |
| type | Campo de ProblemDetails que es una URI que identifica el tipo de problema — constante para cada tipo de error |
| detail | Campo de ProblemDetails específico a la instancia del error — describe el contexto particular del fallo |
| title | Campo de ProblemDetails con la descripción corta del tipo de error — constante para todos los errores del mismo tipo |
| AddProblemDetails | Método de DI que registra el servicio ProblemDetails y permite personalizar campos extra |
| GlobalExceptionHandler | Implementación de IExceptionHandler que centraliza el manejo de todas las excepciones no controladas |

---

*Rogelio Arriaga Gonzalez*
