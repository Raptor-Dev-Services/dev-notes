# 03 — Domain-Driven Design (DDD)

DDD es una forma de diseñar software centrada en el dominio del negocio. No es un framework ni una librería. Es un conjunto de patrones y principios para modelar la lógica de negocio de forma que refleje el mundo real.

Este documento cubre los conceptos tácticos de DDD: los bloques de construcción que se usan dentro de un Bounded Context.

---

## Ubiquitous Language — el lenguaje compartido

El primer principio de DDD: **usar las mismas palabras que el negocio**. El código, los tests, las conversaciones con el cliente y los documentos deben usar el mismo vocabulario.

```csharp
// ❌ Vocabulario técnico — no refleja el negocio
public class UserRecord { public int status_flag { get; set; } }

// ✓ Vocabulario del dominio
public class ExampleUser
{
    public bool IsActive { get; private set; }  // "activo" es el término del negocio
}
```

Si el cliente habla de "Pedido" y el código usa "Order" en inglés, está bien, siempre que sea consistente. Lo que no funciona es que el código use "Order" en algunos lugares y "Purchase" en otros para lo mismo.

---

## Entidades (Entities)

Una entidad es un objeto que tiene **identidad**: existe como una cosa específica en el mundo, independientemente de sus atributos.

```
Dos usuarios con el mismo nombre y email son dos usuarios distintos porque tienen IDs distintos.
→ ExampleUser es una Entidad.

Dos billetes de $100 son intercambiables entre sí.
→ Un billete de $100 podría ser un Value Object.
```

```csharp
// Domain/Entities/ExampleUsers/ExampleUser.cs
public sealed class ExampleUser
{
    // Identidad — lo que hace único a este objeto
    public int  Id       { get; init; }      // PK de DB (no exponer fuera de infrastructure)
    public Guid PublicId { get; init; }      // ID público (lo que se expone por API)

    // Atributos — pueden cambiar, pero el objeto sigue siendo el mismo
    public string   FullName      { get; private set; } = string.Empty;
    public string   Email         { get; private set; } = string.Empty;
    public bool     IsActive      { get; private set; }
    public DateTime CreatedAtUtc  { get; init; }
    public DateTime UpdatedAtUtc  { get; private set; }
}
```

**Características de una Entidad:**
- Tiene identidad única (ID)
- Tiene ciclo de vida (se crea, modifica, desactiva)
- La igualdad se basa en el ID, no en los atributos
- Puede mutar: sus datos pueden cambiar pero sigue siendo la misma entidad

---

## Value Objects (Objetos de Valor)

Un Value Object es un objeto definido completamente por sus atributos. No tiene identidad propia. Dos instancias con los mismos valores son intercambiables.

```csharp
// ❌ Email como string — sin validación, fácil de pasar un email inválido
public class User
{
    public string Email { get; init; } = string.Empty;  // nadie garantiza que sea válido
}

// ✓ Email como Value Object — se valida al crearse, inmutable, autocontenido
public sealed record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El email no puede estar vacío.");
        if (!value.Contains('@'))
            throw new ArgumentException($"'{value}' no es un email válido.");

        Value = value.ToLowerInvariant().Trim();
    }

    public static implicit operator string(Email email) => email.Value;
    public override string ToString() => Value;
}

public class User
{
    public Email Email { get; init; }  // ← garantizado válido, inmutable
}

// Uso:
var email = new Email("  USUARIO@EXAMPLE.COM  ");
Console.WriteLine(email);  // "usuario@example.com"
```

**Ejemplos de Value Objects:**
- `Email`, `PhoneNumber`, `Address`
- `Money` (monto + moneda)
- `DateRange` (fecha inicio + fin con validación de rango)
- `Coordinates` (latitud + longitud)
- `Color` (r, g, b)

**Características de un Value Object:**
- Sin identidad propia: la igualdad es por valor
- Inmutable: no cambia después de crearse
- Autovalidado: nunca existe en estado inválido
- Reemplazable: en lugar de mutar, se reemplaza por un nuevo valor

```csharp
// ❌ Mutar un Value Object — no tiene sentido
address.Street = "Calle Nueva";  // si la dirección cambió, es una dirección NUEVA

// ✓ Reemplazar
user.Address = new Address("Calle Nueva", "Ciudad", "CP");
```

---

## Agregados y Aggregate Root

Un **Agregado** es un grupo de objetos relacionados que se tratan como una unidad para operaciones de datos. El **Aggregate Root** es la entidad principal del grupo. Es el único punto de entrada para modificar el agregado.

```
Pedido (Aggregate Root)
├── LineaDePedido  (entidad hija)
│   ├── ProductoId     (value object)
│   └── Precio         (value object Money)
├── DireccionEnvio     (value object)
└── EstadoPedido       (enum)
```

```csharp
// ❌ Modificar una entidad hija directamente — viola la integridad del agregado
var item = order.Items.First();
item.Quantity = 0;   // ← bypasa validaciones del Order

// ✓ Toda modificación pasa por el Aggregate Root
public sealed class Order   // ← Aggregate Root
{
    private readonly List<OrderLine> _items = new();
    public IReadOnlyCollection<OrderLine> Items => _items.AsReadOnly();

    public void AddItem(ProductId productId, int quantity, Money price)
    {
        if (quantity <= 0) throw new ArgumentException("Cantidad debe ser mayor a 0.");
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("No se puede modificar un pedido confirmado.");

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
            existing.IncrementQuantity(quantity);   // método controlado por el agregado
        else
            _items.Add(new OrderLine(productId, quantity, price));
    }

    public void Confirm()
    {
        if (!_items.Any()) throw new InvalidOperationException("El pedido está vacío.");
        Status = OrderStatus.Confirmed;
        _domainEvents.Add(new OrderConfirmedEvent(Id));  // emitir evento de dominio
    }
}
```

**Reglas de los Agregados:**
1. Solo el Aggregate Root tiene referencias externas (IDs de otros agregados, no objetos)
2. Las modificaciones de entidades hijas pasan por el root
3. Un repositorio existe por cada Aggregate Root. No hay repositorios de entidades hijas.
4. Las transacciones no cruzan límites de agregados (cada agregado es consistente en sí mismo)

---

## Invariantes de Dominio

Las invariantes son reglas que siempre deben cumplirse. El dominio es responsable de hacerlas cumplir, no la base de datos ni la UI.

```csharp
public sealed class ExampleUser
{
    private ExampleUser() { }  // constructor privado — solo el factory method puede crear instancias

    // Factory method — garantiza que el usuario nace válido
    public static ExampleUser Create(string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("El nombre es requerido.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("Email inválido.");

        return new ExampleUser
        {
            PublicId     = Guid.NewGuid(),
            FullName     = fullName.Trim(),
            Email        = email.ToLowerInvariant().Trim(),
            IsActive     = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    // Comportamiento con invariantes
    public void Deactivate()
    {
        if (!IsActive) throw new InvalidOperationException("El usuario ya está inactivo.");
        IsActive     = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateEmail(string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail) || !newEmail.Contains('@'))
            throw new ArgumentException("Email inválido.");
        Email        = newEmail.ToLowerInvariant().Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
```

**Nota sobre este proyecto:** las entidades actuales son principalmente contenedores de datos (se hidratan desde SQL). Agregar invariantes y comportamiento es el camino natural cuando la lógica de negocio se vuelve más compleja.

---

## Eventos de Dominio (Domain Events)

Un evento de dominio representa algo significativo que ocurrió en el dominio. Son hechos en pasado: "Usuario registrado", "Pedido confirmado".

```csharp
// Interfaz base para eventos de dominio
public interface IDomainEvent
{
    Guid   EventId   { get; }
    DateTime OccurredAt { get; }
}

// Evento concreto
public sealed record UserRegisteredEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid UserId,
    string Email) : IDomainEvent;

// La entidad acumula eventos para dispararlos luego
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public sealed class ExampleUser : AggregateRoot
{
    public static ExampleUser Register(string fullName, string email)
    {
        var user = new ExampleUser { /* ... */ };
        user.AddDomainEvent(new UserRegisteredEvent(
            Guid.NewGuid(), DateTime.UtcNow, user.PublicId, email));
        return user;
    }
}

// El repositorio o Handler publica los eventos después de guardar:
var user = ExampleUser.Register(cmd.FullName, cmd.Email);
await _repo.InsertAsync(user, ct);
foreach (var @event in user.DomainEvents)
    await _mediator.Publish(@event, ct);
user.ClearDomainEvents();
```

---

## Bounded Contexts

Un **Bounded Context** es el límite dentro del cual un modelo de dominio es válido y consistente. El mismo concepto puede tener representaciones diferentes en contextos distintos.

```
Contexto "Ventas":
    Cliente { Id, Nombre, LímiteCredito, HistorialCompras }

Contexto "Soporte":
    Cliente { Id, Nombre, TicketsAbiertos, NivelPrioridad }

Contexto "Facturación":
    Cliente { Id, RFC, DireccionFiscal, CuentasBancarias }
```

El mismo "Cliente" tiene propiedades distintas en cada contexto. Intentar tener una sola clase `Cliente` para todo genera un modelo inflado y acoplado.

**En este proyecto:** actualmente es un solo Bounded Context. Cuando el sistema crezca, se identifican los contextos por los módulos naturales del negocio (Usuarios, Pedidos, Inventario, Facturación) y cada uno tiene su propia capa de dominio o microservicio.

---

## DDD táctico — resumen de bloques

| Bloque | Identidad | Mutable | Ejemplo en el proyecto |
|--------|-----------|---------|----------------------|
| **Entity** | Sí (ID) | Sí | `ExampleUser` |
| **Value Object** | No | No | `Email`, `Money`, `Address` |
| **Aggregate Root** | Sí | Sí | `ExampleUser` (es el root del agregado) |
| **Domain Event** | Sí (EventId) | No | `UserRegisteredEvent` |
| **Repository** | — | — | `IExampleUserRepository` |
| **Factory** | — | — | `ExampleUser.Create(...)` |
| **Service de Dominio** | — | — | Lógica que no pertenece a ninguna entidad |

---

## Cuándo aplicar DDD (y cuándo no)

**Sí aplicar DDD cuando:**
- La lógica de negocio es compleja y cambia frecuentemente
- Hay múltiples equipos trabajando en partes distintas del sistema
- El dominio tiene reglas de negocio no triviales (estados, transiciones, invariantes)

**No sobre-aplicar DDD cuando:**
- Es una API CRUD simple: mapear tabla a DTO es suficiente
- El equipo no conoce el dominio todavía: el lenguaje ubiquitario emerge con el tiempo
- La complejidad es técnica (integraciones, infraestructura), no de negocio

**Este proyecto como punto de partida:** las entidades actuales son deliberadamente simples. Agregar Value Objects, invariantes y Domain Events cuando la lógica de negocio lo justifique. No por adelantado.

---

## Servicios de Dominio (Domain Services)
> Fuente: *.NET Microservices Architecture* — Ch.6 Tackling Business Complexity

Un Servicio de Dominio contiene **lógica de negocio que no pertenece a ninguna entidad ni Value Object específico**. Aplica cuando la operación involucra múltiples agregados o cuando la lógica no encaja naturalmente en ninguna entidad.

**Regla:** si no sabes en qué entidad poner la lógica, probablemente es un servicio de dominio.

```csharp
// ❌ ¿Dónde va la lógica de transferencia? No pertenece solo a la cuenta origen ni a la destino
public class BankAccount
{
    public void Transfer(BankAccount destination, decimal amount) { ... }  // ← acoplamiento entre agregados
}

// ✓ Servicio de dominio — coordina múltiples agregados sin pertenecer a ninguno
// Domain/Services/ITransferDomainService.cs
public interface ITransferDomainService
{
    Result Transfer(BankAccount origin, BankAccount destination, Money amount);
}

// Domain/Services/TransferDomainService.cs
public sealed class TransferDomainService : ITransferDomainService
{
    public Result Transfer(BankAccount origin, BankAccount destination, Money amount)
    {
        if (origin.Balance < amount)
            return Result.Failure("Saldo insuficiente.");

        origin.Debit(amount);
        destination.Credit(amount);

        origin.AddDomainEvent(new FundsTransferredEvent(origin.Id, destination.Id, amount));
        return Result.Success();
    }
}
```

```csharp
// Ejemplo con ExampleUser — servicio de dominio para fusionar dos usuarios duplicados
// Domain/Services/IUserMergeService.cs
public interface IUserMergeService
{
    Result<ExampleUser> Merge(ExampleUser primary, ExampleUser duplicate);
}

public sealed class UserMergeService : IUserMergeService
{
    public Result<ExampleUser> Merge(ExampleUser primary, ExampleUser duplicate)
    {
        if (primary.Id == duplicate.Id)
            return Result<ExampleUser>.Failure("No se puede fusionar un usuario consigo mismo.");

        // Regla de negocio: el usuario primario hereda los roles del duplicado
        foreach (var role in duplicate.Roles)
            primary.AddRole(role);

        duplicate.Deactivate("fusionado con usuario " + primary.PublicId);
        primary.AddDomainEvent(new UsersMergedEvent(primary.PublicId, duplicate.PublicId));

        return Result<ExampleUser>.Success(primary);
    }
}
```

**Cuándo crear un Servicio de Dominio:**
- La lógica involucra dos o más agregados distintos
- La operación es significativa para el negocio pero no "pertenece" a ninguna entidad
- Calcular un precio final con reglas de descuento complejas que no viven en `Order` ni en `Product`

**Cuándo NO crear un Servicio de Dominio:**
- La lógica es técnica (acceso a DB, HTTP, email): va en Application o Infrastructure
- La lógica pertenece claramente a una entidad: ponla como método de esa entidad

---

## Patrones de integración entre Bounded Contexts
> Fuente: *.NET Microservices Architecture* — Ch.4 Strategic DDD; *Domain-Driven Design* (Evans) Ch.14

Cuando dos Bounded Contexts necesitan comunicarse, el patrón que uses determina el nivel de acoplamiento.

### 1. Anti-Corruption Layer (ACL) — capa anticorrupción

Traduce el modelo externo al modelo propio. Úsalo cuando integras con un sistema legado o un contexto externo con un modelo diferente.

```csharp
// El contexto externo (ERP legado) tiene su propio modelo de "cliente"
// ErpClient { cliente_id, nom_cliente, dir_fiscal, rfc_num }

// ❌ Sin ACL — el modelo externo contamina el dominio propio
public async Task SyncCustomer(ErpClient erpClient)
{
    var user = new ExampleUser { FullName = erpClient.nom_cliente, ... };  // ← acoplamiento directo
}

// ✓ Con ACL — traducción en la capa de infraestructura
// Infrastructure/ExternalServices/Erp/ErpCustomerTranslator.cs
public sealed class ErpCustomerTranslator
{
    public ExampleUser ToDomain(ErpClient erpClient)
    {
        return ExampleUser.Create(
            fullName: erpClient.nom_cliente?.Trim() ?? throw new InvalidOperationException("Nombre requerido"),
            email:    erpClient.email_contacto ?? $"sin-email-{erpClient.cliente_id}@erp.local"
        );
    }
}

// Application/UseCases/SyncErpCustomer/SyncErpCustomerHandler.cs
public sealed class SyncErpCustomerHandler
{
    private readonly IErpClient _erp;
    private readonly ErpCustomerTranslator _translator;
    private readonly IExampleUserRepository _repo;

    public async Task<Result> Handle(SyncErpCustomerCommand cmd, CancellationToken ct)
    {
        var erpCustomer = await _erp.GetCustomerAsync(cmd.ErpId);
        var user = _translator.ToDomain(erpCustomer);  // ← ACL en acción
        await _repo.InsertAsync(user, ct);
        return Result.Success();
    }
}
```

### 2. Shared Kernel — núcleo compartido

Dos contextos comparten un subconjunto pequeño y estable del modelo. Ambos equipos deben acordar cualquier cambio en él.

```csharp
// Shared/Domain/UserId.cs — value object compartido entre "Usuarios" y "Pedidos"
// ⚠ Modificar esto requiere coordinación entre ambos equipos
public sealed record UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public static UserId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}

// Contexto Usuarios
public sealed class ExampleUser
{
    public UserId Id { get; init; } = UserId.New();
    // ...
}

// Contexto Pedidos — puede referenciar UserId sin importar ExampleUser
public sealed class Order
{
    public UserId CustomerId { get; init; }  // ← referencia solo por ID, no por objeto
    // ...
}
```

### 3. Open Host Service / Published Language

El contexto proveedor expone una API estable (Open Host Service) con un contrato público versionado (Published Language) que los consumidores pueden usar sin acoplarse a internos.

```csharp
// Contexto Usuarios expone un contrato público versionado
// Contracts/V1/UserCreatedIntegrationEvent.cs
public sealed record UserCreatedIntegrationEvent(
    Guid   UserId,
    string FullName,
    string Email,
    DateTime OccurredAt);

// Este evento es el "Published Language" — contrato estable, versionado
// Los otros contextos se suscriben a este evento sin conocer los internos de Usuarios

// Contexto Pedidos — consumidor del evento
public sealed class UserCreatedIntegrationEventHandler : INotificationHandler<UserCreatedIntegrationEvent>
{
    private readonly ICustomerRepository _customers;

    public async Task Handle(UserCreatedIntegrationEvent evt, CancellationToken ct)
    {
        // El contexto Pedidos crea su propia proyección del usuario
        var customer = new Customer(evt.UserId, evt.FullName, evt.Email);
        await _customers.InsertAsync(customer, ct);
    }
}
```

---

## Outbox Pattern — publicación confiable de eventos
> Fuente: *.NET Microservices Architecture* — Ch.6 Implementing DDD with .NET; Ch.8 Resilience

El problema con publicar Domain Events directamente después de `SaveChangesAsync` es que si el bus falla, el evento se pierde pero la transacción ya se confirmó. El **Outbox Pattern** resuelve esto garantizando que el evento y el cambio de estado se guardan en la misma transacción.

```
Flujo sin Outbox (problema):
    1. SaveChangesAsync()   → OK
    2. Publish(event)       → ❌ bus caído → evento perdido, inconsistencia

Flujo con Outbox (solución):
    1. SaveChangesAsync()   → guarda User + OutboxMessage en la misma transacción
    2. Background worker    → lee OutboxMessage → publica al bus → marca processed=true
```

```csharp
// Domain/Outbox/OutboxMessage.cs
public sealed class OutboxMessage
{
    public Guid     Id           { get; init; } = Guid.NewGuid();
    public string   Type         { get; init; } = string.Empty;   // nombre del evento
    public string   Payload      { get; init; } = string.Empty;   // JSON del evento
    public DateTime CreatedAt    { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }

    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;
}
```

```csharp
// Infrastructure/Persistence/Interceptors/OutboxInterceptor.cs
// EF Core interceptor que convierte DomainEvents en OutboxMessages al guardar
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct)
    {
        var db = eventData.Context;
        if (db is null) return await base.SavingChangesAsync(eventData, result, ct);

        var events = db.ChangeTracker
            .Entries<AggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        var outboxMessages = events.Select(e => new OutboxMessage
        {
            Type    = e.GetType().Name,
            Payload = JsonSerializer.Serialize(e, e.GetType())
        }).ToList();

        await db.Set<OutboxMessage>().AddRangeAsync(outboxMessages, ct);

        foreach (var entry in db.ChangeTracker.Entries<AggregateRoot>())
            entry.Entity.ClearDomainEvents();

        return await base.SavingChangesAsync(eventData, result, ct);
    }
}
```

```csharp
// Infrastructure/BackgroundServices/OutboxProcessorService.cs
// Worker que lee OutboxMessages no procesados y los publica
public sealed class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OutboxProcessorService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingMessages(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessPendingMessages(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db       = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await db.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        foreach (var msg in messages)
        {
            try
            {
                var eventType = Type.GetType(msg.Type);
                if (eventType is null) continue;

                var @event = JsonSerializer.Deserialize(msg.Payload, eventType);
                if (@event is INotification notification)
                    await mediator.Publish(notification, ct);

                msg.MarkProcessed();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando outbox message {Id}", msg.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
```

```csharp
// Registro en Program.cs
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(connectionString)
        .AddInterceptors(new OutboxInterceptor()));

builder.Services.AddHostedService<OutboxProcessorService>();
```

**Cuándo usar Outbox Pattern:**
- Cuando un evento de dominio debe disparar acciones en otros servicios o contextos
- Cuando la consistencia entre la DB y el bus de mensajes es crítica
- Sistemas donde "al menos una vez" (at-least-once delivery) es aceptable

---

## Glosario

| Término | Definición |
|---------|-----------|
| DDD | Domain-Driven Design — enfoque de diseño que modela el software alrededor del lenguaje y las reglas del negocio |
| Lenguaje Ubiquitario | vocabulario compartido entre desarrolladores y expertos del dominio que se refleja directamente en el código |
| Entidad | objeto del dominio con identidad única persistente a lo largo del tiempo (identificado por un ID) |
| Value Object | objeto del dominio sin identidad propia, definido solo por sus atributos; es inmutable |
| Aggregate Root | entidad que es el punto de entrada de un agregado; garantiza las invariantes del conjunto de entidades que lo forman |
| Bounded Context | límite explícito dentro del cual un modelo de dominio es válido y consistente |
| Domain Event | hecho ocurrido en el dominio, expresado en pasado; desencadena acciones en otros agregados o contextos |
| Outbox Pattern | técnica para garantizar la publicación confiable de eventos guardando el evento y el cambio de estado en la misma transacción |
| Anti-Corruption Layer | capa de traducción que aisla el modelo propio del modelo de un sistema externo o legado |
| Shared Kernel | subconjunto pequeño y estable del modelo compartido entre dos Bounded Contexts con control conjunto de cambios |
| Servicio de Dominio | clase que contiene lógica de negocio que involucra múltiples agregados y no pertenece a ninguna entidad sola |

---

*Rogelio Arriaga Gonzalez*
