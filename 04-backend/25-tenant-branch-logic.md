# 25 — Tenant + Branch: Jerarquía de Dos Niveles

Algunos sistemas SaaS necesitan un segundo nivel de segmentación dentro de cada empresa. Una empresa (tenant) puede tener múltiples sucursales, plantas, departamentos o unidades de negocio (branches). Los datos de cada branch son privados dentro del tenant. Un usuario de la Sucursal Norte no debe ver los datos de la Sucursal Sur, aunque ambas pertenezcan al mismo tenant.

---

## El modelo conceptual

```
SaaS Product
├── Tenant A  (Empresa Alfa S.A.)
│   ├── Branch 1  (Sucursal Norte)
│   │   ├── Usuario 1
│   │   └── Datos de Sucursal Norte
│   ├── Branch 2  (Sucursal Sur)
│   │   ├── Usuario 2
│   │   └── Datos de Sucursal Sur
│   └── Branch 3  (Corporativo)
│       └── Datos globales del tenant
├── Tenant B  (Beta Corp.)
│   ├── Branch 1  (Planta Monterrey)
│   └── Branch 2  (Planta Tijuana)
```

```
dbo.production_orders
┌─────┬───────────┬───────────┬──────────────────────┐
│ id  │ tenant_id │ branch_id │ order_number          │
├─────┼───────────┼───────────┼──────────────────────┤
│   1 │         1 │        10 │ ORD-2025-001          │ ← Alfa, Sucursal Norte
│   2 │         1 │        11 │ ORD-2025-002          │ ← Alfa, Sucursal Sur
│   3 │         2 │        20 │ ORD-2025-003          │ ← Beta, Planta Monterrey
└─────┴───────────┴───────────┴──────────────────────┘
```

Un usuario pertenece a un tenant **y** a un branch. El JWT lleva ambos: `tenant_id` y `branch_id`.

---

## Cuándo agregar el nivel Branch

| Señal | Decisión |
|-------|---------|
| Los usuarios de una sede no deben ver datos de otra sede | Agregar branch |
| Los reportes se agregan por sede / planta / departamento | Agregar branch |
| El admin corporativo necesita ver TODAS las sedes | Agregar branch con rol cross-branch |
| Todos los usuarios del tenant comparten los mismos datos | Solo tenant es suficiente |
| Las "sedes" son solo un campo de filtro opcional | Solo tenant es suficiente |

---

## Flujo del tenant_id + branch_id en una request

```
1. Cliente envía request con JWT

2. JWT contiene:
   {
     "sub":       "uuid-del-usuario",
     "tenant_id": "1",
     "branch_id": "10",          ← segundo nivel
     "role":      "Operator"
   }

3. UseAuthentication valida el JWT → HttpContext.User tiene ambos claims

4. TenantClaimsMiddleware lee ambos:
   accessor.Current = new TenantContext("1", "10")

5. AppDbContext aplica AMBOS filtros:
   WHERE tenant_id = 1 AND branch_id = 10

6. El operador de Sucursal Norte solo ve sus datos
```

---

## Implementación: extender TenantContext

```csharp
// Common/MultiTenancy/TenantContext.cs
public sealed record TenantContext(string TenantId, string? BranchId = null);
```

`BranchId` es nullable. No todos los sistemas lo usan, y los endpoints admin que cruzan branches lo omiten.

```csharp
// Common/MultiTenancy/ITenantContextAccessor.cs
public interface ITenantContextAccessor
{
    TenantContext? Current { get; set; }
}

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

---

## TenantClaimsMiddleware con branch

```csharp
// Host.Api/Middleware/TenantClaimsMiddleware.cs
public async Task InvokeAsync(HttpContext context)
{
    var tenantId = context.User.FindFirstValue("tenant_id");
    var branchId = context.User.FindFirstValue("branch_id");   // ← nuevo

    if (!string.IsNullOrEmpty(tenantId))
        _accessor.Current = new TenantContext(tenantId, branchId);

    await _next(context);
}
```

---

## Entidad con TenantId + BranchId

```csharp
// {Modulo}.Domain/Entities/ProductionOrder.cs
public sealed class ProductionOrder
{
    public long   Id           { get; init; }
    public Guid   PublicId     { get; init; }
    public long   TenantId     { get; init; }   // ← nivel 1
    public long   BranchId     { get; init; }   // ← nivel 2
    public string OrderNumber  { get; init; } = string.Empty;
    public bool   IsActive     { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
```

---

## Global Query Filter con dos niveles

```csharp
// Shared/Database/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder mb)
{
    mb.HasDefaultSchema("dbo");
    mb.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    // Entidades solo con tenant
    mb.Entity<UserProfile>().HasQueryFilter(e => e.TenantId == CurrentTenantId);

    // Entidades con tenant + branch
    mb.Entity<ProductionOrder>().HasQueryFilter(e =>
        e.TenantId == CurrentTenantId &&
        (CurrentBranchId == 0 || e.BranchId == CurrentBranchId));
    // CurrentBranchId == 0 cuando el accessor no tiene branch → admin ve todo el tenant
}

private long CurrentTenantId =>
    long.TryParse(_tenantAccessor.Current?.TenantId, out var id) ? id : 0L;

private long CurrentBranchId =>
    long.TryParse(_tenantAccessor.Current?.BranchId, out var id) ? id : 0L;
```

La condición `(CurrentBranchId == 0 || e.BranchId == CurrentBranchId)` es la clave. Si el JWT tiene `branch_id`, filtra solo ese branch. Si el JWT no tiene `branch_id` (admin corporativo), devuelve todos los branches del tenant.

---

## Controller con ambos claims

```csharp
[Route("api/production-orders")]
[Authorize]
public sealed class ProductionOrdersController : BaseApiController
{
    private long CurrentTenantId =>
        long.TryParse(User.FindFirstValue("tenant_id"), out var id) ? id : 0;

    private long CurrentBranchId =>
        long.TryParse(User.FindFirstValue("branch_id"), out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await Mediator.Send(
            new GetProductionOrdersRequest(CurrentTenantId, CurrentBranchId), ct);
        // ...
    }
}
```

---

## Request con BranchId

```csharp
public sealed record GetProductionOrdersRequest(long TenantId, long BranchId)
    : IRequest<GetProductionOrdersResponse>;
```

El `BranchId = 0` en el request indica "sin filtro de branch" (admin corporativo). El repositorio puede aprovecharlo:

```csharp
public async Task<List<ProductionOrder>> GetAllAsync(
    long tenantId, long branchId, CancellationToken ct) =>
    await _db.ProductionOrders
        .AsNoTracking()
        // El Global Query Filter ya aplica ambos filtros — no hay que repetirlos aquí
        .OrderByDescending(e => e.CreatedAtUtc)
        .ToListAsync(ct);
```

Con el Global Query Filter activo, el repositorio no necesita filtrar manualmente. El filtro ya hace el trabajo.

---

## Tabla Branches

```csharp
// Tenancy.Domain/Entities/Branch.cs
public sealed class Branch
{
    public long   Id        { get; init; }
    public Guid   PublicId  { get; init; }
    public long   TenantId  { get; init; }   // FK al tenant dueño
    public string Name      { get; init; } = string.Empty;
    public bool   IsActive  { get; init; }
}
```

```
dbo.branches
┌─────┬───────────┬───────────────────────┐
│ id  │ tenant_id │ name                  │
├─────┼───────────┼───────────────────────┤
│  10 │         1 │ Sucursal Norte        │
│  11 │         1 │ Sucursal Sur          │
│  12 │         1 │ Corporativo           │
│  20 │         2 │ Planta Monterrey      │
│  21 │         2 │ Planta Tijuana        │
└─────┴───────────┴───────────────────────┘
```

`dbo.branches` tiene `tenant_id` pero no tiene `branch_id`. La tabla de branches pertenece al tenant, no a un branch específico. El Global Query Filter de `Branch` filtra solo por `TenantId`.

---

## El branch_id en el JWT — quién lo pone

El `branch_id` se incluye en el JWT al momento del login. El usuario pertenece a un branch específico:

```csharp
// Authentication.Infrastructure/Jwt/JwtTokenService.cs
var claims = new List<Claim>
{
    new Claim(JwtRegisteredClaimNames.Sub, credential.PublicId.ToString()),
    new Claim("email",     credential.Email),
    new Claim("role",      credential.Role),
    new Claim("tenant_id", credential.TenantId.ToString()),
    new Claim("branch_id", credential.BranchId.ToString()),  // ← branch del usuario
};
```

Para usuarios admin corporativo que necesitan ver todos los branches, el `branch_id` en el JWT es `"0"` o se omite.

---

## Operaciones cross-branch — admin corporativo

Un admin corporativo puede necesitar ver registros de todos los branches del tenant (reportes consolidados, dashboards):

```csharp
// Repositorio — query cross-branch para admin
public async Task<List<ProductionOrder>> GetAllBranchesAsync(
    long tenantId, CancellationToken ct) =>
    await _db.ProductionOrders
        .IgnoreQueryFilters()   // ← desactiva el filtro de branch
        .AsNoTracking()
        .Where(e => e.TenantId == tenantId && e.IsActive)
        .OrderByDescending(e => e.CreatedAtUtc)
        .ToListAsync(ct);
```

Usar `IgnoreQueryFilters()` y filtrar manualmente por `TenantId`. El tenant sigue siendo el límite máximo.

**Regla:** `IgnoreQueryFilters()` para branches siempre va acompañado de un filtro manual de `TenantId`. Nunca datos cross-tenant.

---

## Índices con tenant_id + branch_id

```csharp
// Shared/Database/EntityTypeConfigurations/ProductionOrderConfiguration.cs
public sealed class ProductionOrderConfiguration : IEntityTypeConfiguration<ProductionOrder>
{
    public void Configure(EntityTypeBuilder<ProductionOrder> b)
    {
        b.ToTable("production_orders");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).UseIdentityByDefaultColumn();
        b.Property(e => e.PublicId).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
        b.Property(e => e.CreatedAtUtc)
            .HasColumnType("timestamp(0)")
            .HasDefaultValueSql("timezone('utc', now())");

        // Índice compuesto — tenant + branch + public_id para lookups por publicId
        b.HasIndex(e => new { e.TenantId, e.BranchId, e.PublicId }).IsUnique();
        // Índice para listar por branch activos
        b.HasIndex(e => new { e.TenantId, e.BranchId, e.IsActive });

        b.HasOne<Tenant>().WithMany()
            .HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Branch>().WithMany()
            .HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
```

---

## Comparación: un nivel vs dos niveles

| Aspecto | Solo Tenant | Tenant + Branch |
|---------|------------|-----------------|
| JWT claims | `tenant_id` | `tenant_id` + `branch_id` |
| Global Query Filter | `TenantId == current` | `TenantId == current && BranchId == current` |
| Complejidad | Baja | Media |
| Admin corporativo | N/A | `IgnoreQueryFilters()` + filtro manual de `TenantId` |
| Índices | `(tenant_id, ...)` | `(tenant_id, branch_id, ...)` |
| Casos de uso | SaaS simple, un solo workspace | Manufactura, retail con sucursales, educación con planteles |

---

## Relación con el back-template

El back-template implementa **solo el nivel Tenant**: es el punto de partida correcto para la mayoría de SaaS. Si el producto requiere el nivel Branch, los pasos de extensión son:

1. Agregar `BranchId` a `TenantContext` en `Common/`.
2. Actualizar `TenantClaimsMiddleware` para leer `branch_id` del JWT.
3. Agregar `BranchId` a las entidades que lo requieran.
4. Agregar `CurrentBranchId` y el filtro doble en `AppDbContext.OnModelCreating`.
5. Crear la tabla `dbo.branches` con su `EntityTypeConfiguration`.
6. Agregar `branch_id` al JWT en `JwtTokenService`.
7. Extender el controller para extraer `CurrentBranchId` de los claims.

Ver `04-backend/24-multi-tenancy-logic.md` para el nivel Tenant base.
Ver `04-backend/22-ef-core-multi-tenancy.md` para el Global Query Filter de EF Core.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Branch | Subdivisión de un tenant — representa una sucursal, sede u organización interna dentro de la empresa |
| BranchId | Identificador de la sucursal almacenado en JWT y en las filas de la base de datos junto a TenantId |
| TenantContext | Objeto que contiene TenantId y BranchId del request actual, gestionado con AsyncLocal |
| Admin Corporativo | Rol con acceso a todas las branches del tenant — usa IgnoreQueryFilters para el BranchId |
| Índice Compuesto | Índice de base de datos sobre (tenant_id, branch_id) para optimizar queries con los dos filtros |
| IgnoreQueryFilters Cross-Branch | Uso intencional de IgnoreQueryFilters para que el admin corporativo vea datos de múltiples branches |
| CurrentBranchId | Propiedad del TenantContext que expone el BranchId del request actual |
| Doble Global Query Filter | Aplicación simultánea de filtros por TenantId y BranchId en una sola HasQueryFilter |
| Transferencia entre branches | Operación que requiere IgnoreQueryFilters y validación explícita de pertenencia al mismo tenant |
| Jerarquía dos niveles | Estructura Tenant → Branch que modela empresa → sucursal en el SaaS |
| branch_id en JWT | Claim en el token del usuario que indica a qué branch pertenece su sesión actual |

---

*Rogelio Arriaga Gonzalez*
