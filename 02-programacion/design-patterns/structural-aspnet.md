# Patrones Estructurales — ASP.NET Core (Ferreira Ch.11)

Los patrones estructurales del GoF resuelven cómo organizar jerarquías de objetos de forma mantenible: cómo añadir comportamientos dinámicamente, cómo gestionar estructuras complejas de objetos, y cómo conectar interfaces incompatibles. En un stack ASP.NET Core con Clean Architecture, estos patrones aparecen en capas clave: registro de dependencias en el composition root, pipelines de mediator, organización de módulos y simplificación del acceso a subsistemas de infraestructura.

> Fuente: *Architecting ASP.NET Core Applications* (Ferreira) — Ch.11 Structural Patterns

---

## Decorator

### El problema

Tienes una clase `ExampleUserRepository` y necesitas añadirle caché, logging de timing y circuit-breaking — pero solo en algunos contextos. Con herencia se produce explosión combinatoria estática. El código que consume el repositorio no debería saber que está decorado.

```csharp
// ❌ Con herencia — estático y combinatorio
class CachedLoggingExampleUserRepository : ExampleUserRepository { /* combina N comportamientos */ }
// No puedes activar/desactivar en runtime
// Cada combinación nueva requiere una clase nueva
```

```csharp
// ✓ Con Decorator — dinámico y componible en el composition root
public interface IExampleUserRepository
{
    Task<ExampleUserDto?> GetByPublicIdAsync(Guid publicId, CancellationToken ct);
}

public class ExampleUserRepository : IExampleUserRepository
{
    private readonly IDbConnection _db;
    public ExampleUserRepository(IDbConnection db) { _db = db; }

    public async Task<ExampleUserDto?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
    {
        // implementación Dapper real
        return await _db.QuerySingleOrDefaultAsync<ExampleUserDto>(
            ExampleUsersSql.GetByPublicId, new { PublicId = publicId });
    }
}

// Decorator: añade caché en memoria sin tocar la implementación base
public class CachedExampleUserRepository : IExampleUserRepository
{
    private readonly IExampleUserRepository _inner;
    private readonly IMemoryCache _cache;

    public CachedExampleUserRepository(IExampleUserRepository inner, IMemoryCache cache)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache;
    }

    public async Task<ExampleUserDto?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
    {
        var key = $"example-user:{publicId}";
        if (_cache.TryGetValue(key, out ExampleUserDto? cached))
            return cached;

        var result = await _inner.GetByPublicIdAsync(publicId, ct);
        if (result is not null)
            _cache.Set(key, result, TimeSpan.FromMinutes(5));

        return result;
    }
}

// Decorator: añade logging de timing sin tocar caché ni repositorio base
public class TimedExampleUserRepository : IExampleUserRepository
{
    private readonly IExampleUserRepository _inner;
    private readonly ILogger<TimedExampleUserRepository> _logger;

    public TimedExampleUserRepository(IExampleUserRepository inner,
        ILogger<TimedExampleUserRepository> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger;
    }

    public async Task<ExampleUserDto?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await _inner.GetByPublicIdAsync(publicId, ct);
        _logger.LogInformation("GetByPublicId {PublicId} took {Ms}ms", publicId, sw.ElapsedMilliseconds);
        return result;
    }
}
```

### Registro con Scrutor

Sin Scrutor el composition root se complica con `new` anidados:

```csharp
// ❌ Sin Scrutor — verboso y gestión manual de lifetimes
builder.Services.AddScoped<IExampleUserRepository>(sp =>
    new TimedExampleUserRepository(
        new CachedExampleUserRepository(
            new ExampleUserRepository(sp.GetRequiredService<IDbConnection>()),
            sp.GetRequiredService<IMemoryCache>()),
        sp.GetRequiredService<ILogger<TimedExampleUserRepository>>()));
```

Con Scrutor el composition root queda declarativo:

```csharp
// ✓ Con Scrutor — el contenedor gestiona los lifetimes y las dependencias transitivas
// dotnet add package Scrutor
builder.Services
    .AddScoped<IExampleUserRepository, ExampleUserRepository>()
    .Decorate<IExampleUserRepository, CachedExampleUserRepository>()
    .Decorate<IExampleUserRepository, TimedExampleUserRepository>();
// El contenedor resuelve: TimedExampleUserRepository(CachedExampleUserRepository(ExampleUserRepository(...)))
```

### Principios SOLID que refuerza

| Principio | Relación |
|-----------|----------|
| S — Single Responsibility | Cada decorator encapsula un único cross-cutting concern (caché, timing, retry...) |
| O — Open/Closed | Se añaden comportamientos sin modificar la clase base |
| I — Interface Segregation | Interfaces pequeñas son fáciles de decorar; si cuesta decorar, la interfaz es demasiado grande |
| D — Dependency Inversion | El poder del Decorator reside en depender de la abstracción, no de la implementación |

---

## Composite

### El problema

Tienes una estructura jerárquica (tenants → sucursales → departamentos → usuarios). El código consumidor necesita tratar un único usuario y una sucursal con 500 usuarios de la misma forma: calcular totales, serializar, aplicar permisos. Sin Composite, el consumidor necesita saber si maneja un leaf o una colección.

```csharp
// ❌ Sin Composite — el consumidor decide según el tipo concreto
int count = item is SingleUser u ? 1
          : item is BranchGroup g ? g.Users.Sum(x => x.Count)
          : 0; // no se puede extender sin tocar este código
```

```csharp
// ✓ Con Composite — el consumidor trata todo como IComponent
public interface IComponent
{
    int Count { get; }
    string Type { get; }
}

// Leaf: un usuario individual
public class ExampleUser : IComponent
{
    public ExampleUser(string fullName) { FullName = fullName; }
    public string FullName { get; }
    public string Type => "ExampleUser";
    public int Count => 1; // siempre es 1 — es un nodo hoja
}

// Composite base: gestiona hijos de forma automática
public abstract class ExampleUserComposite : IComponent
{
    protected readonly List<IComponent> _children = new();
    public string Name { get; }

    protected ExampleUserComposite(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public virtual string Type => GetType().Name;
    public virtual int Count => _children.Sum(c => c.Count); // recursivo — delega a los hijos

    public virtual IEnumerable<IComponent> Children =>
        _children.AsReadOnly();

    public virtual void Add(IComponent component) => _children.Add(component);
    public virtual void Remove(IComponent component) => _children.Remove(component);
}

// Composites concretos — cada uno añade su contexto propio
public class Tenant : ExampleUserComposite
{
    public Tenant(string name, string subdomain) : base(name)
    {
        Subdomain = subdomain;
    }
    public string Subdomain { get; }
}

public class Branch : ExampleUserComposite
{
    public Branch(string name, string location) : base(name)
    {
        Location = location;
    }
    public string Location { get; }
}

public class Department : ExampleUserComposite
{
    public Department(string name) : base(name) { }
}

// Uso — el consumidor solo conoce IComponent
var acme = new Tenant("Acme Corp", "acme");
var hq   = new Branch("Headquarters", "CDMX");
var eng  = new Department("Engineering");

eng.Add(new ExampleUser("Alice"));
eng.Add(new ExampleUser("Bob"));
hq.Add(eng);
hq.Add(new ExampleUser("Manager"));
acme.Add(hq);

// Una sola llamada — el árbol se gestiona solo
Console.WriteLine(acme.Count); // 3 (Alice + Bob + Manager)
```

### Principios SOLID que refuerza

| Principio | Relación |
|-----------|----------|
| S — Single Responsibility | Cada nodo gestiona su propia lógica; el consumidor no coordina la jerarquía |
| O — Open/Closed | Se añaden nuevos tipos de nodo implementando `IComponent` sin tocar los existentes |
| D — Dependency Inversion | Todos los actores dependen de `IComponent`, no de implementaciones concretas |

---

## Adapter

### El problema

Quieres reutilizar una librería externa (o un SDK de terceros) que tiene su propia interfaz, incompatible con la que usa tu dominio. No puedes modificar la librería. Refactorizar todos los consumidores para usar la API externa directamente acopla tu dominio a un proveedor externo.

```csharp
// ❌ Sin Adapter — el handler conoce la API del proveedor externo directamente
public class GetExampleUserHandler : IRequestHandler<GetExampleUserRequest, IResponse>
{
    private readonly ExternalUserSdkClient _sdk; // dependencia directa del SDK

    public async Task<IResponse> Handle(GetExampleUserRequest request, CancellationToken ct)
    {
        // si el SDK cambia de versión, hay que cambiar aquí y en todos los demás handlers
        var result = await _sdk.FetchUserByExternalId(request.PublicId.ToString());
        return new GetExampleUserSuccess(new ExampleUserDto(result.Uid, result.Name, result.Email, result.Active));
    }
}
```

```csharp
// ✓ Con Adapter — el dominio depende de su propia abstracción

// Interfaz de destino: definida por el dominio
public interface IExampleUserRepository
{
    Task<ExampleUserDto?> GetByPublicIdAsync(Guid publicId, CancellationToken ct);
}

// Adaptee: clase externa que no podemos modificar
public class ExternalUserSdkClient
{
    public Task<ExternalUserResult> FetchUserByExternalId(string id) { /* ... */ return null!; }
}

// Adapter: adapta ExternalUserSdkClient → IExampleUserRepository
public class ExternalUserSdkAdapter : IExampleUserRepository
{
    private readonly ExternalUserSdkClient _adaptee;

    public ExternalUserSdkAdapter(ExternalUserSdkClient adaptee)
    {
        _adaptee = adaptee ?? throw new ArgumentNullException(nameof(adaptee));
    }

    public async Task<ExampleUserDto?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
    {
        var result = await _adaptee.FetchUserByExternalId(publicId.ToString());
        if (result is null) return null;
        return new ExampleUserDto(Guid.Parse(result.Uid), result.Name, result.Email, result.Active);
    }
}

// Handler: solo conoce IExampleUserRepository — desacoplado del SDK
public class GetExampleUserHandler : IRequestHandler<GetExampleUserRequest, IResponse>
{
    private readonly IExampleUserRepository _repo;

    public GetExampleUserHandler(IExampleUserRepository repo) { _repo = repo; }

    public async Task<IResponse> Handle(GetExampleUserRequest request, CancellationToken ct)
    {
        var dto = await _repo.GetByPublicIdAsync(request.PublicId, ct);
        if (dto is null)
            return new GetExampleUserNotFoundFailure($"Usuario {request.PublicId} no encontrado.");
        return new GetExampleUserSuccess(dto);
    }
}

// Composition root: registrar el adapter como implementación de IExampleUserRepository
builder.Services.AddSingleton<ExternalUserSdkClient>();
builder.Services.AddScoped<IExampleUserRepository, ExternalUserSdkAdapter>();
```

### Adapter por herencia vs composición

| Variante | Cuándo usarla |
|----------|---------------|
| Composición (recomendado) | Siempre que sea posible — bajo acoplamiento, fácil de testear |
| Herencia | Solo si necesitas acceder a miembros `protected` del Adaptee |

### Principios SOLID que refuerza

| Principio | Relación |
|-----------|----------|
| S — Single Responsibility | El Adapter tiene una sola responsabilidad: traducir una interfaz a otra |
| O — Open/Closed | El Adaptee no se modifica; el Adapter añade compatibilidad desde afuera |
| D — Dependency Inversion | El consumidor depende de la interfaz de dominio, no del Adaptee concreto |

---

## Façade

### El problema

Un handler necesita coordinar tres servicios de infraestructura (inventario, procesamiento de órdenes, envío) para completar una operación de negocio. Sin Facade, el handler conoce los tres servicios y el orden exacto de invocación — alto acoplamiento y difícil de testear.

```csharp
// ❌ Sin Facade — el handler orquesta N subsistemas directamente
public async Task<IResponse> Handle(PlaceOrderRequest request, CancellationToken ct)
{
    var inStock = await _inventoryService.CheckStockAsync(request.ProductId, request.Quantity);
    if (!inStock) return new PlaceOrderFailure("Stock insuficiente.");

    var orderId = await _orderProcessingService.CreateOrderAsync(request.ProductId, request.Quantity);
    await _shippingService.ScheduleShippingAsync(orderId);
    // si se añade un 4to subsistema, hay que modificar este handler
    return new PlaceOrderSuccess(orderId);
}
```

```csharp
// ✓ Con Facade — el handler delega a una interfaz simple

// Interfaz de la Facade
public interface IOrderFacade
{
    Task<string> PlaceOrderAsync(string productId, int quantity, CancellationToken ct);
    Task<string> GetOrderStatusAsync(int orderId, CancellationToken ct);
}

// Subsistemas internos (pueden ser internal en una class library opaca)
internal class InventoryService
{
    public Task<bool> CheckStockAsync(string productId, int quantity, CancellationToken ct)
        => Task.FromResult(true); // simplificado
}

internal class OrderProcessingService
{
    public Task<int> CreateOrderAsync(string productId, int quantity, CancellationToken ct)
        => Task.FromResult(123); // mock order ID
    public Task<string> GetOrderStatusAsync(int orderId, CancellationToken ct)
        => Task.FromResult("Order Shipped");
}

internal class ShippingService
{
    public Task ScheduleShippingAsync(int orderId, CancellationToken ct)
        => Task.CompletedTask;
}

// Implementación de la Facade: coordina los subsistemas
public class OrderFacade : IOrderFacade
{
    private readonly InventoryService _inventory;
    private readonly OrderProcessingService _orders;
    private readonly ShippingService _shipping;

    internal OrderFacade(InventoryService inventory, OrderProcessingService orders,
        ShippingService shipping)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _orders    = orders    ?? throw new ArgumentNullException(nameof(orders));
        _shipping  = shipping  ?? throw new ArgumentNullException(nameof(shipping));
    }

    public async Task<string> PlaceOrderAsync(string productId, int quantity, CancellationToken ct)
    {
        if (!await _inventory.CheckStockAsync(productId, quantity, ct))
            return "Order failed due to insufficient stock.";

        var orderId = await _orders.CreateOrderAsync(productId, quantity, ct);
        await _shipping.ScheduleShippingAsync(orderId, ct);
        return $"Order {orderId} placed successfully.";
    }

    public async Task<string> GetOrderStatusAsync(int orderId, CancellationToken ct)
        => await _orders.GetOrderStatusAsync(orderId, ct);
}

// Extension method del subsistema (opaque facade): oculta los new internos
public static class OrderSubsystemExtensions
{
    public static IServiceCollection AddOrderSubsystem(this IServiceCollection services)
    {
        services.AddSingleton<IOrderFacade>(_ =>
            new OrderFacade(new InventoryService(), new OrderProcessingService(), new ShippingService()));
        return services;
    }
}

// Handler: solo conoce IOrderFacade — un método, una responsabilidad
public class PlaceOrderHandler : IRequestHandler<PlaceOrderRequest, IResponse>
{
    private readonly IOrderFacade _facade;
    public PlaceOrderHandler(IOrderFacade facade) { _facade = facade; }

    public async Task<IResponse> Handle(PlaceOrderRequest request, CancellationToken ct)
    {
        var result = await _facade.PlaceOrderAsync(request.ProductId, request.Quantity, ct);
        return new PlaceOrderSuccess(result);
    }
}
```

### Facade Transparente (para DI flexible)

La facade opaca usa `internal` y extension method con `new`. La facade transparente expone interfaces públicas para permitir que el consumidor sobreescriba cualquier subsistema:

```csharp
// Subsistemas con interfaces públicas — el consumidor puede reemplazar cualquiera
public interface IInventoryService { Task<bool> CheckStockAsync(string productId, int quantity, CancellationToken ct); }
public interface IOrderProcessingService { Task<int> CreateOrderAsync(string productId, int quantity, CancellationToken ct); Task<string> GetOrderStatusAsync(int orderId, CancellationToken ct); }
public interface IShippingService { Task ScheduleShippingAsync(int orderId, CancellationToken ct); }

// Extension method transparente: usa TryAdd para permitir overrides antes de la llamada
public static IServiceCollection AddTransparentOrderSubsystem(this IServiceCollection services)
{
    services.TryAddSingleton<IInventoryService, InventoryService>();
    services.TryAddSingleton<IOrderProcessingService, OrderProcessingService>();
    services.TryAddSingleton<IShippingService, ShippingService>();
    services.TryAddSingleton<IOrderFacade, OrderFacade>();
    return services;
}

// Sobrescribir un subsistema registrando ANTES del AddTransparentOrderSubsystem:
builder.Services
    .AddSingleton<IInventoryService, UpdatedInventoryService>() // override: siempre retorna false
    .AddTransparentOrderSubsystem(); // TryAdd no sobreescribe si ya existe el binding
```

### Comparación Opaque vs Transparent

| Aspecto | Opaque Facade | Transparent Facade |
|---------|---------------|-------------------|
| Visibilidad subsistemas | `internal` | `public` con interfaces |
| Extensibilidad desde el consumidor | ✗ | ✓ |
| Control de acceso | Alto | Medio |
| Registro DI en subsistema | `new` manual (constructor `internal`) | `TryAdd` normal |
| Uso recomendado | Librería sin extensión planeada | Módulo extendible en monolito |

### Principios SOLID que refuerza

| Principio | Opaque | Transparent |
|-----------|--------|-------------|
| S — Single Responsibility | ✓ Facade cohesiva | ✓ Facade cohesiva |
| O — Open/Closed | Limitado (oculta subsistema) | ✓ Subsistema extendible sin modificar |
| D — Dependency Inversion | Facade sí; subsistemas no | ✓ Todo depende de abstracciones |

---

## Relación con el back-template

| Patrón | Dónde aparece en el stack |
|--------|---------------------------|
| Decorator | Pipeline behaviors del mediator (`LoggingBehavior`, `ValidationBehavior`) envuelven el `Handler` — Decorator puro. `[Authorize]` en controllers es un Decorator declarativo. |
| Decorator + Scrutor | `builder.Services.Decorate<IExampleUserRepository, CachedExampleUserRepository>()` en `InfrastructureServiceCollectionEx` — añade caché sin tocar la implementación base. |
| Composite | Estructuras de jerarquía multi-tenant: Tenant → Branch → Department → User. Cada nodo gestiona su `Count` y serialización. |
| Adapter | `ExternalUserSdkAdapter : IExampleUserRepository` cuando se integra con un SDK externo (Keycloak, AWS Cognito, proveedores de identidad). El Handler solo ve `IExampleUserRepository`. |
| Facade (Opaque) | Class libraries de módulos en Monolito Modular: `AddQualityModule()` registra todos los servicios internos sin exponerlos. El composition root queda limpio. |
| Facade (Transparent) | `AddTransparentOrderSubsystem()` con `TryAddSingleton` cuando se necesita que el proyecto consumidor pueda reemplazar un subsistema en pruebas de integración. |

---

## Cuándo usar / no usar

| Patrón | Usar | No usar |
|--------|------|---------|
| Decorator | Cross-cutting concerns (caché, logging, retry, validación) sobre una interfaz ya existente | Cuando el comportamiento adicional aplica siempre a todas las instancias — mejor incluirlo en la clase base |
| Decorator | Quieres habilitar/deshabilitar comportamientos en runtime según configuración | Cuando la interfaz es demasiado grande — indicador de que hay que segregarla primero (ISP) |
| Composite | Jerarquías de datos donde leaf y colección deben ser indistinguibles para el consumidor | Cuando la estructura es plana y no recursiva — Composite añade complejidad innecesaria |
| Composite | Menús multi-nivel, árbol de permisos, estructura de archivos, org-chart de tenants | Cuando solo hay dos niveles (1 padre, N hijos) — una lista simple es suficiente |
| Adapter | Integrar SDKs externos o librerías de terceros sin acoplar el dominio a ellas | Cuando puedes modificar directamente la interfaz del Adaptee — refactoriza en lugar de adaptar |
| Adapter | Migrar APIs legacy a una nueva interfaz mientras coexisten ambas versiones | Cuando Adapter y Target son casi idénticos — posible over-engineering |
| Facade (Opaque) | Class library donde quieres controlar completamente la superficie pública | En el application layer donde necesitas testear los subsistemas por separado |
| Facade (Transparent) | Módulos extendibles en monolito modular con DI flexible | Cuando el subsistema es simple y tiene una sola clase — Facade no aporta valor |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Decorator | Patrón estructural que envuelve un objeto con otro que implementa la misma interfaz, añadiendo comportamiento antes/después sin modificar el original |
| Scrutor | Librería open-source de NuGet que extiende el contenedor DI de ASP.NET Core con el método `Decorate<TInterface, TDecorator>()` |
| `Decorate<TInterface, TDecorator>()` | Método de Scrutor que re-registra el binding existente de `TInterface` como decorado por `TDecorator`, permitiendo cadenas de decoradores declarativas |
| Composite | Patrón estructural que permite tratar objetos individuales y colecciones de objetos de forma uniforme a través de una interfaz común; ideal para jerarquías árbol |
| Leaf | Nodo hoja en el Composite — objeto individual sin hijos; su `Count` siempre es 1 |
| Adapter | Patrón estructural que envuelve un Adaptee para que su interfaz sea compatible con la interfaz Target que el consumidor espera; también llamado Wrapper |
| Adaptee | Clase existente (frecuentemente externa o legacy) cuya interfaz es incompatible con la interfaz Target deseada |
| Facade | Patrón estructural que proporciona una interfaz simplificada frente a uno o más subsistemas complejos, reduciendo el acoplamiento del consumidor |
| Opaque Facade | Variante de Facade donde todos los componentes del subsistema son `internal`; solo la interfaz de la Facade es pública. Máximo control, mínima extensibilidad |
| Transparent Facade | Variante de Facade donde el subsistema expone interfaces públicas. El consumidor puede reemplazar cualquier subsistema registrando su propio binding antes del `TryAddSingleton` |
| `TryAddSingleton` | Método de `IServiceCollection` que solo registra el binding si no existe ya uno para el tipo — permite al consumidor hacer override registrando antes de la llamada al extension method |
| Composition Root | Punto único de la aplicación (normalmente `Program.cs` o `ServiceCollectionExtensions`) donde se ensamblan todas las dependencias — lugar correcto para configurar Decorators y Facades |

---

*Rogelio Arriaga Gonzalez*
