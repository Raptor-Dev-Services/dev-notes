# 03 · Azure DevOps Pipelines — CI/CD

## Problema que resuelve

Azure DevOps Pipelines automatiza el ciclo completo de integración y entrega continua para proyectos .NET alojados en Azure DevOps Repos. Define el pipeline como código YAML versionado junto al repositorio, elimina configuración manual y garantiza reproducibilidad entre builds.

## Conceptos clave

| Concepto | Descripción |
|----------|-------------|
| Pipeline | archivo YAML que define el flujo completo |
| Stage | agrupación lógica (ej. Build, Test, Deploy) |
| Job | unidad de trabajo que corre en un agente |
| Step | tarea individual dentro de un job |
| Agent | máquina (hosted o self-hosted) que ejecuta el job |
| Environment | destino de deploy con historial y aprobaciones |
| Variable group | conjunto de variables/secretos reutilizables entre pipelines |
| Service connection | credencial almacenada para conectarse a Azure, Docker Hub, etc. |

## Pipeline CI — validación en PR

```yaml
# azure-pipelines-ci.yml
trigger: none

pr:
  branches:
    include:
      - main
      - develop

pool:
  vmImage: ubuntu-latest

variables:
  buildConfiguration: Release

steps:
  - task: UseDotNet@2
    displayName: Setup .NET 10
    inputs:
      packageType: sdk
      version: '10.x'

  - script: dotnet restore
    displayName: Restore

  - script: dotnet build --no-restore --configuration $(buildConfiguration)
    displayName: Build

  - task: DotNetCoreCLI@2
    displayName: Test
    inputs:
      command: test
      projects: '**/*Tests/*.csproj'
      arguments: '--no-build --configuration $(buildConfiguration) --collect:"XPlat Code Coverage"'

  - task: PublishCodeCoverageResults@2
    displayName: Publish coverage
    inputs:
      codeCoverageTool: Cobertura
      summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'
```

## Pipeline CD — build, push a ACR y deploy a App Service

```yaml
# azure-pipelines-cd.yml
trigger:
  branches:
    include:
      - main
  tags:
    include:
      - 'v*'

pool:
  vmImage: ubuntu-latest

variables:
  - group: prod-secrets               # variable group con secrets
  - name: imageRepository
    value: gtm-suite-api
  - name: containerRegistry
    value: miacr.azurecr.io
  - name: dockerfilePath
    value: '$(Build.SourcesDirectory)/Dockerfile'
  - name: tag
    value: $[coalesce(variables['Build.SourceBranchName'], 'latest')]

stages:
  # ── STAGE 1: Build y push imagen ────────────────────────────────
  - stage: Build
    displayName: Build and Push
    jobs:
      - job: BuildJob
        steps:
          - task: Docker@2
            displayName: Build and push to ACR
            inputs:
              command: buildAndPush
              repository: $(imageRepository)
              dockerfile: $(dockerfilePath)
              containerRegistry: acr-service-connection   # nombre del service connection
              tags: |
                $(tag)
                latest

  # ── STAGE 2: Deploy a Staging ────────────────────────────────────
  - stage: DeployStaging
    displayName: Deploy to Staging
    dependsOn: Build
    condition: succeeded()
    jobs:
      - deployment: DeployStaging
        environment: staging                              # environment con historial
        strategy:
          runOnce:
            deploy:
              steps:
                - task: AzureWebAppContainer@1
                  displayName: Deploy to App Service (staging)
                  inputs:
                    azureSubscription: azure-service-connection
                    appName: gtm-suite-staging
                    containers: $(containerRegistry)/$(imageRepository):$(tag)

  # ── STAGE 3: Deploy a Producción (con aprobación manual) ─────────
  - stage: DeployProd
    displayName: Deploy to Production
    dependsOn: DeployStaging
    condition: and(succeeded(), startsWith(variables['Build.SourceBranch'], 'refs/tags/'))
    jobs:
      - deployment: DeployProd
        environment: production                           # tiene aprobadores configurados
        strategy:
          runOnce:
            deploy:
              steps:
                - task: AzureWebAppContainer@1
                  displayName: Deploy to App Service (prod)
                  inputs:
                    azureSubscription: azure-service-connection
                    appName: gtm-suite-api
                    containers: $(containerRegistry)/$(imageRepository):$(tag)
```

## Aprobaciones en Environments

Los environments de Azure DevOps permiten requerir aprobación manual antes de que un stage ejecute:

1. Ir a **Pipelines → Environments**
2. Abrir el environment `production`
3. **Approvals and checks → Add check → Approvals**
4. Agregar los aprobadores y configurar timeout

El pipeline se pausa en `DeployProd` hasta que un aprobador confirme desde Azure DevOps.

## Variable Groups y KeyVault

```yaml
# referenciar variable group en el pipeline
variables:
  - group: prod-secrets    # contiene CONNECTIONSTRING, JWT_SECRET, etc.
```

Los variable groups pueden estar vinculados a Azure Key Vault — los secretos se sincronizan automáticamente y no se almacenan en Azure DevOps.

Configuración: **Pipelines → Library → Variable Groups → Link secrets from Azure Key Vault**.

## Pipeline completo CI + CD en un archivo

```yaml
# azure-pipelines.yml
trigger:
  branches:
    include: [main]
  tags:
    include: ['v*']

pr:
  branches:
    include: [main, develop]

pool:
  vmImage: ubuntu-latest

stages:
  - stage: CI
    displayName: CI — Build and Test
    jobs:
      - job: CIJob
        steps:
          - task: UseDotNet@2
            inputs: { packageType: sdk, version: '10.x' }
          - script: dotnet restore
          - script: dotnet build --no-restore -c Release
          - task: DotNetCoreCLI@2
            inputs:
              command: test
              projects: '**/*Tests/*.csproj'
              arguments: '--no-build -c Release'

  - stage: CD
    displayName: CD — Deploy
    dependsOn: CI
    condition: and(succeeded(), ne(variables['Build.Reason'], 'PullRequest'))
    jobs:
      - deployment: Deploy
        environment: production
        strategy:
          runOnce:
            deploy:
              steps:
                - task: Docker@2
                  inputs:
                    command: buildAndPush
                    repository: gtm-suite-api
                    containerRegistry: acr-service-connection
                    tags: $(Build.BuildId)
                - task: AzureWebAppContainer@1
                  inputs:
                    azureSubscription: azure-service-connection
                    appName: gtm-suite-api
                    containers: miacr.azurecr.io/gtm-suite-api:$(Build.BuildId)
```

`ne(variables['Build.Reason'], 'PullRequest')` evita que el stage de deploy corra en PRs.

## Service connections requeridas

| Nombre sugerido | Tipo | Para qué |
|-----------------|------|----------|
| `acr-service-connection` | Docker Registry | push a Azure Container Registry |
| `azure-service-connection` | Azure Resource Manager | deploy a App Service |

Configurar en: **Project Settings → Service connections**.

## Relación con back-template

El `Dockerfile` del back-template es compatible sin modificación. El `azure-pipelines.yml` va en la raíz del repositorio. Las variables de conexión (`ConnectionStrings__Default`, `Jwt__Secret`) van en el variable group vinculado a Key Vault — nunca en el YAML.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| proyectos hospedados en Azure DevOps Repos | repos en GitHub (usar GitHub Actions) |
| equipos que ya usan Azure para infraestructura | cuando no se tiene suscripción Azure activa |
| aprobaciones formales antes de producción | scripts de deploy ad-hoc sin ciclo de vida de PR |


> Fuente: *AWS Certified DevOps Engineer Professional* (Cybellium) — Ch.1 CI/CD Fundamentals and Pipeline Design

---

## Glosario

| Término | Definición |
|---------|-----------|
| Pipeline | Archivo YAML que define el flujo completo de CI/CD como código versionado junto al repositorio |
| Stage | Agrupación lógica de jobs dentro de un pipeline (ej. Build, Test, Deploy) |
| Job | Unidad de trabajo que se ejecuta en un agente; los steps de un job corren secuencialmente |
| Step | Tarea individual dentro de un job (script, task de Azure DevOps o acción) |
| Agent | Máquina (hosted por Microsoft o self-hosted) que ejecuta los jobs del pipeline |
| Environment | Destino de despliegue en Azure DevOps con historial de deploys y soporte de aprobaciones |
| Variable Group | Conjunto de variables y secretos reutilizables entre múltiples pipelines del proyecto |
| Service Connection | Credencial almacenada en Azure DevOps para conectarse a servicios externos (Azure, Docker Hub, ACR) |
| ACR | Azure Container Registry; registro privado de imágenes Docker equivalente a Amazon ECR |
| App Service | Servicio PaaS de Azure para ejecutar aplicaciones web y contenedores Docker |
| Key Vault | Servicio de Azure para almacenar secretos cifrados que se sincronizan con Variable Groups |
| Aprobación manual | Control de environment que pausa el pipeline hasta que un aprobador confirma el despliegue |
| Trigger | Condición que dispara automáticamente el pipeline (push a rama, tag, pull request) |
| Artefacto de build | Resultado compilado del stage CI que el stage CD usa para el despliegue |

---

*Rogelio Arriaga Gonzalez*
