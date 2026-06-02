# 41 — Background Jobs en Multi-Tenant

Los background jobs procesan tareas fuera del ciclo request-response: envío de emails, generación de reportes, sincronización de datos, limpieza periódica. En un SaaS multi-tenant, el desafío es que cada job necesita saber para qué tenant está trabajando — sin ese contexto, los Global Query Filters de EF Core retornan 0 filas y los datos se mezclan.

---

## El problema sin contexto de tenant

```csharp
// ❌ Job sin contexto de tenant
public class ReportGenerationJob
{
    private readonly AppDbContext _db;

    public async Task ExecuteAsync(long tenantId, CancellationToken ct)
    {
        // El TenantContextAccessor está vacío (no hay request HTTP)
        // _db.UserProfiles aplica el filtro WHERE tenant_id = 0
        // → retorna 0 filas aunque haya datos para el tenant
        var profiles = await _db.UserProfiles.ToListAsync(ct);
    }
}
```

---

## Solución: establecer el TenantContext manualmente

```csharp
// ✓ Job con contexto de tenant explícito
public class ReportGenerationJob
{
    private readonly IServiceScopeFactory      _scopeFactory;
    private readonly ITenantContextAccessor    _tenantAccessor;

    public async Task ExecuteAsync(long tenantId, long? branchId, CancellationToken ct)
    {
        // Establecer el contexto ANTES de usar el DbContext
        _tenantAccessor.Current = new TenantContext(
            tenantId.ToString(), branchId?.ToString());

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ahora los filtros funcionan correctamente
        var profiles = await db.UserProfiles.ToListAsync(ct);
        // → WHERE tenant_id = @tenantId aplicado automáticamente
    }
}
```

---

## Patrón base para jobs multi-tenant

```csharp
// Common/BackgroundJobs/TenantScopedJob.cs
public abstract class TenantScopedJob
{
    private readonly IServiceScopeFactory   _scopeFactory;
    private readonly ITenantContextAccessor _tenantAccessor;
    private readonly ILogger                _logger;

    protected TenantScopedJob(
        IServiceScopeFactory scopeFactory,
        ITenantContextAccessor tenantAccessor,
        ILogger logger)
    {
        _scopeFactory   = scopeFactory;
        _tenantAccessor = tenantAccessor;
        _logger         = logger;
    }

    // Ejecutar el job para UN tenant específico
    protected async Task ExecuteForTenantAsync(
        long tenantId, long? branchId, CancellationToken ct)
    {
        _tenantAccessor.Current = new TenantContext(tenantId.ToString(), branchId?.ToString());

        using var scope = _scopeFactory.CreateScope();
        var services    = scope.ServiceProvider;

        try
        {
            await ExecuteCoreAsync(tenantId, branchId, services, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error en job {JobName} para TenantId={TenantId}",
                GetType().Name, tenantId);
            throw;
        }
        finally
        {
            _tenantAccessor.Current = null;
        }
    }

    // Ejecutar el job para TODOS los tenants activos
    protected async Task ExecuteForAllTenantsAsync(CancellationToken ct)
    {
        // Leer tenants SIN filtro de tenant (son todos)
        using var scope = _scopeFactory.CreateScope();
        var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenants = await db.Tenants
            .AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active)
            .Select(t => new { t.Id })
            .ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            await ExecuteForTenantAsync(tenant.Id, branchId: null, ct);
        }
    }

    // Subclases implementan la lógica específica
    protected abstract Task ExecuteCoreAsync(
        long tenantId, long? branchId,
        IServiceProvider services, CancellationToken ct);
}
```

---

## Tipos de jobs y cuándo usar cada uno

### 1. BackgroundService de .NET (jobs periódicos simples)

Para jobs que corren en loop con un intervalo fijo:

```csharp
// Modules/Reports/Reports.Infrastructure/BackgroundJobs/ReportCleanupJob.cs
public sealed class ReportCleanupJob : TenantScopedJob, IHostedService
{
    private readonly ILogger<ReportCleanupJob> _logger;
    private Task? _runningTask;
    private CancellationTokenSource? _cts;

    public Task StartAsync(CancellationToken ct)
    {
        _cts         = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _runningTask = RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        // Ejecutar al inicio y luego cada 24 horas
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ExecuteForAllTenantsAsync(ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error en ReportCleanupJob");
            }
            await Task.Delay(TimeSpan.FromHours(24), ct);
        }
    }

    protected override async Task ExecuteCoreAsync(
        long tenantId, long? branchId, IServiceProvider services, CancellationToken ct)
    {
        var reportRepo = services.GetRequiredService<IReportRepository>();
        // Eliminar reportes temporales de más de 7 días
        await reportRepo.DeleteOldTemporaryAsync(tenantId, DateTime.UtcNow.AddDays(-7), ct);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _cts?.Cancel();
        if (_runningTask is not null)
            await _runningTask.WaitAsync(ct);
    }
}
```

### 2. Hangfire (jobs persistidos, con UI, reintentos)

Para jobs de larga duración que necesitan sobrevivir reinicios del servidor:

```xml
<!-- Host.Api.csproj -->
<PackageReference Include="Hangfire.AspNetCore"            Version="1.8.*" />
<PackageReference Include="Hangfire.PostgreSql"            Version="1.20.*" />
```

```csharp
// Host.Api/Program.cs
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(
        connectionString, new PostgreSqlStorageOptions
        {
            SchemaName = "hangfire"
        }));

builder.Services.AddHangfireServer();
```

```csharp
// Encolar un job para un tenant específico
public sealed class ExportJobService : IExportJobService
{
    private readonly IBackgroundJobClient _jobClient;

    public Task<string> EnqueueExportAsync(long tenantId, long? branchId, string reportType)
    {
        var jobId = _jobClient.Enqueue<ExportJob>(
            job => job.ExecuteAsync(tenantId, branchId, reportType, CancellationToken.None));
        return Task.FromResult(jobId);
    }

    public Task ScheduleDailyAsync(long tenantId)
    {
        RecurringJob.AddOrUpdate<DailyReportJob>(
            $"daily-report-{tenantId}",                    // ID único por tenant
            job => job.ExecuteAsync(tenantId, null, CancellationToken.None),
            "0 6 * * *");                                  // 6 AM diario
        return Task.CompletedTask;
    }
}
```

```csharp
// ExportJob.cs — job con contexto de tenant
public sealed class ExportJob
{
    private readonly ITenantContextAccessor _tenantAccessor;
    private readonly IServiceScopeFactory   _scopeFactory;
    private readonly IStorageService        _storage;

    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(long tenantId, long? branchId,
        string reportType, CancellationToken ct)
    {
        // Establecer contexto de tenant
        _tenantAccessor.Current = new TenantContext(tenantId.ToString(), branchId?.ToString());

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Aquí los filtros aplican correctamente
        var data = await db.UserProfiles
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(ct);

        // Generar y guardar el reporte
        var key = StoragePaths.Export(tenantId, branchId, reportType);
        await using var stream = GenerateExcelReport(data);
        await _storage.UploadAsync(key, stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ct);
    }
}
```

---

## Jobs que procesan TODOS los tenants

Algunos jobs procesan todos los tenants en secuencia (limpieza, alertas de límite, emails):

```csharp
// Tenancy.Infrastructure/BackgroundJobs/LimitAlertJob.cs
public sealed class LimitAlertJob : BackgroundService
{
    private readonly IServiceScopeFactory   _scopeFactory;
    private readonly ITenantContextAccessor _tenantAccessor;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await CheckAllTenantsAsync(ct);
            // Ejecutar una vez al día a las 8 AM
            var nextRun = DateTime.UtcNow.Date.AddDays(1).AddHours(8);
            await Task.Delay(nextRun - DateTime.UtcNow, ct);
        }
    }

    private async Task CheckAllTenantsAsync(CancellationToken ct)
    {
        // Leer tenants SIN contexto de tenant (la tabla tenants no tiene query filter)
        using var outerScope = _scopeFactory.CreateScope();
        var outerDb = outerScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenants = await outerDb.Tenants
            .AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active)
            .ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            using var innerScope = _scopeFactory.CreateScope();

            // Establecer contexto para este tenant
            var accessor = innerScope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
            accessor.Current = new TenantContext(tenant.Id.ToString());

            var db          = innerScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailSvc    = innerScope.ServiceProvider.GetRequiredService<IEmailService>();
            var planSvc     = innerScope.ServiceProvider.GetRequiredService<IPlanService>();

            var check = await planSvc.CanAddUserAsync(tenant.Id, ct);
            if (!check.Allowed)
                await emailSvc.SendLimitWarningAsync(tenant.AdminEmail!, tenant.Name,
                    "usuarios", 0, 0, ct);  // valores reales de la DB

            accessor.Current = null;
        }
    }
}
```

---

## Jobs con branch context

En sistemas con branches, los jobs pueden necesitar iterar por branches:

```csharp
public async Task ExecuteForAllBranchesAsync(long tenantId, CancellationToken ct)
{
    // Primero obtener los branches del tenant
    _tenantAccessor.Current = new TenantContext(tenantId.ToString());

    using var scope = _scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var branches = await db.Branches
        .AsNoTracking()
        .Where(b => b.IsActive)
        .ToListAsync(ct);

    foreach (var branch in branches)
    {
        // Ejecutar job para este branch específico
        _tenantAccessor.Current = new TenantContext(
            tenantId.ToString(), branch.Id.ToString());

        await ExecuteBranchJobAsync(tenantId, branch.Id, scope.ServiceProvider, ct);
    }
}
```

---

## Observabilidad — loguear el tenant en jobs

```csharp
// En el job, enriquecer el contexto de Serilog con el tenant
using (LogContext.PushProperty("TenantId", tenantId))
using (LogContext.PushProperty("BranchId", branchId))
using (LogContext.PushProperty("JobName", GetType().Name))
{
    _logger.LogInformation("Iniciando job para TenantId={TenantId}", tenantId);
    // ...
    _logger.LogInformation("Job completado en {ElapsedMs}ms", sw.ElapsedMilliseconds);
}
```

---

## Checklist

- [ ] Siempre establecer `_tenantAccessor.Current` antes de usar `AppDbContext` en un job
- [ ] Limpiar `_tenantAccessor.Current = null` en el bloque `finally`
- [ ] Crear un scope DI nuevo por tenant al iterar todos los tenants
- [ ] Los jobs de Hangfire con múltiples tenants usan IDs únicos por tenant (`daily-report-{tenantId}`)
- [ ] Reintentos configurados en Hangfire (`[AutomaticRetry(Attempts = 3)]`)
- [ ] Los errores por tenant no deben cancelar el procesamiento de los demás tenants
- [ ] Logs enriquecidos con `TenantId` y `BranchId` para diagnóstico
- [ ] Jobs de larga duración en Hangfire (persistencia) — jobs simples periódicos en BackgroundService

---

*Rogelio Arriaga Gonzalez*
