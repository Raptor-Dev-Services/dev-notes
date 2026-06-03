# 44 — GDPR: Exportación de Datos y Derecho al Olvido

El GDPR (Reglamento General de Protección de Datos) y leyes equivalentes (LGPD en Brasil, CCPA en California) otorgan a los usuarios y empresas derechos sobre sus datos: derecho a acceso (exportar todos sus datos) y derecho al olvido (eliminar todos sus datos). En un SaaS multi-tenant, estos derechos aplican al nivel del tenant.

---

## Derechos que el SaaS debe implementar

```
Derecho de Acceso (Art. 15 GDPR):
  → El tenant puede solicitar una exportación de TODOS sus datos en formato legible
  → El SaaS tiene 30 días para cumplir

Derecho al Olvido / Supresión (Art. 17 GDPR):
  → El tenant puede solicitar la eliminación de todos sus datos
  → El SaaS tiene 30 días para cumplir
  → Excepciones: datos requeridos por ley (facturas, registros fiscales)

Portabilidad de datos (Art. 20 GDPR):
  → Los datos deben entregarse en formato estructurado y legible por máquina (JSON/CSV)
```

---

## Exportación de datos: flujo

```
1. Admin del tenant solicita exportación: POST /api/data/export
2. API crea un job de exportación (proceso largo → background job)
3. Job recopila todos los datos del tenant:
   - Usuarios y perfiles
   - Credenciales (sin passwords)
   - Registros de negocio (órdenes, facturas, etc.)
   - Audit trail del tenant
   - Configuración del tenant
4. Job genera un ZIP con JSONs y lo sube a S3
5. API notifica al Admin (email + SignalR) con presigned URL
6. Presigned URL válida 7 días: luego se elimina el archivo
```

---

## Entidad DataExportRequest

```csharp
// Compliance.Domain/Entities/DataExportRequest.cs
public sealed class DataExportRequest
{
    public long     Id              { get; init; }
    public Guid     PublicId        { get; init; }
    public long     TenantId        { get; init; }
    public Guid     RequestedByPublicId { get; init; }
    public ExportStatus Status      { get; init; }
    public string?  FileKey         { get; init; }   // key en S3
    public string?  ErrorMessage    { get; init; }
    public DateTime RequestedAtUtc  { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc   { get; init; }   // cuándo expira el link de descarga
}

public enum ExportStatus
{
    Pending    = 0,
    Processing = 1,
    Completed  = 2,
    Failed     = 3,
    Expired    = 4
}
```

---

## Handler de exportación

```csharp
// Compliance.Application/UseCases/RequestDataExport/RequestDataExportHandler.cs
public async Task<RequestDataExportResponse> Handle(
    RequestDataExportRequest request, CancellationToken ct)
{
    // Verificar que no hay exportación en progreso
    var existingPending = await _exportRequests.GetPendingByTenantAsync(request.TenantId, ct);
    if (existingPending is not null)
        return new RequestDataExportConflictFailure(
            "Ya hay una exportación en proceso. Espera a que termine.");

    // Crear el registro de la solicitud
    await _exportRequests.InsertAsync(
        request.TenantId,
        request.RequestedByPublicId,
        ct);

    // Encolar el job de exportación (Hangfire)
    _jobClient.Enqueue<DataExportJob>(
        job => job.ExecuteAsync(request.TenantId, CancellationToken.None));

    return new RequestDataExportSuccess(
        "Tu exportación está en proceso. Te notificaremos cuando esté lista (puede tardar unos minutos).");
}
```

---

## Job de exportación

```csharp
// Compliance.Infrastructure/BackgroundJobs/DataExportJob.cs
public sealed class DataExportJob
{
    private readonly IServiceScopeFactory   _scopeFactory;
    private readonly ITenantContextAccessor _tenantAccessor;
    private readonly IStorageService        _storage;
    private readonly INotificationService   _notifications;
    private readonly IEmailService          _email;

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(long tenantId, CancellationToken ct)
    {
        _tenantAccessor.Current = new TenantContext(tenantId.ToString());

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var exportRequest = await db.DataExportRequests
            .FirstAsync(e => e.TenantId == tenantId && e.Status == ExportStatus.Pending, ct);

        try
        {
            await db.DataExportRequests
                .Where(e => e.Id == exportRequest.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, ExportStatus.Processing), ct);

            // Recopilar todos los datos del tenant
            var exportData = await CollectTenantDataAsync(tenantId, db, ct);

            // Generar ZIP
            var zipBytes = GenerateZip(exportData);
            var fileKey  = $"tenants/{tenantId}/exports/data-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";

            await using var stream = new MemoryStream(zipBytes);
            await _storage.UploadAsync(fileKey, stream, "application/zip", ct);

            // Actualizar estado
            var expiresAt = DateTime.UtcNow.AddDays(7);
            await db.DataExportRequests
                .Where(e => e.Id == exportRequest.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.Status, ExportStatus.Completed)
                    .SetProperty(e => e.FileKey, fileKey)
                    .SetProperty(e => e.CompletedAtUtc, DateTime.UtcNow)
                    .SetProperty(e => e.ExpiresAtUtc, expiresAt), ct);

            // Generar presigned URL para descarga
            var downloadUrl = await _storage.GetPresignedDownloadUrlAsync(
                fileKey, TimeSpan.FromDays(7), ct);

            // Notificar al Admin
            var requestor = await db.Credentials
                .IgnoreQueryFilters()
                .FirstAsync(c => c.PublicId == exportRequest.RequestedByPublicId, ct);

            await _email.SendDataExportReadyAsync(requestor.Email, downloadUrl, expiresAt, ct);
            await _notifications.NotifyUserAsync(
                tenantId, exportRequest.RequestedByPublicId,
                NotificationEvents.ExportReady,
                new { downloadUrl, expiresAt },
                ct);
        }
        catch (Exception ex)
        {
            await db.DataExportRequests
                .Where(e => e.Id == exportRequest.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.Status, ExportStatus.Failed)
                    .SetProperty(e => e.ErrorMessage, ex.Message), ct);
            throw;
        }
    }

    private async Task<TenantExportData> CollectTenantDataAsync(
        long tenantId, AppDbContext db, CancellationToken ct)
    {
        // Recopilar todo con el contexto de tenant activo
        var users = await db.UserProfiles
            .AsNoTracking()
            .Select(p => new { p.PublicId, p.FullName, p.CreatedAtUtc })
            .ToListAsync(ct);

        var credentials = await db.Credentials
            .AsNoTracking()
            .Select(c => new { c.PublicId, c.Email, c.Role, c.CreatedAtUtc })
            // Sin password — nunca exportar hashes
            .ToListAsync(ct);

        var auditEntries = await db.AuditEntries
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(10_000)  // limitar el tamaño
            .ToListAsync(ct);

        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstAsync(t => t.Id == tenantId, ct);

        return new TenantExportData(tenant, users, credentials, auditEntries);
    }

    private static byte[] GenerateZip(TenantExportData data)
    {
        using var ms      = new MemoryStream();
        using var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true);

        AddJsonEntry(archive, "tenant.json",      data.Tenant);
        AddJsonEntry(archive, "users.json",       data.Users);
        AddJsonEntry(archive, "credentials.json", data.Credentials);
        AddJsonEntry(archive, "audit_trail.json", data.AuditEntries);
        AddReadme(archive);

        ms.Seek(0, SeekOrigin.Begin);
        return ms.ToArray();
    }

    private static void AddJsonEntry(ZipArchive archive, string name, object data)
    {
        var entry  = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    private static void AddReadme(ZipArchive archive)
    {
        var entry  = archive.CreateEntry("README.txt");
        using var writer = new StreamWriter(entry.Open());
        writer.Write($"""
            Exportación de datos de MiSaaS
            Generado: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC

            Archivos incluidos:
            - tenant.json:      Información de la empresa
            - users.json:       Perfiles de usuario (sin passwords)
            - credentials.json: Cuentas de acceso (sin passwords ni tokens)
            - audit_trail.json: Historial de cambios (últimos 10,000 registros)

            Para más información: privacy@misaas.com
            """);
    }
}
```

---

## Derecho al olvido: eliminar todos los datos del tenant

```csharp
// Compliance.Application/UseCases/RequestDataDeletion/RequestDataDeletionHandler.cs
public async Task<RequestDataDeletionResponse> Handle(
    RequestDataDeletionRequest request, CancellationToken ct)
{
    // Requiere confirmación explícita del Admin
    if (request.ConfirmationPhrase != $"ELIMINAR {request.TenantSlug}")
        return new RequestDataDeletionValidationFailure(
            $"Para confirmar, escribe exactamente: ELIMINAR {request.TenantSlug}");

    // Verificar que no hay facturas pendientes de pago
    var billing = await _billing.GetByTenantIdAsync(request.TenantId, ct);
    if (billing?.BillingStatus == "past_due")
        return new RequestDataDeletionConflictFailure(
            "Hay facturas pendientes. Resuelve el pago antes de eliminar los datos.");

    // Soft delete del tenant
    await _tenants.SoftDeleteAsync(request.TenantId, ct);

    // Encolar el job de purga (se ejecuta después del período de gracia)
    // Dar 30 días de gracia por si el Admin se arrepiente
    _jobClient.Schedule<TenantDataPurgeJob>(
        job => job.ExecuteAsync(request.TenantId, CancellationToken.None),
        delay: TimeSpan.FromDays(30));

    // Cancelar la suscripción en Stripe
    await _stripeService.CancelSubscriptionAsync(billing?.StripeSubscriptionId, ct);

    // Email de confirmación
    await _email.SendDataDeletionConfirmedAsync(request.AdminEmail, request.TenantSlug, ct);

    // Revocar todas las sesiones activas
    await _sessions.RevokeAllByTenantAsync(request.TenantId, "tenant_deletion", ct);

    return new RequestDataDeletionSuccess(
        "Tu solicitud fue registrada. Los datos serán eliminados permanentemente en 30 días. " +
        "Recibirás una confirmación por email.");
}
```

---

## Job de purga de datos

```csharp
// Compliance.Infrastructure/BackgroundJobs/TenantDataPurgeJob.cs
public sealed class TenantDataPurgeJob
{
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync(long tenantId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Verificar que el tenant está en estado Deleted (no fue restaurado durante el período de gracia)
        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant?.Status != TenantStatus.Deleted)
        {
            _logger.LogInformation(
                "Purga cancelada para TenantId={TenantId} — tenant fue restaurado", tenantId);
            return;
        }

        // Purgar datos en orden (respetar FKs)
        // 1. Datos de negocio (órdenes, facturas, etc.)
        await db.Database.ExecuteSqlAsync(
            $"DELETE FROM dbo.orders WHERE tenant_id = {tenantId}", ct);

        // 2. Archivos de auditoría
        await db.AuditEntries
            .Where(a => a.TenantId == tenantId)
            .ExecuteDeleteAsync(ct);

        // 3. Perfiles de usuario
        await db.UserProfiles
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId)
            .ExecuteDeleteAsync(ct);

        // 4. Credenciales
        await db.Credentials
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId)
            .ExecuteDeleteAsync(ct);

        // 5. Configuraciones del tenant
        await db.TenantSettings
            .Where(s => s.TenantId == tenantId)
            .ExecuteDeleteAsync(ct);

        // 6. Finalmente, el tenant
        await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Id == tenantId)
            .ExecuteDeleteAsync(ct);

        // 7. Eliminar archivos de S3
        await _storage.DeletePrefixAsync($"tenants/{tenantId}/", ct);

        // 8. Registrar la purga en el log del SaaS (para compliance)
        _logger.LogInformation(
            "Datos del tenant {TenantId} purgados permanentemente a las {Time}",
            tenantId, DateTime.UtcNow);
    }
}
```

---

## Retención mínima obligatoria (excepciones al olvido)

Algunos datos no pueden eliminarse aunque el tenant lo solicite (obligaciones legales):

```
Datos que NO se eliminan (retención mínima por ley):
├── Facturas y registros fiscales (5-10 años según jurisdicción)
├── Registros de transacciones financieras
└── Logs de acceso para compliance SOC2/ISO27001

Estos datos se anonimizan en lugar de eliminarse:
  { "tenant_id": "DELETED-123", "email": "ANONYMIZED", ... }
```

```csharp
// Anonimizar en lugar de eliminar los datos financieros
await db.TenantBillings
    .IgnoreQueryFilters()
    .Where(b => b.TenantId == tenantId)
    .ExecuteUpdateAsync(s => s
        .SetProperty(b => b.TenantId, 0)   // desligar del tenant
        .SetProperty(b => b.Notes, $"DELETED TENANT {tenantId}"),
    ct);
```

---

## Endpoints de compliance

```csharp
[Route("api/data")]
[Authorize(Roles = "Admin")]
public sealed class DataComplianceController : BaseApiController
{
    [HttpPost("export")]
    public async Task<IActionResult> RequestExport(CancellationToken ct) { ... }

    [HttpGet("export/status")]
    public async Task<IActionResult> GetExportStatus(CancellationToken ct) { ... }

    [HttpGet("export/download")]
    public async Task<IActionResult> GetDownloadUrl(CancellationToken ct) { ... }

    [HttpPost("delete")]
    public async Task<IActionResult> RequestDeletion(
        [FromBody] RequestDeletionBody body, CancellationToken ct)
    {
        // body.ConfirmationPhrase = "ELIMINAR alfacorp"
    }
}
```

---

## Checklist

- [ ] Exportación en background job (datos grandes no caben en un HTTP response)
- [ ] ZIP con JSONs por tipo de entidad + README explicativo
- [ ] Passwords y tokens NUNCA incluidos en la exportación
- [ ] Presigned URL de descarga con expiración (7 días)
- [ ] Período de gracia de 30 días antes de la purga permanente
- [ ] Confirmación explícita requerida para eliminar (`ELIMINAR {slug}`)
- [ ] Datos fiscales/financieros anonimizados, no eliminados (obligación legal)
- [ ] Job de purga registra la eliminación en el log del SaaS para compliance
- [ ] Cancelar suscripción de Stripe al solicitar eliminación
- [ ] Revocar todas las sesiones activas al iniciar la eliminación

---

## Glosario

| Término | Definición |
|---------|-----------|
| GDPR | Reglamento General de Protección de Datos de la UE — define derechos de exportación y eliminación de datos personales |
| DataExportRequest | Entidad que registra una solicitud de exportación de datos del tenant con su estado y URL de descarga |
| TenantDataPurgeJob | Job que elimina o anonimiza todos los datos de un tenant tras el período de gracia de eliminación |
| Derecho al Olvido | Derecho GDPR que permite a un usuario solicitar la eliminación de sus datos personales |
| Exportación ZIP | Archivo comprimido con todos los datos del tenant en formato JSON — descargable por el Admin |
| Presigned URL | URL temporal de S3/Azure Blob con firma de acceso — válida por tiempo limitado para descargar el export |
| Período de Gracia | Tiempo entre la solicitud de eliminación y la purga permanente — permite cancelar el proceso |
| Anonimización | Reemplazo de datos personales por valores genéricos — alternativa a la eliminación para cumplir obligaciones legales |
| ExportStatus | Estado de la exportación: Pending, Processing, Ready, Expired — controla el flujo del job |
| Datos Fiscales | Registros financieros (facturas, pagos) que por ley no pueden eliminarse aunque se solicite el olvido |
| Right to Portability | Derecho GDPR de recibir los propios datos en formato estándar (JSON, CSV) para transferirlos |

---

*Rogelio Arriaga Gonzalez*
