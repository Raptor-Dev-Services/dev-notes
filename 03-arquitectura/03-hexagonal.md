# 03 — Arquitectura Hexagonal (Ports & Adapters)

La arquitectura hexagonal aísla el dominio de negocio de todo lo que lo rodea: bases de datos, HTTP, mensajería, CLIs. El dominio no sabe nada del mundo exterior; el mundo exterior se adapta al dominio a través de interfaces bien definidas llamadas **puertos**.

> Autor: Alistair Cockburn (2005). Popularizada por Eric Evans y los libros de DDD.

---

## El problema que resuelve

```csharp
// ❌ El dominio depende directamente de infraestructura
public class ExampleUserService
{
    private readonly SqlConnection _sql;   // ← acoplado a SQL Server

    public async Task<ExampleUser?> GetByIdAsync(Guid id)
    {
        return await _sql.QueryFirstOrDefaultAsync<ExampleUser>(
            "SELECT * FROM users WHERE id = @id", new { id });
    }
}
// El dominio no se puede testear sin una base de datos real.
// Cambiar de SQL Server a PostgreSQL requiere tocar la capa de negocio.
```

```csharp
// ✓ El dominio depende de una abstracción (puerto)
public class ExampleUserService
{
    private readonly IExampleUserRepository _repo;  // ← solo la interfaz

    public async Task<ExampleUser?> GetByIdAsync(Guid id)
        => await _repo.GetByPublicIdAsync(id, CancellationToken.None);
}
// El adaptador concreto (Dapper, EF Core, mock) se intercambia sin tocar el dominio.
```

---

## Conceptos clave

```
                    ┌─────────────────────────────────┐
                    │        APLICACIÓN (hexágono)     │
                    │                                  │
[HTTP Request]  ──► │ Puerto primario                  │ Puerto secundario ──► [Base de datos]
[CLI]           ──► │ IExampleUserController (driving) │ IExampleUserRepository (driven) ──► [Cache]
[Test]          ──► │                                  │                       ──► [Email]
                    │         Dominio puro             │
                    └─────────────────────────────────┘
```

| Concepto | Rol | Ejemplo en back-template |
|---------|-----|--------------------------|
| **Puerto primario** (driving) | Interfaz que el mundo exterior llama hacia adentro | Mediator `IRequest<T>` |
| **Puerto secundario** (driven) | Interfaz que el dominio llama hacia afuera | `IExampleUserRepository` |
| **Adaptador primario** | Implementa el puerto primario — traduce HTTP/CLI al dominio | `ExampleUsersController` |
| **Adaptador secundario** | Implementa el puerto secundario — traduce el dominio a infra | `ExampleUserRepository` (Dapper) |

---

## Hexagonal vs Clean Architecture

Son compatibles. Clean Architecture es una forma concreta de implementar hexagonal:

| Hexagonal | Clean Architecture (back-template) |
|-----------|-------------------------------------|
| Dominio | `Domain/` — entidades e interfaces |
| Puertos primarios | `Application/` — handlers, requests, responses |
| Puertos secundarios | `Domain/Repositories/` — interfaces de repositorio |
| Adaptadores primarios | `WebApi/` — controllers, presenters |
| Adaptadores secundarios | `Infrastructure/` — repos concretos, SQL classes |
| Composition Root | `Host/Program.cs` — conecta puertos con adaptadores |

---

## Ejemplo completo: puerto secundario con dos adaptadores

```csharp
// Puerto — en Domain/Repositories/
public interface IExampleUserRepository
{
    Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct);
    Task InsertAsync(ExampleUser user, CancellationToken ct);
}

// Adaptador 1 — Dapper (producción)
public class ExampleUserRepository : IExampleUserRepository
{
    private readonly IDbConnection _db;

    public async Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
    {
        const string sql = ExampleUsersSql.GetByPublicId;
        return await _db.QueryFirstOrDefaultAsync<ExampleUser>(sql, new { publicId });
    }

    public async Task InsertAsync(ExampleUser user, CancellationToken ct)
    {
        const string sql = ExampleUsersSql.Insert;
        await _db.ExecuteAsync(sql, user);
    }
}

// Adaptador 2 — en memoria (tests)
public class InMemoryExampleUserRepository : IExampleUserRepository
{
    private readonly List<ExampleUser> _store = [];

    public Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
        => Task.FromResult(_store.FirstOrDefault(u => u.PublicId == publicId));

    public Task InsertAsync(ExampleUser user, CancellationToken ct)
    {
        _store.Add(user);
        return Task.CompletedTask;
    }
}

// Composition root — selecciona el adaptador según el entorno
builder.Services.AddScoped<IExampleUserRepository, ExampleUserRepository>();
// o en tests: services.AddSingleton<IExampleUserRepository, InMemoryExampleUserRepository>();
```

---

## Puerto primario: el flujo HTTP → Dominio

```csharp
// Adaptador primario (WebApi) — traduce HTTP a IRequest<T>
[ApiController]
[Route("api/users")]
public class ExampleUsersController(IMediator mediator) : ControllerBase
{
    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> GetById(Guid publicId)
    {
        var request = new GetExampleUserRequest(publicId);
        var response = await mediator.Send(request);
        return _viewModel.IsSuccess ? Ok(_viewModel) : NotFound(_viewModel);
    }
}

// Puerto primario (Application) — el handler no sabe nada de HTTP
public class GetExampleUserHandler
    : IRequestHandler<GetExampleUserRequest, GetExampleUserResponse>
{
    private readonly IExampleUserRepository _repo;

    public async Task<GetExampleUserResponse> Handle(
        GetExampleUserRequest request, CancellationToken ct)
    {
        var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);
        return user is null
            ? new GetExampleUserNotFoundFailure("Usuario no encontrado.")
            : new GetExampleUserSuccess(new ExampleUserDto(user.PublicId, user.FullName, user.Email, user.IsActive));
    }
}
```

---

## Relación con el back-template

El back-template es hexagonal por diseño:

- `Domain/Repositories/IExampleUserRepository.cs`: puerto secundario
- `Infrastructure/Repositories/ExampleUserRepository.cs`: adaptador secundario
- `WebApi/Controllers/ExampleUsersController.cs`: adaptador primario
- `Application/Handlers/GetExampleUserHandler.cs`: lógica dentro del hexágono
- `Host/Program.cs`: composition root que conecta todo

La regla que lo garantiza: **Domain no referencia Infrastructure**. Solo Infrastructure referencia Domain.

---

## Hexagonal vs Clean Architecture vs Onion

Los tres resuelven el mismo problema con la misma idea. Se confunden porque son variantes de un único principio.

| Aspecto | Hexagonal | Clean Architecture | Onion |
|---------|-----------|-------------------|-------|
| **Autor / año** | Cockburn, 2005 | Martin, 2012 | Palermo, 2008 |
| **Metáfora** | Hexágono con puertos | Capas concéntricas | Cebolla — capas anidadas |
| **Foco** | Separar driving/driven adapters | Regla de dependencia (→ adentro) | Domain Services explícitos |
| **Capas internas** | No define capas internas | Domain + Application + Infrastructure | Domain Model + Domain Services + Application |
| **Puertos** | Concepto explícito (in/out) | Interfaces en capas internas | Interfaces en capas internas |
| **Adapters** | Concepto explícito | Controllers/Repos son implícitamente adapters | Igual que Clean |

**En la práctica:** son la misma idea expresada distinto. Aprender uno es aprender todos. El back-template usa **Clean Architecture con mentalidad Hexagonal**. Clean da la estructura de capas; Hexagonal ayuda a razonar sobre qué es un puerto y qué es un adaptador.

```
Hexagonal         → Clean Architecture    → Este proyecto
──────────────────────────────────────────────────────────
Port (in)         → Use Case Interface    → IRequest + IRequestHandler
Driving Adapter   → Controller            → ExampleUsersController
Core Application  → Application layer     → GetExampleUserHandler
Port (out)        → Repository Interface  → IExampleUserRepository
Driven Adapter    → Infrastructure        → ExampleUserRepository + ExampleUsersSql
```

---

## La Dependency Rule

> **Las dependencias de código solo pueden apuntar hacia adentro.** Nada en una capa interna puede conocer nada de una capa externa.

```
✓ Application conoce Domain
✓ Infrastructure conoce Domain
✓ WebApi conoce Application
✗ Domain conoce Infrastructure      ← viola la regla
✗ Application conoce Infrastructure ← viola la regla
✗ Domain conoce WebApi              ← viola la regla
```

Se implementa con DI: la capa externa implementa la interface definida en la capa interna. La capa interna nunca importa la externa.

---

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Sistemas que necesitan testearse sin infra real | CRUDs simples sin lógica de negocio |
| Cuando hay múltiples adaptadores (Dapper + EF Core, REST + GraphQL) | Scripts o herramientas de un solo uso |
| Dominios complejos con reglas de negocio cambiantes | Proyectos de vida muy corta (PoC descartable) |
| Multi-tenancy donde los adaptadores varían por tenant | Cuando Clean Architecture ya da suficiente estructura |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Puerto | Interfaz que define cómo el dominio se comunica con el exterior — no contiene implementación |
| Adaptador | Clase concreta que implementa un puerto — traduce entre el dominio y la tecnología específica |
| Puerto primario (driving) | Puerto que el mundo exterior llama hacia el dominio — HTTP request, CLI command, test |
| Puerto secundario (driven) | Puerto que el dominio llama hacia afuera — repositorio, servicio de email, cache |
| Hexágono | Metáfora visual del dominio rodeado de puertos — el número de lados no tiene significado especial |
| Composition Root | Punto único (Program.cs) donde se conectan puertos con adaptadores mediante DI |
| Inversión de dependencias | El dominio define la interfaz (puerto); la infraestructura depende del dominio, no al revés |
| Adaptador primario | Implementa el puerto primario — controller, consumer de mensajes, CLI parser |
| Adaptador secundario | Implementa el puerto secundario — repositorio Dapper, cliente HTTP, mock en memoria |
| Clean Architecture | Implementación concreta de hexagonal con capas nombradas: Domain, Application, Infrastructure, WebApi |
| Dominio puro | Código sin dependencias de frameworks o infraestructura — solo lógica de negocio y entidades |
| Testabilidad | Propiedad que hexagonal garantiza: reemplazar adaptadores secundarios por mocks sin tocar el dominio |

---

*Rogelio Arriaga Gonzalez*
