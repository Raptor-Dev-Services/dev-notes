# 29 — Audit Trail: Bitácora de Cambios

Un audit trail (bitácora de auditoría) registra quién hizo qué, cuándo, en qué entidad, desde qué tenant y branch. Es indispensable para compliance, diagnóstico de incidentes y trazabilidad de cambios en producción.

---

## Qué registrar

Cada evento de auditoría debe responder:

```
¿Quién?    → UserPublicId + email
¿Qué?      → Action (Created, Updated, Deleted, LoggedIn, RoleChanged, ...)
¿Sobre qué? → EntityType + EntityPublicId
¿Cuándo?   → OccurredAtUtc
¿Dónde?    → TenantId + BranchId (si aplica)
¿Desde dónde? → IpAddress + UserAgent
¿Qué cambió? → OldValues + NewValues (JSON)
```

---

## Entidad AuditEntry

```csharp
// Shared/Audit/AuditEntry.cs  (o {Modulo}.Domain si es por módulo)
public sealed class AuditEntry
{
    public long     Id              { get; init; }
    public Guid     PublicId        { get; init; }
    public long     TenantId        { get; init; }
    public long?    BranchId        { get; init; }   // null si el actor no tiene branch
    public Guid?    UserPublicId    { get; init; }   // null en eventos del sistema
    public string?  UserEmail       { get; init; }
    public string   Action          { get; init; } = string.Empty;
    public string   EntityType      { get; init; } = string.Empty;
    public string?  EntityPublicId  { get; init; }
    public string?  OldValues       { get; init; }   // JSON serializado
    public string?  NewValues       { get; init; }   // JSON serializado
    public string?  IpAddress       { get; init; }
    public string?  UserAgent       { get; init; }
    public string?  Notes           { get; init; }   // contexto extra
    public DateTime OccurredAtUtc   { get; init; }
}

public static class AuditActions
{
    public const string Created       = "Created";
    public const string Updated       = "Updated";
    public const string Deleted       = "Deleted";
    public const string SoftDeleted   = "SoftDeleted";
    public const string Restored      = "Restored";
    public const string LoggedIn      = "LoggedIn";
    public const string LoggedOut     = "LoggedOut";
    public const string LoginFailed   = "LoginFailed";
    public const string RoleChanged   = "RoleChanged";
    public const string PasswordChanged = "PasswordChanged";
    public const string Invited       = "Invited";
    public const string InviteAccepted = "InviteAccepted";
    public const string TenantSuspended = "TenantSuspended";
    public const string TenantActivated = "TenantActivated";
    public const string ExportedData  = "ExportedData";
}
```

---

## Estrategia A — EF Core SaveChanges Interceptor (automático)

Interceptar el `SaveChanges` del `AppDbContext` y registrar cambios automáticamente para todas las entidades auditables.

```csharp
// Shared/Database/Interceptors/AuditInterceptor.cs
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContextAccessor _tenantAccessor;
    private readonly IAuditContextAccessor  _auditAccessor;   // lleva UserPublicId + IP

    public AuditInterceptor(
        ITenantContextAccessor tenantAccessor,
        IAuditContextAccessor auditAccessor)
    {
        _tenantAccessor = tenantAccessor;
        _auditAccessor  = auditAccessor;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is null) return await base.SavingChangesAsync(eventData, result, ct);

        var entries = eventData.Context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable &&
                        e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var action = entry.State switch
            {
                EntityState.Added    => AuditActions.Created,
                EntityState.Modified => AuditActions.Updated,
                EntityState.Deleted  => AuditActions.Deleted,
                _                    => null
            };
            if (action is null) continue;

            var auditEntry = new AuditEntry
            {
                TenantId      = CurrentTenantId(),
                BranchId      = CurrentBranchId(),
                UserPublicId  = _auditAccessor.Current?.UserPublicId,
                UserEmail     = _auditAccessor.Current?.UserEmail,
                Action        = action,
                EntityType    = entry.Entity.GetType().Name,
                EntityPublicId = (entry.Entity as IHasPublicId)?.PublicId.ToString(),
                OldValues     = action != AuditActions.Created
                    ? JsonSerializer.Serialize(entry.OriginalValues.ToObject()) : null,
                NewValues     = action != AuditActions.Deleted
                    ? JsonSerializer.Serialize(entry.CurrentValues.ToObject()) : null,
                IpAddress     = _auditAccessor.Current?.IpAddress,
                OccurredAtUtc = DateTime.UtcNow
            };

            eventData.Context.Set<AuditEntry>().Add(auditEntry);
        }

        return await base.SavingChangesAsync(eventData, result, ct);
    }

    private long CurrentTenantId() =>
        long.TryParse(_tenantAccessor.Current?.TenantId, out var id) ? id : 0;
    private long? CurrentBranchId() =>
        long.TryParse(_tenantAccessor.Current?.BranchId, out var id) ? id : null;
}

// Interfaz para marcar entidades auditables
public interface IAuditable { }
public interface IHasPublicId { Guid PublicId { get; } }
```

Registrar el interceptor:

```csharp
// Shared/Database/ServiceCollectionEx.cs
services.AddSingleton<AuditInterceptor>();

services.AddDbContext<AppDbContext>((sp, options) =>
    options
        .UseNpgsql(configuration.GetConnectionString("MainDbConnection"))
        .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
```

---

## Estrategia B — Registro manual en el handler (explícito)

Para eventos de negocio que no corresponden a cambios de entidad (login, export, role change), registrar manualmente:

```csharp
// IAuditService.cs
public interface IAuditService
{
    Task RecordAsync(
        long    tenantId,
        long?   branchId,
        Guid?   userPublicId,
        string? userEmail,
        string  action,
        string  entityType,
        string? entityPublicId = null,
        object? oldValues      = null,
        object? newValues      = null,
        string? ipAddress      = null,
        string? notes          = null,
        CancellationToken ct   = default);
}

// AuditService.cs
public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    public async Task RecordAsync(
        long tenantId, long? branchId, Guid? userPublicId, string? userEmail,
        string action, string entityType, string? entityPublicId = null,
        object? oldValues = null, object? newValues = null,
        string? ipAddress = null, string? notes = null,
        CancellationToken ct = default)
    {
        _db.AuditEntries.Add(new AuditEntry
        {
            TenantId      = tenantId,
            BranchId      = branchId,
            UserPublicId  = userPublicId,
            UserEmail     = userEmail,
            Action        = action,
            EntityType    = entityType,
            EntityPublicId = entityPublicId,
            OldValues     = oldValues is not null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues     = newValues is not null ? JsonSerializer.Serialize(newValues) : null,
            IpAddress     = ipAddress,
            Notes         = notes,
            OccurredAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
```

```csharp
// LoginHandler.cs — registro manual de login
public async Task<LoginResponse> Handle(LoginRequest request, CancellationToken ct)
{
    var credential = await _credentials.GetForLoginAsync(request.Email, ct);
    if (credential is null || !BCrypt.Net.BCrypt.Verify(request.Password, credential.PasswordHash))
    {
        await _audit.RecordAsync(
            tenantId:    0,
            branchId:    null,
            userPublicId: null,
            userEmail:   request.Email,
            action:      AuditActions.LoginFailed,
            entityType:  nameof(UserCredential),
            ipAddress:   request.IpAddress,
            notes:       "Email no encontrado o password incorrecto",
            ct:          ct);
        return new LoginInvalidCredentialsFailure("Credenciales inválidas.");
    }

    // ... generar tokens ...

    await _audit.RecordAsync(
        tenantId:    credential.TenantId,
        branchId:    credential.BranchId,
        userPublicId: credential.PublicId,
        userEmail:   credential.Email,
        action:      AuditActions.LoggedIn,
        entityType:  nameof(UserCredential),
        ipAddress:   request.IpAddress,
        ct:          ct);

    return new LoginSuccess(tokens);
}
```

---

## IAuditContextAccessor — quién hace la acción

Similar al `ITenantContextAccessor`, para llevar el contexto del usuario actual al interceptor:

```csharp
public sealed record AuditContext(
    Guid?   UserPublicId,
    string? UserEmail,
    string? IpAddress,
    string? UserAgent);

public interface IAuditContextAccessor
{
    AuditContext? Current { get; set; }
}

public sealed class AuditContextAccessor : IAuditContextAccessor
{
    private static readonly AsyncLocal<AuditContext?> _current = new();
    public AuditContext? Current { get => _current.Value; set => _current.Value = value; }
}
```

```csharp
// Host.Api/Middleware/AuditContextMiddleware.cs
public async Task InvokeAsync(HttpContext context)
{
    var sub       = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    var email     = context.User.FindFirstValue("email");
    var ip        = context.Connection.RemoteIpAddress?.ToString();
    var userAgent = context.Request.Headers.UserAgent.ToString();

    if (sub is not null && Guid.TryParse(sub, out var publicId))
        _accessor.Current = new AuditContext(publicId, email, ip, userAgent);

    await _next(context);
}
```

---

## Tabla y configuración EF Core

```csharp
// Shared/Database/EntityTypeConfigurations/AuditEntryConfiguration.cs
public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> b)
    {
        b.ToTable("audit_entries");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).UseIdentityByDefaultColumn();
        b.Property(e => e.PublicId).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Action).HasMaxLength(50).IsRequired();
        b.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
        b.Property(e => e.EntityPublicId).HasMaxLength(36);
        b.Property(e => e.UserEmail).HasMaxLength(320);
        b.Property(e => e.IpAddress).HasMaxLength(45);   // IPv6 max
        b.Property(e => e.OccurredAtUtc)
            .HasColumnType("timestamp(0)")
            .HasDefaultValueSql("timezone('utc', now())");

        // Columnas de texto largo para JSON
        b.Property(e => e.OldValues).HasColumnType("text");
        b.Property(e => e.NewValues).HasColumnType("text");

        // Índices para búsqueda
        b.HasIndex(e => new { e.TenantId, e.OccurredAtUtc });
        b.HasIndex(e => new { e.TenantId, e.BranchId, e.OccurredAtUtc });
        b.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityPublicId });
        b.HasIndex(e => new { e.TenantId, e.UserPublicId });
    }
}
```

La tabla `audit_entries` **no lleva Global Query Filter** de tenant — el repositorio filtra manualmente, y el Super Admin puede consultarla sin restricciones.

---

## Consulta del audit trail

```csharp
// Audit.Application/UseCases/GetAuditEntries/GetAuditEntriesRequest.cs
public sealed record GetAuditEntriesRequest(
    long    TenantId,
    long?   BranchId       = null,
    Guid?   UserPublicId   = null,
    string? EntityType     = null,
    string? EntityPublicId = null,
    string? Action         = null,
    DateTime? From         = null,
    DateTime? To           = null,
    int     Page           = 1,
    int     PageSize       = 50)
    : IRequest<GetAuditEntriesResponse>;
```

```csharp
// Audit.Infrastructure/Repositories/AuditEntryRepository.cs
public async Task<(List<AuditEntry> Items, int Total)> GetAsync(
    GetAuditEntriesRequest q, CancellationToken ct)
{
    var query = _db.AuditEntries
        .AsNoTracking()
        .Where(e => e.TenantId == q.TenantId);

    if (q.BranchId.HasValue)      query = query.Where(e => e.BranchId == q.BranchId);
    if (q.UserPublicId.HasValue)  query = query.Where(e => e.UserPublicId == q.UserPublicId);
    if (q.EntityType is not null) query = query.Where(e => e.EntityType == q.EntityType);
    if (q.Action is not null)     query = query.Where(e => e.Action == q.Action);
    if (q.From.HasValue)          query = query.Where(e => e.OccurredAtUtc >= q.From);
    if (q.To.HasValue)            query = query.Where(e => e.OccurredAtUtc <= q.To);

    var total = await query.CountAsync(ct);
    var items = await query
        .OrderByDescending(e => e.OccurredAtUtc)
        .Skip((q.Page - 1) * q.PageSize)
        .Take(q.PageSize)
        .ToListAsync(ct);

    return (items, total);
}
```

---

## Retención de datos y archivado

Los registros de auditoría crecen indefinidamente. Estrategia de retención:

```sql
-- Archivar registros de más de 90 días a tabla histórica
INSERT INTO dbo.audit_entries_archive
SELECT * FROM dbo.audit_entries
WHERE occurred_at_utc < NOW() - INTERVAL '90 days';

DELETE FROM dbo.audit_entries
WHERE occurred_at_utc < NOW() - INTERVAL '90 days';
```

```csharp
// Background job de archivado mensual
public sealed class AuditArchiveJob : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Ejecutar el primer día de cada mes
        while (!ct.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = new DateTime(now.Year, now.Month, 1)
                .AddMonths(1)
                .AddHours(2);  // 2 AM UTC
            await Task.Delay(nextRun - now, ct);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO dbo.audit_entries_archive SELECT * FROM dbo.audit_entries WHERE occurred_at_utc < NOW() - INTERVAL '90 days'",
                ct);
            await db.Database.ExecuteSqlRawAsync(
                "DELETE FROM dbo.audit_entries WHERE occurred_at_utc < NOW() - INTERVAL '90 days'",
                ct);
        }
    }
}
```

---

## Sensibilidad de datos en OldValues/NewValues

No registrar passwords, tokens ni datos sensibles en los valores del audit:

```csharp
// En el interceptor o en el handler — limpiar antes de serializar
private static object SanitizeForAudit(object entity) => entity switch
{
    UserCredential c => new { c.Email, c.Role, c.TenantId, c.IsActive },
    RefreshToken   _ => new { Redacted = true },
    _                => entity
};
```

---

## Relación con back-template (tenant + branch)

En sistemas con tenant+branch, el audit trail registra ambos:

```
TenantId=1, BranchId=10, User=Ana, Action=Created, EntityType=ProductionOrder, EntityPublicId=uuid
TenantId=1, BranchId=null, User=Director, Action=ExportedData, EntityType=Report, Notes=Q4-2025
```

El Admin corporativo (sin branch_id) puede ver el audit de todos sus branches. El Admin de branch solo ve los de su branch.

Ver `04-backend/24-multi-tenancy-logic.md` para el contexto de tenant en requests.
Ver `04-backend/25-tenant-branch-logic.md` para la jerarquía de dos niveles.

---

*Rogelio Arriaga Gonzalez*
