# 11 · AWS VPC y Networking

## Problema que resuelve

Cuando se despliega una aplicación en AWS sin entender la red, todos los recursos quedan expuestos públicamente o no pueden comunicarse entre sí. Una VPC mal configurada deja a la base de datos accesible desde Internet, expone puertos internos al mundo exterior, o impide que las instancias privadas accedan a Internet para descargar paquetes. Este documento cubre el diseño completo de red en AWS: aislamiento, enrutamiento, seguridad y DNS.

---

## VPC — Virtual Private Cloud

Una VPC es una red virtual privada dentro de AWS completamente aislada de otras cuentas. Todo recurso (EC2, RDS, Lambda en VPC) vive dentro de una VPC.

**Características fundamentales:**
- Rango de direcciones IP definido con CIDR (ej. `10.0.0.0/16`)
- Aislada por defecto: ningún recurso externo puede entrar sin configuración explícita
- Se divide en subredes distribuidas en Availability Zones
- Tiene tablas de enrutamiento, gateways y listas de control de acceso

**VPC por defecto:**
AWS crea una VPC por defecto en cada región con CIDR `172.31.0.0/16`. Es funcional pero no recomendada para producción porque tiene todas las subredes públicas.

---

## Direccionamiento CIDR — cómo partir una red

CIDR (Classless Inter-Domain Routing) define bloques de direcciones IP. El número después de `/` indica cuántos bits son la red (el resto son hosts).

```
10.0.0.0/16   → 65.536 IPs disponibles (bits de host: 16)
10.0.0.0/24   → 256 IPs disponibles   (bits de host: 8)
10.0.0.0/28   → 16 IPs disponibles    (bits de host: 4)

AWS reserva 5 IPs por subnet (las primeras 4 y la última):
  10.0.1.0   → dirección de red
  10.0.1.1   → router de VPC
  10.0.1.2   → servidor DNS de AWS
  10.0.1.3   → reservada para uso futuro
  10.0.1.255 → broadcast
Una /24 real tiene 251 IPs utilizables.
```

### Diseño de VPC para producción (3 tiers, 2 AZs)

```
VPC: 10.0.0.0/16  (65.536 IPs — espacio suficiente para crecer)

Subredes públicas (ALB, NAT Gateway, Bastion):
  10.0.1.0/24  → us-east-1a  (251 IPs)
  10.0.2.0/24  → us-east-1b  (251 IPs)

Subredes privadas de aplicación (EC2/ECS):
  10.0.11.0/24 → us-east-1a  (251 IPs)
  10.0.12.0/24 → us-east-1b  (251 IPs)

Subredes privadas de datos (RDS, ElastiCache):
  10.0.21.0/24 → us-east-1a  (251 IPs)
  10.0.22.0/24 → us-east-1b  (251 IPs)
```

**Diagrama de arquitectura:**

```
Internet
    |
    | (tráfico público HTTPS 443)
    |
[Internet Gateway]
    |
[Subredes Públicas: 10.0.1.0/24, 10.0.2.0/24]
    |-- ALB (Application Load Balancer)
    |-- NAT Gateway (una por AZ)
    |
[Subredes Privadas App: 10.0.11.0/24, 10.0.12.0/24]
    |-- EC2 / ECS (back-template .NET)
    |
[Subredes Privadas Data: 10.0.21.0/24, 10.0.22.0/24]
    |-- RDS PostgreSQL (Multi-AZ)
    |-- ElastiCache Redis
```

---

## Subredes — pública vs privada

**El atributo "pública" NO es inherente a la subnet**. Lo que determina si una subnet es pública o privada es su tabla de enrutamiento:

```
❌ Concepto erróneo: "esta subnet tiene el flag 'público' activado"

✓ Correcto:
  - Subnet pública = su route table tiene 0.0.0.0/0 → Internet Gateway
  - Subnet privada = su route table NO tiene ruta a Internet (o tiene 0.0.0.0/0 → NAT Gateway)
```

| Característica | Subnet Pública | Subnet Privada |
|----------------|---------------|----------------|
| Ruta a Internet | 0.0.0.0/0 → IGW | No hay / usa NAT |
| Recursos típicos | ALB, NAT GW, Bastion | EC2 de app, RDS |
| Asignación de IP pública | Automática (opcional) | Nunca |
| Accesible desde Internet | Si el SG lo permite | No directamente |

---

## Route Tables — tablas de enrutamiento

Cada subnet tiene una route table asociada. AWS evalúa las rutas de más específica a menos específica.

### Route table de subnet pública

```
Destino         Objetivo
10.0.0.0/16    local          ← tráfico dentro de la VPC
0.0.0.0/0      igw-12345678   ← todo lo demás sale a Internet
```

### Route table de subnet privada (con NAT)

```
Destino         Objetivo
10.0.0.0/16    local          ← tráfico dentro de la VPC
0.0.0.0/0      nat-12345678   ← sale a Internet via NAT (sin entrada)
```

### Route table de subnet de datos (sin Internet)

```
Destino         Objetivo
10.0.0.0/16    local          ← solo tráfico interno de la VPC
```

---

## Internet Gateway (IGW)

Un IGW es el componente que conecta la VPC a Internet. Es stateful, escalable horizontalmente, y no tiene coste por tráfico (solo el tráfico de datos tiene coste).

```bash
# crear IGW
aws ec2 create-internet-gateway

# asociar IGW a la VPC
aws ec2 attach-internet-gateway \
  --internet-gateway-id igw-12345678 \
  --vpc-id vpc-87654321

# agregar ruta en la route table pública
aws ec2 create-route \
  --route-table-id rtb-public \
  --destination-cidr-block 0.0.0.0/0 \
  --gateway-id igw-12345678
```

**Regla:** solo puede haber UN IGW por VPC.

---

## NAT Gateway

Permite que recursos en subredes PRIVADAS inicien conexiones a Internet (para descargar paquetes, llamar APIs externas) sin que Internet pueda iniciar conexiones hacia ellos.

```
❌ El problema sin NAT:
  EC2 en subred privada no puede descargar actualizaciones de Linux,
  no puede llamar a APIs de terceros, no puede enviar emails via SES.

✓ La solución:
  NAT Gateway en subred PÚBLICA con Elastic IP.
  La subred privada tiene ruta 0.0.0.0/0 → NAT Gateway.
  El NAT traduce la IP privada a su Elastic IP pública (NAT).
```

**Puntos clave:**
- El NAT Gateway vive en la **subred pública** (necesita acceso a IGW)
- Lo usan las **subredes privadas** (apuntan a él en su route table)
- Para alta disponibilidad: un NAT Gateway **por AZ** (costo adicional, pero sin single point of failure)
- Costo: ~$0.045/hora + $0.045/GB procesado

```bash
# crear Elastic IP para el NAT
aws ec2 allocate-address --domain vpc

# crear NAT Gateway en la subnet pública
aws ec2 create-nat-gateway \
  --subnet-id subnet-public-1a \
  --allocation-id eipalloc-12345678

# agregar ruta en subnet privada apuntando al NAT
aws ec2 create-route \
  --route-table-id rtb-private-1a \
  --destination-cidr-block 0.0.0.0/0 \
  --nat-gateway-id nat-12345678
```

### IGW vs NAT Gateway

| Característica | Internet Gateway | NAT Gateway |
|----------------|-----------------|-------------|
| Dirección del tráfico | Bidireccional | Solo salida (outbound) |
| Quién lo usa | Recursos públicos | Recursos privados |
| Vive en | La VPC (asociado) | Subred pública |
| Entrada desde Internet | Si el SG lo permite | Nunca |
| Costo | Gratuito (solo tráfico) | $0.045/hora + datos |

---

## Availability Zones (AZ)

Las AZs son centros de datos físicamente separados dentro de una región. Tienen energía, refrigeración y conectividad de red independientes.

```
us-east-1 (N. Virginia)
├── us-east-1a  → data center A
├── us-east-1b  → data center B
├── us-east-1c  → data center C
├── us-east-1d  → data center D
└── us-east-1f  → data center F
```

**Latencia entre AZs:** 1-2 ms (dentro de la misma región). Suficiente para replicación síncrona de RDS Multi-AZ.

### Alta disponibilidad real con Multi-AZ

```
❌ Diseño de una sola AZ:
  EC2 en us-east-1a + RDS en us-east-1a
  → si falla us-east-1a, cae todo el servicio

✓ Diseño Multi-AZ correcto:
  ALB             → distribuye entre las 2 AZs
  EC2 en 1a       → maneja tráfico cuando 1b falla
  EC2 en 1b       → maneja tráfico cuando 1a falla
  RDS Primary 1a  → escribe aquí normalmente
  RDS Standby 1b  → failover automático en ~60 segundos
```

---

## Security Groups (SG)

Los Security Groups son firewalls **stateful** a nivel de instancia/interfaz de red. Stateful significa que si se permite una conexión entrante, la respuesta sale automáticamente aunque no haya regla de salida explícita.

**Características:**
- Solo reglas de tipo ALLOW (no existe DENY explícito)
- Todo lo no permitido está denegado por defecto
- Se puede referenciar otro SG como origen/destino (mejor práctica que usar rangos IP)
- Pueden aplicarse a EC2, RDS, Lambda en VPC, ALB, etc.

### Reglas típicas para el stack .NET + PostgreSQL

```bash
# SG del Application Load Balancer
aws ec2 authorize-security-group-ingress \
  --group-id sg-alb \
  --protocol tcp --port 443 --cidr 0.0.0.0/0   # HTTPS desde Internet

aws ec2 authorize-security-group-ingress \
  --group-id sg-alb \
  --protocol tcp --port 80 --cidr 0.0.0.0/0    # HTTP (redirect a HTTPS)

# SG del servidor de aplicacion (EC2/ECS)
# Solo acepta trafico desde el ALB — referencia por SG, no por IP
aws ec2 authorize-security-group-ingress \
  --group-id sg-app \
  --protocol tcp --port 8080 \
  --source-group sg-alb                          # origen: SG del ALB

# SG de la base de datos PostgreSQL
# Solo acepta trafico desde el SG de la aplicacion
aws ec2 authorize-security-group-ingress \
  --group-id sg-db \
  --protocol tcp --port 5432 \
  --source-group sg-app                          # origen: SG de la app
```

**Por qué referenciar SG en lugar de IPs:**

```
❌ Regla con IP: --cidr 10.0.11.0/24
  Problema: permite CUALQUIER IP en ese rango,
  incluso si un atacante llega a esa subnet.

✓ Regla con referencia a SG: --source-group sg-app
  Solo instancias que tengan el sg-app pueden conectar.
  Si el EC2 pierde ese SG, pierde acceso automáticamente.
```

---

## NACLs — Network Access Control Lists

Las NACLs son firewalls **stateless** a nivel de subnet. Stateless significa que debes crear reglas explícitas para el tráfico de ida Y el de vuelta.

| Característica | Security Group | NACL |
|----------------|---------------|------|
| Nivel | Instancia/interfaz | Subnet |
| Estado | Stateful | Stateless |
| Tipo de reglas | Solo Allow | Allow y Deny |
| Evaluación | Todas las reglas | Por número (orden) |
| Uso típico | Siempre | Bloqueo adicional |

**Cuándo usar NACLs:**
- Bloquear un rango de IPs que está haciendo fuerza bruta
- Defensa en profundidad cuando un SG está comprometido
- Cumplimiento regulatorio que exige control a nivel de subnet

**Regla importante:** las NACLs tienen reglas numeradas. Se evalúan en orden ascendente y se detiene en la primera coincidencia. La última regla es siempre `* → DENY`.

---

## VPC Endpoints

Permiten conectar servicios de AWS sin que el tráfico salga a Internet público. El tráfico permanece en la red de AWS.

### Gateway Endpoints (gratuitos)

Solo para S3 y DynamoDB. Se agrega una ruta en la route table de la subnet privada que apunta al endpoint.

```bash
# crear endpoint para S3
aws ec2 create-vpc-endpoint \
  --vpc-id vpc-87654321 \
  --service-name com.amazonaws.us-east-1.s3 \
  --route-table-ids rtb-private-1a rtb-private-1b

# La subnet privada puede acceder a S3 sin pasar por NAT Gateway
# → ahorro significativo en costo de NAT (~$0.045/GB)
```

**Beneficio cuantificable:** si tu app en subred privada sube 100 GB/mes a S3:
- Sin VPC Endpoint: 100 GB × $0.045 = $4.50/mes solo en NAT
- Con VPC Endpoint: $0 (el tráfico a S3 no pasa por NAT)

### Interface Endpoints / PrivateLink (con costo)

Para otros servicios de AWS: Secrets Manager, SSM, CloudWatch, ECR, SQS, SNS, etc. Crean ENIs (Elastic Network Interfaces) en la subnet con IPs privadas.

```bash
# crear interface endpoint para Secrets Manager
aws ec2 create-vpc-endpoint \
  --vpc-id vpc-87654321 \
  --vpc-endpoint-type Interface \
  --service-name com.amazonaws.us-east-1.secretsmanager \
  --subnet-ids subnet-private-1a subnet-private-1b \
  --security-group-ids sg-endpoints

# Costo: ~$0.01/hora por AZ + $0.01/GB procesado
```

**Cuándo vale la pena usar Interface Endpoints:**
- El servicio es crítico y no quieres que el tráfico salga a Internet
- Tienes requisitos de compliance que exigen que los datos no crucen Internet público
- Tienes alto volumen de llamadas a Secrets Manager o ECR

---

## Route 53 — DNS gestionado

### Tipos de registro DNS

| Tipo | Descripción | Caso de uso |
|------|-------------|-------------|
| A | Nombre → IPv4 | `api.miapp.com` → `52.1.2.3` |
| AAAA | Nombre → IPv6 | Igual que A pero IPv6 |
| CNAME | Nombre → otro nombre | `www` → `miapp.com` |
| ALIAS | Nombre → recurso AWS | `api.miapp.com` → ALB/CloudFront |
| MX | Mail exchange | Servidor de correo |
| TXT | Texto arbitrario | Verificación de dominio, SPF |

**Por qué ALIAS es especial en AWS:**

```
❌ CNAME en el apex del dominio (miapp.com) → NO permitido por DNS estándar
   CNAME solo funciona en subdominios (www.miapp.com)

✓ ALIAS resuelve esto:
  - Funciona en el apex del dominio (miapp.com) → ALB
  - Apunta a recursos AWS: ALB, CloudFront, S3 website, API Gateway
  - No tiene costo por query (AWS lo absorbe)
  - Se actualiza automáticamente si cambia la IP del ALB
```

### Routing Policies

| Policy | Comportamiento | Caso de uso |
|--------|---------------|-------------|
| Simple | Un solo destino | Aplicación básica |
| Weighted | Porcentaje de tráfico a cada destino | Canary deployment (10%/90%) |
| Failover | Primario → Secundario si falla health check | Disaster recovery |
| Latency | Dirige al recurso con menor latencia | App multi-región |
| Geolocation | Dirige por ubicación geográfica del cliente | GDPR, contenido localizado |
| Multivalue | Responde con hasta 8 IPs (solo registros sanos) | Distribución básica |

### Configuración típica para el back-template

```bash
# Crear registro ALIAS que apunta al ALB
aws route53 change-resource-record-sets \
  --hosted-zone-id Z1234567890 \
  --change-batch '{
    "Changes": [{
      "Action": "UPSERT",
      "ResourceRecordSet": {
        "Name": "api.misaas.com",
        "Type": "A",
        "AliasTarget": {
          "HostedZoneId": "Z35SXDOTRQ7X7K",
          "DNSName": "gtm-alb-123456.us-east-1.elb.amazonaws.com",
          "EvaluateTargetHealth": true
        }
      }
    }]
  }'

# Registro para subdominio de tenant (routing CNAME al ALB principal)
# alfacorp.misaas.com → misaas.com ALB
# La resolucion del tenant se hace en la aplicacion (middleware)
```

### Health Checks de Route 53

```
Frecuencia de check: cada 10 o 30 segundos
Protocolo: HTTP, HTTPS, TCP
Umbral: falla después de N checks consecutivos (default 3)
Acción: si falla, Route 53 no incluye ese endpoint en respuestas DNS
```

---

## Relación con back-template

El stack .NET se despliega en subredes privadas. El ALB vive en subredes públicas y es el único punto de entrada. La configuración de VPC es fundamental para el modelo de subdominio de multi-tenancy:

```
alfacorp.misaas.com  ─→  Route 53 ALIAS → ALB
betacorp.misaas.com  ─→  Route 53 ALIAS → ALB (mismo ALB)
                            |
                          ALB listeners con SSL wildcard *.misaas.com
                            |
                      SubdomainTenantMiddleware extrae "alfacorp" del Host header
                            |
                      ITenantContextAccessor → TenantId para queries
```

El `SubdomainTenantMiddleware` del back-template necesita que el DNS resuelva `*.misaas.com` al ALB. Un registro wildcard en Route 53 cubre todos los subdominios.

---

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Subredes privadas para EC2 y RDS | Exponer EC2 directamente a Internet sin ALB |
| VPC Endpoint Gateway para S3 (gratis) | NAT para tráfico a S3 cuando hay VPC Endpoint disponible |
| Referencia de SG a SG en reglas de firewall | Rangos CIDR amplios en reglas de SG |
| ALB en subred pública, app en privada | Instancias de app con IP pública directa |
| Route 53 ALIAS para recursos AWS | CNAME en el apex del dominio |
| Un NAT Gateway por AZ para HA | Un solo NAT para toda la VPC en producción |
| NACLs para bloquear IPs maliciosas | NACLs como reemplazo de Security Groups |

---

## Glosario

| Término | Definición |
|---------|-----------|
| VPC | Red virtual privada aislada dentro de AWS. Todo recurso vive en una VPC. |
| CIDR | Notación para definir rangos de IPs. `/16` = 65.536 IPs, `/24` = 256 IPs. |
| Subnet pública | Subred cuya route table tiene `0.0.0.0/0 → IGW`. Los recursos pueden tener IP pública. |
| Subnet privada | Subred sin ruta directa a Internet. Los recursos no son alcanzables desde afuera. |
| Route Table | Tabla de enrutamiento que define hacia dónde van los paquetes según su IP destino. |
| Internet Gateway (IGW) | Componente que conecta la VPC a Internet. Uno por VPC. |
| NAT Gateway | Permite que recursos privados inicien conexiones a Internet. Vive en subred pública. |
| Security Group | Firewall stateful a nivel de instancia. Solo reglas ALLOW. |
| NACL | Firewall stateless a nivel de subnet. Permite ALLOW y DENY. Se evalúa en orden. |
| VPC Endpoint Gateway | Conexión directa a S3/DynamoDB sin salir a Internet. Gratuito. |
| PrivateLink | Conexión privada a otros servicios AWS via ENIs. Tiene costo por hora. |
| Availability Zone | Data center físicamente separado dentro de una región AWS. |
| Route 53 | Servicio DNS gestionado de AWS. Soporta routing policies avanzados. |
| Registro ALIAS | Tipo de registro DNS específico de AWS. Apunta a recursos AWS, gratis por query. |
| CNAME | Registro DNS que apunta un nombre a otro nombre. No funciona en el apex del dominio. |
| Elastic IP | IP pública fija asignada a tu cuenta. No cambia al reiniciar instancias. |
| ENI | Elastic Network Interface. Interfaz de red virtual que pueden tener EC2, RDS, Lambda. |

> Fuente: *AWS Certified Solutions Architect – Associate Guide* (Gabriel Ramirez, Packt): Ch.5 VPC, Networking y Route 53

---

*Rogelio Arriaga Gonzalez*
