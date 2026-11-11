# Glosario Técnico  -  Cloud  -  AWS  -  Azure

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### Access Key (AWS Access Key ID + Secret Access Key)
**Categoría:** AWS
**Definición:** Par de credenciales estáticas (ID público + clave secreta) que autentican llamadas programáticas a la API de AWS. Son de larga vida y deben rotarse manualmente si se filtran. El antipatrón más común es guardarlas en código fuente o en archivos de configuración versionados.
**En el proyecto:** El sistema de mensajería nunca usa access keys en el código ni en `appsettings.json`. La API en EC2 o ECS obtiene permisos a través de un IAM Role adjunto; GitHub Actions los obtiene mediante OIDC.

### ACR  -  Azure Container Registry
**Categoría:** Azure
**Definición:** Registro privado de imágenes Docker en Azure, equivalente a ECR en AWS. Almacena y versiona las imágenes de contenedores que luego se despliegan en App Service o en Azure Kubernetes Service. Soporta autenticación con Managed Identity sin credenciales explícitas.
**En el proyecto:** La imagen de la API .NET del sistema de mensajería se publica en ACR con la etiqueta semántica `miacr.azurecr.io/mensajeria-api:v1.0.0` y se consume desde App Service usando un IAM Role equivalente (Managed Identity con rol `AcrPull`).

### AMI  -  Amazon Machine Image
**Categoría:** AWS
**Definición:** Snapshot de un sistema operativo y su configuración (runtime, dependencias, configuración base) que sirve para lanzar instancias EC2 idénticas. Permite reproducibilidad: una AMI capturada en staging puede lanzarse en producción con exactamente el mismo entorno.
**En el proyecto:** Si el sistema de mensajería se despliega en EC2, la AMI base (Amazon Linux 2023 o Ubuntu) define el SO. En la práctica, se prefiere ECS Fargate con imágenes Docker, que reemplaza la necesidad de gestionar AMIs.

### App Service
**Categoría:** Azure
**Definición:** Plataforma PaaS de Azure para alojar aplicaciones web, APIs REST y contenedores Docker sin gestionar el sistema operativo subyacente. Equivalente funcional a EC2 + gestión del SO, o a AWS Elastic Beanstalk. Soporta escalado automático, SSL gestionado y despliegue continuo desde registros de contenedores.
**En el proyecto:** Alternativa a EC2 para alojar la API .NET 10 del sistema de mensajería en Azure. El plan B2 (2 vCPU / 3.5 GB) es suficiente para staging; P1v3 (2 vCPU / 8 GB) para producción inicial.

### App Service Plan
**Categoría:** Azure
**Definición:** Unidad de capacidad de cómputo que subyace a uno o más App Services. Define la región, el SKU (CPU/RAM) y el sistema operativo. El App Service en sí define la aplicación; el Plan define los recursos físicos que la ejecutan.
**En el proyecto:** El plan `crm-plan` con SKU B2 en Linux aloja la instancia del App Service `mensajeria-api`. Escalar el plan cambia la capacidad de toda la aplicación sin reconfigurar la app.

### ARN  -  Amazon Resource Name
**Categoría:** AWS
**Definición:** Identificador único y global de cualquier recurso en AWS. Tiene el formato `arn:aws:<servicio>:<región>:<cuenta>:<recurso>`. Se usa en políticas IAM para especificar exactamente qué recurso puede ser accedido, evitando el uso de wildcards (`*`) en producción.
**En el proyecto:** Las políticas IAM del sistema de mensajería usan ARNs específicos: `arn:aws:s3:::mensajeria-media/conversations/*` para el bucket de media y `arn:aws:secretsmanager:us-east-1:*:secret:mensajeria/*` para los secretos.

### Automated Backups (RDS)
**Categoría:** AWS
**Definición:** Funcionalidad de RDS que toma snapshots diarios de la base de datos y registra los transaction logs para permitir restauración a cualquier punto en el tiempo (Point-In-Time Recovery) dentro del período de retención configurado (1 a 35 días).
**En el proyecto:** El sistema de mensajería configura un mínimo de 7 días de retención en la instancia RDS de PostgreSQL. Esto permite recuperar la base de datos ante errores de lógica de negocio o corrupción accidental de datos.

### Availability Zone (AZ)
**Categoría:** AWS / Azure
**Definición:** Uno o más data centers físicamente separados dentro de una región, con alimentación eléctrica, redes y refrigeración independientes. Un fallo en una AZ no afecta a las demás. En Azure se llama también "Availability Zone". La distribución de recursos entre AZs elimina los puntos únicos de fallo.
**En el proyecto:** La instancia RDS del sistema de mensajería se despliega en modo Multi-AZ: la instancia principal corre en una AZ y la réplica standby en otra. Si la principal falla, el failover es automático en menos de 2 minutos.

### Azure Blob Storage
**Categoría:** Azure
**Definición:** Servicio de almacenamiento de objetos de Azure, equivalente a S3 en AWS. Organiza los datos en cuentas de almacenamiento y contenedores (equivalentes a buckets de S3). Soporta tres tipos de blobs: Block Blobs (archivos), Append Blobs (logs) y Page Blobs (discos virtuales).
**En el proyecto:** El contenedor `media` dentro de la cuenta `mensajeriastorage` almacena los adjuntos de conversaciones del sistema de mensajería en el escenario Azure, con la misma lógica de pre-signed URLs que se usa con S3.

### Azure Budgets / Azure Cost Management
**Categoría:** Azure
**Definición:** Herramientas de Azure para monitorear el gasto, establecer límites de presupuesto y configurar alertas automáticas cuando el costo supera un umbral definido. Equivalente a AWS Budgets. Permite filtrar costos por Resource Group, suscripción o etiqueta.
**En el proyecto:** Se configura una alerta de costo para el Resource Group `crm-rg` desde el primer despliegue, notificando al equipo cuando el gasto mensual supera el 80% del presupuesto asignado.

### Azure CDN / Front Door
**Categoría:** Azure
**Definición:** Servicios de distribución de contenido (CDN) de Azure. Azure CDN sirve assets estáticos desde nodos de borde globales; Azure Front Door añade balanceo de carga global, terminación SSL y reglas de enrutamiento. Equivalente a CloudFront en AWS.
**En el proyecto:** El frontend del sistema de mensajería (build de React/Vite) puede servirse desde Azure CDN o Front Door para reducir la latencia de carga de los assets estáticos para los usuarios finales de la empresa.

### Azure DNS
**Categoría:** Azure
**Definición:** Servicio de DNS gestionado de Azure que aloja zonas DNS y gestiona los registros A, CNAME, TXT y MX. Equivalente a Route 53 en AWS. Integra con otros servicios Azure para actualización automática de registros al escalar o redirigir tráfico.
**En el proyecto:** En el escenario Azure, el dominio `mensajeria.io` se configura en Azure DNS apuntando al App Service o a Azure Front Door, equivalente al rol que Route 53 cumple en el escenario AWS.

### Azure Monitor / Activity Log
**Categoría:** Azure
**Definición:** Plataforma unificada de observabilidad de Azure que centraliza métricas, logs y alertas de todos los servicios. El Activity Log registra todas las operaciones de plano de control (quién creó, modificó o eliminó qué recurso y cuándo), equivalente a CloudTrail en AWS.
**En el proyecto:** En el hardening del sistema de mensajería, Azure Monitor se configura para registrar toda actividad en el Resource Group `crm-rg` y alertar sobre operaciones anómalas como eliminación de secretos en Key Vault.

### Azure Service Bus / Storage Queues
**Categoría:** Azure
**Definición:** Servicios de mensajería asíncrona de Azure. Storage Queues es simple y económico; Service Bus es más avanzado, soporta tópicos, suscripciones y patrones pub/sub. Equivalentes a SQS (y SNS) en AWS.
**En el proyecto:** Si el sistema de mensajería implementa procesamiento asíncrono de mensajes entrantes de WhatsApp o Instagram en Azure, Service Bus desacopla la recepción del webhook de su procesamiento por el BackgroundService.

### Azure SignalR Service
**Categoría:** Azure
**Definición:** Servicio gestionado de Azure que abstrae la infraestructura de WebSockets y conexiones en tiempo real, compatible con la librería SignalR de .NET. Equivalente aproximado a API Gateway + WebSockets en AWS. Gestiona el escalado de conexiones concurrentes sin que la aplicación gestione el estado de las conexiones.
**En el proyecto:** Alternativa al backplane Redis + SignalR autogestionado para las notificaciones en tiempo real del sistema de mensajería (mensajes entrantes en la bandeja de conversaciones).

---

## B

### Blob Storage
**Categoría:** Azure
**Definición:** Ver "Azure Blob Storage". El término "blob" (Binary Large Object) designa cualquier tipo de dato binario o de texto almacenado como objeto. En Azure, el servicio completo se llama Azure Blob Storage y es la solución principal de almacenamiento de objetos no estructurados.
**En el proyecto:** Los archivos adjuntos (imágenes, PDFs, audios) enviados en conversaciones del sistema de mensajería se almacenan como blobs en el contenedor `media` de la cuenta de almacenamiento.

### Budget (AWS Budgets)
**Categoría:** AWS
**Definición:** Herramienta de AWS que permite definir límites de gasto mensual o por período y enviar notificaciones automáticas cuando el gasto real o previsto supera un umbral. Se configura antes de desplegar cualquier recurso productivo para evitar sorpresas en la factura.
**En el proyecto:** El sistema de mensajería configura un budget de $200 USD mensuales con alerta al 80% ($160) enviada al correo `devops@example.com`. Esto cubre la estimación de ~$80-150/mes del escenario de producción inicial.

---

## C

### CDN  -  Content Delivery Network
**Categoría:** Cloud
**Definición:** Red de servidores distribuidos globalmente (edge locations) que almacenan en caché copias de contenido estático (imágenes, CSS, JavaScript, videos) cerca de los usuarios finales para reducir la latencia. En AWS el servicio es CloudFront; en Azure, Azure CDN o Front Door.
**En el proyecto:** El build de React/Vite del sistema de mensajería puede servirse desde CloudFront (AWS) o Azure CDN para que los agentes de la empresa en distintas ubicaciones carguen la UI rápidamente.

### CloudFront
**Categoría:** AWS
**Definición:** CDN de AWS que distribuye contenido desde edge locations globales. Soporta cacheo de respuestas de API, terminación SSL, headers de seguridad (CSP, HSTS) y funciones Lambda@Edge para lógica en el borde. Equivalente a Azure CDN / Front Door.
**En el proyecto:** En el escenario AWS, el frontend del sistema de mensajería (build estático en S3) se sirve a través de CloudFront, habilitando HTTPS con certificado gratuito de ACM y compresión gzip automática.

### CloudTrail
**Categoría:** AWS
**Definición:** Servicio de auditoría de AWS que registra cada llamada a la API de AWS realizada en la cuenta: quién la hizo, desde dónde, qué recurso afectó y cuándo. Los logs se almacenan en S3 y son fundamentales para investigar incidentes de seguridad o cambios no autorizados.
**En el proyecto:** En el checklist de hardening del sistema de mensajería, CloudTrail debe estar activo en la región `us-east-1` para registrar todas las operaciones sobre IAM, S3, RDS y Secrets Manager.

### Cost Explorer
**Categoría:** AWS
**Definición:** Herramienta visual de AWS para analizar y proyectar el gasto en la nube. Permite filtrar por servicio, región, cuenta o etiqueta (tag), ver tendencias históricas y detectar picos de costo inesperados. Se habilita por cuenta y los datos están disponibles con retardo de hasta 24 horas.
**En el proyecto:** Durante los primeros 6 meses del sistema de mensajería en producción (fase MVP On-Demand), Cost Explorer se usa semanalmente para validar que el gasto esté dentro de la estimación y decidir cuándo migrar a Savings Plans.

---

## D

### DefaultAzureCredential
**Categoría:** Azure
**Definición:** Clase del SDK de Azure para .NET que resuelve automáticamente las credenciales del contexto en ejecución: en producción usa la Managed Identity del servicio (App Service, AKS), en desarrollo local usa `az login`, y en CI/CD usa variables de entorno o un Service Principal. Elimina la necesidad de código diferente por ambiente.
**En el proyecto:** `builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential())` en `Program.cs` lee los secretos del Key Vault en producción usando la Managed Identity del App Service, sin ninguna credencial en el código fuente.

### DNS  -  Domain Name System
**Categoría:** Cloud
**Definición:** Sistema jerárquico que traduce nombres de dominio legibles (por ejemplo, `mensajeria.io`) en direcciones IP numéricas que los servidores pueden enrutar. En cloud, los servicios DNS gestionados (Route 53, Azure DNS) permiten registrar dominios, gestionar registros y enrutar tráfico con lógica de failover o latencia.
**En el proyecto:** Route 53 (AWS) o Azure DNS (Azure) apunta el dominio `mensajeria.io` al load balancer de la API del sistema de mensajería y, opcionalmente, a un registro separado para el frontend estático.

---

## E

### EBS  -  Elastic Block Store
**Categoría:** AWS
**Definición:** Servicio de almacenamiento de bloques persistente de AWS que se adjunta a instancias EC2. A diferencia del Instance Store, los volúmenes EBS sobreviven a los reinicios e incluso pueden desconectarse y reconectarse a otra instancia. Es el disco virtual principal de las instancias EC2.
**En el proyecto:** Si la API del sistema de mensajería corre en EC2, el sistema operativo y los binarios de la aplicación residen en un volumen EBS. Los logs de la aplicación también se almacenan aquí antes de enviarse a CloudWatch o a un sistema de observabilidad.

### EC2  -  Elastic Compute Cloud
**Categoría:** AWS
**Definición:** Servicio IaaS de AWS que provee máquinas virtuales (instancias) bajo demanda. El usuario elige el tipo de instancia (CPU, RAM, red), el sistema operativo (mediante una AMI) y gestiona el SO, el runtime y la aplicación. Equivalente a Azure Virtual Machines.
**En el proyecto:** El tipo de instancia `t3.medium` (2 vCPU / 4 GB RAM) es suficiente para la API .NET 10 del sistema de mensajería en producción inicial. En arquitecturas más avanzadas, ECS Fargate reemplaza a EC2 para eliminar la gestión del SO.

### ECS Fargate
**Categoría:** AWS
**Definición:** Modalidad serverless del servicio de contenedores ECS (Elastic Container Service) de AWS. Ejecuta contenedores Docker sin necesidad de gestionar instancias EC2 subyacentes. AWS provisiona, escala y parchetea la infraestructura de cómputo de forma transparente.
**En el proyecto:** ECS Fargate es la forma preferida de desplegar la API .NET del sistema de mensajería en AWS: se define una "task definition" con la imagen Docker del ECR y Fargate gestiona dónde y cómo correrla, con IAM Role adjunto a la tarea.

### ECR  -  Elastic Container Registry
**Categoría:** AWS
**Definición:** Registro privado de imágenes Docker gestionado de AWS. Integrado con IAM para control de acceso, con ECS/EKS para despliegues y con GitHub Actions para pipelines CI/CD. Equivalente a Azure Container Registry (ACR).
**En el proyecto:** Las imágenes Docker de la API .NET 10 del sistema de mensajería se publican en ECR desde el pipeline de GitHub Actions y se referencian en las task definitions de ECS Fargate para despliegue.

### Edge Location
**Categoría:** AWS
**Definición:** Nodo de infraestructura de AWS (servidor de caché y enrutamiento) ubicado en ciudades de todo el mundo para servir contenido de CloudFront con mínima latencia. Hay más edge locations que regiones: permiten cachear respuestas cerca del usuario final sin correr computación ahí.
**En el proyecto:** El build estático del frontend del sistema de mensajería (React/Vite) servido desde CloudFront llega al navegador del agente desde el edge location más cercano a su ubicación, no desde `us-east-1`.

### Elastic Beanstalk
**Categoría:** AWS
**Definición:** Plataforma PaaS de AWS que simplifica el despliegue de aplicaciones web gestionando automáticamente el balanceo de carga, el escalado automático y la salud de las instancias. El desarrollador solo sube el código o la imagen Docker. Equivalente aproximado a App Service en Azure.
**En el proyecto:** Mencionado en la tabla comparativa como alternativa a ECS Fargate para el despliegue de la API del sistema de mensajería. En la práctica del curso se prefiere ECS Fargate por su mayor control y portabilidad.

### Encryption at Rest
**Categoría:** Cloud
**Definición:** Cifrado de datos almacenados en disco. En AWS RDS se activa con un parámetro al crear la instancia (`--storage-encrypted`); en Azure Flexible Server está habilitado por defecto. Protege los datos si el medio físico de almacenamiento es comprometido.
**En el proyecto:** La instancia RDS del sistema de mensajería tiene encryption at rest activado, protegiendo los datos de conversaciones, contactos y mensajes de la empresa almacenados en PostgreSQL.

### Entra ID (Azure Entra ID, antes Azure Active Directory)
**Categoría:** Azure
**Definición:** Servicio de identidad y acceso de Microsoft Azure. Gestiona usuarios, grupos, aplicaciones y permisos. En el contexto de cloud, provee RBAC (Role-Based Access Control), Managed Identities y Service Principals. Equivalente funcional a IAM en AWS para identidad humana y de servicios.
**En el proyecto:** En el escenario Azure del sistema de mensajería, Entra ID gestiona la Managed Identity del App Service (para acceder a Key Vault y ACR sin credenciales) y los roles de acceso RBAC a los recursos del Resource Group `crm-rg`.

---

## F

### Failover (Multi-AZ)
**Categoría:** AWS / Azure
**Definición:** Proceso automático por el cual, ante un fallo de la instancia de base de datos principal, el sistema promueve la réplica standby como nueva instancia principal. En RDS Multi-AZ, el failover ocurre típicamente en menos de 2 minutos sin intervención manual, con actualización automática del endpoint DNS.
**En el proyecto:** La instancia RDS PostgreSQL del sistema de mensajería tiene Multi-AZ activado. Si el data center de la AZ principal falla, el failover automático preserva la disponibilidad del servicio para los agentes de la empresa con mínima interrupción.

### Free Tier (AWS)
**Categoría:** AWS
**Definición:** Capa gratuita de AWS disponible durante 12 meses desde la creación de la cuenta, que incluye cuotas de uso sin costo para servicios como EC2 (`t2.micro`/`t3.micro` 750h/mes), RDS (750h/mes), S3 (5 GB) y otros. No cubre todas las configuraciones: Multi-AZ en RDS, por ejemplo, no está incluido.
**En el proyecto:** El ambiente de desarrollo del sistema de mensajería puede mantenerse dentro del Free Tier usando instancias `t3.micro` para EC2 y RDS con single-AZ. Para el ambiente de producción con Multi-AZ se estiman $80-150/mes adicionales.

---

## G

### GitHub Actions OIDC
**Categoría:** Cloud / AWS / Azure
**Definición:** Ver "OIDC  -  OpenID Connect". En el contexto específico de GitHub Actions, OIDC permite que el runner del pipeline solicite credenciales temporales de AWS (o Azure) sin almacenar ningún secret estático en el repositorio. GitHub actúa como Identity Provider y AWS STS valida el token emitido.
**En el proyecto:** El pipeline de despliegue del sistema de mensajería en `.github/workflows/deploy.yml` usa `aws-actions/configure-aws-credentials@v4` con `role-to-assume` en lugar de `AWS_ACCESS_KEY_ID` y `AWS_SECRET_ACCESS_KEY` como secrets del repositorio.

---

## H

### Hardcoding de credenciales
**Categoría:** Cloud
**Definición:** Antipatrón que consiste en escribir credenciales (access keys, contraseñas, tokens) directamente en el código fuente o en archivos de configuración versionados. Es el error de seguridad cloud más común y uno de los más graves: si el repositorio es público o es comprometido, las credenciales quedan expuestas permanentemente hasta que se roten.
**En el proyecto:** El sistema de mensajería prohíbe explícitamente hardcoding. La API obtiene permisos a través de IAM Roles; los secretos de conexión y JWT residen en Secrets Manager (AWS) o Key Vault (Azure), nunca en `appsettings.Production.json`.

### HSTS  -  HTTP Strict Transport Security
**Categoría:** Cloud
**Definición:** Header HTTP que instruye al navegador a conectarse únicamente por HTTPS al dominio por un período de tiempo determinado. Previene ataques de downgrade a HTTP. Se configura en el load balancer, CloudFront o App Service como parte del hardening de seguridad web.
**En el proyecto:** El load balancer o CloudFront del sistema de mensajería añade el header `Strict-Transport-Security: max-age=31536000; includeSubDomains` a todas las respuestas, protegiendo las sesiones de los agentes de la empresa.

---

## I

### IaaS  -  Infrastructure as a Service
**Categoría:** Cloud
**Definición:** Modelo de servicio cloud en el que el proveedor gestiona el hardware, la red y la virtualización, mientras el cliente gestiona el sistema operativo, el runtime, la aplicación y los datos. Ofrece máximo control pero mayor responsabilidad operacional. Ejemplos: EC2, Azure Virtual Machines.
**En el proyecto:** EC2 es la opción IaaS para el sistema de mensajería. Se elige cuando se necesita control total del SO (por ejemplo, para configurar parámetros del kernel o instalar software específico), pero implica gestionar parches, seguridad del SO y actualizaciones de runtime.

### IAM  -  Identity and Access Management
**Categoría:** AWS
**Definición:** Sistema de control de acceso de AWS. Gestiona usuarios (identidades humanas), grupos, roles (identidades asumibles por servicios o pipelines) y políticas (documentos JSON que definen permisos Allow/Deny sobre recursos específicos). Es la capa de autorización de toda la plataforma AWS.
**En el proyecto:** En el sistema de mensajería, la API en ECS Fargate asume un IAM Role con permisos mínimos: `s3:GetObject` y `s3:PutObject` solo en `mensajeria-media/conversations/*`, y `secretsmanager:GetSecretValue` solo en `mensajeria/*`.

### IAM Group
**Categoría:** AWS
**Definición:** Colección de usuarios IAM que comparten el mismo conjunto de políticas. Simplifica la gestión de permisos: en lugar de adjuntar políticas usuario por usuario, se adjuntan al grupo y todos los miembros heredan los permisos. No puede contener otros grupos.
**En el proyecto:** El equipo puede tener grupos IAM como `crm-developers` (acceso a ECR y CloudWatch) y `crm-ops` (acceso a RDS y Secrets Manager), separando claramente los permisos por rol funcional.

### IAM Policy
**Categoría:** AWS
**Definición:** Documento JSON que define permisos de AWS mediante declaraciones `Effect` (Allow/Deny), `Action` (qué operaciones), `Resource` (sobre qué ARN) y opcionalmente `Condition`. Se adjunta a usuarios, grupos o roles para otorgar o denegar acceso. La política más restrictiva siempre gana.
**En el proyecto:** La política IAM del sistema de mensajería restringe el acceso de la API al bucket `mensajeria-media` solo a la subcarpeta `conversations/` y a los secretos con prefijo `mensajeria/`, siguiendo el principio de mínimo privilegio.

### IAM Role
**Categoría:** AWS
**Definición:** Identidad de AWS que puede ser asumida temporalmente por servicios, aplicaciones o usuarios. A diferencia de los usuarios, no tiene credenciales permanentes: genera credenciales temporales al ser asumido. Es la forma correcta de otorgar permisos a servicios como EC2, ECS y Lambda.
**En el proyecto:** La tarea ECS del sistema de mensajería tiene un IAM Role adjunto. El SDK de AWS lo detecta automáticamente: `new AmazonS3Client(RegionEndpoint.USEast1)` sin credenciales explícitas funciona porque el Role del contenedor tiene los permisos necesarios.

### IAM User
**Categoría:** AWS
**Definición:** Identidad permanente en AWS para una persona o aplicación. Tiene credenciales de larga vida (contraseña para consola, access keys para CLI/SDK). Se recomienda un usuario por persona para auditoría, y nunca usar el mismo usuario para múltiples personas del equipo.
**En el proyecto:** Cada miembro del equipo que acceda a la consola de AWS debe tener su propio usuario IAM. La cuenta root se usa solo para la configuración inicial y luego se desactiva el acceso programático de root.

### Instance Store
**Categoría:** AWS
**Definición:** Almacenamiento efímero adjunto físicamente a la instancia EC2. A diferencia de EBS, los datos en Instance Store se pierden cuando la instancia se detiene, se reinicia o falla. Tiene mayor velocidad de I/O que EBS, por lo que es útil para caché temporal, buffers y datos que pueden reconstruirse.
**En el proyecto:** El sistema de mensajería no usa Instance Store para ningún dato persistente. Los logs, uploads temporales y cualquier dato que deba sobrevivir un reinicio residen en EBS o directamente en S3 / RDS.

---

## K

### Key Vault (Azure Key Vault)
**Categoría:** Azure
**Definición:** Servicio de Azure para almacenar y acceder de forma segura a secretos, claves de cifrado y certificados. Equivalente a Secrets Manager en AWS. Soporta versionado de secretos, auditoría de accesos y rotación automática. El acceso se controla mediante políticas o RBAC de Entra ID.
**En el proyecto:** `crm-keyvault` almacena el connection string de PostgreSQL, el secreto JWT y las API keys de WhatsApp/Instagram para el sistema de mensajería en Azure. La aplicación lee estos valores al iniciar usando `DefaultAzureCredential` sin exponer credenciales en el código.

---

## L

### Lambda@Edge
**Categoría:** AWS
**Definición:** Funcionalidad de CloudFront que permite ejecutar funciones Lambda en las edge locations, cerca del usuario final, para manipular requests y responses HTTP antes o después del cacheo. Útil para autenticación, redirecciones y personalización de contenido sin roundtrip al servidor origen.
**En el proyecto:** Mencionado en el contexto de CloudFront como capacidad disponible. Para el sistema de mensajería, Lambda@Edge podría usarse para agregar headers de seguridad (CSP, HSTS) en el frontend estático servido desde CloudFront.

### Load Balancer
**Categoría:** AWS / Azure
**Definición:** Componente que distribuye el tráfico de red entrante entre múltiples instancias de la aplicación para mejorar disponibilidad y escalabilidad. En AWS, el Application Load Balancer (ALB) opera en capa 7 (HTTP/HTTPS). Es el único recurso con IP pública en una arquitectura segura; las instancias de la aplicación permanecen en la red privada (VPC).
**En el proyecto:** El load balancer del sistema de mensajería termina HTTPS en el puerto 443 y distribuye el tráfico a las instancias EC2 o tareas ECS Fargate que ejecutan la API .NET. Es el punto de entrada único y tiene la única IP pública de la arquitectura.

---

## M

### Managed Identity (Azure)
**Categoría:** Azure
**Definición:** Identidad gestionada automáticamente por Azure y asignada a un servicio (App Service, AKS, Function App). Equivalente a IAM Role en AWS. El servicio puede autenticarse ante otros recursos de Azure (Key Vault, ACR, Storage) sin credenciales explícitas: Azure rota las credenciales subyacentes de forma transparente.
**En el proyecto:** El App Service `mensajeria-api` tiene una Managed Identity asignada con políticas de acceso a `crm-keyvault` (lectura de secretos) y rol `AcrPull` sobre el ACR. Cero credenciales en código o variables de entorno.

### Mínimo Privilegio (Principio de)
**Categoría:** Cloud
**Definición:** Principio de seguridad que establece que cada entidad (usuario, servicio, pipeline) debe tener únicamente los permisos estrictamente necesarios para realizar su función, ni más ni menos. Limita el radio de impacto ante un compromiso: si una credencial se filtra, solo puede afectar los recursos para los que tiene permiso.
**En el proyecto:** La política IAM de la API del sistema de mensajería permite `s3:GetObject` y `s3:PutObject` solo en `mensajeria-media/conversations/*`. No puede leer otros buckets, borrar objetos ni acceder a secretos de otros proyectos.

### Multi-AZ (RDS)
**Categoría:** AWS
**Definición:** Configuración de alta disponibilidad de RDS en la que AWS mantiene una réplica standby sincrónica en una Availability Zone diferente a la instancia principal. En caso de fallo de la instancia primaria, el failover es automático y transparente para la aplicación (el endpoint DNS se actualiza solo). No sirve para escalar lecturas; para eso existe Read Replicas.
**En el proyecto:** El sistema de mensajería activa Multi-AZ en producción (`--multi-az` en el comando de creación RDS). En desarrollo se usa single-AZ para mantenerse dentro del Free Tier o reducir costos.

---

## N

### NAT Gateway
**Categoría:** AWS
**Definición:** Componente de red que permite a instancias en subredes privadas (sin IP pública) acceder a Internet para descargar actualizaciones, llamar APIs externas o conectarse a servicios AWS, sin exponer las instancias a conexiones entrantes. Tiene un costo fijo por hora (~$32/mes) independientemente del tráfico.
**En el proyecto:** Mencionado como recurso de costo oculto: un NAT Gateway sin tráfico activo sigue cobrando ~$32/mes. En el sistema de mensajería, si las instancias EC2 están en subnets privadas, se necesita un NAT Gateway para que la API pueda llamar a la API de WhatsApp o Instagram.

---

## O

### OIDC  -  OpenID Connect
**Categoría:** Cloud
**Definición:** Protocolo de autenticación basado en OAuth 2.0 que permite a una entidad (como GitHub Actions) probar su identidad ante otro sistema (como AWS STS) mediante un JWT firmado digitalmente, sin intercambiar credenciales estáticas. AWS y Azure configuran un trust relationship con el Identity Provider (GitHub) para validar estos tokens.
**En el proyecto:** GitHub Actions usa OIDC para asumir el role `github-actions-crm` en AWS al desplegar el sistema de mensajería. El token JWT contiene el nombre del repositorio y la rama, permitiendo restringir qué workflows pueden asumir el role.

### On-Demand (EC2)
**Categoría:** AWS
**Definición:** Modelo de precio de EC2 en el que se paga por hora o por segundo de uso sin compromisos ni pagos anticipados. Es el precio de referencia (más caro) pero ofrece máxima flexibilidad. Recomendado para ambientes de desarrollo, MVPs y carga variable sin patrón predecible.
**En el proyecto:** Durante los primeros 6 meses del sistema de mensajería en producción (fase MVP), toda la infraestructura corre en On-Demand para mantener flexibilidad y monitorear el patrón de uso real antes de comprometerse con Reserved Instances o Savings Plans.

### On-Premise
**Categoría:** Cloud
**Definición:** Modelo en el que la infraestructura (servidores, red, almacenamiento) está físicamente en las instalaciones de la organización. El equipo gestiona absolutamente todo: hardware, electricidad, cooling, SO, runtime y aplicación. Es el extremo opuesto al modelo SaaS en la pirámide de responsabilidad.
**En el proyecto:** El modelo on-premise es el antipatrón de referencia para el sistema de mensajería: instalar y administrar un servidor físico en las oficinas de la empresa implicaría responsabilidad de hardware, parches y disponibilidad que el equipo de desarrollo no debería asumir.

---

## P

### PaaS  -  Platform as a Service
**Categoría:** Cloud
**Definición:** Modelo de servicio cloud en el que el proveedor gestiona el hardware, la red, la virtualización, el sistema operativo y el runtime. El cliente solo gestiona la aplicación y los datos. Elimina el "toil operacional" de parches, actualizaciones de SO y gestión de hipervisores. Ejemplos: RDS, App Service, S3.
**En el proyecto:** El sistema de mensajería prioriza PaaS donde sea posible: RDS en lugar de PostgreSQL en EC2, App Service en lugar de VM en Azure, S3/Blob Storage para objetos en lugar de un servidor de archivos propio.

### Parameter Store (AWS SSM Parameter Store)
**Categoría:** AWS
**Definición:** Servicio de AWS Systems Manager para almacenar parámetros de configuración y secretos. Más simple y económico que Secrets Manager pero con menos funcionalidades (sin rotación automática ni versionado avanzado). Equivalente simplificado a Secrets Manager para configuraciones no críticas.
**En el proyecto:** Mencionado en la tabla comparativa junto a Secrets Manager como opciones de AWS para almacenar secretos. El sistema de mensajería usa Secrets Manager (no Parameter Store) por sus capacidades de rotación y auditoría más robustas.

### PostgreSQL Flexible Server (Azure)
**Categoría:** Azure
**Definición:** Servicio PaaS de Azure para bases de datos PostgreSQL. Equivalente a RDS PostgreSQL en AWS. "Flexible Server" es la generación actual del servicio (más configurable que el obsoleto "Single Server"). AWS gestiona los backups, parches y disponibilidad; el desarrollador solo escribe SQL.
**En el proyecto:** `mensajeria-db` como PostgreSQL Flexible Server en Azure es la base de datos gestionada del sistema de mensajería en el escenario Azure. Se crea con `--public-access Disabled` para que solo el App Service en la misma red privada pueda conectarse.

### Pre-signed URL
**Categoría:** AWS / Azure
**Definición:** URL firmada temporalmente que otorga acceso directo a un objeto en S3 (o Azure Blob Storage) a cualquier portador, sin necesidad de credenciales AWS. La API genera la URL con un tiempo de expiración (por ejemplo, 5 minutos) y el cliente sube o descarga el archivo directamente al almacenamiento, sin que el tráfico binario pase por el servidor de la aplicación.
**En el proyecto:** El endpoint `POST /api/v1/media/upload-url` del sistema de mensajería genera una pre-signed URL con `HttpVerb.PUT` y 5 minutos de vigencia. El frontend la recibe y hace un PUT directo a S3, evitando que la API .NET procese binarios y reduciendo el ancho de banda del servidor.

---

## R

### RBAC  -  Role-Based Access Control
**Categoría:** Cloud
**Definición:** Modelo de control de acceso donde los permisos se asignan a roles (no directamente a usuarios) y los usuarios o servicios asumen roles. En Azure, RBAC es el sistema de autorización primario de Entra ID: roles predefinidos (Owner, Contributor, Reader) o roles personalizados se asignan sobre scopes (suscripción, Resource Group, recurso individual).
**En el proyecto:** En Azure, el pipeline de GitHub Actions recibe un RBAC Role Assignment de `AcrPull` sobre el ACR y `Key Vault Secrets User` sobre el Key Vault, con permisos exactos y sin acceso a otros recursos de la suscripción.

### RDS  -  Relational Database Service
**Categoría:** AWS
**Definición:** Servicio PaaS de AWS para bases de datos relacionales gestionadas. Soporta PostgreSQL, MySQL, MariaDB, Oracle y SQL Server. AWS gestiona backups automáticos, parches del motor de base de datos, failover Multi-AZ y escalado de almacenamiento. El desarrollador solo administra el esquema y los datos.
**En el proyecto:** La instancia `mensajeria-db` en RDS PostgreSQL 16.3 (`db.t3.micro` en Free Tier, `db.t3.medium` en producción) almacena todos los datos del CRM: conversaciones, contactos, mensajes, usuarios y configuración de workspace de la empresa.

### Reserved Instances (EC2)
**Categoría:** AWS
**Definición:** Modelo de precio de EC2 en el que se compromete el uso de un tipo de instancia específico por 1 o 3 años a cambio de un descuento de hasta ~40% respecto al precio On-Demand. El compromiso es de tipo de instancia y región; no se puede cambiar sin penalización. Recomendado para carga de producción estable con patrón predecible.
**En el proyecto:** A partir del año 2 del sistema de mensajería en producción, se recomienda Reserved Instances para la instancia RDS (`db.t3.medium` o superior) que corre continuamente, optimizando el costo del componente más estable de la arquitectura.

### Resource Group (Azure)
**Categoría:** Azure
**Definición:** Contenedor lógico en Azure que agrupa recursos relacionados (App Service, base de datos, Blob Storage, Key Vault) para gestionarlos, facturarlos y controlarlos juntos. No tiene equivalente directo en AWS (en AWS los recursos se agrupan conceptualmente con tags). Permite eliminar todos los recursos del grupo con un solo comando.
**En el proyecto:** El Resource Group `crm-rg` contiene todos los recursos del sistema de mensajería en Azure: App Service, PostgreSQL Flexible Server, cuenta de Blob Storage, Key Vault y ACR. Facilita la gestión de costos y la limpieza del ambiente.

### Route 53
**Categoría:** AWS
**Definición:** Servicio de DNS gestionado de AWS. Permite registrar dominios, gestionar zonas DNS, configurar registros A, CNAME, ALIAS y más, y añadir lógica de enrutamiento (latency-based routing, failover routing, geolocation routing). Equivalente a Azure DNS.
**En el proyecto:** Route 53 registra el dominio `mensajeria.io` y apunta el registro A (o ALIAS) al Application Load Balancer de la API del sistema de mensajería, habilitando HTTPS con el certificado de ACM (AWS Certificate Manager).

---

## S

### S3  -  Simple Storage Service
**Categoría:** AWS
**Definición:** Servicio de almacenamiento de objetos de AWS. Los datos se organizan en buckets (contenedores) y objetos (archivos referenciados por una clave). Diseñado para durabilidad del 99.999999999% (11 nueves). Soporta versionado, lifecycle policies, replicación entre regiones y clases de almacenamiento para optimizar costos.
**En el proyecto:** El bucket `mensajeria-media` almacena los archivos adjuntos de conversaciones (imágenes, PDFs, audios) del sistema de mensajería. El frontend los sube directamente mediante pre-signed URLs; el build estático de React/Vite también puede hostearse en S3 servido por CloudFront.

### S3 Glacier Instant Retrieval
**Categoría:** AWS
**Definición:** Clase de almacenamiento de S3 para datos accedidos raramente (menos de una vez al trimestre) que requieren recuperación inmediata cuando se necesitan. Costo de almacenamiento mucho menor que S3 Standard, con costo adicional por recuperación. Ideal para archivos históricos que se consultan ocasionalmente.
**En el proyecto:** El historial de conversaciones del sistema de mensajería con más de 1 año de antigüedad se migra automáticamente a S3 Glacier Instant Retrieval mediante una lifecycle policy del bucket, reduciendo el costo de almacenamiento a largo plazo.

### S3 Standard
**Categoría:** AWS
**Definición:** Clase de almacenamiento predeterminada de S3, optimizada para acceso frecuente. Ofrece alta durabilidad, disponibilidad y baja latencia. Es la clase más cara por GB almacenado pero más económica en costo de recuperación. Adecuada para datos activos y de acceso regular.
**En el proyecto:** Los adjuntos activos (imágenes y archivos de conversaciones recientes) y los assets del frontend del sistema de mensajería residen en S3 Standard, donde la latencia de acceso y la disponibilidad son prioritarias.

### S3 Standard-IA (Infrequent Access)
**Categoría:** AWS
**Definición:** Clase de almacenamiento de S3 para datos que se acceden con poca frecuencia pero que deben estar disponibles inmediatamente cuando se necesitan. Costo de almacenamiento menor que S3 Standard, con un cargo adicional por recuperación. Ideal para backups y datos de referencia no activos.
**En el proyecto:** Los backups semanales de la base de datos del sistema de mensajería (snapshots exportados de RDS) se almacenan en S3 Standard-IA, equilibrando el costo de almacenamiento con la disponibilidad ante necesidades de recuperación de desastres.

### SaaS  -  Software as a Service
**Categoría:** Cloud
**Definición:** Modelo de servicio cloud en el que el proveedor gestiona absolutamente todo (hardware, SO, runtime, aplicación) y el cliente solo configura y usa el servicio. El cliente no tiene acceso al código subyacente ni a la infraestructura. Ejemplos: GitHub, Stripe, Google Workspace.
**En el proyecto:** El sistema de mensajería consume servicios SaaS de terceros: la API de WhatsApp Business, Stripe para billing y GitHub para control de versiones y CI/CD. El equipo solo los configura y usa, sin gestionar su infraestructura.

### Savings Plans (AWS)
**Categoría:** AWS
**Definición:** Modelo de precio flexible de AWS que ofrece descuentos de hasta ~66% a cambio de un compromiso de gasto en dólares por hora durante 1 o 3 años, aplicable a cualquier tipo de instancia EC2 o uso de Fargate en cualquier región. Más flexible que Reserved Instances: el descuento se aplica automáticamente sin especificar el tipo exacto de instancia.
**En el proyecto:** A partir del mes 6 del sistema de mensajería en producción, se recomienda migrar los recursos de cómputo (EC2 / ECS Fargate) a Savings Plans Compute para aprovechar el ~66% de descuento manteniendo flexibilidad para cambiar tipos de instancia.

### Secrets Manager (AWS Secrets Manager)
**Categoría:** AWS
**Definición:** Servicio de AWS para almacenar, rotar y auditar secretos como contraseñas de bases de datos, API keys y claves JWT. Soporta rotación automática (integrado con RDS) y versionado de secretos. El acceso se controla mediante IAM Policies. Equivalente a Key Vault en Azure.
**En el proyecto:** El secreto `mensajeria/production` en Secrets Manager almacena el connection string de PostgreSQL, el secreto JWT y las API keys de WhatsApp. La API .NET los carga al iniciar mediante `AddSecretsManager` en `Program.cs`, usando el IAM Role del contenedor para autenticarse.

### Security Group
**Categoría:** AWS
**Definición:** Firewall virtual a nivel de instancia (EC2) o servicio (RDS, ECS) que controla el tráfico de red entrante y saliente mediante reglas de protocolo, puerto y origen/destino. Por defecto deniega todo el tráfico; solo pasa lo explícitamente permitido. Es stateful: si el tráfico de entrada es permitido, la respuesta saliente es automáticamente permitida.
**En el proyecto:** El sistema de mensajería usa dos security groups: `sg-api` (permite TCP 443 desde `0.0.0.0/0`) y `sg-db` (permite TCP 5432 solo desde `sg-api`). La base de datos nunca es accesible desde Internet, solo desde la API.

### Service Principal (Azure)
**Categoría:** Azure
**Definición:** Identidad de aplicación en Entra ID equivalente a un IAM User para aplicaciones en AWS. Se usa cuando una aplicación externa (por ejemplo, un pipeline de CI/CD en GitHub Actions con Workload Identity Federation) necesita autenticarse ante Azure. Tiene credenciales (certificado o secreto de cliente) con fecha de expiración.
**En el proyecto:** En el escenario Azure, el pipeline de GitHub Actions puede usar un Service Principal (con Workload Identity Federation para evitar secretos estáticos) o directamente Managed Identity si el pipeline corre dentro de Azure DevOps con identidad integrada.

### Spot Instances (EC2)
**Categoría:** AWS
**Definición:** Capacidad de EC2 no utilizada disponible con descuentos de hasta 70-90% respecto al precio On-Demand, a cambio de que AWS puede interrumpirlas con 2 minutos de aviso cuando necesite esa capacidad. Adecuadas para cargas tolerantes a interrupciones: CI runners, batch jobs, procesamiento de imágenes.
**En el proyecto:** Los runners de GitHub Actions del sistema de mensajería pueden usar Spot Instances para reducir el costo de CI/CD en hasta un 90%. Si una instancia es interrumpida durante un build, GitHub Actions reinicia el job automáticamente en otra instancia disponible.

### STS  -  AWS Security Token Service
**Categoría:** AWS
**Definición:** Servicio de AWS que emite credenciales temporales de corta duración (15 minutos a 12 horas) cuando una entidad asume un IAM Role. Es la pieza central del flujo OIDC: recibe el JWT de GitHub Actions, lo valida contra el Identity Provider configurado y emite credenciales temporales con los permisos del Role asumido.
**En el proyecto:** En el flujo OIDC del sistema de mensajería, STS recibe el JWT generado por el GitHub Actions runner, verifica que el repositorio y la rama estén en la trust policy del role `github-actions-crm`, y emite credenciales temporales válidas por 1 hora para el pipeline de despliegue.

### Subscription (Azure)
**Categoría:** Azure
**Definición:** Unidad de facturación y gestión de acceso en Azure. Equivalente a una cuenta de AWS. Todos los recursos Azure pertenecen a una suscripción. El límite de recursos, el presupuesto y los permisos de administración se configuran a nivel de suscripción. Una organización puede tener múltiples suscripciones (por ambiente, por proyecto).
**En el proyecto:** Los recursos del sistema de mensajería en Azure residen en una suscripción dedicada de la empresa, separada de otras suscripciones de la organización, lo que facilita el control de costos y el aislamiento de permisos.

---

## T

### Tag (Etiqueta de recurso)
**Categoría:** AWS / Azure
**Definición:** Par clave-valor adjunto a un recurso cloud para clasificarlo y filtrarlo. Permiten organizar recursos por proyecto, ambiente, propietario o cualquier dimensión relevante. Son indispensables para el control de costos: Cost Explorer y Azure Cost Management pueden filtrar el gasto por tag para saber exactamente cuánto cuesta cada ambiente o componente.
**En el proyecto:** Todos los recursos del sistema de mensajería tienen los tags `Project=mensajeria`, `Environment=production|staging|development` y `Owner=acme`. Sin estos tags, Cost Explorer muestra un total global sin granularidad por ambiente.

### Toil operacional
**Categoría:** Cloud
**Definición:** Trabajo repetitivo, manual y automatable relacionado con la operación de sistemas (aplicar parches, reiniciar servicios, gestionar backups). Concepto de SRE (Site Reliability Engineering). La adopción de PaaS reduce el toil porque el proveedor gestiona parches del SO, backups de base de datos y actualizaciones de runtime.
**En el proyecto:** Usar RDS en lugar de PostgreSQL en EC2 elimina el toil de aplicar `apt upgrade postgresql` manualmente, gestionar archivos de backup y configurar replicación. El equipo dedica ese tiempo a desarrollar funcionalidades del CRM.

### Trust Policy (IAM Role)
**Categoría:** AWS
**Definición:** Documento JSON adjunto a un IAM Role que define qué entidades (servicios, cuentas, Identity Providers) pueden asumir ese role. Es la parte de "quién puede usar este role", a diferencia de las políticas de permisos que definen "qué puede hacer el role". En OIDC, la trust policy especifica el Identity Provider y las condiciones (repositorio, rama).
**En el proyecto:** La trust policy del role `github-actions-crm` permite que solo el repositorio `acme/mensajeria` en la rama `main` pueda asumirlo, evitando que otros repositorios o workflows obtengan permisos de despliegue en la cuenta AWS de la empresa.

---

## V

### VPC  -  Virtual Private Cloud
**Categoría:** AWS
**Definición:** Red privada virtual aislada dentro de AWS donde se despliegan los recursos. Permite definir subredes públicas (con acceso a Internet) y privadas (sin acceso directo a Internet), tablas de ruteo, security groups y NACLs (Network Access Control Lists). Es el perímetro de red básico de toda arquitectura AWS.
**En el proyecto:** Toda la infraestructura del sistema de mensajería vive dentro de la VPC: EC2/ECS en subnets privadas, RDS en subnets privadas, y solo el load balancer en la subnet pública con IP accesible desde Internet. La VPC actúa como el "firewall perimetral" de la arquitectura.

### VM  -  Virtual Machine (Azure Virtual Machines)
**Categoría:** Azure
**Definición:** Servicio IaaS de Azure que provee máquinas virtuales bajo demanda, equivalente a EC2 en AWS. El usuario gestiona el SO, los parches, el runtime y la aplicación. En la práctica, para aplicaciones .NET modernas se prefiere App Service (PaaS) o AKS (contenedores) sobre VMs, por menor toil operacional.
**En el proyecto:** Azure Virtual Machines es el equivalente IaaS a EC2 para el sistema de mensajería en Azure. Como con EC2, se recomienda preferir App Service (PaaS) para reducir la responsabilidad operacional del equipo.

---

## W

### Workload Identity Federation (Azure)
**Categoría:** Azure
**Definición:** Mecanismo de Azure equivalente a OIDC en AWS. Permite que un identity provider externo (como GitHub Actions) asuma una Managed Identity o Service Principal de Azure temporalmente, mediante un token JWT firmado, sin almacenar secrets estáticos en el repositorio. Elimina la gestión de client secrets con fecha de expiración.
**En el proyecto:** En el escenario Azure del sistema de mensajería, el pipeline de despliegue en `.github/workflows/deploy.yml` usa Workload Identity Federation para autenticarse ante Azure y publicar la imagen en ACR y desplegar en App Service, sin ningún secret en el repositorio de GitHub.
