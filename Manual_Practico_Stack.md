# Manual Práctico del Stack

**v2.0 — Edición ampliada**  
Configuraciones, archivos y rutas del día a día  
\+ Implementación práctica con la librería Common (Raptor-Dev-Services)

> Complemento operativo al Glosario Maestro de SaaS Multi-Tenant

*Variables de entorno · Secretos · Connection strings · Migrations  
CORS · Rate limiting · HTTP Client · Polly  
Background jobs · API versioning · OpenAPI  
JWT · Validación · Pipeline Behaviors · Repository  
Performance · Testing · CI/CD  
React Router · React Hook Form · Axios interceptors · i18n  
★ Librería Common: Logging · OpenTelemetry · Mediator · MultiTenancy*

**Instructor: Ing. Rogelio Arriaga González**  
M.I. en Gestión de Sistemas de TI · UVM  
Mayo 2026

---

## Cómo usar este manual

El Glosario Maestro cubre los conceptos: qué es cada cosa y cuándo se usa. Este manual cubre lo operativo: cómo se configura, cómo se conecta, cómo se despliega, cómo se diagnostica. Es el material que se aprende con dolor en producción y que rara vez aparece en libros.

La versión 2.0 incorpora un capítulo final dedicado a la librería Common de Raptor-Dev-Services — la base de código que cada nuevo SaaS arranca con los problemas transversales ya resueltos: logging, observabilidad, multi-tenancy, mediator, results y conexiones a base de datos por tenant.

El manual está pensado como una referencia consultable. No se lee de corrido: se busca el escenario que estás resolviendo y se aplica. Cada sección incluye archivos completos de ejemplo, comandos exactos y los errores más comunes.

### Áreas operativas cubiertas

| # | Área |
|---|------|
| 1 | Variables de entorno y configuración (Vite, .NET, Docker) |
| 2 | Secretos: dónde viven, cómo rotarlos, cómo no exponerlos |
| 3 | Connection strings y manejo de bases de datos por ambiente |
| 4 | CORS |
| 5 | Rate limiting |
| 6 | HTTP Client Factory + Polly |
| 7 | Background jobs (IHostedService + Hangfire) |
| 8 | API versioning + OpenAPI |
| 9 | Output caching |
| 10 | EF Core en producción (migrations, soft delete, auditing) |
| 11 | JWT — generación, validación y refresh |
| 12 | Validación con FluentValidation |
| 13 | Pipeline Behaviors |
| 14 | Repository + Unit of Work |
| 15 | Performance y memoria |
| 16 | React de producción |
| 17 | Tooling de equipo |
| 18 | Testing (xUnit, integration, TestContainers) |
| 19 | CI/CD con GitHub Actions |
| 20 | Checklists de verificación |
| 21 | Librería Common — bootstrap completo |

---

## 1 · Variables de entorno y configuración

Las variables de entorno son la forma estándar de inyectar configuración sin meterla en el código. Permiten que la misma imagen Docker corra en desarrollo, staging y producción cambiando solo variables. El principio Twelve-Factor App lo dice claro: configuración separada del código.

### 1.1 Variables de entorno en Vite

Vite tiene un sistema simple pero estricto. Solo expone al cliente las variables con prefijo `VITE_`. Todas las demás están disponibles solo en build-time del lado de Node, nunca llegan al navegador.

**Archivos `.env` reconocidos por Vite**

| Archivo | Cuándo se carga |
|---------|----------------|
| `.env` | Siempre. Variables compartidas por todos los modos. |
| `.env.local` | Siempre, salvo en CI. Para overrides personales (NUNCA al repo). |
| `.env.development` | Solo en modo dev (`npm run dev`). |
| `.env.production` | Solo en modo build (`npm run build`). |
| `.env.development.local` | Override local de development. |
| `.env.production.local` | Override local de production. |

Orden de precedencia (de menor a mayor): `.env` → `.env.{mode}` → `.env.local` → `.env.{mode}.local`. Los archivos `.local` **nunca** se comitean.

```ini
# .env (compartido, va al repo)
VITE_APP_NAME=TaskFlow
VITE_DEFAULT_LOCALE=es-MX

# .env.development (va al repo)
VITE_API_BASE_URL=http://localhost:5000/api
VITE_WS_URL=ws://localhost:5000/hubs
VITE_STRIPE_PUBLIC_KEY=pk_test_51AbCd...
VITE_ENABLE_DEVTOOLS=true

# .env.production (va al repo, valores reales no sensibles)
VITE_API_BASE_URL=https://api.taskflow.com/api
VITE_WS_URL=wss://api.taskflow.com/hubs
VITE_ENABLE_DEVTOOLS=false

# .env.local (NUNCA al repo, .gitignore obligatorio)
VITE_API_BASE_URL=http://192.168.1.50:5000/api
```

**Centralizar el acceso — nunca dispersar `import.meta.env`**

```typescript
// src/config/env.ts
export const config = {
  appName: import.meta.env.VITE_APP_NAME ?? 'App',
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL,
  wsUrl: import.meta.env.VITE_WS_URL,
  stripeKey: import.meta.env.VITE_STRIPE_PUBLIC_KEY,
  defaultLocale: import.meta.env.VITE_DEFAULT_LOCALE ?? 'en-US',
  enableDevtools: import.meta.env.VITE_ENABLE_DEVTOOLS === 'true',
  isDev: import.meta.env.DEV,
  isProd: import.meta.env.PROD,
  mode: import.meta.env.MODE,
} as const;

if (!config.apiBaseUrl) {
  throw new Error('VITE_API_BASE_URL es requerido');
}
```

> **Todo lo que pongas con prefijo `VITE_` se incluye en el bundle final.** JAMÁS pongas API keys secretas, contraseñas o connection strings con ese prefijo.

### 1.2 Configuración en .NET / ASP.NET Core

.NET tiene un sistema de configuración por capas que combina múltiples fuentes. La configuración se lee como un árbol; cualquier fuente puede sobreescribir a la anterior.

**Orden de precedencia (de menor a mayor)**

1. `appsettings.json` — la base, va al repo, valores neutros.
2. `appsettings.{Environment}.json` — overrides por ambiente.
3. User Secrets (solo en Development) — secretos locales del dev, NO van al repo.
4. Variables de entorno — sobreescriben todo lo anterior.
5. Argumentos de línea de comandos — la última palabra.

**`IOptions<T>` tipado (patrón recomendado)**

```csharp
// 1. Clase POCO que mapea la sección
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; init; } = 15;
    public int RefreshTokenLifetimeDays { get; init; } = 30;
    public string SigningKey { get; init; } = string.Empty;
}

// 2. Registro en Program.cs con validación al arrancar
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// 3. Inyección donde se necesita
public class TokenService
{
    private readonly JwtOptions _options;
    public TokenService(IOptions<JwtOptions> options) => _options = options.Value;
}
```

**Variables de entorno para sobreescribir configuración**

En .NET, el separador `__` navega secciones del JSON. Es la forma estándar en Linux/Docker (`:` no es válido en nombres de variables en Linux).

```ini
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default="Host=db.prod;Port=5432;..."
Jwt__SigningKey="..."
Stripe__SecretKey="sk_live_..."
Cors__AllowedOrigins__0="https://app.taskflow.com"
Logging__LogLevel__Default="Warning"
```

**User Secrets en desarrollo local**

```bash
cd src/TaskFlow.Api
dotnet user-secrets init
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Jwt:SigningKey" "super-long-random-key-for-dev"
dotnet user-secrets list
dotnet user-secrets remove "Stripe:SecretKey"
# Windows: %APPDATA%\Microsoft\UserSecrets\<id>\secrets.json
# Linux/Mac: ~/.microsoft/usersecrets/<id>/secrets.json
```

### 1.3 Variables en Docker y docker-compose

```yaml
# docker-compose.yml
services:
  api:
    image: registry/taskflow-api:${APP_VERSION:-latest}
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Production}
      ConnectionStrings__Default: ${DB_CONNECTION}
      Jwt__SigningKey: ${JWT_SIGNING_KEY}
      Stripe__SecretKey: ${STRIPE_SECRET_KEY}
    env_file:
      - .env.docker
    ports:
      - "${API_PORT:-8080}:8080"
    depends_on:
      - postgres

  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: ${DB_NAME}
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

---

## 2 · Secretos en producción

En desarrollo local, User Secrets resuelve. En producción, los secretos viven en gestores especializados, **nunca** en archivos de configuración, repositorios, ni variables de entorno expuestas.

### 2.1 Reglas absolutas

- **Cero secretos en el repositorio.** Nunca, bajo ninguna circunstancia.
- **Cero secretos en el Dockerfile.** Una imagen es pública dentro del registry; sus capas son inspeccionables.
- **Cero secretos en logs.** Los strings sensibles se enmascaran al loggear.
- **Rotación periódica.** Cada secreto tiene fecha de caducidad — al menos cada 90 días.
- **Acceso mínimo.** Cada servicio usa un secreto distinto con permisos mínimos. No reutilizar.
- **Auditoría.** Cada acceso a un secreto queda registrado y es auditable.

### 2.2 Gestores de secretos por nube

| Plataforma | Servicio | Cuándo usarlo |
|-----------|---------|--------------|
| Azure | Azure Key Vault | Stack Microsoft. Integración nativa con .NET. |
| AWS | AWS Secrets Manager | Stack AWS. Soporta rotación automática para RDS. |
| AWS | AWS SSM Parameter Store | Alternativa más barata para configuración menos sensible. |
| GCP | Google Secret Manager | Stack Google Cloud. |
| Multi-cloud | HashiCorp Vault | Para ser independiente del cloud provider. |
| Self-hosted | Bitwarden Secrets / Infisical | SaaS pequeños o on-premise. |

### 2.3 Integración Azure Key Vault con .NET

```csharp
// Program.cs
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    var keyVaultUri = builder.Configuration["KeyVault:Uri"]
        ?? throw new InvalidOperationException("KeyVault:Uri no configurado");

    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}
```

### 2.4 Secretos en Dockerfile con BuildKit

```dockerfile
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# El secreto NUNCA queda en una capa de imagen
RUN --mount=type=secret,id=nuget_token \
    dotnet nuget add source "https://nuget.pkg.github.com/org/index.json" \
    --username "ci" \
    --password "$(cat /run/secrets/nuget_token)" \
    --store-password-in-clear-text
```

> Después de cada cambio en logging, revisa Seq buscando: `password`, `secret`, `token`, `apikey`, `pk_test`, `sk_test`, `eyJ` (inicio típico de JWT). Si aparecen, hay un bug que corregir antes de desplegar.

---

## 3 · Connection strings y manejo por ambiente

La cadena de conexión es uno de los secretos más sensibles del sistema. Su manejo merece la misma disciplina que cualquier secreto.

### 3.1 Componentes de una connection string

| Componente | Descripción |
|-----------|-------------|
| `Host` / `Server` | Hostname o IP del servidor de base de datos |
| `Port` | Puerto. 1433 SQL Server, 5432 Postgres, 3306 MySQL |
| `Database` | Nombre de la base de datos |
| `Username` / `User Id` | Usuario de la conexión |
| `Password` | Contraseña del usuario |
| `Maximum Pool Size` | Número máximo de conexiones. Default 100 |
| `Connection Idle Lifetime` | Segundos antes de cerrar conexiones ociosas |
| `Timeout` | Segundos antes de fallar al conectar. Default 15 |
| `Application Name` | Identifica la app en logs del servidor. **Siempre ponerlo.** |
| `Ssl Mode` | Manejo de SSL/TLS |

### 3.2 PostgreSQL (Npgsql)

```
Host=db.taskflow.internal;Port=5432;Database=taskflow_prod;
Username=app_user;Password=***;Maximum Pool Size=100;
Connection Idle Lifetime=300;Application Name=TaskFlow.Api;
Ssl Mode=Require;Trust Server Certificate=true
```

### 3.3 SQL Server

```
Server=db.taskflow.internal,1433;Database=taskflow_prod;
User Id=app_user;Password=***;Max Pool Size=100;
Connection Timeout=15;Application Name=TaskFlow.Api;
TrustServerCertificate=False;Encrypt=True
```

### 3.4 Connection pooling

| Variable | Recomendación |
|---------|--------------|
| `Maximum Pool Size` | 100 por instancia API. Si tienes 4 instancias = 400 conexiones totales a la BD. |
| BD acepta | Postgres default acepta 100. SQL Server acepta más. Ajusta el pool al límite del servidor. |
| Síntoma de pool agotado | Errores "timeout obteniendo conexión". Sube `Maximum Pool Size` o investiga conexiones no liberadas. |
| Conexiones zombie | Si tu app no usa `using`/`await using` para cerrar, las conexiones se quedan tomadas. Bug clásico. |

### 3.5 Dapper con PostgreSQL

```csharp
// Patrón base con logging de tiempo
public abstract class DapperRepository
{
    private readonly string _connectionString;
    private readonly ILogger _logger;

    protected DapperRepository(string connectionString, ILogger logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    protected async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? param = null,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var result = await conn.QueryAsync<T>(
            new CommandDefinition(sql, param, cancellationToken: ct));
        _logger.LogDebug("Query {Sql} took {Ms}ms", sql, sw.ElapsedMilliseconds);
        return result;
    }
}
```

---

## 4 · CORS

CORS (Cross-Origin Resource Sharing) es el mecanismo del navegador que permite o bloquea peticiones de un origen distinto al del documento HTML. En SaaS típico, frontend y backend viven en dominios diferentes, así que CORS es obligatorio.

### 4.1 Configuración en ASP.NET Core

```csharp
// Program.cs
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaPolicy", policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// CRÍTICO: orden importa. UseCors va ANTES de UseAuthentication/UseAuthorization
app.UseCors("SpaPolicy");
app.UseAuthentication();
app.UseAuthorization();
```

### 4.2 Wildcard subdomains para SaaS multi-tenant

```csharp
options.AddPolicy("TenantPolicy", policy =>
{
    policy.SetIsOriginAllowed(origin =>
    {
        var uri = new Uri(origin);
        return uri.Host == "taskflow.com"
            || uri.Host.EndsWith(".taskflow.com")
            || uri.Host == "localhost";
    })
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
});
```

---

## 5 · Rate limiting

Limitar peticiones por segundo protege contra abuso, ataques DDoS y tenants que saturan tu API. Desde .NET 7 hay middleware nativo.

### 5.1 Estrategias

| Estrategia | Cómo funciona |
|-----------|--------------|
| Fixed Window | X peticiones por ventana fija. Simple. Permite picos al cambio de ventana. |
| Sliding Window | Ventana móvil. Más justo, ligeramente más caro de calcular. |
| Token Bucket | Tokens se regeneran a cierta tasa. Permite ráfagas controladas. |
| Concurrency | Limita peticiones simultáneas. Útil para endpoints costosos. |

### 5.2 Configuración en .NET 8+

```csharp
// Program.cs
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Por tenant — 1000 req/min
    options.AddPolicy("per-tenant", context =>
    {
        var tenantId = context.User.FindFirst("tenant_id")?.Value ?? "anonymous";
        return RateLimitPartition.GetSlidingWindowLimiter(tenantId, _ =>
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1000,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
            });
    });

    // Autenticación — 5 intentos por IP cada 5 min
    options.AddPolicy("auth-strict", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
            });
    });
});

app.UseRateLimiter();
app.MapPost("/api/auth/login", LoginHandler).RequireRateLimiting("auth-strict");
app.MapControllers().RequireRateLimiting("per-tenant");
```

---

## 6 · HTTP Client Factory y resiliencia con Polly

Crear instancias de `HttpClient` con `new HttpClient()` es uno de los bugs más clásicos de .NET. Agota sockets y causa fallos intermitentes. La solución es `IHttpClientFactory`.

### 6.1 Patrón correcto — cliente tipado

```csharp
// Registro
builder.Services.AddHttpClient<StripeClient>(client =>
{
    client.BaseAddress = new Uri("https://api.stripe.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Cliente tipado
public class StripeClient
{
    private readonly HttpClient _http;
    public StripeClient(HttpClient http) => _http = http;

    public async Task<Customer> CreateCustomerAsync(string email, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync("v1/customers", new { email }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Customer>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Respuesta vacía de Stripe");
    }
}
```

### 6.2 Polly — retry, circuit breaker y timeout

```csharp
builder.Services
    .AddHttpClient<StripeClient>(client =>
    {
        client.BaseAddress = new Uri("https://api.stripe.com/");
    })
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;

        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.MinimumThroughput = 10;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);

        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
    });
```

### 6.3 Handler personalizado para autenticación

```csharp
public class BearerTokenHandler : DelegatingHandler
{
    private readonly ITokenStore _tokens;
    public BearerTokenHandler(ITokenStore tokens) => _tokens = tokens;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _tokens.GetAccessTokenAsync(ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, ct);
    }
}

// Registro
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services
    .AddHttpClient<InternalServiceClient>()
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddStandardResilienceHandler();
```

---

## 7 · Background jobs y trabajos programados

Tareas que corren fuera del request HTTP: envío de emails, generación de reportes, sincronizaciones, procesamiento asíncrono.

### 7.1 IHostedService — el más simple

```csharp
public class EmailSenderService : BackgroundService
{
    private readonly ILogger<EmailSenderService> _logger;
    private readonly IServiceProvider _services;

    public EmailSenderService(ILogger<EmailSenderService> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IEmailQueue>();
                var pending = await queue.DequeuePendingAsync(stoppingToken);
                foreach (var email in pending)
                    await SendAsync(email, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando emails");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

builder.Services.AddHostedService<EmailSenderService>();
```

### 7.2 Channel<T> — desacoplamiento productor/consumidor

```csharp
// Registro del canal
builder.Services.AddSingleton(Channel.CreateBounded<EmailMessage>(
    new BoundedChannelOptions(1000)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
    }));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<Channel<EmailMessage>>().Writer);
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<Channel<EmailMessage>>().Reader);

// Productor
public class OrderService
{
    private readonly ChannelWriter<EmailMessage> _writer;
    public OrderService(ChannelWriter<EmailMessage> writer) => _writer = writer;

    public async Task ConfirmOrderAsync(Order order, CancellationToken ct)
    {
        // lógica de negocio...
        await _writer.WriteAsync(new EmailMessage(order.CustomerEmail, "Confirmación"), ct);
    }
}

// Consumidor
public class EmailWorker : BackgroundService
{
    private readonly ChannelReader<EmailMessage> _reader;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var msg in _reader.ReadAllAsync(stoppingToken))
            await SendAsync(msg, stoppingToken);
    }
}
```

### 7.3 Hangfire — jobs persistentes con UI

```csharp
// Registro
builder.Services.AddHangfire(config => config
    .UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default"))));
builder.Services.AddHangfireServer();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AdminOnlyAuthorization() }
});

// Tipos de jobs
BackgroundJob.Enqueue<IEmailService>(svc => svc.SendWelcomeAsync(userId));
BackgroundJob.Schedule<IEmailService>(svc => svc.SendReminderAsync(userId),
    TimeSpan.FromHours(24));
RecurringJob.AddOrUpdate<IBillingService>(
    "daily-invoice-generation",
    svc => svc.GenerateDailyInvoicesAsync(),
    Cron.Daily(2));
```

### 7.4 Regla crítica — TenantId en jobs de SaaS multi-tenant

```csharp
// MAL — el job no sabe a qué tenant pertenece
public Task SendWelcomeEmail(Guid userId)
{
    var user = _db.Users.Find(userId);  // ← ¿qué tenant filtra?
}

// BIEN — el TenantId es explícito, el handler recrea el contexto
public async Task SendWelcomeEmail(Guid tenantId, Guid userId)
{
    _tenantContext.SetTenant(tenantId);
    var user = await _db.Users.FindAsync(userId);
}
```

---

## 8 · API versioning, OpenAPI y documentación

### 8.1 API Versioning

| Estrategia | Pros y contras |
|-----------|---------------|
| URL: `/api/v1/customers` | Visible, fácil de cachear. Cambia URLs. |
| Header: `api-version: 1.0` | URLs limpias. Menos visible, complica testing manual. |
| Query string: `?api-version=1.0` | Fácil de testear. Mancha URLs. |
| Media type | RESTful purista. Complejo en práctica. |

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("api-version"));
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/customers")]
public class CustomersController : ControllerBase
{
    [HttpGet, MapToApiVersion("1.0")]
    public Task<IActionResult> GetV1() { /* ... */ }

    [HttpGet, MapToApiVersion("2.0")]
    public Task<IActionResult> GetV2() { /* ... */ }
}
```

### 8.2 OpenAPI / Swagger / Scalar

```csharp
// .NET 9+ — OpenAPI nativo
builder.Services.AddOpenApi();
app.MapOpenApi();

// UI moderna con Scalar
// dotnet add package Scalar.AspNetCore
app.MapScalarApiReference();

// O Swashbuckle clásico:
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskFlow API", Version = "v1",
        Description = "API SaaS multi-tenant"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});
```

### 8.3 Problem Details (RFC 7807)

```csharp
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions["tenantId"] =
            ctx.HttpContext.User.FindFirst("tenant_id")?.Value;
    };
});

app.UseExceptionHandler();
app.UseStatusCodePages();
```

---

## 9 · Output caching y response caching

.NET 7+ trae output caching nativo con políticas finas, incluyendo invalidación por tags.

```csharp
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("public-short", builder =>
        builder.Expire(TimeSpan.FromMinutes(5)));

    options.AddPolicy("per-tenant", builder =>
        builder
            .Expire(TimeSpan.FromMinutes(10))
            .SetVaryByQuery("*")
            .SetVaryByHeader("X-Tenant")
            .Tag("tenant-data"));
});

app.UseOutputCache();

app.MapGet("/api/plans", GetPublicPlans).CacheOutput("public-short");
app.MapGet("/api/dashboard", GetDashboard).CacheOutput("per-tenant");

// Invalidar cache cuando cambia el dato
public class PlanService(IOutputCacheStore cache)
{
    public async Task UpdatePlanAsync(Plan plan, CancellationToken ct)
    {
        // ... actualizar BD
        await cache.EvictByTagAsync("tenant-data", ct);
    }
}
```

---

## 10 · Entity Framework Core en producción

### 10.1 Workflow de migrations

```bash
dotnet ef migrations add AddSubscriptionsTable --project src/TaskFlow.Infrastructure
dotnet ef database update --project src/TaskFlow.Infrastructure

# Generar script SQL
dotnet ef migrations script --project src/TaskFlow.Infrastructure
dotnet ef migrations script --idempotent -o migrate.sql

# Revertir una migración
dotnet ef database update PreviousMigrationName
dotnet ef migrations remove
```

### 10.2 Estrategias para aplicar migrations

| Estrategia | Cuándo se usa |
|-----------|--------------|
| `context.Database.Migrate()` en `Program.cs` | Apps pequeñas. Una sola instancia. Riesgoso en multi-instancia. |
| Script SQL aplicado por pipeline | Producción seria. La migración se aprueba como parte del deploy. |
| Flyway / Liquibase | Equipos grandes con DBA. Más robusto. |

### 10.3 Soft delete

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}

// En DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
        {
            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(GetSoftDeleteFilter(entityType.ClrType));
        }
    }
}

public override int SaveChanges()
{
    foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
    {
        if (entry.State == EntityState.Deleted)
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = DateTime.UtcNow;
            entry.Entity.DeletedBy = _currentUser.UserId;
        }
    }
    return base.SaveChanges();
}
```

### 10.4 Auditoría — created/updated

```csharp
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    Guid CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}

public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var now = DateTime.UtcNow;
    var userId = _currentUser.UserId;

    foreach (var entry in ChangeTracker.Entries<IAuditable>())
    {
        if (entry.State == EntityState.Added)
        {
            entry.Entity.CreatedAt = now;
            entry.Entity.CreatedBy = userId;
        }
        else if (entry.State == EntityState.Modified)
        {
            entry.Entity.UpdatedAt = now;
            entry.Entity.UpdatedBy = userId;
        }
    }
    return await base.SaveChangesAsync(ct);
}
```

### 10.5 Multi-tenancy con query filter global

```csharp
public interface ITenantScoped
{
    Guid TenantId { get; set; }
}

// En OnModelCreating — aplica filtro a todas las entidades del tenant
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
    {
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var body = Expression.Equal(
            Expression.Property(parameter, nameof(ITenantScoped.TenantId)),
            Expression.Property(
                Expression.Constant(this),
                nameof(AppDbContext.CurrentTenantId)));
        modelBuilder.Entity(entityType.ClrType)
            .HasQueryFilter(Expression.Lambda(body, parameter));
    }
}
```

---

## 11 · JWT — Generación, validación y refresh

### 11.1 Configuración de autenticación JWT en .NET

```csharp
// Program.cs
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOpts = builder.Configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()!;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOpts.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOpts.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOpts.SigningKey)),
        };

        // Para SignalR — token desde query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            },
        };
    });
```

### 11.2 Servicio de tokens

```csharp
public class TokenService
{
    private readonly JwtOptions _opts;

    public TokenService(IOptions<JwtOptions> opts) => _opts = opts.Value;

    public string GenerateAccessToken(ClaimsIdentity identity)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: identity.Claims,
            expires: DateTime.UtcNow.AddMinutes(_opts.AccessTokenLifetimeMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? ValidateTokenWithoutLifetime(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = _opts.Issuer,
            ValidateAudience = true, ValidAudience = _opts.Audience,
            ValidateLifetime = false,  // para refresh no validamos expiración
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_opts.SigningKey)),
        };
        try { return handler.ValidateToken(token, parameters, out _); }
        catch { return null; }
    }
}
```

### 11.3 Claims estándar en SaaS multi-tenant

```csharp
var claims = new List<Claim>
{
    new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
    new(JwtRegisteredClaimNames.Email, user.Email),
    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    new("tenant_id", user.TenantId.ToString()),
    new("tenant_slug", user.TenantSlug),
    new(ClaimTypes.Role, user.Role.ToString()),
};
```

---

## 12 · Validación con FluentValidation

### 12.1 Registro automático

```csharp
// dotnet add package FluentValidation.AspNetCore
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Pipeline behavior para ejecutar validadores automáticamente
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### 12.2 Validador con reglas de negocio

```csharp
public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    private readonly ICustomerRepository _repo;

    public CreateCustomerValidator(ICustomerRepository repo)
    {
        _repo = repo;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(200).WithMessage("Máximo 200 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("Email inválido")
            .MustAsync(BeUniqueEmail).WithMessage("Email ya registrado");

        RuleFor(x => x.TaxId)
            .Matches(@"^\d{3}-\d{6}-\d{3}$").When(x => !string.IsNullOrEmpty(x.TaxId))
            .WithMessage("RFC debe tener formato 123-123456-123");
    }

    private async Task<bool> BeUniqueEmail(
        string email, CancellationToken ct)
        => !await _repo.ExistsByEmailAsync(email, ct);
}
```

---

## 13 · Pipeline Behaviors

Los Pipeline Behaviors de MediatR / Common.Messaging se ejecutan en la cadena de procesamiento de cada request. Son el lugar correcto para cross-cutting concerns: logging, validación, autorización, caching.

### 13.1 Logging behavior

```csharp
public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", name);
        var sw = Stopwatch.StartNew();

        var response = await next();

        _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms",
            name, sw.ElapsedMilliseconds);
        return response;
    }
}
```

### 13.2 Idempotency behavior

```csharp
public class IdempotencyBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentRequest<TResponse>
{
    private readonly IIdempotencyStore _store;

    public IdempotencyBehavior(IIdempotencyStore store) => _store = store;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var key = request.IdempotencyKey;
        var cached = await _store.GetAsync<TResponse>(key, ct);
        if (cached is not null) return cached;

        var result = await next();
        await _store.SetAsync(key, result, TimeSpan.FromHours(24), ct);
        return result;
    }
}
```

---

## 14 · Repository + Unit of Work

### 14.1 Contratos base

```csharp
public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface IUnitOfWork
{
    ICustomerRepository Customers { get; }
    IOrderRepository Orders { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

### 14.2 Implementación con EF Core

```csharp
public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly AppDbContext _context;

    public ICustomerRepository Customers { get; }
    public IOrderRepository Orders { get; }

    public UnitOfWork(AppDbContext context,
        ICustomerRepository customers,
        IOrderRepository orders)
    {
        _context = context;
        Customers = customers;
        Orders = orders;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    public void Dispose() => _context.Dispose();
}

// Registro
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

### 14.3 Uso en un caso de uso

```csharp
public class CreateOrderHandler : IRequestHandler<CreateOrderRequest, Result<OrderDto>>
{
    private readonly IUnitOfWork _uow;

    public CreateOrderHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Result<OrderDto>> HandleAsync(
        CreateOrderRequest request, CancellationToken ct)
    {
        var customer = await _uow.Customers.GetByIdAsync(request.CustomerId, ct);
        if (customer is null)
            return Result<OrderDto>.NotFound("Cliente no encontrado");

        var order = Order.Create(customer, request.Items);
        await _uow.Orders.AddAsync(order, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<OrderDto>.Success(order.ToDto());
    }
}
```

---

## 15 · Performance y memoria

### 15.1 Span\<T\> y Memory\<T\> — cero allocations

```csharp
// Sin Span — crea subcadena en el heap
public static string ParseTenantId(string header)
    => header.Contains(':') ? header.Split(':')[0] : header;

// Con Span — cero allocations
public static ReadOnlySpan<char> ParseTenantId(ReadOnlySpan<char> header)
{
    var idx = header.IndexOf(':');
    return idx >= 0 ? header[..idx] : header;
}

// Parsing sin allocations
public static bool TryParseGuid(ReadOnlySpan<char> input, out Guid result)
    => Guid.TryParse(input, out result);
```

### 15.2 ObjectPool — reutilizar objetos costosos

```csharp
builder.Services.AddSingleton<ObjectPool<StringBuilder>>(sp =>
    new DefaultObjectPoolProvider().CreateStringBuilderPool(32, 4096));

public class ReportGenerator
{
    private readonly ObjectPool<StringBuilder> _pool;
    public ReportGenerator(ObjectPool<StringBuilder> pool) => _pool = pool;

    public string Generate(IEnumerable<Row> rows)
    {
        var sb = _pool.Get();
        try
        {
            foreach (var row in rows) sb.AppendLine(row.ToString());
            return sb.ToString();
        }
        finally { _pool.Return(sb); }
    }
}
```

### 15.3 BenchmarkDotNet — medir antes de optimizar

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class ParserBenchmarks
{
    private const string Header = "tenant-abc:extra-data";

    [Benchmark(Baseline = true)]
    public string WithStringSplit()
        => Header.Contains(':') ? Header.Split(':')[0] : Header;

    [Benchmark]
    public ReadOnlySpan<char> WithSpan()
    {
        var span = Header.AsSpan();
        var idx = span.IndexOf(':');
        return idx >= 0 ? span[..idx] : span;
    }
}
```

### 15.4 Reducir presión sobre el GC

```csharp
// Usar ArrayPool en lugar de arrays temporales
var buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    // usar buffer...
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}

// Colecciones pre-dimensionadas cuando el tamaño es conocido
var results = new List<CustomerDto>(customers.Count);

// Enumeración por referencia en structs — evita copia
foreach (ref readonly var item in span) { /* item no se copia */ }
```

### 15.5 PeriodicTimer — el temporizador moderno

```csharp
// BackgroundService con PeriodicTimer — se cancela limpiamente
public class MetricsFlusher : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await FlushMetricsAsync(stoppingToken);
        }
    }
}
```

---

## 16 · React de producción

### 16.1 React Router con lazy loading

```tsx
import { createBrowserRouter, RouterProvider, Navigate, Outlet } from 'react-router-dom';
import { lazy, Suspense } from 'react';

const Login = lazy(() => import('./pages/Login'));
const Dashboard = lazy(() => import('./pages/Dashboard'));
const Tasks = lazy(() => import('./pages/Tasks'));

const router = createBrowserRouter([
  {
    path: '/login',
    element: (
      <Suspense fallback={<PageLoader />}>
        <Login />
      </Suspense>
    ),
  },
  {
    path: '/',
    element: <ProtectedLayout />,
    children: [
      { index: true, element: <Dashboard /> },
      { path: 'tasks', element: <Tasks /> },
    ],
  },
]);

export function ProtectedLayout() {
  const { isAuthenticated, loading } = useAuth();
  if (loading) return <PageLoader />;
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  return <Outlet />;
}
```

### 16.2 Axios con interceptors para JWT y refresh

```typescript
import axios from 'axios';
import { config } from '@/config/env';
import { getAccessToken, refreshAccessToken, clearTokens } from '@/auth/tokens';

export const api = axios.create({
  baseURL: config.apiBaseUrl,
  timeout: 30_000,
});

api.interceptors.request.use((req) => {
  const token = getAccessToken();
  if (token) req.headers.Authorization = `Bearer ${token}`;
  return req;
});

let refreshPromise: Promise<string> | null = null;

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config;
    if (error.response?.status === 401 && !original._retry) {
      original._retry = true;
      refreshPromise = refreshPromise || refreshAccessToken();
      try {
        const newToken = await refreshPromise;
        refreshPromise = null;
        original.headers.Authorization = `Bearer ${newToken}`;
        return api(original);
      } catch (e) {
        clearTokens();
        window.location.href = '/login';
        return Promise.reject(e);
      }
    }
    return Promise.reject(error);
  }
);
```

### 16.3 React Hook Form + Zod

```tsx
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const schema = z.object({
  email: z.string().email('Email inválido'),
  password: z.string().min(8, 'Mínimo 8 caracteres'),
});

type FormData = z.infer<typeof schema>;

export function LoginForm() {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  const onSubmit = async (data: FormData) => {
    await api.post('/auth/login', data);
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <input {...register('email')} type="email" />
      {errors.email && <p>{errors.email.message}</p>}
      <input {...register('password')} type="password" />
      {errors.password && <p>{errors.password.message}</p>}
      <button disabled={isSubmitting}>Iniciar sesión</button>
    </form>
  );
}
```

### 16.4 date-fns

```typescript
import { format, formatDistance, parseISO, addDays, isAfter } from 'date-fns';
import { es } from 'date-fns/locale';

format(new Date(), 'dd/MM/yyyy HH:mm', { locale: es });
// '08/05/2026 14:30'

formatDistance(parseISO(task.createdAt), new Date(), { locale: es, addSuffix: true });
// 'hace 3 horas'

const trialEndsAt = addDays(new Date(), 14);
if (isAfter(new Date(), parseISO(subscription.expiresAt))) { /* vencida */ }
```

### 16.5 i18n con react-i18next

```typescript
import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import esMX from './locales/es-MX/common.json';
import enUS from './locales/en-US/common.json';

i18n.use(initReactI18next).init({
  resources: {
    'es-MX': { common: esMX },
    'en-US': { common: enUS },
  },
  lng: 'es-MX',
  fallbackLng: 'en-US',
  interpolation: { escapeValue: false },
});
```

```tsx
import { useTranslation } from 'react-i18next';

export function Header() {
  const { t, i18n } = useTranslation();
  return (
    <header>
      <h1>{t('welcome', { name: user.name })}</h1>
      <button onClick={() => i18n.changeLanguage('en-US')}>EN</button>
    </header>
  );
}
```

### 16.6 Optimistic updates con TanStack Query

```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query';

function useToggleTask() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (taskId: string) => api.patch(`/tasks/${taskId}/toggle`),
    onMutate: async (taskId) => {
      await queryClient.cancelQueries({ queryKey: ['tasks'] });
      const previous = queryClient.getQueryData<Task[]>(['tasks']);
      queryClient.setQueryData<Task[]>(['tasks'], (old) =>
        old?.map((t) => (t.id === taskId ? { ...t, completed: !t.completed } : t))
      );
      return { previous };
    },
    onError: (_err, _taskId, context) => {
      queryClient.setQueryData(['tasks'], context?.previous);
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
    },
  });
}
```

### 16.7 Feature hook — patrón de encapsulamiento

```typescript
// Encapsula toda la lógica de una feature en un hook
export function useTaskManager() {
  const [filter, setFilter] = useState<'all' | 'done' | 'pending'>('all');
  const { data: tasks = [], isLoading } = useQuery({
    queryKey: ['tasks', filter],
    queryFn: () => api.get<Task[]>(`/tasks?filter=${filter}`).then((r) => r.data),
  });
  const toggleTask = useToggleTask();
  const createTask = useCreateTask();

  const filteredTasks = useMemo(() => {
    if (filter === 'done') return tasks.filter((t) => t.completed);
    if (filter === 'pending') return tasks.filter((t) => !t.completed);
    return tasks;
  }, [tasks, filter]);

  return { filteredTasks, isLoading, filter, setFilter, toggleTask, createTask };
}
```

---

## 17 · Tooling de equipo

### 17.1 .gitignore

```gitignore
# .NET
bin/
obj/
*.user
*.suo
.vs/
[Tt]est[Rr]esult*/

# Node / React
node_modules/
dist/
build/
.vite/
coverage/

# Variables de entorno
.env
.env.local
.env.*.local

# IDE
.idea/
.vscode/
!.vscode/settings.json
!.vscode/extensions.json

# OS
.DS_Store
Thumbs.db

# Secretos
*.pfx
*.pem
*.key
secrets.json
```

### 17.2 .dockerignore

```dockerignore
**/.git
**/.gitignore
**/.vs
**/.vscode
**/.idea
**/bin
**/obj
**/node_modules
**/dist
**/build
**/.env
**/.env.**
**/coverage
**/*.md
**/Dockerfile*
**/docker-compose*
**/.dockerignore
**/README.md
**/LICENSE
```

### 17.3 .editorconfig

```ini
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.{js,jsx,ts,tsx,json,yml,yaml,html,css}]
indent_size = 2

[*.cs]
indent_size = 4
csharp_new_line_before_open_brace = all
csharp_indent_case_contents = true
```

### 17.4 Husky + lint-staged

```bash
# Frontend:
npm install --save-dev husky lint-staged
npx husky init
```

```json
// package.json
{
  "scripts": {
    "prepare": "husky",
    "lint": "eslint .",
    "format": "prettier --write ."
  },
  "lint-staged": {
    "*.{js,jsx,ts,tsx}": ["eslint --fix", "prettier --write"],
    "*.{json,css,md}": ["prettier --write"]
  }
}
```

```bash
# .husky/pre-commit
npx lint-staged
```

### 17.5 Dependabot

```yaml
# .github/dependabot.yml
version: 2
updates:
  - package-ecosystem: "npm"
    directory: "/frontend"
    schedule: { interval: "weekly" }
    open-pull-requests-limit: 10

  - package-ecosystem: "nuget"
    directory: "/backend"
    schedule: { interval: "weekly" }

  - package-ecosystem: "docker"
    directory: "/"
    schedule: { interval: "weekly" }

  - package-ecosystem: "github-actions"
    directory: "/"
    schedule: { interval: "monthly" }
```

---

## 18 · Testing

### 18.1 xUnit — estructura básica

```csharp
public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _repoMock = new();
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _sut = new CustomerService(_repoMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenEmailIsUnique_ReturnsSuccess()
    {
        // Arrange
        _repoMock.Setup(r => r.ExistsByEmailAsync("test@test.com", default))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.CreateAsync(
            new CreateCustomerRequest("Test", "test@test.com"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    public async Task CreateAsync_WhenEmailInvalid_ReturnsFailure(string email)
    {
        var result = await _sut.CreateAsync(new CreateCustomerRequest("Test", email));
        Assert.False(result.IsSuccess);
    }
}
```

### 18.2 Integration tests con TestContainers

```csharp
public class CustomersApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(opts =>
                        opts.UseNpgsql(_postgres.GetConnectionString()));
                });
            });

        _client = factory.CreateClient();

        // Aplicar migrations
        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task POST_customers_returns_201()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/customers",
            new { name = "Acme Corp", email = "admin@acme.com" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

### 18.3 Pruebas de validación con FluentValidation

```csharp
public class CreateCustomerValidatorTests
{
    private readonly CreateCustomerValidator _validator;

    public CreateCustomerValidatorTests()
    {
        var repoMock = new Mock<ICustomerRepository>();
        repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);
        _validator = new CreateCustomerValidator(repoMock.Object);
    }

    [Fact]
    public async Task Should_fail_when_email_is_empty()
    {
        var result = await _validator.TestValidateAsync(
            new CreateCustomerRequest("Test", ""));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Should_pass_valid_request()
    {
        var result = await _validator.TestValidateAsync(
            new CreateCustomerRequest("Test Corp", "admin@test.com"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

### 18.4 TDD — ciclo Red-Green-Refactor

```
1. Red   — escribir el test que falla (la feature no existe aún)
2. Green — escribir el código mínimo para pasar el test
3. Refactor — limpiar sin romper los tests
```

```csharp
// Paso 1: Red — test primero
[Fact]
public void Discount_WhenMoreThan10Items_Applies10Percent()
{
    var order = new Order();
    for (int i = 0; i < 11; i++)
        order.AddItem(new OrderItem { Price = 100m });

    Assert.Equal(0.10m, order.DiscountRate);
}

// Paso 2: Green — implementación mínima
public class Order
{
    private readonly List<OrderItem> _items = new();
    public decimal DiscountRate => _items.Count > 10 ? 0.10m : 0m;
    public void AddItem(OrderItem item) => _items.Add(item);
}
```

---

## 19 · CI/CD con GitHub Actions

### 19.1 Pipeline básico .NET

```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test
        run: dotnet test --no-build --configuration Release --verbosity normal
          --collect:"XPlat Code Coverage"

      - name: Upload coverage
        uses: codecov/codecov-action@v4
```

### 19.2 Deploy a AWS con OIDC (sin credenciales de larga vida)

```yaml
# .github/workflows/deploy.yml
name: Deploy

on:
  push:
    branches: [main]

permissions:
  id-token: write
  contents: read

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@v4

      - name: Configure AWS credentials (OIDC)
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::123456789:role/github-actions-deploy
          aws-region: us-east-1

      - name: Login to ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v2

      - name: Build, tag, push
        env:
          REGISTRY: ${{ steps.login-ecr.outputs.registry }}
          IMAGE_TAG: ${{ github.sha }}
        run: |
          docker build -t $REGISTRY/taskflow-api:$IMAGE_TAG .
          docker push $REGISTRY/taskflow-api:$IMAGE_TAG

      - name: Deploy ECS
        run: |
          aws ecs update-service \
            --cluster taskflow-prod \
            --service api \
            --force-new-deployment
```

### 19.3 Workflow reutilizable

```yaml
# .github/workflows/reusable-test.yml
on:
  workflow_call:
    inputs:
      dotnet-version:
        required: false
        type: string
        default: '10.x'

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ inputs.dotnet-version }}
      - run: dotnet test --configuration Release

# Consumidor
jobs:
  tests:
    uses: ./.github/workflows/reusable-test.yml
    with:
      dotnet-version: '10.x'
```

---

## 20 · Checklists de verificación

### 20.1 Antes de hacer commit

- [ ] ¿Hay secretos, contraseñas o API keys reales en el diff?
- [ ] ¿Hay `console.log`, `Console.WriteLine`, `debugger` o código de prueba?
- [ ] ¿Pasa el lint y el format?
- [ ] ¿Compila localmente?
- [ ] ¿Pasan los tests que tocan el área modificada?
- [ ] ¿El mensaje de commit sigue conventional commits?

### 20.2 Antes de hacer merge a main

- [ ] ¿El PR fue revisado por al menos una persona?
- [ ] ¿Pasaron todos los checks de CI?
- [ ] ¿Hay tests para el código nuevo?
- [ ] ¿La documentación está actualizada?
- [ ] ¿Las variables de entorno nuevas están documentadas?
- [ ] ¿Las migraciones de BD son reversibles?
- [ ] ¿Hay cambios breaking? Si sí, ¿se documentaron y se incrementó MAJOR?

### 20.3 Antes de desplegar a producción

- [ ] ¿Se probó en staging con datos reales (no producción) primero?
- [ ] ¿Las migraciones se probaron en staging?
- [ ] ¿Se agregaron las variables de entorno nuevas a producción?
- [ ] ¿Los secretos nuevos están en Key Vault / Secrets Manager?
- [ ] ¿Hay un plan de rollback documentado?
- [ ] ¿La ventana de deploy es la correcta (no viernes 5 PM)?
- [ ] ¿Está alguien on-call por si algo falla?

### 20.4 Diagnóstico rápido cuando algo falla en producción

1. **Health checks** — `/health` responde 200 en cada servicio?
2. **Logs** — en Seq buscar errores en los últimos 15 minutos.
3. **Métricas** — latencia p95, tasa de error, uso de CPU/memoria.
4. **Conexiones BD** — ¿hay pool agotado? ¿queries lentas?
5. **Servicios externos** — ¿Stripe está caído? ¿el SMTP responde?
6. **Última versión** — ¿este bug existía antes? ¿coincide con un deploy reciente?

---

## 21 · La librería Common — bootstrap completo de un SaaS

Toda la teoría del manual converge en una librería real. Common es la base de código que cada SaaS arranca con los problemas transversales ya resueltos. Está construida en .NET 10, es open-source bajo Raptor-Dev-Services, y resuelve de un golpe los temas más dolorosos del bootstrap: logging estructurado con Seq, observabilidad con OpenTelemetry, mediator propio, multi-tenancy con resolución por header y subdominio, factories de conexión a Postgres, propagación automática de tenant en HTTP clients y health checks listos.

### 21.1 Qué resuelve Common

Antes de Common, cada proyecto nuevo repetía las mismas cien líneas de plumbing: configurar Serilog, configurar OTel, escribir un middleware de tenant, una factory de conexión, un Result tipado, contratos de mediator. Cada implementación era ligeramente distinta y cada bug se reproducía en cada proyecto.

**Beneficios concretos:**

- Bootstrap de un SaaS profesional en cinco líneas de código.
- Comportamiento consistente en logs, traces y métricas a través de todos los servicios.
- Multi-tenancy correctamente implementado desde el día uno, sin parches a futuro.
- Tenant propagado automáticamente en HTTP clients salientes.
- Health checks integrados para Postgres y Redis sin escribir código por proyecto.
- Mediator propio sin dependencia externa — evita romper cuando MediatR cambia su licenciamiento.
- Factories de conexión Npgsql con Dapper y logging integrado.

### 21.2 Mapa de módulos

| Módulo | Namespace | Responsabilidad |
|--------|-----------|----------------|
| Logging | `Common.Logging` | Serilog con sinks Console/Debug/Seq y enriquecimiento automático con TenantId. |
| Observability | `Common.Observability` | OpenTelemetry: traces y metrics por OTLP a Tempo/Jaeger, exporter Prometheus. |
| Results | `Common.Results` | `Result`/`Success`/`Failure` tipado para evitar excepciones de control. |
| Errors | `Common.Errors` | `ErrorList` para acumular errores de validación o dominio. |
| Exceptions | `Common.Exceptions` | Excepciones de dominio reutilizables. |
| Messaging | `Common.Messaging` | Contratos `IRequest`, `IResponse`, `IMediator` y pipeline behaviors. |
| Abstractions | `Common.Abstractions` | Interfaces base de interactores y presenters. |
| ViewModels | `Common.ViewModels` | ViewModels genéricos reutilizables en respuestas. |
| MultiTenancy | `Common.MultiTenancy` | Resolución de tenant, contexto actual y configuración por tenant. |
| Data | `Common.Data` | Abstracciones de conexiones DB para que cada proyecto implemente su proveedor. |
| PostgreSql | `Common.PostgreSql` | Factorías Npgsql, health checks, migraciones por scripts al arranque. |

### 21.3 Dependencias clave

| Paquete | Para qué se usa |
|---------|----------------|
| `Microsoft.Extensions.Configuration 10.0.2` | Lectura y binding de configuración. |
| `Microsoft.Extensions.DependencyInjection 10.0.2` | Registro y resolución de dependencias. |
| `Microsoft.Extensions.Http.Resilience 10.2.0` | Resiliencia para llamadas HTTP salientes (Polly integrado). |
| `Serilog 4.3.0` | Logger estructurado. |
| `AspNetCore.HealthChecks.NpgSql 9.0.0` | Health checks para PostgreSQL. |
| `AspNetCore.HealthChecks.Redis 9.0.0` | Health checks para Redis. |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | Exportación OTLP a Grafana Tempo, Jaeger u OTEL Collector. |
| `OpenTelemetry.Exporter.Prometheus.AspNetCore` | Métricas para scraping de Prometheus. |
| `OpenTelemetry.Instrumentation.AspNetCore` | Instrumentación automática de requests. |
| `OpenTelemetry.Instrumentation.Http` | Instrumentación de HttpClient. |
| `OpenTelemetry.Instrumentation.Runtime` | Métricas de runtime: GC, CPU, threads. |

### 21.4 Cómo consumirla en un proyecto nuevo

**Paso 1 — Referenciar el proyecto**

```xml
<!-- En el .csproj del Web API -->
<ItemGroup>
  <ProjectReference Include="..\Common\Common.csproj" />
</ItemGroup>
```

**Paso 2 — Registrar servicios principales**

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

**Paso 3 — Registrar middlewares base**

```csharp
var app = builder.Build();

// Resuelve el tenant del request entrante y lo deja disponible en todo el pipeline
app.UseTenantResolution();

// Asigna o propaga un correlation id para trazas y logs
app.UseCorrelationId();

// Manejo estandarizado de errores con Problem Details (RFC 7807)
app.UseCoreProblemDetails();

app.MapControllers();
app.Run();
```

### 21.5 Configuración esperada en appsettings.json

```json
{
  "CustomLogging": {
    "Project": "TaskFlow",
    "SeqUri": "http://localhost:5341",
    "LogEventLevel": "Information",
    "Application": "TaskFlow.Api",
    "Version": "1.0.0"
  },
  "Observability": {
    "ServiceName": "TaskFlow.Api",
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
          "Default": "Host=db;Database=TenantA;..."
        },
        "Settings": {
          "Region": "MX"
        }
      },
      "tenant-b": {
        "IsEnabled": true,
        "ConnectionStrings": {
          "Default": "Host=db;Database=TenantB;..."
        },
        "Settings": {
          "Region": "US"
        }
      }
    }
  }
}
```

**Flags de MultiTenancy**

| Flag | Comportamiento |
|------|---------------|
| `RequireTenant` | Si `true`, los requests sin tenant identificable son rechazados con 400. |
| `RejectUnknownTenants` | Si `true`, los tenants no registrados son rechazados. Defensa contra subdominios inventados. |
| `TenantHeaderName` | Nombre del header HTTP usado para resolver tenant. Convención: `X-Tenant-Id`. |
| `ResolveFromHeader` | Activa la resolución por header HTTP. |
| `ResolveFromSubdomain` | Activa la resolución por subdominio (`acme.miapp.com` → tenant `acme`). |
| `DefaultTenantId` | Fallback para casos sin tenant explícito (típicamente endpoints como `/health`). |
| `Tenants` | Catálogo de tenants conocidos. Cada uno define `IsEnabled`, `ConnectionStrings` y `Settings`. |

### 21.6 Conexión a base de datos por tenant — patrón con clases marcadoras

```csharp
using Common.PostgreSql;

// 1. Clase marcadora — solo identifica tipo, sin lógica
public sealed class MainDbConnection { }

// 2. Factory específica — el nombre de la cadena en appsettings es "MainDbConnection"
public sealed class ConfigurationMainDbConnectionFactory
    : ConfigurationNpgsqlConnectionFactory<MainDbConnection>
{
    public ConfigurationMainDbConnectionFactory(IConfiguration configuration)
        : base(configuration) { }
}

// 3. Registro
builder.Services.AddSingleton<ConfigurationMainDbConnectionFactory>();

// 4. Uso desde un repositorio
public class CustomerRepository
{
    private readonly ConfigurationMainDbConnectionFactory _factory;

    public CustomerRepository(ConfigurationMainDbConnectionFactory factory)
        => _factory = factory;

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<Customer>(
            "SELECT * FROM customers WHERE id = @id AND is_deleted = false",
            new { id });
    }
}
```

### 21.7 Ejecución de jobs con contexto de tenant

```csharp
public class DailyReportsJob
{
    private readonly ITenantExecutionContextRunner _runner;
    private readonly IReportService _reportService;

    public DailyReportsJob(
        ITenantExecutionContextRunner runner,
        IReportService reportService)
    {
        _runner = runner;
        _reportService = reportService;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var activeTenants = new[] { "tenant-a", "tenant-b" };

        foreach (var tenantId in activeTenants)
        {
            await _runner.RunAsync(tenantId, async ct =>
            {
                // Todo lo que se ejecute aquí conserva TenantId en:
                // - contexto (ITenantContext devuelve este tenant)
                // - logs (cada log de Serilog enriquece con TenantId)
                // - traces (Activity.Current tiene tenant.id)
                // - HTTP clients salientes (propagan X-Tenant-Id)
                await _reportService.GenerateDailyReportAsync(ct);
            }, cancellationToken);
        }
    }
}
```

> **Si tu código corre fuera de un request HTTP, debe entrar a un `RunAsync` explícito.** Sin eso, no tiene tenant.

### 21.8 Propagación de tenant en HTTP clients

```csharp
// Cualquier HttpClient registrado con AddTenantPropagation()
// agrega automáticamente el header X-Tenant-Id en cada request saliente.
builder.Services
    .AddHttpClient("billing", c => c.BaseAddress = new Uri("https://billing.internal/"))
    .AddCoreResilience()
    .AddTenantPropagation();

// Resultado: cuando llamas
//   var client = httpClientFactory.CreateClient("billing");
//   await client.GetAsync("/invoices");
// la petición sale con X-Tenant-Id: <tenant-actual> sin escribir nada extra.
```

### 21.9 Logging con TenantId automático

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
//   "Application": "TaskFlow.Api",
//   "Environment": "Production"
// }

// En Seq — errores del tenant 'acme' en la última hora:
// TenantId = 'acme' and @Level = 'Error' and @t > Now() - 1h
```

### 21.10 Observabilidad — traces y métricas listos

Después de `AddObservability`, la API expone automáticamente:

- **Traces** vía OTLP al endpoint configurado. Compatible con Grafana Tempo, Jaeger, Datadog.
- **Métricas** vía OTLP y adicionalmente en `/metrics` para scraping de Prometheus.
- **Métricas de runtime**: GC, heap, exceptions, threads.
- **Instrumentación automática** de ASP.NET Core: latencias, status codes, throughput por endpoint.
- **Instrumentación automática** de HttpClient: latencia, errores, dependencias salientes.
- Cada Activity (span) lleva `tenant.id` automáticamente cuando hay tenant resuelto.

### 21.11 Health checks integrados

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

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false  // solo verifica que la app responde
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = h => h.Tags.Contains("ready")  // verifica dependencias
});
```

### 21.12 Mediator propio (Common.Messaging)

Common incluye su propia implementación de mediator para no depender de MediatR, cuya licencia comercial cambió en 2024.

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

// Controlador delgado — sin lógica de negocio
[HttpPost]
public async Task<IActionResult> Create(
    CreateCustomerRequest request,
    [FromServices] IMediator mediator,
    CancellationToken ct)
{
    var result = await mediator.SendAsync(request, ct);
    return result.IsSuccess
        ? Ok(result.Value)
        : BadRequest(result.Error);
}
```

### 21.13 Result pattern (Common.Results)

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
if (result.IsSuccess)
    return Ok(result.Value);
else
    return result.Error.Type switch
    {
        ErrorType.NotFound => NotFound(result.Error),
        ErrorType.Conflict => Conflict(result.Error),
        _ => BadRequest(result.Error),
    };
```

### 21.14 Troubleshooting de Common

| Síntoma | Causa probable | Solución |
|---------|---------------|---------|
| `AddSerilog` no existe | Falta paquete `Serilog.Extensions.Logging` | Verificar `PackageReference` en `Common.csproj` |
| `WriteTo.Seq` no existe | Falta paquete `Serilog.Sinks.Seq` | Instalar el paquete correspondiente |
| No llegan traces a Grafana/Tempo | Endpoint OTLP incorrecto | Verificar `Observability:OtlpEndpoint` y conectividad |
| No aparecen métricas en Prometheus | Falta endpoint de scrape | Habilitar endpoint Prometheus en la API |
| Logs sin tenant | Middleware multi-tenant no registrado | Asegurar `app.UseTenantResolution()` ANTES de procesar endpoints |
| HTTP salientes sin tenant | Falta propagación en HttpClient | Agregar `.AddTenantPropagation()` al registrar el cliente |
| Jobs sin tenant en logs | No se setea contexto fuera de HTTP | Ejecutar con `ITenantExecutionContextRunner.RunAsync` |
| `RejectUnknownTenants` rechaza válidos | Tenant no está en sección `Tenants` del appsettings | Agregar tenant al catálogo o desactivar el flag |

### 21.15 Bootstrap completo de un SaaS desde cero

```csharp
// Program.cs — el punto de partida real de cualquier nuevo SaaS
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
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!, tags: ["ready"]);

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

**El valor real:** estas ~40 líneas de `Program.cs` dan: logging estructurado a Seq con tenant y correlation id automáticos; traces y métricas con OpenTelemetry; resolución de tenant por header y subdominio con catálogo y guardia de unknown; mediator listo para casos de uso; conexiones a Postgres tipadas; health checks live/ready; HTTP clients resilientes con propagación de tenant. Tres semanas de bootstrap colapsadas en quince minutos.

### 21.16 Cuándo NO usar Common

Common es una librería de opinión. No tiene sentido en estos casos:

- **Sistemas mono-tenant** donde el overhead de multi-tenancy es ruido.
- **Microservicios mínimos** tipo Lambda donde la huella de runtime importa más que la productividad.
- **Apps con infraestructura propia consolidada** — migrar a Common solo se justifica con cambios graduales.
- **Equipos que prefieren MediatR, EF Core con providers exóticos** o stacks de observabilidad diferentes.

La regla simple: si vas a construir un SaaS multi-tenant nuevo en .NET, parte de Common. Si no, evalúa si los módulos individuales (Logging, Observability) te sirven aislados.

---

## Cierre

Este manual cubre las rutas operativas que se aprenden con dolor. Cada configuración aquí presente fue, en algún momento, una madrugada de debug. Aprender de la experiencia ajena ahorra esas madrugadas.

La librería Common es la materialización en código de buena parte del manual. Donde el manual dice "haz X", Common ofrece "llama a este método y X queda hecho". La capacitación del equipo combina los dos: el manual explica el por qué, Common entrega el cómo.

El manual evolucionará. Cada vez que un integrante del equipo tope con un problema operativo no documentado aquí, se agrega. La meta es que ningún ingeniero tenga que tropezar con la misma piedra dos veces.

> **Documento vivo.** Si encuentras algo que falta, está mal o hay una mejor práctica, dilo. Esta es la versión 2.0; la 3.0 incluirá los aprendizajes de la primera generación que tome el curso y las nuevas funcionalidades de Common conforme la librería evolucione.

---

> Fuente: *Apps and Services with .NET 8* (Mark J. Price) — Ch. 2-4 Dependency Injection y Configuración; *Web API Development with ASP.NET Core 8* (Quan Nguyen) — Ch. 5-9 Seguridad y Middleware; *Full Stack React, TypeScript, and Node* (David Choi) — Ch. 8-12 Frontend de producción; *Docker: Up and Running 3rd Ed* (Sean Kane) — Ch. 4 Variables de entorno y secretos

---

*Rogelio Arriaga Gonzalez*
