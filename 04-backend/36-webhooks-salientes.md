# 36 — Webhooks Salientes

Los webhooks salientes permiten que el SaaS notifique a los sistemas de los tenants cuando ocurren eventos relevantes: "una orden fue creada", "un pago fue procesado", "un usuario fue agregado". El tenant registra una URL en el SaaS y el SaaS hace un POST a esa URL cuando ocurre el evento.

---

## El modelo conceptual

```
SaaS                           Sistema del Tenant (ERP, custom app, etc.)
  │                                        │
  │  Ocurre evento                         │
  │  OrderCreated { id, tenantId, ... }    │
  │                                        │
  │  POST https://erp.alfacorp.com/hook    │
  │  Headers: X-Webhook-Signature: sha256=abc123
  │  Body: { "event": "order.created", "data": { ... } }
  │                                        │
  │                               ←  HTTP 200 OK
  │                                        │
  │  Si recibe 2xx → éxito                 │
  │  Si recibe 4xx/5xx o timeout → reintentar
```

---

## Entidades

### WebhookEndpoint: URL registrada por el tenant

```csharp
// Webhooks.Domain/Entities/WebhookEndpoint.cs
public sealed class WebhookEndpoint
{
    public long     Id          { get; init; }
    public Guid     PublicId    { get; init; }
    public long     TenantId    { get; init; }
    public long?    BranchId    { get; init; }   // null = recibe eventos de todo el tenant
    public string   Url         { get; init; } = string.Empty;
    public string   Secret      { get; init; } = string.Empty;   // para firmar el payload
    public string   Events      { get; init; } = string.Empty;   // "order.created,order.updated"
    public bool     IsActive    { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
```

### WebhookDelivery: intento de entrega

```csharp
// Webhooks.Domain/Entities/WebhookDelivery.cs
public sealed class WebhookDelivery
{
    public long     Id              { get; init; }
    public Guid     PublicId        { get; init; }
    public long     EndpointId      { get; init; }
    public long     TenantId        { get; init; }
    public long?    BranchId        { get; init; }
    public string   EventType       { get; init; } = string.Empty;   // "order.created"
    public string   Payload         { get; init; } = string.Empty;   // JSON
    public int      AttemptNumber   { get; init; }
    public DeliveryStatus Status    { get; init; }
    public int?     ResponseCode    { get; init; }
    public string?  ResponseBody    { get; init; }
    public long?    DurationMs      { get; init; }
    public string?  ErrorMessage    { get; init; }
    public DateTime CreatedAtUtc    { get; init; }
    public DateTime? DeliveredAtUtc { get; init; }
    public DateTime? NextRetryAt    { get; init; }
}

public enum DeliveryStatus
{
    Pending    = 0,   // en cola, no intentado aún
    Delivered  = 1,   // éxito (2xx)
    Failed     = 2,   // fallo no retriable (4xx)
    Retrying   = 3,   // fallo retriable, en espera del próximo intento
    Exhausted  = 4    // se agotaron los reintentos
}
```

---

## Tipos de eventos

```csharp
public static class WebhookEvents
{
    // Usuarios
    public const string UserCreated   = "user.created";
    public const string UserUpdated   = "user.updated";
    public const string UserDisabled  = "user.disabled";

    // Órdenes (ejemplo)
    public const string OrderCreated  = "order.created";
    public const string OrderUpdated  = "order.updated";
    public const string OrderCompleted = "order.completed";
    public const string OrderCanceled = "order.canceled";

    // Pagos
    public const string PaymentSucceeded = "payment.succeeded";
    public const string PaymentFailed    = "payment.failed";
}
```

---

## Publicar un evento desde un handler

```csharp
// Orders.Application/UseCases/CreateOrder/CreateOrderHandler.cs
public async Task<CreateOrderResponse> Handle(
    CreateOrderRequest request, CancellationToken ct)
{
    // ... lógica de negocio ...
    var orderId = await _orders.InsertAsync(order, ct);

    // Publicar evento para webhooks (en proceso — no bloquea)
    await _webhookPublisher.PublishAsync(
        tenantId:  request.TenantId,
        branchId:  request.BranchId,
        eventType: WebhookEvents.OrderCreated,
        payload:   new
        {
            id        = order.PublicId,
            number    = order.Number,
            tenantId  = request.TenantId,
            branchId  = request.BranchId,
            createdAt = DateTime.UtcNow
        },
        ct);

    return new CreateOrderSuccess(orderId);
}
```

---

## IWebhookPublisher: encolar la entrega

```csharp
// Webhooks.Application/Services/WebhookPublisher.cs
public sealed class WebhookPublisher : IWebhookPublisher
{
    private readonly IWebhookEndpointRepository _endpoints;
    private readonly IWebhookDeliveryRepository _deliveries;

    public async Task PublishAsync(
        long tenantId, long? branchId, string eventType,
        object payload, CancellationToken ct)
    {
        // 1. Buscar endpoints suscritos a este evento en este tenant
        var endpoints = await _endpoints.GetByTenantAndEventAsync(
            tenantId, branchId, eventType, ct);

        if (!endpoints.Any()) return;

        var payloadJson = JsonSerializer.Serialize(new WebhookPayload
        {
            Id        = Guid.NewGuid().ToString(),
            Event     = eventType,
            TenantId  = tenantId,
            BranchId  = branchId,
            OccurredAt = DateTime.UtcNow,
            Data      = payload
        });

        // 2. Crear una delivery por cada endpoint suscrito
        foreach (var endpoint in endpoints)
        {
            await _deliveries.InsertAsync(
                endpointId:    endpoint.Id,
                tenantId:      tenantId,
                branchId:      branchId,
                eventType:     eventType,
                payload:       payloadJson,
                attemptNumber: 1,
                status:        DeliveryStatus.Pending,
                nextRetryAt:   DateTime.UtcNow,   // inmediato
                ct:            ct);
        }
    }
}

public sealed record WebhookPayload
{
    public string   Id        { get; init; } = string.Empty;
    public string   Event     { get; init; } = string.Empty;
    public long     TenantId  { get; init; }
    public long?    BranchId  { get; init; }
    public DateTime OccurredAt { get; init; }
    public object?  Data      { get; init; }
}
```

---

## WebhookDeliveryWorker: Background Service que entrega

```csharp
// Webhooks.Infrastructure/BackgroundJobs/WebhookDeliveryWorker.cs
public sealed class WebhookDeliveryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookDeliveryWorker> _logger;
    private readonly HttpClient _httpClient;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var scope    = _scopeFactory.CreateScope();
            var deliveries     = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryRepository>();
            var endpointRepo   = scope.ServiceProvider.GetRequiredService<IWebhookEndpointRepository>();

            // Obtener deliveries pendientes de este momento
            var pending = await deliveries.GetPendingAsync(batchSize: 50, ct);

            foreach (var delivery in pending)
            {
                await DeliverAsync(delivery, endpointRepo, deliveries, ct);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    private async Task DeliverAsync(
        WebhookDelivery delivery,
        IWebhookEndpointRepository endpointRepo,
        IWebhookDeliveryRepository deliveryRepo,
        CancellationToken ct)
    {
        var endpoint = await endpointRepo.GetByIdAsync(delivery.EndpointId, ct);
        if (endpoint is null || !endpoint.IsActive)
        {
            await deliveryRepo.MarkAsFailedAsync(delivery.Id, "Endpoint inactivo", ct);
            return;
        }

        // Firmar el payload
        var signature = ComputeSignature(delivery.Payload, endpoint.Secret);

        var sw = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
            {
                Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Webhook-Signature", $"sha256={signature}");
            request.Headers.Add("X-Webhook-Event", delivery.EventType);
            request.Headers.Add("X-Webhook-Delivery", delivery.PublicId.ToString());
            request.Headers.Add("X-Tenant-Id", delivery.TenantId.ToString());

            using var cts     = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));  // timeout de 30 segundos

            var response     = await _httpClient.SendAsync(request, cts.Token);
            sw.Stop();

            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                await deliveryRepo.MarkAsDeliveredAsync(
                    delivery.Id, (int)response.StatusCode,
                    responseBody, sw.ElapsedMilliseconds, ct);
            }
            else
            {
                await HandleFailureAsync(delivery, deliveryRepo,
                    (int)response.StatusCode, responseBody, sw.ElapsedMilliseconds, ct);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            await HandleFailureAsync(delivery, deliveryRepo,
                null, ex.Message, sw.ElapsedMilliseconds, ct);
        }
    }

    private async Task HandleFailureAsync(
        WebhookDelivery delivery, IWebhookDeliveryRepository repo,
        int? statusCode, string errorMessage, long durationMs, CancellationToken ct)
    {
        // No reintentar errores 4xx (problema del cliente — no vas a tener éxito reintentando)
        if (statusCode.HasValue && statusCode >= 400 && statusCode < 500)
        {
            await repo.MarkAsFailedAsync(delivery.Id, errorMessage, ct);
            return;
        }

        // Reintentar con backoff exponencial
        var nextAttempt = delivery.AttemptNumber + 1;
        if (nextAttempt > MaxAttempts)
        {
            await repo.MarkAsExhaustedAsync(delivery.Id, errorMessage, ct);
            _logger.LogWarning(
                "Webhook exhausted. DeliveryId={DeliveryId}, TenantId={TenantId}, Event={Event}",
                delivery.PublicId, delivery.TenantId, delivery.EventType);
            return;
        }

        // Backoff: 1m, 5m, 30m, 2h, 12h
        var delay = GetBackoffDelay(delivery.AttemptNumber);
        await repo.MarkForRetryAsync(
            delivery.Id, nextAttempt, DateTime.UtcNow.Add(delay), errorMessage, ct);
    }

    private static TimeSpan GetBackoffDelay(int attempt) => attempt switch
    {
        1 => TimeSpan.FromMinutes(1),
        2 => TimeSpan.FromMinutes(5),
        3 => TimeSpan.FromMinutes(30),
        4 => TimeSpan.FromHours(2),
        _ => TimeSpan.FromHours(12)
    };

    private static string ComputeSignature(string payload, string secret)
    {
        var key  = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hash).ToLower();
    }

    private const int MaxAttempts = 5;
}
```

---

## Verificar la firma en el sistema del tenant

El sistema del tenant verifica que el webhook viene del SaaS (no de un tercero malicioso):

```csharp
// Ejemplo en el sistema receptor del tenant
[HttpPost("saas-webhook")]
public async Task<IActionResult> Receive(CancellationToken ct)
{
    var payload   = await Request.Body.ReadAllAsync(ct);
    var signature = Request.Headers["X-Webhook-Signature"].ToString();  // "sha256=abc123"
    var expected  = "sha256=" + ComputeSignature(payload, _webhookSecret);

    if (!CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(signature),
        Encoding.UTF8.GetBytes(expected)))
        return Unauthorized("Firma inválida.");

    // Procesar el evento
    // ...
    return Ok();
}
```

Usar `CryptographicOperations.FixedTimeEquals` en lugar de `==` para evitar timing attacks.

---

## Gestión de endpoints en el tenant

```csharp
[Route("api/webhooks")]
[Authorize(Roles = "Admin")]
public sealed class WebhooksController : BaseApiController
{
    [HttpGet]           // Listar endpoints registrados
    [HttpPost]          // Crear endpoint nuevo
    [HttpPut("{id:guid}")]    // Actualizar URL o eventos suscritos
    [HttpDelete("{id:guid}")] // Eliminar endpoint

    // Historial de deliveries
    [HttpGet("deliveries")]
    public async Task<IActionResult> GetDeliveries(
        [FromQuery] string? eventType, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        _ = await Mediator.Send(new GetDeliveriesRequest(
            CurrentTenantId, CurrentBranchId, eventType, page), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }

    // Re-enviar una delivery fallida
    [HttpPost("deliveries/{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
    {
        _ = await Mediator.Send(new RetryDeliveryRequest(id, CurrentTenantId), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }
}
```

---

## Webhooks y branches

En un sistema con branches, el tenant puede registrar endpoints a nivel tenant (reciben todos los eventos) o a nivel branch (reciben solo los de ese branch):

```csharp
// Buscar endpoints para un evento específico del branch
public async Task<List<WebhookEndpoint>> GetByTenantAndEventAsync(
    long tenantId, long? branchId, string eventType, CancellationToken ct) =>
    await _db.WebhookEndpoints
        .AsNoTracking()
        .Where(e => e.TenantId == tenantId
            && e.IsActive
            && e.Events.Contains(eventType)
            && (e.BranchId == null           // endpoints del tenant (reciben de todos los branches)
                || e.BranchId == branchId))  // o endpoints específicos de este branch
        .ToListAsync(ct);
```

---

## Checklist

- [ ] Payload firmado con HMAC-SHA256 usando el secret del endpoint
- [ ] Header `X-Webhook-Signature: sha256=...` en cada request
- [ ] Timeout de 30 segundos en el HTTP request
- [ ] No reintentar errores 4xx (son del cliente, no van a cambiar)
- [ ] Backoff exponencial para errores 5xx y timeouts
- [ ] Máximo 5 intentos antes de marcar como Exhausted
- [ ] Historial de deliveries visible para el Admin del tenant
- [ ] Re-envío manual disponible para deliveries fallidas
- [ ] Secret del endpoint diferente por tenant: nunca compartido
- [ ] El feature de webhooks solo disponible en planes que lo incluyen (ver `04-backend/31-planes-limites.md`)

---

## Glosario

| Término | Definición |
|---------|-----------|
| WebhookEndpoint | Entidad que almacena la URL destino, eventos suscritos y el secreto de firma de un tenant |
| WebhookDelivery | Registro de cada intento de entrega de un webhook con su estado, request y response |
| DeliveryStatus | Estado de un intento de entrega: Pending, Success, Failed, Exhausted |
| HMAC-SHA256 | Algoritmo de firma del payload del webhook para que el receptor verifique la autenticidad |
| Backoff Exponencial | Estrategia de reintento donde el tiempo entre intentos crece exponencialmente: 1min, 2min, 4min |
| WebhookDeliveryWorker | BackgroundService que procesa la cola de deliveries pendientes y ejecuta los reintentos |
| WebhookPublisher | Servicio que encola un WebhookDelivery cuando ocurre un evento relevante en el SaaS |
| X-Webhook-Signature | Header HTTP que contiene la firma HMAC-SHA256 del payload — el receptor la verifica |
| FixedTimeEquals | Comparación en tiempo constante de strings para prevenir timing attacks al verificar firmas |
| MaxAttempts | Límite de reintentos antes de marcar la entrega como Exhausted — típicamente 5 intentos |
| Webhook Secret | Secreto único por tenant usado para firmar y verificar los payloads — nunca compartido entre tenants |
| Event Type | Tipo de evento que disparó el webhook: tenant.user.created, payment.succeeded, etc. |

---

*Rogelio Arriaga Gonzalez*
