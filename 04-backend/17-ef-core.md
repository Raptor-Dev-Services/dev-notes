# 17 — Entity Framework Core en producción

EF Core es el ORM de .NET. Gestiona el mapeo entre clases C# y tablas de base de datos, el change tracking y las migrations. Este documento cubre su uso en producción con patrones del back-template.

---

## Workflow de migrations
> Fuente: *Entity Framework Core in Action* (Smith) — Ch.3 Migrations

```bash
# Crear una nueva migration
dotnet ef migrations add AgregarTablaExampleUsers --project src/GTM.Infrastructure

# Aplicar la migration a la DB local
dotnet ef database update --project src/GTM.Infrastructure

# Generar script SQL (para revisar y aplicar en producción)
dotnet ef migrations script --project src/GTM.Infrastructure

# Script idempotente — seguro de re-ejecutar (para CI/CD)
dotnet ef migrations script --idempotent -o migrate.sql

# Rollback a una migration anterior
dotnet ef database update MigrationAnterior

# Eliminar la última migration (si aún no se aplicó)
dotnet ef migrations remove
```

### Estrategias para aplicar migrations en producción

| Estrategia | Cuándo usarla |
|------------|--------------|
| `context.Database.Migrate()` en `Program.cs` | Apps pequeñas, una sola instancia. Riesgoso con múltiples instancias (condición de carrera). |
| Script SQL aprobado en el pipeline | Producción seria. La migration se revisa como parte del deploy. |
| Herramientas dedicadas (Flyway, Liquibase) | Equipos con DBA, bases de datos grandes con historial largo. |

```csharp
// Opción 1: aplicar en startup (solo para desarrollo/staging)
// Host/Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Opción 2 (recomendada para producción): generar script y aplicarlo en el pipeline
// El script idempotente verifica si cada migration ya fue aplicada antes de ejecutarla
```

---

## Configuración de entidades con Fluent API

```csharp
// Infrastructure/Persistence/Configurations/ExampleUserConfiguration.cs
public sealed class ExampleUserConfiguration : IEntityTypeConfiguration<ExampleUser>
{
    public void Configure(EntityTypeBuilder<ExampleUser> builder)
    {
        builder.ToTable("ExampleUsers", schema: "gtt");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.PublicId)
            .IsRequired()
            .HasDefaultValueSql("gen_random_uuid()");  // PostgreSQL

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex(u => u.PublicId)
            .IsUnique();

        // Value Object como owned entity
        builder.OwnsOne(u => u.Address, a =>
        {
            a.Property(x => x.Street).HasColumnName("AddressStreet").HasMaxLength(200);
            a.Property(x => x.City).HasColumnName("AddressCity").HasMaxLength(100);
            a.Property(x => x.ZipCode).HasColumnName("AddressZipCode").HasMaxLength(10);
        });
    }
}
```

```csharp
// AppDbContext registra todas las configuraciones automáticamente
public sealed class AppDbContext : DbContext
{
    public DbSet<ExampleUser> ExampleUsers => Set<ExampleUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas las IEntityTypeConfiguration en el assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
```

---

## Consultas eficientes
> Fuente: *Entity Framework Core in Action* (Smith) — Ch.2 Querying the Database

```csharp
// ✓ AsNoTracking para Queries de solo lectura — sin change tracking, más rápido
var users = await _db.ExampleUsers
    .AsNoTracking()
    .Where(u => u.IsActive)
    .OrderByDescending(u => u.CreatedAtUtc)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(ct);

// ✓ Proyección directa a DTO — EF solo carga los campos que necesita
var dtos = await _db.ExampleUsers
    .AsNoTracking()
    .Where(u => u.IsActive)
    .Select(u => new ExampleUserDto(u.PublicId, u.FullName, u.Email, u.IsActive))
    .ToListAsync(ct);

// ✓ Include para relaciones — evita N+1 queries
var usersWithRoles = await _db.ExampleUsers
    .AsNoTracking()
    .Include(u => u.Roles)
    .Where(u => u.IsActive)
    .ToListAsync(ct);
```

### Problema N+1 y cómo evitarlo

```csharp
// ❌ N+1 — EF hace un query por cada usuario para cargar sus roles
var users = await _db.ExampleUsers.ToListAsync(ct);   // 1 query
foreach (var user in users)
{
    var roles = user.Roles;  // N queries — uno por usuario → problema en producción
}

// ✓ Eager loading con Include — un solo JOIN
var users = await _db.ExampleUsers
    .Include(u => u.Roles)
    .ToListAsync(ct);   // 1 query con JOIN

// ✓ Split queries para colecciones grandes (evita producto cartesiano)
var users = await _db.ExampleUsers
    .Include(u => u.Roles)
    .AsSplitQuery()   // 2 queries separados en lugar de un JOIN
    .ToListAsync(ct);
```

---

## Soft Delete pattern

```csharp
// Domain/Common/ISoftDeletable.cs
public interface ISoftDeletable
{
    bool      IsDeleted  { get; set; }
    DateTime? DeletedAt  { get; set; }
    Guid?     DeletedBy  { get; set; }
}

// La entidad implementa la interfaz
public sealed class ExampleUser : ISoftDeletable
{
    public bool      IsDeleted  { get; set; }
    public DateTime? DeletedAt  { get; set; }
    public Guid?     DeletedBy  { get; set; }
    // ... resto de propiedades
}
```

```csharp
// AppDbContext — intercepta eliminaciones y las convierte en soft delete
public sealed class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser)
        : base(options) => _currentUser = currentUser;

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State             = EntityState.Modified;  // no borrar de la DB
                entry.Entity.IsDeleted  = true;
                entry.Entity.DeletedAt  = DateTime.UtcNow;
                entry.Entity.DeletedBy  = _currentUser.UserId;
            }
        }
        return await base.SaveChangesAsync(ct);
    }

    // Global query filter — todos los queries excluyen registros borrados automáticamente
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(BuildSoftDeleteFilter(entityType.ClrType));
            }
        }
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    private static LambdaExpression BuildSoftDeleteFilter(Type entityType)
    {
        var param = Expression.Parameter(entityType, "e");
        var prop  = Expression.Property(param, nameof(ISoftDeletable.IsDeleted));
        var body  = Expression.Not(prop);
        return Expression.Lambda(body, param);
    }
}
```

```csharp
// Cuando necesitas incluir registros borrados (administración, auditoría)
var todosLosUsuarios = await _db.ExampleUsers
    .IgnoreQueryFilters()   // ← desactiva el global filter para esta query
    .ToListAsync(ct);
```

---

## Auditoría: created/updated automático

```csharp
// Domain/Common/IAuditable.cs
public interface IAuditable
{
    DateTime  CreatedAt  { get; set; }
    Guid      CreatedBy  { get; set; }
    DateTime? UpdatedAt  { get; set; }
    Guid?     UpdatedBy  { get; set; }
}

// AppDbContext.SaveChangesAsync — audita automáticamente
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var now    = DateTime.UtcNow;
    var userId = _currentUser.UserId;

    foreach (var entry in ChangeTracker.Entries<IAuditable>())
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userId;
                break;

            case EntityState.Modified:
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userId;
                break;
        }
    }

    // Soft delete también auditado (ver sección anterior)
    foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
    {
        if (entry.State == EntityState.Deleted)
        {
            entry.State             = EntityState.Modified;
            entry.Entity.IsDeleted  = true;
            entry.Entity.DeletedAt  = now;
            entry.Entity.DeletedBy  = userId;
        }
    }

    return await base.SaveChangesAsync(ct);
}
```

```csharp
// La entidad con ambas interfaces
public sealed class ExampleUser : IAuditable, ISoftDeletable
{
    public int      Id         { get; init; }
    public Guid     PublicId   { get; init; }
    public string   FullName   { get; private set; } = string.Empty;
    public string   Email      { get; private set; } = string.Empty;
    public bool     IsActive   { get; private set; }

    // IAuditable
    public DateTime  CreatedAt { get; set; }
    public Guid      CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid?     UpdatedBy { get; set; }

    // ISoftDeletable
    public bool      IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid?     DeletedBy { get; set; }
}
```

---

## Interceptors — lógica transversal en la capa de persistencia
> Fuente: *Entity Framework Core in Action* (Smith) — Ch.16 Advanced Features

Los interceptors de EF Core permiten ejecutar código antes/después de queries y SaveChanges, sin modificar el DbContext directamente.

```csharp
// Infrastructure/Persistence/Interceptors/AuditableInterceptor.cs
public sealed class AuditableInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public AuditableInterceptor(ICurrentUserService currentUser)
        => _currentUser = currentUser;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct)
    {
        var db  = eventData.Context;
        var now = DateTime.UtcNow;

        foreach (var entry in db!.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = _currentUser.UserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = _currentUser.UserId;
            }
        }
        return base.SavingChangesAsync(eventData, result, ct);
    }
}

// Registro — el interceptor se registra en AddDbContext
builder.Services.AddScoped<AuditableInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, opts) =>
{
    opts.UseNpgsql(connectionString)
        .AddInterceptors(sp.GetRequiredService<AuditableInterceptor>());
});
```

---

## Cuándo usar EF Core vs Dapper

| Criterio | EF Core | Dapper |
|----------|---------|--------|
| Migrations automáticas | ✓ Integrado | ✗ Manual |
| Queries simples CRUD | ✓ LINQ fluido | ✓ SQL directo |
| Queries complejos | ✗ LINQ complejo, difícil de optimizar | ✓ SQL completo |
| Change tracking | ✓ Automático | ✗ Manual |
| Soft delete / Auditoría global | ✓ Global filters + interceptors | ✗ Lógica manual en cada repo |
| Rendimiento en queries masivos | ✗ Overhead del ORM | ✓ Más rápido con SQL puro |
| **Usar cuando** | Escrituras con relaciones, migrations, auditoría | Reportes, queries analíticos, optimización fina |

**Estrategia mixta (recomendada):** EF Core para Commands (escrituras con change tracking), Dapper para Queries (lecturas optimizadas).


---

## Glosario

| Término | Definición |
|---------|-----------|
| DbContext | Clase de EF Core que representa la sesión con la base de datos, gestiona change tracking y expone DbSets |
| Migrations | Sistema de EF Core para gestionar cambios incrementales al schema de la base de datos de forma controlada |
| Global Query Filter | Filtro aplicado automáticamente a todas las queries de una entidad — usado para soft delete y multi-tenancy |
| AsNoTracking | Modificador que desactiva el change tracking para queries de solo lectura, mejorando el rendimiento |
| Include | Método para cargar relaciones de forma eager loading en la misma query |
| AsSplitQuery | Modificador que divide una query con múltiples Includes en múltiples SELECTs para evitar producto cartesiano |
| Soft Delete | Patrón que marca registros como eliminados sin borrarlos físicamente de la base de datos |
| SaveChangesInterceptor | Hook que se ejecuta antes y después de SaveChanges — usado para auditoría y timestamps automáticos |
| IAuditable | Interfaz del back-template que marca entidades con CreatedAt, CreatedBy, UpdatedAt, UpdatedBy |
| ISoftDeletable | Interfaz del back-template que marca entidades con IsDeleted y DeletedAt |
| IgnoreQueryFilters | Método para omitir los Global Query Filters en una query específica — usar con precaución |
| HasQueryFilter | Método de configuración de entidad que registra un Global Query Filter en el ModelBuilder |
| Change Tracking | Mecanismo de EF Core que rastrea modificaciones a entidades cargadas para generar los UPDATE automáticos |

---

*Rogelio Arriaga Gonzalez*
