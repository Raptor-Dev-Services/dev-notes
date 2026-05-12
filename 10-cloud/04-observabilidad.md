# 04 · Observabilidad — logs, métricas y alertas

> Fuente: *Learning Modern Linux* Ch.8 (Logging, Monitoring, Advanced Observability); *AWS Certified Solutions Architect – Associate Guide* Ch.15 (CloudTrail, CloudWatch); *Building Secure and Reliable Systems* (Google SRE Book) — Ch.3 Reliability

---

## Los tres pilares

| Pilar | Pregunta que responde | Herramientas principales |
|-------|----------------------|--------------------------|
| **Logs** | ¿qué pasó y cuándo? | Seq, Grafana Loki, CloudWatch Logs |
| **Métricas** | ¿cuánto y con qué frecuencia? | Prometheus, Grafana, CloudWatch Metrics |
| **Trazas** | ¿dónde tardó una request? | OpenTelemetry, Grafana Tempo, Jaeger |

La observabilidad moderna requiere los tres pilares conectados entre sí. Un log sin trace correlacionado es útil pero incompleto. Un trace sin métricas de contexto no dice si el problema es sistémico o puntual. El objetivo es saltar de logs → traces → métricas en una sola UI sin cambiar de pestaña.

---

## Logs estructurados con Serilog

Los logs en texto plano son inbuscables. Los logs en JSON permiten filtrar, agregar y correlacionar por campo.

### Configuración completa de Serilog

```csharp
// Program.cs
builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "TaskFlow.Api")
        .Enrich.WithProperty("Version",
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0")
        // Sink 1: consola en dev (texto legible), en prod (JSON compacto)
        .WriteTo.Console(
            ctx.HostingEnvironment.IsDevelopment()
                ? new ExpressionTemplate(
                    "[{@t:HH:mm:ss} {@l:u3}] {@m}\n{@x}")
                : new CompactJsonFormatter())
        // Sink 2: Seq para búsqueda interactiva
        .WriteTo.Seq(
            ctx.Configuration["Seq:Uri"] ?? "http://localhost:5341",
            apiKey: ctx.Configuration["Seq:ApiKey"])
        // Sink 3: Loki para stack Grafana (opcional, ver sección Loki)
        .WriteTo.GrafanaLoki(
            ctx.Configuration["Loki:Uri"] ?? "http://localhost:3100",
            labels: new[] { new LokiLabel { Key = "app", Value = "taskflow-api" } });
});
```

### Enriquecimiento contextual

```csharp
// Middleware que agrega propiedades al scope del request
public class RequestEnrichmentMiddleware
{
    private readonly RequestDelegate _next;
    public RequestEnrichmentMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("ClientIp", context.Connection.RemoteIpAddress))
        using (LogContext.PushProperty("UserId",
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value))
        {
            await _next(context);
        }
    }
}
```

### Niveles y cuándo usarlos

| Nivel | Cuándo | Ejemplo |
|-------|--------|---------|
| `Verbose` | Trazas muy finas, solo en dev | Cada paso de un parser |
| `Debug` | Diagnóstico en dev/staging | Parámetros de una query |
| `Information` | Flujo normal de negocio | "Customer {Id} created" |
| `Warning` | Situaciones inesperadas, sistema sigue funcionando | Retry intento 2 de 3 |
| `Error` | Fallo que afecta una operación | Exception en handler |
| `Fatal` | Sistema no puede continuar | BD no responde al arrancar |

### Evitar filtrar datos sensibles en logs

```csharp
// Destructuring con filtrado
Log.Logger = new LoggerConfiguration()
    .Destructure.ByTransforming<CreateCustomerRequest>(r => new
    {
        r.Name,
        Email = "***", // nunca loggear emails completos
    })
    .CreateLogger();

// Uso
_logger.LogInformation("Processing {@Request}", request);
// Produce: Processing {"Name": "Acme", "Email": "***"}
```

---

## Seq — servidor de logs self-hosted

Seq es un servidor de logs estructurados con UI poderosa para buscar, filtrar y crear dashboards. Ideal para staging y desarrollo. Con autenticación habilitada también sirve para producción small/medium.

### Setup con Docker Compose

```yaml
# docker-compose.yml
services:
  seq:
    image: datalust/seq:latest
    restart: unless-stopped
    ports:
      - "5341:5341"   # ingesta HTTP/HTTPS
      - "8080:80"     # UI web
    environment:
      ACCEPT_EULA: "Y"
      SEQ_FIRSTRUN_ADMINPASSWORDHASH: "" # dejar vacío = sin auth en dev
    volumes:
      - seq-data:/data

volumes:
  seq-data:
```

### Lenguaje de consulta de Seq

```
# Errores de las últimas 2 horas
@Level = 'Error' and @Timestamp > Now() - 2h

# Requests lentas (campo Elapsed en ms)
Elapsed > 1000

# Por tenant y nivel
TenantId = 'acme' and @Level = 'Warning'

# Todos los requests 5xx con su trace ID
StatusCode >= 500 and RequestPath like '/api/%'

# Excepciones de un tipo específico
@Exception like '%NullReferenceException%'

# Requests del usuario X en la última hora
UserId = 'usr-abc-123' and @Timestamp > Now() - 1h

# Percentil de latencia (requiere Seq 2024+)
select percentile(Elapsed, 95) as p95 from stream
  where RequestPath like '/api/%'
  group by time(5m)
```

### Señales (alerts) en Seq

Seq permite crear alertas que disparan un webhook cuando una condición se cumple:

1. **Dashboard → Signals → New Signal**
2. Condición: `@Level = 'Error' and @Timestamp > Now() - 5m`
3. Threshold: `count(*) > 10`
4. Notificación: webhook a Slack o PagerDuty

---

## Prometheus — métricas

Prometheus recoge métricas haciendo scraping (pull) a endpoints HTTP `/metrics` expuestos por las apps. Almacena series de tiempo y permite consultas con PromQL.

### Tipos de métricas

| Tipo | Cuándo usarlo | Ejemplo |
|------|--------------|---------|
| **Counter** | Siempre crece. Nunca baja. | Requests totales, errores totales |
| **Gauge** | Puede subir y bajar. | Conexiones activas, tamaño de cola |
| **Histogram** | Distribución + percentiles. | Latencia de requests (p50, p95, p99) |
| **Summary** | Similar a Histogram, percentiles pre-calculados | Menos flexible que Histogram |

### Exponer métricas en ASP.NET Core

```csharp
// dotnet add package prometheus-net.AspNetCore
builder.Services.AddSingleton<AppMetrics>();
app.UseHttpMetrics(); // instrumentación automática de HTTP
app.MapMetrics();     // expone /metrics para Prometheus
```

```csharp
// Métricas de negocio custom
public class AppMetrics
{
    // Counter: número total de pedidos creados
    public readonly Counter OrdersCreated = Metrics
        .CreateCounter("taskflow_orders_created_total",
            "Total orders created",
            new CounterConfiguration { LabelNames = ["tenant", "status"] });

    // Gauge: tamaño actual de la cola de emails
    public readonly Gauge EmailQueueSize = Metrics
        .CreateGauge("taskflow_email_queue_size",
            "Current email queue size",
            new GaugeConfiguration { LabelNames = ["tenant"] });

    // Histogram: latencia de queries a BD
    public readonly Histogram DbQueryDuration = Metrics
        .CreateHistogram("taskflow_db_query_duration_seconds",
            "Database query duration",
            new HistogramConfiguration
            {
                LabelNames = ["operation"],
                Buckets = [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5]
            });
}

// Uso en el código
public class OrderService
{
    private readonly AppMetrics _metrics;

    public async Task<Order> CreateOrderAsync(CreateOrderRequest req, CancellationToken ct)
    {
        var order = await _repo.CreateAsync(req, ct);

        _metrics.OrdersCreated
            .WithLabels(req.TenantId, "created")
            .Inc();

        return order;
    }
}
```

### PromQL — consultas básicas

```promql
# Tasa de requests por segundo (últimos 5 min)
rate(http_requests_total[5m])

# Tasa de errores 5xx
rate(http_requests_total{code=~"5.."}[5m])

# Latencia p95 de la API
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Requests por tenant (top 5)
topk(5, sum by (tenant) (rate(taskflow_orders_created_total[5m])))

# Porcentaje de errores
sum(rate(http_requests_total{code=~"5.."}[5m]))
  /
sum(rate(http_requests_total[5m]))

# Alertar si p99 > 1 segundo
histogram_quantile(0.99, rate(http_request_duration_seconds_bucket[5m])) > 1
```

### Setup de Prometheus con Docker Compose

```yaml
# docker-compose.yml
services:
  prometheus:
    image: prom/prometheus:latest
    restart: unless-stopped
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus-data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.retention.time=30d'

volumes:
  prometheus-data:
```

```yaml
# prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: "taskflow-api"
    static_configs:
      - targets: ["api:8080"]
    metrics_path: /metrics

  - job_name: "postgres"
    static_configs:
      - targets: ["postgres-exporter:9187"]
```

---

## Grafana — dashboards y alertas

Grafana es la UI central de observabilidad. Conecta múltiples data sources (Prometheus, Loki, Tempo, CloudWatch) y permite crear dashboards y alertas unificadas.

### Setup con Docker Compose

```yaml
# docker-compose.yml
services:
  grafana:
    image: grafana/grafana:latest
    restart: unless-stopped
    ports:
      - "3000:3000"
    environment:
      GF_SECURITY_ADMIN_PASSWORD: admin
      GF_USERS_ALLOW_SIGN_UP: "false"
    volumes:
      - grafana-data:/var/lib/grafana
      - ./grafana/provisioning:/etc/grafana/provisioning:ro

volumes:
  grafana-data:
```

### Provisioning automático de data sources

```yaml
# grafana/provisioning/datasources/datasources.yml
apiVersion: 1
datasources:
  - name: Prometheus
    type: prometheus
    url: http://prometheus:9090
    isDefault: true

  - name: Loki
    type: loki
    url: http://loki:3100

  - name: Tempo
    type: tempo
    url: http://tempo:3200
    jsonData:
      tracesToLogsV2:
        datasourceUid: loki
        spanStartTimeShift: '-1h'
        spanEndTimeShift: '1h'
        tags: [{ key: 'service.name', value: 'app' }]
      logsToTraces:
        datasourceUid: tempo
```

### Métricas clave a monitorear

| Servicio | Métrica | Umbral de alerta |
|---------|---------|-----------------|
| API | `http_request_duration_seconds p95` | > 500ms |
| API | Tasa de errores 5xx | > 1% |
| API | Requests activos | > capacidad nominal |
| BD | `pg_connections` | > 80% del límite |
| BD | Tiempo de queries lentas | > 1s |
| BD | `pg_database_size_bytes` | Crecimiento anormal |
| Host | CPU | > 80% sostenido |
| Host | Memoria libre | < 10% |
| Host | Disco | < 15% libre |

### Alertas en Grafana

```yaml
# Alerta: tasa de error > 1% durante 5 minutos
# Configurar desde: Grafana → Alerting → Alert Rules → New Alert Rule
#
# Query A (Prometheus):
# sum(rate(http_requests_total{code=~"5.."}[5m]))
#   /
# sum(rate(http_requests_total[5m]))
#
# Condition: IS ABOVE 0.01
# Evaluate every: 1m
# For: 5m
#
# Notification policy: apuntar a contacto de Slack/email/PagerDuty
```

---

## Grafana Loki — agregación de logs

Loki es el sistema de agregación de logs de Grafana. A diferencia de Elasticsearch, solo indexa las etiquetas (labels), no el contenido completo del log. Es mucho más barato de operar y se integra nativamente con Grafana.

### Setup

```yaml
# docker-compose.yml
services:
  loki:
    image: grafana/loki:latest
    restart: unless-stopped
    ports:
      - "3100:3100"
    command: -config.file=/etc/loki/local-config.yaml
    volumes:
      - loki-data:/loki

volumes:
  loki-data:
```

### Integración con Serilog

```bash
dotnet add package Serilog.Sinks.Grafana.Loki
```

```csharp
.WriteTo.GrafanaLoki(
    "http://localhost:3100",
    labels: new[]
    {
        new LokiLabel { Key = "app", Value = "taskflow-api" },
        new LokiLabel { Key = "env", Value = builder.Environment.EnvironmentName },
    },
    propertiesAsLabels: new[] { "TenantId" }
)
```

### LogQL — lenguaje de consulta de Loki

```logql
# Todos los logs de la app
{app="taskflow-api"}

# Solo errores
{app="taskflow-api"} |= "Error"

# Filtrar por tenant (si TenantId es label)
{app="taskflow-api", TenantId="acme"}

# Regex — requests que tardaron más de 1000ms
{app="taskflow-api"} | json | Elapsed > 1000

# Contar errores por minuto
sum(count_over_time({app="taskflow-api"} |= "Error" [1m]))

# Correlacionar con trace — buscar traceId específico
{app="taskflow-api"} |= "4bf92f3577b34da6"
```

### Ventaja de correlación Loki ↔ Tempo

Con el data source de Grafana configurado correctamente, al ver un log en Loki que tiene el campo `TraceId`, aparece un botón directo para abrir ese trace en Tempo. Sin copiar/pegar IDs.

---

## Stack LGTM local con Docker Compose

```yaml
# docker-compose.observability.yml
# Levantar con: docker compose -f docker-compose.yml -f docker-compose.observability.yml up

services:
  # Logs
  loki:
    image: grafana/loki:latest
    restart: unless-stopped
    ports: ["3100:3100"]
    command: -config.file=/etc/loki/local-config.yaml
    volumes:
      - loki-data:/loki

  # Métricas
  prometheus:
    image: prom/prometheus:latest
    restart: unless-stopped
    ports: ["9090:9090"]
    volumes:
      - ./infra/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus-data:/prometheus

  # Traces (ver 09-tracing-distribuido.md)
  tempo:
    image: grafana/tempo:latest
    restart: unless-stopped
    command: -config.file=/etc/tempo/tempo.yaml
    ports:
      - "3200:3200"   # HTTP query
      - "4317:4317"   # OTLP gRPC
      - "4318:4318"   # OTLP HTTP
    volumes:
      - ./infra/tempo.yaml:/etc/tempo/tempo.yaml:ro
      - tempo-data:/var/tempo

  # UI unificada
  grafana:
    image: grafana/grafana:latest
    restart: unless-stopped
    ports: ["3000:3000"]
    environment:
      GF_SECURITY_ADMIN_PASSWORD: admin
      GF_USERS_ALLOW_SIGN_UP: "false"
      GF_FEATURE_TOGGLES_ENABLE: traceToMetrics
    volumes:
      - grafana-data:/var/lib/grafana
      - ./infra/grafana/provisioning:/etc/grafana/provisioning:ro

  # Logs locales con UI simple
  seq:
    image: datalust/seq:latest
    restart: unless-stopped
    ports:
      - "5341:5341"
      - "8081:80"
    environment:
      ACCEPT_EULA: "Y"
    volumes:
      - seq-data:/data

volumes:
  loki-data:
  prometheus-data:
  tempo-data:
  grafana-data:
  seq-data:
```

---

## Uptime Kuma — status page y monitoreo sintético

Uptime Kuma es un monitor de disponibilidad self-hosted con UI moderna. Reemplaza a servicios como StatusCake o Pingdom para equipos que prefieren control total.

### Setup

```yaml
services:
  uptime-kuma:
    image: louislam/uptime-kuma:latest
    restart: unless-stopped
    ports:
      - "3001:3001"
    volumes:
      - uptime-kuma-data:/app/data

volumes:
  uptime-kuma-data:
```

UI disponible en `http://localhost:3001`. Crear cuenta admin en el primer acceso.

### Tipos de monitores

| Tipo | Cuándo usarlo |
|------|--------------|
| HTTP(s) | Verificar que `/health` responde 200 |
| TCP Port | Verificar que el puerto de la BD está abierto |
| DNS | Verificar que el dominio resuelve correctamente |
| Ping | Latencia básica a un host |
| Keyword | HTTP + verificar que el body contiene una palabra clave |
| JSON Query | HTTP + evaluar el resultado de un JSONPath |
| gRPC Keyword | Para servicios gRPC |

### Integración con Serilog para alertas

Uptime Kuma envía notificaciones por: **Slack, Teams, Telegram, Discord, Email, PagerDuty, OpsGenie, webhook** y más de 90 integraciones.

Para conectar con el webhook de la API:

1. Settings → Notifications → Add Notification → Webhook
2. URL: `https://api.taskflow.com/internal/alerts/uptime`
3. El body llega como JSON con `monitor`, `heartbeat`, `msg`

### Status page

Uptime Kuma puede servir una **página de estado pública** (tipo `status.taskflow.com`) mostrando el uptime histórico de cada servicio. Se configura en: Status Pages → New Status Page.

---

## CloudWatch — AWS

### Logs

La app en ECS Fargate envía logs a CloudWatch automáticamente con el log driver `awslogs`:

```json
{
  "logConfiguration": {
    "logDriver": "awslogs",
    "options": {
      "awslogs-group": "/ecs/taskflow-api",
      "awslogs-region": "us-east-1",
      "awslogs-stream-prefix": "ecs"
    }
  }
}
```

```bash
# Buscar logs con AWS CLI
aws logs filter-log-events \
  --log-group-name /ecs/taskflow-api \
  --filter-pattern "ERROR" \
  --start-time $(date -d '1 hour ago' +%s000)
```

### Métricas y alarmas

```bash
# Alarma: CPU > 80% por 5 minutos → notificar por SNS
aws cloudwatch put-metric-alarm \
  --alarm-name taskflow-cpu-high \
  --metric-name CPUUtilization \
  --namespace AWS/ECS \
  --dimensions Name=ClusterName,Value=taskflow-cluster \
              Name=ServiceName,Value=taskflow-api \
  --statistic Average \
  --period 300 \
  --threshold 80 \
  --comparison-operator GreaterThanThreshold \
  --evaluation-periods 1 \
  --alarm-actions arn:aws:sns:us-east-1:123456789:taskflow-alerts
```

### CloudTrail — auditoría de API calls

CloudTrail registra todas las llamadas a la API de AWS (quién hizo qué y cuándo):

```bash
# Buscar quién borró un security group en las últimas 24h
aws cloudtrail lookup-events \
  --lookup-attributes AttributeKey=EventName,AttributeValue=DeleteSecurityGroup \
  --start-time $(date -d '24 hours ago' -u +%Y-%m-%dT%H:%M:%SZ)
```

### Dashboard Grafana + CloudWatch

1. Instalar el data source de CloudWatch en Grafana
2. Configurar credenciales IAM con permisos `cloudwatch:GetMetricData`
3. Crear paneles con métricas de ECS, RDS, ALB

| Servicio | Métrica | Umbral alerta |
|---------|---------|---------------|
| ECS | CPUUtilization | > 80% |
| ECS | MemoryUtilization | > 85% |
| RDS | DatabaseConnections | > 80% del límite |
| RDS | FreeStorageSpace | < 10 GB |
| ALB | TargetResponseTime | > 1s (p95) |
| ALB | HTTPCode_Target_5XX_Count | > 0 en 5 min |

---

## Alertmanager — routing de alertas

Alertmanager recibe alertas de Prometheus y las enruta a los canales correctos con lógica de agrupación, deduplicación e inhibición.

### Setup

```yaml
services:
  alertmanager:
    image: prom/alertmanager:latest
    restart: unless-stopped
    ports: ["9093:9093"]
    volumes:
      - ./infra/alertmanager.yml:/etc/alertmanager/alertmanager.yml:ro
```

```yaml
# alertmanager.yml
global:
  slack_api_url: 'https://hooks.slack.com/services/...'

route:
  group_by: ['alertname', 'tenant']
  group_wait: 30s
  group_interval: 5m
  repeat_interval: 4h
  receiver: 'slack-default'
  routes:
    - match:
        severity: critical
      receiver: 'pagerduty-oncall'
    - match:
        tenant: acme
      receiver: 'slack-acme-team'

receivers:
  - name: 'slack-default'
    slack_configs:
      - channel: '#alerts'
        text: '{{ .CommonAnnotations.summary }}'

  - name: 'pagerduty-oncall'
    pagerduty_configs:
      - service_key: '<pagerduty-key>'

inhibit_rules:
  # Si hay alerta crítica del servicio, silenciar las warnings del mismo servicio
  - source_match:
      severity: 'critical'
    target_match:
      severity: 'warning'
    equal: ['alertname', 'service']
```

### Reglas de alerta en Prometheus

```yaml
# prometheus-rules.yml
groups:
  - name: taskflow.rules
    rules:
      - alert: HighErrorRate
        expr: |
          sum(rate(http_requests_total{code=~"5.."}[5m]))
            /
          sum(rate(http_requests_total[5m])) > 0.01
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "Tasa de error > 1%"
          description: "{{ $value | humanizePercentage }} de errores en los últimos 5 min"

      - alert: HighLatency
        expr: |
          histogram_quantile(0.95,
            rate(http_request_duration_seconds_bucket[5m])) > 0.5
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Latencia p95 > 500ms"
```

---

## Health checks

```csharp
// Program.cs
builder.Services
    .AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("Default")!,
        name: "postgres",
        tags: new[] { "db", "ready" })
    .AddRedis(
        redisConnectionString: builder.Configuration.GetConnectionString("Redis")!,
        name: "redis",
        tags: new[] { "cache", "ready" })
    .AddUrlGroup(
        new Uri("https://api.stripe.com/v1/"),
        name: "stripe",
        tags: new[] { "external" });

// Endpoints separados para liveness y readiness
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false  // solo confirma que la app responde
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = h => h.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse  // JSON detallado
});
```

```bash
# Verificar manualmente
curl http://localhost:5000/health/live
# {"status":"Healthy"}

curl http://localhost:5000/health/ready
# {
#   "status": "Healthy",
#   "checks": [
#     { "name": "postgres", "status": "Healthy", "duration": "00:00:00.012" },
#     { "name": "redis", "status": "Healthy", "duration": "00:00:00.003" }
#   ]
# }
```

---

## SLOs y error budget

```
SLA (Service Level Agreement)  = contrato legal con el cliente
SLO (Service Level Objective)  = objetivo interno de confiabilidad
SLI (Service Level Indicator)  = métrica que mide el SLO

Ejemplo:
SLA: "99.9% uptime mensual o crédito del 10%"
SLO: "99.95% de requests exitosas (< 500ms) en ventana de 30 días"
SLI: tasa de éxito = requests_ok / requests_total
```

### Cálculo del Error Budget

```
SLO = 99.9% de disponibilidad mensual
Error Budget = 100% - 99.9% = 0.1% de requests pueden fallar

En un mes (30 días × 86,400 segundos = 2,592,000 segundos):
Tiempo disponible para fallar = 2,592,000 × 0.001 = 2,592 segundos (~43 minutos)

Si el equipo gasta el error budget en un deploy:
→ No más features hasta que se recupere el budget
→ Fuerza la conversación entre velocidad y confiabilidad
```

### SLI medido desde la aplicación

```csharp
public sealed class SliMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly Counter RequestsTotal = Metrics
        .CreateCounter("http_requests_total",
            "Total requests",
            new CounterConfiguration
            {
                LabelNames = ["method", "path", "status_class"]
            });

    public SliMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        var statusClass = context.Response.StatusCode switch
        {
            >= 200 and < 300 => "2xx",
            >= 400 and < 500 => "4xx",
            >= 500           => "5xx",
            _                => "other"
        };

        RequestsTotal
            .WithLabels(context.Request.Method, context.Request.Path, statusClass)
            .Inc();
    }
}
```

### SLO como alarma en CloudWatch

```bash
aws cloudwatch put-metric-alarm \
  --alarm-name "taskflow-slo-error-rate" \
  --alarm-description "SLO violation: error rate > 0.1%" \
  --metric-name "5xxErrorRate" \
  --namespace "AWS/ApplicationELB" \
  --dimensions Name=LoadBalancer,Value=app/taskflow-alb/xxx \
  --statistic Average \
  --period 300 \
  --threshold 0.001 \
  --comparison-operator GreaterThanThreshold \
  --evaluation-periods 1 \
  --alarm-actions "arn:aws:sns:us-east-1:123456789:taskflow-oncall"
```

---

## Cuándo usar cada herramienta

| Herramienta | Usar cuando | No usar cuando |
|-------------|-------------|---------------|
| **Seq** | Dev/staging, queries ad-hoc fáciles, equipo pequeño | Producción de alto volumen sin licencia Pro |
| **Loki** | Stack Grafana, costo bajo en prod, logs ya van a Grafana | Necesitas búsqueda full-text rápida sobre contenido |
| **Prometheus** | Métricas de sistema y negocio, alertas, SLOs | Métricas de largo plazo (>90d, usa Thanos/VictoriaMetrics) |
| **Grafana** | UI unificada de todos los data sources | Simplicity > todo (usa solo CloudWatch si estás en AWS) |
| **Tempo** | Distributed tracing, correlación con Loki | Solo una API sin microservicios (sobra) |
| **CloudWatch** | Stack AWS, ya pagas por el servicio | Multi-cloud o costos impredecibles en alto volumen |
| **Uptime Kuma** | Status page self-hosted, alertas de disponibilidad | SLAs contractuales que requieren auditoría de terceros |

---

*Rogelio Arriaga Gonzalez*
