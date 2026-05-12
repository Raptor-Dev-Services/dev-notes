# 01 · AWS — Servicios esenciales

> Fuente: *AWS Certified Solutions Architect – Associate Guide* (Packt) — Ch.1 IAM, Ch.2 Infraestructura global y S3, Ch.3 EC2, Ch.9 Storage y RDS, Ch.13 Access Control

## Problema que resuelve

Un equipo que empieza a operar en AWS necesita entender la infraestructura global, los servicios de cómputo, almacenamiento y base de datos, y el sistema de permisos antes de desplegar cualquier aplicación.

## Infraestructura global

| Concepto | Descripción |
|----------|-------------|
| Región | conjunto de zonas de disponibilidad geográficamente separadas (ej. `us-east-1`, `sa-east-1`) |
| Zona de disponibilidad (AZ) | uno o más data centers físicamente separados dentro de una región |
| Edge location | nodo de CloudFront (CDN) para cachear contenido cerca del usuario |

Regla: los recursos de alta disponibilidad se distribuyen en al menos 2 AZs de la misma región.

## EC2 — Elastic Compute Cloud

Máquinas virtuales bajo demanda. Conceptos clave:

```
# lanzar instancia con AWS CLI
aws ec2 run-instances \
  --image-id ami-0abcdef1234567890 \
  --instance-type t3.micro \
  --key-name mi-keypair \
  --security-group-ids sg-12345678 \
  --subnet-id subnet-12345678
```

| Tipo de instancia | Caso de uso |
|-------------------|-------------|
| t3.micro / t3.small | desarrollo, baja carga |
| t3.medium / t3.large | apps web de producción ligeras |
| c6i.large | cómputo intensivo (APIs con alta CPU) |
| r6i.large | memoria intensiva (caches, bases de datos) |

### Modelo de persistencia EC2

- **Instance store**: almacenamiento efímero, se pierde al detener la instancia
- **EBS (Elastic Block Store)**: volumen persistente adjunto a la instancia, sobrevive reinicios
- **AMI (Amazon Machine Image)**: snapshot del estado de la instancia para lanzar réplicas

### Auto Scaling

```
# grupo de escalado automático
Min: 2 instancias  →  siempre disponible
Desired: 2          →  capacidad deseada en estado normal
Max: 10             →  límite en pico de tráfico

Política: Scale Out cuando CPU > 70% por 5 min
          Scale In  cuando CPU < 30% por 10 min
```

## S3 — Simple Storage Service

Almacenamiento de objetos. Un bucket contiene objetos (archivos) referenciados por clave.

```bash
# crear bucket
aws s3 mb s3://mi-bucket-raptor --region us-east-1

# subir archivo
aws s3 cp archivo.txt s3://mi-bucket-raptor/

# listar
aws s3 ls s3://mi-bucket-raptor/

# sincronizar directorio
aws s3 sync ./dist s3://mi-bucket-raptor/frontend/
```

### Clases de almacenamiento S3

| Clase | Acceso | Caso de uso |
|-------|--------|-------------|
| S3 Standard | frecuente | assets, backups activos |
| S3 Standard-IA | infrecuente | backups semanales |
| S3 Glacier Instant | archivo, ms de recuperación | logs históricos |
| S3 Glacier Flexible | archivo, minutos/horas | compliance, auditoría |

### Lifecycle policies

Transición automática entre clases por tiempo:

```
0-30 días   → S3 Standard
30-90 días  → S3 Standard-IA
90-365 días → S3 Glacier Instant
> 365 días  → eliminar
```

## RDS — Relational Database Service

Base de datos relacional gestionada. AWS gestiona backups, patches, failover.

```bash
# crear instancia RDS PostgreSQL
aws rds create-db-instance \
  --db-instance-identifier gtm-db \
  --db-instance-class db.t3.micro \
  --engine postgres \
  --engine-version 16.3 \
  --master-username admin \
  --master-user-password "S3cur3Pass!" \
  --allocated-storage 20 \
  --multi-az \
  --no-publicly-accessible
```

| Parámetro | Producción recomendado |
|-----------|----------------------|
| Multi-AZ | activado (failover automático) |
| Automated backups | 7-35 días de retención |
| Publicly accessible | desactivado |
| Encryption at rest | activado |

### Connection string desde .NET

```
# appsettings.Production.json — valor desde Secrets Manager
"ConnectionStrings": {
  "Default": "Host=gtm-db.xyz.us-east-1.rds.amazonaws.com;Port=5432;Database=gtm;Username=admin;Password=..."
}
```

## IAM — Identity and Access Management

Controla quién puede hacer qué sobre qué recursos.

### Conceptos

| Concepto | Descripción |
|----------|-------------|
| User | identidad para una persona o aplicación |
| Group | conjunto de usuarios con los mismos permisos |
| Role | identidad asumible temporalmente (por servicios, pipelines, etc.) |
| Policy | documento JSON que define permisos (Allow/Deny + Action + Resource) |

### Política de ejemplo — acceso S3 a un bucket específico

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"],
      "Resource": "arn:aws:s3:::mi-bucket-raptor/*"
    },
    {
      "Effect": "Allow",
      "Action": "s3:ListBucket",
      "Resource": "arn:aws:s3:::mi-bucket-raptor"
    }
  ]
}
```

### Shared responsibility model

| AWS gestiona | El cliente gestiona |
|-------------|---------------------|
| hardware físico, red, hipervisor | sistema operativo de EC2 |
| parches de RDS | aplicación y su código |
| seguridad física de data centers | configuración de IAM, grupos de seguridad |

### Principio de mínimo privilegio

Nunca usar credenciales de root para operaciones normales. Crear un usuario IAM con solo los permisos necesarios. Para servicios en EC2/ECS usar IAM Roles, no access keys.

## Grupos de seguridad (Security Groups)

Firewall a nivel de instancia. Solo permite tráfico explícitamente autorizado.

```bash
# permitir HTTPS desde cualquier IP
aws ec2 authorize-security-group-ingress \
  --group-id sg-12345678 \
  --protocol tcp \
  --port 443 \
  --cidr 0.0.0.0/0

# permitir PostgreSQL solo desde el security group de la app
aws ec2 authorize-security-group-ingress \
  --group-id sg-db \
  --protocol tcp \
  --port 5432 \
  --source-group sg-app
```

Regla: las bases de datos no tienen acceso público. Solo el security group de la app puede alcanzar el puerto de la DB.

## Relación con back-template

El back-template se despliega en ECS Fargate (contenedor Docker). La base de datos va en RDS PostgreSQL con Multi-AZ. Los assets estáticos del frontend van en S3 con CloudFront. Los secretos (connection strings, JWT secret) se almacenan en AWS Secrets Manager (ver `05-secretos-produccion.md`). El pipeline CI/CD empuja imágenes a ECR (ver `09-cicd/02-github-actions.md`).

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| EC2 cuando se necesita control total del SO | EC2 para cargas stateless que caben en contenedores (usar ECS/Fargate) |
| RDS para bases de datos de producción con failover | RDS en entornos de dev (usar contenedor Docker local) |
| S3 para almacenamiento de objetos de cualquier tamaño | S3 como base de datos (no es transaccional) |
| IAM Roles para servicios, nunca access keys embebidas | crear un solo usuario IAM con todos los permisos |


---

*Rogelio Arriaga Gonzalez*
