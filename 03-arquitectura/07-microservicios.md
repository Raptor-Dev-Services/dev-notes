# 07 — Microservicios con .NET

Los microservicios dividen una aplicación en servicios pequeños e independientes, cada uno con su propia base de datos y desplegable por separado. Este documento cubre los patrones tácticos para implementarlos con .NET.

---

## Monolito vs Microservicios — cuándo migrar
> Fuente: *.NET Microservices Architecture* (Cesarini et al.) — Ch.1 Introduction

```
Monolito                              Microservicios
─────────                             ──────────────
Un solo deploy                        Deploy independiente por servicio
Base de datos compartida              Una DB por servicio
Comunicación en proceso (rápida)      Comunicación por red (lenta, puede fallar)
Fácil de desarrollar al inicio        Complejo desde el principio
Difícil de escalar por componente     Escala por componente

Migrar cuando:
✓ El equipo crece y hay conflictos frecuentes en el monolito
✓ Necesitas escalar partes independientes (ej: módulo de reportes)
✓ Diferentes módulos tienen ciclos de release distintos
✓ Diferentes módulos necesitan tecnologías distintas

No migrar cuando:
✗ El equipo es pequeño (<8 personas)
✗ El dominio no está bien entendido aún
✗ Es una app nueva (empezar con monolito, migrar cuando sea necesario)
```

---

## Comunicación entre servicios

### 1. Comunicación sincrónica — HTTP/gRPC

```csharp
// Servicio de Pedidos llama al Servicio de Inventario por HTTP
// Application/Services/IInventoryServiceClient.cs
public interface IInventoryServiceClient
{
    Task<bool> HasStockAsync(Guid productId, int quantity, CancellationToken ct);
    Task<bool> ReserveStockAsync(Guid productId, int quantity, CancellationToken ct);
}

// Infrastructure/ExternalServices/InventoryServiceClient.cs
public sealed class InventoryServiceClient : IInventoryServiceClient
{
    private readonly HttpClient _http;

    public InventoryServiceClient(HttpClient http) => _http = http;

    public async Task<bool> HasStockAsync(Guid productId, int quantity, CancellationToken ct)
    {
        var response = await _http.GetFromJsonAsync<StockCheckResponse>(
            $"/api/inventory/{productId}/check?quantity={quantity}", ct);
        return response?.Available ?? false;
    }

    public async Task<bool> ReserveStockAsync(Guid productId, int quantity, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync(
            $"/api/inventory/{productId}/reserve",
            new ReserveStockRequest(productId, quantity), ct);
        return response.IsSuccessStatusCode;
    }
}

// Registro con resiliencia (Polly)
builder.Services.AddHttpClient<IInventoryServiceClient, InventoryServiceClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:Inventory:Url"]!))
    .AddStandardResilienceHandler();
```

### 2. Comunicación asincrónica — Mensajería

```csharp
// Evento de integración publicado cuando se confirma un pedido
// Contracts/V1/OrderConfirmedIntegrationEvent.cs
public sealed record OrderConfirmedIntegrationEvent(
    Guid     OrderId,
    Guid     CustomerId,
    decimal  Total,
    DateTime OccurredAt);

// Servicio de Pedidos — publica el evento
public sealed class ConfirmOrderHandler
    : IRequestHandler<ConfirmOrderCommand, ConfirmOrderResponse>
{
    private readonly IOrderRepository _repo;
    private readonly IMessageBus      _bus;

    public async Task<ConfirmOrderResponse> Handle(
        ConfirmOrderCommand cmd, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(cmd.OrderId, ct);
        order.Confirm();
        await _repo.UpdateAsync(order, ct);

        // Publicar evento de integración — el inventario y facturación lo escucharán
        await _bus.PublishAsync(new OrderConfirmedIntegrationEvent(
            order.Id, order.CustomerId, order.Total, DateTime.UtcNow), ct);

        return new ConfirmOrderSuccess(order.Id);
    }
}

// Servicio de Inventario — consume el evento
public sealed class OrderConfirmedEventHandler
    : IIntegrationEventHandler<OrderConfirmedIntegrationEvent>
{
    private readonly IInventoryRepository _inventory;

    public async Task Handle(OrderConfirmedIntegrationEvent evt, CancellationToken ct)
    {
        foreach (var item in evt.Items)
            await _inventory.DecrementStockAsync(item.ProductId, item.Quantity, ct);
    }
}
```

---

## API Gateway pattern

El API Gateway es el punto de entrada único para los clientes externos. Agrega autenticación, rate limiting y routing.

```csharp
// YARP (Yet Another Reverse Proxy) como API Gateway en .NET
// ApiGateway/Program.cs

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience  = "api-gateway";
    });

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();
app.Run();
```

```json
// appsettings.json — configuración de rutas del gateway
{
  "ReverseProxy": {
    "Routes": {
      "orders-route": {
        "ClusterId": "orders-cluster",
        "Match": { "Path": "/api/orders/{**catch-all}" }
      },
      "inventory-route": {
        "ClusterId": "inventory-cluster",
        "Match": { "Path": "/api/inventory/{**catch-all}" }
      }
    },
    "Clusters": {
      "orders-cluster": {
        "Destinations": {
          "orders-api": { "Address": "http://orders-service:8080" }
        }
      },
      "inventory-cluster": {
        "Destinations": {
          "inventory-api": { "Address": "http://inventory-service:8080" }
        }
      }
    }
  }
}
```

---

## Saga pattern — transacciones distribuidas

En microservicios no hay transacciones ACID entre servicios. El patrón Saga coordina una operación de larga duración mediante una secuencia de transacciones locales y mensajes de compensación.

```csharp
// Saga de "Crear Pedido" — orquestada
// Pasos: Reservar inventario → Procesar pago → Confirmar pedido
// Compensaciones: si pago falla → liberar inventario

public sealed class CreateOrderSaga
{
    private readonly IInventoryServiceClient _inventory;
    private readonly IPaymentServiceClient   _payment;
    private readonly IOrderRepository        _orders;
    private readonly IMessageBus             _bus;

    public async Task<Result> ExecuteAsync(CreateOrderCommand cmd, CancellationToken ct)
    {
        // Paso 1: Reservar inventario
        var reservation = await _inventory.ReserveAsync(cmd.Items, ct);
        if (!reservation.Success)
            return Result.Failure("Sin stock disponible.");

        // Paso 2: Procesar pago
        var payment = await _payment.ChargeAsync(cmd.CustomerId, cmd.Total, ct);
        if (!payment.Success)
        {
            // Compensación: liberar inventario reservado
            await _inventory.ReleaseReservationAsync(reservation.ReservationId, ct);
            return Result.Failure("Pago rechazado.");
        }

        // Paso 3: Confirmar pedido
        var order = Order.Create(cmd.CustomerId, cmd.Items, payment.TransactionId);
        await _orders.InsertAsync(order, ct);
        await _bus.PublishAsync(new OrderConfirmedIntegrationEvent(order.Id, cmd.CustomerId, cmd.Total, DateTime.UtcNow), ct);

        return Result.Success();
    }
}
```

---

## Health checks entre servicios

```csharp
// Program.cs — health checks que incluyen dependencias externas
builder.Services.AddHealthChecks()
    .AddNpgsql(connectionString, name: "database")
    .AddUrlGroup(
        new Uri(builder.Configuration["Services:Inventory:Url"] + "/health"),
        name: "inventory-service")
    .AddUrlGroup(
        new Uri(builder.Configuration["Services:Payment:Url"] + "/health"),
        name: "payment-service");

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false  // liveness check: la app responde (sin dependencias)
});
```

---

## Event Sourcing — historial inmutable de eventos
> Fuente: *Microservices Design Patterns in .NET* — Ch.6 Applying Event Sourcing Patterns

En lugar de guardar el **estado actual** de una entidad, Event Sourcing guarda la **secuencia de eventos** que llevaron a ese estado. El estado se reconstruye reproduciendo los eventos.

```
Estado tradicional (store current state):
  User { Id: 1, Email: "nuevo@test.com", IsActive: false }  ← solo el estado actual

Event Sourcing (store events):
  1. UserRegistered     { UserId: 1, Email: "original@test.com",   At: 2024-01-01 }
  2. EmailChanged       { UserId: 1, Email: "nuevo@test.com",      At: 2024-03-15 }
  3. UserDeactivated    { UserId: 1, Reason: "por solicitud",      At: 2024-06-01 }
  ← el estado actual se reconstruye reproduciendo estos 3 eventos
```

**Atributos clave de los eventos:**
- **Inmutables** — un evento es un hecho ocurrido; no se modifica
- **Únicos** — cada ocurrencia genera un evento nuevo, aunque sea el mismo tipo
- **Históricos** — siempre representan un punto en el tiempo (nombrarlos en pasado)

```csharp
// Event Store — tabla append-only para guardar eventos
public sealed class StoredEvent
{
    public Guid     Id          { get; init; } = Guid.NewGuid();
    public Guid     AggregateId { get; init; }   // ID del agregado (usuario, orden, etc.)
    public string   EventType   { get; init; } = string.Empty;
    public string   Payload     { get; init; } = string.Empty;  // JSON del evento
    public DateTime OccurredAt  { get; init; } = DateTime.UtcNow;
    public int      Version     { get; init; }   // número secuencial — para detectar conflictos
}

// Repository que usa Event Sourcing
public sealed class EventSourcedOrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    // Guardar un agregado = guardar sus eventos pendientes
    public async Task SaveAsync(Order order, CancellationToken ct)
    {
        var events = order.DomainEvents.Select(e => new StoredEvent
        {
            AggregateId = order.Id,
            EventType   = e.GetType().Name,
            Payload     = JsonSerializer.Serialize(e, e.GetType()),
            Version     = order.Version
        });

        await _db.StoredEvents.AddRangeAsync(events, ct);
        await _db.SaveChangesAsync(ct);
        order.ClearDomainEvents();
    }

    // Reconstruir un agregado = reproducir sus eventos
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var events = await _db.StoredEvents
            .Where(e => e.AggregateId == id)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(ct);

        if (!events.Any()) return null;

        var order = new Order();
        foreach (var stored in events)
        {
            var eventType = Type.GetType(stored.EventType);
            if (eventType is null) continue;
            var @event = JsonSerializer.Deserialize(stored.Payload, eventType);
            order.Apply(@event!);  // el agregado aplica cada evento a su estado
        }
        return order;
    }
}
```

**Cuándo usar Event Sourcing:**
- Sistemas donde la **auditoría completa** es un requisito (finanzas, salud, legal)
- CQRS con modelos de lectura que necesitan reconstruirse desde los eventos
- Cuando se necesita "viaje en el tiempo" (ver el estado en una fecha pasada)

**Cuándo NO usar Event Sourcing:**
- CRUDs simples donde la historia no importa — agrega complejidad sin beneficio
- Cuando el volumen de eventos crece tanto que la reproducción se vuelve lenta sin snapshots
- Sin CQRS — Event Sourcing sin un read model separado genera queries lentas

**Snapshot pattern** — cuando hay muchos eventos, guardar un snapshot del estado cada N eventos para no reproducir desde el principio:

```
Evento 1 → Evento 50 → Snapshot v50 → Evento 51 → Evento 100 → Snapshot v100
                                    ↑
                         Para reconstruir desde v100: leer snapshot + eventos 51-100
```

---

## Event-Driven Architecture — tipos de eventos
> Fuente: *Architecting ASP.NET Core Applications* (Marcotte, 3rd Ed) — Ch.19 Introduction to Microservices Architecture

EDA es un paradigma donde los componentes se comunican emitiendo y consumiendo eventos en lugar de llamarse directamente. Los tres términos clave:

| Concepto | Definición |
|----------|-----------|
| **Mensaje** | pieza de datos con payload, headers e identificador — base de todo |
| **Evento** | mensaje que representa algo que ya ocurrió (en pasado) — `OrderConfirmed`, `UserRegistered` |
| **Comando** | mensaje enviado para que uno o más destinatarios ejecuten una acción — `SendWelcomeEmail` |

### Los cuatro tipos de eventos

```
Domain Event        → integra lógica dentro de la misma aplicación (SRP interno)
Application Event   → evento interno a una aplicación o grupo de microservicios del mismo equipo
Integration Event   → propaga mensajes a sistemas externos; usa un message broker
Enterprise Event    → integration event que cruza límites organizacionales (entre departamentos o empresas)
```

```csharp
// Domain event — publicado internamente con MediatR, sin message broker
public sealed record ExampleUserCreatedDomainEvent(Guid PublicId, string Email)
    : INotification;

// Integration event — publicado al message broker para otros microservicios
// Contracts/V1/ExampleUserRegisteredIntegrationEvent.cs
public sealed record ExampleUserRegisteredIntegrationEvent(
    Guid     PublicId,
    string   Email,
    string   FullName,
    DateTime OccurredAt);

// Handler que publica el domain event — dentro de la misma app
public sealed class ExampleUserCreatedDomainEventHandler
    : INotificationHandler<ExampleUserCreatedDomainEvent>
{
    private readonly IMessageBus _bus;

    public async Task Handle(ExampleUserCreatedDomainEvent evt, CancellationToken ct)
    {
        // Convierte domain event → integration event para publicar externamente
        await _bus.PublishAsync(
            new ExampleUserRegisteredIntegrationEvent(
                evt.PublicId, evt.Email, evt.FullName, DateTime.UtcNow), ct);
    }
}
```

---

## Publish-Subscribe pattern

En lugar de un queue (un mensaje → un consumidor), Pub-Sub permite que **un publicador envíe un evento a cero o más suscriptores**. El publicador no conoce a los suscriptores: fire and forget.

```
Queue:      Publisher → Message → [Queue] → 1 Consumer

Pub-Sub:    Publisher → Event → [Broker / Topic] → N Consumers (0, 1, o muchos)
```

```csharp
// ❌ Sin Pub-Sub — AuthServer conoce todos los pasos post-registro (acoplamiento)
public async Task RegisterUserAsync(RegisterUserCommand cmd, CancellationToken ct)
{
    var user = ExampleUser.Create(cmd.Email, cmd.FullName);
    await _repo.AddAsync(user, ct);

    await _emailService.SendWelcomeEmailAsync(user.Email, ct);   // acoplado
    await _imageService.ProcessAvatarAsync(user.PublicId, ct);   // acoplado
    await _mailboxService.SendOnboardingMessageAsync(user.PublicId, ct); // acoplado
}

// ✓ Con Pub-Sub — AuthServer solo publica el evento; cada servicio reacciona independientemente
public async Task RegisterUserAsync(RegisterUserCommand cmd, CancellationToken ct)
{
    var user = ExampleUser.Create(cmd.Email, cmd.FullName);
    await _repo.AddAsync(user, ct);

    // Un solo publish; los suscriptores hacen el resto en paralelo
    await _bus.PublishAsync(
        new ExampleUserRegisteredIntegrationEvent(
            user.PublicId, user.Email, user.FullName, DateTime.UtcNow), ct);
}

// Cada suscriptor es un microservicio o handler independiente
public sealed class SendWelcomeEmailOnUserRegistered
    : IIntegrationEventHandler<ExampleUserRegisteredIntegrationEvent>
{
    public async Task Handle(ExampleUserRegisteredIntegrationEvent evt, CancellationToken ct)
        => await _emailService.SendWelcomeEmailAsync(evt.Email, ct);
}
```

### Message brokers comunes

| Broker | Protocolo | Mejor para |
|--------|-----------|-----------|
| **Azure Service Bus** | AMQP | Microservicios en Azure, topics y queues gestionados |
| **Amazon SQS / SNS** | HTTP | Microservicios en AWS, integración con Lambda y ECS |
| **Apache Kafka** | Kafka | Alta velocidad, event streaming, replay de historial |
| **RabbitMQ** | AMQP | On-premise, open-source, flexible |
| **MQTT / Mosquitto** | MQTT | IoT, dispositivos con ancho de banda limitado |

### Dead Letter Queue (DLQ)

Cuando un mensaje falla repetidamente (por error de procesamiento o expiración), el broker lo mueve a una **dead letter queue** en lugar de bloquearlo o perderlo:

```
Normal Queue → falla 3 veces → Dead Letter Queue
                                    ↑
                            Aquí se diagnostica el error
                            y se decide si reencolar o descartar
```

```csharp
// Azure Service Bus — configurar DLQ
var options = new ServiceBusProcessorOptions
{
    MaxConcurrentCalls = 5,
    AutoCompleteMessages = false
};

await using var processor = client.CreateProcessor("orders-topic", "inventory-sub", options);

processor.ProcessMessageAsync += async args =>
{
    try
    {
        var evt = args.Message.Body.ToObjectFromJson<OrderConfirmedIntegrationEvent>();
        await HandleAsync(evt, args.CancellationToken);
        await args.CompleteMessageAsync(args.Message);  // éxito: eliminar del queue
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error procesando mensaje {MessageId}", args.Message.MessageId);
        await args.AbandonMessageAsync(args.Message);   // fracaso: reencolar (hasta max retries → DLQ)
    }
};
```

---

## Cuándo usar / no usar microservicios

| Usar | No usar |
|------|---------|
| Módulos con equipos independientes y ciclos de release distintos | Apps nuevas donde el dominio no está claro |
| Módulos que escalan de forma muy diferente | Equipos pequeños (<8 personas) |
| Módulos con requisitos de disponibilidad distintos | Cuando la latencia de red impacta la UX |
| Cumplimiento normativo requiere aislamiento de datos | Sin CI/CD maduro — los microservicios requieren automatización |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Microservicio | servicio pequeño e independiente con su propia BD y ciclo de deploy |
| Monolito Modular | monolito con módulos bien separados — alternativa antes de microservicios |
| EDA (Event-Driven Architecture) | paradigma donde los componentes se comunican mediante eventos, no llamadas directas |
| Mensaje | pieza de datos (payload + headers + ID) que fluye entre sistemas |
| Evento | mensaje que representa un hecho ocurrido en el pasado — `OrderConfirmed`, `UserRegistered` |
| Comando | mensaje enviado para que un destinatario ejecute una acción — `SendEmail` |
| Domain Event | evento interno de la aplicación; publicado con MediatR; no cruza límites de proceso |
| Integration Event | evento que cruza límites de proceso; publicado en un message broker |
| Application Event | evento interno a una aplicación o grupo de microservicios del mismo equipo |
| Enterprise Event | integration event que cruza límites organizacionales (entre departamentos o empresas) |
| Pub-Sub | patrón donde un publicador envía un evento a cero o más suscriptores sin conocerlos |
| Message Broker | componente central que recibe eventos de publicadores y los entrega a suscriptores |
| Topic | canal de un message broker donde los publicadores envían y los suscriptores reciben |
| Dead Letter Queue (DLQ) | queue donde el broker mueve mensajes que fallaron repetidamente, para diagnóstico |
| FIFO Queue | cola que garantiza procesamiento en orden de llegada (First In, First Out) |
| Event Sourcing | patrón que almacena el historial de eventos como fuente de verdad en lugar del estado actual |
| Eventual Consistency | consistencia de datos que se logra con un pequeño retardo — aceptable en sistemas distribuidos |
| Materialized View | modelo pre-computado que un microservicio mantiene en su propia BD a partir de eventos consumidos |
| API Gateway | punto de entrada único para los clientes externos — autentica, enruta y limita tráfico |
| Saga | patrón para transacciones distribuidas — coordina pasos locales con mensajes de compensación |
| gRPC | protocolo binario de llamada remota, más eficiente que REST para comunicación entre microservicios |
| YARP | Yet Another Reverse Proxy — librería .NET para implementar API Gateway con configuración YAML |

---

*Rogelio Arriaga Gonzalez*
