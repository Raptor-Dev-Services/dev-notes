# 16 · AWS CloudWatch — Observabilidad

## Problema que resuelve

Sin observabilidad, un problema de producción se detecta cuando el usuario se queja. Con CloudWatch se puede detectar que el CPU lleva 20 minutos al 95% antes de que el servidor colapse, ver qué query está tardando 10 segundos, rastrear cuántos errores 500 se producen por minuto, y recibir una alerta en Slack antes de que el cliente note algo. Este documento cubre métricas, logs, alarmas y dashboards en CloudWatch para el stack .NET en AWS.

---

## Conceptos fundamentales

### Namespaces

Los namespaces agrupan métricas por servicio o aplicación. Evitan colisiones entre métricas con el mismo nombre de servicios diferentes.

```
AWS/EC2             → métricas de instancias EC2
AWS/RDS             → métricas de RDS
AWS/ApplicationELB  → métricas del ALB
AWS/ECS             → métricas de ECS
CWAgent             → métricas del CloudWatch Agent (memoria, disco)
GTMSuite/Backend    → métricas personalizadas de la aplicación .NET
```

### Dimensiones

Las dimensiones identifican de qué instancia/recurso específico proviene una métrica.

```
Namespace: AWS/EC2
Métrica: CPUUtilization
Dimensión: InstanceId = i-1234567890abcdef0
→ CPU de la instancia específica i-1234567890abcdef0

Namespace: AWS/RDS
Métrica: DatabaseConnections
Dimensión: DBInstanceIdentifier = gtm-suite-prod
→ Conexiones a la instancia RDS específica
```

### Estadísticas disponibles

| Estadística | Descripción | Caso de uso |
|-------------|-------------|-------------|
| Average | Promedio en el período | CPU promedio del período |
| Sum | Suma total | Total de bytes transferidos |
| Maximum | Valor máximo en el período | Pico de CPU |
| Minimum | Valor mínimo en el período | |
| SampleCount | Número de muestras | |
| p50, p90, p99 | Percentiles | Latencia de API (p99 = peor 1%) |

### Retención de métricas

| Resolución | Retención |
|-----------|-----------|
| 1 segundo (high-resolution) | 3 horas |
| 1 minuto | 15 días |
| 5 minutos | 63 días |
| 1 hora | 455 días (~15 meses) |

---

## Métricas EC2 por defecto

EC2 envía estas métricas automáticamente (sin agente):

| Métrica | Descripción | Umbral de alerta |
|---------|-------------|-----------------|
| CPUUtilization | CPU en % | > 80% por 5 min |
| NetworkIn | Bytes recibidos | Contexto dependiente |
| NetworkOut | Bytes enviados | Contexto dependiente |
| DiskReadOps | Operaciones de lectura de disco | |
| DiskWriteOps | Operaciones de escritura de disco | |
| StatusCheckFailed | Fallo de health check de instancia | > 0 |

**Lo que EC2 NO envía por defecto (requiere CloudWatch Agent):**

```
❌ Memoria RAM utilizada (MemoryUtilization)
❌ Espacio en disco utilizado (DiskSpaceUtilization)
❌ Logs de la aplicación

✓ Con CloudWatch Agent:
  MemoryUtilization, MemoryUsed, MemoryAvailable
  DiskSpaceUtilization, DiskSpaceUsed, DiskSpaceAvailable
  Cualquier archivo de log (/var/log/app/*.log, /var/log/syslog)
```

---

## CloudWatch Agent — instalación y configuración

### Instalación en Amazon Linux 2023

```bash
# Instalar el agente
yum install -y amazon-cloudwatch-agent

# Crear configuracion del agente
cat > /opt/aws/amazon-cloudwatch-agent/etc/amazon-cloudwatch-agent.json << 'EOF'
{
  "agent": {
    "metrics_collection_interval": 60,
    "run_as_user": "cwagent"
  },
  "metrics": {
    "namespace": "CWAgent",
    "metrics_collected": {
      "mem": {
        "measurement": ["mem_used_percent", "mem_available_percent"]
      },
      "disk": {
        "measurement": ["disk_used_percent", "disk_free"],
        "resources": ["/", "/var"]
      },
      "cpu": {
        "measurement": ["cpu_usage_idle", "cpu_usage_user", "cpu_usage_system"],
        "totalcpu": true
      }
    },
    "append_dimensions": {
      "AutoScalingGroupName": "${aws:AutoScalingGroupName}",
      "InstanceId": "${aws:InstanceId}"
    }
  },
  "logs": {
    "logs_collected": {
      "files": {
        "collect_list": [
          {
            "file_path": "/var/log/gtm-suite/app.log",
            "log_group_name": "/gtm-suite/prod/application",
            "log_stream_name": "{instance_id}",
            "timestamp_format": "%Y-%m-%dT%H:%M:%S",
            "multi_line_start_pattern": "^[0-9]{4}-[0-9]{2}-[0-9]{2}"
          },
          {
            "file_path": "/var/log/gtm-suite/error.log",
            "log_group_name": "/gtm-suite/prod/errors",
            "log_stream_name": "{instance_id}",
            "timestamp_format": "%Y-%m-%dT%H:%M:%S"
          }
        ]
      }
    }
  }
}
EOF

# Iniciar el agente con la configuracion
/opt/aws/amazon-cloudwatch-agent/bin/amazon-cloudwatch-agent-ctl \
  -a fetch-config \
  -m ec2 \
  -c file:/opt/aws/amazon-cloudwatch-agent/etc/amazon-cloudwatch-agent.json \
  -s

# Verificar que el agente esta corriendo
systemctl status amazon-cloudwatch-agent
```

---

## CloudWatch Logs

### Conceptos

```
Log Group  → contenedor lógico de logs. Ej: /gtm-suite/prod/application
Log Stream → fuente individual dentro del grupo. Ej: i-1234567890abcdef0
Log Event  → una línea/entrada de log con timestamp y mensaje

Jerarquía:
  /gtm-suite/prod/application (Log Group)
  ├── i-1234567890abcdef0 (Log Stream — instancia 1)
  └── i-abcdef1234567890 (Log Stream — instancia 2)
```

### Crear Log Group con retención

```bash
# Crear log group con retención de 30 días
aws logs create-log-group \
  --log-group-name /gtm-suite/prod/application

aws logs put-retention-policy \
  --log-group-name /gtm-suite/prod/application \
  --retention-in-days 30

# Otros valores válidos: 1, 3, 5, 7, 14, 30, 60, 90, 120, 150, 180, 365, ...
```

### CloudWatch Logs Insights — queries

```bash
# Buscar errores en los últimos 15 minutos
aws logs start-query \
  --log-group-name /gtm-suite/prod/application \
  --start-time $(date -d '15 minutes ago' +%s) \
  --end-time $(date +%s) \
  --query-string 'fields @timestamp, @message
    | filter @message like /ERROR|Exception/
    | sort @timestamp desc
    | limit 100'

# Contar errores por tipo en la última hora
aws logs start-query \
  --log-group-name /gtm-suite/prod/application \
  --start-time $(date -d '1 hour ago' +%s) \
  --end-time $(date +%s) \
  --query-string 'fields @message
    | filter @message like /Exception/
    | parse @message "Exception: *\n" as exceptionType
    | stats count(*) as errorCount by exceptionType
    | sort errorCount desc'
```

**Queries útiles para el back-template:**

```
# Latencia promedio de endpoints (si el app loguea el tiempo de respuesta)
fields @timestamp, endpoint, durationMs
| filter ispresent(durationMs)
| stats avg(durationMs) as avgLatency, pct(durationMs, 99) as p99 by endpoint
| sort avgLatency desc

# Errores por tenant (si el app loguea el tenantId)
fields @timestamp, tenantId, @message
| filter @message like /ERROR/
| stats count(*) as errors by tenantId
| sort errors desc
```

### Exportar logs a S3

```bash
aws logs create-export-task \
  --log-group-name /gtm-suite/prod/application \
  --from $(date -d '30 days ago' +%s000) \
  --to $(date +%s000) \
  --destination gtm-suite-prod-logs \
  --destination-prefix "cloudwatch-exports/application/$(date +%Y/%m)"
```

---

## Métricas personalizadas desde la aplicación .NET

```csharp
// Publicar métricas personalizadas desde el back-template
public class MetricsService
{
    private readonly IAmazonCloudWatch _cloudWatch;
    private readonly string _environment;

    public async Task PublicarMetricaTenantAsync(string tenantId, string metricName, double value)
    {
        await _cloudWatch.PutMetricDataAsync(new PutMetricDataRequest
        {
            Namespace = "GTMSuite/Backend",
            MetricData = new List<MetricDatum>
            {
                new MetricDatum
                {
                    MetricName = metricName,
                    Value = value,
                    Unit = StandardUnit.Count,
                    Timestamp = DateTime.UtcNow,
                    Dimensions = new List<Dimension>
                    {
                        new Dimension { Name = "Environment", Value = _environment },
                        new Dimension { Name = "TenantId", Value = tenantId }
                    }
                }
            }
        });
    }
}

// Uso en un handler
public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentRequest, IResponse>
{
    public async Task<IResponse> Handle(ProcessPaymentRequest request, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _paymentService.ProcessAsync(request);
            
            await _metrics.PublicarMetricaTenantAsync(
                _tenantContext.TenantId.ToString(),
                "PaymentProcessed",
                1
            );
            
            return new ProcessPaymentSuccess(result);
        }
        catch (Exception ex)
        {
            await _metrics.PublicarMetricaTenantAsync(
                _tenantContext.TenantId.ToString(),
                "PaymentFailed",
                1
            );
            return new ProcessPaymentFailure(ex.Message);
        }
        finally
        {
            await _metrics.PublicarMetricaTenantAsync(
                _tenantContext.TenantId.ToString(),
                "PaymentLatencyMs",
                stopwatch.ElapsedMilliseconds
            );
        }
    }
}
```

---

## Alarmas de CloudWatch

Las alarmas monitorean una métrica y ejecutan acciones cuando cruza un umbral.

### Estructura de una alarma

```
Estado de la alarma:
  OK              → métrica dentro del umbral
  ALARM           → métrica supera el umbral
  INSUFFICIENT_DATA → no hay suficientes datos para evaluar

Configuración:
  Threshold          → valor límite (ej. 80 para CPU 80%)
  Period             → granularidad de evaluación (ej. 300 = 5 minutos)
  EvaluationPeriods  → cuántos períodos consecutivos deben superar el umbral
  DatapointsToAlarm  → cuántos de los períodos deben estar en ALARM
```

### Alarmas esenciales para el stack .NET

```bash
# 1. CPU del servidor de aplicacion > 80% por 5 minutos
aws cloudwatch put-metric-alarm \
  --alarm-name "gtm-prod-cpu-high" \
  --alarm-description "CPU del servidor de aplicacion supera el 80%" \
  --namespace AWS/EC2 \
  --metric-name CPUUtilization \
  --dimensions Name=AutoScalingGroupName,Value=gtm-asg \
  --statistic Average \
  --period 300 \
  --evaluation-periods 2 \
  --datapoints-to-alarm 2 \
  --threshold 80 \
  --comparison-operator GreaterThanOrEqualToThreshold \
  --alarm-actions arn:aws:sns:us-east-1:123456789:alertas-produccion \
  --ok-actions arn:aws:sns:us-east-1:123456789:alertas-produccion

# 2. Disco > 85%
aws cloudwatch put-metric-alarm \
  --alarm-name "gtm-prod-disk-high" \
  --alarm-description "Disco del servidor supera el 85%" \
  --namespace CWAgent \
  --metric-name disk_used_percent \
  --dimensions Name=InstanceId,Value=i-1234567890 Name=path,Value=/ \
  --statistic Average \
  --period 300 \
  --evaluation-periods 1 \
  --datapoints-to-alarm 1 \
  --threshold 85 \
  --comparison-operator GreaterThanOrEqualToThreshold \
  --alarm-actions arn:aws:sns:us-east-1:123456789:alertas-produccion

# 3. Errores 5xx del ALB > 5 por minuto
aws cloudwatch put-metric-alarm \
  --alarm-name "gtm-prod-5xx-errors" \
  --alarm-description "ALB devuelve mas de 5 errores 5xx por minuto" \
  --namespace AWS/ApplicationELB \
  --metric-name HTTPCode_Target_5XX_Count \
  --dimensions Name=LoadBalancer,Value=app/gtm-alb/1234567890 \
  --statistic Sum \
  --period 60 \
  --evaluation-periods 2 \
  --datapoints-to-alarm 2 \
  --threshold 5 \
  --comparison-operator GreaterThanOrEqualToThreshold \
  --treat-missing-data notBreaching \
  --alarm-actions arn:aws:sns:us-east-1:123456789:alertas-produccion

# 4. Latencia p99 del ALB > 500ms
aws cloudwatch put-metric-alarm \
  --alarm-name "gtm-prod-latency-high" \
  --alarm-description "Latencia p99 del ALB supera 500ms" \
  --namespace AWS/ApplicationELB \
  --metric-name TargetResponseTime \
  --dimensions Name=LoadBalancer,Value=app/gtm-alb/1234567890 \
  --extended-statistic p99 \
  --period 60 \
  --evaluation-periods 3 \
  --datapoints-to-alarm 2 \
  --threshold 0.5 \
  --comparison-operator GreaterThanOrEqualToThreshold \
  --alarm-actions arn:aws:sns:us-east-1:123456789:alertas-produccion

# 5. Conexiones a RDS > 80% del maximo (max_connections del parameter group)
aws cloudwatch put-metric-alarm \
  --alarm-name "gtm-prod-rds-connections" \
  --alarm-description "Conexiones a RDS superan el 80% del maximo" \
  --namespace AWS/RDS \
  --metric-name DatabaseConnections \
  --dimensions Name=DBInstanceIdentifier,Value=gtm-suite-prod \
  --statistic Maximum \
  --period 60 \
  --evaluation-periods 2 \
  --datapoints-to-alarm 2 \
  --threshold 160 \
  --comparison-operator GreaterThanOrEqualToThreshold \
  --alarm-actions arn:aws:sns:us-east-1:123456789:alertas-produccion

# 6. Memoria > 85% (requiere CloudWatch Agent)
aws cloudwatch put-metric-alarm \
  --alarm-name "gtm-prod-memory-high" \
  --alarm-description "Uso de memoria supera el 85%" \
  --namespace CWAgent \
  --metric-name mem_used_percent \
  --dimensions Name=AutoScalingGroupName,Value=gtm-asg \
  --statistic Average \
  --period 300 \
  --evaluation-periods 2 \
  --datapoints-to-alarm 2 \
  --threshold 85 \
  --comparison-operator GreaterThanOrEqualToThreshold \
  --alarm-actions arn:aws:sns:us-east-1:123456789:alertas-produccion
```

---

## SNS — notificaciones desde alarmas

Las alarmas disparan acciones. La acción más común es publicar en un topic de SNS que envía emails o llama a un webhook.

```bash
# Crear topic SNS para alertas
aws sns create-topic --name alertas-produccion

# Suscribir email
aws sns subscribe \
  --topic-arn arn:aws:sns:us-east-1:123456789:alertas-produccion \
  --protocol email \
  --notification-endpoint rogelioarriaga@outlook.es

# Suscribir webhook (para Slack o Teams)
aws sns subscribe \
  --topic-arn arn:aws:sns:us-east-1:123456789:alertas-produccion \
  --protocol https \
  --notification-endpoint https://hooks.slack.com/services/T.../B.../...
```

---

## Dashboards

```bash
# Crear dashboard operativo
aws cloudwatch put-dashboard \
  --dashboard-name "GTMSuite-Produccion" \
  --dashboard-body '{
    "widgets": [
      {
        "type": "metric",
        "x": 0, "y": 0, "width": 12, "height": 6,
        "properties": {
          "title": "CPU del Servidor",
          "metrics": [
            ["AWS/EC2", "CPUUtilization",
             "AutoScalingGroupName", "gtm-asg",
             {"stat": "Average", "period": 60}]
          ],
          "view": "timeSeries",
          "region": "us-east-1",
          "period": 300,
          "yAxis": {"left": {"min": 0, "max": 100}}
        }
      },
      {
        "type": "metric",
        "x": 12, "y": 0, "width": 12, "height": 6,
        "properties": {
          "title": "Latencia ALB (p50, p90, p99)",
          "metrics": [
            ["AWS/ApplicationELB", "TargetResponseTime",
             "LoadBalancer", "app/gtm-alb/1234567890",
             {"stat": "p50", "label": "p50"}],
            ["AWS/ApplicationELB", "TargetResponseTime",
             "LoadBalancer", "app/gtm-alb/1234567890",
             {"stat": "p90", "label": "p90"}],
            ["AWS/ApplicationELB", "TargetResponseTime",
             "LoadBalancer", "app/gtm-alb/1234567890",
             {"stat": "p99", "label": "p99"}]
          ],
          "view": "timeSeries",
          "region": "us-east-1",
          "period": 60
        }
      }
    ]
  }'
```

### Checklist de dashboard operativo mínimo

```
Panel 1: CPU de EC2/ECS (Average, máximo 5 min)
Panel 2: Memoria RAM (requiere CW Agent)
Panel 3: Latencia del ALB (p50, p90, p99)
Panel 4: Errores 4xx y 5xx del ALB
Panel 5: Conexiones activas a RDS
Panel 6: Espacio libre en disco
Panel 7: Throughput de red (NetworkIn/Out)
Panel 8: Conteo de instancias del ASG (healthy vs unhealthy)
```

---

## Metric Math — alarmas compuestas

CloudWatch Metric Math permite crear alarmas basadas en expresiones que combinan múltiples métricas.

```bash
# Alarma: tasa de error > 1% de las requests
aws cloudwatch put-metric-alarm \
  --alarm-name "gtm-prod-error-rate" \
  --alarm-description "Tasa de error supera el 1% de las requests" \
  --metrics '[
    {
      "Id": "errors5xx",
      "MetricStat": {
        "Metric": {
          "Namespace": "AWS/ApplicationELB",
          "MetricName": "HTTPCode_Target_5XX_Count",
          "Dimensions": [{"Name": "LoadBalancer", "Value": "app/gtm-alb/1234"}]
        },
        "Period": 60,
        "Stat": "Sum"
      },
      "ReturnData": false
    },
    {
      "Id": "totalRequests",
      "MetricStat": {
        "Metric": {
          "Namespace": "AWS/ApplicationELB",
          "MetricName": "RequestCount",
          "Dimensions": [{"Name": "LoadBalancer", "Value": "app/gtm-alb/1234"}]
        },
        "Period": 60,
        "Stat": "Sum"
      },
      "ReturnData": false
    },
    {
      "Id": "errorRate",
      "Expression": "errors5xx / totalRequests * 100",
      "Label": "Tasa de Error (%)",
      "ReturnData": true
    }
  ]' \
  --threshold 1 \
  --comparison-operator GreaterThanOrEqualToThreshold \
  --evaluation-periods 2 \
  --datapoints-to-alarm 2 \
  --alarm-actions arn:aws:sns:us-east-1:123456789:alertas-produccion
```

---

## Resumen de alarmas esenciales por capa

| Alarma | Métrica | Umbral | Período |
|--------|---------|--------|---------|
| CPU alto — servidor | EC2/CPUUtilization | > 80% | 5 min × 2 |
| Memoria alta | CWAgent/mem_used_percent | > 85% | 5 min × 2 |
| Disco lleno | CWAgent/disk_used_percent | > 85% | 5 min × 1 |
| Errores 5xx | ALB/HTTPCode_Target_5XX_Count | > 5 / min | 1 min × 2 |
| Latencia p99 alta | ALB/TargetResponseTime p99 | > 500 ms | 1 min × 3 |
| Conexiones DB altas | RDS/DatabaseConnections | > 80% max | 1 min × 2 |
| Instancias no sanas | ASG/GroupUnhealthyInstances | > 0 | 1 min × 1 |
| Tasa de error > 1% | Metric Math | > 1% | 1 min × 2 |

---

## Relación con back-template

El back-template integra OpenTelemetry para tracing distribuido (ver `09-tracing-distribuido.md`). Las métricas de CloudWatch complementan el tracing:

```
Tracing (OTel/Tempo)   → correlacionar requests individuales
Métricas (CloudWatch)  → ver tendencias agregadas en el tiempo
Logs (CloudWatch Logs) → detalle de errores y contexto

Integración en el back-template:
  appsettings.json → Serilog sink a CloudWatch Logs
  IMetricsService  → PutMetricData para métricas de negocio (pagos, logins, etc.)
  Health checks    → endpoint /health → ALB → ASG
```

---

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| CloudWatch Agent para métricas de memoria y disco en EC2 | Confiar solo en las métricas nativas de EC2 (sin memoria) |
| Percentiles (p90, p99) para medir latencia | Solo Average (oculta los peores casos) |
| Alarmas con EvaluationPeriods > 1 para evitar falsos positivos | Alarmar en el primer período (muy ruidoso) |
| SNS para notificaciones de alarmas a múltiples destinos | Solo email directo (no integra con Slack/Teams fácilmente) |
| Logs Insights para investigar incidentes | CloudWatch Logs como única fuente de verdad para métricas |
| Retención de logs según su valor (30 días para debug, 365 para auditoría) | Retención indefinida en CloudWatch (costo alto) |
| Dashboard operativo siempre visible en el equipo | Solo revisar CloudWatch cuando hay un incidente |

---

## Glosario

| Término | Definición |
|---------|-----------|
| CloudWatch | Servicio de observabilidad de AWS: métricas, logs, alarmas y dashboards. |
| Namespace | Agrupación lógica de métricas. Ej: `AWS/EC2`, `GTMSuite/Backend`. |
| Dimensión | Atributo que identifica la fuente de una métrica. Ej: `InstanceId`, `TenantId`. |
| Log Group | Contenedor de streams de logs con la misma política de retención. |
| Log Stream | Fuente individual dentro de un Log Group (una instancia, un contenedor). |
| Logs Insights | Lenguaje de query para analizar logs en CloudWatch. |
| CW Agent | CloudWatch Agent. Software instalado en EC2 para enviar métricas de OS y logs de app. |
| Alarma | Monitor de una métrica que ejecuta acciones al cruzar un umbral. |
| SNS | Simple Notification Service. Pub/sub para notificaciones. |
| Threshold | Valor límite que una métrica debe superar para activar la alarma. |
| EvaluationPeriods | Número de períodos consecutivos que se evalúan antes de cambiar el estado de la alarma. |
| DatapointsToAlarm | Cuántos de los EvaluationPeriods deben estar en ALARM para activar la alarma. |
| Metric Math | Funcionalidad de CloudWatch para crear métricas derivadas con expresiones matemáticas. |
| p99 | Percentil 99. El 99% de las requests son más rápidas que este valor. Representa el peor 1%. |
| DBLoad | Métrica de Performance Insights. Sesiones activas / vCPUs. > 1 indica saturación. |
| High-Resolution | Métricas con granularidad de 1 segundo (vs 1 minuto estándar). Más caro. |

> Fuente: *AWS Certified Solutions Architect – Associate Guide* (Gabriel Ramirez, Packt): Ch.21 CloudWatch Logs y Automatizacion; *AWS Certified SysOps Administrator – Associate Guide*: Ch.4 Monitoring y Reporting

---

*Rogelio Arriaga Gonzalez*
