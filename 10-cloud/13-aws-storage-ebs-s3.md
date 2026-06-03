# 13 · AWS Storage — EBS, S3 y Snapshots

## Problema que resuelve

Una aplicación de producción necesita almacenamiento para datos persistentes del sistema operativo (EBS), archivos subidos por usuarios o tenants (S3), y backups automáticos del sistema de archivos (Snapshots). Sin entender las clases de almacenamiento de S3 y sus políticas de ciclo de vida se gasta de más. Sin entender EBS se elige el tipo incorrecto y se paga por IOPS que no se necesitan. Este documento cubre el almacenamiento de bloques (EBS) y de objetos (S3) en profundidad.

---

## EBS — Elastic Block Store

EBS son discos virtuales que se adjuntan a instancias EC2. Son el "disco duro" de la instancia. Los datos persisten aunque la instancia se detenga o reinicie (a diferencia del instance store).

```
EC2 ──────── EBS Volume
  ↑               ↑
Efímero si usa  Persistente (sobrevive
instance store  detener/iniciar la instancia)

Analogía:
  Instance store = RAM del servidor (se pierde)
  EBS            = disco duro (persiste)
```

### Tipos de volumen EBS

| Tipo | Nombre | IOPS base | Throughput | Latencia | Caso de uso |
|------|--------|-----------|-----------|----------|-------------|
| **gp3** | General Purpose SSD | 3.000 provisionados | 125-1000 MB/s | 1-digit ms | Default sensato para casi todo |
| **gp2** | General Purpose SSD (legacy) | 3 IOPS/GB (max 16.000) | 250 MB/s | 1-digit ms | Evitar, usar gp3 |
| **io2** | Provisioned IOPS SSD | Hasta 64.000 | 1.000 MB/s | sub-ms | Bases de datos OLTP críticas |
| **io2 Block Express** | Ultra performance | Hasta 256.000 | 4.000 MB/s | sub-ms | Oracle RAC, SQL Server máximo rendimiento |
| **st1** | Throughput Optimized HDD | 500 IOPS max | 500 MB/s | decenas de ms | Big data, data warehouses, logs |
| **sc1** | Cold HDD | 250 IOPS max | 250 MB/s | decenas de ms | Archivos raramente accedidos, backup |

**Recomendación para el stack .NET:**

```
OS de EC2 (volumen raíz):    gp3, 20-50 GB
Base de datos en RDS:        gp3 o io2 (RDS gestiona automáticamente)
Logs y archivos temporales:  gp3 suficiente
```

### gp3 vs gp2 — por qué preferir gp3

```
gp2: 3 IOPS por GB. Un volumen de 100 GB tiene 300 IOPS base.
     Para tener 3.000 IOPS necesitas 1 TB de disco (aunque no lo uses).
     Costo: $0.10/GB-mes

gp3: 3.000 IOPS base SIEMPRE, independiente del tamaño.
     IOPS y throughput se pueden provisionar por separado.
     Costo: $0.08/GB-mes + IOPS extras opcionales
     
✓ gp3 es más barato Y más eficiente que gp2.
```

---

## EBS Snapshots

Los Snapshots son backups incrementales del volumen EBS guardados en S3 (gestionado por AWS, no tu bucket).

```
Snapshot 1 (completo): 50 GB
Snapshot 2 (incremental): solo los bloques que cambiaron desde snapshot 1
Snapshot 3 (incremental): solo los bloques que cambiaron desde snapshot 2

Restaurar desde cualquier snapshot = estado completo del volumen en ese momento.
Eliminar snapshot intermedio = AWS recalcula los datos para que otros snapshots sigan siendo válidos.
```

### Operaciones con snapshots

```bash
# Crear snapshot manual
aws ec2 create-snapshot \
  --volume-id vol-12345678 \
  --description "backup-prod-$(date +%Y%m%d-%H%M)"

# Listar snapshots propios
aws ec2 describe-snapshots \
  --owner-ids self \
  --query 'Snapshots[*].[SnapshotId,VolumeSize,StartTime,Description]' \
  --output table

# Copiar snapshot a otra region (para DR o migracion)
aws ec2 copy-snapshot \
  --source-region us-east-1 \
  --source-snapshot-id snap-12345678 \
  --destination-region us-west-2 \
  --description "DR-copy"

# Crear volumen desde snapshot (en otra AZ)
aws ec2 create-volume \
  --snapshot-id snap-12345678 \
  --availability-zone us-east-1b \
  --volume-type gp3

# Esto permite mover datos entre AZs:
# Snapshot del volumen en 1a → nuevo volumen en 1b
```

### DLM — Data Lifecycle Manager

Automatiza la creación y retención de snapshots sin necesitar scripts manuales.

```bash
# Crear politica DLM — snapshot diario a las 3AM, retener 7 dias
aws dlm create-lifecycle-policy \
  --description "daily-backup-prod-volumes" \
  --state ENABLED \
  --execution-role-arn arn:aws:iam::123456789:role/AWSDataLifecycleManagerDefaultRole \
  --policy-details '{
    "PolicyType": "EBS_SNAPSHOT_MANAGEMENT",
    "ResourceTypes": ["VOLUME"],
    "TargetTags": [{"Key": "Environment", "Value": "production"}],
    "Schedules": [{
      "Name": "Daily",
      "CreateRule": {
        "Interval": 24,
        "IntervalUnit": "HOURS",
        "Times": ["03:00"]
      },
      "RetainRule": {
        "Count": 7
      },
      "CopyTags": true
    }]
  }'
```

**Estrategia de retención recomendada para producción:**

```
Diario:   retener 7 snapshots (última semana)
Semanal:  retener 4 snapshots (último mes)
Mensual:  retener 12 snapshots (último año)
```

---

## S3 — Simple Storage Service

S3 es almacenamiento de objetos serverless. No hay servidores que gestionar, no hay disco que provisionar. Paga solo por lo que usas.

**Características fundamentales:**
- Durabilidad: 99.999999999% (11 nueves). Datos replicados en múltiples AZs.
- Disponibilidad: 99.99% para S3 Standard
- Tamaño máximo por objeto: 5 TB
- Tamaño máximo en un PUT simple: 5 GB (usar multipart para más de 100 MB)
- Sin límite de almacenamiento total

### Nomenclatura de buckets

```
Reglas obligatorias:
  - Globalmente únicos (en toda AWS, no solo tu cuenta)
  - Solo minúsculas, números y guiones
  - Sin puntos si usas SSL/TLS con tu propio dominio (conflictos de certificado)
  - Entre 3 y 63 caracteres
  - No puede ser una IP

✓ Nombres correctos:
  gtm-suite-prod-files
  raptor-backups-2026
  misaas-tenant-assets

❌ Nombres incorrectos:
  GTM-Suite-Prod    (mayúsculas)
  my.bucket.name    (puntos — evitar para SSL)
  192.168.1.1       (formato IP)
```

---

## Clases de almacenamiento S3

| Clase | Acceso | Disponibilidad | Min días | Costo GB/mes | Caso de uso |
|-------|--------|---------------|----------|-------------|-------------|
| **Standard** | Inmediato, frecuente | 99.99% | — | $0.023 | Assets activos, uploads de usuarios |
| **Standard-IA** | Inmediato, infrecuente | 99.9% | 30 días | $0.0125 | Backups semanales, logs viejos |
| **One Zone-IA** | Inmediato, infrecuente | 99.5% | 30 días | $0.01 | Datos secundarios que se pueden regenerar |
| **Glacier Instant Retrieval** | Milisegundos | 99.9% | 90 días | $0.004 | Archivos médicos, imágenes de compliance |
| **Glacier Flexible Retrieval** | Minutos a horas | 99.9% | 90 días | $0.0036 | Auditoría anual, compliance largo plazo |
| **Glacier Deep Archive** | 12-48 horas | 99.9% | 180 días | $0.00099 | Retención regulatoria 7+ años |
| **Intelligent-Tiering** | Variable | 99.9% | — | $0.023 + fee monitoreo | Datos con acceso impredecible |

**Costo de recuperación (retrieval):** las clases IA y Glacier cobran por GB recuperado. El costo total = almacenamiento + recuperaciones. Para datos que se acceden más de una vez al mes, Standard puede ser más económico que Standard-IA.

---

## Lifecycle Policies

Reglas automáticas que transicionan objetos entre clases o los eliminan según su edad.

```bash
# Ejemplo: lifecycle para archivos subidos por tenants
cat > lifecycle.json << 'EOF'
{
  "Rules": [
    {
      "ID": "tenant-file-lifecycle",
      "Status": "Enabled",
      "Filter": {
        "Prefix": "uploads/"
      },
      "Transitions": [
        {
          "Days": 30,
          "StorageClass": "STANDARD_IA"
        },
        {
          "Days": 90,
          "StorageClass": "GLACIER_IR"
        },
        {
          "Days": 365,
          "StorageClass": "DEEP_ARCHIVE"
        }
      ],
      "Expiration": {
        "Days": 2555
      }
    },
    {
      "ID": "delete-incomplete-multipart",
      "Status": "Enabled",
      "Filter": {"Prefix": ""},
      "AbortIncompleteMultipartUpload": {
        "DaysAfterInitiation": 7
      }
    }
  ]
}
EOF

aws s3api put-bucket-lifecycle-configuration \
  --bucket gtm-suite-prod-files \
  --lifecycle-configuration file://lifecycle.json
```

**Restricciones de transición:**

```
Standard → Standard-IA: mínimo 30 días
Standard-IA → Glacier IR: mínimo 90 días desde creación (o 60 desde IA)
No se puede ir de Glacier a Standard-IA directamente
One Zone-IA no puede transicionar a Glacier Instant Retrieval
```

---

## Versioning

Cuando se habilita el versioning, S3 conserva todas las versiones de un objeto. Una eliminación crea un "delete marker" en lugar de borrar el objeto.

```bash
# Habilitar versioning
aws s3api put-bucket-versioning \
  --bucket gtm-suite-prod-files \
  --versioning-configuration Status=Enabled

# Listar versiones de un objeto
aws s3api list-object-versions \
  --bucket gtm-suite-prod-files \
  --prefix uploads/tenant-abc/contract.pdf

# Restaurar version anterior (copiar la version deseada sobre la actual)
aws s3api copy-object \
  --bucket gtm-suite-prod-files \
  --copy-source gtm-suite-prod-files/uploads/tenant-abc/contract.pdf?versionId=abc123 \
  --key uploads/tenant-abc/contract.pdf

# Eliminar una version especifica (permanente)
aws s3api delete-object \
  --bucket gtm-suite-prod-files \
  --key uploads/tenant-abc/contract.pdf \
  --version-id abc123
```

**Impacto en costos:** versioning aumenta el costo de almacenamiento porque se conservan todas las versiones. Usar lifecycle policies para expirar versiones antiguas.

```json
{
  "Rules": [{
    "ID": "expire-old-versions",
    "Status": "Enabled",
    "NoncurrentVersionExpiration": {
      "NoncurrentDays": 90
    }
  }]
}
```

---

## Cifrado en S3

### SSE-S3 (Server-Side Encryption con claves gestionadas por S3)

```bash
# Habilitado por defecto desde 2023 en todos los buckets nuevos
# Para forzar en objetos:
aws s3 cp archivo.pdf s3://gtm-bucket/ \
  --server-side-encryption AES256
```

- AWS gestiona las claves completamente
- Sin auditoría de quién usó la clave cuándo
- Sin costo adicional

### SSE-KMS (Server-Side Encryption con AWS KMS)

```bash
aws s3 cp archivo.pdf s3://gtm-bucket/ \
  --server-side-encryption aws:kms \
  --ssekms-key-id arn:aws:kms:us-east-1:123456789:key/abc-123

# La clave puede ser AWS-managed (aws/s3) o Customer Managed (CMK)
```

- Auditoría completa en CloudTrail (quién, cuándo, qué clave)
- Control de acceso granular con key policies
- Costo: ~$0.03 por 10.000 llamadas a KMS

### SSE-C (Server-Side Encryption con clave del cliente)

```bash
# El cliente provee la clave en cada request — AWS no la almacena
aws s3 cp archivo.pdf s3://gtm-bucket/ \
  --sse-customer-algorithm AES256 \
  --sse-customer-key <base64-encoded-key>
```

- El cliente es responsable de gestionar y rotar la clave
- Si pierdes la clave, pierdes el acceso a los datos
- AWS no puede recuperarla

**Cifrado en tránsito:** siempre HTTPS/TLS. Se puede forzar con una bucket policy:

```json
{
  "Statement": [{
    "Effect": "Deny",
    "Principal": "*",
    "Action": "s3:*",
    "Resource": ["arn:aws:s3:::gtm-bucket", "arn:aws:s3:::gtm-bucket/*"],
    "Condition": {
      "Bool": {"aws:SecureTransport": "false"}
    }
  }]
}
```

---

## Control de acceso en S3

### Bucket Policies (a nivel de bucket)

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "AllowTenantFileAccess",
      "Effect": "Allow",
      "Principal": {
        "AWS": "arn:aws:iam::123456789:role/BackTemplateInstanceRole"
      },
      "Action": ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"],
      "Resource": "arn:aws:s3:::gtm-suite-prod-files/*"
    },
    {
      "Sid": "DenyPublicAccess",
      "Effect": "Deny",
      "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::gtm-suite-prod-files/*",
      "Condition": {
        "StringNotEquals": {
          "aws:SourceVpc": "vpc-87654321"
        }
      }
    }
  ]
}
```

### Block Public Access

```bash
# SIEMPRE activado en buckets de produccion con datos privados
aws s3api put-public-access-block \
  --bucket gtm-suite-prod-files \
  --public-access-block-configuration \
    BlockPublicAcls=true,\
    IgnorePublicAcls=true,\
    BlockPublicPolicy=true,\
    RestrictPublicBuckets=true
```

**Jerarquía de control de acceso (de mayor a menor prioridad):**

```
1. Block Public Access (prevalece sobre todo si está activado)
2. Deny explícito en cualquier policy
3. Allow explícito en IAM policy + Bucket policy
4. ACLs (legacy — no usar en cuentas nuevas)
```

---

## Presigned URLs

Permiten acceso temporal a objetos privados sin exponer credenciales ni hacer el objeto público.

```bash
# Generar URL de descarga válida por 1 hora (3600 segundos)
aws s3 presign s3://gtm-suite-prod-files/uploads/tenant-abc/report.pdf \
  --expires-in 3600

# Resultado: URL temporal firmada que incluye credenciales en los query params
# Cualquiera con esta URL puede descargar el archivo durante 1 hora
# Después de 1 hora el acceso expira automaticamente
```

**En el back-template (.NET):**

```csharp
// Generar presigned URL para descarga directa desde el frontend
public async Task<string> GenerarUrlDescarga(string key, TimeSpan vigencia)
{
    var request = new GetPreSignedUrlRequest
    {
        BucketName = _config.BucketName,
        Key = key,
        Expires = DateTime.UtcNow.Add(vigencia)
    };
    return _s3Client.GetPreSignedURL(request);
}

// El frontend descarga directamente desde S3, sin pasar por el backend
// → ahorra ancho de banda del servidor
// → S3 maneja la descarga paralelamente
```

---

## Multipart Upload

Para archivos grandes (recomendado para +100 MB, obligatorio para +5 GB).

```bash
# Iniciar multipart upload
aws s3api create-multipart-upload \
  --bucket gtm-suite-prod-files \
  --key uploads/tenant-abc/video-grande.mp4

# Subir cada parte (minimo 5 MB por parte, excepto la ultima)
aws s3api upload-part \
  --bucket gtm-suite-prod-files \
  --key uploads/tenant-abc/video-grande.mp4 \
  --part-number 1 \
  --upload-id abc123 \
  --body part1.bin

# Completar el upload
aws s3api complete-multipart-upload \
  --bucket gtm-suite-prod-files \
  --key uploads/tenant-abc/video-grande.mp4 \
  --upload-id abc123 \
  --multipart-upload '{
    "Parts": [
      {"PartNumber": 1, "ETag": "etag1"},
      {"PartNumber": 2, "ETag": "etag2"}
    ]
  }'
```

**Importante:** los multipart uploads incompletos generan costo. Usar la regla `AbortIncompleteMultipartUpload` en el lifecycle (ver ejemplo anterior).

---

## Replicación

### Cross-Region Replication (CRR)

Replica objetos automáticamente a un bucket en otra región.

```bash
aws s3api put-bucket-replication \
  --bucket gtm-suite-prod-files \
  --replication-configuration '{
    "Role": "arn:aws:iam::123456789:role/S3ReplicationRole",
    "Rules": [{
      "Status": "Enabled",
      "Destination": {
        "Bucket": "arn:aws:s3:::gtm-suite-dr-files",
        "StorageClass": "STANDARD_IA"
      }
    }]
  }'
```

**Casos de uso CRR:**
- Disaster recovery (región principal cae, datos disponibles en otra)
- Latencia: dar acceso a usuarios en otra región

### Same-Region Replication (SRR)

Réplica en la misma región pero en un bucket diferente. Casos de uso:
- Ambiente de producción → bucket de staging (con mismos datos)
- Cumplimiento: copia de seguridad inmutable separada

---

## S3 Event Notifications

Disparan acciones automáticas cuando se sube, crea o elimina un objeto.

```bash
aws s3api put-bucket-notification-configuration \
  --bucket gtm-suite-prod-files \
  --notification-configuration '{
    "LambdaFunctionConfigurations": [
      {
        "LambdaFunctionArn": "arn:aws:lambda:us-east-1:123456789:function:ProcessUploadedFile",
        "Events": ["s3:ObjectCreated:*"],
        "Filter": {
          "Key": {
            "FilterRules": [{
              "Name": "prefix",
              "Value": "uploads/"
            }]
          }
        }
      }
    ]
  }'
```

**Destinos disponibles:** Lambda, SQS, SNS. No se puede enviar directo a EventBridge (usar CloudTrail para eso).

---

## Relación con back-template

El back-template usa S3 para archivos subidos por tenants. La arquitectura recomendada:

```
Usuario → POST /api/files/upload-url (back-template)
            ↓
  back-template genera presigned URL de PUT (válida 5 min)
  con key que incluye el tenant_id: uploads/{tenantId}/{uuid}.pdf
            ↓
  Frontend sube el archivo DIRECTAMENTE a S3 usando la presigned URL
            ↓
  S3 dispara evento → Lambda → registra el archivo en la DB de la app
```

```csharp
// En el handler del back-template
public class GenerateUploadUrlHandler : IRequestHandler<GenerateUploadUrlRequest, IResponse>
{
    public async Task<IResponse> Handle(GenerateUploadUrlRequest request, CancellationToken ct)
    {
        var key = $"uploads/{_tenantContext.TenantId}/{Guid.NewGuid()}{Path.GetExtension(request.FileName)}";
        
        var presignedUrl = _s3Service.GeneratePresignedPutUrl(
            bucket: _config.BucketName,
            key: key,
            expiry: TimeSpan.FromMinutes(5),
            contentType: request.ContentType
        );
        
        return new GenerateUploadUrlSuccess(new UploadUrlDto(presignedUrl, key));
    }
}
```

---

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| gp3 como tipo de EBS por defecto | gp2 (legacy, más caro por IOPS equivalentes) |
| io2 solo para OLTP con IOPS >16.000 requeridos | io2 para cargas de trabajo generales (costo excesivo) |
| DLM para automatizar snapshots | Scripts manuales de snapshots con cron |
| Presigned URLs para descargas de usuarios | Hacer proxy de archivos grandes por el backend |
| Lifecycle policies para mover a Glacier archivos viejos | Dejar todo en Standard indefinidamente |
| Block Public Access activado siempre | Buckets públicos para datos privados |
| SSE-KMS cuando necesitas auditoría de acceso a datos | SSE-KMS para datos no sensibles (costo de KMS innecesario) |
| Multipart upload para archivos +100 MB | PutObject simple para archivos grandes |
| VPC Endpoint Gateway para acceso a S3 desde subredes privadas | NAT Gateway para todo el tráfico a S3 |

---

## Glosario

| Término | Definición |
|---------|-----------|
| EBS | Elastic Block Store. Almacenamiento de bloques persistente para EC2. |
| gp3 | General Purpose SSD v3. Tipo de EBS recomendado. 3.000 IOPS base independiente del tamaño. |
| io2 | Provisioned IOPS SSD. Para bases de datos que requieren IOPS garantizados y baja latencia. |
| Snapshot | Backup incremental de un volumen EBS. Solo guarda los bloques que cambiaron. |
| DLM | Data Lifecycle Manager. Servicio AWS para automatizar snapshots con políticas de retención. |
| S3 | Simple Storage Service. Almacenamiento de objetos serverless, durable y escalable. |
| Bucket | Contenedor de objetos en S3. Globalmente único en AWS. |
| Objeto | Archivo almacenado en S3 con su metadato y clave (path). |
| Standard-IA | S3 Standard Infrequent Access. Para datos que se acceden menos de una vez al mes. |
| Glacier | Familia de clases de S3 para archivado a largo plazo a bajo costo. |
| Lifecycle Policy | Regla que transiciona objetos entre clases de almacenamiento según su antigüedad. |
| Versioning | Conservar todas las versiones de un objeto. Protege contra eliminaciones accidentales. |
| SSE-S3 | Cifrado en servidor gestionado por AWS. Sin costo adicional. |
| SSE-KMS | Cifrado en servidor usando AWS Key Management Service. Con auditoría y control de claves. |
| Presigned URL | URL temporal firmada que da acceso a un objeto privado de S3. |
| Multipart Upload | Mecanismo para subir archivos grandes en partes paralelas. Obligatorio para +5 GB. |
| CRR | Cross-Region Replication. Réplica automática a un bucket en otra región. |
| Block Public Access | Configuración de seguridad que bloquea todo acceso público, prevaleciendo sobre policies. |

> Fuente: *AWS Certified Solutions Architect – Associate Guide* (Gabriel Ramirez, Packt): Ch.6 S3, Ch.7 Encryption y Storage Security

---

*Rogelio Arriaga Gonzalez*
