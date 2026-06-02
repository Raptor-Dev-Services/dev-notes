# 30 — Configuración por Tenant

En un SaaS multi-tenant, cada empresa puede tener configuraciones propias: zona horaria, idioma, logo, features habilitadas, límites personalizados, integraciones activas. Esta configuración es diferente para cada tenant (y puede variar por branch) y se carga al inicio del request.

---

## Tipos de configuración

```
Configuración de Tenant
├── Configuración Global del Tenant       → aplica a todo el tenant
│   ├── Timezone                          → "America/Mexico_City"
│   ├── Locale                            → "es-MX"
│   ├── LogoUrl                           → CDN URL
│   ├── PrimaryColor                      → "#FF5500"
│   └── MaxUsers                          → 50
│
├── Feature Flags por Tenant              → features habilitadas/deshabilitadas
│   ├── IsReportsEnabled                  → true/false
│   ├── IsApiAccessEnabled                → true/false
│   └── IsWebhooksEnabled                 → true/false
│
└── Configuración por Branch              → settings específicos de cada branch
    ├── Branch Timezone                   → puede diferir del tenant
    └── Branch-specific settings
```

---

## Estrategia A — Key-Value genérico

```csharp
// Tenancy.Domain/Entities/TenantSetting.cs
public sealed class TenantSetting
{
    public long    Id        { get; init; }
    public long    TenantId  { get; init; }
    public long?   BranchId  { get; init; }   // null = setting del tenant, valor = setting del branch
    public string  Key       { get; init; } = string.Empty;
    public string  Value     { get; init; } = string.Empty;   // siempre string, parsear al leer
    public DateTime UpdatedAtUtc { get; init; }
}
```

```
dbo.tenant_settings
┌─────┬───────────┬───────────┬────────────────────┬──────────────────────┐
│ id  │ tenant_id │ branch_id │ key                │ value                │
├─────┼───────────┼───────────┼────────────────────┼──────────────────────┤
│   1 │         1 │      null │ timezone           │ America/Mexico_City  │
│   2 │         1 │      null │ locale             │ es-MX                │
│   3 │         1 │      null │ max_users          │ 50                   │
│   4 │         1 │      null │ feature:reports    │ true                 │
│   5 │         1 │        10 │ timezone           │ America/Monterrey    │ ← override por branch
│   6 │         2 │      null │ timezone           │ America/New_York     │
└─────┴───────────┴───────────┴────────────────────┴──────────────────────┘
```

Ventaja: extensible sin cambiar el esquema. Desventaja: no hay tipado.

### Clave de settings — constantes

```csharp
public static class TenantSettingKeys
{
    // Generales
    public const string Timezone     = "timezone";
    public const string Locale       = "locale";
    public const string LogoUrl      = "logo_url";
    public const string PrimaryColor = "primary_color";
    public const string MaxUsers     = "max_users";

    // Feature flags
    public const string FeatureReports  = "feature:reports";
    public const string FeatureApi      = "feature:api_access";
    public const string FeatureWebhooks = "feature:webhooks";
}
```

---

## Estrategia B — Columnas tipadas en la tabla de Tenants

```csharp
// Tenancy.Domain/Entities/Tenant.cs — settings integrados
public sealed class Tenant
{
    public long     Id           { get; init; }
    public Guid     PublicId     { get; init; }
    public string   Name         { get; init; } = string.Empty;
    public string   Slug         { get; init; } = string.Empty;
    public string   Timezone     { get; init; } = "UTC";
    public string   Locale       { get; init; } = "en-US";
    public string?  LogoUrl      { get; init; }
    public string?  PrimaryColor { get; init; }
    public int      MaxUsers     { get; init; } = 10;
    public bool     IsReportsEnabled  { get; init; }
    public bool     IsApiEnabled      { get; init; }
    public bool     IsWebhooksEnabled { get; init; }
    public TenantStatus Status   { get; init; }
    public string   Plan         { get; init; } = "trial";
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
```

Ventaja: tipado, simple, un solo query para cargar todo. Desventaja: cambiar la configuración requiere migración de esquema.

**Recomendación:** usar columnas tipadas para settings core (timezone, locale, plan) y Key-Value para feature flags y extensiones.

---

## ITenantConfigAccessor — leer settings en la request

```csharp
// Common/MultiTenancy/ITenantConfigAccessor.cs
public interface ITenantConfigAccessor
{
    TenantConfig? Current { get; set; }
}

public sealed record TenantConfig(
    long    TenantId,
    string  Timezone,
    string  Locale,
    bool    IsReportsEnabled,
    bool    IsApiEnabled,
    bool    IsWebhooksEnabled,
    int     MaxUsers);

public sealed class TenantConfigAccessor : ITenantConfigAccessor
{
    private static readonly AsyncLocal<TenantConfig?> _current = new();
    public TenantConfig? Current { get => _current.Value; set => _current.Value = value; }
}
```

```csharp
// Host.Api/Middleware/TenantConfigMiddleware.cs
public sealed class TenantConfigMiddleware
{
    private readonly RequestDelegate      _next;
    private readonly ITenantContextAccessor _tenantAccessor;
    private readonly ITenantConfigAccessor  _configAccessor;
    private readonly ITenantSettingRepository _settings;

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = long.TryParse(
            _tenantAccessor.Current?.TenantId, out var id) ? id : (long?)null;

        if (tenantId.HasValue)
        {
            var config = await _settings.GetConfigAsync(tenantId.Value);
            _configAccessor.Current = config;
        }

        await _next(context);
    }
}
```

---

## Caché de configuración

Cargar la configuración del tenant en cada request desde la DB es costoso. Cachear con un TTL corto (5-15 minutos):

```csharp
// Tenancy.Infrastructure/Repositories/TenantSettingRepository.cs
public sealed class TenantSettingRepository : ITenantSettingRepository
{
    private readonly AppDbContext     _db;
    private readonly IMemoryCache     _cache;
    private readonly ILogger<TenantSettingRepository> _logger;

    public async Task<TenantConfig?> GetConfigAsync(
        long tenantId, CancellationToken ct = default)
    {
        var cacheKey = $"tenant_config:{tenantId}";

        if (_cache.TryGetValue(cacheKey, out TenantConfig? cached))
            return cached;

        var settings = await _db.TenantSettings
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.BranchId == null)
            .ToListAsync(ct);

        var config = MapToConfig(tenantId, settings);

        _cache.Set(cacheKey, config, TimeSpan.FromMinutes(5));
        return config;
    }

    public async Task InvalidateCacheAsync(long tenantId)
    {
        _cache.Remove($"tenant_config:{tenantId}");
        await Task.CompletedTask;
    }

    private static TenantConfig MapToConfig(long tenantId, List<TenantSetting> settings)
    {
        string Get(string key, string def) =>
            settings.FirstOrDefault(s => s.Key == key)?.Value ?? def;

        bool GetBool(string key, bool def) =>
            bool.TryParse(Get(key, def.ToString()), out var v) ? v : def;

        int GetInt(string key, int def) =>
            int.TryParse(Get(key, def.ToString()), out var v) ? v : def;

        return new TenantConfig(
            TenantId:          tenantId,
            Timezone:          Get(TenantSettingKeys.Timezone, "UTC"),
            Locale:            Get(TenantSettingKeys.Locale, "en-US"),
            IsReportsEnabled:  GetBool(TenantSettingKeys.FeatureReports, false),
            IsApiEnabled:      GetBool(TenantSettingKeys.FeatureApi, false),
            IsWebhooksEnabled: GetBool(TenantSettingKeys.FeatureWebhooks, false),
            MaxUsers:          GetInt(TenantSettingKeys.MaxUsers, 10));
    }
}
```

---

## Configuración por Branch

En sistemas con jerarquía Tenant + Branch, los branches pueden sobreescribir settings del tenant:

```csharp
// Obtener el timezone efectivo: branch override > tenant default
public async Task<string> GetEffectiveTimezoneAsync(
    long tenantId, long? branchId, CancellationToken ct)
{
    // 1. Buscar override de branch
    if (branchId.HasValue)
    {
        var branchTz = await _db.TenantSettings
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId
                && s.BranchId == branchId
                && s.Key == TenantSettingKeys.Timezone)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

        if (branchTz is not null) return branchTz;
    }

    // 2. Fallback al setting del tenant
    var tenantTz = await _db.TenantSettings
        .AsNoTracking()
        .Where(s => s.TenantId == tenantId
            && s.BranchId == null
            && s.Key == TenantSettingKeys.Timezone)
        .Select(s => s.Value)
        .FirstOrDefaultAsync(ct);

    return tenantTz ?? "UTC";
}
```

---

## Actualizar configuración

```csharp
// Tenancy.Application/UseCases/UpdateTenantSettings/UpdateTenantSettingsHandler.cs
public async Task<UpdateTenantSettingsResponse> Handle(
    UpdateTenantSettingsRequest request, CancellationToken ct)
{
    foreach (var (key, value) in request.Settings)
    {
        // Validar que la clave es permitida
        if (!AllowedKeys.Contains(key))
            return new UpdateTenantSettingsValidationFailure(
                $"Clave '{key}' no permitida.");

        var existing = await _settings.GetByKeyAsync(
            request.TenantId, request.BranchId, key, ct);

        if (existing is null)
            await _settings.InsertAsync(request.TenantId, request.BranchId, key, value, ct);
        else
            await _settings.UpdateValueAsync(existing.Id, value, ct);
    }

    // Invalidar caché
    await _settings.InvalidateCacheAsync(request.TenantId);

    return new UpdateTenantSettingsSuccess();
}

private static readonly HashSet<string> AllowedKeys = new()
{
    TenantSettingKeys.Timezone,
    TenantSettingKeys.Locale,
    TenantSettingKeys.LogoUrl,
    TenantSettingKeys.PrimaryColor,
};
```

---

## Feature flags por plan

Los feature flags combinan configuración del tenant con el plan de suscripción. El plan define el techo; la configuración del tenant puede estar por debajo:

```csharp
public sealed class FeatureService : IFeatureService
{
    private readonly ITenantConfigAccessor _config;

    // ¿El tenant tiene acceso a reports?
    public bool IsReportsAvailable() =>
        _config.Current?.IsReportsEnabled ?? false;

    // En el handler: verificar antes de ejecutar
    public async Task<GetReportResponse> Handle(GetReportRequest request, CancellationToken ct)
    {
        if (!_featureService.IsReportsAvailable())
            return new GetReportFeatureNotAvailableFailure(
                "Los reportes no están habilitados en tu plan.");
        // ...
    }
}
```

---

## Settings defaults por plan al crear el tenant

Al registrar un tenant nuevo, el sistema inicializa los settings según el plan:

```csharp
public static Dictionary<string, string> GetDefaultSettingsForPlan(string plan) => plan switch
{
    "trial" => new()
    {
        [TenantSettingKeys.MaxUsers]        = "5",
        [TenantSettingKeys.FeatureReports]  = "false",
        [TenantSettingKeys.FeatureApi]      = "false",
        [TenantSettingKeys.FeatureWebhooks] = "false",
    },
    "pro" => new()
    {
        [TenantSettingKeys.MaxUsers]        = "50",
        [TenantSettingKeys.FeatureReports]  = "true",
        [TenantSettingKeys.FeatureApi]      = "true",
        [TenantSettingKeys.FeatureWebhooks] = "false",
    },
    "enterprise" => new()
    {
        [TenantSettingKeys.MaxUsers]        = "unlimited",
        [TenantSettingKeys.FeatureReports]  = "true",
        [TenantSettingKeys.FeatureApi]      = "true",
        [TenantSettingKeys.FeatureWebhooks] = "true",
    },
    _ => new()
};
```

---

## Endpoint de settings

```csharp
[Route("api/tenant/settings")]
[Authorize(Roles = "Admin")]
public sealed class TenantSettingsController : BaseApiController
{
    // Obtener settings del tenant
    [HttpGet]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        _ = await Mediator.Send(new GetTenantSettingsRequest(CurrentTenantId), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }

    // Actualizar settings del tenant
    [HttpPatch]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateTenantSettingsBody body, CancellationToken ct)
    {
        _ = await Mediator.Send(new UpdateTenantSettingsRequest(
            CurrentTenantId, null, body.Settings), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }

    // Obtener settings de un branch (Admin corporativo)
    [HttpGet("branches/{branchId:long}")]
    public async Task<IActionResult> GetBranchSettings(long branchId, CancellationToken ct)
    {
        _ = await Mediator.Send(new GetTenantSettingsRequest(CurrentTenantId, branchId), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }
}
```

---

## Checklist

- [ ] Settings cacheados con TTL (evitar query por request)
- [ ] Invalidar caché al actualizar settings
- [ ] Branch puede sobreescribir settings del tenant (jerarquía branch > tenant)
- [ ] Feature flags determinados por plan + settings del tenant
- [ ] Settings inicializados con defaults al crear el tenant
- [ ] Claves de settings en constantes — nunca strings hardcodeados en el código
- [ ] Solo Admin puede modificar settings del tenant
- [ ] Settings sensibles (API keys, secrets) no se retornan en el GET — solo se actualizan

---

## Glosario

| Término | Definición |
|---------|-----------|
| TenantSetting | Entidad que almacena configuración clave-valor por tenant — permite personalización sin código |
| TenantSettingKeys | Clase de constantes que define los nombres de todas las claves de configuración disponibles |
| ITenantConfigAccessor | Interfaz que provee acceso tipado a la configuración del tenant actual con soporte de caché |
| TenantConfig | Diccionario en memoria de los settings del tenant actual — cargado una vez por request o con caché |
| Feature Flags | Settings booleanos que habilitan o deshabilitan funcionalidades para tenants específicos |
| Key-Value Store | Patrón de almacenamiento de configuración como pares clave-valor — flexible y extensible sin migrations |
| Jerarquía branch > tenant | Regla donde el setting de una branch sobreescribe al setting del tenant si ambos existen |
| Caché de configuración | Almacenamiento temporal de los settings del tenant para evitar queries repetidas en cada request |
| Plan Defaults | Valores por defecto de configuración determinados por el plan del tenant — base de la jerarquía |
| Settings sensibles | Settings que contienen secretos (API keys, tokens) — no se retornan en el GET, solo se actualizan |
| UpdateTenantSetting | Caso de uso para actualizar un setting del tenant — solo ejecutable por el rol Admin |

---

*Rogelio Arriaga Gonzalez*
