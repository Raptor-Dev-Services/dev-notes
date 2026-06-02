# 22 — EF Core Global Query Filters para Multi-Tenancy

Los Global Query Filters de EF Core son condiciones `WHERE` que se aplican automáticamente a todas las queries de una entidad, sin que el repositorio tenga que recordar filtrar manualmente. En un sistema multi-tenant son la primera línea de defensa contra la fuga de datos entre tenants.

---

## El problema sin filtros globales

Sin filtros globales, cada query en cada repositorio debe incluir el filtro de tenant. Es fácil olvidarlo:

```csharp
// ❌ Repositorio sin filtro global — fuga de datos si olvidamos el Where
public async Task<UserProfile?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default) =>
    await _db.UserProfiles
        .AsNoTracking()
        .FirstOrDefaultAsync(e => e.PublicId == publicId, ct);  // ← devuelve perfiles de CUALQUIER tenant

// ❌ Queremos tenantId=5 pero el dev olvidó el filtro
var profile = await _repo.GetByPublicIdAsync(publicId);  // retorna datos de otro tenant
```

Con 10 repositorios y 40 métodos, es solo cuestión de tiempo antes de que alguien cometa este error en producción.

---

## Solución: Global Query Filters en AppDbContext

```csharp
// Shared/Database/AppDbContext.cs
public sealed class AppDbContext : DbContext
{
    private readonly ITenantContextAccessor _tenantAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContextAccessor tenantAccessor)
        : base(options)
    {
        _tenantAccessor = tenantAccessor;
    }

    public DbSet<UserProfile>    UserProfiles  { get; set; } = null!;
    public DbSet<UserCredential> Credentials   { get; set; } = null!;

    private long CurrentTenantId =>
        long.TryParse(_tenantAccessor.Current?.TenantId, out var id) ? id : 0L;

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("dbo");
        mb.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Filtros globales — se aplican a TODAS las queries de estas entidades
        mb.Entity<UserProfile>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        mb.Entity<UserCredential>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
    }
}
```

Ahora el mismo repositorio sin filtro explícito ya es seguro:

```csharp
// ✓ El filtro global agrega WHERE TenantId = @currentTenantId automáticamente
public async Task<UserProfile?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default) =>
    await _db.UserProfiles
        .AsNoTracking()
        .FirstOrDefaultAsync(e => e.PublicId == publicId, ct);
```

El SQL generado es:
```sql
SELECT * FROM dbo.user_profiles
WHERE public_id = @publicId AND tenant_id = @currentTenantId  -- ← agregado automáticamente
LIMIT 1
```

---

## ITenantContextAccessor — el portador del tenant actual

El `CurrentTenantId` en el DbContext se lee de un accessor inyectado como Singleton:

```csharp
// Common/MultiTenancy/ITenantContextAccessor.cs
public interface ITenantContextAccessor
{
    TenantContext? Current { get; set; }
}

public sealed record TenantContext(string TenantId);

// Implementación simple con AsyncLocal
public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<TenantContext?> _current = new();
    public TenantContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
```

### Cómo se llena el accessor — TenantClaimsMiddleware

El accessor se llena al inicio de cada request HTTP, después de que el JWT ha sido validado:

```csharp
// Host.Api/Middleware/TenantClaimsMiddleware.cs
public sealed class TenantClaimsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITenantContextAccessor _accessor;

    public TenantClaimsMiddleware(RequestDelegate next, ITenantContextAccessor accessor)
    {
        _next     = next;
        _accessor = accessor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.User.FindFirstValue("tenant_id");
        if (!string.IsNullOrEmpty(tenantId))
            _accessor.Current = new TenantContext(tenantId);

        await _next(context);
    }
}
```

### Orden del middleware (crítico)

```csharp
// Host.Api/Program.cs
app.UseAuthentication();              // 1. Valida el JWT y llena HttpContext.User
app.UseAuthorization();               // 2. Verifica [Authorize]
app.UseMiddleware<TenantClaimsMiddleware>();  // 3. Lee tenant_id del User ya autenticado
```

`TenantClaimsMiddleware` debe ir DESPUÉS de `UseAuthentication`. Si va antes, `HttpContext.User` está vacío y el tenant nunca se establece.

---

## IgnoreQueryFilters — casos especiales

Algunos casos legítimos no tienen tenant: login, refresh token, creación inicial de credenciales.

```csharp
// Authentication.Infrastructure/Repositories/UserCredentialRepository.cs
public async Task<UserCredential?> GetForLoginAsync(string email, CancellationToken ct = default) =>
    await _db.Credentials
        .IgnoreQueryFilters()           // ← desactiva el filtro de TenantId
        .AsNoTracking()
        .FirstOrDefaultAsync(e => e.Email == email && e.IsActive, ct);
```

**Regla:** solo usar `IgnoreQueryFilters()` cuando el contexto genuinamente no tiene tenant (endpoints de auth). Nunca en endpoints de negocio.

---

## Soft Delete con Global Query Filter

El mismo patrón sirve para soft delete:

```csharp
// Solo entidades no borradas
mb.Entity<ExampleUser>().HasQueryFilter(e =>
    e.DeletedAt == null && e.TenantId == CurrentTenantId);
```

```csharp
// Soft delete — no DELETE físico
public async Task SoftDeleteAsync(Guid publicId, CancellationToken ct = default) =>
    await _db.ExampleUsers
        .Where(e => e.PublicId == publicId)
        .ExecuteUpdateAsync(s => s
            .SetProperty(e => e.DeletedAt, DateTime.UtcNow)
            .SetProperty(e => e.UpdatedAtUtc, DateTime.UtcNow), ct);

// Si necesitas ver registros borrados: IgnoreQueryFilters()
var all = await _db.ExampleUsers.IgnoreQueryFilters().ToListAsync(ct);
```

---

## Fixture de tests — TenantContext dummy

En tests de integración, el `AppDbContext` necesita un accessor con un tenant dummy:

```csharp
// {Modulo}.Tests/DbFixture.cs
public sealed class DbFixture
{
    public AppDbContext Db { get; }

    public DbFixture()
    {
        var tenantAccessor = new TenantContextAccessor();
        tenantAccessor.Current = new TenantContext("1");  // tenant dummy para tests

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=back_template_test;Username=postgres;Password=postgres")
            .Options;

        Db = new AppDbContext(options, tenantAccessor);
        Db.Database.EnsureCreated();
    }
}
```

Sin el tenant dummy, el `CurrentTenantId` devuelve `0` y las queries no retornan datos (filtro activo con `TenantId = 0`).

---

## DatabaseInitializationService — tenant dummy en startup

El mismo problema ocurre al inicializar la base de datos en el startup. El `DatabaseInitializationService` establece un tenant dummy antes de llamar `EnsureCreated`:

```csharp
// Shared/Database/ServiceCollectionEx.cs
internal sealed class DatabaseInitializationService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        // Tenant dummy para que los filtros no fallen durante EnsureCreated
        var accessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        accessor.Current = new TenantContext("0");

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync(ct);
    }
}
```

---

## Cuándo usar / no usar

**Usar Global Query Filters cuando:**
- La entidad siempre pertenece a un tenant y nunca debe cruzar fronteras de tenant
- Quieres garantía en tiempo de compilación (sin filtros = datos de otro tenant)
- Tienes muchos repositorios y métodos — añadir el filtro manualmente en cada uno es error-prone

**No usar Global Query Filters cuando:**
- La entidad es transversal a todos los tenants (ej. catálogos compartidos)
- El endpoint genuinamente no tiene tenant (autenticación, endpoints públicos)
- Los filtros añaden complejidad sin beneficio (tablas de referencia inmutables)

---

## Relación con el back-template

El back-template aplica este patrón en:
- `Shared/Database/AppDbContext.cs` — filtros para `UserCredential` y `UserProfile`
- `Host.Api/Middleware/TenantClaimsMiddleware.cs` — llena el accessor desde el JWT
- `Authentication.Infrastructure/Repositories/` — usa `IgnoreQueryFilters()` para login/refresh
- `Shared/Database/ServiceCollectionEx.cs` — `DatabaseInitializationService` con tenant dummy

Ver `docs/DB.md` y `docs/MultiTenancy.md` del back-template para la implementación completa.
