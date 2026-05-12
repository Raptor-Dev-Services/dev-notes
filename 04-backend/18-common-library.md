# 14 · La librería Common — bootstrap completo de un SaaS

Toda la teoría del manual converge en una librería real. Common es la base de código que cada SaaS arranca con los problemas transversales ya resueltos. Está construida en .NET 10, es open-source bajo Raptor-Dev-Services, y resuelve de un golpe los temas más dolorosos del bootstrap: logging estructurado con Seq, observabilidad con OpenTelemetry, mediator propio, multi-tenancy con resolución por header y subdominio, factories de conexión a Postgres, propagación automática de tenant en HTTP clients, y health checks listos.

Este capítulo describe qué resuelve cada módulo y cómo se consume. La idea es que cualquier nuevo proyecto del equipo empiece referenciando Common y, en cinco líneas de Program.cs, tenga listos los pilares de un SaaS profesional.

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><p><strong>Repositorio</strong></p>
<p>github.com/Raptor-Dev-Services/Common — Librería base reutilizable para Web APIs en .NET 10 con multi-tenancy, observabilidad y mediator. Target framework net10.0. SDK recomendado .NET 10.x.</p></td>
</tr>
</tbody>
</table>

## 14.1 Qué resuelve Common

Antes de Common, cada proyecto nuevo del equipo repetía las mismas cien líneas de plumbing: configurar Serilog, configurar OTel, escribir un middleware de tenant, una factory de conexión, un Result tipado, contratos de mediator. Cada implementación era ligeramente distinta y cada bug se reproducía en cada proyecto.

Common centraliza esa infraestructura. Los beneficios concretos:

- Bootstrap de un SaaS profesional en cinco líneas de código.

- Comportamiento consistente en logs, traces y métricas a través de todos los servicios del equipo.

- Multi-tenancy correctamente implementado desde el día uno, sin parches a futuro.

- Tenant propagado automáticamente en HTTP clients salientes — los servicios consumidores ven el tenant correcto.

- Health checks integrados para Postgres y Redis sin escribir código por proyecto.

- Mediator propio sin dependencia externa, lo que evita romper cuando MediatR cambia su licenciamiento.

- Factories de conexión Npgsql con Dapper y logging integrado.

## 14.2 Mapa de módulos

| **Módulo** | **Namespace** | **Responsabilidad** |
|----|----|----|
| **Logging** | Common.Logging | Registro de Serilog con sinks Console/Debug/Seq y enriquecimiento automático con TenantId. |
| **Observability** | Common.Observability | OpenTelemetry: traces y metrics por OTLP a Tempo/Jaeger, exporter Prometheus. |
| **Results** | Common.Results | Resultado estándar Result/Success/Failure tipado para evitar excepciones de control. |
| **Errors** | Common.Errors | ErrorList utilitaria para acumular errores de validación o dominio. |
| **Exceptions** | Common.Exceptions | Excepciones de dominio reutilizables. |
| **Messaging** | Common.Messaging | Contratos IRequest, IResponse, IMediator y pipeline behaviors. |
| **Abstractions** | Common.Abstractions | Interfaces base de interactores y presenters. |
| **ViewModels** | Common.ViewModels | ViewModels genéricos reutilizables en respuestas. |
| **MultiTenancy** | Common.MultiTenancy | Resolución de tenant, contexto actual y configuración por tenant. |
| **Data** | Common.Data | Abstracciones de conexiones DB para que cada proyecto implemente su proveedor. |
| **PostgreSql** | Common.PostgreSql | Factorías Npgsql, health checks, migraciones por scripts al arranque. |

## 14.3 Dependencias clave

Common no reinventa nada que ya esté resuelto. Se apoya en paquetes maduros y los integra:

| **Paquete** | **Para qué se usa** |
|----|----|
| **Microsoft.Extensions.Configuration 10.0.2** | Lectura y binding de configuración. |
| **Microsoft.Extensions.DependencyInjection 10.0.2** | Registro y resolución de dependencias. |
| **Microsoft.Extensions.Logging 10.0.2** | Abstracciones de logging. |
| **Microsoft.Extensions.Http.Resilience 10.2.0** | Resiliencia para llamadas HTTP salientes (Polly integrado). |
| **Serilog 4.3.0** | Logger estructurado. |
| **AspNetCore.HealthChecks.NpgSql 9.0.0** | Health checks para PostgreSQL. |
| **AspNetCore.HealthChecks.Redis 9.0.0** | Health checks para Redis. |
| **OpenTelemetry.Extensions.Hosting** | Bootstrap OTel en host .NET. |
| **OpenTelemetry.Exporter.OpenTelemetryProtocol** | Exportación OTLP a Grafana Tempo, Jaeger u OTEL Collector. |
| **OpenTelemetry.Exporter.Prometheus.AspNetCore** | Exportación de métricas para scraping de Prometheus. |
| **OpenTelemetry.Instrumentation.AspNetCore** | Instrumentación automática de requests. |
| **OpenTelemetry.Instrumentation.Http** | Instrumentación de HttpClient. |
| **OpenTelemetry.Instrumentation.Runtime** | Métricas de runtime: GC, CPU, threads. |

## 14.4 Cómo consumirla en un proyecto nuevo

La integración tiene tres pasos: referencia, registro de servicios, registro de middlewares.

Paso 1 — Referenciar el proyecto

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>csproj</em></td>
</tr>
<tr>
<td><p>&lt;!-- En el .csproj del Web API que va a consumir Common --&gt;</p>
<p>&lt;ItemGroup&gt;</p>
<p>&lt;ProjectReference Include="..\Common\Common.csproj" /&gt;</p>
<p>&lt;/ItemGroup&gt;</p></td>
</tr>
</tbody>
</table>

Paso 2 — Registrar servicios principales

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C# Program.cs</em></td>
</tr>
<tr>
<td><p>var builder = WebApplication.CreateBuilder(args);</p>
<p>// Logging estructurado con Serilog + Seq</p>
<p>builder.Services.AddLoggingServices(builder.Configuration);</p>
<p>// OpenTelemetry: traces, métricas, exporter OTLP + Prometheus</p>
<p>builder.Services.AddObservability(builder.Configuration);</p>
<p>// Multi-tenancy: resolver por header y subdominio, contexto actual, config por tenant</p>
<p>builder.Services.AddMultiTenancy(builder.Configuration);</p>
<p>// HttpClient resiliente con propagación automática del TenantId actual</p>
<p>builder.Services</p>
<p>.AddHttpClient("core")</p>
<p>.AddCoreResilience()</p>
<p>.AddTenantPropagation();</p></td>
</tr>
</tbody>
</table>

Paso 3 — Registrar middlewares base

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C# Program.cs</em></td>
</tr>
<tr>
<td><p>var app = builder.Build();</p>
<p>// Resuelve el tenant del request entrante (header X-Tenant-Id o subdominio)</p>
<p>// y lo deja disponible en el contexto para toda la cadena posterior.</p>
<p>app.UseTenantResolution();</p>
<p>// Asigna o propaga un correlation id para trazas y logs.</p>
<p>app.UseCorrelationId();</p>
<p>// Manejo estandarizado de errores con Problem Details (RFC 7807).</p>
<p>app.UseCoreProblemDetails();</p>
<p>app.MapControllers();</p>
<p>app.Run();</p></td>
</tr>
</tbody>
</table>

## 14.5 Configuración esperada en appsettings.json

Common espera tres secciones de configuración. La estructura es estable y documentada:

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>appsettings.json</em></td>
</tr>
<tr>
<td><p>{</p>
<p>"CustomLogging": {</p>
<p>"Project": "GTM-Suite",</p>
<p>"SeqUri": "http://localhost:5341",</p>
<p>"LogEventLevel": "Information",</p>
<p>"Application": "MiWebApi",</p>
<p>"Version": "1.0.0"</p>
<p>},</p>
<p>"Observability": {</p>
<p>"ServiceName": "MiWebApi",</p>
<p>"ServiceVersion": "1.0.0",</p>
<p>"OtlpEndpoint": "http://localhost:4317"</p>
<p>},</p>
<p>"MultiTenancy": {</p>
<p>"RequireTenant": true,</p>
<p>"RejectUnknownTenants": true,</p>
<p>"TenantHeaderName": "X-Tenant-Id",</p>
<p>"ResolveFromHeader": true,</p>
<p>"ResolveFromSubdomain": true,</p>
<p>"DefaultTenantId": "default",</p>
<p>"Tenants": {</p>
<p>"tenant-a": {</p>
<p>"IsEnabled": true,</p>
<p>"ConnectionStrings": {</p>
<p>"Default": "Server=...;Database=TenantA;..."</p>
<p>},</p>
<p>"Settings": {</p>
<p>"Region": "MX"</p>
<p>}</p>
<p>},</p>
<p>"tenant-b": {</p>
<p>"IsEnabled": true,</p>
<p>"ConnectionStrings": {</p>
<p>"Default": "Server=...;Database=TenantB;..."</p>
<p>},</p>
<p>"Settings": {</p>
<p>"Region": "US"</p>
<p>}</p>
<p>}</p>
<p>}</p>
<p>}</p>
<p>}</p></td>
</tr>
</tbody>
</table>

Qué controla cada flag de MultiTenancy

| **Flag** | **Comportamiento** |
|----|----|
| **RequireTenant** | Si true, los requests sin tenant identificable son rechazados con 400. |
| **RejectUnknownTenants** | Si true, los tenants no registrados en la sección Tenants son rechazados. Defensa contra subdominios inventados. |
| **TenantHeaderName** | Nombre del header HTTP usado para resolver tenant. Convención: X-Tenant-Id. |
| **ResolveFromHeader** | Activa la resolución por header HTTP. |
| **ResolveFromSubdomain** | Activa la resolución por subdominio (acme.miapp.com → tenant 'acme'). |
| **DefaultTenantId** | Fallback para casos sin tenant explícito (típicamente endpoints públicos como /health). |
| **Tenants** | Catálogo de tenants conocidos. Cada uno define IsEnabled, ConnectionStrings y Settings propios. |

## 14.6 Conexión a base de datos por tenant

Uno de los problemas más finos de SaaS multi-tenant es resolver la conexión correcta según el tenant del request. Common ofrece una jerarquía de contratos para que cada proyecto implemente su proveedor.

| **Tipo** | **Para qué sirve** |
|----|----|
| **DbConnectionFactory** | Base simple para conexiones por cadena fija, sin tenancy. |
| **ConfigurationDbConnectionFactory\<TConnectionName\>** | Resuelve cadena de ConnectionStrings:{typeof(TConnectionName).Name}. Patrón con clases marcadoras. |
| **TenantDbConnectionFactory** | Resuelve la conexión por tenant explícito (útil para jobs). |
| **CurrentTenantDbConnectionFactory** | Usa el tenant actual del request automáticamente. |
| **ITenantConnectionStringResolver** | Contrato para obtener cadenas por tenant desde la configuración. |
| **IDapperSqlDbConnection** | Wrapper Dapper con logging y medición de tiempo automática. |
| **DapperSqlDbConnectionBase** | Implementación base sobre la que cada proyecto extiende. |

Patrón de implementación con clases marcadoras

El patrón usa una clase vacía como marcador de tipo. La factory genérica usa el typeof para resolver el nombre de la connection string. Esto permite tener múltiples conexiones distintas en la misma app de forma tipada.

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>using Common.PostgreSql;</p>
<p>using Microsoft.Extensions.Configuration;</p>
<p>// 1. Clase marcadora — solo identifica tipo, sin lógica.</p>
<p>public sealed class MainDbConnection</p>
<p>{</p>
<p>}</p>
<p>// 2. Factory específica que hereda de la genérica de Common.</p>
<p>// El nombre de la cadena en appsettings es "MainDbConnection".</p>
<p>public sealed class ConfigurationMainDbConnectionFactory</p>
<p>: ConfigurationNpgsqlConnectionFactory&lt;MainDbConnection&gt;</p>
<p>{</p>
<p>public ConfigurationMainDbConnectionFactory(IConfiguration configuration)</p>
<p>: base(configuration)</p>
<p>{</p>
<p>}</p>
<p>}</p>
<p>// 3. Registro en Program.cs</p>
<p>builder.Services.AddSingleton&lt;ConfigurationMainDbConnectionFactory&gt;();</p>
<p>// 4. Uso desde un servicio</p>
<p>public class CustomerRepository</p>
<p>{</p>
<p>private readonly ConfigurationMainDbConnectionFactory _factory;</p>
<p>public CustomerRepository(ConfigurationMainDbConnectionFactory factory)</p>
<p>{</p>
<p>_factory = factory;</p>
<p>}</p>
<p>public async Task&lt;Customer?&gt; GetByIdAsync(Guid id, CancellationToken ct)</p>
<p>{</p>
<p>await using var connection = await _factory.CreateOpenConnectionAsync(ct);</p>
<p>return await connection.QueryFirstOrDefaultAsync&lt;Customer&gt;(</p>
<p>"SELECT * FROM customers WHERE id = @id", new { id });</p>
<p>}</p>
<p>}</p></td>
</tr>
</tbody>
</table>

## 14.7 Ejecución de jobs con contexto de tenant

Los jobs en background son donde se rompe la mayor parte de los SaaS multi-tenant: el contexto del tenant se pierde porque ya no hay request HTTP. Common ofrece un runner que recrea el contexto explícitamente:

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>// Inyectar ITenantExecutionContextRunner</p>
<p>public class DailyReportsJob</p>
<p>{</p>
<p>private readonly ITenantExecutionContextRunner _runner;</p>
<p>private readonly IReportService _reportService;</p>
<p>public DailyReportsJob(</p>
<p>ITenantExecutionContextRunner runner,</p>
<p>IReportService reportService)</p>
<p>{</p>
<p>_runner = runner;</p>
<p>_reportService = reportService;</p>
<p>}</p>
<p>public async Task ExecuteAsync(CancellationToken cancellationToken)</p>
<p>{</p>
<p>// Para cada tenant activo, ejecutar dentro de su contexto</p>
<p>var activeTenants = new[] { "tenant-a", "tenant-b" };</p>
<p>foreach (var tenantId in activeTenants)</p>
<p>{</p>
<p>await _runner.RunAsync(tenantId, async ct =&gt;</p>
<p>{</p>
<p>// Todo lo que se ejecute aquí conserva TenantId en:</p>
<p>// - contexto (ITenantContext devuelve este tenant)</p>
<p>// - logs (cada log de Serilog enriquece con TenantId)</p>
<p>// - traces (Activity.Current.tenant.id queda con este valor)</p>
<p>// - HTTP clients (peticiones salientes propagan el header)</p>
<p>await _reportService.GenerateDailyReportAsync(ct);</p>
<p>}, cancellationToken);</p>
<p>}</p>
<p>}</p>
<p>}</p></td>
</tr>
</tbody>
</table>

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><p><strong>Por qué este runner es crítico</strong></p>
<p>Sin un runner como este, los jobs en background son la fuente número uno de bugs cross-tenant en SaaS. La regla de Common es clara: si tu código corre fuera de un request HTTP, debe entrar a un RunAsync explícito. Si no, no tiene tenant.</p></td>
</tr>
</tbody>
</table>

## 14.8 Propagación de tenant en HTTP clients

Cuando un servicio del SaaS llama a otro servicio (interno o externo), el tenant debe viajar. Common lo automatiza con el extension method AddTenantPropagation:

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>// Cualquier HttpClient registrado con AddTenantPropagation()</p>
<p>// agrega automáticamente el header X-Tenant-Id en cada request saliente,</p>
<p>// usando el tenant actual del contexto de ejecución.</p>
<p>builder.Services</p>
<p>.AddHttpClient("billing", c =&gt; c.BaseAddress = new Uri("https://billing.internal/"))</p>
<p>.AddCoreResilience() // retry + circuit breaker estándar</p>
<p>.AddTenantPropagation(); // ← inyecta header de tenant automáticamente</p>
<p>// Resultado: cuando llamas</p>
<p>// var client = httpClientFactory.CreateClient("billing");</p>
<p>// await client.GetAsync("/invoices");</p>
<p>// la petición sale con X-Tenant-Id: &lt;tenant-actual&gt; sin escribir nada.</p></td>
</tr>
</tbody>
</table>

## 14.9 Logging con TenantId automático

El middleware de tenancy enriquece cada scope de log con el TenantId. Esto significa que CADA log emitido durante un request lleva implícitamente la propiedad. En Seq, filtrar por tenant es trivial:

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>// Sin escribir nada extra:</p>
<p>_logger.LogInformation("Customer {CustomerId} created", customer.Id);</p>
<p>// Lo que llega a Seq:</p>
<p>// {</p>
<p>// "@t": "2026-05-08T14:23:00Z",</p>
<p>// "@l": "Information",</p>
<p>// "@m": "Customer abc-123 created",</p>
<p>// "CustomerId": "abc-123",</p>
<p>// "TenantId": "acme", ← inyectado automáticamente</p>
<p>// "CorrelationId": "req-xyz", ← inyectado automáticamente</p>
<p>// "Application": "MiWebApi",</p>
<p>// "Environment": "Production"</p>
<p>// }</p>
<p>// En Seq buscar todos los errores del tenant 'acme' en la última hora:</p>
<p>// TenantId = 'acme' and @Level = 'Error' and @t &gt; Now() - 1h</p></td>
</tr>
</tbody>
</table>

## 14.10 Observabilidad — traces y métricas listas

Después de AddObservability, la API expone:

- Traces vía OTLP al endpoint configurado en Observability:OtlpEndpoint. Compatible con Grafana Tempo, Jaeger, Datadog.

- Métricas vía OTLP al mismo endpoint, y adicionalmente expuestas en /metrics para scraping de Prometheus.

- Métricas de runtime: contadores de GC, uso de heap, exceptions, threads.

- Instrumentación automática de ASP.NET Core: latencias, status codes, throughput por endpoint.

- Instrumentación automática de HttpClient: latencia, errores, dependencias salientes.

- Cada Activity (span) lleva tenant.id automáticamente cuando hay tenant resuelto.

## 14.11 Health checks integrados

Common incluye health checks para Postgres y Redis. Solo necesitas registrarlos:

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>builder.Services</p>
<p>.AddHealthChecks()</p>
<p>.AddNpgSql(</p>
<p>connectionString: builder.Configuration.GetConnectionString("Default"),</p>
<p>name: "postgres",</p>
<p>tags: new[] { "db", "ready" })</p>
<p>.AddRedis(</p>
<p>redisConnectionString: builder.Configuration.GetConnectionString("Redis"),</p>
<p>name: "redis",</p>
<p>tags: new[] { "cache", "ready" });</p>
<p>// En el pipeline:</p>
<p>app.MapHealthChecks("/health/live", new HealthCheckOptions</p>
<p>{</p>
<p>Predicate = _ =&gt; false // solo verifica que la app responde</p>
<p>});</p>
<p>app.MapHealthChecks("/health/ready", new HealthCheckOptions</p>
<p>{</p>
<p>Predicate = h =&gt; h.Tags.Contains("ready") // verifica dependencias</p>
<p>});</p></td>
</tr>
</tbody>
</table>

## 14.12 Mediator propio (Common.Messaging)

Common incluye su propia implementación de mediator. La razón principal es no depender de MediatR, cuya licencia comercial cambió en 2024 y que ahora cobra para uso empresarial. Common define los contratos puros: IRequest, IResponse, IMediator y pipeline behaviors.

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>// Definir un caso de uso</p>
<p>public sealed record CreateCustomerRequest(string Name, string Email)</p>
<p>: IRequest&lt;Result&lt;CustomerDto&gt;&gt;;</p>
<p>public sealed class CreateCustomerHandler</p>
<p>: IRequestHandler&lt;CreateCustomerRequest, Result&lt;CustomerDto&gt;&gt;</p>
<p>{</p>
<p>private readonly ICustomerRepository _repo;</p>
<p>public CreateCustomerHandler(ICustomerRepository repo) =&gt; _repo = repo;</p>
<p>public async Task&lt;Result&lt;CustomerDto&gt;&gt; HandleAsync(</p>
<p>CreateCustomerRequest request,</p>
<p>CancellationToken ct)</p>
<p>{</p>
<p>// Validación temprana, fail fast</p>
<p>if (string.IsNullOrWhiteSpace(request.Email))</p>
<p>return Result&lt;CustomerDto&gt;.Failure("Email es requerido");</p>
<p>var customer = new Customer(request.Name, request.Email);</p>
<p>await _repo.AddAsync(customer, ct);</p>
<p>return Result&lt;CustomerDto&gt;.Success(customer.ToDto());</p>
<p>}</p>
<p>}</p>
<p>// Uso desde un controller (controlador delgado, sin lógica)</p>
<p>[HttpPost]</p>
<p>public async Task&lt;IActionResult&gt; Create(</p>
<p>CreateCustomerRequest request,</p>
<p>[FromServices] IMediator mediator,</p>
<p>CancellationToken ct)</p>
<p>{</p>
<p>var result = await mediator.SendAsync(request, ct);</p>
<p>return result.IsSuccess</p>
<p>? Ok(result.Value)</p>
<p>: BadRequest(result.Error);</p>
<p>}</p></td>
</tr>
</tbody>
</table>

## 14.13 Result pattern (Common.Results)

Result encapsula éxito o falla sin lanzar excepciones. Las excepciones quedan reservadas para errores realmente excepcionales; los flujos esperados (validaciones, conflictos de negocio, recursos no encontrados) viajan como Result.

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>public Result&lt;Customer&gt; ActivateCustomer(Guid customerId)</p>
<p>{</p>
<p>var customer = _repo.GetById(customerId);</p>
<p>if (customer == null)</p>
<p>return Result&lt;Customer&gt;.NotFound("Cliente no existe");</p>
<p>if (customer.IsActive)</p>
<p>return Result&lt;Customer&gt;.Conflict("Cliente ya está activo");</p>
<p>customer.Activate();</p>
<p>_repo.Update(customer);</p>
<p>return Result&lt;Customer&gt;.Success(customer);</p>
<p>}</p>
<p>// Consumo:</p>
<p>var result = service.ActivateCustomer(id);</p>
<p>if (result.IsSuccess) { /* usar result.Value */ }</p>
<p>else { /* result.Error tiene tipo y mensaje */ }</p></td>
</tr>
</tbody>
</table>

## 14.14 Troubleshooting de Common

| **Síntoma** | **Causa probable** | **Solución** |
|----|----|----|
| **AddSerilog no existe** | Falta paquete Serilog.Extensions.Logging | Verificar PackageReference en Common.csproj |
| **WriteTo.Seq no existe** | Falta paquete Serilog.Sinks.Seq | Instalar el paquete correspondiente |
| **No llegan traces a Grafana/Tempo** | Endpoint OTLP incorrecto | Verificar Observability:OtlpEndpoint y conectividad |
| **No aparecen métricas en Prometheus** | Falta endpoint de scrape | Habilitar endpoint Prometheus en la API y configurar Prometheus |
| **Logs sin tenant** | Middleware multi-tenant no registrado | Asegurar app.UseTenantResolution() ANTES de procesar endpoints |
| **HTTP salientes sin tenant** | Falta propagación en HttpClient | Agregar .AddTenantPropagation() al registrar el cliente |
| **Jobs sin tenant en logs** | No se setea contexto fuera de HTTP | Ejecutar con ITenantExecutionContextRunner.RunAsync |
| **RejectUnknownTenants rechaza válidos** | Tenant no está en sección Tenants del appsettings | Agregar tenant al catálogo o desactivar el flag |

## 14.15 Notas técnicas finales

- OpenTelemetry exporta traces y metrics por OTLP al endpoint configurado.

- OpenTelemetry expone también métricas para Prometheus mediante AddPrometheusExporter.

- Serilog usa CustomLogging:LogEventLevel (default Verbose); en Development fuerza al menos Debug.

- El middleware de tenant agrega tenant.id al Activity actual y TenantId al scope de logs por request.

- RejectUnknownTenants=true rechaza tenants no registrados cuando existe catálogo en configuración.

- Common está en .NET 10 y usa el formato moderno de solución .slnx.

## 14.16 Cuándo NO usar Common

Common es una librería de opinión. Su filosofía es servir a equipos que construyen SaaS multi-tenant con .NET, Postgres, Seq y OpenTelemetry. No tiene sentido en estos casos:

- Sistemas mono-tenant donde el overhead de multi-tenancy es ruido. Mejor usar ASP.NET Core estándar.

- Microservicios mínimos tipo Lambda donde la huella de runtime importa más que la productividad.

- Aplicaciones que ya tienen una infraestructura propia consolidada — migrar a Common solo se justifica con cambios graduales.

- Equipos que prefieren MediatR, EF Core con providers exóticos, o stacks de observabilidad diferentes.

La regla simple: si vas a construir un SaaS multi-tenant nuevo en .NET, parte de Common. Si no, evalúa si los módulos individuales (Logging, Observability) te sirven aislados — están diseñados para usarse juntos pero no se rompen al consumirlos por separado.

## 14.17 Bootstrap completo de un SaaS desde cero

Para cerrar, así se ve un Program.cs completo de un nuevo SaaS que arranca encima de Common. Es el código real con el que un nuevo proyecto debería empezar:

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C# Program.cs</em></td>
</tr>
<tr>
<td><p>using Common.Logging;</p>
<p>using Common.Observability;</p>
<p>using Common.MultiTenancy;</p>
<p>using Common.Messaging;</p>
<p>var builder = WebApplication.CreateBuilder(args);</p>
<p>// === Pilares del SaaS desde Common ===</p>
<p>builder.Services.AddLoggingServices(builder.Configuration);</p>
<p>builder.Services.AddObservability(builder.Configuration);</p>
<p>builder.Services.AddMultiTenancy(builder.Configuration);</p>
<p>builder.Services.AddMediator(typeof(Program).Assembly);</p>
<p>// === Servicios propios del proyecto ===</p>
<p>builder.Services.AddControllers();</p>
<p>builder.Services.AddOpenApi();</p>
<p>builder.Services.AddProblemDetails();</p>
<p>// === Conexiones a base de datos ===</p>
<p>builder.Services.AddSingleton&lt;ConfigurationMainDbConnectionFactory&gt;();</p>
<p>builder.Services.AddScoped&lt;ICustomerRepository, CustomerRepository&gt;();</p>
<p>// === Health checks ===</p>
<p>builder.Services</p>
<p>.AddHealthChecks()</p>
<p>.AddNpgSql(builder.Configuration.GetConnectionString("Default"), tags: new[] { "ready" });</p>
<p>// === HTTP clients salientes ===</p>
<p>builder.Services</p>
<p>.AddHttpClient("billing", c =&gt; c.BaseAddress = new Uri("https://billing.internal/"))</p>
<p>.AddCoreResilience()</p>
<p>.AddTenantPropagation();</p>
<p>// === Pipeline ===</p>
<p>var app = builder.Build();</p>
<p>app.UseTenantResolution(); // ← antes de cualquier lógica que dependa de tenant</p>
<p>app.UseCorrelationId();</p>
<p>app.UseCoreProblemDetails();</p>
<p>app.MapControllers();</p>
<p>app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ =&gt; false });</p>
<p>app.MapHealthChecks("/health/ready", new HealthCheckOptions</p>
<p>{</p>
<p>Predicate = h =&gt; h.Tags.Contains("ready")</p>
<p>});</p>
<p>app.MapOpenApi();</p>
<p>app.Run();</p></td>
</tr>
</tbody>
</table>

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><p><strong>El valor real</strong></p>
<p>Estas ~40 líneas de Program.cs te dan: logging estructurado a Seq con tenant y correlation id automáticos; traces y métricas con OpenTelemetry; resolución de tenant por header y subdominio con catálogo y guardia de unknown; mediator listo para casos de uso; conexiones a Postgres tipadas; health checks live/ready; HTTP clients resilientes con propagación de tenant. Eso son tres semanas de bootstrap colapsadas en quince minutos.</p></td>
</tr>
</tbody>
</table>



---

*Rogelio Arriaga Gonzalez*
