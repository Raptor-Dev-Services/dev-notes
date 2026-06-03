# 45 — Idempotency Keys: Prevenir Operaciones Duplicadas

Las idempotency keys previenen que una operación se ejecute más de una vez aunque el cliente la envíe múltiples veces. Son críticas en pagos, creación de órdenes, y cualquier operación que no deba repetirse (cobros duplicados, órdenes dobles, invitaciones múltiples).

---

## El problema sin idempotency keys

```
Cliente → POST /api/orders      → 201 Created  (red lenta, cliente no recibe respuesta)
Cliente → POST /api/orders      → 201 Created  (reintento automático)
Cliente → POST /api/orders      → 201 Created  (otro reintento)

Resultado: 3 órdenes idénticas en la base de datos
```

Con retry automático en el cliente (Axios, HttpClient, fetch con retry), este problema ocurre constantemente. Es especialmente frecuente en mobile con conexión intermitente.

---

## La solución: Idempotency Key en el header

```
Cliente genera una key única (UUID) por operación:
  X-Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000

Primera llamada  → POST /api/orders { X-Idempotency-Key: 550e... }
  → Se procesa, se guarda la respuesta asociada a esa key
  → Retorna 201 { orderId: "abc123" }

Segunda llamada  → POST /api/orders { X-Idempotency-Key: 550e... }  (reintento)
  → Se detecta que esa key ya fue procesada
  → Retorna 201 { orderId: "abc123" }  ← MISMA respuesta, sin procesar de nuevo

Resultado: solo 1 orden creada, aunque el cliente reintentó 3 veces
```

---

## Entidad IdempotencyKey

```csharp
// Common/Idempotency/Entities/IdempotencyRecord.cs
public sealed class IdempotencyRecord
{
    public long     Id              { get; init; }
    public Guid     PublicId        { get; init; }
    public long     TenantId        { get; init; }
    public long?    BranchId        { get; init; }
    public string   KeyHash         { get; init; } = string.Empty;  // SHA-256 de la key
    public string   Endpoint        { get; init; } = string.Empty;  // "POST /api/orders"
    public string   RequestBodyHash { get; init; } = string.Empty;  // SHA-256 del body
    public int      StatusCode      { get; init; }
    public string   ResponseJson    { get; init; } = string.Empty;
    public DateTime CreatedAtUtc    { get; init; }
    public DateTime ExpiresAtUtc    { get; init; }
}
```

La key del cliente se guarda como hash SHA-256, nunca el valor raw, para ahorrar espacio y evitar fugas si alguien accede a la tabla.

---

## Configuración EF Core

```csharp
// Shared/Database/EntityTypeConfigurations/IdempotencyRecordConfiguration.cs
public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityColumn();
        builder.Property(e => e.PublicId).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.KeyHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Endpoint).HasMaxLength(200).IsRequired();
        builder.Property(e => e.RequestBodyHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.ResponseJson).IsRequired();
        builder.Property(e => e.CreatedAtUtc).HasDefaultValueSql("now() AT TIME ZONE 'utc'");

        // Índice único: (tenant_id, key_hash, endpoint)
        // La misma key puede usarse en diferentes endpoints sin colisión
        builder.HasIndex(e => new { e.TenantId, e.KeyHash, e.Endpoint })
               .IsUnique()
               .HasDatabaseName("ix_idempotency_records_tenant_key_endpoint");

        // Índice para cleanup de expirados
        builder.HasIndex(e => e.ExpiresAtUtc)
               .HasDatabaseName("ix_idempotency_records_expires_at");
    }
}
```

---

## Servicio de idempotency

```csharp
// Common/Idempotency/IIdempotencyService.cs
public interface IIdempotencyService
{
    // Buscar si la key ya fue procesada para este endpoint y tenant
    Task<IdempotencyRecord?> GetAsync(
        long tenantId, long? branchId,
        string idempotencyKey, string endpoint,
        CancellationToken ct);

    // Guardar la respuesta después de procesar
    Task SaveAsync(
        long tenantId, long? branchId,
        string idempotencyKey, string endpoint, string requestBodyHash,
        int statusCode, string responseJson,
        CancellationToken ct);
}
```

```csharp
// Common/Idempotency/IdempotencyService.cs
public sealed class IdempotencyService : IIdempotencyService
{
    private readonly AppDbContext _db;

    public IdempotencyService(AppDbContext db) => _db = db;

    public async Task<IdempotencyRecord?> GetAsync(
        long tenantId, long? branchId,
        string idempotencyKey, string endpoint, CancellationToken ct)
    {
        var keyHash = ComputeHash(idempotencyKey);

        return await _db.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.TenantId == tenantId &&
                r.KeyHash  == keyHash  &&
                r.Endpoint == endpoint &&
                r.ExpiresAtUtc > DateTime.UtcNow, ct);
    }

    public async Task SaveAsync(
        long tenantId, long? branchId,
        string idempotencyKey, string endpoint, string requestBodyHash,
        int statusCode, string responseJson, CancellationToken ct)
    {
        var record = new IdempotencyRecord
        {
            PublicId        = Guid.NewGuid(),
            TenantId        = tenantId,
            BranchId        = branchId,
            KeyHash         = ComputeHash(idempotencyKey),
            Endpoint        = endpoint,
            RequestBodyHash = requestBodyHash,
            StatusCode      = statusCode,
            ResponseJson    = responseJson,
            CreatedAtUtc    = DateTime.UtcNow,
            ExpiresAtUtc    = DateTime.UtcNow.AddDays(7)  // las keys expiran a los 7 días
        };

        _db.IdempotencyRecords.Add(record);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Race condition: dos requests con la misma key llegaron simultáneamente
            // El que llegó segundo encontró el registro ya guardado — ignorar el error
        }
    }

    private static string ComputeHash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLower();

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("duplicate key") == true ||
        ex.InnerException?.Message.Contains("23505") == true;  // PostgreSQL unique violation
}
```

---

## Middleware de idempotency

```csharp
// Host.Api/Middleware/IdempotencyMiddleware.cs
public sealed class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;

    // Solo aplica a estos métodos — GET y DELETE no necesitan idempotency key
    private static readonly HashSet<string> IdempotentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH"
    };

    public IdempotencyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IIdempotencyService idempotency)
    {
        var method = context.Request.Method;

        if (!IdempotentMethods.Contains(method))
        {
            await _next(context);
            return;
        }

        // Si no viene X-Idempotency-Key, dejar pasar sin idempotency
        if (!context.Request.Headers.TryGetValue("X-Idempotency-Key", out var keyValues))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = keyValues.FirstOrDefault()?.Trim();
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            await _next(context);
            return;
        }

        // Extraer tenant del claim (el middleware de tenant ya corrió antes)
        var tenantIdClaim = context.User.FindFirstValue("tenant_id");
        if (!long.TryParse(tenantIdClaim, out var tenantId))
        {
            await _next(context);
            return;
        }

        long.TryParse(context.User.FindFirstValue("branch_id"), out var branchId);

        var endpoint = $"{method} {context.Request.Path}";

        // Leer el body para hashear (necesario para detectar body diferente con misma key)
        context.Request.EnableBuffering();
        var bodyBytes = await ReadBodyAsync(context.Request);
        var bodyHash  = Convert.ToHexString(SHA256.HashData(bodyBytes)).ToLower();
        context.Request.Body.Seek(0, SeekOrigin.Begin);  // rebobinar para que el controller lo lea

        // Buscar en DB si ya existe una respuesta para esta key
        var existing = await idempotency.GetAsync(
            tenantId, branchId > 0 ? branchId : null,
            idempotencyKey, endpoint, context.RequestAborted);

        if (existing is not null)
        {
            // Ya fue procesada — devolver la respuesta cacheada
            context.Response.StatusCode  = existing.StatusCode;
            context.Response.ContentType = "application/json";
            context.Response.Headers["X-Idempotency-Replayed"] = "true";
            await context.Response.WriteAsync(existing.ResponseJson, context.RequestAborted);
            return;
        }

        // Capturar la respuesta que genere el handler
        var originalBody   = context.Response.Body;
        using var captured = new MemoryStream();
        context.Response.Body = captured;

        await _next(context);

        // Guardar la respuesta para futuros reintentos
        captured.Seek(0, SeekOrigin.Begin);
        var responseJson = await new StreamReader(captured).ReadToEndAsync(context.RequestAborted);
        var statusCode   = context.Response.StatusCode;

        // Solo cachear respuestas exitosas (2xx) y de validación (4xx) — no errores de servidor (5xx)
        if (statusCode < 500)
        {
            await idempotency.SaveAsync(
                tenantId, branchId > 0 ? branchId : null,
                idempotencyKey, endpoint, bodyHash,
                statusCode, responseJson, context.RequestAborted);
        }

        // Copiar la respuesta al stream original
        captured.Seek(0, SeekOrigin.Begin);
        await captured.CopyToAsync(originalBody, context.RequestAborted);
        context.Response.Body = originalBody;
    }

    private static async Task<byte[]> ReadBodyAsync(HttpRequest request)
    {
        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms);
        return ms.ToArray();
    }
}
```

### Registro del middleware

```csharp
// Host.Api/Program.cs
// Orden importa: después de autenticación y tenant, antes de los controllers
app.UseAuthentication();
app.UseAuthorization();
app.UseTenantClaims();             // inyecta tenant_id en ITenantContextAccessor
app.UseMiddleware<IdempotencyMiddleware>();  // ← aquí
app.MapControllers();
```

---

## Idempotency en el handler (enfoque alternativo por use case)

Para endpoints que lo necesitan explícitamente sin el middleware global:

```csharp
// Orders.Application/UseCases/CreateOrder/CreateOrderHandler.cs
public sealed class CreateOrderHandler : IRequestHandler<CreateOrderRequest, CreateOrderResponse>
{
    private readonly IOrderRepository     _orders;
    private readonly IIdempotencyService  _idempotency;

    public async Task<CreateOrderResponse> Handle(
        CreateOrderRequest request, CancellationToken ct)
    {
        // Verificar idempotency a nivel de handler
        if (request.IdempotencyKey is not null)
        {
            var existing = await _idempotency.GetAsync(
                request.TenantId, request.BranchId,
                request.IdempotencyKey, "CreateOrder", ct);

            if (existing is not null)
            {
                // Deserializar la respuesta cacheada
                var cached = JsonSerializer.Deserialize<CreateOrderSuccessDto>(existing.ResponseJson);
                return new CreateOrderSuccess(cached!, replayed: true);
            }
        }

        // Lógica normal de negocio
        var order = new Order { ... };
        await _orders.InsertAsync(order, ct);

        var dto = new CreateOrderSuccessDto(order.PublicId, order.OrderNumber);
        return new CreateOrderSuccess(dto, replayed: false);
    }
}
```

```csharp
// CreateOrderRequest.cs
public sealed record CreateOrderRequest(
    long    TenantId,
    long?   BranchId,
    Guid    RequestedByPublicId,
    string? IdempotencyKey,      // opcional en el request
    // ... campos del pedido
) : IRequest<CreateOrderResponse>;
```

---

## Request body: mismo key, body diferente

Un cliente que reutiliza una key con un body diferente comete un error. La API debe rechazarlo:

```csharp
if (existing is not null)
{
    // Verificar que el body es el mismo
    if (existing.RequestBodyHash != bodyHash)
    {
        context.Response.StatusCode = 422;
        await context.Response.WriteAsJsonAsync(new
        {
            error   = "idempotency_key_mismatch",
            message = "La misma Idempotency-Key fue enviada con un cuerpo diferente. " +
                      "Usa una key distinta para una operación diferente."
        });
        return;
    }

    // Body igual → respuesta cacheada
    context.Response.StatusCode  = existing.StatusCode;
    context.Response.ContentType = "application/json";
    context.Response.Headers["X-Idempotency-Replayed"] = "true";
    await context.Response.WriteAsync(existing.ResponseJson);
    return;
}
```

---

## Idempotency con Stripe: pagos

Stripe acepta su propio sistema de idempotency keys. El patrón es pasar la key del cliente directamente a Stripe:

```csharp
// Billing.Infrastructure/Stripe/StripePaymentService.cs
public async Task<PaymentIntentDto> CreatePaymentIntentAsync(
    long tenantId, decimal amount, string currency,
    string clientIdempotencyKey, CancellationToken ct)
{
    // Construir una key de Stripe combinando tenant + key del cliente
    // para que sea única a nivel de plataforma
    var stripeIdempotencyKey = $"tenant-{tenantId}-{clientIdempotencyKey}";

    var options = new PaymentIntentCreateOptions
    {
        Amount   = (long)(amount * 100),
        Currency = currency,
        Customer = await GetStripeCustomerIdAsync(tenantId, ct),
    };

    var requestOptions = new RequestOptions
    {
        IdempotencyKey = stripeIdempotencyKey
    };

    var service = new PaymentIntentService();
    var intent  = await service.CreateAsync(options, requestOptions, ct);

    return new PaymentIntentDto(intent.Id, intent.ClientSecret, intent.Status);
}
```

Stripe retorna la misma `PaymentIntent` si recibe la misma `IdempotencyKey` dentro de 24 horas, incluso si la primera llamada nunca llegó a completarse.

---

## Idempotency en tenant + branch

Para sistemas con branches, la key debe ser única por tenant+branch:

```csharp
// Índice en la tabla: (tenant_id, branch_id, key_hash, endpoint)
builder.HasIndex(e => new { e.TenantId, e.BranchId, e.KeyHash, e.Endpoint })
       .IsUnique()
       .HasDatabaseName("ix_idempotency_records_tenant_branch_key_endpoint");
```

```csharp
// En el middleware, extraer branch_id del claim
long.TryParse(context.User.FindFirstValue("branch_id"), out var rawBranchId);
long? branchId = rawBranchId > 0 ? rawBranchId : null;

// Buscar incluyendo branch
var existing = await idempotency.GetAsync(
    tenantId, branchId, idempotencyKey, endpoint, ct);
```

Esto significa que:
- `tenant:1 branch:10 key:abc` → orden creada en la sucursal 10
- `tenant:1 branch:20 key:abc` → otra orden en la sucursal 20 (key diferente en contexto diferente)

Las mismas keys en branches distintos son operaciones distintas. Son contextos de negocio distintos.

---

## Limpieza de keys expiradas

```csharp
// Common/BackgroundJobs/IdempotencyCleanupJob.cs
public sealed class IdempotencyCleanupJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IdempotencyCleanupJob> _logger;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var cutoff  = DateTime.UtcNow;
                var deleted = await db.IdempotencyRecords
                    .Where(r => r.ExpiresAtUtc < cutoff)
                    .ExecuteDeleteAsync(ct);

                _logger.LogInformation(
                    "Idempotency cleanup: {Deleted} records eliminados", deleted);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error en IdempotencyCleanupJob");
            }

            // Ejecutar cada 24 horas
            await Task.Delay(TimeSpan.FromHours(24), ct);
        }
}
```

---

## Endpoints que NECESITAN idempotency keys

```
✓ POST /api/orders           → crear orden (dinero en juego)
✓ POST /api/payments         → procesar pago
✓ POST /api/invoices         → generar factura
✓ POST /api/users/invite     → enviar invitación (evitar emails duplicados)
✓ POST /api/data/export      → solicitar exportación
✓ POST /api/reports/generate → generar reporte (proceso costoso)
✓ POST /api/subscriptions    → crear suscripción en Stripe

✗ GET  /api/orders           → idempotente por naturaleza
✗ GET  /api/users/:id        → idempotente por naturaleza
✗ DELETE /api/orders/:id     → idempotente por diseño (borrar lo ya borrado = ok)
✗ POST /api/auth/login       → no se debe cachear (cada login es un evento nuevo)
✗ POST /api/auth/refresh     → no se debe cachear
```

---

## Respuesta al cliente

Cuando se detecta una respuesta cacheada (reintento):

```http
HTTP/1.1 201 Created
Content-Type: application/json
X-Idempotency-Replayed: true

{
  "data": {
    "orderId": "550e8400-e29b-41d4-a716-446655440000",
    "orderNumber": "ORD-2024-001"
  },
  "isSuccess": true,
  "message": null,
  "utcTimeStamp": "2024-01-15T10:00:00Z"
}
```

El header `X-Idempotency-Replayed: true` permite que el cliente sepa que recibió una respuesta cacheada. Es útil para logging y debugging.

---

## Ejemplo en el frontend (TypeScript)

```typescript
// frontend/src/lib/api.ts
async function createOrder(orderData: CreateOrderRequest): Promise<Order> {
    // Generar una key única para esta operación — guardar en localStorage
    // para poder reenviarla si la página se recarga
    const idempotencyKey = localStorage.getItem("pending_order_key")
        ?? crypto.randomUUID();
    localStorage.setItem("pending_order_key", idempotencyKey);

    try {
        const response = await fetch("/api/orders", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${getAccessToken()}`,
                "X-Idempotency-Key": idempotencyKey,
            },
            body: JSON.stringify(orderData),
        });

        const result = await response.json();

        if (result.isSuccess) {
            // Limpiar la key — la operación completó exitosamente
            localStorage.removeItem("pending_order_key");

            if (response.headers.get("X-Idempotency-Replayed") === "true") {
                console.log("Respuesta recuperada de caché — reintento exitoso");
            }

            return result.data;
        }

        throw new Error(result.message);
    } catch (error) {
        // En error de red, la key persiste en localStorage
        // El próximo intento usará la misma key → idempotent
        throw error;
    }
}
```

---

## Checklist

- [ ] `IdempotencyRecord` con `KeyHash` SHA-256 (nunca la key raw), `TenantId`, `BranchId`, `Endpoint`, `ExpiresAtUtc`
- [ ] Índice único en `(tenant_id, key_hash, endpoint)`: o `(tenant_id, branch_id, key_hash, endpoint)` con branches
- [ ] Solo cachear respuestas 2xx y 4xx: nunca 5xx (puede haber error transitorio)
- [ ] Rechazar misma key con body diferente (HTTP 422)
- [ ] Header `X-Idempotency-Replayed: true` en respuestas cacheadas
- [ ] Expiration de 7 días: job de limpieza nocturno
- [ ] Manejar race condition (dos requests simultáneas con misma key) con catch de unique constraint
- [ ] Endpoints de auth (login/refresh) excluidos de idempotency
- [ ] Stripe: usar `IdempotencyKey` en `RequestOptions` para pagos críticos
- [ ] Frontend: generar key con `crypto.randomUUID()`, persistir en localStorage hasta éxito

---

## Glosario

| Término | Definición |
|---------|-----------|
| Idempotency Key | Identificador único enviado por el cliente para que el servidor detecte y descarte requests duplicados |
| IdempotencyRecord | Entidad que almacena el hash de la key, el endpoint, el response cacheado y la fecha de expiración |
| IdempotencyMiddleware | Middleware que intercepta todos los requests POST/PUT para verificar y cachear respuestas por key |
| X-Idempotency-Key | Header HTTP estándar para enviar la clave de idempotencia — generado por el cliente con UUID v4 |
| SHA-256 Hash | Hash de la key almacenado en base de datos en lugar del valor original para consistencia e indexación |
| Unique Constraint Violation | Excepción que indica que dos requests simultáneos intentaron registrar la misma key — se resuelve con SELECT |
| X-Idempotency-Replayed | Header en la respuesta que indica que se está retornando una respuesta cacheada de un request anterior |
| Race Condition | Situación donde dos requests con la misma key llegan simultáneamente — se gestiona con catch de constraint |
| Stripe RequestOptions | Objeto de la SDK de Stripe donde se especifica la IdempotencyKey para pagos y suscripciones críticos |
| ExpiresAtUtc | Timestamp de expiración del IdempotencyRecord — típicamente 7 días — limpiado por job nocturno |
| Idempotencia de webhooks | Estrategia para ignorar eventos duplicados de Stripe usando el EventId como idempotency key |
| crypto.randomUUID() | API del navegador para generar UUIDs v4 criptográficamente seguros — forma recomendada de generar keys en el frontend |

---

*Rogelio Arriaga Gonzalez*
