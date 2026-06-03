# 05 — CQRS (Command Query Responsibility Segregation)

CQRS separa las operaciones de lectura (Queries) de las de escritura (Commands) en modelos distintos. Es el principio arquitectónico que da estructura a los casos de uso de este proyecto.

---

## El principio base: CQS

CQRS viene de **Command Query Separation (CQS)** de Bertrand Meyer:

> Un método debe ser o un **Command** (modifica estado, no retorna datos) o una **Query** (retorna datos, no modifica estado). Nunca los dos a la vez.

```csharp
// ❌ Viola CQS — modifica Y retorna al mismo tiempo
public User InsertAndReturn(CreateUserDto dto)
{
    var user = /* crear usuario */;
    _db.Insert(user);
    return user;  // ← retorna el usuario recién insertado
}

// ✓ CQS — separados
public void Insert(CreateUserDto dto) { _db.Insert(/* ... */); }   // Command: modifica, no retorna
public User GetById(Guid id) { return _db.Get(id); }               // Query: retorna, no modifica
```

**CQRS** lleva esto a nivel arquitectónico: modelos separados para leer y escribir.

---

## CQRS en este proyecto

Cada caso de uso es o un Command o una Query:

```
Commands — modifican estado:            Queries — leen estado:
  InsertExampleUserRequest                GetExampleUserRequest
  UpdateExampleUserRequest                GetExampleUsersRequest (paginado)
  DeleteExampleUserRequest                GetExampleUserByEmailRequest
  ActivateExampleUserRequest
```

```csharp
// Query — solo lee, no modifica nada
public sealed record GetExampleUserRequest(Guid PublicId)
    : IRequest<GetExampleUserResponse>;

public sealed class GetExampleUserHandler
    : IRequestHandler<GetExampleUserRequest, GetExampleUserResponse>
{
    private readonly IExampleUserRepository _repo;

    public async Task<GetExampleUserResponse> Handle(
        GetExampleUserRequest request, CancellationToken ct)
    {
        var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);
        return user is null
            ? new GetExampleUserNotFoundFailure("No encontrado.")
            : new GetExampleUserSuccess(new ExampleUserDto(user));
    }
}

// Command — modifica estado, puede o no retornar datos
public sealed record InsertExampleUserRequest(string FullName, string Email, string Password)
    : IRequest<InsertExampleUserResponse>;

public sealed class InsertExampleUserHandler
    : IRequestHandler<InsertExampleUserRequest, InsertExampleUserResponse>
{
    private readonly IExampleUserRepository _repo;

    public async Task<InsertExampleUserResponse> Handle(
        InsertExampleUserRequest request, CancellationToken ct)
    {
        if (await _repo.EmailExistsAsync(request.Email, ct))
            return new InsertExampleUserConflictFailure("El email ya está registrado.");

        var user = new ExampleUser { /* ... */ };
        await _repo.InsertAsync(user, ct);
        return new InsertExampleUserSuccess(new ExampleUserDto(user));
    }
}
```

---

## Modelos de lectura vs escritura

En CQRS básico (como este proyecto), Commands y Queries usan la misma base de datos pero modelos de objeto distintos.

```
Base de datos PostgreSQL
        ↓              ↓
  Write Model        Read Model
  (entidades)        (DTOs / proyecciones)

Commands → ExampleUser (entidad con validaciones, comportamiento)
Queries  → ExampleUserDto (estructura plana optimizada para la respuesta)
```

```csharp
// Write Model — entidad con comportamiento e invariantes
public sealed class ExampleUser
{
    public int    Id       { get; init; }
    public Guid   PublicId { get; init; }
    public string FullName { get; private set; } = string.Empty;
    public string Email    { get; private set; } = string.Empty;
    // ...métodos de negocio
}

// Read Model — DTO plano, optimizado para la respuesta API
public sealed record ExampleUserDto(
    Guid   PublicId,
    string FullName,
    string Email,
    bool   IsActive);
```

### Optimización de queries (Read Model optimizado)

Las Queries pueden usar proyecciones SQL que solo traen los campos necesarios:

```csharp
// Query que solo trae los campos del DTO — no hidrata toda la entidad
public Task<ExampleUserDto?> GetDtoByPublicIdAsync(Guid publicId, CancellationToken ct) =>
    _db.QuerySingleAsync<ExampleUserDto>(
        """
        SELECT PublicId, FullName, Email, IsActive
        FROM dbo.ExampleUsers
        WHERE PublicId = @publicId;
        """,
        new { publicId },
        cancellationToken: ct);

// vs la versión que hidrata la entidad completa (para Commands que necesitan el objeto)
public Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct) =>
    _db.QuerySingleAsync<ExampleUser>(
        """
        SELECT Id, PublicId, FullName, Email, PasswordHash, IsActive, CreatedAtUtc, UpdatedAtUtc
        FROM dbo.ExampleUsers
        WHERE PublicId = @publicId;
        """,
        new { publicId },
        cancellationToken: ct);
```

---

## CQRS con bases de datos separadas (nivel avanzado)

En sistemas de alta escala, Commands y Queries pueden usar bases de datos separadas:

```
Client
  │
  ├── Commands → Write DB (PostgreSQL) → publica eventos → sincroniza Read DB
  │                                                              ↓
  └── Queries  ←─────────────────────────────── Read DB (PostgreSQL read replica / Redis / ElasticSearch)
```

**Cuándo tiene sentido esta separación:**
- Las lecturas son órdenes de magnitud más frecuentes que las escrituras
- Las lecturas necesitan queries complejos que no se optimizan bien en el modelo de escritura
- Se necesita escalar lectura y escritura independientemente

**Para este proyecto:** Una sola DB es suficiente. El modelo de dos DBs se agrega cuando los números lo justifican.

---

## Event Sourcing (extensión de CQRS)

Event Sourcing es una extensión donde el estado no se guarda directamente. Se guardan los **eventos** que llevaron a ese estado. El estado actual se reconstruye reproduciendo los eventos.

```
// Modelo tradicional (este proyecto):
ExampleUsers table:
  | Id | FullName    | Email           | IsActive |
  |----|-------------|-----------------|----------|
  | 1  | Juan García | juan@example.com | false   |
  (el estado actual — la historia se pierde)

// Event Sourcing:
ExampleUsersEvents table:
  | EventType            | Data                              | OccurredAt |
  |----------------------|-----------------------------------|------------|
  | UserRegistered       | { fullName: "Juan García", ... }  | 2026-01-01 |
  | UserEmailUpdated     | { email: "new@example.com" }      | 2026-03-15 |
  | UserDeactivated      | { reason: "request" }             | 2026-05-10 |
  (el estado es la proyección de todos los eventos — la historia queda completa)
```

**Cuándo usar Event Sourcing:** auditoría completa requerida (finanzas, salud, legal), necesitas "time travel" al estado en cualquier punto del pasado, la historia de cambios es tan importante como el estado actual.

**Cuándo NO usar Event Sourcing:** la mayoría de los casos. Es significativamente más complejo.

---

## Beneficios de CQRS en este proyecto

### 1. Handlers enfocados

Cada Handler tiene una sola responsabilidad. El Handler de "obtener usuario" no conoce nada del "insertar usuario".

### 2. Optimización independiente

```csharp
// Query Handler — puede optimizar la lectura sin afectar escrituras
public async Task<GetExampleUsersResponse> Handle(GetExampleUsersRequest req, CancellationToken ct)
{
    // SELECT solo los campos del DTO, con paginación eficiente
    var (items, total) = await _repo.GetPagedAsync(req.Page, req.PageSize, ct);
    return new GetExampleUsersSuccess(
        items.Select(u => new ExampleUserDto(u)).ToList().AsReadOnly(),
        total, req.Page, req.PageSize);
}

// Command Handler — puede cargar la entidad completa para validar invariantes
public async Task<InsertExampleUserResponse> Handle(InsertExampleUserRequest req, CancellationToken ct)
{
    if (await _repo.EmailExistsAsync(req.Email, ct))
        return new InsertExampleUserConflictFailure("Email ya registrado.");
    // ...
}
```

### 3. Testabilidad por caso de uso

```csharp
// Test del GetExampleUserHandler — aislado, no afecta InsertHandler
[Fact]
public async Task Handle_UserExists_ReturnsSuccess()
{
    var user = new ExampleUser { PublicId = Guid.NewGuid(), FullName = "Test" };
    var repo = Substitute.For<IExampleUserRepository>();
    repo.GetByPublicIdAsync(user.PublicId).Returns(user);

    var handler = new GetExampleUserHandler(repo);
    var result  = await handler.Handle(new GetExampleUserRequest(user.PublicId), default);

    Assert.IsType<GetExampleUserSuccess>(result);
}
```

---

## CQRS vs CRUD

| CRUD | CQRS |
|------|------|
| Controller → Service → Repository | Controller → Handler → Repository |
| Un Service con todos los métodos | Un Handler por caso de uso |
| Mezcla lógica de lectura y escritura | Lectura y escritura separadas |
| Escala poco cuando hay mucha lógica | Escala bien — cada caso de uso es independiente |
| Apropiado para CRUD puro | Apropiado cuando hay lógica de negocio |

---

## Cuándo no usar CQRS

CQRS agrega complejidad. Para una API puramente CRUD (sin lógica de negocio significativa) un Controller que llame al Repository directamente es suficiente y más simple. CQRS paga su costo cuando:

- Hay lógica de negocio que varía entre casos de uso del mismo recurso
- El equipo crece y necesita trabajar en casos de uso en paralelo sin conflictos
- Se necesita separar la optimización de lecturas y escrituras

---

## Proyecciones en CQRS
> Fuente: *.NET Microservices Architecture* — Ch.7 Creating and Evolving Event Sourced Aggregates

Una **proyección** es un modelo de lectura derivado del estado de escritura. En CQRS, las proyecciones son los Read Models que el Query side usa: pueden ser vistas materializadas, tablas denormalizadas o caché.

```
Write Side                           Read Side
──────────                           ─────────
ExampleUser (entidad)                ExampleUserSummaryView (proyección)
├── Id                               ├── PublicId
├── PublicId                         ├── FullName
├── FullName                         ├── Email
├── PasswordHash          →→→        ├── IsActive
├── CreatedAtUtc          proyecta   └── CreatedAtUtcFormatted ("01/01/2026")
├── UpdatedAtUtc
└── Roles[]
```

### Proyección como vista SQL (PostgreSQL)

```sql
-- Proyección materializada en la base de datos
CREATE MATERIALIZED VIEW ExampleUserSummaryView AS
SELECT
    PublicId,
    FullName,
    Email,
    IsActive,
    TO_CHAR(CreatedAtUtc AT TIME ZONE 'America/Mexico_City', 'DD/MM/YYYY') AS CreatedAtFormatted,
    (SELECT COUNT(*) FROM ExampleUserRoles r WHERE r.UserId = u.Id) AS RoleCount
FROM ExampleUsers u;

-- Índice para el PublicId más buscado
CREATE UNIQUE INDEX idx_example_user_summary_public_id ON ExampleUserSummaryView (PublicId);

-- Refrescar cuando cambian los datos
REFRESH MATERIALIZED VIEW CONCURRENTLY ExampleUserSummaryView;
```

```csharp
// Query Handler que usa la proyección directamente — no hidrata la entidad
public sealed class GetExampleUserSummaryHandler
    : IRequestHandler<GetExampleUserSummaryRequest, GetExampleUserSummaryResponse>
{
    private readonly IDbConnection _db;

    public async Task<GetExampleUserSummaryResponse> Handle(
        GetExampleUserSummaryRequest request, CancellationToken ct)
    {
        var summary = await _db.QuerySingleOrDefaultAsync<ExampleUserSummaryDto>(
            """
            SELECT PublicId, FullName, Email, IsActive, CreatedAtFormatted, RoleCount
            FROM ExampleUserSummaryView
            WHERE PublicId = @PublicId
            """,
            new { request.PublicId });

        return summary is null
            ? new GetExampleUserSummaryNotFoundFailure("Usuario no encontrado.")
            : new GetExampleUserSummarySuccess(summary);
    }
}
```

### Proyección actualizada por Domain Events

En lugar de vistas SQL, la proyección puede ser una tabla separada actualizada por eventos:

```csharp
// Evento de dominio dispara actualización de la proyección
public sealed class UserRegisteredEventHandler
    : INotificationHandler<UserRegisteredEvent>
{
    private readonly IDbConnection _db;

    public async Task Handle(UserRegisteredEvent evt, CancellationToken ct)
    {
        // Actualiza la proyección de "usuarios activos por día"
        await _db.ExecuteAsync(
            """
            INSERT INTO UserRegistrationsByDay (Date, Count)
            VALUES (@Date, 1)
            ON CONFLICT (Date) DO UPDATE SET Count = UserRegistrationsByDay.Count + 1
            """,
            new { Date = evt.OccurredAt.Date });
    }
}
```

---

## MediatR — tipos de mensajes
> Fuente: *Architecting ASP.NET Core Applications* (Ferreira) — Ch.16 Mediator and CQS Patterns

MediatR soporta tres tipos de mensajes:

| Tipo | Handlers | Uso típico |
|------|----------|------------|
| **Request/Response** (`IRequest<T>`) | Exactamente 1 | Commands y Queries — relación 1:1 |
| **Notifications** (`INotification`) | 0 o N | Domain Events, Integration Events — Publish/Subscribe |
| **Streams** (`IStreamRequest<T>`) | Exactamente 1 | Respuesta paginada como `IAsyncEnumerable<T>` |

```csharp
// Request/Response — un solo handler (Command o Query)
public record GetUserQuery(Guid Id) : IRequest<GetUserResponse>;

// Notification — múltiples handlers (Domain Event)
public record UserRegisteredEvent(Guid UserId, string Email) : INotification;

// Los handlers de Notification se ejecutan todos en paralelo o secuencial
public class SendWelcomeEmailHandler : INotificationHandler<UserRegisteredEvent> { }
public class CreateUserProfileHandler : INotificationHandler<UserRegisteredEvent> { }
```

### Extensiones del pipeline de MediatR

Además de `IPipelineBehavior<TRequest,TResponse>`, MediatR ofrece:

```csharp
// Ejecuta ANTES del handler — ideal para enriquecer el request o pre-validar
public class AuditPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    public Task Process(TRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Request: {Type}", typeof(TRequest).Name);
        return Task.CompletedTask;
    }
}

// Ejecuta DESPUÉS del handler — ideal para logging de respuesta o post-proceso
public class LogResponsePostProcessor<TRequest, TResponse>
    : IRequestPostProcessor<TRequest, TResponse>
{
    public Task Process(TRequest request, TResponse response, CancellationToken ct)
    {
        _logger.LogInformation("Response: {Type}", typeof(TResponse).Name);
        return Task.CompletedTask;
    }
}

// Manejo de excepciones por tipo de exception dentro del pipeline
public class NotFoundExceptionHandler<TRequest, TResponse, TException>
    : IRequestExceptionHandler<TRequest, TResponse, TException>
    where TException : NotFoundException
{
    public Task Handle(TRequest request, TException exception,
        RequestExceptionHandlerState<TResponse> state, CancellationToken ct)
    {
        state.SetHandled(/* default response */);
        return Task.CompletedTask;
    }
}
```

---

## CQRS con MediatR — pipeline de behaviors
> Fuente: *.NET Microservices Architecture* — Ch.6 CQRS Patterns

MediatR permite agregar comportamientos transversales (cross-cutting concerns) como validación, logging y caché en el pipeline sin tocar los Handlers.

```
Request → ValidationBehavior → LoggingBehavior → CachingBehavior → Handler → Response
```

```csharp
// Behavior de validación — aplica a todos los Commands con validadores registrados
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}

// Behavior de caché — solo para Queries que implementen ICacheableQuery
public sealed class CachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableQuery
{
    private readonly IDistributedCache _cache;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var cacheKey = request.CacheKey;
        var cached   = await _cache.GetStringAsync(cacheKey, ct);

        if (cached is not null)
            return JsonSerializer.Deserialize<TResponse>(cached)!;

        var response = await next();
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = request.Expiration },
            ct);

        return response;
    }
}

// Marcar un Query como cacheable
public sealed record GetExampleUserRequest(Guid PublicId)
    : IRequest<GetExampleUserResponse>, ICacheableQuery
{
    public string   CacheKey   => $"user:{PublicId}";
    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
```

```csharp
// Registro en Program.cs
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
});
```

---

## Glosario

| Término | Definición |
|---------|-----------|
| CQRS | Command Query Responsibility Segregation — patrón que separa las operaciones de lectura (queries) y escritura (commands) en flujos independientes |
| Command | mensaje que produce un cambio de estado en el sistema; no retorna datos, solo éxito o falla |
| Query | mensaje que lee el estado del sistema sin modificarlo; retorna un DTO o lista de DTOs |
| Handler | clase con una sola responsabilidad que procesa un Command o Query específico |
| Mediator | intermediario que desacopla el emisor de un mensaje de su receptor; en .NET se usa MediatR |
| Pipeline Behavior | middleware del pipeline de MediatR que se ejecuta antes o después del Handler para concerns transversales |
| Proyección | modelo de lectura derivado del estado de escritura; puede ser una vista SQL, tabla desnormalizada o caché |
| Read Model | representación optimizada para consulta (DTO, vista materializada) independiente del modelo de escritura |
| Notification | mensaje de MediatR que puede tener cero o múltiples handlers; se usa para Domain Events e Integration Events |
| Event Sourcing | patrón donde el estado se deriva de una secuencia de eventos inmutables en lugar de un registro mutable |
| Vista materializada | tabla precalculada en la base de datos que se refresca periódicamente y optimiza las consultas de lectura |

---

*Rogelio Arriaga Gonzalez*
