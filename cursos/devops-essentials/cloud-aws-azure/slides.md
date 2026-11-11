<!-- _class: lead -->

# Cloud para Desarrolladores
## AWS + Azure  -  Lo que necesitas saber para desplegar el sistema de mensajería

**Dev Notes  -  Material de autoestudio**
Rogelio Arriaga  -  Raptor Dev Services

---

<!-- _class: lead -->

# Parte 1
## Modelos de Servicio
### IaaS  -  PaaS  -  SaaS  -  por qué importa la distinción

---

## IaaS  -  PaaS  -  SaaS  -  La pirámide de responsabilidad

| Modelo | Tú gestionas | El proveedor gestiona | Ejemplo |
|--------|-------------|----------------------|---------|
| **On-Premise** | todo | nada | servidor físico en oficina |
| **IaaS** | SO, runtime, app, datos | hardware, red, hipervisor | EC2, Azure VM |
| **PaaS** | app y datos | SO, runtime, escalado | App Service, RDS |
| **SaaS** | configuración y uso | todo lo demás | GitHub, Stripe |

> Regla práctica: cuanto más arriba en la pirámide, menos tiempo en operaciones y más en código.

---

## Por qué importa para el sistema de mensajería

**Antipatrón:** instalar PostgreSQL en una EC2 y administrarlo manualmente.

**Correcto:** usar RDS (PaaS)  -  AWS gestiona backups, patches y failover.

```
Sistema de mensajería  -  mapa de responsabilidades:

  EC2 (IaaS) -> correr la API .NET si se necesita control total del SO
  RDS (PaaS) -> PostgreSQL gestionado, sin tocar SO ni patches
  S3  (PaaS) -> almacenamiento de archivos adjuntos (media uploads)
  App Service (PaaS) -> alternativa a EC2 en Azure, sin gestionar SO
```

Elegir PaaS donde sea posible = menos toil operacional, más tiempo en producto.

---

<!-- _class: lead -->

# Parte 2
## AWS Core
### EC2  -  S3  -  RDS  -  VPC  -  IAM  -  Security Groups  -  Route 53

---

## AWS  -  Infraestructura global

| Concepto | Descripción |
|----------|-------------|
| **Región** | conjunto de AZs geográficamente separadas (`us-east-1`, `sa-east-1`) |
| **Zona de Disponibilidad (AZ)** | uno o más data centers físicamente separados dentro de una región |
| **Edge Location** | nodo de CloudFront (CDN) para cachear contenido cerca del usuario |

**Regla:** los recursos de alta disponibilidad se distribuyen en al menos 2 AZs de la misma región.

Para el sistema de mensajería: API en `us-east-1`, RDS con Multi-AZ activado, S3 (global por diseño).

---

## EC2  -  Elastic Compute Cloud

Máquinas virtuales bajo demanda. Tú eliges el tipo de instancia según la carga:

| Tipo | vCPU / RAM | Caso de uso |
|------|-----------|-------------|
| `t3.micro` | 2 / 1 GB | desarrollo, demos |
| `t3.small` | 2 / 2 GB | producción inicial MVP |
| `t3.medium` | 2 / 4 GB | producción ligera (API .NET) |
| `c6i.large` | 2 / 4 GB | cómputo intensivo |
| `r6i.large` | 2 / 16 GB | memoria intensiva (caches) |

**Modelo de persistencia:**
- **EBS**  -  volumen persistente adjunto, sobrevive reinicios
- **Instance Store**  -  efímero, se pierde al detener la instancia
- **AMI**  -  snapshot para lanzar réplicas idénticas

---

## S3  -  Simple Storage Service

Almacenamiento de objetos. Bucket -> objetos referenciados por clave. Ideal para media uploads del CRM.

```bash
# crear bucket en us-east-1
aws s3 mb s3://mensajeria-media --region us-east-1

# subir archivo (media upload desde el backend)
aws s3 cp adjunto.pdf s3://mensajeria-media/conversations/123/

# sincronizar build del frontend
aws s3 sync ./dist s3://mensajeria-media/frontend/
```

**Clases de almacenamiento:**

| Clase | Acceso | Uso CRM |
|-------|--------|---------|
| S3 Standard | frecuente | adjuntos activos, assets del frontend |
| S3 Standard-IA | infrecuente | backups semanales |
| S3 Glacier Instant | archivo | historial de conversaciones > 1 año |

---

## RDS  -  Relational Database Service

PostgreSQL gestionado: AWS administra backups, patches, failover. Tú solo escribes SQL.

```bash
aws rds create-db-instance \
  --db-instance-identifier mensajeria-db \
  --db-instance-class db.t3.micro \
  --engine postgres \
  --engine-version 16.3 \
  --master-username admin \
  --master-user-password "S3cur3Pass!" \
  --allocated-storage 20 \
  --multi-az \
  --no-publicly-accessible
```

| Parámetro | Valor en producción |
|-----------|---------------------|
| Multi-AZ | activado (failover automático) |
| Automated backups | 7 días mínimo |
| Publicly accessible | **desactivado** |
| Encryption at rest | activado |

---

## VPC  -  Security Groups  -  Route 53

**VPC (Virtual Private Cloud):** red privada virtual donde viven tus recursos.

**Security Groups:** firewall a nivel de instancia  -  solo permite tráfico explícito.

```bash
# HTTPS desde cualquier IP -> API pública
aws ec2 authorize-security-group-ingress \
  --group-id sg-api --protocol tcp --port 443 --cidr 0.0.0.0/0

# PostgreSQL solo desde el security group de la API (nunca desde internet)
aws ec2 authorize-security-group-ingress \
  --group-id sg-db --protocol tcp --port 5432 --source-group sg-api
```

**Route 53:** DNS gestionado de AWS. Registrar el dominio `mensajeria.io` y apuntar al load balancer.

> Regla de oro: la base de datos nunca tiene acceso público. Solo el security group de la app puede alcanzar el puerto 5432.

---

<!-- _class: lead -->

# Parte 3
## AWS para el Proyecto
### EC2  -  RDS PostgreSQL  -  S3 Media Uploads  -  Secrets Manager

---

## Arquitectura del sistema de mensajería en AWS

```
Internet
   |

Route 53 (mensajeria.io)
   |

Load Balancer (HTTPS:443)
   |

EC2 / ECS Fargate  <----- ECR (imagen Docker)
  API .NET 10             |
   |                      `- IAM Role (sin access keys)
   |-- RDS PostgreSQL (sg-db, puerto 5432, Multi-AZ)
   |-- S3 mensajeria-media (media uploads via pre-signed URLs)
   `-- Secrets Manager (connection string, JWT secret, API keys)
```

Todo dentro de la VPC. Solo el load balancer tiene IP pública.

---

## S3 para Media Uploads  -  Pre-signed URLs

La API genera una URL firmada temporalmente. El cliente sube directamente a S3 sin pasar por el servidor.

```csharp
// Endpoint: POST /api/v1/media/upload-url
var request = new GetPreSignedUrlRequest
{
    BucketName = "mensajeria-media",
    Key        = $"conversations/{conversationId}/{fileName}",
    Verb       = HttpVerb.PUT,
    Expires    = DateTime.UtcNow.AddMinutes(5),
    ContentType = contentType
};

var presignedUrl = s3Client.GetPreSignedURL(request);
return Results.Ok(new { uploadUrl = presignedUrl });
```

El frontend llama al endpoint, recibe la URL y hace PUT directo a S3. La API nunca maneja el binario.

---

## Secrets Manager  -  sin credenciales en código

```bash
# crear secreto con todos los valores sensibles del CRM
aws secretsmanager create-secret \
  --name "mensajeria/production" \
  --secret-string '{
    "ConnectionStrings__Default": "Host=crm-db.xyz.rds.amazonaws.com;...",
    "Jwt__Secret": "super-secret-key-256-bits",
    "WhatsApp__ApiKey": "wa-key-prod"
  }'
```

```csharp
// Program.cs  -  leer secretos al iniciar la API
builder.Configuration.AddSecretsManager(region: RegionEndpoint.USEast1, configurator: opts =>
{
    opts.SecretFilter = entry => entry.Name.StartsWith("mensajeria/");
    opts.KeyGenerator = (entry, key) => key
        .Replace("mensajeria/production:", "")
        .Replace("__", ":");
});
```

La API asume el IAM Role del contenedor  -  no hay access keys en el código ni en variables de entorno.

---

<!-- _class: lead -->

# Parte 4
## Azure Core
### VM  -  App Service  -  Azure SQL  -  Blob Storage  -  Key Vault  -  Entra ID

---

## Azure  -  Infraestructura global

| Concepto Azure | Equivalente AWS | Descripción |
|----------------|-----------------|-------------|
| **Region** (East US, Brazil South) | Region (`us-east-1`) | zona geográfica |
| **Availability Zone** | AZ | data center físicamente separado |
| **Resource Group** | (sin equivalente) | contenedor lógico para agrupar recursos |
| **Subscription** | AWS Account | unidad de facturación y acceso |

Un Resource Group agrupa todos los recursos del sistema de mensajería (App Service + DB + Blob + Key Vault) y permite eliminarlos o facturarlos juntos.

---

## App Service  -  PaaS para la API .NET

Equivalente a EC2 + gestión del SO. Soporta contenedores Docker directamente.

```bash
# crear App Service Plan (capacidad de cómputo)
az appservice plan create \
  --name crm-plan --resource-group crm-rg --sku B2 --is-linux

# desplegar contenedor Docker de la API
az webapp create \
  --name mensajeria-api \
  --resource-group crm-rg \
  --plan crm-plan \
  --deployment-container-image-name miacr.azurecr.io/mensajeria-api:v1.0.0
```

| SKU | vCPU / RAM | Uso recomendado |
|-----|-----------|-----------------|
| B1 | 1 / 1.75 GB | desarrollo local staging |
| B2 | 2 / 3.5 GB | staging + QA |
| P1v3 | 2 / 8 GB | producción inicial |

---

## Azure Blob Storage + PostgreSQL Flexible Server

**Blob Storage**  -  equivalente a S3:

```bash
az storage account create \
  --name mensajeriastorage --resource-group crm-rg --sku Standard_LRS
az storage container create --name media --account-name mensajeriastorage
```

**PostgreSQL Flexible Server**  -  equivalente a RDS:

```bash
az postgres flexible-server create \
  --name mensajeria-db \
  --resource-group crm-rg \
  --location eastus \
  --admin-user admin \
  --admin-password "S3cur3Pass!" \
  --sku-name Standard_B1ms \
  --public-access Disabled
```

---

## Azure Key Vault + Managed Identity

**Key Vault**  -  equivalente a Secrets Manager:

```bash
az keyvault create --name crm-keyvault --resource-group crm-rg --location eastus
az keyvault secret set --vault-name crm-keyvault \
  --name "ConnectionStrings--Default" \
  --value "Host=crm-db.postgres.database.azure.com;..."
```

**Leer desde .NET  -  sin credenciales en código:**

```csharp
// Program.cs
var keyVaultUri = new Uri("https://crm-keyvault.vault.azure.net/");
builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
```

`DefaultAzureCredential` usa la **Managed Identity** del App Service en producción y `az login` en local  -  mismo código, cero credenciales embebidas.

---

## Managed Identity  -  el equivalente a IAM Roles en Azure

```bash
# asignar identidad al App Service
az webapp identity assign --name mensajeria-api --resource-group crm-rg

# dar acceso al Key Vault con esa identidad
az keyvault set-policy \
  --name crm-keyvault \
  --object-id <principalId-del-app-service> \
  --secret-permissions get list

# dar acceso al ACR para desplegar imágenes
az role assignment create \
  --assignee <principalId> \
  --role AcrPull \
  --scope /subscriptions/.../registries/miacr
```

| Concepto Azure | Equivalente AWS |
|----------------|-----------------|
| Managed Identity | IAM Role para servicios |
| Service Principal | IAM User para aplicaciones |
| RBAC Role Assignment | IAM Policy |

---

<!-- _class: lead -->

# Parte 5
## AWS vs Azure
### Tabla comparativa de servicios equivalentes

---

## Tabla comparativa  -  servicios equivalentes

| Categoría | AWS | Azure |
|-----------|-----|-------|
| Cómputo VMs | EC2 | Azure Virtual Machines |
| Cómputo PaaS | ECS Fargate / Elastic Beanstalk | App Service |
| Contenedores registry | ECR | Azure Container Registry (ACR) |
| Base de datos relacional | RDS (PostgreSQL / MySQL) | Azure Database for PostgreSQL Flexible Server |
| Almacenamiento de objetos | S3 | Azure Blob Storage |
| Secretos y credenciales | Secrets Manager / Parameter Store | Azure Key Vault |
| Identidad y acceso | IAM (Users, Roles, Policies) | Entra ID (RBAC, Managed Identity) |
| DNS | Route 53 | Azure DNS |
| CDN | CloudFront | Azure CDN / Front Door |
| Colas de mensajes | SQS | Azure Service Bus / Storage Queues |
| Real-time / WebSockets | API Gateway + Lambda | Azure SignalR Service |
| CI/CD registry imágenes | ECR + CodePipeline | ACR + Azure DevOps |

---

<!-- _class: lead -->

# Parte 6
## IAM / RBAC
### Principio de mínimo privilegio  -  Roles vs Users vs Policies

---

## IAM en AWS  -  conceptos fundamentales

| Concepto | Descripción | Cuándo usarlo |
|----------|-------------|---------------|
| **User** | identidad para una persona | acceso humano a la consola / CLI |
| **Group** | conjunto de usuarios con mismos permisos | equipo de devs, ops |
| **Role** | identidad asumible temporalmente | servicios, pipelines, EC2, ECS |
| **Policy** | documento JSON con permisos (Allow/Deny) | adjunta a usuarios, grupos o roles |

**Regla crítica:** los servicios (EC2, ECS, Lambda) usan **Roles**, nunca access keys embebidas.

```
Nunca:  AWS_ACCESS_KEY_ID=AKIA... en .env o en código
Siempre: IAM Role adjunto a la tarea ECS o instancia EC2
```

---

## Principio de mínimo privilegio  -  en práctica

Otorgar solo los permisos estrictamente necesarios para la operación.

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:PutObject"],
      "Resource": "arn:aws:s3:::mensajeria-media/conversations/*"
    },
    {
      "Effect": "Allow",
      "Action": ["secretsmanager:GetSecretValue"],
      "Resource": "arn:aws:secretsmanager:us-east-1:*:secret:mensajeria/*"
    }
  ]
}
```

Este policy da a la API acceso solo a la carpeta `conversations/` del bucket y a los secretos con prefijo `mensajeria/`. No puede leer otros buckets ni otras cuentas.

---

## Errores comunes de IAM

| Antipatrón | Consecuencia | Solución |
|------------|-------------|----------|
| Usar la cuenta **root** para operaciones diarias | compromiso total de la cuenta | crear usuario IAM con permisos limitados |
| Policy con `"Resource": "*"` en producción | cualquier bucket, cualquier secreto | restringir al ARN exacto |
| Access keys en `appsettings.Production.json` | credenciales expuestas si el repo es público | IAM Role para el servicio |
| Un solo usuario IAM para todo el equipo | imposible auditar quién hizo qué | un usuario por persona + CloudTrail |
| Secrets estáticos de larga vida en Azure | riesgo acumulado con el tiempo | Managed Identity |

> En AWS: roles para máquinas. Usuarios para personas. Nunca al revés.

---

<!-- _class: lead -->

# Parte 7
## Costos
### Free Tier  -  Estimación  -  Alertas de Billing  -  Cómo evitar sorpresas

---

## AWS Free Tier  -  qué incluye (12 meses)

| Servicio | Límite Free Tier |
|----------|-----------------|
| EC2 | 750 h/mes de `t2.micro` o `t3.micro` |
| RDS | 750 h/mes de `db.t2.micro` o `db.t3.micro`, 20 GB almacenamiento |
| S3 | 5 GB almacenamiento, 20 000 GET, 2 000 PUT/mes |
| Data transfer | 100 GB salientes/mes |
| Secrets Manager | 30 días de prueba por secreto |

**Importante:** Free Tier no cubre Multi-AZ en RDS. En desarrollo usar single-AZ; en producción pagar Multi-AZ.

El sistema de mensajería en desarrollo cabe en Free Tier. En producción (con Multi-AZ y RDS real) presupuestar ~$80-150/mes.

---

## Modelos de precio EC2  -  ahorrar desde el día 1

| Modelo | Ahorro vs On-Demand | Compromiso | Usar cuando |
|--------|---------------------|------------|-------------|
| On-Demand | referencia (0%) | ninguno | desarrollo, MVPs, staging |
| Reserved Instances 1 año | ~40% | 1 año, tipo fijo | producción estable >3 meses |
| Savings Plans Compute | ~66% | 1-3 años, flexible | reemplazo moderno de Reserved |
| Spot Instances | ~70-90% | pueden interrumpirse | CI runners, batch jobs |

```
Estrategia para el sistema de mensajería:
  - Mes 1-6 (MVP):     todo On-Demand, monitorear con Cost Explorer
  - Mes 6-12:          Savings Plans para producción, Spot para CI
  - Año 2+:            Reserved Instances para RDS (db.t3.medium+)
```

---

## Alertas de billing y recursos ocultos

**Crear budget desde el día 1  -  antes de desplegar cualquier recurso:**

```bash
aws budgets create-budget \
  --account-id 123456789012 \
  --budget '{
    "BudgetName": "mensajeria-monthly",
    "BudgetLimit": {"Amount": "200", "Unit": "USD"},
    "TimeUnit": "MONTHLY",
    "BudgetType": "COST"
  }' \
  --notifications-with-subscribers '[{
    "Notification": {
      "NotificationType": "ACTUAL",
      "ComparisonOperator": "GREATER_THAN",
      "Threshold": 80
    },
    "Subscribers": [{"SubscriptionType": "EMAIL", "Address": "devops@example.com"}]
  }]'
```

**Recursos que generan costo oculto:** Elastic IP sin asignar ($3.65/mes), NAT Gateway sin tráfico ($32/mes), RDS detenida (se reinicia sola a los 7 días y vuelve a cobrar), EBS snapshots acumulados.

---

## Tagging  -  sin tags no hay control de costos

```bash
aws ec2 create-tags \
  --resources i-1234567890 \
  --tags \
    Key=Project,Value=mensajeria \
    Key=Environment,Value=production \
    Key=Owner,Value=acme
```

| Tag | Valores posibles |
|-----|-----------------|
| `Project` | mensajeria, front-template |
| `Environment` | production, staging, development |
| `Owner` | acme, raptor-dev |

Sin tags, Cost Explorer muestra un solo total. Con tags, puedes filtrar por ambiente y saber exactamente cuánto cuesta staging vs producción.

---

<!-- _class: lead -->

# Parte 8
## Acceso Seguro
### Sin credenciales hardcodeadas  -  OIDC para CI/CD  -  Instance Roles

---

## La regla más importante de cloud security

**Nunca hardcodear credenciales. Nunca.**

```
// MAL  -  credenciales en código (se van al repositorio)
var s3Client = new AmazonS3Client(
    "AKIAIOSFODNN7EXAMPLE",
    "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    RegionEndpoint.USEast1
);

// BIEN  -  SDK lee el IAM Role del contenedor/instancia automáticamente
var s3Client = new AmazonS3Client(RegionEndpoint.USEast1);
```

Si la instancia EC2 o tarea ECS tiene un IAM Role adjunto, el SDK de AWS lo detecta y usa sus credenciales temporales automáticamente. No se necesita ninguna key.

---

## OIDC para GitHub Actions  -  sin secrets de larga vida

**El problema:** guardar `AWS_ACCESS_KEY_ID` como secret en GitHub es un secreto estático. Si se filtra, el acceso es permanente hasta rotarlo manualmente.

**La solución:** OIDC (OpenID Connect) permite que GitHub Actions asuma un IAM Role temporalmente sin guardar ninguna credencial.

```yaml
# .github/workflows/deploy.yml
permissions:
  id-token: write   # necesario para OIDC
  contents: read

- name: Configure AWS credentials via OIDC
  uses: aws-actions/configure-aws-credentials@v4
  with:
    role-to-assume: arn:aws:iam::123456789012:role/github-actions-crm
    aws-region: us-east-1
```

GitHub genera un token JWT firmado por GitHub. AWS lo valida y emite credenciales temporales (15 min - 1 h). No hay access keys que gestionar ni rotar.

---

## Flujo completo OIDC GitHub Actions -> AWS

```
GitHub Actions Runner
  |
  |  1. genera JWT firmado por GitHub
  |     (incluye: repo, branch, workflow)
  |

AWS STS (Security Token Service)
  |
  |  2. valida JWT contra GitHub OIDC provider
  |  3. verifica trust policy del Role
  |     (solo este repo puede asumir este role)
  |

Credenciales temporales (expiran en 1 hora)
  |

Pipeline: push a ECR, deploy en ECS/App Service
```

En Azure, el equivalente es **Workload Identity Federation** con el mismo principio: el pipeline asume una identidad temporalmente sin secretos estáticos.

---

## Resumen  -  checklist de seguridad cloud

| Verificar | AWS | Azure |
|-----------|-----|-------|
| No hay access keys en código o appsettings | IAM Role para EC2/ECS | Managed Identity para App Service |
| Secretos en vault centralizado | Secrets Manager | Key Vault |
| Pipeline sin credenciales estáticas | OIDC en GitHub Actions | Workload Identity Federation |
| Base de datos sin acceso público | `--no-publicly-accessible` en RDS | `--public-access Disabled` en Flexible Server |
| Budget alert configurado | AWS Budgets al 80% | Azure Cost Management alerts |
| Tagging de recursos | `Project`, `Environment`, `Owner` | Resource Group + Tags |
| Logs de auditoría activos | CloudTrail | Azure Monitor / Activity Log |

> Este checklist se aplica una sesión posterior (Hardening del proyecto) sobre el sistema de mensajería desplegado.
