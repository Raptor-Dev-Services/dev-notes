# 24 — Multi-Tenancy: Lógica de Tenant

Multi-tenancy es el modelo en que una sola instancia de la aplicación sirve a múltiples empresas (tenants) con aislamiento completo de datos. Cada tenant ve únicamente sus propios registros — nunca los de otro.

---

## El modelo conceptual

```
SaaS Product
├── Tenant A  (Empresa Alfa S.A.)
│   ├── Usuario 1
│   ├── Usuario 2
│   └── Datos exclusivos de Alfa
├── Tenant B  (Beta Corp.)
│   ├── Usuario 3
│   └── Datos exclusivos de Beta
└── Tenant C  (Gamma LLC.)
    └── ...
```

Un tenant es una empresa cliente del producto SaaS. Sus usuarios, datos, configuraciones y registros son completamente privados. La misma tabla de base de datos contiene registros de todos los tenants — la separación es lógica, no física.

---

## Estrategias de aislamiento

### Columna `tenant_id` (Row-Level Security)

La estrategia más común para un monolito o un SaaS pequeño. Una columna `tenant_id` en cada tabla filtrante. Todo registro lleva el identificador del tenant dueño.

```
dbo.user_profiles
┌─────┬───────────┬───────────┬──────────────┐
│ id  │ tenant_id │ public_id │ full_name     │
├─────┼───────────┼───────────┼──────────────┤
│   1 │         1 │ uuid-001  │ Ana García    │ ← Tenant A
│   2 │         1 │ uuid-002  │ Luis Pérez    │ ← Tenant A
│   3 │         2 │ uuid-003  │ Carmen López  │ ← Tenant B
│   4 │         2 │ uuid-004  │ Jorge Ramos   │ ← Tenant B
└─────┴───────────┴───────────┴──────────────┘
```

### Esquemas por tenant

Cada tenant tiene su propio schema en la misma DB (`tenant_1.user_profiles`, `tenant_2.user_profiles`). Más aislamiento pero mucho más complejo de operar.

### Bases de datos separadas

El máximo aislamiento. Cada tenant tiene su propia DB. Caro y difícil de mantener — solo justificable para tenants Enterprise con requisitos de compliance.

**El back-template usa Row-Level Security con `tenant_id`** — es el balance correcto para un SaaS bootstrapped.

---

## Flujo del tenant_id en una request

```
1. Cliente envía request con JWT en Authorization header

2. JWT contiene claims:
   {
     "sub":       "uuid-del-usuario",
     "email":     "usuario@empresa.com",
     "role":      "Admin",
     "tenant_id": "42"          ← el tenant del usuario
   }

3. ASP.NET Core valida el JWT (UseAuthentication)
   → HttpContext.User se llena con los claims

4. TenantClaimsMiddleware lee el claim y llena el accessor:
   accessor.Current = new TenantContext("42")

5. AppDbContext lee el accessor en cada query:
   WHERE tenant_id = 42          ← automático, sin que el repositorio lo pida

6. Resultado: el repositorio solo ve datos del tenant 42
```

El `tenant_id` nace en el momento del login y vive en el JWT hasta que expira. El backend nunca confía en el `tenant_id` que el cliente envíe en el body — lo saca exclusivamente del JWT validado.

---

## Implementación en .NET — los 4 componentes

### 1. Entidad con TenantId

Cada entidad que pertenece a un tenant lleva la columna:

```csharp
// {Modulo}.Domain/Entities/UserProfile.cs
public sealed class UserProfile
{
    public long   Id          { get; init; }
    public Guid   PublicId    { get; init; }
    public long   TenantId    { get; init; }   // ← discriminador de tenant
    public string FullName    { get; init; } = string.Empty;
    public bool   IsActive    { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
```

### 2. ITenantContextAccessor — portador del tenant

```csharp
// Common/MultiTenancy/ITenantContextAccessor.cs
public interface ITenantContextAccessor
{
    TenantContext? Current { get; set; }
}

public sealed record TenantContext(string TenantId);

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

`AsyncLocal<T>` garantiza que el valor está aislado por request (no hay mezcla entre requests concurrentes).

Registro como **Singleton** — `AsyncLocal` maneja el aislamiento:

```csharp
services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
```

### 3. TenantClaimsMiddleware — extrae el claim

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

Orden crítico en `Program.cs`:

```csharp
app.UseAuthentication();                          // 1. Valida JWT → llena HttpContext.User
app.UseAuthorization();                           // 2. Verifica [Authorize]
app.UseMiddleware<TenantClaimsMiddleware>();       // 3. Extrae tenant_id del User ya autenticado
```

Si el middleware va antes de `UseAuthentication`, `HttpContext.User` está vacío.

### 4. Global Query Filter en AppDbContext

```csharp
// Shared/Database/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder mb)
{
    mb.HasDefaultSchema("dbo");
    mb.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    mb.Entity<UserProfile>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
    mb.Entity<UserCredential>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
}

private long CurrentTenantId =>
    long.TryParse(_tenantAccessor.Current?.TenantId, out var id) ? id : 0L;
```

Con esto, cada query sobre `UserProfile` genera automáticamente `WHERE tenant_id = @currentTenantId`. El repositorio no necesita filtrarlo.

---

## Excepciones — cuándo no filtrar por tenant

### Endpoints de autenticación

Login y refresh token no tienen JWT todavía — no hay tenant_id en el accessor. Los repositorios de auth usan `IgnoreQueryFilters()`:

```csharp
public async Task<UserCredential?> GetForLoginAsync(string email, CancellationToken ct = default) =>
    await _db.Credentials
        .IgnoreQueryFilters()     // no hay tenant en el momento del login
        .AsNoTracking()
        .FirstOrDefaultAsync(e => e.Email == email && e.IsActive, ct);
```

### Tablas de catálogos compartidos

Tablas de referencia que pertenecen a todos los tenants (países, monedas, tipos de documento) no llevan `tenant_id` y no tienen Global Query Filter.

### Endpoints administrativos (super-admin)

Un endpoint de administración del SaaS que ve todos los tenants usa `IgnoreQueryFilters()` y requiere un rol especial (ej. `SuperAdmin`) verificado antes de llamarlo.

---

## El tenant_id en el controller

El controller extrae el tenant del JWT claim en cada acción que lo necesita:

```csharp
[Route("api/users")]
[Authorize]
public sealed class UsersController : BaseApiController
{
    private long CurrentTenantId =>
        long.TryParse(User.FindFirstValue("tenant_id"), out var id) ? id : 0;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        // TenantId ya está en el accessor (vía middleware) Y en el claim
        // Se pasa al handler para que el handler no dependa del accessor
        var result = await Mediator.Send(new GetUserProfileRequest(id, CurrentTenantId), ct);
        // ...
    }
}
```

El handler recibe el `TenantId` como parámetro del request — no lo lee del accessor directamente. Esto hace los unit tests más simples (no hay que mockear el accessor):

```csharp
public sealed record GetUserProfileRequest(Guid PublicId, long TenantId)
    : IRequest<GetUserProfileResponse>;
```

---

## Creación de un registro — tenant automático

Al crear un registro nuevo, el `TenantId` viene del JWT, no del body del request:

```csharp
// Handler
public async Task<CreateUserResponse> Handle(CreateUserRequest request, CancellationToken ct)
{
    // TenantId viene del JWT — nunca del body
    var id = await _profiles.InsertAsync(request.PublicId, request.TenantId, request.FullName, ct);
    return new CreateUserSuccess(id);
}

// Repositorio
public async Task<long> InsertAsync(Guid publicId, long tenantId, string fullName, CancellationToken ct)
{
    var entity = new UserProfile
    {
        PublicId  = publicId,
        TenantId  = tenantId,   // ← viene del JWT, no del cliente
        FullName  = fullName,
        IsActive  = true
    };
    _db.UserProfiles.Add(entity);
    await _db.SaveChangesAsync(ct);
    return entity.Id;
}
```

---

## Tenant en el JWT — claim en el login

Al hacer login, el JWT se genera con el `tenant_id` del usuario:

```csharp
// Authentication.Infrastructure/Jwt/JwtTokenService.cs
var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub,  credential.PublicId.ToString()),
    new Claim("email",     credential.Email),
    new Claim("role",      credential.Role),
    new Claim("tenant_id", credential.TenantId.ToString())   // ← string en el claim
};
```

El `tenant_id` en el claim es un `string` (así funciona JWT). Al leerlo en el backend se convierte a `long`:

```csharp
long.TryParse(User.FindFirstValue("tenant_id"), out var id) ? id : 0
```

---

## Tablas que llevan tenant_id vs las que no

| Tabla | Lleva tenant_id | Por qué |
|-------|----------------|---------|
| `dbo.user_profiles` | ✓ | Perfil de usuario es privado por tenant |
| `dbo.credentials` | ✓ | Las credenciales pertenecen al tenant |
| `dbo.refresh_tokens` | ✗ | FK a credentials — hereda el tenant implícitamente |
| `dbo.tenants` | ✗ | Los tenants son el nivel raíz — no pertenecen a otro tenant |
| Catálogos compartidos | ✗ | Son globales — no varían por tenant |

---

## Consideraciones de diseño

**El `tenant_id` es sagrado:** nunca lo aceptes del request body para operaciones de lectura/escritura. Siempre del JWT.

**Soft delete con tenant:** combinar `IsActive` y `TenantId` en el mismo Global Query Filter:
```csharp
mb.Entity<UserProfile>().HasQueryFilter(e =>
    e.TenantId == CurrentTenantId && e.IsActive);
```

**Índices:** siempre agregar `tenant_id` como primera columna de los índices más frecuentes — los queries siempre filtran por tenant:
```csharp
b.HasIndex(e => new { e.TenantId, e.PublicId }).IsUnique();
b.HasIndex(e => new { e.TenantId, e.IsActive });
```

**Onboarding de tenant nuevo:** al registrar una empresa nueva, se crea el registro en `dbo.tenants` y el primer usuario Admin. El `tenant_id` del usuario Admin es el `Id` del tenant recién creado.

---

## Relación con el back-template

El back-template implementa single-level multi-tenancy con:
- `ITenantContextAccessor` en `Common/` — AsyncLocal singleton
- `TenantClaimsMiddleware` en `Host.Api/Middleware/`
- Global Query Filters en `Shared/Database/AppDbContext.cs`
- `IgnoreQueryFilters()` en `Authentication.Infrastructure/Repositories/`

Ver `04-backend/22-ef-core-multi-tenancy.md` para la implementación detallada del Global Query Filter.
Ver `03-arquitectura/09-modular-monolith.md` para cómo el tenant se propaga a través de los módulos.

---

*Rogelio Arriaga Gonzalez*
