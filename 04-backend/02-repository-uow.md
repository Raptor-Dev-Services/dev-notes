# 04 — Repository Pattern y Unit of Work

Dos patrones de acceso a datos que trabajan juntos: Repository abstrae la persistencia, Unit of Work coordina las operaciones dentro de una transacción.

---

## Repository Pattern

### El problema que resuelve

Sin Repository, los handlers conocen directamente la base de datos. Imposible testear sin una DB real, imposible cambiar el motor de persistencia:

```csharp
// ❌ Handler acoplado a la persistencia — no testeable, viola DIP
public class GetExampleUserHandler
{
    private readonly NpgsqlConnection _conn;

    public async Task<GetExampleUserResponse> Handle(GetExampleUserRequest req, CancellationToken ct)
    {
        var user = await _conn.QuerySingleOrDefaultAsync<ExampleUser>(
            "SELECT * FROM dbo.ExampleUsers WHERE PublicId = @id",
            new { id = req.PublicId });

        return user is null
            ? new GetExampleUserNotFoundFailure("No encontrado.")
            : new GetExampleUserSuccess(new ExampleUserDto(user));
    }
}
```

### La solución

```csharp
// Domain/Repositories/ExampleUsers/IExampleUserRepository.cs
// La interfaz vive en Domain — sin dependencias de infraestructura
public interface IExampleUserRepository
{
    Task<ExampleUser?>            GetByPublicIdAsync(Guid publicId, CancellationToken ct = default);
    Task<IEnumerable<ExampleUser>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task                           InsertAsync(ExampleUser user, CancellationToken ct = default);
    Task                           UpdateAsync(ExampleUser user, CancellationToken ct = default);
}

// Infrastructure/Repositories/ExampleUsers/ExampleUserRepository.cs
// La implementación concreta vive en Infrastructure
public sealed class ExampleUserRepository : IExampleUserRepository
{
    private readonly ExampleUsersSql _sql;
    public ExampleUserRepository(ExampleUsersSql sql) => _sql = sql;

    public Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
        => _sql.GetByPublicIdAsync(publicId, ct);

    public Task<IEnumerable<ExampleUser>> GetAllAsync(int page, int pageSize, CancellationToken ct)
        => _sql.GetAllAsync(page, pageSize, ct);

    public Task InsertAsync(ExampleUser user, CancellationToken ct)
        => _sql.InsertAsync(user, ct);

    public Task UpdateAsync(ExampleUser user, CancellationToken ct)
        => _sql.UpdateAsync(user, ct);
}
```

```csharp
// Handler — solo conoce la interfaz, no sabe si es Postgres, MongoDB o memoria
public class GetExampleUserHandler
{
    private readonly IExampleUserRepository _repo;
    public GetExampleUserHandler(IExampleUserRepository repo) => _repo = repo;

    public async Task<GetExampleUserResponse> Handle(GetExampleUserRequest req, CancellationToken ct)
    {
        var user = await _repo.GetByPublicIdAsync(req.PublicId, ct);
        return user is null
            ? new GetExampleUserNotFoundFailure("No encontrado.")
            : new GetExampleUserSuccess(new ExampleUserDto(user));
    }
}
```

### Beneficios del Repository

| Beneficio | Descripción |
|-----------|-------------|
| **Testabilidad** | En tests se puede inyectar un `InMemoryUserRepository` sin DB |
| **Desacoplamiento** | El Handler no sabe de SQL, Npgsql ni Dapper |
| **DIP** | La interfaz está en Domain; la implementación en Infrastructure |
| **Consistencia** | Todos los accesos a una entidad pasan por un punto |

### Qué va en el Repository vs en la clase Sql

```
IExampleUserRepository   ← interfaz en Domain, define QUÉ se puede hacer
ExampleUserRepository    ← impl en Infrastructure, orquesta entre Sql classes
ExampleUsersSql          ← queries SQL concretos (nunca SQL fuera de aquí)
```

```csharp
// ExampleUserRepository puede combinar múltiples Sql classes en una operación:
public async Task InsertWithProfileAsync(ExampleUser user, UserProfile profile, CancellationToken ct)
{
    await _userSql.InsertAsync(user, ct);
    await _profileSql.InsertAsync(profile, ct);
    // ← sin transacción: si la segunda falla, la primera ya se ejecutó → problema
    // → aquí es donde Unit of Work entra
}
```

---

## Unit of Work

### El problema que resuelve

Cuando una operación debe modificar múltiples tablas de forma atómica: todo o nada. Sin Unit of Work, las operaciones son independientes y una falla parcial deja la DB en estado inconsistente.

```csharp
// ❌ Sin transacción — el sistema puede quedar en estado inconsistente
await _userRepo.InsertAsync(user, ct);        // OK
await _profileRepo.InsertAsync(profile, ct);  // FALLA → usuario sin perfil
```

### Implementación con Dapper

```csharp
// Infrastructure/Persistence/IUnitOfWork.cs
public interface IUnitOfWork : IAsyncDisposable
{
    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}

// Infrastructure/Persistence/DapperUnitOfWork.cs
public sealed class DapperUnitOfWork : IUnitOfWork
{
    private readonly MainDbConnectionFactory _factory;
    private NpgsqlConnection?   _connection;
    private NpgsqlTransaction?  _transaction;

    public DapperUnitOfWork(MainDbConnectionFactory factory) => _factory = factory;

    public async Task BeginAsync(CancellationToken ct = default)
    {
        _connection  = await _factory.OpenConnectionAsync(ct);
        _transaction = await _connection.BeginTransactionAsync(ct);
    }

    public Task CommitAsync(CancellationToken ct = default)
        => _transaction!.CommitAsync(ct);

    public Task RollbackAsync(CancellationToken ct = default)
        => _transaction!.RollbackAsync(ct);

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null) await _transaction.DisposeAsync();
        if (_connection  is not null) await _connection.DisposeAsync();
    }
}
```

```csharp
// Uso en un Handler que necesita transacción:
public sealed class RegisterUserHandler
    : IRequestHandler<RegisterUserRequest, RegisterUserResponse>
{
    private readonly IExampleUserRepository _userRepo;
    private readonly IUserProfileRepository _profileRepo;
    private readonly IUnitOfWork _uow;

    public RegisterUserHandler(
        IExampleUserRepository userRepo,
        IUserProfileRepository profileRepo,
        IUnitOfWork uow)
    {
        _userRepo    = userRepo;
        _profileRepo = profileRepo;
        _uow         = uow;
    }

    public async Task<RegisterUserResponse> Handle(
        RegisterUserRequest request, CancellationToken ct)
    {
        await _uow.BeginAsync(ct);
        try
        {
            var user = ExampleUser.Create(request.FullName, request.Email);
            await _userRepo.InsertAsync(user, ct);

            var profile = UserProfile.Create(user.PublicId, request.BirthDate);
            await _profileRepo.InsertAsync(profile, ct);

            await _uow.CommitAsync(ct);
            return new RegisterUserSuccess(new ExampleUserDto(user));
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }
}
```

### Alternativa más simple: pasar la conexión/transacción

Para casos más sencillos, Dapper acepta una transacción como parámetro:

```csharp
// MainDapperDbConnection acepta transacción opcional
public Task<int> ExecuteAsync(string sql, object? param = null,
    IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    => _connection.ExecuteAsync(
        new CommandDefinition(sql, param, transaction: transaction,
            cancellationToken: cancellationToken));

// Uso directo:
await using var conn = await _factory.OpenConnectionAsync(ct);
await using var tx   = await conn.BeginTransactionAsync(ct);
try
{
    await _userSql.InsertAsync(user, ct, tx);
    await _profileSql.InsertAsync(profile, ct, tx);
    await tx.CommitAsync(ct);
}
catch
{
    await tx.RollbackAsync(ct);
    throw;
}
```

---

## Cuándo usar Unit of Work

```
Operación en UNA tabla:    Repository solo → sin UoW
Operación en DOS+ tablas:  Repository + UoW → transacción atómica
```

**Ejemplos que necesitan UoW:**
- Registrar usuario + crear perfil + enviar welcome email log
- Confirmar pedido + decrementar inventario + crear factura
- Transferir dinero entre cuentas

**Ejemplos que NO necesitan UoW:**
- Obtener usuario por ID
- Listar usuarios paginados
- Actualizar un solo campo de una tabla

---

## Repositorio genérico vs específico

```csharp
// ❌ Repositorio genérico — cómodo al principio, problemático después
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct);
    Task InsertAsync(T entity, CancellationToken ct);
    Task UpdateAsync(T entity, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
// Problemas: GetAll sin paginación, no permite queries específicos de cada entidad,
// las diferencias entre entidades fuerzan hacks en la interfaz genérica

// ✓ Repositorio específico — expone exactamente lo que cada entidad necesita
public interface IExampleUserRepository
{
    Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct);
    Task<ExampleUser?> GetByEmailAsync(string email, CancellationToken ct);
    Task<(IEnumerable<ExampleUser> Items, int Total)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct);
    Task InsertAsync(ExampleUser user, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
}
// Cada método tiene nombre y parámetros que reflejan el dominio
```

---

## Registro DI

```csharp
// Infrastructure/ServiceCollectionEx.cs
public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services, IConfiguration configuration)
{
    // Repositorios — Scoped (viven por request)
    services.AddScoped<IExampleUserRepository, ExampleUserRepository>();

    // Unit of Work — Scoped (misma instancia por request que los repositorios)
    services.AddScoped<IUnitOfWork, DapperUnitOfWork>();

    // Sql classes — Scoped
    services.AddScoped<ExampleUsersSql>();

    return services;
}
```

---

## Repository con EF Core
> Fuente: *Entity Framework Core in Action* (Smith) — Ch.2 Querying; Ch.11 Repository Pattern

Con EF Core el Repository encapsula el `DbContext` y usa LINQ en lugar de SQL crudo. Esto cambia cómo se construyen los queries pero no el contrato de la interfaz.

```csharp
// La interfaz en Domain es la misma — no importa si la implementación usa Dapper o EF Core
public interface IExampleUserRepository
{
    Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct);
    Task<(IEnumerable<ExampleUser> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken ct);
    Task InsertAsync(ExampleUser user, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
}

// Infrastructure/Repositories/ExampleUsers/EfExampleUserRepository.cs
public sealed class EfExampleUserRepository : IExampleUserRepository
{
    private readonly AppDbContext _db;
    public EfExampleUserRepository(AppDbContext db) => _db = db;

    public Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
        => _db.ExampleUsers
              .AsNoTracking()
              .FirstOrDefaultAsync(u => u.PublicId == publicId, ct);

    public async Task<(IEnumerable<ExampleUser> Items, int Total)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct)
    {
        var query = _db.ExampleUsers.AsNoTracking();
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task InsertAsync(ExampleUser user, CancellationToken ct)
    {
        _db.ExampleUsers.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
        => _db.ExampleUsers.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);
}
```

### AsNoTracking — cuándo usarlo

```csharp
// ✓ AsNoTracking para Queries — no necesitan change tracking
var user = await _db.ExampleUsers
    .AsNoTracking()                  // EF no rastrea cambios → más rápido
    .FirstOrDefaultAsync(u => u.PublicId == id, ct);

// ✗ No usar AsNoTracking para Commands — EF necesita rastrear cambios para SaveChanges
var user = await _db.ExampleUsers
    .FirstOrDefaultAsync(u => u.PublicId == id, ct);   // ← tracking ON
user.UpdateEmail(newEmail);
await _db.SaveChangesAsync(ct);                        // ← detecta el cambio y genera UPDATE
```

| Escenario | ¿Tracking? |
|-----------|------------|
| Query (solo lectura) | `AsNoTracking()` — más rápido, menos memoria |
| Command (va a modificar) | Sin `AsNoTracking` — EF detecta los cambios |
| Proyección a DTO | No aplica — EF no trackea tipos no mapeados |

### Unit of Work con EF Core

EF Core ya implementa Unit of Work internamente a través del `DbContext`. `SaveChangesAsync` actúa como el "commit" de todas las operaciones pendientes en una transacción.

```csharp
// Con EF Core, el DbContext ES el Unit of Work
// Todos los cambios en la misma instancia se guardan juntos en SaveChangesAsync

public sealed class RegisterUserHandler
    : IRequestHandler<RegisterUserRequest, RegisterUserResponse>
{
    private readonly AppDbContext _db;

    public async Task<RegisterUserResponse> Handle(
        RegisterUserRequest request, CancellationToken ct)
    {
        // Ambas operaciones se acumulan en el DbContext
        var user    = ExampleUser.Create(request.FullName, request.Email);
        var profile = UserProfile.Create(user.PublicId, request.BirthDate);

        _db.ExampleUsers.Add(user);
        _db.UserProfiles.Add(profile);

        // UN solo SaveChangesAsync — ambas inserciones en la misma transacción
        await _db.SaveChangesAsync(ct);

        return new RegisterUserSuccess(new ExampleUserDto(user));
    }
}
```

```csharp
// Para transacciones explícitas con EF Core (casos complejos)
await using var tx = await _db.Database.BeginTransactionAsync(ct);
try
{
    _db.ExampleUsers.Add(user);
    await _db.SaveChangesAsync(ct);

    // Operación que podría fallar
    await _externalService.NotifyUserCreatedAsync(user.PublicId, ct);

    await tx.CommitAsync(ct);
}
catch
{
    await tx.RollbackAsync(ct);
    throw;
}
```


---

## Glosario

| Término | Definición |
|---------|-----------|
| Repository Pattern | Abstracción que desacopla el acceso a datos del dominio — la interfaz vive en Domain, la implementación en Infrastructure |
| IExampleUserRepository | Interfaz de repositorio que define las operaciones de acceso a datos para ExampleUser |
| ExampleUsersSql | Clase estática que centraliza las consultas SQL parametrizadas del repositorio |
| Unit of Work | Patrón que agrupa varias operaciones en una transacción atómica — commit o rollback conjunto |
| IUnitOfWork | Interfaz con BeginTransactionAsync, CommitAsync y RollbackAsync |
| DapperUnitOfWork | Implementación de IUnitOfWork usando conexiones Dapper y transacciones ADO.NET |
| AsNoTracking | Modificador de EF Core que evita el seguimiento de entidades para lecturas de solo lectura |
| SaveChangesAsync | Método de DbContext que persiste todos los cambios trackeados en la base de datos |
| DIP | Dependency Inversion Principle — las capas superiores dependen de abstracciones, no de implementaciones concretas |
| Captive Dependency | Error donde un servicio Singleton retiene una dependencia Scoped, causando bugs de estado compartido |
| Dapper | Micro-ORM liviano que mapea resultados SQL a objetos C# con mínima configuración |
| DbContext | Clase de EF Core que representa la sesión con la base de datos y rastreo de cambios |

---

*Rogelio Arriaga Gonzalez*
