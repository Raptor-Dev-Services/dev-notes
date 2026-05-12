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

## Cuándo usar / no usar microservicios

| Usar | No usar |
|------|---------|
| Módulos con equipos independientes y ciclos de release distintos | Apps nuevas donde el dominio no está claro |
| Módulos que escalan de forma muy diferente | Equipos pequeños (<8 personas) |
| Módulos con requisitos de disponibilidad distintos | Cuando la latencia de red impacta la UX |
| Cumplimiento normativo requiere aislamiento de datos | Sin CI/CD maduro — los microservicios requieren automatización |


---

*Rogelio Arriaga Gonzalez*
