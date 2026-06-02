# 14 · AWS RDS con PostgreSQL

## Problema que resuelve

Administrar PostgreSQL directamente en EC2 requiere gestionar backups, parches del motor, failover manual, monitoreo de rendimiento y configuración del sistema operativo. Un error en cualquier de estos puntos puede significar pérdida de datos o horas de downtime. RDS delega toda esa operación a AWS y permite concentrarse en el diseño del esquema y las queries. Este documento cubre RDS PostgreSQL desde la configuración inicial hasta las prácticas de seguridad y rendimiento.

---

## EC2 + PostgreSQL autoadministrado vs RDS

| Característica | EC2 + PostgreSQL | RDS PostgreSQL |
|----------------|-----------------|----------------|
| Instalación y configuración | Manual | AWS gestiona |
| Parches del motor | Manual (tu eliges cuándo) | AWS aplica en ventana de mantenimiento |
| Backups automáticos | Script cron + pg_dump | Incluido (1-35 días retención) |
| Point-in-Time Recovery | Requiere implementación propia | Incluido hasta el segundo |
| Failover automático | Requiere Pacemaker/Patroni | Multi-AZ: automático (~60-120 s) |
| Read Replicas | Requiere configuración de streaming replication | 1 clic |
| Monitoreo de rendimiento | Instalar pg_stat_statements + Prometheus | Performance Insights incluido |
| Costo | EC2 + EBS + tu tiempo | RDS (20-30% más caro, pero sin overhead ops) |
| Control del SO | Total | Ninguno |
| Usar `postgresql.conf` directamente | Si | No (usar Parameter Groups) |

**Cuándo usar EC2 con PostgreSQL:**
- Necesitas una versión muy reciente o una extensión que RDS no soporta
- Requieres acceso al sistema de archivos de PostgreSQL (pg_basebackup personalizado)
- Tienes experiencia profunda en administración de PostgreSQL y quieres control total

---

## Crear una instancia RDS

```bash
# Crear subnet group (debe abarcar minimo 2 AZs)
aws rds create-db-subnet-group \
  --db-subnet-group-name gtm-db-subnet-group \
  --db-subnet-group-description "Subnets privadas para RDS del back-template" \
  --subnet-ids subnet-data-1a subnet-data-1b

# Crear instance RDS PostgreSQL con configuracion de produccion
aws rds create-db-instance \
  --db-instance-identifier gtm-suite-prod \
  --db-instance-class db.t3.large \
  --engine postgres \
  --engine-version 16.3 \
  --master-username gtmadmin \
  --master-user-password "$(openssl rand -base64 32)" \
  --allocated-storage 100 \
  --max-allocated-storage 500 \
  --storage-type gp3 \
  --storage-encrypted \
  --kms-key-id arn:aws:kms:us-east-1:123456789:key/prod-rds-key \
  --db-subnet-group-name gtm-db-subnet-group \
  --vpc-security-group-ids sg-db \
  --db-name gtmdb \
  --multi-az \
  --no-publicly-accessible \
  --backup-retention-period 7 \
  --preferred-backup-window "03:00-04:00" \
  --preferred-maintenance-window "Mon:05:00-Mon:06:00" \
  --enable-performance-insights \
  --performance-insights-retention-period 7 \
  --enable-cloudwatch-logs-exports postgresql \
  --deletion-protection

# Obtener el endpoint de conexion
aws rds describe-db-instances \
  --db-instance-identifier gtm-suite-prod \
  --query 'DBInstances[0].Endpoint.Address'
```

---

## Subnet Groups — qué son y por qué importan

Un DB Subnet Group es un conjunto de subredes donde RDS puede colocar la instancia (y su réplica Multi-AZ). Debe abarcar al menos 2 AZs.

```
❌ Mal diseño:
  DB Subnet Group con una sola subnet en us-east-1a
  → Si la AZ falla, RDS no puede hacer failover a otra AZ

✓ Diseño correcto:
  DB Subnet Group con:
    - subnet-data-1a (10.0.21.0/24 en us-east-1a)
    - subnet-data-1b (10.0.22.0/24 en us-east-1b)
  → RDS puede colocar la instancia en 1a y la standby en 1b
  → Failover automático sin cambiar el connection string
```

**Importante:** las subredes del Subnet Group deben ser **privadas** (sin ruta a Internet Gateway). La base de datos nunca debe ser accesible desde Internet.

---

## Parameter Groups — configurar PostgreSQL en RDS

Los Parameter Groups son la forma de modificar los parámetros de PostgreSQL en RDS. No puedes editar `postgresql.conf` directamente porque no tienes acceso al SO.

```bash
# Crear parameter group personalizado
aws rds create-db-parameter-group \
  --db-parameter-group-name gtm-postgres16-params \
  --db-parameter-group-family postgres16 \
  --description "Parametros PostgreSQL optimizados para back-template"

# Modificar parametros clave
aws rds modify-db-parameter-group \
  --db-parameter-group-name gtm-postgres16-params \
  --parameters \
    "ParameterName=shared_buffers,ParameterValue={DBInstanceClassMemory/4},ApplyMethod=pending-reboot" \
    "ParameterName=max_connections,ParameterValue=200,ApplyMethod=pending-reboot" \
    "ParameterName=work_mem,ParameterValue=16384,ApplyMethod=immediate" \
    "ParameterName=log_min_duration_statement,ParameterValue=1000,ApplyMethod=immediate" \
    "ParameterName=log_connections,ParameterValue=1,ApplyMethod=immediate" \
    "ParameterName=log_disconnections,ParameterValue=1,ApplyMethod=immediate" \
    "ParameterName=rds.force_ssl,ParameterValue=1,ApplyMethod=immediate"

# Asociar el parameter group a la instancia
aws rds modify-db-instance \
  --db-instance-identifier gtm-suite-prod \
  --db-parameter-group-name gtm-postgres16-params \
  --apply-immediately
```

### Parámetros importantes para el stack .NET

| Parámetro | Valor recomendado | Descripción |
|-----------|------------------|-------------|
| `max_connections` | 200 | Con RDS Proxy aumenta esto significativamente |
| `shared_buffers` | `{DBInstanceClassMemory/4}` | 25% de la RAM (fórmula dinámica) |
| `work_mem` | 16-64 MB | Memoria por operación de sort/hash |
| `log_min_duration_statement` | 1000 | Loguear queries que tarden más de 1 segundo |
| `rds.force_ssl` | 1 | Forzar SSL en todas las conexiones |
| `pg_stat_statements.track` | all | Habilitar pg_stat_statements para Performance Insights |

---

## Multi-AZ — alta disponibilidad

Multi-AZ crea una réplica síncrona en una AZ diferente. Es transparente para la aplicación.

```
✓ Cómo funciona Multi-AZ:
  
  AZ us-east-1a                    AZ us-east-1b
  ┌─────────────────┐              ┌─────────────────┐
  │  RDS Primary    │──replicación │  RDS Standby    │
  │  (escribe aquí) │──síncrona──→│  (no acepta     │
  │                 │             │   conexiones)    │
  └─────────────────┘             └─────────────────┘
           ↑
  Endpoint: gtm-suite-prod.xyz.us-east-1.rds.amazonaws.com
  (CNAME que apunta al primary)

  Si falla us-east-1a:
  1. AWS detecta el fallo (segundos)
  2. Promueve el Standby a Primary
  3. Actualiza el CNAME del endpoint
  4. Tu app se reconecta (el connection string no cambia)
  5. Tiempo total: ~60-120 segundos
```

```bash
# Verificar que Multi-AZ esta habilitado
aws rds describe-db-instances \
  --db-instance-identifier gtm-suite-prod \
  --query 'DBInstances[0].MultiAZ'

# Forzar un failover manual (prueba de DR)
aws rds reboot-db-instance \
  --db-instance-identifier gtm-suite-prod \
  --force-failover
```

**Multi-AZ NO es para escalar lecturas.** La réplica Standby no acepta conexiones de lectura. Para escalar lecturas usar Read Replicas.

---

## Read Replicas — escalado de lecturas

Las Read Replicas son réplicas asíncronas que aceptan conexiones de solo lectura.

```
Primary (escribe) → replica asíncrona → Read Replica (solo lectura)
                                         Read Replica 2 (para reportes)

Replication lag: normalmente milisegundos, puede ser mayor bajo carga alta
→ Los datos pueden estar ligeramente desactualizados (eventually consistent)
→ No usar Read Replicas para lecturas que deben ser inmediatamente consistentes
   (ej. leer el registro que acabas de escribir)
```

```bash
# Crear Read Replica en la misma region
aws rds create-db-instance-read-replica \
  --db-instance-identifier gtm-suite-prod-replica \
  --source-db-instance-identifier gtm-suite-prod \
  --db-instance-class db.t3.medium \
  --availability-zone us-east-1c

# Crear Read Replica en otra region (para latencia o DR)
aws rds create-db-instance-read-replica \
  --db-instance-identifier gtm-suite-prod-replica-west \
  --source-db-instance-identifier arn:aws:rds:us-east-1:123456789:db:gtm-suite-prod \
  --db-instance-class db.t3.medium \
  --destination-region us-west-2

# Obtener endpoint de la replica (endpoint diferente al primary)
aws rds describe-db-instances \
  --db-instance-identifier gtm-suite-prod-replica \
  --query 'DBInstances[0].Endpoint.Address'
```

**Uso en el back-template — separar lecturas de escrituras:**

```csharp
// En DI configuration
services.AddScoped<IDbConnectionFactory>(sp =>
    new DbConnectionFactory(
        writeConnectionString: config["ConnectionStrings:Primary"],
        readConnectionString: config["ConnectionStrings:Replica"]
    ));

// Para queries de solo lectura (reportes, dashboards)
public class GetTenantReportHandler : IRequestHandler<GetTenantReportRequest, IResponse>
{
    public async Task<IResponse> Handle(GetTenantReportRequest request, CancellationToken ct)
    {
        // Usa la conexion de lectura (replica)
        using var conn = _factory.CreateReadConnection();
        var result = await conn.QueryAsync<ReportRow>(ExampleUsersSql.GetMonthlyReport, ...);
        return new GetTenantReportSuccess(result);
    }
}
```

---

## Backups automáticos y PITR

RDS toma backups automáticos durante la ventana de backup configurada.

```bash
# Ver ventana de backup
aws rds describe-db-instances \
  --db-instance-identifier gtm-suite-prod \
  --query 'DBInstances[0].[PreferredBackupWindow,BackupRetentionPeriod]'

# Cambiar retension a 14 dias (maximo es 35)
aws rds modify-db-instance \
  --db-instance-identifier gtm-suite-prod \
  --backup-retention-period 14 \
  --apply-immediately

# Restaurar a un punto en el tiempo (crea una NUEVA instancia)
aws rds restore-db-instance-to-point-in-time \
  --source-db-instance-identifier gtm-suite-prod \
  --target-db-instance-identifier gtm-suite-restored-20260601 \
  --restore-time "2026-06-01T03:00:00Z" \
  --db-instance-class db.t3.large \
  --db-subnet-group-name gtm-db-subnet-group
```

**PITR (Point-in-Time Recovery):**
- Granularidad: hasta el segundo exacto
- Requiere: backup automático habilitado (>0 días retención)
- Funcionamiento: combina el snapshot más reciente + transaction logs
- Resultado: una nueva instancia RDS (no modifica la original)

---

## RDS Proxy

Pool de conexiones gestionado por AWS. Especialmente importante cuando la app usa muchas conexiones cortas (APIs REST, Lambda).

```
Sin RDS Proxy:
  100 instancias ECS → 100 conexiones directas a RDS (costoso en memoria y CPU)
  PostgreSQL tiene overhead significativo por conexión

Con RDS Proxy:
  100 instancias ECS → RDS Proxy → pool de 20 conexiones → RDS
  El Proxy reutiliza conexiones del pool → menos overhead en PostgreSQL
```

```bash
# Crear RDS Proxy
aws rds create-db-proxy \
  --db-proxy-name gtm-rds-proxy \
  --engine-family POSTGRESQL \
  --auth '[{
    "AuthScheme": "SECRETS",
    "SecretArn": "arn:aws:secretsmanager:us-east-1:123456789:secret:rds/gtm-prod",
    "IAMAuth": "REQUIRED"
  }]' \
  --role-arn arn:aws:iam::123456789:role/RDSProxyRole \
  --vpc-subnet-ids subnet-data-1a subnet-data-1b \
  --vpc-security-group-ids sg-rds-proxy \
  --require-tls

# Obtener endpoint del proxy (usar este en lugar del RDS directo)
aws rds describe-db-proxies \
  --db-proxy-name gtm-rds-proxy \
  --query 'DBProxies[0].Endpoint'
```

---

## Seguridad en RDS

### Red y acceso

```bash
# Verificar que publicly-accessible sea false
aws rds describe-db-instances \
  --db-instance-identifier gtm-suite-prod \
  --query 'DBInstances[0].PubliclyAccessible'
# Debe retornar: false

# El SG de RDS solo permite puerto 5432 desde el SG de la app
aws ec2 authorize-security-group-ingress \
  --group-id sg-db \
  --protocol tcp \
  --port 5432 \
  --source-group sg-app
```

### Forzar SSL en las conexiones

```bash
# En el parameter group
aws rds modify-db-parameter-group \
  --db-parameter-group-name gtm-postgres16-params \
  --parameters "ParameterName=rds.force_ssl,ParameterValue=1,ApplyMethod=immediate"
```

**Connection string desde .NET con SSL:**

```
Host=gtm-suite-prod.xyz.us-east-1.rds.amazonaws.com;
Port=5432;
Database=gtmdb;
Username=gtmadmin;
Password=...;
SSL Mode=Require;
Trust Server Certificate=false;
SSL Certificate=/app/certs/rds-combined-ca-bundle.pem
```

### Cifrado at-rest

```bash
# Cifrado se habilita al crear (no se puede habilitar en una instancia existente)
aws rds create-db-instance ... --storage-encrypted --kms-key-id <key-arn>

# Para cifrar una instancia existente:
# 1. Crear snapshot de la instancia no cifrada
aws rds create-db-snapshot --db-instance-identifier gtm-suite-prod --db-snapshot-identifier gtm-snap-uncrypted

# 2. Copiar el snapshot con cifrado habilitado
aws rds copy-db-snapshot \
  --source-db-snapshot-identifier gtm-snap-uncrypted \
  --target-db-snapshot-identifier gtm-snap-encrypted \
  --kms-key-id arn:aws:kms:us-east-1:123456789:key/prod-rds-key

# 3. Restaurar desde el snapshot cifrado (nueva instancia)
aws rds restore-db-instance-from-db-snapshot \
  --db-instance-identifier gtm-suite-prod-encrypted \
  --db-snapshot-identifier gtm-snap-encrypted
```

---

## Performance Insights

Performance Insights muestra qué queries están consumiendo más recursos en la base de datos en tiempo real.

```bash
# Habilitar Performance Insights en instancia existente
aws rds modify-db-instance \
  --db-instance-identifier gtm-suite-prod \
  --enable-performance-insights \
  --performance-insights-retention-period 7 \
  --apply-immediately
```

**Métricas clave en Performance Insights:**

| Métrica | Descripción | Umbral de alerta |
|---------|-------------|-----------------|
| DBLoad | Número de sesiones activas / vCPUs | > 1 (saturado) |
| db.sql.tps | Transacciones por segundo | Contexto dependiente |
| db.wait_event | Qué espera están causando latencia | Lock, IO, CPU |
| db.sql.rows_examined | Filas escaneadas por query | > 1M → index faltante |

---

## Relación con back-template

```csharp
// Program.cs del back-template — configuración de Dapper con RDS
builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionString 'Default' no configurado");
    
    return new NpgsqlConnectionFactory(connectionString);
});

// Para multi-tenancy: la fabrica inyecta tenant_id en las queries
// La contraseña viene de Secrets Manager, no del appsettings
```

```
Flujo de conexion en produccion:
  back-template (ECS/EC2) → RDS Proxy → RDS Primary
  back-template (ECS/EC2) → RDS Proxy → RDS Read Replica (queries de solo lectura)
  
  Credenciales: Secrets Manager → rotation automatica cada 30 dias
```

---

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Multi-AZ para cualquier DB de producción | Single-AZ en producción (punto único de falla) |
| Read Replicas para queries de reportes y dashboards | Read Replicas para datos que deben ser consistentes inmediatamente |
| RDS Proxy cuando usas Lambda o más de 50 conexiones | RDS Proxy en entornos con pocas conexiones (costo innecesario) |
| Parameter Groups para ajustar config de PostgreSQL | Intentar editar postgresql.conf directamente |
| Deletion Protection en producción | Dejar la DB sin protection (eliminación accidental) |
| PITR para recuperar datos corruptos o eliminados accidentalmente | Snapshot manual como único mecanismo de recovery |
| gp3 para la mayoría de workloads | io2 salvo que necesites >16.000 IOPS confirmados |

---

## Glosario

| Término | Definición |
|---------|-----------|
| RDS | Relational Database Service. Base de datos relacional gestionada por AWS. |
| Parameter Group | Contenedor de parámetros de configuración del motor de base de datos en RDS. |
| DB Subnet Group | Colección de subredes donde RDS puede colocar la instancia. Requiere múltiples AZs. |
| Multi-AZ | Réplica síncrona en otra AZ con failover automático. No escala lecturas. |
| Read Replica | Réplica asíncrona para escalar lecturas. Tiene su propio endpoint. Eventualmente consistente. |
| Replication Lag | Retraso entre el Primary y la Read Replica. Normalmente milisegundos. |
| PITR | Point-in-Time Recovery. Restaurar la DB a cualquier segundo dentro del período de retención. |
| Backup Window | Ventana horaria diaria en la que RDS toma el snapshot automático. |
| RDS Proxy | Pool de conexiones gestionado por AWS entre la app y RDS. Reduce overhead. |
| Performance Insights | Herramienta de diagnóstico de RDS que muestra queries lentas y wait events en tiempo real. |
| DBLoad | Métrica de Performance Insights. Número de sesiones activas dividido entre vCPUs. |
| Deletion Protection | Flag de RDS que impide eliminar la instancia accidentalmente. |
| Publicly Accessible | Flag que determina si RDS tiene endpoint público. Siempre `false` en producción. |
| SSL Certificate | Certificado de RDS para cifrado en tránsito. Descargar desde AWS. |

> Fuente: *AWS Certified Solutions Architect – Associate Guide* (Gabriel Ramirez, Packt) — Ch.9 Storage y Relational Databases, Ch.4 Scaling Databases

---

*Rogelio Arriaga Gonzalez*
