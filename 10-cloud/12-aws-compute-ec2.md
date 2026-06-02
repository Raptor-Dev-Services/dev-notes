# 12 · AWS Compute — EC2, Auto Scaling y ALB

## Problema que resuelve

Desplegar una API .NET en producción requiere elegir el tipo correcto de instancia, garantizar alta disponibilidad ante fallos de hardware, escalar automáticamente ante picos de tráfico, y distribuir carga entre instancias. Sin Auto Scaling un servidor saturado cae sin recuperarse solo. Sin ALB un solo servidor es un punto único de fallo. Este documento cubre EC2 desde la elección de instancia hasta el balanceo de carga production-ready.

---

## Familias de instancia EC2

AWS organiza las instancias en familias según su uso. El nombre sigue el patrón `[familia][generación].[tamaño]`:

```
t3.micro    → familia t, generación 3, tamaño micro
c6i.large   → familia c, generación 6, variante Intel, tamaño large
r6g.xlarge  → familia r, generación 6, variante Graviton (ARM), tamaño xlarge
```

### Tabla comparativa de familias

| Familia | Característica | Caso de uso | Ejemplo |
|---------|---------------|-------------|---------|
| **t** (burstable) | CPU variable con créditos, más barato | Desarrollo, staging, cargas variables | `t3.micro`, `t3.small` |
| **m** (general purpose) | CPU/RAM balanceados | Producción de APIs .NET, servidores web | `m6i.large`, `m6i.xlarge` |
| **c** (compute optimized) | Mayor ratio CPU/RAM | APIs CPU-intensivas, procesamiento de imágenes | `c6i.large`, `c6i.2xlarge` |
| **r** (memory optimized) | Mayor ratio RAM/CPU | Bases de datos en EC2, caches en memoria grandes | `r6i.large`, `r6i.2xlarge` |
| **i** (storage optimized) | NVMe de alta velocidad | Bases de datos NoSQL, data warehouses | `i3.large` |
| **p/g** (GPU) | GPU NVIDIA | Machine learning, inferencia, rendering | `p3.2xlarge`, `g4dn.xlarge` |

**Instancias burstable (familia t) — cómo funcionan los créditos:**

```
Acumulación de créditos cuando CPU < baseline (ej. 10% para t3.micro)
Consumo de créditos cuando CPU > baseline
Si se agotan los créditos → CPU limitada al baseline

Para producción con carga consistente: usar familia m, no t.
t3 solo para entornos donde la CPU es baja la mayor parte del tiempo.
```

---

## Tipos de compra

| Tipo | Descuento | Compromiso | Interrupción | Caso de uso |
|------|-----------|-----------|--------------|-------------|
| **On-Demand** | — | Ninguno | No | Desarrollo, cargas impredecibles |
| **Reserved Instances** (1 año) | ~40% | 1 año | No | Producción con carga estable |
| **Reserved Instances** (3 años) | ~60-72% | 3 años | No | Producción muy estable |
| **Savings Plans** | ~66% | 1-3 años, $ comprometidos | No | Flexibilidad entre familias |
| **Spot Instances** | ~70-90% | Ninguno | Si (2 min de aviso) | Jobs de batch, ML, renderizado |

### Reserved Instances — dos tipos

```
Standard RI:
  - Fija: familia, tamaño, región, OS
  - Mayor descuento (~72%)
  - No se puede cambiar el tipo de instancia
  - Se puede vender en el Reserved Instance Marketplace

Convertible RI:
  - Puede cambiarse por otra familia o tipo durante el período
  - Menor descuento (~54%)
  - No vendible en el Marketplace
```

### Spot Instances — advertencias importantes

```bash
# Las Spot pueden ser terminadas con 2 minutos de aviso
# AWS envía una notificación de terminación via:
#   - Instance Metadata Service (IMDS)
#   - CloudWatch Events / EventBridge

# Consultar si la instancia va a ser terminada
TOKEN=$(curl -s -X PUT "http://169.254.169.254/latest/api/token" \
  -H "X-aws-ec2-metadata-token-ttl-seconds: 21600")
curl -s -H "X-aws-ec2-metadata-token: $TOKEN" \
  http://169.254.169.254/latest/meta-data/spot/termination-time
# Si responde 404: no hay terminación pendiente
# Si responde con timestamp: la instancia se terminará en ~2 min
```

---

## AMIs — Amazon Machine Images

Una AMI es una imagen del sistema con el SO, software preinstalado y configuración base. Es el punto de partida para lanzar instancias.

### AMIs de AWS disponibles

| AMI | Caso de uso recomendado |
|-----|------------------------|
| Amazon Linux 2023 | Primera opción para servidores .NET en AWS |
| Ubuntu 22.04 LTS | Familiaridad con el ecosistema Linux/Debian |
| Windows Server 2022 | Apps .NET que requieren Windows |
| Amazon ECS-Optimized | Instancias que van a correr contenedores en ECS |

### Crear una AMI propia

```bash
# Crear AMI desde una instancia en ejecucion
aws ec2 create-image \
  --instance-id i-1234567890abcdef0 \
  --name "back-template-v1.2.0-$(date +%Y%m%d)" \
  --description "AMI con Docker, CloudWatch Agent y .NET 10 preinstalados" \
  --no-reboot   # false si quieres garantizar estado limpio del filesystem

# La AMI incluye el EBS root volume de la instancia
# Puede compartirse entre cuentas o regiones (copiando)
```

---

## User Data — bootstrap de instancias

User Data es un script que se ejecuta una sola vez al primer arranque de la instancia (via `cloud-init`). Se usa para instalar dependencias, configurar el agente de CloudWatch, y preparar el ambiente.

```bash
#!/bin/bash
# Ejemplo de User Data para servidor del back-template

# 1. Actualizar el sistema
yum update -y

# 2. Instalar Docker
yum install -y docker
systemctl start docker
systemctl enable docker
usermod -a -G docker ec2-user

# 3. Instalar .NET 10
rpm --import https://packages.microsoft.com/keys/microsoft.asc
curl -o /etc/yum.repos.d/microsoft-prod.repo \
  https://packages.microsoft.com/config/centos/8/prod.repo
yum install -y dotnet-sdk-10.0

# 4. Instalar y configurar el agente de CloudWatch
yum install -y amazon-cloudwatch-agent
/opt/aws/amazon-cloudwatch-agent/bin/amazon-cloudwatch-agent-ctl \
  -a fetch-config -m ec2 -c ssm:/cloudwatch-config -s

# 5. Descargar configuracion de Secrets Manager (usando el IAM Role de la instancia)
SECRET=$(aws secretsmanager get-secret-value \
  --secret-id prod/backtemplate/appsettings \
  --region us-east-1 \
  --query SecretString \
  --output text)
echo "$SECRET" > /app/appsettings.Production.json
```

---

## Key Pairs y SSH

```bash
# Crear key pair (guarda el .pem localmente — AWS no lo almacena)
aws ec2 create-key-pair \
  --key-name prod-keypair \
  --query 'KeyMaterial' \
  --output text > prod-keypair.pem

chmod 400 prod-keypair.pem

# Conectar a la instancia
ssh -i prod-keypair.pem ec2-user@<IP-PUBLICA-O-BASTION>
```

**Alternativa preferida: AWS Systems Manager Session Manager (sin Puerto 22)**

```bash
# Requisito: instancia tiene SSM Agent + IAM Role con AmazonSSMManagedInstanceCore
# Ventajas:
#   - No necesita abrir puerto 22 en el Security Group
#   - Registro de sesiones en CloudWatch / S3
#   - Acceso desde consola web o CLI sin key pair

# Iniciar sesion desde CLI
aws ssm start-session --target i-1234567890abcdef0

# O abrir tunel para conectar al puerto 8080 del servidor de forma local
aws ssm start-session \
  --target i-1234567890abcdef0 \
  --document-name AWS-StartPortForwardingSession \
  --parameters '{"portNumber":["8080"],"localPortNumber":["8080"]}'
```

---

## Elastic IP vs IP pública dinámica

```
❌ IP pública dinámica:
  - Se asigna al lanzar la instancia
  - Cambia cada vez que se detiene y reinicia la instancia
  - No se puede referenciar con un registro DNS estático

✓ Elastic IP:
  - IP fija asignada a tu cuenta AWS
  - Permanece la misma aunque la instancia se detenga
  - Puede reasociarse a otra instancia en caso de failover manual

Costo Elastic IP:
  - Gratuita mientras esté asociada a una instancia en ejecucion
  - ~$0.005/hora cuando NO está asociada (AWS penaliza las IPs no usadas)

Recomendacion para produccion:
  Usar ALB con DNS en lugar de Elastic IPs directas en instancias.
  El ALB gestiona la exposicion publica sin necesidad de IPs fijas en EC2.
```

---

## IAM Instance Profiles

Permiten que una instancia EC2 acceda a servicios de AWS (S3, Secrets Manager, SSM, CloudWatch) sin almacenar credenciales en el código o el servidor.

```
Mecanismo:
  1. EC2 tiene un IAM Role asociado (vía Instance Profile)
  2. El agente de metadata en 169.254.169.254 provee credenciales temporales
  3. Las credenciales rotan automáticamente cada hora
  4. El SDK de .NET las obtiene automáticamente (DefaultAWSCredentials)
```

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue",
        "secretsmanager:DescribeSecret"
      ],
      "Resource": "arn:aws:secretsmanager:us-east-1:123456789:secret:prod/backtemplate/*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject"
      ],
      "Resource": "arn:aws:s3:::gtm-files-prod/*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "cloudwatch:PutMetricData",
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents"
      ],
      "Resource": "*"
    }
  ]
}
```

```bash
# Asociar el role a la instancia al lanzarla
aws ec2 run-instances \
  --image-id ami-12345678 \
  --instance-type m6i.large \
  --iam-instance-profile Name=BackTemplateInstanceProfile \
  --key-name prod-keypair \
  --security-group-ids sg-app \
  --subnet-id subnet-private-1a
```

---

## Auto Scaling Groups (ASG)

Un ASG mantiene la cantidad de instancias EC2 deseada y escala automáticamente según métricas de CloudWatch.

```
min/desired/max:
  min     = mínimo de instancias siempre en ejecución (garantía de disponibilidad)
  desired = cantidad actual deseada en estado normal
  max     = techo máximo al que puede escalar

Ejemplo:
  min=2, desired=2, max=10
  → Siempre 2 instancias corriendo (una por AZ para HA)
  → Escala hasta 10 en pico de tráfico
  → Nunca baja de 2 (garantiza uptime)
```

### Políticas de escalado

```bash
# Crear politica de Scale Out (agregar instancias cuando CPU > 70%)
aws autoscaling put-scaling-policy \
  --auto-scaling-group-name gtm-asg \
  --policy-name scale-out-cpu \
  --policy-type TargetTrackingScaling \
  --target-tracking-configuration '{
    "PredefinedMetricSpecification": {
      "PredefinedMetricType": "ASGAverageCPUUtilization"
    },
    "TargetValue": 70.0,
    "ScaleOutCooldown": 300,
    "ScaleInCooldown": 600
  }'
# TargetTracking mantiene el CPU ~70%
# ScaleOutCooldown: esperar 5 min después de escalar antes de volver a evaluar
# ScaleInCooldown: esperar 10 min antes de reducir (evitar "flapping")
```

### Health Checks del ASG

```
EC2 Health Check (default):
  - ASG monitorea el estado de la instancia en EC2
  - Si la instancia está en estado "impaired" → se reemplaza
  - No detecta si la app dentro esta caída

ELB Health Check (recomendado para produccion):
  - ASG usa el health check del ALB Target Group
  - Si el endpoint /health devuelve 5xx → la instancia se reemplaza
  - Detecta fallos de la aplicacion, no solo de la instancia
```

---

## Application Load Balancer (ALB)

El ALB es un proxy reverso gestionado que distribuye tráfico HTTP/HTTPS entre instancias en múltiples AZs.

### Componentes del ALB

```
Listener  → escucha en un puerto (80 o 443)
  └── Rules → condiciones para redirigir el tráfico
       └── Target Group → conjunto de instancias (o contenedores)
            └── Health Check → endpoint que valida la salud
```

```bash
# Crear ALB
aws elbv2 create-load-balancer \
  --name gtm-alb \
  --subnets subnet-public-1a subnet-public-1b \
  --security-groups sg-alb \
  --type application

# Crear target group
aws elbv2 create-target-group \
  --name gtm-tg \
  --protocol HTTP \
  --port 8080 \
  --vpc-id vpc-87654321 \
  --health-check-path /health \
  --health-check-interval-seconds 30 \
  --healthy-threshold-count 2 \
  --unhealthy-threshold-count 3

# Crear listener HTTPS con certificado ACM
aws elbv2 create-listener \
  --load-balancer-arn arn:aws:elasticloadbalancing:... \
  --protocol HTTPS \
  --port 443 \
  --certificates CertificateArn=arn:aws:acm:... \
  --default-actions Type=forward,TargetGroupArn=arn:aws:elasticloadbalancing:...

# Redirect HTTP a HTTPS
aws elbv2 create-listener \
  --load-balancer-arn arn:aws:elasticloadbalancing:... \
  --protocol HTTP \
  --port 80 \
  --default-actions \
    Type=redirect,RedirectConfig='{Protocol=HTTPS,StatusCode=HTTP_301}'
```

### Path-based routing con el ALB

```bash
# Ruta /api/* → Target Group del back-template
# Ruta /admin/* → Target Group del panel de admin
# Default → Target Group del frontend

aws elbv2 create-rule \
  --listener-arn arn:aws:elasticloadbalancing:... \
  --priority 10 \
  --conditions Field=path-pattern,Values=/api/* \
  --actions Type=forward,TargetGroupArn=arn:...-api-tg

aws elbv2 create-rule \
  --listener-arn arn:aws:elasticloadbalancing:... \
  --priority 20 \
  --conditions Field=path-pattern,Values=/admin/* \
  --actions Type=forward,TargetGroupArn=arn:...-admin-tg
```

### ALB vs NLB

| Característica | ALB | NLB |
|----------------|-----|-----|
| Capa OSI | Capa 7 (HTTP/HTTPS) | Capa 4 (TCP/UDP) |
| Routing | Path, host, headers | Solo IP y puerto |
| SSL termination | Si | Si (pass-through posible) |
| WebSockets | Si | Si |
| Static IP | No (DNS dinámico) | Si (Elastic IPs por AZ) |
| Latencia | ~ms adicional | Ultra baja |
| Caso de uso | APIs, aplicaciones web | Alto rendimiento TCP, juegos, VoIP |

---

## Relación con back-template

```
Internet → Route 53 ALIAS → ALB (sg-alb: 443 pública)
                              ↓
                       ALB listener 443
                              ↓
                    Target Group (health check /health)
                              ↓
              ASG [min=2, max=10, policy CPU>70%]
              ├── EC2 1 en us-east-1a (sg-app: 8080 desde sg-alb)
              └── EC2 2 en us-east-1b (sg-app: 8080 desde sg-alb)
                         ↓
              IAM Instance Profile → Secrets Manager, S3, CloudWatch
                         ↓
                 RDS PostgreSQL (sg-db: 5432 desde sg-app)
```

El back-template expone `/health` para el health check del ALB. El ASG reemplaza instancias enfermas automáticamente. Las credenciales viven en Secrets Manager, no en el código.

---

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Familia `m` para producción de API .NET estable | Familia `t` para cargas consistentes en producción |
| Reserved Instances para workloads predecibles 1+ años | On-Demand para todo (costo innecesario) |
| Spot para jobs de batch o procesamiento de imágenes | Spot para el servidor web principal |
| ALB con path routing para microservicios o multi-app | NLB para tráfico HTTP (ALB es el correcto) |
| IAM Instance Profile, nunca access keys en el servidor | Guardar `AWS_ACCESS_KEY_ID` en archivos de config |
| Session Manager para acceso SSH sin abrir puerto 22 | Exponer puerto 22 al mundo (`0.0.0.0/0`) |
| ELB Health Checks en el ASG | Solo EC2 Health Checks (no detecta fallos de la app) |

---

## Glosario

| Término | Definición |
|---------|-----------|
| EC2 | Elastic Compute Cloud. Máquinas virtuales bajo demanda en AWS. |
| AMI | Amazon Machine Image. Snapshot de un sistema listo para lanzar instancias. |
| User Data | Script que se ejecuta al primer arranque de una instancia (bootstrap). |
| Instance Profile | Contenedor de un IAM Role que se asocia a una instancia EC2. |
| Auto Scaling Group | Grupo lógico de instancias EC2 con escalado automático basado en métricas. |
| On-Demand | Pago por hora sin compromiso. Precio completo. |
| Reserved Instance | Capacidad comprometida 1-3 años. Hasta 72% de descuento. |
| Spot Instance | Capacidad sobrante de AWS. Hasta 90% descuento. Puede terminarse con 2 min de aviso. |
| Savings Plans | Compromiso de gasto $ por hora, no de tipo de instancia. Flexibilidad. |
| Elastic IP | IP pública fija asignada a tu cuenta AWS. No cambia al reiniciar. |
| ALB | Application Load Balancer. Proxy reverso HTTP/HTTPS en Capa 7. |
| NLB | Network Load Balancer. Proxy TCP/UDP en Capa 4. Ultra baja latencia. |
| Target Group | Grupo de destinos (instancias, IPs, Lambdas) que reciben tráfico del ALB. |
| Listener | Puerto/protocolo en el que el ALB escucha tráfico entrante. |
| Session Manager | Herramienta de AWS Systems Manager para acceso a instancias sin SSH. |
| Burstable (familia t) | Instancias con CPU variable mediante un sistema de créditos. |
| Cooldown | Período de espera después de un evento de escalado antes de evaluar nuevamente. |
| IMDS | Instance Metadata Service. Servidor en 169.254.169.254 con info de la instancia. |

> Fuente: *AWS Certified Solutions Architect – Associate Guide* (Gabriel Ramirez, Packt) — Ch.3 Elasticity, Scalability and EC2, Ch.8 Elastic Load Balancing y Auto Scaling

---

*Rogelio Arriaga Gonzalez*
