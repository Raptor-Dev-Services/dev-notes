# 46 — Patrones de Dependency Injection en ASP.NET Core

Cuando una clase crea sus propias dependencias con `new`, queda acoplada a la implementación concreta: no se puede testear en aislamiento, no se puede swappear por otra implementación, y el contenedor no puede gestionar el ciclo de vida del objeto. Dependency Injection (DI) resuelve esto moviendo la creación de dependencias al contenedor.

> Fuente: *Architecting ASP.NET Core Applications* (Carl-Hugo Marcotte, 3rd Ed) — Ch.7 Strategy/Singleton/Factory, Ch.8 Dependency Injection, Ch.11 Structural Patterns

---

## IoC — la diferencia conceptual

IoC (Inversion of Control) es el principio. DI es una forma de aplicarlo.

```
❌ Control Freak — la clase controla la creación de su dependencia
public class OrderService
{
    public void Process(Order order)
    {
        var repo = new OrderRepository();   // acoplamiento concreto
        repo.Save(order);
    }
}

✓ Dependency Injection — el contenedor inyecta la dependencia
public class OrderService(IOrderRepository repo)
{
    public void Process(Order order) => repo.Save(order);
}
```

**Regla de oro:** nunca usar `new` para crear dependencias volátiles (clases de negocio, repositorios, servicios). Reservar `new` para dependencias estables (DTOs, tipos del BCL de .NET).

- **Dependencias estables:** DTOs, tipos del BCL (`List<T>`, `StringBuilder`), clases sin estado externo — su cambio nunca rompe nada.
- **Dependencias volátiles:** repositorios, servicios HTTP, clases de negocio, acceso a BD — candidatos a inyección.

---

## Composition Root — Program.cs

Toda la configuración de DI debe concentrarse en un único lugar: la composition root.

```csharp
// ✓ Program.cs — composition root de la aplicación
var builder = WebApplication.CreateBuilder(args);

// Registrar dependencias aquí
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

var app = builder.Build();
app.Run();
```

```csharp
// ✓ Mejor: delegar el registro a extensiones por módulo (back-template)
builder.Services.AddOrdersModule(builder.Configuration);
builder.Services.AddInventoryModule(builder.Configuration);
```

---

## Lifetimes — los tres ciclos de vida

| Lifetime | Instancias por... | Cuándo usar |
|----------|-------------------|-------------|
| `Singleton` | Aplicación (1 global) | Sin estado mutable compartido, costoso de crear (HttpClient, DbContext factory) |
| `Scoped` | HTTP Request (1 por request) | La mayoría de servicios, repositorios, Unit of Work |
| `Transient` | Inyección (nueva cada vez) | Objetos ligeros con estado local; evitar para objetos costosos |

```csharp
// Ejemplos de registro
builder.Services.AddSingleton<IMainDbConnectionFactory, MainDbConnectionFactory>();
builder.Services.AddScoped<IExampleUserRepository, ExampleUserRepository>();
builder.Services.AddTransient<IEmailRenderer, HandlebarsEmailRenderer>();
```

### Captive dependency — el error más común de lifetime

Un Scoped inyectado en un Singleton queda "capturado" por el Singleton y nunca se libera, lo que puede causar datos de otro usuario filtrándose:

```csharp
// ❌ Singleton consume un Scoped → captive dependency
builder.Services.AddSingleton<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
// OrderService recibirá el primer OrderRepository creado y lo retendrá para siempre

// ✓ Si el Singleton necesita un Scoped, usar IServiceScopeFactory
public class OrderSyncJob(IServiceScopeFactory scopeFactory)
{
    public async Task RunAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        await repo.SyncAsync(ct);
    }
}
```

---

## Service Locator — el anti-pattern

El Service Locator es resolver dependencias manualmente desde `IServiceProvider` dentro de la clase. Oculta las dependencias, dificulta el testing y viola IoC.

```csharp
// ❌ Service Locator — anti-pattern
public class OrderService
{
    private readonly IServiceProvider _sp;

    public OrderService(IServiceProvider sp) { _sp = sp; }

    public void Process(Order order)
    {
        var repo = _sp.GetRequiredService<IOrderRepository>();  // oculta la dependencia
        repo.Save(order);
    }
}

// ✓ Constructor injection — la dependencia es explícita y testeable
public class OrderService(IOrderRepository repo)
{
    public void Process(Order order) => repo.Save(order);
}
```

**Excepción válida:** usar `IServiceProvider` o `IServiceScopeFactory` en el composition root, en factories, o en clases que deben resolver dependencias opcionales de forma dinámica.

---

## Decorator pattern con Scrutor

El Decorator extiende el comportamiento de una clase sin modificarla, envolviéndola con otra que implementa la misma interfaz. Ideal para logging, caching, métricas, reintentos.

```csharp
// Interfaz
public interface IExampleUserRepository
{
    Task<ExampleUser?> GetByIdAsync(Guid id, CancellationToken ct);
}

// Implementación base
public sealed class ExampleUserRepository(IDbConnection db) : IExampleUserRepository
{
    public async Task<ExampleUser?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.QueryFirstOrDefaultAsync<ExampleUser>("SELECT ... WHERE public_id = @id", new { id });
}

// Decorator de caching — añade caché sin modificar el repositorio
public sealed class CachedExampleUserRepository(
    IExampleUserRepository inner,
    IMemoryCache cache) : IExampleUserRepository
{
    public async Task<ExampleUser?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var key = $"user:{id}";
        if (cache.TryGetValue(key, out ExampleUser? cached))
            return cached;

        var user = await inner.GetByIdAsync(id, ct);

        if (user is not null)
            cache.Set(key, user, TimeSpan.FromMinutes(5));

        return user;
    }
}
```

Sin Scrutor, el registro es verboso y requiere `new` manual. Con Scrutor:

```bash
dotnet add package Scrutor
```

```csharp
// ✓ Con Scrutor — limpio y sin new manual
builder.Services.AddScoped<IExampleUserRepository, ExampleUserRepository>();
builder.Services.Decorate<IExampleUserRepository, CachedExampleUserRepository>();

// El contenedor resuelve:
// CachedExampleUserRepository(inner: ExampleUserRepository, cache: IMemoryCache)
```

Se pueden encadenar múltiples decorators:

```csharp
builder.Services.AddScoped<IExampleUserRepository, ExampleUserRepository>();
builder.Services.Decorate<IExampleUserRepository, CachedExampleUserRepository>();
builder.Services.Decorate<IExampleUserRepository, LoggedExampleUserRepository>();

// Cadena en ejecución:
// LoggedExampleUserRepository → CachedExampleUserRepository → ExampleUserRepository
```

---

## Keyed Services — múltiples implementaciones de la misma interfaz

.NET 8+ permite registrar y resolver múltiples implementaciones de una interfaz con una clave:

```csharp
// Registrar múltiples implementaciones con clave
builder.Services.AddKeyedScoped<IStorageService, LocalStorageService>("local");
builder.Services.AddKeyedScoped<IStorageService, S3StorageService>("s3");
builder.Services.AddKeyedScoped<IStorageService, AzureBlobStorageService>("azure");
```

```csharp
// Resolver por clave en el constructor
public class FileUploadService(
    [FromKeyedServices("s3")] IStorageService storage)
{
    public async Task UploadAsync(Stream file, string name, CancellationToken ct)
        => await storage.UploadAsync(file, name, ct);
}
```

```csharp
// Resolver dinámicamente (cuando la clave viene de configuración o runtime)
public class DynamicStorageSelector(IServiceProvider sp, IConfiguration config)
{
    public IStorageService GetStorage()
    {
        var provider = config["Storage:Provider"] ?? "local";
        return sp.GetRequiredKeyedService<IStorageService>(provider);
    }
}
```

---

## Guard clauses — validar entradas

Un guard clause es un chequeo temprano que lanza excepción si el argumento es inválido. Evita que el error aparezca tarde con un mensaje confuso.

```csharp
// ❌ Sin guard clauses — falla con NullReferenceException lejos del origen
public class OrderService
{
    private readonly IOrderRepository _repo;
    public OrderService(IOrderRepository repo) { _repo = repo; }
    // ... si repo es null, el error aparece al llamar un método, no en el constructor
}

// ✓ Con ArgumentNullException.ThrowIfNull (C# 10+)
public class OrderService
{
    private readonly IOrderRepository _repo;

    public OrderService(IOrderRepository repo)
    {
        ArgumentNullException.ThrowIfNull(repo);
        _repo = repo;
    }
}

// ✓ Con primary constructor (C# 12 + .NET 8) — el contenedor garantiza no-null
public class OrderService(IOrderRepository repo)
{
    // El contenedor lanza InvalidOperationException si repo no está registrado
    // Guard clause innecesario para dependencias obligatorias inyectadas por el contenedor
}
```

---

## Factory pattern con DI

Cuando un objeto necesita crearse con parámetros de runtime (que no están disponibles en el composition root), se usa un factory:

```csharp
// Interfaz del factory
public interface ITenantDbContextFactory
{
    TenantDbContext Create(string tenantId);
}

// Implementación
public sealed class TenantDbContextFactory(
    IConfiguration config,
    ILoggerFactory loggerFactory) : ITenantDbContextFactory
{
    public TenantDbContext Create(string tenantId)
    {
        var connectionString = config[$"Tenants:{tenantId}:ConnectionString"]
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found.");

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connectionString)
            .UseLoggerFactory(loggerFactory)
            .Options;

        return new TenantDbContext(options);
    }
}

// Registro y uso
builder.Services.AddSingleton<ITenantDbContextFactory, TenantDbContextFactory>();

// En el servicio que lo consume
public class TenantDataService(ITenantDbContextFactory factory)
{
    public async Task<IList<Order>> GetOrdersAsync(string tenantId, CancellationToken ct)
    {
        using var ctx = factory.Create(tenantId);
        return await ctx.Orders.ToListAsync(ct);
    }
}
```

---

## Relación con back-template

El back-template sigue estas convenciones de DI:

| Tipo | Lifetime | Razón |
|------|----------|-------|
| `MainDbConnectionFactory` | Singleton | Sin estado mutable; costoso de crear |
| `IExampleUserRepository` | Scoped | Un repositorio por request (Unit of Work pattern) |
| `GetExampleUserHandler` | Scoped | Handler CQRS: stateless pero con repositorio Scoped |
| `IJwtTokenService` | Singleton | Sin estado; las opciones JwtOptions son inmutables |
| `IMediator` | Singleton | Sin estado; gestiona el bus de mensajes |

Los módulos registran sus dependencias en `ServiceCollectionEx` dentro de cada capa:

```csharp
// Host/ServiceCollectionEx.cs — composition root del módulo
public static class ServiceCollectionEx
{
    public static IServiceCollection AddOrdersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<GetOrderHandler>();
        services.AddScoped<CreateOrderHandler>();
        // ...
        return services;
    }
}
```

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Constructor injection para todas las dependencias obligatorias | Service Locator para dependencias en clases de negocio |
| Scrutor `Decorate` para añadir cross-cutting concerns (cache, logging) | Registrar como Singleton una clase que depende de recursos por-request |
| Keyed services para múltiples implementaciones de la misma interfaz | `IServiceProvider` directamente en clases de negocio |
| `IServiceScopeFactory` en singletons que necesitan acceder a scoped services | `Transient` para objetos costosos de crear (usar Singleton o Scoped) |

---

## Glosario

| Término | Definición |
|---------|-----------|
| IoC (Inversion of Control) | principio donde el control del flujo del programa se invierte — el framework/contenedor llama al código, no al revés |
| DI (Dependency Injection) | técnica para implementar IoC: las dependencias se pasan al objeto en lugar de que el objeto las cree |
| Composition Root | punto único de la aplicación donde se configuran todas las dependencias (Program.cs en ASP.NET Core) |
| Volatile dependency | clase de negocio o acceso a datos que puede cambiar — candidata a inyección con interfaz |
| Stable dependency | tipo del BCL o DTO sin comportamiento — puede crearse con `new` |
| Control Freak | anti-pattern donde una clase crea sus dependencias con `new` en lugar de recibirlas |
| Service Locator | anti-pattern donde se usa `IServiceProvider` dentro de clases de negocio para resolver dependencias |
| Captive dependency | bug de lifetime: un Singleton captura un Scoped, que nunca es liberado por el contenedor |
| Decorator | patrón que extiende el comportamiento de un objeto envolviéndolo con otro que implementa la misma interfaz |
| Scrutor | librería NuGet que añade `Decorate()` a `IServiceCollection` para registrar decoradores con DI |
| Keyed services | característica de .NET 8+ para registrar y resolver múltiples implementaciones de una interfaz con una clave |
| Guard clause | validación temprana (ThrowIfNull, ArgumentException) que falla rápido con un mensaje claro |

---

*Rogelio Arriaga Gonzalez*
