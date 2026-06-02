# 18 — La Librería Common: Bootstrap Completo de un SaaS

Toda la teoría del manual converge en una librería real. `Common` es la base de código que cada SaaS arranca con los problemas transversales ya resueltos. Construida en .NET 10, resuelve de un golpe los temas más dolorosos del bootstrap: logging estructurado con Seq, observabilidad con OpenTelemetry, mediator propio, multi-tenancy con resolución por header y subdominio, factories de conexión a Postgres, propagación automática de tenant en HTTP clients, y health checks listos.

> **Repositorio:** `github.com/Raptor-Dev-Services/Common` — Librería base reutilizable para Web APIs en .NET 10.

> Fuente: *Architecting ASP.NET Core Applications* (Carl-Hugo Marcotte) — Ch.16 Building a Shared Kernel

---

## Qué resuelve Common

Antes de Common, cada proyecto nuevo repetía las mismas cien líneas de plumbing: configurar Serilog, OTel, un middleware de tenant, una factory de conexión, Result tipado, contratos de mediator. Cada implementación era ligeramente distinta y cada bug se reproducía en cada proyecto.

Common centraliza esa infraestructura:
- Bootstrap de un SaaS profesional en ~40 líneas de `Program.cs`
- Comportamiento consistente en logs, traces y métricas a través de todos los servicios
- Multi-tenancy correctamente implementado desde el día uno
- Tenant propagado automáticamente en HTTP clients salientes
- Health checks integrados para Postgres y Redis sin código por proyecto
- Mediator propio sin dependencia de MediatR (cuya licencia comercial cambió en 2024)

---

## Mapa de módulos

| Módulo | Namespace | Responsabilidad |
|--------|-----------|----------------|
| **Logging** | `Common.Logging` | Serilog con sinks Console/Seq y enriquecimiento automático con TenantId |
| **Observability** | `Common.Observability` | OpenTelemetry: traces y metrics por OTLP, exporter Prometheus |
| **Results** | `Common.Results` | `Result<T>` tipado — éxito/falla sin excepciones de control |
| **Errors** | `Common.Errors` | `ErrorList` — acumula errores de validación o dominio |
| **Exceptions** | `Common.Exceptions` | Excepciones de dominio reutilizables |
| **Messaging** | `Common.Messaging` | Contratos `IRequest`, `IMediator` y pipeline behaviors |
| **Abstractions** | `Common.Abstractions` | Interfaces base de interactores y presenters |
| **ViewModels** | `Common.ViewModels` | ViewModels genéricos reutilizables en respuestas |
| **MultiTenancy** | `Common.MultiTenancy` | Resolución de tenant, contexto actual, config por tenant |
| **Data** | `Common.Data` | Abstracciones de conexiones DB |
| **PostgreSql** | `Common.PostgreSql` | Factorías Npgsql, health checks, migraciones por scripts |

---

## Dependencias clave

| Paquete | Para qué se usa |
|---------|----------------|
| `Serilog 4.3.0` | Logger estructurado con sinks |
| `AspNetCore.HealthChecks.NpgSql 9.0.0` | Health checks para PostgreSQL |
| `AspNetCore.HealthChecks.Redis 9.0.0` | Health checks para Redis |
| `OpenTelemetry.Extensions.Hosting` | Bootstrap OTel en host .NET |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | Exportación OTLP a Tempo/Jaeger/Datadog |
| `OpenTelemetry.Exporter.Prometheus.AspNetCore` | Métricas para scraping de Prometheus |
| `OpenTelemetry.Instrumentation.AspNetCore` | Instrumentación automática de requests |
| `OpenTelemetry.Instrumentation.Http` | Instrumentación de HttpClient |
| `OpenTelemetry.Instrumentation.Runtime` | Métricas de runtime: GC, CPU, threads |
| `Microsoft.Extensions.Http.Resilience 10.2.0` | Resiliencia HTTP con Polly integrado |

---

## Cómo consumirla en un proyecto nuevo

**Paso 1 — Referenciar el proyecto:**

```xml
<!-- En el .csproj del Web API que consume Common -->
<ItemGroup>
  <ProjectReference Include="..\Common\Common.csproj" />
</ItemGroup>
```

**Paso 2 — Registrar servicios principales:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Logging estructurado con Serilog + Seq
builder.Services.AddLoggingServices(builder.Configuration);

// OpenTelemetry: traces, métricas, exporter OTLP + Prometheus
builder.Services.AddObservability(builder.Configuration);

// Multi-tenancy: resolver por header y subdominio, contexto actual, config por tenant
builder.Services.AddMultiTenancy(builder.Configuration);

// HttpClient resiliente con propagación automática del TenantId actual
builder.Services
    .AddHttpClient("core")
    .AddCoreResilience()
    .AddTenantPropagation();
```

**Paso 3 — Registrar middlewares base:**

```csharp
var app = builder.Build();

// Resuelve el tenant del request entrante (header X-Tenant-Id o subdominio)
app.UseTenantResolution();

// Asigna o propaga un correlation id para trazas y logs
app.UseCorrelationId();

// Manejo estandarizado de errores con Problem Details (RFC 7807)
app.UseCoreProblemDetails();

app.MapControllers();
app.Run();
```

---

## Configuración esperada en appsettings.json

```json
{
  "CustomLogging": {
    "Project": "GTM-Suite",
    "SeqUri": "http://localhost:5341",
    "LogEventLevel": "Information",
    "Application": "MiWebApi",
    "Version": "1.0.0"
  },
  "Observability": {
    "ServiceName": "MiWebApi",
    "ServiceVersion": "1.0.0",
    "OtlpEndpoint": "http://localhost:4317"
  },
  "MultiTenancy": {
    "RequireTenant": true,
    "RejectUnknownTenants": true,
    "TenantHeaderName": "X-Tenant-Id",
    "ResolveFromHeader": true,
    "ResolveFromSubdomain": true,
    "DefaultTenantId": "default",
    "Tenants": {
      "tenant-a": {
        "IsEnabled": true,
        "ConnectionStrings": {
          "Default": "Server=...;Database=TenantA;..."
        },
        "Settings": { "Region": "MX" }
      },
      "tenant-b": {
        "IsEnabled": true,
        "ConnectionStrings": {
          "Default": "Server=...;Database=TenantB;..."
        },
        "Settings": { "Region": "US" }
      }
    }
  }
}
```

### Flags de MultiTenancy

| Flag | Comportamiento |
|------|---------------|
| `RequireTenant` | Si `true`, requests sin tenant → 400 |
| `RejectUnknownTenants` | Si `true`, tenants no registrados en el catálogo → 400 |
| `TenantHeaderName` | Nombre del header HTTP. Convención: `X-Tenant-Id` |
| `ResolveFromHeader` | Activa resolución por header HTTP |
| `ResolveFromSubdomain` | Activa resolución por subdominio (`acme.miapp.com` → `acme`) |
| `DefaultTenantId` | Fallback para requests sin tenant (ej. `/health`) |
| `Tenants` | Catálogo de tenants con connection strings y settings propios |

---

## Conexión a base de datos por tenant

Common ofrece una jerarquía de factories. El patrón usa una clase vacía como marcador de tipo — la factory genérica usa `typeof` para resolver el nombre de la connection string:

```csharp
// 1. Clase marcadora — solo identifica el tipo, sin lógica
public sealed class MainDbConnection { }

// 2. Factory que hereda de la genérica de Common
//    El nombre de la cadena en appsettings es "MainDbConnection"
public sealed class ConfigurationMainDbConnectionFactory
    : ConfigurationNpgsqlConnectionFactory<MainDbConnection>
{
    public ConfigurationMainDbConnectionFactory(IConfiguration configuration)
        : base(configuration) { }
}

// 3. Registro en Program.cs
builder.Services.AddSingleton<ConfigurationMainDbConnectionFactory>();

// 4. Uso desde un repositorio
public class CustomerRepository(ConfigurationMainDbConnectionFactory factory)
{
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await factory.CreateOpenConnectionAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<Customer>(
            "SELECT * FROM customers WHERE id = @id", new { id });
    }
}
```

| Tipo | Para qué sirve |
|------|---------------|
| `DbConnectionFactory` | Conexión por cadena fija, sin tenancy |
| `ConfigurationDbConnectionFactory<T>` | Resuelve `ConnectionStrings:{typeof(T).Name}` |
| `TenantDbConnectionFactory` | Conexión por tenant explícito (útil para jobs) |
| `CurrentTenantDbConnectionFactory` | Usa el tenant actual del request automáticamente |
| `IDapperSqlDbConnection` | Wrapper Dapper con logging y medición de tiempo |

---

## Ejecución de jobs con contexto de tenant

Los jobs en background son donde se pierde el contexto de tenant — ya no hay request HTTP. Common provee `ITenantExecutionContextRunner`:

```csharp
public class DailyReportsJob(
    ITenantExecutionContextRunner runner,
    IReportService reportService)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var activeTenants = new[] { "tenant-a", "tenant-b" };

        foreach (var tenantId in activeTenants)
        {
            await runner.RunAsync(tenantId, async innerCt =>
            {
                // Dentro del RunAsync el tenant está disponible en:
                // - contexto (ITenantContext devuelve este tenant)
                // - logs (Serilog enriquece con TenantId)
                // - traces (Activity.Current.tenant.id)
                // - HTTP clients salientes (header X-Tenant-Id)
                await reportService.GenerateDailyReportAsync(innerCt);
            }, ct);
        }
    }
}
```

> **Regla crítica:** Si tu código corre fuera de un request HTTP, debe entrar en un `RunAsync` explícito. Sin eso, no tiene tenant y todo el logging, tracing y multi-tenancy queda roto.

---

## Propagación de tenant en HTTP clients

```csharp
// Cualquier HttpClient registrado con AddTenantPropagation() agrega
// automáticamente X-Tenant-Id en cada request saliente.
builder.Services
    .AddHttpClient("billing", c => c.BaseAddress = new Uri("https://billing.internal/"))
    .AddCoreResilience()      // retry + circuit breaker estándar
    .AddTenantPropagation();  // ← inyecta header de tenant automáticamente

// Resultado: cuando llamas
//   var client = httpClientFactory.CreateClient("billing");
//   await client.GetAsync("/invoices");
// la petición sale con X-Tenant-Id: <tenant-actual> sin escribir nada extra.
```

---

## Logging con TenantId automático

El middleware de tenancy enriquece cada scope de log. CADA log emitido durante un request lleva el TenantId implícitamente:

```csharp
// Sin escribir nada extra:
_logger.LogInformation("Customer {CustomerId} created", customer.Id);

// Lo que llega a Seq:
// {
//   "@t": "2026-05-08T14:23:00Z",
//   "@l": "Information",
//   "@m": "Customer abc-123 created",
//   "CustomerId": "abc-123",
//   "TenantId": "acme",          ← inyectado automáticamente
//   "CorrelationId": "req-xyz",  ← inyectado automáticamente
//   "Application": "MiWebApi",
//   "Environment": "Production"
// }

// En Seq — todos los errores del tenant 'acme' en la última hora:
// TenantId = 'acme' and @Level = 'Error' and @t > Now() - 1h
```

---

## Health checks integrados

```csharp
builder.Services
    .AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("Default"),
        name: "postgres",
        tags: new[] { "db", "ready" })
    .AddRedis(
        redisConnectionString: builder.Configuration.GetConnectionString("Redis"),
        name: "redis",
        tags: new[] { "cache", "ready" });

// Dos endpoints — live (¿responde?) y ready (¿sus dependencias responden?)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false   // solo verifica que la app responde
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = h => h.Tags.Contains("ready")   // verifica BD, Redis, etc.
});
```

---

## Mediator propio (Common.Messaging)

Common define contratos puros sin depender de MediatR (licencia comercial desde 2024):

```csharp
// Definir un caso de uso
public sealed record CreateCustomerRequest(string Name, string Email)
    : IRequest<Result<CustomerDto>>;

public sealed class CreateCustomerHandler
    : IRequestHandler<CreateCustomerRequest, Result<CustomerDto>>
{
    private readonly ICustomerRepository _repo;
    public CreateCustomerHandler(ICustomerRepository repo) => _repo = repo;

    public async Task<Result<CustomerDto>> HandleAsync(
        CreateCustomerRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Result<CustomerDto>.Failure("Email es requerido");

        var customer = new Customer(request.Name, request.Email);
        await _repo.AddAsync(customer, ct);
        return Result<CustomerDto>.Success(customer.ToDto());
    }
}

// Controller delgado — sin lógica
[HttpPost]
public async Task<IActionResult> Create(
    CreateCustomerRequest request,
    [FromServices] IMediator mediator,
    CancellationToken ct)
{
    var result = await mediator.SendAsync(request, ct);
    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
}
```

---

## Result Pattern (Common.Results)

```csharp
public Result<Customer> ActivateCustomer(Guid customerId)
{
    var customer = _repo.GetById(customerId);
    if (customer == null)
        return Result<Customer>.NotFound("Cliente no existe");

    if (customer.IsActive)
        return Result<Customer>.Conflict("Cliente ya está activo");

    customer.Activate();
    _repo.Update(customer);
    return Result<Customer>.Success(customer);
}

// Consumo
var result = service.ActivateCustomer(id);
if (result.IsSuccess) { /* usar result.Value */ }
else { /* result.Error tiene tipo y mensaje */ }
```

---

## Troubleshooting

| Síntoma | Causa probable | Solución |
|---------|---------------|---------|
| `AddLoggingServices` no existe | Missing NuGet | Verificar `PackageReference` en `Common.csproj` |
| No llegan traces a Tempo | Endpoint OTLP incorrecto | Verificar `Observability:OtlpEndpoint` y conectividad |
| Logs sin TenantId | `UseTenantResolution()` no registrado | Agregar ANTES de `MapControllers()` |
| HTTP salientes sin tenant | Falta `AddTenantPropagation()` | Agregar al registrar el HttpClient |
| Jobs sin tenant en logs | No usa `ITenantExecutionContextRunner` | Envolver con `runner.RunAsync(tenantId, ...)` |
| `RejectUnknownTenants` rechaza tenants válidos | Tenant no está en el catálogo | Agregar al catálogo en `MultiTenancy:Tenants` |

---

## Bootstrap completo — Program.cs de un nuevo SaaS

```csharp
using Common.Logging;
using Common.Observability;
using Common.MultiTenancy;
using Common.Messaging;

var builder = WebApplication.CreateBuilder(args);

// === Pilares del SaaS desde Common ===
builder.Services.AddLoggingServices(builder.Configuration);
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddMultiTenancy(builder.Configuration);
builder.Services.AddMediator(typeof(Program).Assembly);

// === Servicios propios del proyecto ===
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// === Conexiones a base de datos ===
builder.Services.AddSingleton<ConfigurationMainDbConnectionFactory>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

// === Health checks ===
builder.Services
    .AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default"), tags: new[] { "ready" });

// === HTTP clients salientes ===
builder.Services
    .AddHttpClient("billing", c => c.BaseAddress = new Uri("https://billing.internal/"))
    .AddCoreResilience()
    .AddTenantPropagation();

// === Pipeline ===
var app = builder.Build();
app.UseTenantResolution();   // ← antes de cualquier lógica que dependa de tenant
app.UseCorrelationId();
app.UseCoreProblemDetails();
app.MapControllers();
app.MapHealthChecks("/health/live",  new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = h => h.Tags.Contains("ready") });
app.MapOpenApi();
app.Run();
```

Estas ~40 líneas dan: logging estructurado con tenant y correlation id automáticos; traces y métricas con OpenTelemetry; resolución de tenant por header y subdominio con guardia de unknown; mediator; conexiones a Postgres tipadas; health checks live/ready; HTTP clients resilientes con propagación de tenant. Tres semanas de bootstrap en quince minutos.

---

## Cuándo NO usar Common

Common sirve a equipos que construyen SaaS multi-tenant con .NET, Postgres, Seq y OpenTelemetry. No tiene sentido en:

- **Sistemas mono-tenant** — el overhead de multi-tenancy es ruido innecesario
- **Microservicios mínimos** (tipo Lambda) — la huella de runtime importa más que la productividad
- **Apps con infraestructura propia consolidada** — migrar solo se justifica gradualmente
- **Equipos con stacks diferentes** — MediatR, EF Core con providers exóticos, observabilidad diferente

---

## Glosario

| Término | Definición |
|---------|-----------|
| ITenantContextAccessor | Interfaz del Common que expone TenantId y BranchId del contexto actual de forma tipada |
| AddMultiTenancy | Método de extensión del Common que registra todos los servicios de multi-tenancy en DI |
| AddLoggingServices | Método del Common que configura Serilog con enriquecedores de tenant_id y branch_id |
| AddObservability | Método del Common que configura OpenTelemetry con traces, métricas y logs exportados a Seq/OTLP |
| ITenantExecutionContextRunner | Interfaz del Common para ejecutar código en el contexto de un tenant específico desde jobs |
| IDapperSqlDbConnection | Interfaz del Common que abstrae la conexión Dapper con tenant context integrado |
| AddTenantPropagation | Método del Common que propaga el TenantId a través de HttpClient en requests salientes |
| NuGet package | Artefacto binario publicado en NuGet para reutilizar la librería Common entre proyectos |
| Git Submodule | Forma alternativa de compartir el Common como referencia de código fuente en lugar de NuGet |
| OpenTelemetry | Estándar de observabilidad para trazas, métricas y logs — integrado en el Common del stack |
| Serilog | Librería de logging estructurado para .NET — enriquece logs con tenant_id, request_id y más |
| ServiceCollectionExtensions | Patrón de extensión de IServiceCollection para agrupar registros DI por capa o módulo |

---

*Rogelio Arriaga Gonzalez*
