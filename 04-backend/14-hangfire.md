# 14 — Background Jobs y Trabajos Programados

Tareas que corren fuera del request HTTP: envío de emails, generación de reportes, sincronizaciones, procesamiento batch. Elegir la herramienta correcta depende de si los jobs necesitan persistencia y visibilidad.

> Fuente: *Apps and Services with .NET 8* (Mark J. Price) — Ch.12 Scheduling Jobs with Background Services

---

## Opciones disponibles

| Herramienta | Persistencia | UI | Uso típico |
|-------------|-------------|-----|-----------|
| `BackgroundService` (nativo) | No | No | Polling simple, procesamiento continuo |
| Hangfire | Sí (DB) | Sí | Jobs en producción, reintentos, dashboard |
| Quartz.NET | Sí (DB) | Opcional | Jobs complejos con triggers y calendarios |
| Azure Functions / AWS Lambda | Sí (cloud) | Cloud console | Serverless, sin gestionar infraestructura |

---

## BackgroundService — el más simple (nativo)

```csharp
public class EmailSenderService : BackgroundService
{
    private readonly ILogger<EmailSenderService> _logger;
    private readonly IServiceProvider _services;

    public EmailSenderService(ILogger<EmailSenderService> logger, IServiceProvider services)
    {
        _logger   = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope  = _services.CreateScope();
                var queue        = scope.ServiceProvider.GetRequiredService<IEmailQueue>();
                var pendingEmails = await queue.DequeuePendingAsync(stoppingToken);
                foreach (var email in pendingEmails)
                    await SendAsync(email, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando cola de emails");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

builder.Services.AddHostedService<EmailSenderService>();
```

**Limitaciones:** sin persistencia — si el proceso cae, el job se pierde. Sin UI de monitoreo.

---

## Hangfire — jobs persistentes con dashboard

```csharp
// dotnet add package Hangfire.AspNetCore
// dotnet add package Hangfire.PostgreSql  (o SqlServer, MySql, InMemory)

builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default"))));

builder.Services.AddHangfireServer();

// Dashboard (proteger con autenticación en producción)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AdminOnlyAuthorization() }
});

// Tipos de jobs:
// Fire-and-forget — ejecutar una vez lo antes posible
BackgroundJob.Enqueue<IEmailService>(svc => svc.SendWelcomeAsync(userId));

// Delayed — ejecutar después de un delay
BackgroundJob.Schedule<IEmailService>(
    svc => svc.SendReminderAsync(userId),
    TimeSpan.FromHours(24));

// Recurring — cron job
RecurringJob.AddOrUpdate<IBillingService>(
    "daily-invoice-generation",
    svc => svc.GenerateDailyInvoicesAsync(),
    Cron.Daily(2));   // 2am diario

// Continuations — encadenar jobs
var firstJobId = BackgroundJob.Enqueue<IReportService>(svc => svc.GenerateAsync(reportId));
BackgroundJob.ContinueJobWith<IEmailService>(firstJobId, svc => svc.SendReportAsync(reportId));
```

---

## Regla crítica para jobs en SaaS multi-tenant

Cada job **debe** recibir el `TenantId` como parámetro explícito. Los jobs corren en otro thread/proceso — el contexto HTTP del tenant original ya no existe.

```csharp
// ❌ El job no sabe a qué tenant pertenece
public async Task SendWelcomeEmail(Guid userId)
{
    var user = await _db.Users.FindAsync(userId);  // ← ¿qué tenant filtra?
}

// ✓ El TenantId es explícito — el job recrea el contexto
public async Task SendWelcomeEmail(Guid tenantId, Guid userId)
{
    _tenantContext.SetTenant(tenantId);             // establece el filtro de tenant
    var user = await _db.Users.FindAsync(userId);  // ahora sí filtra por tenant
    // ...
}

// Al encolar el job, pasar el TenantId del contexto actual
var tenantId = _currentTenant.TenantId;
BackgroundJob.Enqueue<IEmailService>(svc => svc.SendWelcomeEmail(tenantId, userId));
```

Ver `04-backend/41-background-jobs-multitenant.md` para la implementación completa con filtros de tenant en Hangfire.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Hangfire | Librería de .NET para jobs en segundo plano con persistencia en base de datos y dashboard web |
| Fire-and-forget | Tipo de job de Hangfire que se ejecuta una sola vez lo antes posible tras ser encolado |
| Delayed Job | Tipo de job de Hangfire que se ejecuta después de un tiempo de espera configurado |
| Recurring Job | Job periódico de Hangfire definido con expresión cron — equivalente a un cron job del sistema |
| Continuation | Job de Hangfire que se ejecuta al completarse otro job previo — permite encadenar tareas |
| BackgroundJob.Enqueue | Método estático de Hangfire para encolar un fire-and-forget job |
| RecurringJob.AddOrUpdate | Método para registrar o actualizar un job recurrente identificado por nombre |
| Cron | Formato de expresión temporal para definir la frecuencia de los recurring jobs |
| Dashboard | Interfaz web de Hangfire para monitorear, reintentar y cancelar jobs en tiempo real |
| TenantId en jobs | Parámetro explícito requerido en cada job de SaaS para recrear el contexto del tenant |
| AutomaticRetry | Atributo de Hangfire que configura el número máximo de reintentos ante fallos del job |
| Job Storage | Backend donde Hangfire persiste el estado de los jobs — PostgreSQL, SQL Server, Redis |

---

*Rogelio Arriaga Gonzalez*
