# 43 — Super-Admin: Panel de Operaciones del SaaS

El Super-Admin es el panel interno para el equipo que opera el SaaS. Permite gestionar todos los tenants desde un punto centralizado: ver el estado de la plataforma, suspender tenants, acceder a datos para soporte técnico e impersonar usuarios para reproducir bugs.

---

## Qué puede hacer el Super-Admin

```
Gestión de tenants:
├── Listar todos los tenants con estado, plan, fecha de creación
├── Ver detalles de un tenant (usuarios, uso, billing)
├── Suspender / reactivar un tenant
├── Cambiar el plan manualmente (sin pasar por Stripe)
└── Eliminar un tenant (soft delete + schedule de purga)

Soporte:
├── Impersonar a un usuario (ver el sistema como ese usuario lo ve)
├── Ver el audit trail completo de cualquier tenant
├── Resetear el password de cualquier usuario
└── Revocar sesiones activas de cualquier usuario

Métricas de negocio:
├── Total de tenants activos / suspendidos
├── Distribución por plan
├── Tenants en trial / próximos a vencer
├── Uso promedio de storage, API calls, usuarios
└── Tenants con pagos vencidos (past_due)
```

---

## Rol SuperAdmin vs roles de tenant

```csharp
public static class SystemRoles
{
    public const string SuperAdmin = "SuperAdmin";  // acceso total cross-tenant
    public const string Support    = "Support";     // acceso de solo lectura cross-tenant
}
```

Un SuperAdmin **no pertenece a ningún tenant**. Su JWT tiene `role = "SuperAdmin"` y `tenant_id = 0` (o sin claim `tenant_id`). Los repositorios del panel de admin usan `IgnoreQueryFilters()`.

---

## Endpoints del Super-Admin

```csharp
// Host.Api/Controllers/SuperAdmin/AdminTenantsController.cs
[Route("api/admin/tenants")]
[Authorize(Roles = SystemRoles.SuperAdmin)]
[ApiExplorerSettings(GroupName = "admin")]  // grupo Swagger separado
public sealed class AdminTenantsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] string? status = null,
        [FromQuery] string? plan   = null,
        CancellationToken ct = default)
    {
        _ = await Mediator.Send(new AdminGetTenantsRequest(page, status, plan), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }

    [HttpGet("{tenantId:long}")]
    public async Task<IActionResult> GetDetail(long tenantId, CancellationToken ct)
    {
        _ = await Mediator.Send(new AdminGetTenantDetailRequest(tenantId), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : NotFound(_viewModel);
    }

    [HttpPost("{tenantId:long}/suspend")]
    public async Task<IActionResult> Suspend(long tenantId, [FromBody] AdminSuspendBody body, CancellationToken ct)
    {
        _ = await Mediator.Send(new AdminSuspendTenantRequest(tenantId, body.Reason), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }

    [HttpPost("{tenantId:long}/activate")]
    public async Task<IActionResult> Activate(long tenantId, CancellationToken ct)
    {
        _ = await Mediator.Send(new AdminActivateTenantRequest(tenantId), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }

    [HttpPut("{tenantId:long}/plan")]
    public async Task<IActionResult> ChangePlan(long tenantId, [FromBody] AdminChangePlanBody body, CancellationToken ct)
    {
        _ = await Mediator.Send(new AdminChangePlanRequest(tenantId, body.Plan), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }
}
```

---

## Repository de admin — sin filtros de tenant

```csharp
// Tenancy.Infrastructure/Repositories/AdminTenantRepository.cs
public sealed class AdminTenantRepository : IAdminTenantRepository
{
    private readonly AppDbContext _db;

    public async Task<List<TenantAdminDto>> GetAllAsync(
        string? status, string? plan, int page, int pageSize, CancellationToken ct) =>
        await _db.Tenants
            .IgnoreQueryFilters()           // ← cross-tenant
            .AsNoTracking()
            .Where(t => (status == null || t.Status.ToString() == status)
                     && (plan   == null || t.Plan == plan))
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TenantAdminDto(
                t.Id, t.PublicId, t.Name, t.Slug,
                t.Status.ToString(), t.Plan, t.CreatedAtUtc))
            .ToListAsync(ct);

    public async Task<TenantDetailAdminDto?> GetDetailAsync(long tenantId, CancellationToken ct)
    {
        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null) return null;

        // Estadísticas del tenant sin filtros
        var userCount = await _db.UserProfiles
            .IgnoreQueryFilters()
            .CountAsync(u => u.TenantId == tenantId && u.IsActive, ct);

        var billing = await _db.TenantBillings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.TenantId == tenantId, ct);

        return new TenantDetailAdminDto(tenant, userCount, billing);
    }
}
```

---

## Impersonación de usuario

La impersonación permite que un agente de soporte (SuperAdmin) vea el sistema exactamente como lo ve el usuario afectado. Es esencial para reproducir bugs y ayudar al usuario.

### Cómo funciona

```
1. SuperAdmin llama POST /api/admin/impersonate/{userPublicId}
2. API verifica que el SuperAdmin tiene el role correcto
3. API busca al usuario objetivo (en cualquier tenant)
4. API emite un JWT temporal con los claims del usuario objetivo + un claim "impersonated_by"
5. SuperAdmin usa ese JWT para navegar por el sistema como ese usuario
6. El JWT de impersonación tiene duración corta (1 hora) y no puede hacer acciones destructivas
```

```csharp
// Host.Api/Controllers/SuperAdmin/AdminImpersonateController.cs
[Route("api/admin/impersonate")]
[Authorize(Roles = SystemRoles.SuperAdmin)]
public sealed class AdminImpersonateController : BaseApiController
{
    [HttpPost("{userPublicId:guid}")]
    public async Task<IActionResult> Impersonate(Guid userPublicId, CancellationToken ct)
    {
        _ = await Mediator.Send(new ImpersonateUserRequest(
            userPublicId,
            impersonatedBy: Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!)),
            ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : NotFound(_viewModel);
    }
}
```

```csharp
// Authentication.Application/UseCases/ImpersonateUser/ImpersonateUserHandler.cs
public async Task<ImpersonateUserResponse> Handle(
    ImpersonateUserRequest request, CancellationToken ct)
{
    // Buscar el usuario en cualquier tenant (IgnoreQueryFilters implícito porque no hay tenant context)
    var credential = await _credentials.GetByPublicIdAnyTenantAsync(request.UserPublicId, ct);
    if (credential is null)
        return new ImpersonateUserNotFoundFailure("Usuario no encontrado.");

    // JWT de impersonación — duración corta, claim especial
    var impersonationToken = _jwt.GenerateImpersonation(
        targetPublicId:  credential.PublicId,
        targetEmail:     credential.Email,
        targetRole:      credential.Role,
        targetTenantId:  credential.TenantId,
        targetBranchId:  credential.BranchId,
        impersonatedBy:  request.ImpersonatedBy,
        expiresIn:       TimeSpan.FromHours(1));

    // Registrar en el audit trail del SaaS
    await _auditService.RecordAsync(
        tenantId:     0,
        branchId:     null,
        userPublicId: request.ImpersonatedBy,
        userEmail:    null,
        action:       "ImpersonationStarted",
        entityType:   "UserCredential",
        entityPublicId: request.UserPublicId.ToString(),
        notes:        $"Impersonating tenant {credential.TenantId}",
        ct:           ct);

    return new ImpersonateUserSuccess(impersonationToken);
}
```

```csharp
// JwtTokenService — generar token de impersonación
public string GenerateImpersonation(
    Guid targetPublicId, string targetEmail, string targetRole,
    long targetTenantId, long? targetBranchId,
    Guid impersonatedBy, TimeSpan expiresIn)
{
    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, targetPublicId.ToString()),
        new Claim("email",          targetEmail),
        new Claim(ClaimTypes.Role,  targetRole),
        new Claim("tenant_id",      targetTenantId.ToString()),
        new Claim("impersonated_by", impersonatedBy.ToString()),  // ← claim especial
        new Claim("is_impersonation", "true"),
    };

    if (targetBranchId.HasValue)
        claims.Add(new Claim("branch_id", targetBranchId.Value.ToString()));

    // ... generar JWT con expiresIn ...
}
```

### Restricciones durante impersonación

```csharp
// Middleware para restringir acciones en modo impersonación
public sealed class ImpersonationRestrictionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var isImpersonation = context.User.FindFirstValue("is_impersonation") == "true";

        if (isImpersonation)
        {
            var method = context.Request.Method;
            var path   = context.Request.Path.Value ?? "";

            // No permitir acciones destructivas durante impersonación
            bool isDestructive = method is "DELETE" ||
                                  path.Contains("/sessions") ||
                                  path.Contains("/password") ||
                                  path.Contains("/billing");

            if (isDestructive)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Esta acción no está disponible en modo impersonación."
                });
                return;
            }
        }

        await _next(context);
    }
}
```

---

## Métricas del SaaS para el Super-Admin

```csharp
// Host.Api/Controllers/SuperAdmin/AdminMetricsController.cs
[HttpGet("metrics")]
public async Task<IActionResult> GetMetrics(CancellationToken ct)
{
    _ = await Mediator.Send(new AdminGetMetricsRequest(), ct);
    return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
}
```

```csharp
// AdminGetMetricsHandler.cs — sin filtros de tenant
public async Task<AdminGetMetricsResponse> Handle(
    AdminGetMetricsRequest request, CancellationToken ct)
{
    var metrics = new AdminMetricsDto(
        TotalTenants:   await _db.Tenants.IgnoreQueryFilters().CountAsync(ct),
        ActiveTenants:  await _db.Tenants.IgnoreQueryFilters()
                            .CountAsync(t => t.Status == TenantStatus.Active, ct),
        TrialTenants:   await _db.Tenants.IgnoreQueryFilters()
                            .CountAsync(t => t.Plan == "trial" && t.Status == TenantStatus.Active, ct),
        PastDueTenants: await _db.TenantBillings.IgnoreQueryFilters()
                            .CountAsync(b => b.BillingStatus == "past_due", ct),
        TotalUsers:     await _db.UserProfiles.IgnoreQueryFilters()
                            .CountAsync(u => u.IsActive, ct),
        PlanDistribution: await _db.Tenants.IgnoreQueryFilters()
                              .Where(t => t.Status == TenantStatus.Active)
                              .GroupBy(t => t.Plan)
                              .Select(g => new { Plan = g.Key, Count = g.Count() })
                              .ToListAsync(ct)
    );

    return new AdminGetMetricsSuccess(metrics);
}
```

---

## Seguridad del panel de admin

```csharp
// Solo accesible desde IPs del equipo interno (whitelist)
// Host.Api/Middleware/AdminIpWhitelistMiddleware.cs
public sealed class AdminIpWhitelistMiddleware
{
    private readonly HashSet<string> _allowedIps;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/admin"))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            if (ip is null || !_allowedIps.Contains(ip))
            {
                context.Response.StatusCode = 404;  // 404, no 403 — no revelar que existe
                return;
            }
        }

        await _next(context);
    }
}
```

Adicionalmente, en producción el panel de admin puede estar en un subdominio separado (`admin.misaas.com`) detrás de VPN.

---

## Checklist

- [ ] Endpoints de admin con `[Authorize(Roles = "SuperAdmin")]`
- [ ] Repositorios de admin usan `IgnoreQueryFilters()` para acceso cross-tenant
- [ ] Toda acción de admin registrada en el audit trail con `impersonated_by` si aplica
- [ ] Token de impersonación de corta duración (1 hora) con claim `is_impersonation`
- [ ] Acciones destructivas bloqueadas durante impersonación
- [ ] Panel de admin detrás de IP whitelist o VPN
- [ ] Swagger separado para endpoints admin (`GroupName = "admin"`)
- [ ] Impersonación requiere rol `SuperAdmin` — nunca disponible para roles de tenant

---

*Rogelio Arriaga Gonzalez*
