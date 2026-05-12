# 04 · Observabilidad — logs, métricas y alertas

> Fuente: *Learning Modern Linux* Ch.8 (Logging, Monitoring, Advanced Observability) + *AWS Certified Solutions Architect – Associate Guide* Ch.15 (CloudTrail, CloudWatch)

## Problema que resuelve

Sin observabilidad, los fallos en producción se detectan por quejas de usuarios. Con logs estructurados, métricas y alertas, el equipo detecta anomalías antes de que se conviertan en incidentes y puede diagnosticar causas raíz en minutos.

## Los tres pilares

| Pilar | Pregunta que responde | Herramienta |
|-------|----------------------|-------------|
| Logs | ¿qué pasó y cuándo? | Seq, CloudWatch Logs, ELK |
| Métricas | ¿cuánto y con qué frecuencia? | CloudWatch Metrics, Prometheus, Grafana |
| Trazas | ¿dónde tardó una request? | AWS X-Ray, OpenTelemetry |

## Logs estructurados en .NET

Los logs en texto plano son difíciles de buscar. Los logs en JSON permiten filtrar por campo.

```csharp
// Program.cs — Serilog con output JSON
builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .Enrich.WithProperty("Application", "GTM.Suite.Api")
       .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
       .WriteTo.Console(new CompactJsonFormatter())
       .WriteTo.Seq(ctx.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341");
});
```

```json
// ejemplo de log estructurado en JSON
{
  "@t": "2026-05-11T14:23:01.123Z",
  "@l": "Information",
  "@mt": "HTTP {Method} {Path} responded {StatusCode} in {Elapsed}ms",
  "Method": "POST",
  "Path": "/api/v1/quality/approvals",
  "StatusCode": 200,
  "Elapsed": 43,
  "Application": "GTM.Suite.Api",
  "TenantId": "mxgrogu",
  "UserId": "usr-123"
}
```

## Seq — servidor de logs local/self-hosted

Seq es un servidor de logs estructurados con UI para buscar y filtrar. Útil en staging o instancias EC2.

```yaml
# docker-compose.yml
services:
  seq:
    image: datalust/seq:latest
    ports:
      - "5341:5341"   # ingesta
      - "8080:80"     # UI
    environment:
      ACCEPT_EULA: Y
    volumes:
      - seq-data:/data

volumes:
  seq-data:
```

Acceder a `http://localhost:8080` para buscar logs con el lenguaje de consulta de Seq:

```
# logs de error de las últimas 2 horas
@Level = 'Error' and @Timestamp > Now() - 2h

# requests lentas
Elapsed > 1000

# por tenant
TenantId = 'mxgrogu' and @Level = 'Warning'
```

## CloudWatch — AWS

### Logs

La app en ECS Fargate envía logs a CloudWatch automáticamente con el log driver `awslogs`:

```json
// task definition ECS
"logConfiguration": {
  "logDriver": "awslogs",
  "options": {
    "awslogs-group": "/ecs/gtm-suite-api",
    "awslogs-region": "us-east-1",
    "awslogs-stream-prefix": "ecs"
  }
}
```

```bash
# buscar logs con AWS CLI
aws logs filter-log-events \
  --log-group-name /ecs/gtm-suite-api \
  --filter-pattern "ERROR" \
  --start-time $(date -d '1 hour ago' +%s000)
```

### Métricas y alarmas

```bash
# crear alarma: CPU > 80% por 5 minutos → notificar por SNS
aws cloudwatch put-metric-alarm \
  --alarm-name gtm-cpu-high \
  --metric-name CPUUtilization \
  --namespace AWS/ECS \
  --dimensions Name=ClusterName,Value=gtm-cluster Name=ServiceName,Value=gtm-suite-api \
  --statistic Average \
  --period 300 \
  --threshold 80 \
  --comparison-operator GreaterThanThreshold \
  --evaluation-periods 1 \
  --alarm-actions arn:aws:sns:us-east-1:123456789:gtm-alerts
```

### CloudTrail — auditoría de API calls

CloudTrail registra todas las llamadas a la API de AWS (quién hizo qué y cuándo):

```bash
# buscar quién borró un security group en las últimas 24h
aws cloudtrail lookup-events \
  --lookup-attributes AttributeKey=EventName,AttributeValue=DeleteSecurityGroup \
  --start-time $(date -d '24 hours ago' -u +%Y-%m-%dT%H:%M:%SZ)
```

Habilitar CloudTrail en todas las regiones desde la consola: **CloudTrail → Trails → Create trail → Apply to all regions**.

## Health checks

El back-template expone un endpoint de health que ECS y el load balancer usan para determinar si la instancia está sana:

```csharp
// Program.cs
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

```bash
# verificar manualmente
curl http://localhost:5000/health
# {"status":"Healthy"}
```

## Dashboard de Grafana con CloudWatch

Para visualizar métricas de CloudWatch en Grafana:

1. Instalar el data source de CloudWatch en Grafana
2. Configurar credenciales IAM con permisos `cloudwatch:GetMetricData`
3. Crear paneles con métricas de ECS, RDS, ALB

Métricas clave a monitorear:

| Servicio | Métrica | Umbral alerta |
|---------|---------|---------------|
| ECS | CPUUtilization | > 80% |
| ECS | MemoryUtilization | > 85% |
| RDS | DatabaseConnections | > 80% del límite |
| RDS | FreeStorageSpace | < 10 GB |
| ALB | TargetResponseTime | > 1s (p95) |
| ALB | HTTPCode_Target_5XX_Count | > 0 en 5 min |

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| logs estructurados (JSON) desde el inicio | logs en texto plano en producción |
| CloudWatch para apps en AWS (incluido gratis en ECS) | instalar Grafana+Prometheus si CloudWatch es suficiente |
| Seq en staging para buscar logs con query fácil | Seq en producción sin autenticación habilitada |
| alarmas en métricas clave antes de lanzar a producción | esperar al primer incidente para configurar alertas |

---

## SLOs y SLAs — definir el nivel de servicio
> Fuente: *Building Secure and Reliable Systems* (Google SRE Book) — Ch.3 Measuring and Reporting Reliability

```
SLA (Service Level Agreement)  = contrato legal con el cliente
SLO (Service Level Objective)  = objetivo interno de confiabilidad
SLI (Service Level Indicator)  = métrica que mide el SLO

Ejemplo:
SLA: "99.9% uptime mensual o crédito del 10%"
SLO: "99.95% de requests exitosas (< 500ms) en ventana de 30 días"
SLI: tasa de éxito = requests_ok / requests_total (medido cada 1 min en CloudWatch)
```

### Error Budget

El Error Budget es el margen de fallas permitido antes de violar el SLO.

```
SLO = 99.9% de disponibilidad mensual
Error Budget = 100% - 99.9% = 0.1% de requests pueden fallar

En un mes (30 días × 24h × 60min × 60s = 2,592,000 segundos):
Error Budget de tiempo = 2,592,000 × 0.001 = 2,592 segundos (~43 minutos)

Si el equipo gasta el error budget en el deploy:
→ No más features hasta que se recupere el budget
→ Fuerza la conversación entre velocidad y confiabilidad
```

```csharp
// Middleware para medir el SLI desde la aplicación
public sealed class SliMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Counter RequestsTotal = Metrics
        .CreateCounter("http_requests_total", "Total requests",
            new CounterConfiguration { LabelNames = ["method", "path", "status_class"] });

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
# Alarma: tasa de error > 0.1% en ventana de 5 minutos
aws cloudwatch put-metric-alarm \
  --alarm-name "gtm-slo-error-rate" \
  --alarm-description "SLO violation: error rate > 0.1%" \
  --metric-name "5xxErrorRate" \
  --namespace "AWS/ApplicationELB" \
  --dimensions Name=LoadBalancer,Value=app/gtm-alb/xxx \
  --statistic Average \
  --period 300 \
  --threshold 0.001 \
  --comparison-operator GreaterThanThreshold \
  --evaluation-periods 1 \
  --alarm-actions "arn:aws:sns:us-east-1:123456789:gtm-oncall"
```


---

*Rogelio Arriaga Gonzalez*
