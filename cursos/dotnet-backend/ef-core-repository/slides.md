<!-- _class: lead -->

# EF Core 10  -  Repository Pattern  -  Unit of Work

## Material de auto-estudio  -  Sistema de mensajería
Rogelio Arriaga  -  Raptor Dev Services  -  Mayo 2026

---

<!-- _class: lead -->

# Parte 1
## EF Core: DbContext, DbSet, Change Tracker y Migrations

---

## ¿Qué es EF Core?

EF Core es el ORM oficial de .NET. Gestiona tres responsabilidades clave:

| Responsabilidad | Qué hace |
|-----------------|----------|
| **Mapeo O/R** | Convierte clases C# ↔ tablas de la base de datos |
| **Change Tracking** | Detecta qué entidades cambiaron para generar el SQL correcto |
| **Migrations** | Genera y aplica cambios de esquema incrementales |

**DbContext** es la clase central: es tu unidad de trabajo y tu sesión con la base de datos.

**DbSet\<T\>** representa una tabla: `DbSet<Contact>` = tabla `Contacts`.

---

## AppDbContext  -  estructura real del back-template

```csharp
// Shared/Database/AppDbContext.cs
using Authentication.Domain.Entities;
using Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Tenancy.Domain.Entities;
using Users.Domain.Entities;

namespace Shared.Database;

public sealed class AppDbContext : DbContext
{
    private readonly ITenantContextAccessor _tenantAccessor;

    public DbSet<Tenant>         Tenants      { get; set; } = null!;
    public DbSet<UserCredential> Credentials  { get; set; } = null!;
    public DbSet<UserProfile>    UserProfiles { get; set; } = null!;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantContextAccessor tenantAccessor) : base(options)
    {
        _tenantAccessor = tenantAccessor;
    }
```

---

## AppDbContext  -  OnModelCreating y filtros globales

```csharp
    // Shared/Database/AppDbContext.cs (continuación)
    private long CurrentTenantId =>
        long.TryParse(_tenantAccessor.Current?.TenantId, out var id) ? id : 0L;

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("dbo");
        mb.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);

        // HasQueryFilter  -  filtro global automático por tenant
        // Todos los queries de UserProfile ya llevan WHERE TenantId = @tenantId
        mb.Entity<UserProfile>()
            .HasQueryFilter(e => e.TenantId == CurrentTenantId);
    }
}
```

> `HasQueryFilter` aplica automáticamente el filtro multi-tenant en cada query.
> Los repositorios reciben `tenantId` en su firma para claridad semántica,
> pero el filtro efectivo ya lo impone el DbContext.

---

## Change Tracker: cómo EF sabe qué guardar

El Change Tracker es el motor interno que observa las entidades cargadas y determina qué SQL ejecutar en `SaveChangesAsync`.

| Estado | Cuándo ocurre | SQL generado |
|--------|---------------|--------------|
| `Added` | `_db.Contacts.Add(entity)` | `INSERT` |
| `Modified` | Se modifica una propiedad tracked | `UPDATE` |
| `Deleted` | `_db.Contacts.Remove(entity)` | `DELETE` |
| `Unchanged` | Cargada pero sin cambios | Nada |
| `Detached` | No está siendo tracked | Nada |

> **Regla práctica:** si vas a modificar una entidad, cárgala **sin** `AsNoTracking`.
> Si solo vas a leerla, úsalo siempre.

---

<!-- _class: lead -->

# Parte 2
## Configuración: DbContext en Program.cs, connection string y pooling

---

## Registro del DbContext  -  Host/Program.cs

```csharp
// Host/Program.cs
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
```

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection":
      "Host=localhost;Port=5432;Database=mensajeria;Username=crm_user;Password=secret"
  }
}
```

---

## AddDbContext vs AddDbContextPool

```csharp
// AddDbContext  -  nueva instancia por cada request (Scoped)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// AddDbContextPool  -  reutiliza instancias; la instancia se resetea entre usos
builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseNpgsql(connectionString),
    poolSize: 128);
```

| | `AddDbContext` | `AddDbContextPool` |
|-|----------------|--------------------|
| Lifecycle | Scoped (nueva instancia) | Pool reutilizable |
| Overhead | Más alto | Más bajo |
| Estado compartido | No (cada req tiene su ctx) | No (se resetea entre usos) |
| Limitación |  -  | No soporta Scoped en el constructor del DbContext |

> `AddDbContextPool` no es compatible con la inyección de `ITenantContextAccessor`
> si es Scoped. En el back-template se usa `AddDbContext`.

---

<!-- _class: lead -->

# Parte 3
## Entidades del dominio: propiedades con `init`

---

## UserProfile  -  entidad real del back-template

```csharp
// Modules/Users/Users.Domain/Entities/UserProfile.cs
namespace Users.Domain.Entities;

public sealed class UserProfile
{
    public long     Id           { get; init; }
    public Guid     PublicId     { get; init; }
    public long     TenantId     { get; init; }
    public string   FullName     { get; init; } = string.Empty;
    public bool     IsActive     { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
```

> Las propiedades usan `init`, no `set`. La entidad se construye de una vez
> y EF Core la hidrata correctamente al leer de la base de datos.
> No hay factory method `Create(...)`  -  el repositorio construye el objeto
> con object initializer directamente.

---

## Equivalente Sistema de mensajería  -  Contact

```csharp
// Modules/Contacts/Contacts.Domain/Entities/Contact.cs
namespace Contacts.Domain.Entities;

public sealed class Contact
{
    public long     Id           { get; init; }
    public Guid     PublicId     { get; init; }
    public long     TenantId     { get; init; }
    public string   FullName     { get; init; } = string.Empty;
    public string   Email        { get; init; } = string.Empty;
    public string   Phone        { get; init; } = string.Empty;
    public bool     IsActive     { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
```

> Misma estructura que `UserProfile`  -  mismo patrón en cada entidad del sistema.

---

<!-- _class: lead -->

# Parte 4
## EntityTypeConfiguration: mapeo explícito con Fluent API

---

## IEntityTypeConfiguration\<T\>

Separa la configuración de mapeo de la entidad. Una clase por entidad, registradas automáticamente con `ApplyConfigurationsFromAssembly`.

```csharp
// Modules/Contacts/Contacts.Infrastructure/Configurations/ContactConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Contacts.Domain.Entities;

namespace Contacts.Infrastructure.Configurations;

public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.PublicId).IsRequired()
               .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.FullName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).HasMaxLength(320);
        builder.HasIndex(c => c.Email).IsUnique();
        builder.HasIndex(c => c.PublicId).IsUnique();
    }
}
```

---

## Convenciones vs configuración explícita

| Escenario | Convención de EF Core | Configuración explícita necesaria |
|-----------|----------------------|-----------------------------------|
| PK | Propiedad `Id` -> PK automática | `builder.HasKey(...)` si el nombre difiere |
| Nombre de tabla | Nombre del DbSet | `builder.ToTable("nombre")` |
| Longitud de string | `text` en Postgres | `builder.Property(...).HasMaxLength(200)` |
| Índice único | No | `builder.HasIndex(...).IsUnique()` |
| Valor por defecto | No | `builder.Property(...).HasDefaultValueSql(...)` |
| Relaciones | Por navegación | `builder.HasMany(...).WithOne(...)` |

> Preferir configuración explícita en proyectos productivos. Las convenciones ocultan intención.

---

<!-- _class: lead -->

# Parte 5
## Consultas: AsNoTracking, Select, Include y HasQueryFilter

---

## AsNoTracking  -  queries de solo lectura

```csharp
//  Query de lista  -  solo lectura, sin tracking
var contacts = await _db.Contacts
    .AsNoTracking()
    .Where(c => c.IsActive)
    .OrderByDescending(c => c.CreatedAtUtc)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(ct);
```

```csharp
//  Proyección a DTO  -  EF genera SELECT con las columnas exactas
var dtos = await _db.Contacts
    .AsNoTracking()
    .Where(c => c.IsActive)
    .Select(c => new ContactDto(c.PublicId, c.FullName, c.Email))
    .ToListAsync(ct);
```

> `AsNoTracking` elimina el overhead del Change Tracker.
> Úsalo siempre en queries de solo lectura.

---

## Include  -  eager loading para evitar N+1

```csharp
//  N+1  -  un query por cada conversación para cargar sus mensajes
var convs = await _db.Conversations.ToListAsync(ct);
foreach (var conv in convs)
{
    var msgs = conv.Messages; // N queries adicionales
}
```

```csharp
//  Eager loading con Include  -  un solo JOIN
var convs = await _db.Conversations
    .AsNoTracking()
    .Include(c => c.Messages)
    .Where(c => c.AgentId == agentId)
    .ToListAsync(ct);
```

```csharp
//  ThenInclude para relaciones anidadas
var convs = await _db.Conversations
    .AsNoTracking()
    .Include(c => c.Messages)
        .ThenInclude(m => m.Attachments)
    .ToListAsync(ct);
```

---

## HasQueryFilter  -  filtros globales automáticos

En el back-template el multi-tenant se implementa con `HasQueryFilter` en `AppDbContext`. Todos los queries de esa entidad ya traen `WHERE TenantId = @tenantId` sin código adicional.

```csharp
// Shared/Database/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder mb)
{
    mb.ApplyConfigurationsFromAssembly(
        typeof(AppDbContext).Assembly);

    // El filtro lee el tenant del request actual vía ITenantContextAccessor
    mb.Entity<UserProfile>()
        .HasQueryFilter(e => e.TenantId == CurrentTenantId);
}
```

```csharp
// Cuando necesitas ignorar el filtro (admin, auditoría):
var todos = await _db.UserProfiles
    .IgnoreQueryFilters()
    .Where(e => !e.IsActive)
    .ToListAsync(ct);
```

---

<!-- _class: lead -->

# Parte 6
## Migraciones: Add-Migration, Update-Database, rollback y producción

---

## Comandos esenciales de migraciones

```bash
# Crear una nueva migration
dotnet ef migrations add AgregarTablaContacts \
    --project Modules/Contacts/Contacts.Infrastructure \
    --startup-project Host

# Aplicar migraciones pendientes a la DB local
dotnet ef database update \
    --project Modules/Contacts/Contacts.Infrastructure \
    --startup-project Host

# Ver el historial de migraciones aplicadas
dotnet ef migrations list

# Rollback: volver a una migration anterior (por nombre)
dotnet ef database update MigrationAnterior

# Eliminar la última migration (solo si aún NO se aplicó a la DB)
dotnet ef migrations remove
```

---

## Estrategias para producción

| Estrategia | Cuándo usarla |
|------------|---------------|
| `context.Database.MigrateAsync()` en startup | Apps pequeñas, una sola instancia. Riesgoso con múltiples instancias (race condition). |
| Script SQL revisado en el pipeline | Producción. La migration se aprueba antes del deploy. |
| Herramientas dedicadas (Flyway, Liquibase) | Equipos con DBA, bases de datos grandes. |

```bash
# Generar script SQL idempotente (seguro de re-ejecutar en CI/CD)
dotnet ef migrations script --idempotent \
    --project Modules/Contacts/Contacts.Infrastructure \
    --startup-project Host \
    -o migrate.sql
```

```csharp
// Solo para desarrollo/staging: aplicar en startup
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.MigrateAsync();
```

---

<!-- _class: lead -->

# Parte 7
## Repository Pattern: encapsular EF Core detrás de interfaces

---

## El problema: handler acoplado a la persistencia

```csharp
//  Handler conoce AppDbContext directamente  -  viola DIP, no testeable
using Shared.Database;

public sealed class GetContactHandler
{
    private readonly AppDbContext _db;

    public GetContactHandler(AppDbContext db) => _db = db;

    public async Task<ContactDto?> Handle(
        GetContactRequest req, CancellationToken ct)
    {
        var contact = await _db.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.PublicId == req.PublicId, ct);

        return contact is null ? null : new ContactDto(contact);
    }
}
// Para testear: necesitas una base de datos real o InMemory.
// Para cambiar de PostgreSQL a otro motor: modificas todos los handlers.
```

---

## La solución: interfaz en Domain, implementación en Infrastructure

```
Modules/
`-- Contacts/
    |-- Contacts.Domain/
    |   `-- Repositories/
    |       `-- IContactRepository.cs   <- interfaz  -  sin referencias a EF Core
    |-- Contacts.Application/
    |   `-- UseCases/
    |       `-- GetContact/
    |           `-- GetContactHandler.cs <- solo conoce IContactRepository
    `-- Contacts.Infrastructure/
        `-- Repositories/
            `-- ContactRepository.cs    <- implementación con AppDbContext
```

> **DIP (Dependency Inversion):** el handler depende de la abstracción (`IContactRepository`),
> no de la concreción (`AppDbContext`).

---

## IContactRepository  -  interfaz en Domain

La interfaz vive en el proyecto de dominio. No tiene referencias a EF Core ni a `Shared.Database`.

```csharp
// Modules/Contacts/Contacts.Domain/Repositories/IContactRepository.cs
using Contacts.Domain.Entities;

namespace Contacts.Domain.Repositories;

public interface IContactRepository
{
    Task<Contact?> GetByPublicIdAsync(
        Guid publicId, long tenantId,
        CancellationToken ct = default);

    Task<IReadOnlyCollection<Contact>> GetPagedAsync(
        long tenantId, int page, int pageSize,
        CancellationToken ct = default);

    Task<int> GetCountAsync(long tenantId, CancellationToken ct = default);

    Task InsertAsync(
        Guid publicId, long tenantId, string fullName, string email,
        CancellationToken ct = default);

    Task<bool> UpdateAsync(
        Guid publicId, long tenantId, string fullName,
        CancellationToken ct = default);

    Task<bool> DisableAsync(
        Guid publicId, long tenantId,
        CancellationToken ct = default);
}
```

---

## Firma de métodos  -  parámetros escalares, no entidades

Observa el patrón real del back-template en `IUserProfileRepository`:

```csharp
// Modules/Users/Users.Domain/Repositories/IUserProfileRepository.cs
using Users.Domain.Entities;

namespace Users.Domain.Repositories;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByPublicIdAsync(
        Guid publicId, long tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserProfile>> GetPagedAsync(
        long tenantId, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(long tenantId,
        CancellationToken cancellationToken = default);

    Task InsertAsync(Guid publicId, long tenantId, string fullName,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Guid publicId, long tenantId, string fullName,
        CancellationToken cancellationToken = default);

    Task<bool> DisableAsync(Guid publicId, long tenantId,
        CancellationToken cancellationToken = default);
}
```

> `InsertAsync` recibe parámetros escalares  -  el repositorio construye la entidad internamente.

---

## Implementación  -  ContactRepository.cs (parte 1)

```csharp
// Modules/Contacts/Contacts.Infrastructure/Repositories/ContactRepository.cs
using Contacts.Domain.Entities;
using Contacts.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Database;

namespace Contacts.Infrastructure.Repositories;

public sealed class ContactRepository : IContactRepository
{
    private readonly AppDbContext _db;

    public ContactRepository(AppDbContext db) => _db = db;

    public Task<Contact?> GetByPublicIdAsync(
        Guid publicId, long tenantId,
        CancellationToken ct = default) =>
        _db.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.PublicId == publicId && e.IsActive, ct);

    public async Task<IReadOnlyCollection<Contact>> GetPagedAsync(
        long tenantId, int page, int pageSize,
        CancellationToken ct = default) =>
        await _db.Contacts
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(ct);
```

---

## Implementación  -  ContactRepository.cs (parte 2)

```csharp
    // InsertAsync  -  construye la entidad internamente con object initializer
    public async Task InsertAsync(
        Guid publicId, long tenantId, string fullName, string email,
        CancellationToken ct = default)
    {
        var contact = new Contact
        {
            PublicId     = publicId,
            TenantId     = tenantId,
            FullName     = fullName,
            Email        = email,
            IsActive     = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync(ct);
    }
```

---

## Implementación  -  ContactRepository.cs (parte 3)

```csharp
    // ExecuteUpdateAsync  -  update sin cargar la entidad (EF Core 7+)
    public async Task<bool> UpdateAsync(
        Guid publicId, long tenantId, string fullName,
        CancellationToken ct = default)
    {
        var rows = await _db.Contacts
            .Where(e => e.PublicId == publicId && e.IsActive)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.FullName,     fullName)
                .SetProperty(e => e.UpdatedAtUtc, DateTime.UtcNow), ct);

        return rows > 0;
    }

    // Soft delete con ExecuteUpdateAsync
    public async Task<bool> DisableAsync(
        Guid publicId, long tenantId,
        CancellationToken ct = default)
    {
        var rows = await _db.Contacts
            .Where(e => e.PublicId == publicId && e.IsActive)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.IsActive,     false)
                .SetProperty(e => e.UpdatedAtUtc, DateTime.UtcNow), ct);

        return rows > 0;
    }
}
```

---

## ExecuteUpdateAsync vs cargar + modificar

`ExecuteUpdateAsync` (EF Core 7+) ejecuta un `UPDATE` directo sin cargar la entidad en memoria.

```csharp
//  Patrón clásico  -  dos roundtrips a la DB + overhead del Change Tracker
var contact = await _db.Contacts
    .FirstOrDefaultAsync(e => e.PublicId == publicId, ct);

if (contact is null) return false;

// No funciona con init  -  tendrías que cambiar init a set
contact.FullName = fullName;
await _db.SaveChangesAsync(ct);
```

```csharp
//  ExecuteUpdateAsync  -  un solo roundtrip, sin Change Tracker
var rows = await _db.Contacts
    .Where(e => e.PublicId == publicId && e.IsActive)
    .ExecuteUpdateAsync(s => s
        .SetProperty(e => e.FullName,     fullName)
        .SetProperty(e => e.UpdatedAtUtc, DateTime.UtcNow), ct);

return rows > 0;
```

> Con entidades `init`-only, `ExecuteUpdateAsync` es la única forma de actualizar
> sin cambiar las propiedades a `set`.

---

## Multi-tenancy en queries  -  regla de aislamiento

El `HasQueryFilter` del `AppDbContext` filtra por tenant automáticamente.
El parámetro `tenantId` en la firma del repositorio documenta la intención, pero el filtro efectivo ya lo aplica el contexto.

```csharp
//  Sin tenantId en la firma  -  ambiguo, el lector no sabe que hay filtro global
Task<Contact?> GetByPublicIdAsync(Guid publicId, CancellationToken ct);

//  Con tenantId en la firma  -  el contrato es explícito sobre el aislamiento
Task<Contact?> GetByPublicIdAsync(
    Guid publicId, long tenantId, CancellationToken ct);
```

```csharp
//  Filtro adicional innecesario (el HasQueryFilter ya lo hace)
_db.Contacts.Where(c => c.TenantId == tenantId && c.PublicId == publicId)

//  EF Core aplica HasQueryFilter + el Where explícito que necesitas
_db.Contacts.Where(c => c.PublicId == publicId && c.IsActive)
```

---

## Registro DI  -  Infrastructure/ServiceCollectionEx.cs

```csharp
// Modules/Contacts/Contacts.Infrastructure/ServiceCollectionEx.cs
using Contacts.Domain.Repositories;
using Contacts.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Contacts.Infrastructure;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddContactsInfrastructureServices(
        this IServiceCollection services)
    {
        services.AddScoped<IContactRepository, ContactRepository>();
        return services;
    }
}
```

> Cada módulo registra sus propios repositorios.
> Lifetime **Scoped**: una instancia por request HTTP, comparte el `AppDbContext` del request.

---

<!-- _class: lead -->

# Parte 8
## Result Pattern: Response, ISuccess, IFailure, INotFoundFailure

---

## El contrato de respuesta  -  abstract record

Cada caso de uso tiene su propia jerarquía de respuesta. No hay `Result<T>` genérico.

```csharp
// Modules/Contacts/Contacts.Application/UseCases/GetContact/
//   Responses/GetContactResponse.cs
using Common.Messaging;

namespace Contacts.Application.UseCases.GetContact.Responses;

public abstract record GetContactResponse : IResponse;
```

```csharp
// Responses/GetContactSuccess.cs
using Common.Results;
using Contacts.Contracts.Dtos;

namespace Contacts.Application.UseCases.GetContact.Responses;

public sealed record GetContactSuccess(ContactDto Data)
    : GetContactResponse, ISuccess<ContactDto>;
```

```csharp
// Responses/GetContactNotFoundFailure.cs
using Common.Results;

namespace Contacts.Application.UseCases.GetContact.Responses;

public sealed record GetContactNotFoundFailure(string Message)
    : GetContactResponse, INotFoundFailure;
```

---

## Interfaces de resultado  -  Common/Common/Results/

```csharp
// Common/Common/Results/ISuccess.cs
namespace Common.Results;

public interface ISuccess { }

public interface ISuccess<T> : ISuccess
{
    T Data { get; }
}
```

```csharp
// Common/Common/Results/IFailure.cs
namespace Common.Results;

public interface IFailure
{
    string Message { get; }
}

public interface IValidationFailure : IFailure { }
public interface INotFoundFailure   : IFailure { }
public interface IConflictFailure   : IFailure { }
```

> `INotFoundFailure` existe y se usa. `GetContactNotFoundFailure` lo implementa.

---

## Handler  -  solo conoce la interfaz del repositorio

```csharp
// Modules/Contacts/Contacts.Application/UseCases/GetContact/
//   GetContactHandler.cs
using Common.Messaging;
using Contacts.Application.UseCases.GetContact.Responses;
using Contacts.Contracts.Dtos;
using Contacts.Domain.Repositories;

namespace Contacts.Application.UseCases.GetContact;

public sealed class GetContactHandler
    : IRequestHandler<GetContactRequest, GetContactResponse>
{
    private readonly IContactRepository _contacts;

    public GetContactHandler(IContactRepository contacts)
        => _contacts = contacts;

    public async Task<GetContactResponse> Handle(
        GetContactRequest request, CancellationToken cancellationToken)
    {
        var contact = await _contacts.GetByPublicIdAsync(
            request.PublicId, request.TenantId, cancellationToken);

        if (contact is null)
            return new GetContactNotFoundFailure(
                "Contacto no encontrado.");

        return new GetContactSuccess(
            new ContactDto(contact.PublicId, contact.FullName, contact.Email));
    }
}
```

---

## Handler de update  -  retorno bool desde repositorio

El repositorio devuelve `bool` (filas afectadas > 0). El handler lo convierte en la respuesta del caso de uso.

```csharp
// Modules/Contacts/Contacts.Application/UseCases/UpdateContact/
//   UpdateContactHandler.cs
using Common.Messaging;
using Contacts.Application.UseCases.UpdateContact.Responses;
using Contacts.Domain.Repositories;

namespace Contacts.Application.UseCases.UpdateContact;

public sealed class UpdateContactHandler
    : IRequestHandler<UpdateContactRequest, UpdateContactResponse>
{
    private readonly IContactRepository _contacts;

    public UpdateContactHandler(IContactRepository contacts)
        => _contacts = contacts;

    public async Task<UpdateContactResponse> Handle(
        UpdateContactRequest request, CancellationToken cancellationToken)
    {
        var updated = await _contacts.UpdateAsync(
            request.PublicId, request.TenantId,
            request.FullName, cancellationToken);

        return updated
            ? new UpdateContactSuccess()
            : new UpdateContactNotFoundFailure("Contacto no encontrado.");
    }
}
```

---

## Presenter  -  pattern matching sobre la respuesta

```csharp
// Modules/Contacts/Contacts.Presentation/Presenters/
//   GetContactPresenter.cs
using Common.Messaging;
using Common.Results;
using Common.ViewModels;
using Contacts.Application.UseCases.GetContact.Responses;
using Contacts.Contracts.Dtos;
using Contacts.Presentation;

namespace Contacts.Presentation.Presenters;

public sealed class GetContactPresenter
    : INotificationHandler<GetContactResponse>
{
    private readonly ResultViewModel<ContactsController> _viewModel;

    public GetContactPresenter(
        ResultViewModel<ContactsController> viewModel)
        => _viewModel = viewModel;

    public Task Handle(
        GetContactResponse notification, CancellationToken cancellationToken)
    {
        if (notification is IFailure failure)
            _viewModel.Fail(failure.Message);
        else if (notification is ISuccess<ContactDto> success)
            _viewModel.Set(success);

        return Task.CompletedTask;
    }
}
```

---

<!-- _class: lead -->

# Parte 9
## Unit of Work: SaveChangesAsync como UoW implícito, y cuándo necesitas uno explícito

---

## UoW implícito: el DbContext como unidad de trabajo

EF Core implementa Unit of Work internamente. El `DbContext` acumula todos los cambios y los persiste juntos en `SaveChangesAsync` dentro de una sola transacción.

```csharp
// InsertAsync en ContactRepository  -  un solo SaveChangesAsync
public async Task InsertAsync(
    Guid publicId, long tenantId, string fullName, string email,
    CancellationToken ct = default)
{
    var contact = new Contact
    {
        PublicId     = publicId,
        TenantId     = tenantId,
        FullName     = fullName,
        Email        = email,
        IsActive     = true,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow,
    };

    _db.Contacts.Add(contact);
    await _db.SaveChangesAsync(ct);
}
```

---

## UoW implícito: dos entidades en la misma transacción

Cuando el handler llama a dos repositorios que comparten el mismo `AppDbContext` (Scoped), se puede llamar `SaveChangesAsync` una sola vez.

```csharp
// Modules/Conversations/Conversations.Application/
//   UseCases/CreateConversation/CreateConversationHandler.cs

public async Task<CreateConversationResponse> Handle(
    CreateConversationRequest request, CancellationToken ct)
{
    // Ambos repositorios comparten el mismo AppDbContext Scoped
    await _conversations.AddAsync(
        request.PublicId, request.TenantId, request.ContactId, ct);

    await _messages.AddInitialMessageAsync(
        request.PublicId, "Conversación iniciada.", ct);

    // UN solo SaveChangesAsync  -  ambas inserciones en la misma transacción
    // (si los repos delegan el SaveChanges al handler o a un UoW explícito)
    return new CreateConversationSuccess(request.PublicId);
}
```

> En el back-template cada repositorio llama a su propio `SaveChangesAsync`.
> Para operaciones atómicas entre dos repositorios, usa transacción explícita.

---

## UoW explícito: transacciones con operaciones externas

Necesitas transacción explícita cuando la operación incluye llamadas fuera del DbContext.

```csharp
// Transacción explícita con EF Core
await using var tx = await _db.Database.BeginTransactionAsync(ct);
try
{
    var conversation = new Conversation
    {
        PublicId  = Guid.NewGuid(),
        TenantId  = request.TenantId,
        ContactId = request.ContactId,
    };

    _db.Conversations.Add(conversation);
    await _db.SaveChangesAsync(ct);   // primer SaveChanges

    // Llamada externa  -  si falla, el rollback deshace el SaveChanges anterior
    await _channelService.NotifyAsync(conversation.PublicId, ct);

    await tx.CommitAsync(ct);
    return new CreateConversationSuccess(conversation.PublicId);
}
catch
{
    await tx.RollbackAsync(ct);
    throw;
}
```

---

## ¿Cuándo necesitas UoW explícito?

| Escenario | UoW implícito | UoW explícito |
|-----------|:---:|:---:|
| Crear un Contact |  |  -  |
| Crear Contact + ContactNote (mismo ctx) |  |  -  |
| Crear Conversation + notificar canal externo |  -  |  |
| Soft delete de Conversation (un update) |  |  -  |
| Importar CSV de 500 contactos con rollback parcial |  -  |  |
| Transferir conversación entre agentes (dos updates) |  |  -  |

```
Operación en UNA entidad:         repo.InsertAsync -> SaveChanges  -> sin UoW explícito
Operación en DOS+ entidades:      _db.X.Add() + _db.Y.Add() + SaveChanges -> UoW implícito OK
Operación con servicio externo:   BeginTransaction -> SaveChanges -> ExternalCall -> Commit
```

---

<!-- _class: lead -->

# Parte 10
## Performance: AsNoTracking, proyección, N+1 y ExecuteUpdateAsync

---

## Las cuatro reglas de performance con EF Core

**Regla 1  -  AsNoTracking en todo query de lectura**

```csharp
//  Sin tracking: EF no aloca memoria para rastrear el estado de la entidad
public async Task<IReadOnlyCollection<Contact>> GetPagedAsync(
    long tenantId, int page, int pageSize,
    CancellationToken ct = default) =>
    await _db.Contacts
        .AsNoTracking()
        .Where(e => e.IsActive)
        .OrderByDescending(e => e.CreatedAtUtc)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToArrayAsync(ct);
```

> El back-template usa `.ToArrayAsync()`  -  devuelve `IReadOnlyCollection<T>`,
> más restrictivo y semánticamente correcto para resultados de repositorio.

---

## Regla 2  -  Select para proyección a DTO

```csharp
//  Proyección en la query  -  EF genera SELECT con las columnas exactas
var dtos = await _db.Contacts
    .AsNoTracking()
    .Where(c => c.IsActive)
    .Select(c => new ContactDto(c.PublicId, c.FullName, c.Email))
    .ToListAsync(ct);
```

```csharp
//  Proyección en memoria  -  trae todas las columnas y luego filtra
var contacts = await _db.Contacts.ToListAsync(ct);
var dtos = contacts.Select(c =>
    new ContactDto(c.PublicId, c.FullName, c.Email));
```

> Proyectar en la query puede reducir los bytes transferidos de la DB a la mitad
> cuando la entidad tiene columnas como `BlobContent`, `Notes`, etc.

---

## Regla 3  -  Include para evitar N+1

```csharp
//  N+1: un query adicional por cada Contact para cargar sus Conversations
var contacts = await _db.Contacts.ToListAsync(ct);
foreach (var contact in contacts)
{
    var count = contact.Conversations.Count; // Query por cada contact
}
```

```csharp
//  Un solo JOIN con Include
var contacts = await _db.Contacts
    .AsNoTracking()
    .Include(c => c.Conversations)
    .Where(c => c.IsActive)
    .ToListAsync(ct);

//  AsSplitQuery cuando el JOIN genera producto cartesiano grande
var contacts = await _db.Contacts
    .AsNoTracking()
    .Include(c => c.Conversations)
        .ThenInclude(cv => cv.Messages)
    .AsSplitQuery()
    .ToListAsync(ct);
```

---

## Regla 4  -  ExecuteUpdateAsync para updates masivos

```csharp
//  Cargar + modificar: dos roundtrips + overhead del Change Tracker
var contact = await _db.Contacts
    .FirstOrDefaultAsync(e => e.PublicId == publicId, ct);
if (contact is null) return false;

// Además: no funciona con propiedades init  -  necesitarías cambiarlas a set
contact.FullName = fullName; // <- error de compilación con init
await _db.SaveChangesAsync(ct);
```

```csharp
//  ExecuteUpdateAsync  -  un solo roundtrip, SQL directo, sin Change Tracker
var rows = await _db.Contacts
    .Where(e => e.PublicId == publicId && e.IsActive)
    .ExecuteUpdateAsync(s => s
        .SetProperty(e => e.FullName,     fullName)
        .SetProperty(e => e.UpdatedAtUtc, DateTime.UtcNow), ct);

return rows > 0;
```

---

## EF Core vs Dapper: estrategia mixta

| Criterio | EF Core | Dapper |
|----------|---------|--------|
| CRUD con relaciones |  LINQ fluido |  -  SQL manual |
| Migrations y schema |  Integrado | [Err] Manual |
| Change tracking y auditoría global |  Automático | [Err] Lógica en cada repo |
| Queries complejos / reportes / FTS |  -  Difícil de optimizar |  SQL completo |
| Performance en reads masivos | Aceptable con AsNoTracking |  Más rápido |
| Multi-tenancy vía HasQueryFilter |  Automático | [Err] Cláusula manual en cada query |

**Estrategia recomendada para el sistema de mensajería:**
- **Commands (escrituras):** EF Core  -  change tracking, soft delete, auditoría automática
- **Queries complejos (bandeja, reportes, FTS):** Dapper  -  SQL directo, sin overhead

---

## Qué NO existe en este back-template

| Anti-patrón | Por qué no se usa |
|-------------|-------------------|
| `IRepository<T>` genérico | Interfaces específicas por módulo (`IContactRepository`, `IConversationRepository`) |
| `.Result` en código async | Bloquea el thread; siempre `await` |
| `async void` | No se puede await ni capturar excepciones |
| `string.Interpolation` en Serilog | Usar propiedades estructuradas: `Log.Info("Id: {Id}", id)` |
| `:latest` como tag de Docker | Usar versión semántica: `:v1.2.0` |
| Scoped dentro de Singleton | El Singleton duraría más que el Scoped  -  bug de tenant leakage |
| `DapperDbConnection` / clases `...Sql` | Usar Dapper con `IDbConnection` directo o encapsulado en el repositorio |
| `BranchId` como entidad | No existe en el modelo de dominio del sistema de mensajería |

---

## Resumen del módulo

| Concepto | Regla clave |
|----------|-------------|
| `AppDbContext` | Una sola instancia por request (Scoped); recibe `ITenantContextAccessor` |
| `HasQueryFilter` | Filtro multi-tenant automático; no repetir `WHERE TenantId` en cada query |
| `IEntityTypeConfiguration<T>` | Una clase por entidad en `*.Infrastructure/Configurations/` |
| Entidades con `init` | Las propiedades son inmutables; el repositorio construye con object initializer |
| `AsNoTracking` | Siempre en queries de solo lectura; `.ToArrayAsync()` devuelve `IReadOnlyCollection` |
| `ExecuteUpdateAsync` | Updates sin cargar la entidad; compatible con propiedades `init` |
| Soft delete | `ExecuteUpdateAsync` seteando `IsActive = false` |
| `IContactRepository` | Interfaz en `Domain/Repositories/`; implementación en `Infrastructure/Repositories/` |
| `IRepository<T>` genérico |  No existe  -  repositorios específicos por entidad |
| `InsertAsync` | Recibe parámetros escalares; el repositorio construye la entidad internamente |
| `INotFoundFailure` | Existe en `Common.Results`; lo implementan los `*NotFoundFailure` records |
| UoW implícito | `SaveChangesAsync` agrupa todos los cambios en una transacción |
| UoW explícito | `BeginTransactionAsync` cuando hay operaciones externas al DbContext |
