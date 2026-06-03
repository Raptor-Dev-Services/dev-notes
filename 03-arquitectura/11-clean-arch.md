# 11 · Layering y Clean Architecture

El modelo clásico de tres capas (Presentación, Dominio, Datos) genera acoplamiento directo entre capas: el Dominio depende de la capa de Datos, lo que impide testear la lógica de negocio sin una base de datos real y obliga a reescribir el Dominio cada vez que cambia el origen de datos. Clean Architecture resuelve esto invirtiendo el flujo de dependencias: el núcleo no conoce a nadie, y todas las capas externas lo conocen a él.

> Fuente: *Architecting ASP.NET Core Applications* (Marcotte, 3rd Ed) — Ch.14 Layering and Clean Architecture

---

## Capas, tiers y assemblies — la diferencia conceptual

Tres términos que se confunden frecuentemente:

| Concepto | Definición | Ejemplo |
|----------|-----------|---------|
| **Layer** (capa) | Unidad lógica de organización del código | Domain, Application, Infrastructure |
| **Tier** (nivel) | Unidad física de despliegue (máquina) | API server + DB server = 2 tiers |
| **Assembly** | Unidad compilada de .NET (.dll) | `GTM.Suite.Domain.dll` |

No existe relación 1:1: tres layers pueden residir en el mismo assembly, o cada layer puede ser un assembly separado. Un assembly no garantiza que las layers estén desacopladas. La disciplina del equipo lo hace.

---

## El modelo clásico de 3 capas

```
❌ Flujo clásico — acoplamiento directo hacia abajo

Presentation
     │ depende de
     ▼
  Domain
     │ depende de
     ▼
   Data
```

El problema concreto:

```csharp
// ❌ Domain depende directamente de la implementación concreta de datos
public class OrderService
{
    private readonly OrderRepository _repo;  // clase concreta — no interfaz

    public OrderService()
    {
        _repo = new OrderRepository();       // Control Freak — crea su propia dependencia
    }

    public async Task<Order?> GetAsync(Guid id, CancellationToken ct)
        => await _repo.GetByIdAsync(id, ct); // Domain → Data: acoplamiento directo
}
```

Consecuencias:
- No se puede testear `OrderService` sin una BD real o mocks invasivos
- Cambiar de EF Core a Dapper requiere modificar el Domain
- Las cascading changes cruzan todas las capas cuando cambia el schema

---

## Clean Architecture — el flujo invertido

Clean Architecture organiza las capas como círculos concéntricos donde **las dependencias solo apuntan hacia adentro**:

```
┌─────────────────────────────────────────────┐
│  Infrastructure / UI (outer)                │
│  ┌───────────────────────────────────────┐  │
│  │  Application / Use Cases              │  │
│  │  ┌─────────────────────────────────┐  │  │
│  │  │  Domain / Entities (innermost)  │  │  │
│  │  └─────────────────────────────────┘  │  │
│  └───────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
        Dependencias solo hacia adentro →
```

**Regla de dependencia:** ninguna capa interna conoce a las capas externas. La capa de Domain no importa nada de Infrastructure; Infrastructure importa de Application y Domain.

```
✓ Flujo invertido — el Domain no depende de Data

  Infrastructure → Application → Domain
       UI       → Application → Domain
```

### Implementación en .NET — la técnica de inversión

La inversión se logra con interfaces en el Domain/Application e implementaciones en Infrastructure:

```csharp
// ✓ Domain — define la interfaz (contrato), sin implementación
// Domain/Repositories/IExampleUserRepository.cs
public interface IExampleUserRepository
{
    Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct);
    Task<IReadOnlyList<ExampleUser>> GetAllAsync(CancellationToken ct);
    Task AddAsync(ExampleUser user, CancellationToken ct);
}
```

```csharp
// ✓ Infrastructure — implementa la interfaz (Dapper en el back-template)
// Infrastructure/Repositories/ExampleUserRepository.cs
public sealed class ExampleUserRepository(IMainDbConnectionFactory connectionFactory)
    : IExampleUserRepository
{
    public async Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryFirstOrDefaultAsync<ExampleUser>(
            ExampleUsersSql.GetByPublicId, new { PublicId = publicId });
    }

    public async Task<IReadOnlyList<ExampleUser>> GetAllAsync(CancellationToken ct)
    {
        using var connection = connectionFactory.Create();
        var result = await connection.QueryAsync<ExampleUser>(ExampleUsersSql.GetAll);
        return result.AsList();
    }

    public async Task AddAsync(ExampleUser user, CancellationToken ct)
    {
        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(ExampleUsersSql.Insert, user);
    }
}
```

```csharp
// ✓ Application — usa la interfaz, no conoce la implementación
// Application/UseCases/GetExampleUser/GetExampleUserHandler.cs
public sealed class GetExampleUserHandler(IExampleUserRepository repository)
    : IRequestHandler<GetExampleUserRequest, GetExampleUserResponse>
{
    public async Task<GetExampleUserResponse> Handle(
        GetExampleUserRequest request, CancellationToken ct)
    {
        var user = await repository.GetByPublicIdAsync(request.PublicId, ct);

        if (user is null)
            return new GetExampleUserNotFoundFailure("Usuario no encontrado.");

        return new GetExampleUserSuccess(
            new ExampleUserDto(user.PublicId, user.FullName, user.Email, user.IsActive));
    }
}
```

---

## Las capas del back-template

El back-template implementa Clean Architecture con estas capas:

| Capa | Proyecto | Responsabilidad |
|------|---------|----------------|
| **Domain** | `GTM.Suite.Domain` | Entidades, interfaces de repositorio |
| **Application** | `GTM.Suite.Application` | Handlers CQRS, Requests, Responses, DTOs, Pipeline Behaviors |
| **Infrastructure** | `GTM.Suite.Infrastructure` | Repositorios concretos (Dapper/EF Core), SQL classes, migraciones, EF configs |
| **WebApi** | `GTM.Suite.WebApi` | Controllers, Presenters, ViewModels |
| **Host** | `GTM.Suite.Host` | Program.cs, composition root, `ServiceCollectionEx` por capa |
| **Common** | `GTM.Suite.Common` | IMediator, IRequest, IResponse, ISuccess\<T\>, IFailure — sin dependencias externas |

### Reglas de referencia entre proyectos

```
Domain         ← sin dependencias de proyecto (solo .NET BCL)
Common         ← sin dependencias de proyecto
Application    ← Domain, Common
Infrastructure ← Domain, Application
WebApi         ← Application, Common
Host           ← todos los anteriores (solo el Host ensambla todo)
```

**Regla de oro:** Infrastructure nunca es referenciado por Application ni Domain. Solo el Host y la WebApi pueden referenciar Infrastructure para la composition root.

---

## Flujo por request — de extremo a extremo

```
HTTP Request
     │
     ▼
Controller (WebApi)
     │  Mediator.Send(GetExampleUserRequest)
     ▼
[Pipeline Behaviors] → ValidationBehavior → LoggingBehavior
     │
     ▼
GetExampleUserHandler (Application)
     │  repository.GetByPublicIdAsync(...)
     ▼
ExampleUserRepository (Infrastructure) → Dapper → PostgreSQL
     │
     ▼
GetExampleUserSuccess(ExampleUserDto) ← o GetExampleUserNotFoundFailure
     │
     ▼  Mediator.Publish(response)
GetExampleUserPresenter (WebApi)
     │  _viewModel = response
     ▼
Controller → _viewModel.IsSuccess ? Ok(_viewModel) : NotFound(...)
```

---

## Rich model vs Anemic model

| Aspecto | Rich Domain Model | Anemic Domain Model |
|---------|------------------|---------------------|
| Lógica | Dentro de la entidad (métodos) | En servicios/handlers externos |
| Estado | La entidad se protege a sí misma | El servicio gestiona el estado |
| DI | Difícil inyectar dependencias en la entidad | Los servicios reciben DI normalmente |
| Testeo | Lógica testeable sin DI (unit tests directos) | Tests a nivel de servicio |
| Mejor para | Dominios complejos con invariantes estrictas | REST APIs stateless, CRUD-heavy |

```csharp
// Rich model — la entidad controla su estado
public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid PublicId { get; } = Guid.NewGuid();
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    public void AddItem(Product product, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("No se puede modificar un pedido confirmado.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        _items.Add(new OrderItem(product, quantity));
    }

    public void Confirm()
    {
        if (_items.Count == 0)
            throw new InvalidOperationException("El pedido no tiene items.");
        Status = OrderStatus.Confirmed;
    }
}
```

```csharp
// Anemic model — la entidad es un contenedor de datos
public sealed class ExampleUser
{
    public Guid PublicId { get; init; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
// La lógica vive en el Handler/Service
```

El back-template usa **anemic model**: las entidades son DTOs de base de datos y la lógica vive en los Handlers. Esto es idóneo para una REST API stateless con CQRS.

---

## SQL classes — separación de SQL del repositorio

El back-template extrae las queries SQL a clases separadas, manteniendo el repositorio limpio:

```csharp
// Infrastructure/Persistence/SQLDB/ExampleUsersSql.cs
internal static class ExampleUsersSql
{
    internal const string GetByPublicId = """
        SELECT public_id, full_name, email, is_active
        FROM example_users
        WHERE public_id = @PublicId
        """;

    internal const string GetAll = """
        SELECT public_id, full_name, email, is_active
        FROM example_users
        ORDER BY full_name
        """;

    internal const string Insert = """
        INSERT INTO example_users (public_id, full_name, email, is_active)
        VALUES (@PublicId, @FullName, @Email, @IsActive)
        """;
}
```

---

## Result Pattern — sin excepciones para flujo de negocio

El back-template reemplaza las excepciones de negocio con el Result Pattern:

```csharp
// Common — interfaces base
public interface ISuccess<out T> : IResponse { T Data { get; } }
public interface INotFoundFailure : IFailure { string Message { get; } }

// Application — responses tipados
public abstract record GetExampleUserResponse : IResponse;

public sealed record GetExampleUserSuccess(ExampleUserDto Data)
    : GetExampleUserResponse, ISuccess<ExampleUserDto>;

public sealed record GetExampleUserNotFoundFailure(string Message)
    : GetExampleUserResponse, INotFoundFailure;

// WebApi — presenter convierte response a HTTP
public sealed class GetExampleUserPresenter : INotificationHandler<GetExampleUserResponse>
{
    private IResult? _viewModel;

    public IResult ViewModel => _viewModel ?? Results.StatusCode(500);

    public Task Handle(GetExampleUserResponse response, CancellationToken ct)
    {
        _viewModel = response switch
        {
            GetExampleUserSuccess s    => Results.Ok(s.Data),
            GetExampleUserNotFoundFailure f => Results.NotFound(new { f.Message }),
            _ => Results.StatusCode(500)
        };
        return Task.CompletedTask;
    }
}
```

---

## Composition Root — Host/ServiceCollectionEx.cs

Toda la configuración de DI se concentra en el Host, que es el único proyecto que conoce todas las capas:

```csharp
// Host/ServiceCollectionEx.cs
public static class ServiceCollectionEx
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<GetExampleUserHandler>();
        // Handlers CQRS registrados aquí
        return services;
    }

    public static IServiceCollection AddInfrastructureLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IMainDbConnectionFactory>(
            _ => new MainDbConnectionFactory(
                configuration.GetConnectionString("MainDb")!));
        services.AddScoped<IExampleUserRepository, ExampleUserRepository>();
        return services;
    }
}

// Program.cs — el Host ensambla todo
var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddApplicationLayer()
    .AddInfrastructureLayer(builder.Configuration);
```

---

## Relación con el back-template

El back-template implementa Clean Architecture con estas decisiones concretas:

| Decisión | Elección | Razón |
|----------|---------|-------|
| ORM | Dapper (no EF Core) | Control total sobre SQL, sin overhead de tracking |
| Modelo | Anemic | REST API stateless — la lógica en Handlers es suficiente |
| Patrón de use case | CQRS con MediatR custom | Un Handler por caso de uso, testeable en aislamiento |
| Result Pattern | `ISuccess<T>` / `IFailure` | Sin excepciones para flujo de negocio |
| SQL | Clases `...Sql` separadas | Queries legibles y versionables fuera del repositorio |
| DI | Host como composition root | Solo el Host conoce todas las capas |

---

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| aplicaciones con dominio complejo o que evolucionarán | prototipos o herramientas internas de vida corta |
| equipos de más de 2 personas con múltiples capas | aplicaciones CRUD puras sin lógica de negocio |
| cuando los tests de dominio sin BD son un requisito | si el overhead de separación supera el valor entregado |
| proyectos SaaS Multi-Tenant con múltiples módulos | cuando se necesita velocidad de entrega sobre mantenibilidad |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Layer (capa) | unidad lógica de organización del código — no implica un assembly ni un tier separado |
| Tier | unidad física de despliegue; una sola máquina que ejecuta múltiples layers sigue siendo un tier |
| Assembly | unidad compilada de .NET (.dll) — varios layers pueden estar en el mismo assembly |
| Clean Architecture | patrón de organización de capas donde las dependencias solo apuntan hacia el núcleo (inward) |
| Core | capa central de Clean Architecture que contiene Entities + Use Cases — sin dependencias externas |
| Infrastructure | capa externa que contiene implementaciones concretas (BD, HTTP, colas, almacenamiento) |
| Dependency Rule | regla de Clean Architecture: ninguna capa interna puede importar de una capa externa |
| Rich Domain Model | entidad que encapsula su propia lógica en métodos — orientada a objetos pura |
| Anemic Domain Model | entidad que es solo un contenedor de datos; la lógica vive en servicios/handlers |
| Result Pattern | patrón que reemplaza excepciones para flujo de negocio con tipos discriminados (`ISuccess<T>` / `IFailure`) |
| CQRS | Command Query Responsibility Segregation — separa operaciones de lectura de escritura en handlers distintos |
| Composition Root | punto único donde se configuran todas las dependencias del contenedor DI (Program.cs / Host) |
| Persistence Ignorance | capacidad de testear lógica de dominio sin necesidad de una base de datos real |
| Repository pattern | abstracción de acceso a datos que expone operaciones en términos del dominio, no de SQL |

---

*Rogelio Arriaga Gonzalez*
