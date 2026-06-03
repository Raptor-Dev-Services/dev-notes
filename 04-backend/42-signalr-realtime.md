# 42 — SignalR: Notificaciones en Tiempo Real por Tenant

SignalR provee comunicación bidireccional en tiempo real entre el servidor y el cliente (WebSocket con fallback a long-polling). En un SaaS multi-tenant, el aislamiento es crítico: las notificaciones de un tenant no deben llegar al cliente de otro tenant.

---

## Casos de uso en un SaaS

```
✓ Notificación cuando un job largo termina (reporte listo)
✓ Actualización en tiempo real de un dashboard operativo
✓ Alertas de sistema (pago fallido, límite alcanzado)
✓ Colaboración: otro usuario está editando el mismo registro
✓ Chat interno del tenant
✓ Notificaciones push dentro del SaaS (campana de notificaciones)
```

---

## Aislamiento por tenant: Groups de SignalR

SignalR tiene el concepto de Groups. Un cliente puede unirse a uno o más grupos y los mensajes enviados al grupo llegan solo a los miembros. En un SaaS multi-tenant, el group name incluye el `tenant_id`:

```
Group: "tenant:1"          → todos los usuarios del tenant 1
Group: "tenant:1:branch:10" → todos los usuarios del branch 10 del tenant 1
Group: "tenant:1:user:uuid" → un usuario específico del tenant 1
```

---

## Hub de notificaciones

```csharp
// Host.Api/Hubs/NotificationsHub.cs
[Authorize]
public sealed class NotificationsHub : Hub
{
    private readonly ITenantContextAccessor _tenantAccessor;

    public NotificationsHub(ITenantContextAccessor tenantAccessor)
        => _tenantAccessor = tenantAccessor;

    public override async Task OnConnectedAsync()
    {
        var tenantId  = Context.User!.FindFirstValue("tenant_id");
        var branchId  = Context.User!.FindFirstValue("branch_id");
        var userPubId = Context.User!.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (tenantId is null)
        {
            // Rechazar conexiones sin tenant_id
            Context.Abort();
            return;
        }

        // Unir al grupo del tenant (recibe notificaciones de todo el tenant)
        await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tenantId));

        // Si tiene branch, unir también al grupo del branch
        if (branchId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, BranchGroup(tenantId, branchId));

        // Grupo personal para notificaciones directas al usuario
        if (userPubId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(tenantId, userPubId));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // SignalR limpia los grupos automáticamente al desconectar
        await base.OnDisconnectedAsync(exception);
    }

    // Método que el cliente puede llamar para confirmar recepción
    public async Task AcknowledgeNotification(string notificationId)
    {
        var tenantId = Context.User!.FindFirstValue("tenant_id")!;
        var userId   = Context.User!.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
        // Marcar la notificación como leída en la DB
        // _notificationRepo.MarkAsReadAsync(notificationId, userId, tenantId);
    }

    // Helpers para construir nombres de grupos
    public static string TenantGroup(string tenantId) => $"tenant:{tenantId}";
    public static string BranchGroup(string tenantId, string branchId) => $"tenant:{tenantId}:branch:{branchId}";
    public static string UserGroup(string tenantId, string userId) => $"tenant:{tenantId}:user:{userId}";
}
```

---

## Registrar SignalR

```csharp
// Host.Api/Program.cs
builder.Services.AddSignalR();

// ...

app.MapHub<NotificationsHub>("/hubs/notifications");
```

---

## INotificationService: enviar notificaciones desde el backend

```csharp
// Common/Notifications/INotificationService.cs
public interface INotificationService
{
    // Notificar a todos los usuarios del tenant
    Task NotifyTenantAsync(long tenantId, string eventType, object payload, CancellationToken ct = default);

    // Notificar a todos los usuarios de un branch
    Task NotifyBranchAsync(long tenantId, long branchId, string eventType, object payload, CancellationToken ct = default);

    // Notificar a un usuario específico
    Task NotifyUserAsync(long tenantId, Guid userPublicId, string eventType, object payload, CancellationToken ct = default);
}
```

```csharp
// Infrastructure/Notifications/SignalRNotificationService.cs
public sealed class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationsHub> _hub;

    public SignalRNotificationService(IHubContext<NotificationsHub> hub)
        => _hub = hub;

    public async Task NotifyTenantAsync(
        long tenantId, string eventType, object payload, CancellationToken ct)
    {
        var group   = NotificationsHub.TenantGroup(tenantId.ToString());
        var message = new NotificationMessage(eventType, payload, DateTime.UtcNow);
        await _hub.Clients.Group(group).SendAsync("notification", message, ct);
    }

    public async Task NotifyBranchAsync(
        long tenantId, long branchId, string eventType, object payload, CancellationToken ct)
    {
        var group   = NotificationsHub.BranchGroup(tenantId.ToString(), branchId.ToString());
        var message = new NotificationMessage(eventType, payload, DateTime.UtcNow);
        await _hub.Clients.Group(group).SendAsync("notification", message, ct);
    }

    public async Task NotifyUserAsync(
        long tenantId, Guid userPublicId, string eventType, object payload, CancellationToken ct)
    {
        var group   = NotificationsHub.UserGroup(tenantId.ToString(), userPublicId.ToString());
        var message = new NotificationMessage(eventType, payload, DateTime.UtcNow);
        await _hub.Clients.Group(group).SendAsync("notification", message, ct);
    }
}

public sealed record NotificationMessage(
    string   EventType,
    object   Payload,
    DateTime OccurredAt);
```

---

## Tipos de eventos de notificación

```csharp
public static class NotificationEvents
{
    // Jobs y procesos asincrónicos
    public const string ReportReady         = "report.ready";
    public const string ExportReady         = "export.ready";
    public const string ImportCompleted     = "import.completed";
    public const string ImportFailed        = "import.failed";

    // Operaciones del tenant
    public const string UserInvited         = "user.invited";
    public const string UserJoined          = "user.joined";
    public const string LimitWarning        = "limit.warning";
    public const string LimitReached        = "limit.reached";

    // Pagos y facturación
    public const string PaymentFailed       = "payment.failed";
    public const string SubscriptionUpdated = "subscription.updated";

    // Datos en tiempo real
    public const string OrderStatusChanged  = "order.status_changed";
    public const string DashboardUpdated    = "dashboard.updated";
}
```

---

## Notificar desde un handler

```csharp
// Reports.Application/UseCases/GenerateReport/GenerateReportHandler.cs
public async Task<GenerateReportResponse> Handle(
    GenerateReportRequest request, CancellationToken ct)
{
    // ... generar el reporte (proceso largo) ...
    var downloadUrl = await _storage.GetPresignedDownloadUrlAsync(reportKey, TimeSpan.FromHours(1), ct);

    // Notificar al usuario que lo solicitó — no a todo el tenant
    await _notifications.NotifyUserAsync(
        request.TenantId,
        request.RequestedByPublicId,
        NotificationEvents.ReportReady,
        new
        {
            reportName  = request.ReportName,
            downloadUrl = downloadUrl,
            expiresAt   = DateTime.UtcNow.AddHours(1)
        },
        ct);

    return new GenerateReportSuccess(reportKey);
}
```

```csharp
// Orders.Application/UseCases/UpdateOrderStatus/UpdateOrderStatusHandler.cs
public async Task<UpdateOrderStatusResponse> Handle(
    UpdateOrderStatusRequest request, CancellationToken ct)
{
    await _orders.UpdateStatusAsync(request.OrderId, request.NewStatus, request.TenantId, ct);

    // Notificar a todos los del branch (dashboard operativo en tiempo real)
    if (request.BranchId.HasValue)
        await _notifications.NotifyBranchAsync(
            request.TenantId,
            request.BranchId.Value,
            NotificationEvents.OrderStatusChanged,
            new { orderId = request.OrderId, status = request.NewStatus },
            ct);

    return new UpdateOrderStatusSuccess();
}
```

---

## Persistir notificaciones (inbox)

Para notificaciones que el usuario puede ver aunque no esté conectado:

```csharp
// Notifications.Domain/Entities/Notification.cs
public sealed class Notification
{
    public long     Id              { get; init; }
    public Guid     PublicId        { get; init; }
    public long     TenantId        { get; init; }
    public long?    BranchId        { get; init; }
    public Guid?    UserPublicId    { get; init; }   // null = todos los usuarios del tenant/branch
    public string   EventType       { get; init; } = string.Empty;
    public string   PayloadJson     { get; init; } = string.Empty;
    public bool     IsRead          { get; init; }
    public DateTime? ReadAtUtc      { get; init; }
    public DateTime CreatedAtUtc    { get; init; }
}
```

```csharp
// NotificationService — guardar Y enviar en tiempo real
public async Task NotifyUserAsync(
    long tenantId, Guid userPublicId, string eventType, object payload, CancellationToken ct)
{
    // 1. Persistir en DB para el inbox
    await _notificationRepo.InsertAsync(new Notification
    {
        TenantId      = tenantId,
        UserPublicId  = userPublicId,
        EventType     = eventType,
        PayloadJson   = JsonSerializer.Serialize(payload)
    }, ct);

    // 2. Enviar en tiempo real si el usuario está conectado
    var group   = NotificationsHub.UserGroup(tenantId.ToString(), userPublicId.ToString());
    var message = new NotificationMessage(eventType, payload, DateTime.UtcNow);
    await _hub.Clients.Group(group).SendAsync("notification", message, ct);
}
```

---

## Endpoint para obtener notificaciones no leídas (al cargar la app)

```csharp
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : BaseApiController
{
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnread(CancellationToken ct)
    {
        var userPublicId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        _ = await Mediator.Send(new GetUnreadNotificationsRequest(
            CurrentTenantId, CurrentBranchId, userPublicId), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct) { ... }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct) { ... }
}
```

---

## Conexión desde el frontend (JavaScript/TypeScript)

```typescript
// frontend/src/lib/notifications.ts
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications", {
        accessTokenFactory: () => localStorage.getItem("access_token") ?? ""
    })
    .withAutomaticReconnect([0, 1000, 5000, 10000, 30000])  // backoff de reconexión
    .configureLogging(signalR.LogLevel.Warning)
    .build();

connection.on("notification", (message: NotificationMessage) => {
    console.log("Notificación recibida:", message.eventType, message.payload);
    // Actualizar el estado de la app según el tipo de evento
    handleNotification(message);
});

await connection.start();
```

---

## SignalR con múltiples instancias (scale-out)

Con un solo servidor, SignalR funciona out of the box. Con múltiples instancias (horizontal scaling), los clientes conectados a diferentes instancias no reciben los mensajes entre sí. Solución: Redis Backplane:

```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.StackExchangeRedis" Version="9.*" />
```

```csharp
// Host.Api/Program.cs
builder.Services.AddSignalR()
    .AddStackExchangeRedis(
        redisConnectionString,
        options => options.Configuration.ChannelPrefix = RedisChannel.Literal("misaas"));
```

Con Redis Backplane, un mensaje enviado en la instancia A llega a los clientes conectados a la instancia B.

---

## Checklist

- [ ] Hub con `[Authorize]`: solo usuarios autenticados pueden conectarse
- [ ] `OnConnectedAsync` une al cliente a los grupos correctos: tenant + branch + user
- [ ] El nombre del grupo siempre incluye `tenant_id`: nunca grupos sin tenant
- [ ] `INotificationService` abstrae el `IHubContext<>`: los handlers no referencian SignalR directamente
- [ ] Notificaciones persistidas en DB para el inbox (no solo tiempo real)
- [ ] Redis Backplane configurado para múltiples instancias
- [ ] Frontend con reconexión automática (`withAutomaticReconnect`)
- [ ] Limpiar notificaciones antiguas con un background job periódico

---

## Glosario

| Término | Definición |
|---------|-----------|
| SignalR | Librería de ASP.NET Core para comunicación en tiempo real — WebSockets con fallback a SSE y Long Polling |
| NotificationsHub | Hub de SignalR que gestiona conexiones, grupos y envío de notificaciones en el back-template |
| Groups | Mecanismo de SignalR para enviar mensajes a subconjuntos de conexiones — base del aislamiento por tenant |
| TenantGroup | Grupo de SignalR nombrado `tenant:{tenantId}` que aísla las notificaciones de un tenant |
| BranchGroup | Grupo de SignalR nombrado `branch:{tenantId}:{branchId}` para notificaciones a una sucursal específica |
| UserGroup | Grupo de SignalR nombrado `user:{userId}` para notificaciones privadas a un usuario específico |
| INotificationService | Interfaz del back-template que abstrae el IHubContext — los handlers usan esta interfaz, no SignalR directamente |
| IHubContext | Interfaz de ASP.NET Core para enviar mensajes a grupos y conexiones desde fuera del Hub |
| Redis Backplane | Coordinador de mensajes SignalR entre múltiples instancias del servidor — necesario para escalar horizontalmente |
| withAutomaticReconnect | Método del cliente JavaScript de SignalR para reconexión automática con backoff |
| OnConnectedAsync | Método del Hub que se ejecuta cuando el cliente conecta — aquí se une a los grupos correctos |
| Inbox | Notificaciones persistidas en base de datos para que el usuario las vea aunque estuviera desconectado |

---

*Rogelio Arriaga Gonzalez*
