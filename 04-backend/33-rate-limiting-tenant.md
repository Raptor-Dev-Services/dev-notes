# 33 — Rate Limiting por Tenant

El rate limiting por tenant evita que una empresa abuse de la API y degrade el servicio para los demás. A diferencia del rate limiting global (por IP), el rate limiting por tenant usa el `tenant_id` del JWT como clave — múltiples usuarios del mismo tenant comparten la misma cuota.

---

## Escenarios que justifican rate limiting por tenant

```
Sin rate limiting por tenant:
  Tenant A tiene un bug en su integración → llama /api/orders 10,000 veces/minuto
  → La DB se satura → todos los tenants experimentan latencia

Con rate limiting por tenant:
  Tenant A llega a su límite (ej. 1,000 req/min)
  → Recibe 429 Too Many Requests
  → Los demás tenants no se ven afectados
```

---

## Opciones en ASP.NET Core 10

ASP.NET Core incluye `Microsoft.AspNetCore.RateLimiting` (desde .NET 7). No hay que instalar nada extra.

### 1. Fixed Window — ventana fija (más simple)

```csharp
// Host.Api/Extensions/RateLimitingExtensions.cs
public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
{
    services.AddRateLimiter(options =>
    {
        // Política por defecto: 200 req/min por tenant
        options.AddPolicy("tenant", context =>
        {
            // Clave: tenant_id del JWT (o IP si no está autenticado)
            var tenantId = context.User.FindFirstValue("tenant_id");
            var key      = tenantId ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

            return RateLimitPartition.GetFixedWindowLimiter(key, _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window      = TimeSpan.FromMinutes(1),
                    QueueLimit  = 0   // no encolar — rechazar inmediatamente
                });
        });

        // Política estricta para endpoints sensibles: 10 req/min
        options.AddPolicy("strict", context =>
        {
            var tenantId = context.User.FindFirstValue("tenant_id")
                ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

            return RateLimitPartition.GetFixedWindowLimiter($"strict:{tenantId}", _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window      = TimeSpan.FromMinutes(1),
                    QueueLimit  = 0
                });
        });

        // Respuesta cuando se supera el límite
        options.OnRejected = async (context, ct) =>
        {
            context.HttpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";

            var retryAfter = context.Lease.TryGetMetadata(
                MetadataName.RetryAfter, out var retry)
                ? (int)retry.TotalSeconds : 60;

            context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();

            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                isSuccess  = false,
                message    = "Demasiadas solicitudes. Intenta de nuevo en un momento.",
                retryAfter = retryAfter
            }, ct);
        };
    });

    return services;
}
```

### 2. Sliding Window — ventana deslizante (más suave)

```csharp
return RateLimitPartition.GetSlidingWindowLimiter(key, _ =>
    new SlidingWindowRateLimiterOptions
    {
        PermitLimit          = 200,
        Window               = TimeSpan.FromMinutes(1),
        SegmentsPerWindow    = 4,   // ventana dividida en 4 segmentos de 15 seg
        QueueLimit           = 0
    });
```

La ventana deslizante distribuye mejor los picos — si el tenant hace 200 req en los primeros 15 segundos, en el siguiente segmento ya tiene cuota disponible proporcional.

---

## Registro y uso del middleware

```csharp
// Host.Api/Program.cs
builder.Services.AddAppRateLimiting();

// ...

app.UseRateLimiter();   // debe ir después de UseAuthentication para tener el tenant_id

// Orden correcto:
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantClaimsMiddleware>();
app.UseRateLimiter();             // ← después de auth — ya tiene el claim
app.MapControllers();
```

---

## Aplicar la política en los controllers

### A todo el controller

```csharp
[Route("api/orders")]
[Authorize]
[EnableRateLimiting("tenant")]   // política por defecto para todo el controller
public sealed class OrdersController : BaseApiController { ... }
```

### A endpoints específicos

```csharp
[Route("api/reports")]
[Authorize]
public sealed class ReportsController : BaseApiController
{
    [HttpPost("generate")]
    [EnableRateLimiting("strict")]   // reportes tienen límite estricto
    public async Task<IActionResult> Generate(...) { ... }
}
```

### Desactivar para endpoints que no necesitan límite

```csharp
[HttpGet("health")]
[DisableRateLimiting]   // el health check no debe limitarse
public IActionResult Health() => Ok();
```

---

## Rate limiting por plan

Los tenants con plan Enterprise pueden tener límites más altos. Leer el plan del JWT o del accessor:

```csharp
options.AddPolicy("tenant", context =>
{
    var tenantId = context.User.FindFirstValue("tenant_id");
    var plan     = context.User.FindFirstValue("plan") ?? "free";  // agregar "plan" al JWT
    var key      = tenantId ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon";

    var limit = plan switch
    {
        "enterprise" => 2000,
        "pro"        => 500,
        "free"       => 100,
        _            => 50
    };

    return RateLimitPartition.GetFixedWindowLimiter(key, _ =>
        new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit,
            Window      = TimeSpan.FromMinutes(1),
            QueueLimit  = 0
        });
});
```

Si el plan no está en el JWT, leerlo desde la DB (con caché):

```csharp
options.AddPolicy("tenant", context =>
{
    var tenantId  = context.User.FindFirstValue("tenant_id");
    var key       = tenantId ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon";

    // El TenantConfigAccessor ya fue llenado por el middleware
    var config    = context.RequestServices.GetService<ITenantConfigAccessor>()?.Current;
    var limit     = GetLimitForPlan(config?.Plan ?? "free");

    return RateLimitPartition.GetFixedWindowLimiter(key, _ =>
        new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit,
            Window      = TimeSpan.FromMinutes(1),
            QueueLimit  = 0
        });
});
```

---

## Rate limiting por tenant + branch

En sistemas con branches, puede ser útil limitar por `tenant_id:branch_id`:

```csharp
var tenantId = context.User.FindFirstValue("tenant_id") ?? "anon";
var branchId = context.User.FindFirstValue("branch_id");
var key      = branchId is not null
    ? $"{tenantId}:{branchId}"
    : tenantId;

// Cada branch tiene su propia cuota dentro del tenant
// Evita que una sucursal sature los recursos de otra
```

---

## Headers de respuesta — informar al cliente

El cliente debe saber cuánto límite le queda:

```csharp
// Middleware que agrega headers de rate limit a cada respuesta exitosa
// ASP.NET Core RateLimiting agrega automáticamente:
//   X-RateLimit-Limit: 200
//   X-RateLimit-Remaining: 150
//   X-RateLimit-Reset: 1717200000  (epoch del reset)

// Al rechazar (OnRejected):
//   HTTP 429
//   Retry-After: 45  (segundos hasta el reset)
```

---

## Redis para rate limiting distribuido

Si hay múltiples instancias de la API (horizontal scaling), el estado del rate limiter debe ser compartido. El rate limiter in-memory no funciona con múltiples pods.

```csharp
// Usar Redis con el paquete: dotnet add package Microsoft.AspNetCore.RateLimiting.Redis
// (o implementar con StackExchange.Redis directamente)

// Con StackExchange.Redis — Sliding Window manual
public sealed class RedisSlidingWindowRateLimiter
{
    private readonly IDatabase _redis;

    public async Task<RateLimitResult> IsAllowedAsync(
        string key, int limit, int windowSeconds)
    {
        var now   = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var start = now - windowSeconds * 1000;

        var multi = _redis.CreateTransaction();
        multi.SortedSetRemoveRangeByScoreAsync(key, 0, start);
        multi.SortedSetAddAsync(key, now.ToString(), now);
        multi.SortedSetLengthAsync(key);
        multi.KeyExpireAsync(key, TimeSpan.FromSeconds(windowSeconds + 1));

        await multi.ExecuteAsync();
        var count = (int)await _redis.SortedSetLengthAsync(key, start, now);

        return new RateLimitResult(count <= limit, limit - count);
    }
}
```

---

## Logging de rate limiting excedido

```csharp
options.OnRejected = async (context, ct) =>
{
    var tenantId = context.HttpContext.User.FindFirstValue("tenant_id") ?? "anon";
    var path     = context.HttpContext.Request.Path;

    // Loguear con Serilog — incluir tenant para diagnóstico
    Log.Warning("Rate limit excedido. TenantId: {TenantId}, Path: {Path}", tenantId, path);

    // ... respuesta 429 ...
};
```

---

## Checklist

- [ ] Política `tenant` aplicada como default en todos los controllers con `[Authorize]`
- [ ] Política `strict` para endpoints de alto costo (reportes, exportaciones, búsquedas pesadas)
- [ ] `[DisableRateLimiting]` en health check y webhooks entrantes
- [ ] `UseRateLimiter()` después de `UseAuthentication()` — necesita el claim `tenant_id`
- [ ] Headers `Retry-After` en respuestas 429
- [ ] Límites diferenciados por plan (Enterprise > Pro > Free)
- [ ] Redis para rate limiting con múltiples instancias (horizontal scaling)
- [ ] Alertas cuando un tenant supera el 80% de su límite frecuentemente (señal de que necesita upgrade)

---

## Glosario

| Término | Definición |
|---------|-----------|
| Rate Limiting por Tenant | Límite de requests configurado por tenant_id en lugar de por IP — justa distribución en SaaS |
| Tenant Partition Key | Uso del tenant_id como clave de partición en RateLimitPartition — cada tenant tiene su propio límite |
| Fixed Window por Tenant | Ventana fija de tiempo donde se cuentan los requests del tenant — límite se resetea al inicio de la ventana |
| Plan-based Limits | Límites de rate diferenciados por plan de suscripción: Enterprise tiene mayor límite que Free |
| OnRejected | Callback de rate limiting que construye la respuesta HTTP 429 con Retry-After y mensaje de upgrade |
| EnableRateLimiting | Atributo para aplicar una política de rate limiting específica a un endpoint o controller |
| DisableRateLimiting | Atributo para excluir endpoints como health checks o webhooks entrantes del rate limiting |
| UseRateLimiter | Middleware de ASP.NET Core que debe registrarse después de UseAuthentication para tener el claim tenant_id |
| Redis Distributed Rate Limiting | Rate limiting coordinado entre múltiples instancias del servidor usando Redis como store compartido |
| 429 Too Many Requests | Código HTTP devuelto cuando el tenant supera su límite — incluye headers Retry-After |
| Strict Policy | Política de rate limiting más restrictiva aplicada a endpoints de alto costo (reportes, exportaciones) |

---

*Rogelio Arriaga Gonzalez*
