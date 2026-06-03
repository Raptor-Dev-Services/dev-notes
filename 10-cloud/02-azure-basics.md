# 02 · Azure — Servicios esenciales

> Nota: no hay libro de Azure en la biblioteca actual. Este documento cubre los servicios equivalentes a los de AWS para el stack .NET.

## Problema que resuelve

Para equipos que despliegan en Azure en lugar de AWS, los servicios equivalentes tienen nombres y configuración distintos. Este documento mapea los servicios de Azure al stack .NET + Docker.

## Infraestructura global

| Concepto | Azure | Equivalente AWS |
|----------|-------|-----------------|
| Region | East US, Brazil South, etc. | us-east-1 |
| Availability Zone | zona dentro de la región | AZ |
| Resource Group | contenedor lógico para agrupar recursos | (no tiene equivalente directo) |
| Subscription | unidad de facturación y acceso | AWS Account |

## App Service — cómputo para contenedores y código

Equivalente a EC2 + ECS para aplicaciones web.

```bash
# crear App Service Plan (define capacidad)
az appservice plan create \
  --name gtm-plan \
  --resource-group gtm-rg \
  --sku B2 \
  --is-linux

# crear Web App para contenedor Docker
az webapp create \
  --name gtm-suite-api \
  --resource-group gtm-rg \
  --plan gtm-plan \
  --deployment-container-image-name miacr.azurecr.io/gtm-suite-api:latest
```

### SKUs de App Service Plan

| SKU | vCPU | RAM | Uso |
|-----|------|-----|-----|
| B1 | 1 | 1.75 GB | desarrollo |
| B2 | 2 | 3.5 GB | staging |
| P1v3 | 2 | 8 GB | producción ligera |
| P2v3 | 4 | 16 GB | producción media |

## Azure Container Registry (ACR)

Equivalente a Amazon ECR.

```bash
# crear registry
az acr create --name miacr --resource-group gtm-rg --sku Basic

# login
az acr login --name miacr

# push imagen
docker tag gtm-suite-api miacr.azurecr.io/gtm-suite-api:v1.0.0
docker push miacr.azurecr.io/gtm-suite-api:v1.0.0
```

## Azure SQL / PostgreSQL Flexible Server

Equivalente a RDS.

```bash
# crear servidor PostgreSQL Flexible Server
az postgres flexible-server create \
  --name gtm-db \
  --resource-group gtm-rg \
  --location eastus \
  --admin-user admin \
  --admin-password "S3cur3Pass!" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --public-access Disabled
```

## Azure Blob Storage

Equivalente a S3.

```bash
# crear cuenta de storage
az storage account create \
  --name gtmstorage \
  --resource-group gtm-rg \
  --sku Standard_LRS \
  --kind StorageV2

# crear contenedor
az storage container create \
  --name assets \
  --account-name gtmstorage

# subir archivo
az storage blob upload \
  --account-name gtmstorage \
  --container-name assets \
  --name logo.png \
  --file ./logo.png
```

## Managed Identity — sin credenciales en código

Equivalente a IAM Roles. Permite que App Service, AKS o Azure Functions accedan a otros servicios Azure sin almacenar credenciales.

```bash
# asignar identidad al App Service
az webapp identity assign \
  --name gtm-suite-api \
  --resource-group gtm-rg

# dar acceso al ACR con la identidad
az role assignment create \
  --assignee <principalId> \
  --role AcrPull \
  --scope /subscriptions/.../resourceGroups/gtm-rg/providers/Microsoft.ContainerRegistry/registries/miacr
```

Con Managed Identity, el App Service puede leer de Key Vault y ACR sin ninguna clave almacenada.

## Azure Active Directory (Entra ID)

Gestión de identidades y accesos.

| Concepto Azure | Equivalente AWS |
|----------------|-----------------|
| Service Principal | IAM User para aplicaciones |
| Managed Identity | IAM Role para servicios |
| RBAC Role Assignment | IAM Policy |
| Tenant | AWS Organization |

## Relación con back-template

El back-template funciona sin cambios en Azure: el `Dockerfile` se construye igual, el pipeline de Azure DevOps (ver `09-cicd/03-azure-devops.md`) empuja a ACR, y App Service corre el contenedor. Los secretos van en Azure Key Vault con Managed Identity (ver `05-secretos-produccion.md`).

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| cuando el cliente ya tiene suscripción Azure y licencias Microsoft | cuando el equipo tiene más experiencia en AWS |
| App Service para apps .NET sin gestionar infraestructura | Azure VMs para aplicaciones stateless en contenedores |
| Managed Identity siempre que sea posible | Service Principals con secretos estáticos de larga vida |


> Fuente: *Architecting Modern Web Apps with ASP.NET Core and Azure* (Steve Smith): Ch.2 Azure Architecture and Services Overview

---

## Glosario

| Término | Definición |
|---------|-----------|
| Resource Group | Contenedor lógico en Azure que agrupa recursos relacionados para gestión y facturación conjunta |
| Subscription | Unidad de facturación y acceso en Azure; equivalente a una cuenta AWS |
| App Service Plan | Define la capacidad (SKU, vCPU, RAM) que comparten una o varias Web Apps |
| App Service | Servicio PaaS de Azure para ejecutar aplicaciones web y contenedores sin gestionar VMs |
| ACR | Azure Container Registry; registro privado de imágenes Docker alojado en Azure |
| Azure Blob Storage | Almacenamiento de objetos no estructurados en Azure; equivalente a Amazon S3 |
| PostgreSQL Flexible Server | Servicio gestionado de PostgreSQL en Azure con opciones de escalado y alta disponibilidad |
| Managed Identity | Identidad asignada a un servicio de Azure que permite acceder a otros recursos sin credenciales estáticas |
| Entra ID | Servicio de gestión de identidades y accesos de Azure (anteriormente Azure Active Directory) |
| Service Principal | Identidad de aplicación en Entra ID equivalente a un usuario IAM para cargas de trabajo automatizadas |
| RBAC | Control de acceso basado en roles; mecanismo para asignar permisos sobre recursos de Azure |
| SKU | Stock Keeping Unit; nivel de servicio que determina capacidad y precio (B1, B2, P1v3, etc.) |
| az CLI | Herramienta de línea de comandos para gestionar recursos de Azure desde terminal |

---

*Rogelio Arriaga Gonzalez*
