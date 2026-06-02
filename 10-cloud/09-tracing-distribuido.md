# 09 · Tracing distribuido — OpenTelemetry, Tempo y correlación

> Fuente: *Building Microservices* (Sam Newman) — Ch.10 Distributed Tracing; *Web API Development with ASP.NET Core 8* (Quan Nguyen) — Ch.12 Observability

---

## El problema que resuelve

En una arquitectura con múltiples servicios, una sola request del usuario puede pasar por: API Gateway → Auth Service → Orders API → Inventory API → Notification Service. Si algo falla o tarda, los logs de cada servicio son islas de información. Sin un trace distribuido, el diagnóstico es: comparar timestamps a ojo en cinco dashboards distintos.

Un trace distribuido une todos esos fragmentos bajo un único ID (`TraceId`) que viaja con la request. Se puede ver la cadena completa, qué servicio tardó más, dónde se generó el error, y correlacionarlo con los logs de cada paso.

---

## Conceptos fundamentales

```
Trace  = representación completa de una operación de extremo a extremo.
         Un trace agrupa todos los spans relacionados.

Span   = unidad individual de trabajo dentro del trace.
         Tiene: nombre, TraceId, SpanId, ParentSpanId, timestamps, tags, eventos.

Context propagation = mecanismo para pasar el TraceId entre procesos
                      (servicios, queues, jobs) sin perder la cadena.

Baggage = datos adicionales que viajan junto al contexto del trace
          (ejemplo: TenantId, CorrelationId).
```

### Anatomía de un trace

```
TraceId: 4bf92f3577b34da6a3ce929d0e0e4736
│
├── Span: POST /api/v1/orders                    [API Gateway]     0ms → 245ms
│   ├── Span: ValidateToken                      [Auth Service]    5ms → 32ms
│   ├── Span: CreateOrder                        [Orders API]      33ms → 180ms
│   │   ├── Span: SELECT inventory               [DB query]        40ms → 65ms
│   │   └── Span: INSERT order                   [DB query]        70ms → 95ms
│   └── Span: SendOrderConfirmation              [Notifications]   181ms → 245ms
│       └── Span: POST api.sendgrid.com/send     [HTTP client]     185ms → 244ms
```

---

## W3C TraceContext — el estándar de propagación

W3C TraceContext es el estándar para propagar trace context entre servicios vía HTTP headers. OpenTelemetry lo implementa por defecto.

### Headers

```http
# Header principal — identifica el trace y el span actual
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
             ^^ versión  ^^ trace-id (32 hex)       ^^ parent-span-id  ^^ flags

# Header de estado adicional (vendor-specific, opcional)
tracestate: acme=abc123
```

### Desglose de traceparent

| Campo | Valor en el ejemplo | Descripción |
|-------|--------------------|----|
| version | `00` | Versión del spec (siempre `00`) |
| trace-id | `4bf92f3577b34da6a3ce929d0e0e4736` | ID del trace completo, 16 bytes hex |
| parent-span-id | `00f067aa0ba902b7` | ID del span que hace la llamada, 8 bytes hex |
| trace-flags | `01` | `01` = sampled (traza activa), `00` = no sampled |

---

## OpenTelemetry en .NET

### Instrumentación automática

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService(
            serviceName: "taskflow-api",
            serviceVersion: "1.0.0"))
    .WithTracing(tracing =>
    {
        tracing
            // Instrumentación automática de HTTP requests entrantes
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.RecordException = true;
                opts.Filter = ctx =>
                    !ctx.Request.Path.StartsWithSegments("/health");
            })
            // Instrumentación automática de HttpClient saliente
            .AddHttpClientInstrumentation(opts =>
            {
                opts.RecordException = true;
            })
            // Instrumentación automática de EF Core
            .AddEntityFrameworkCoreInstrumentation(opts =>
            {
                opts.SetDbStatementForText = true;  // incluir el SQL en el span
            })
            // Exportador OTLP (Grafana Tempo, Jaeger, OTEL Collector)
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri("http://localhost:4317");
                opts.Protocol = OtlpExportProtocol.Grpc;
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()    // GC, threads, CPU
            .AddPrometheusExporter();       // expone /metrics para Prometheus
    });

app.MapPrometheusScrapingEndpoint();        // /metrics
```

### ActivitySource — traces personalizados

```csharp
// Definir el ActivitySource de la app (singleton, en una clase estática)
public static class Telemetry
{
    public static readonly ActivitySource ActivitySource =
        new("TaskFlow.Api", "1.0.0");
}

// Registrar el ActivitySource con OTel
tracing.AddSource("TaskFlow.Api");
```

```csharp
// Crear spans custom en casos de uso
public class OrderService
{
    public async Task<Order> ProcessOrderAsync(
        CreateOrderRequest request, CancellationToken ct)
    {
        // Span principal del caso de uso
        using var activity = Telemetry.ActivitySource
            .StartActivity("ProcessOrder", ActivityKind.Internal);

        // Enriquecer el span con datos de negocio
        activity?.SetTag("order.tenant_id", request.TenantId);
        activity?.SetTag("order.customer_id", request.CustomerId);
        activity?.SetTag("order.items_count", request.Items.Count);

        try
        {
            // Sub-span para validación de inventario
            using var validateActivity = Telemetry.ActivitySource
                .StartActivity("ValidateInventory", ActivityKind.Internal);

            var inventoryOk = await _inventory.CheckAsync(request.Items, ct);
            validateActivity?.SetTag("inventory.available", inventoryOk);

            if (!inventoryOk)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Insufficient inventory");
                return null!;
            }

            var order = await _repo.CreateAsync(request, ct);

            // Agregar evento al span (como un log dentro del trace)
            activity?.AddEvent(new ActivityEvent("OrderCreated",
                tags: new ActivityTagsCollection
                {
                    ["order.id"] = order.Id.ToString()
                }));

            activity?.SetTag("order.id", order.Id);
            return order;
        }
        catch (Exception ex)
        {
            // Marcar el span como fallido
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### Baggage — datos que viajan en el trace

```csharp
// Escribir baggage (en middleware de tenant, por ejemplo)
Activity.Current?.SetBaggage("tenant_id", tenantId);
Activity.Current?.SetBaggage("correlation_id", correlationId);

// Leer baggage desde cualquier punto en la cadena
var tenantId = Activity.Current?.GetBaggageItem("tenant_id");
```

---

## IMeterFactory — métricas custom con OTel

```csharp
// Definir métricas de negocio (singleton)
public sealed class OrderMetrics : IDisposable
{
    private readonly Meter _meter;

    public readonly Counter<long> OrdersCreated;
    public readonly Histogram<double> OrderProcessingDuration;
    public readonly ObservableGauge<int> ActiveOrders;

    private int _activeOrderCount;

    public OrderMetrics(IMeterFactory factory)
    {
        _meter = factory.Create("TaskFlow.Orders");

        OrdersCreated = _meter.CreateCounter<long>(
            "taskflow.orders.created",
            unit: "{orders}",
            description: "Total de pedidos creados");

        OrderProcessingDuration = _meter.CreateHistogram<double>(
            "taskflow.orders.processing_duration",
            unit: "s",
            description: "Duración del procesamiento de pedidos");

        ActiveOrders = _meter.CreateObservableGauge<int>(
            "taskflow.orders.active",
            () => _activeOrderCount,
            unit: "{orders}",
            description: "Pedidos actualmente en proceso");
    }

    public void IncrementActive() => Interlocked.Increment(ref _activeOrderCount);
    public void DecrementActive() => Interlocked.Decrement(ref _activeOrderCount);

    public void Dispose() => _meter.Dispose();
}

// Registro
builder.Services.AddSingleton<OrderMetrics>();

// Uso
public class OrderService
{
    private readonly OrderMetrics _metrics;

    public async Task<Order> ProcessOrderAsync(CreateOrderRequest request, CancellationToken ct)
    {
        _metrics.IncrementActive();
        var sw = Stopwatch.StartNew();
        try
        {
            var order = await CreateInternalAsync(request, ct);
            _metrics.OrdersCreated.Add(1,
                new KeyValuePair<string, object?>("tenant", request.TenantId),
                new KeyValuePair<string, object?>("status", "success"));
            return order;
        }
        catch
        {
            _metrics.OrdersCreated.Add(1,
                new KeyValuePair<string, object?>("tenant", request.TenantId),
                new KeyValuePair<string, object?>("status", "error"));
            throw;
        }
        finally
        {
            _metrics.DecrementActive();
            _metrics.OrderProcessingDuration.Record(sw.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("tenant", request.TenantId));
        }
    }
}
```

---

## Correlation IDs

El Correlation ID es un ID que identifica una operación de negocio completa, más allá del scope de un trace técnico. Útil para correlacionar requests de usuario que generan múltiples llamadas asíncronas.

### Middleware de Correlation ID

```csharp
public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Leer el correlation ID del header o generar uno nuevo
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        // Guardarlo en el contexto del request
        context.Items["CorrelationId"] = correlationId;

        // Incluirlo en el response
        context.Response.Headers.TryAdd(HeaderName, correlationId);

        // Enriquecer el scope de Serilog
        using (LogContext.PushProperty("CorrelationId", correlationId))
        // Enriquecer el Activity de OTel
        {
            Activity.Current?.SetTag("correlation.id", correlationId);
            Activity.Current?.SetBaggage("correlation.id", correlationId);

            await _next(context);
        }
    }
}

// Registro — ANTES de UseAuthentication
app.UseMiddleware<CorrelationIdMiddleware>();
```

### Propagar Correlation ID en HTTP clients salientes

```csharp
public class CorrelationIdHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdHandler(IHttpContextAccessor accessor)
        => _httpContextAccessor = accessor;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var correlationId = _httpContextAccessor.HttpContext?
            .Items["CorrelationId"]?.ToString()
            ?? Activity.Current?.GetBaggageItem("correlation.id")
            ?? Guid.NewGuid().ToString("N");

        request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        return base.SendAsync(request, ct);
    }
}

// Registro
builder.Services.AddTransient<CorrelationIdHandler>();
builder.Services
    .AddHttpClient("billing")
    .AddHttpMessageHandler<CorrelationIdHandler>();
```

---

## OpenTelemetry Collector — pipeline centralizado

El OTel Collector es un proceso intermediario que recibe telemetría de múltiples apps, la procesa (filtra, enriquece, agrega) y la reenvía a múltiples backends. Desacopla las apps de los backends de observabilidad.

```
Apps (.NET, Node, Python)
    ↓  OTLP gRPC/HTTP
OTel Collector
    ├── Processors: filter, batch, attributes, sampling
    └── Exporters:
        ├── Grafana Tempo (traces)
        ├── Prometheus (metrics)
        └── Grafana Loki (logs)
```

### Setup

```yaml
# docker-compose.yml
services:
  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    restart: unless-stopped
    ports:
      - "4317:4317"   # OTLP gRPC
      - "4318:4318"   # OTLP HTTP
      - "8888:8888"   # métricas del propio collector
    volumes:
      - ./infra/otel-collector.yaml:/etc/otelcol-contrib/config.yaml:ro
```

```yaml
# otel-collector.yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  batch:
    timeout: 10s
    send_batch_size: 1024

  # Enriquecer todos los spans con el environment
  attributes:
    actions:
      - key: deployment.environment
        value: production
        action: insert

  # Filtrar health check spans
  filter/drop_health:
    traces:
      span:
        - 'attributes["http.target"] == "/health/live"'
        - 'attributes["http.target"] == "/health/ready"'

  # Sampling probabilístico — 10% en producción
  probabilistic_sampler:
    sampling_percentage: 10

exporters:
  otlp/tempo:
    endpoint: tempo:4317
    tls:
      insecure: true

  prometheus:
    endpoint: "0.0.0.0:8889"

  loki:
    endpoint: http://loki:3100/loki/api/v1/push

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch, attributes, filter/drop_health]
      exporters: [otlp/tempo]

    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
```

---

## Grafana Tempo — distributed tracing

Tempo es el backend de traces de Grafana. Compatible con OTLP, Zipkin y Jaeger.

### Setup

```yaml
services:
  tempo:
    image: grafana/tempo:latest
    restart: unless-stopped
    command: -config.file=/etc/tempo/tempo.yaml
    ports:
      - "3200:3200"   # HTTP query API (Grafana data source)
      - "4317:4317"   # OTLP gRPC (recibir traces)
    volumes:
      - ./infra/tempo.yaml:/etc/tempo/tempo.yaml:ro
      - tempo-data:/var/tempo
```

```yaml
# tempo.yaml
server:
  http_listen_port: 3200

distributor:
  receivers:
    otlp:
      protocols:
        grpc:
          endpoint: 0.0.0.0:4317

storage:
  trace:
    backend: local
    local:
      path: /var/tempo/traces
    wal:
      path: /var/tempo/wal

# Configurar retención
compactor:
  compaction:
    block_retention: 336h   # 14 días
```

### TraceQL — lenguaje de consulta de Tempo

```traceql
# Buscar traces con error
{ status = error }

# Buscar por servicio y duración
{ resource.service.name = "taskflow-api" && duration > 500ms }

# Buscar por span name y tenant
{ name = "ProcessOrder" && span.order.tenant_id = "acme" }

# Spans con excepciones
{ span.exception.type != nil }

# Traces que incluyen una llamada a la BD lenta
{ span.db.system = "postgresql" && duration > 200ms }
```

### Correlación Tempo ↔ Loki

Con el data source configurado (ver sección Grafana), al abrir un trace en Tempo aparece el botón **"Logs for this span"** que lleva directamente a Loki filtrando por `TraceId` y la ventana de tiempo del span. Requiere que los logs incluyan el campo `TraceId`.

```csharp
// Asegurarse de que los logs incluyan el TraceId de OTel
builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.Enrich.WithProperty("TraceId",
        Activity.Current?.TraceId.ToString() ?? "");
    // ... resto de la configuración
});

// O mejor: usar el enricher automático de OTel + Serilog
// dotnet add package Serilog.Enrichers.OpenTelemetry
cfg.Enrich.WithOpenTelemetryTraceId()
   .Enrich.WithOpenTelemetrySpanId();
```

---

## Sampling — no guardar todo

En producción con alto tráfico, guardar el 100% de los traces es costoso. El sampling controla qué fracción se guarda.

### Head-based sampling — decidir al inicio

```csharp
// Sampling en la app — 10% de todos los requests
tracing.SetSampler(new TraceIdRatioBasedSampler(0.1));

// Siempre samplear errores + 1% del resto
tracing.SetSampler(new ParentBasedSampler(
    new TraceIdRatioBasedSampler(0.01)));
```

### Tail-based sampling — decidir después de ver el trace

El tail-based sampling requiere el OTel Collector. Permite guardar el 100% de los traces con error y solo el 1% de los exitosos:

```yaml
# otel-collector.yaml — tail sampling
processors:
  tail_sampling:
    decision_wait: 10s
    policies:
      # Siempre guardar traces con error
      - name: errors-policy
        type: status_code
        status_code: { status_codes: [ERROR] }

      # Siempre guardar traces lentos (> 1s)
      - name: latency-policy
        type: latency
        latency: { threshold_ms: 1000 }

      # 1% del resto
      - name: probabilistic-policy
        type: probabilistic
        probabilistic: { sampling_percentage: 1 }
```

---

## Propagación entre servicios heterogéneos

### Desde .NET a Node.js

El W3C TraceContext es estándar — OpenTelemetry lo implementa en todos los lenguajes. Si el servicio .NET llama a un servicio Node.js:

```javascript
// Node.js — recibir el trace context automáticamente
// (con @opentelemetry/sdk-node configurado)
const { NodeSDK } = require('@opentelemetry/sdk-node');
const { OTLPTraceExporter } = require('@opentelemetry/exporter-trace-otlp-grpc');

const sdk = new NodeSDK({
  traceExporter: new OTLPTraceExporter({ url: 'http://localhost:4317' }),
});
sdk.start();
// El SDK lee el header traceparent automáticamente
```

### A través de message queues

Cuando la propagación pasa por una queue (RabbitMQ, Kafka, Azure Service Bus), el trace context debe propagarse en los headers del mensaje:

```csharp
// Productor — inyectar el trace context en el mensaje
public async Task PublishAsync<T>(T message, CancellationToken ct)
    where T : IEvent
{
    var properties = new BasicProperties();

    // Propagar el trace context como headers del mensaje
    var propagator = Propagators.DefaultTextMapPropagator;
    propagator.Inject(
        new PropagationContext(Activity.Current?.Context ?? default, Baggage.Current),
        properties.Headers ??= new Dictionary<string, object>(),
        (headers, key, value) => headers[key] = value);

    await _channel.BasicPublishAsync(
        exchange: _exchange,
        routingKey: typeof(T).Name,
        body: JsonSerializer.SerializeToUtf8Bytes(message),
        basicProperties: properties,
        cancellationToken: ct);
}

// Consumidor — extraer el trace context del mensaje
public async Task ConsumeAsync(BasicDeliverEventArgs args, CancellationToken ct)
{
    var propagator = Propagators.DefaultTextMapPropagator;
    var parentContext = propagator.Extract(
        default,
        args.BasicProperties.Headers,
        (headers, key) =>
        {
            if (headers.TryGetValue(key, out var value) && value is byte[] bytes)
                return new[] { Encoding.UTF8.GetString(bytes) };
            return Array.Empty<string>();
        });

    using var activity = Telemetry.ActivitySource
        .StartActivity("ProcessMessage",
            ActivityKind.Consumer,
            parentContext.ActivityContext);

    activity?.SetTag("messaging.system", "rabbitmq");
    activity?.SetTag("messaging.operation", "process");

    // ... procesar el mensaje
}
```

---

## Debugging con tracing — localizar cuellos de botella

### Workflow de diagnóstico

```
1. Síntoma detectado (alerta de latencia p95 > 500ms en Grafana)
2. Abrir Grafana → Explore → Tempo
3. Consulta: { resource.service.name = "taskflow-api" && duration > 500ms }
4. Seleccionar uno de los traces lentos
5. Expandir el flamegraph — identificar el span más largo
6. Clic en "Logs for this span" → ver los logs exactos de esa ejecución
7. Identificar: ¿es una query lenta? ¿un servicio externo? ¿retry que tardó?
8. Si es query: el span tiene db.statement con el SQL exacto → analizar en PostgreSQL
```

### Instrumentación de Dapper con spans

```csharp
public async Task<IEnumerable<T>> QueryAsync<T>(
    string sql, object? param = null, CancellationToken ct = default)
{
    using var activity = Telemetry.ActivitySource
        .StartActivity("db.query", ActivityKind.Client);

    activity?.SetTag("db.system", "postgresql");
    activity?.SetTag("db.statement", sql);
    activity?.SetTag("db.operation", "SELECT");

    var sw = Stopwatch.StartNew();
    try
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var result = await conn.QueryAsync<T>(
            new CommandDefinition(sql, param, cancellationToken: ct));

        activity?.SetTag("db.rows_returned", result.Count());
        return result;
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);
        throw;
    }
    finally
    {
        activity?.SetTag("db.duration_ms", sw.ElapsedMilliseconds);
    }
}
```

---

## Checklist de observabilidad completa

### ¿Está el sistema observable?

- [ ] Cada request tiene un `TraceId` en los logs
- [ ] Cada request tiene un `CorrelationId` en los logs
- [ ] Los logs incluyen `TenantId` cuando aplica
- [ ] Los errores quedan registrados en el span con `RecordException`
- [ ] Los spans custom tienen tags de negocio relevantes (`order.id`, `tenant_id`)
- [ ] Las métricas de negocio están instrumentadas (no solo las de infraestructura)
- [ ] Los health checks cubren todas las dependencias (`/health/ready`)
- [ ] Las alertas de SLO están configuradas y probadas
- [ ] El sampling en producción no es 100% (ni 0%)
- [ ] Los traces fluyen correctamente a través de queues y HTTP clients
- [ ] En Grafana puedo ir de un log a su trace y de su trace a su métrica

---

> Fuente: *Building Microservices* (Sam Newman) — Ch.10 Distributed Tracing; *Web API Development with ASP.NET Core 8* (Quan Nguyen) — Ch.12 Observability; *Building Secure and Reliable Systems* (Google SRE Book) — Ch.3 Measuring Reliability

---

## Glosario

| Término | Definición |
|---------|-----------|
| Trace | Representación completa de una operación de extremo a extremo que agrupa todos los spans relacionados |
| Span | Unidad individual de trabajo dentro de un trace; tiene nombre, TraceId, SpanId, timestamps y etiquetas |
| TraceId | Identificador único de 128 bits que acompaña a todos los spans de una misma operación distribuida |
| Context Propagation | Mecanismo para pasar el TraceId entre procesos (HTTP, queues, jobs) sin perder la cadena del trace |
| W3C TraceContext | Estándar que define el formato del header `traceparent` para propagar el trace context entre servicios |
| OpenTelemetry | Framework de observabilidad agnóstico al proveedor para instrumentar, recopilar y exportar trazas y métricas |
| ActivitySource | Clase de .NET para crear spans custom dentro de la instrumentación de OpenTelemetry |
| Baggage | Datos clave-valor que viajan junto al trace context (ej. TenantId, CorrelationId) |
| Grafana Tempo | Backend de almacenamiento y consulta de trazas distribuidas compatible con OTLP, Jaeger y Zipkin |
| TraceQL | Lenguaje de consulta de Grafana Tempo para buscar y filtrar trazas por atributos, duración y estado |
| OTLP | OpenTelemetry Protocol; protocolo estándar para exportar trazas, métricas y logs a backends compatibles |
| OTel Collector | Proceso intermediario que recibe telemetría, la procesa y la reenvía a múltiples backends |
| Head-based sampling | Estrategia de muestreo que decide al inicio del trace si se guarda o se descarta |
| Tail-based sampling | Estrategia que decide si guardar el trace después de completarse; permite priorizar errores y traces lentos |
| Correlation ID | Identificador de negocio que une múltiples requests relacionadas más allá del scope técnico de un trace |

---

*Rogelio Arriaga Gonzalez*
