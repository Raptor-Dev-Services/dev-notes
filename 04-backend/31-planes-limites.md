# 31 — Planes, Tiers y Límites de Uso

Los planes (Free, Pro, Enterprise) definen qué features tiene disponibles cada tenant y cuánto puede usar. Los límites se validan en tiempo de ejecución. Antes de permitir una operación, el sistema verifica si el tenant tiene cuota disponible.

---

## Modelo de planes

```
Plan Trial     → 14 días gratis, 5 usuarios, sin reportes, sin API
Plan Free      → gratis ilimitado, 3 usuarios, features básicas
Plan Pro       → $49/mes, 50 usuarios, reportes, API key, sin webhooks
Plan Enterprise→ precio negociado, usuarios ilimitados, todo habilitado, SLA
```

---

## Definición de límites por plan

```csharp
// Common/Plans/PlanLimits.cs
public sealed record PlanLimits(
    int     MaxUsers,           // -1 = ilimitado
    int     MaxBranches,        // -1 = ilimitado
    int     MaxApiCallsPerDay,  // -1 = ilimitado
    bool    ReportsEnabled,
    bool    ApiAccessEnabled,
    bool    WebhooksEnabled,
    bool    SsoEnabled,
    int     StorageLimitMb      // -1 = ilimitado
);

public static class Plans
{
    public const string Trial      = "trial";
    public const string Free       = "free";
    public const string Pro        = "pro";
    public const string Enterprise = "enterprise";

    public static PlanLimits GetLimits(string plan) => plan switch
    {
        Trial => new PlanLimits(
            MaxUsers:          5,
            MaxBranches:       1,
            MaxApiCallsPerDay: 0,
            ReportsEnabled:    false,
            ApiAccessEnabled:  false,
            WebhooksEnabled:   false,
            SsoEnabled:        false,
            StorageLimitMb:    100),

        Free => new PlanLimits(
            MaxUsers:          3,
            MaxBranches:       1,
            MaxApiCallsPerDay: 0,
            ReportsEnabled:    false,
            ApiAccessEnabled:  false,
            WebhooksEnabled:   false,
            SsoEnabled:        false,
            StorageLimitMb:    50),

        Pro => new PlanLimits(
            MaxUsers:          50,
            MaxBranches:       10,
            MaxApiCallsPerDay: 10_000,
            ReportsEnabled:    true,
            ApiAccessEnabled:  true,
            WebhooksEnabled:   false,
            SsoEnabled:        false,
            StorageLimitMb:    5_000),

        Enterprise => new PlanLimits(
            MaxUsers:          -1,
            MaxBranches:       -1,
            MaxApiCallsPerDay: -1,
            ReportsEnabled:    true,
            ApiAccessEnabled:  true,
            WebhooksEnabled:   true,
            SsoEnabled:        true,
            StorageLimitMb:    -1),

        _ => throw new ArgumentException($"Plan desconocido: {plan}")
    };
}
```

---

## IPlanService: verificación de límites

```csharp
// Common/Plans/IPlanService.cs
public interface IPlanService
{
    Task<PlanCheckResult> CanAddUserAsync(long tenantId, CancellationToken ct = default);
    Task<PlanCheckResult> CanAddBranchAsync(long tenantId, CancellationToken ct = default);
    Task<PlanCheckResult> CanUseReportsAsync(long tenantId, CancellationToken ct = default);
    Task<PlanCheckResult> CanUseApiAsync(long tenantId, CancellationToken ct = default);
    Task<PlanCheckResult> CanUploadFileAsync(long tenantId, long fileSizeBytes, CancellationToken ct = default);
}

public sealed record PlanCheckResult(bool Allowed, string? Reason = null)
{
    public static PlanCheckResult Ok()               => new(true);
    public static PlanCheckResult Denied(string msg) => new(false, msg);
}
```

```csharp
// Tenancy.Application/Services/PlanService.cs
public sealed class PlanService : IPlanService
{
    private readonly ITenantRepository   _tenants;
    private readonly IUserCountRepository _userCounts;
    private readonly IBranchCountRepository _branchCounts;
    private readonly IStorageUsageRepository _storage;

    public async Task<PlanCheckResult> CanAddUserAsync(long tenantId, CancellationToken ct)
    {
        var tenant = await _tenants.GetByIdAsync(tenantId, ct);
        if (tenant is null) return PlanCheckResult.Denied("Tenant no encontrado.");

        var limits = Plans.GetLimits(tenant.Plan);
        if (limits.MaxUsers == -1) return PlanCheckResult.Ok();   // ilimitado

        var currentCount = await _userCounts.CountActiveAsync(tenantId, ct);
        if (currentCount >= limits.MaxUsers)
            return PlanCheckResult.Denied(
                $"Tu plan {tenant.Plan} permite máximo {limits.MaxUsers} usuarios. " +
                "Actualiza tu plan para agregar más.");

        return PlanCheckResult.Ok();
    }

    public async Task<PlanCheckResult> CanAddBranchAsync(long tenantId, CancellationToken ct)
    {
        var tenant = await _tenants.GetByIdAsync(tenantId, ct);
        if (tenant is null) return PlanCheckResult.Denied("Tenant no encontrado.");

        var limits = Plans.GetLimits(tenant.Plan);
        if (limits.MaxBranches == -1) return PlanCheckResult.Ok();

        var currentCount = await _branchCounts.CountActiveAsync(tenantId, ct);
        if (currentCount >= limits.MaxBranches)
            return PlanCheckResult.Denied(
                $"Tu plan {tenant.Plan} permite máximo {limits.MaxBranches} branches.");

        return PlanCheckResult.Ok();
    }

    public async Task<PlanCheckResult> CanUseReportsAsync(long tenantId, CancellationToken ct)
    {
        var tenant = await _tenants.GetByIdAsync(tenantId, ct);
        if (tenant is null) return PlanCheckResult.Denied("Tenant no encontrado.");

        var limits = Plans.GetLimits(tenant.Plan);
        return limits.ReportsEnabled
            ? PlanCheckResult.Ok()
            : PlanCheckResult.Denied("Los reportes no están disponibles en tu plan. Actualiza a Pro.");
    }

    public async Task<PlanCheckResult> CanUploadFileAsync(
        long tenantId, long fileSizeBytes, CancellationToken ct)
    {
        var tenant = await _tenants.GetByIdAsync(tenantId, ct);
        if (tenant is null) return PlanCheckResult.Denied("Tenant no encontrado.");

        var limits = Plans.GetLimits(tenant.Plan);
        if (limits.StorageLimitMb == -1) return PlanCheckResult.Ok();

        var usedMb   = await _storage.GetUsedMbAsync(tenantId, ct);
        var fileMb   = fileSizeBytes / 1_048_576.0;
        var limitMb  = (double)limits.StorageLimitMb;

        if (usedMb + fileMb > limitMb)
            return PlanCheckResult.Denied(
                $"Has alcanzado el límite de almacenamiento " +
                $"({limits.StorageLimitMb} MB). Actualiza tu plan.");

        return PlanCheckResult.Ok();
    }
}
```

---

## Integrar límites en handlers

```csharp
// Users.Application/UseCases/InviteUser/InviteUserHandler.cs
public async Task<InviteUserResponse> Handle(InviteUserRequest request, CancellationToken ct)
{
    // Verificar límite de usuarios ANTES de crear la invitación
    var check = await _planService.CanAddUserAsync(request.TenantId, ct);
    if (!check.Allowed)
        return new InviteUserLimitReachedFailure(check.Reason!);

    // Proceder con la invitación
    // ...
}

// Reports.Application/UseCases/GetReport/GetReportHandler.cs
public async Task<GetReportResponse> Handle(GetReportRequest request, CancellationToken ct)
{
    var check = await _planService.CanUseReportsAsync(request.TenantId, ct);
    if (!check.Allowed)
        return new GetReportFeatureNotAvailableFailure(check.Reason!);

    // Generar reporte
    // ...
}
```

---

## Response de límite alcanzado

Usar `IConflictFailure` o un tipo dedicado `IPlanLimitFailure`:

```csharp
// Common/Results/IPlanLimitFailure.cs
public interface IPlanLimitFailure : IFailure
{
    string UpgradeUrl { get; }   // link al upgrade de plan
}

// Users.Application/UseCases/InviteUser/Responses/
public sealed record InviteUserLimitReachedFailure(string Message)
    : InviteUserResponse, IPlanLimitFailure
{
    public string UpgradeUrl => "https://misaas.com/upgrade";
}
```

En el presenter, mapear a HTTP 402 (Payment Required) o 403:

```csharp
// HTTP 402 Payment Required — la semántica correcta para "necesitas pagar más"
if (notification is IPlanLimitFailure)
    _viewModel.SetStatus(402, notification.Message);
```

---

## Visualización del uso en el dashboard

El tenant puede ver cuánto ha consumido de sus límites:

```csharp
// Tenancy.Application/UseCases/GetUsageSummary/GetUsageSummaryHandler.cs
public async Task<GetUsageSummaryResponse> Handle(
    GetUsageSummaryRequest request, CancellationToken ct)
{
    var tenant = await _tenants.GetByIdAsync(request.TenantId, ct);
    var limits = Plans.GetLimits(tenant!.Plan);

    var summary = new UsageSummaryDto(
        Plan:          tenant.Plan,
        Users:         new UsageMetric(
            await _userCounts.CountActiveAsync(request.TenantId, ct),
            limits.MaxUsers),
        Branches:      new UsageMetric(
            await _branchCounts.CountActiveAsync(request.TenantId, ct),
            limits.MaxBranches),
        StorageMb:     new UsageMetric(
            (int)await _storage.GetUsedMbAsync(request.TenantId, ct),
            limits.StorageLimitMb),
        ApiCallsToday: new UsageMetric(
            await _apiCalls.CountTodayAsync(request.TenantId, ct),
            limits.MaxApiCallsPerDay));

    return new GetUsageSummarySuccess(summary);
}

public sealed record UsageMetric(int Used, int Limit)
{
    public double Percentage => Limit == -1 ? 0 : (double)Used / Limit * 100;
    public bool   IsUnlimited => Limit == -1;
    public bool   IsNearLimit  => !IsUnlimited && Percentage >= 80;
}
```

---

## Tabla de uso por tenant+branch

En un sistema con branches, el conteo de usuarios puede ser global (tenant) o por branch:

```
Tenant A tiene plan Pro → 50 usuarios en total
├── Branch Norte → 20 usuarios
├── Branch Sur   → 15 usuarios
└── Corporativo  → 15 usuarios
                   ──────────
                   50 usuarios (límite alcanzado)
```

```csharp
// Conteo global del tenant (todos sus branches)
var totalUsers = await _db.Credentials
    .IgnoreQueryFilters()
    .CountAsync(e => e.TenantId == tenantId && e.IsActive, ct);
```

El límite de usuarios del plan es **por tenant**, no por branch.

---

## Cambio de plan

Al cambiar el plan de un tenant (upgrade o downgrade):

```csharp
public async Task<ChangePlanResponse> Handle(ChangePlanRequest request, CancellationToken ct)
{
    var tenant = await _tenants.GetByIdAsync(request.TenantId, ct);
    var newLimits = Plans.GetLimits(request.NewPlan);

    // Downgrade: verificar que el uso actual cabe en el nuevo plan
    if (newLimits.MaxUsers != -1)
    {
        var currentUsers = await _userCounts.CountActiveAsync(request.TenantId, ct);
        if (currentUsers > newLimits.MaxUsers)
            return new ChangePlanConflictFailure(
                $"Tienes {currentUsers} usuarios activos. El plan {request.NewPlan} " +
                $"permite {newLimits.MaxUsers}. Desactiva usuarios antes de degradar.");
    }

    await _tenants.UpdatePlanAsync(request.TenantId, request.NewPlan, ct);

    // Actualizar los feature flags en TenantSettings
    var defaultSettings = Plans.GetDefaultSettingsForPlan(request.NewPlan);
    await _settings.BulkUpsertAsync(request.TenantId, defaultSettings, ct);

    return new ChangePlanSuccess();
}
```

---

## Alertas de límite próximo

Notificar al Admin cuando se acerca al límite (80% de uso):

```csharp
// Background job — se ejecuta diariamente
public async Task CheckLimitsAsync(CancellationToken ct)
{
    var tenants = await _tenants.GetAllActiveAsync(ct);
    foreach (var tenant in tenants)
    {
        var limits  = Plans.GetLimits(tenant.Plan);
        var users   = await _userCounts.CountActiveAsync(tenant.Id, ct);

        if (limits.MaxUsers != -1)
        {
            var pct = (double)users / limits.MaxUsers * 100;
            if (pct >= 80 && pct < 100)
                await _email.SendLimitWarningAsync(tenant, "usuarios", users, limits.MaxUsers, ct);
            if (pct >= 100)
                await _email.SendLimitReachedAsync(tenant, "usuarios", ct);
        }
    }
}
```

---

## Checklist

- [ ] Los límites del plan están centralizados en `Plans.GetLimits()`: no hardcodeados por feature
- [ ] Los handlers verifican el límite ANTES de ejecutar la operación
- [ ] HTTP 402 para respuestas de límite alcanzado: incluir link de upgrade
- [ ] El downgrade de plan verifica que el uso actual cabe en el nuevo plan
- [ ] El conteo de usuarios es tenant-global (suma de todos los branches)
- [ ] Dashboard de uso disponible para el Admin
- [ ] Alertas al 80% y al 100% de uso

---

## Glosario

| Término | Definición |
|---------|-----------|
| PlanLimits | Clase que define los límites de uso para un plan: MaxUsers, MaxProjects, MaxStorageMb, etc. |
| Plans.GetLimits | Método centralizado que retorna los PlanLimits de un plan dado — única fuente de verdad |
| IPlanService | Interfaz de servicio que verifica si el tenant puede realizar una acción dada su suscripción actual |
| PlanCheckResult | Resultado de la verificación de límite: Allowed u OnLimitReached con mensaje de contexto |
| UsageMetric | Métrica de uso actual del tenant: usuarios activos, proyectos creados, almacenamiento usado |
| CanAddUserAsync | Método representativo de IPlanService que verifica si el tenant puede agregar un usuario más |
| HTTP 402 | Payment Required — código de respuesta estándar cuando el tenant ha alcanzado el límite de su plan |
| Downgrade Validation | Verificación que impide bajar de plan si el uso actual supera los límites del plan inferior |
| MaxUsers | Límite de usuarios activos por tenant — varía por plan (Free: 5, Pro: 50, Enterprise: ilimitado) |
| Tenant-global Count | Conteo que suma usuarios o recursos de todos los branches del tenant para comparar con el límite |
| Alerta de uso | Notificación automática al Admin cuando el tenant alcanza el 80% o 100% de un límite de plan |

---

*Rogelio Arriaga Gonzalez*
