# 12 — Rate Limiting en ASP.NET Core

Rate limiting protege la API contra abuso, ataques DDoS y tenants que saturan recursos. Desde .NET 7 hay middleware nativo en `Microsoft.AspNetCore.RateLimiting`.

> Fuente: *Web API Development with ASP.NET Core 8* (Xiaodi Yan) — Ch.11 Rate Limiting  
> Fuente: *ASP.NET Core 9 Essentials* (Packt) — Ch.8 Enhancing Applications with Middleware

---

## Estrategias disponibles

| Estrategia | Cómo funciona | Cuándo usar |
|------------|--------------|-------------|
| **Fixed Window** | X requests por ventana fija (ej: 100/min). Al cambiar de ventana, el contador se reinicia. | Rate limiting general simple |
| **Sliding Window** | Ventana móvil dividida en segmentos. Más justo — evita picos al inicio de la ventana. | Rate limiting justo por cliente |
| **Token Bucket** | Tokens se regeneran a tasa fija. Permite ráfagas controladas. | APIs que permiten picos cortos |
| **Concurrency** | Limita requests simultáneos activos (no por tiempo). | Endpoints costosos (reportes, exports) |

---

## Configuración en Program.cs

```csharp
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Fixed Window — para endpoints de autenticación (strict)
    options.AddPolicy("auth-strict", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,     // sin cola — rechazar inmediatamente
            });
    });

    // Sliding Window — para APIs por tenant (justo)
    options.AddPolicy("per-tenant", context =>
    {
        var tenantId = context.User.FindFirst("tenant_id")?.Value ?? "anonymous";
        return RateLimitPartition.GetSlidingWindowLimiter(tenantId, _ =>
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit      = 1000,
                Window           = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,   // 6 segmentos de 10 segundos
                QueueLimit       = 0,
            });
    });

    // Token Bucket — permite ráfagas cortas
    options.AddPolicy("api-burst", context =>
    {
        var clientId = context.User.FindFirst("sub")?.Value ?? "anonymous";
        return RateLimitPartition.GetTokenBucketLimiter(clientId, _ =>
            new TokenBucketRateLimiterOptions
            {
                TokenLimit          = 20,    // máximo de tokens acumulados
                TokensPerPeriod     = 10,    // tokens que se reponen por periodo
                ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                QueueLimit          = 0,
            });
    });

    // Concurrency — para endpoints costosos
    options.AddPolicy("heavy-ops", context =>
        RateLimitPartition.GetConcurrencyLimiter(
            context.User.FindFirst("sub")?.Value ?? "anonymous", _ =>
            new ConcurrencyLimiterOptions
            {
                PermitLimit = 3,    // máximo 3 requests simultáneos por cliente
                QueueLimit  = 5,    // cola de espera de 5
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }));
});

app.UseRateLimiter();   // antes de MapControllers/MapGet

app.MapPost("/api/auth/login",    LoginHandler)    .RequireRateLimiting("auth-strict");
app.MapPost("/api/reports/export", ExportHandler)  .RequireRateLimiting("heavy-ops");
app.MapControllers()                               .RequireRateLimiting("per-tenant");
```

---

## Respuesta cuando se excede el límite

```
HTTP/1.1 429 Too Many Requests
Retry-After: 30
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc6585#section-4",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Retry after 30 seconds."
}
```

```csharp
// Personalizar la respuesta 429
options.OnRejected = async (context, ct) =>
{
    context.HttpContext.Response.StatusCode = 429;

    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
    {
        context.HttpContext.Response.Headers.RetryAfter =
            ((int)retryAfter.TotalSeconds).ToString();
    }

    await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Title  = "Too Many Requests",
        Status = 429,
        Detail = "Rate limit exceeded. Please retry later.",
    }, ct);
};
```

---

## Beneficios del rate limiting

- **Protección contra sobrecarga** — el servidor mantiene performance estable bajo alta carga
- **Uso justo** — ningún cliente puede monopolizar los recursos (especialmente importante en multi-tenant)
- **Seguridad** — mitiga ataques DDoS y fuerza bruta en endpoints de autenticación
- **Experiencia consistente** — tiempos de respuesta estables para todos los usuarios

---

## Relación con `04-backend/33-rate-limiting-tenant.md`

El doc `33-rate-limiting-tenant.md` cubre la implementación específica para multi-tenancy con límites configurables por plan (Starter, Pro, Enterprise). Este doc cubre la configuración base del middleware nativo de ASP.NET Core.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Rate Limiting | Mecanismo que limita la cantidad de requests que un cliente puede hacer en un período de tiempo |
| Fixed Window | Algoritmo que cuenta requests en ventanas de tiempo fijas — reinicia el contador al inicio de cada ventana |
| Sliding Window | Algoritmo que evalúa los requests en una ventana deslizante — evita el burst al inicio de ventana |
| Token Bucket | Algoritmo que otorga tokens a tasa fija — los bursts se permiten hasta agotar el bucket |
| Concurrency Limiter | Limita el número de requests procesados simultáneamente, no la tasa de llegada |
| RateLimitPartition | Segmento de rate limiting — permite límites distintos por IP, usuario o tenant |
| OnRejected | Callback invocado cuando un request es rechazado por superar el límite configurado |
| 429 Too Many Requests | Código HTTP estándar devuelto cuando un cliente supera el rate limit |
| Retry-After | Header HTTP incluido en respuestas 429 que indica cuándo el cliente puede reintentar |
| EnableRateLimiting | Atributo para aplicar una política de rate limiting a un endpoint o controller específico |
| DisableRateLimiting | Atributo para excluir un endpoint de todas las políticas de rate limiting globales |
| Partition Key | Clave que identifica el segmento de rate limiting — puede ser IP, user_id o tenant_id |

---

*Rogelio Arriaga Gonzalez*
