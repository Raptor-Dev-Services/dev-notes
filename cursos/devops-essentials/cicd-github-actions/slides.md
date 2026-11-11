<!-- _class: lead -->

# CI/CD con GitHub Actions

## GitHub Actions  -  OIDC + AWS  -  Checklists de Deploy

**Dev Notes**

---

<!-- _class: lead -->

# Parte 1

## CI vs CD vs CD
### Continuous Integration  -  Delivery  -  Deployment

---

## ¿Qué son CI y CD?

Tres conceptos que suenan iguales pero son distintos:

| Sigla | Nombre completo | Qué hace |
|-------|----------------|----------|
| **CI** | Continuous Integration | Valida cada cambio: compila, testea, analiza |
| **CD** | Continuous Delivery | Deja la app lista para deployar  -  **un humano aprieta el botón** |
| **CD** | Continuous Deployment | El deploy ocurre automáticamente tras CI verde  -  **sin botón** |

> Sin automatización, el build/deploy es manual, propenso a errores y no reproducible.

---

## Por qué importa en el sistema de mensajería

Sin CI/CD:
- Un dev hace push y rompe el build  -  nadie lo sabe hasta horas después
- El deploy a producción es copy-paste de archivos por SSH
- No hay historial reproducible de qué versión está en cada ambiente

Con CI/CD:
- Cada PR se valida en segundos  -  feedback inmediato
- El deploy es un tag en git: `git tag v1.2.0 && git push --tags`
- Rollback = re-deployar la imagen anterior desde ECR

---

## Flujo completo del pipeline

```
PR abierta         ->  CI: build + tests        (bloquea el merge si falla)
Merge a main       ->  CD: imagen Docker -> ECR -> deploy ECS
Push tag v*.*.*    ->  CD: igual, con tag de versión semántica
```

**Regla de oro:** main siempre deployable. Ningún merge sin CI verde.

---

<!-- _class: lead -->

# Parte 2

## GitHub Actions  -  Conceptos clave
### workflow  -  job  -  step  -  runner  -  trigger  -  artifact

---

## Anatomía de un workflow

```yaml
name: CI                          # nombre del workflow (aparece en la UI)

on:                               # trigger  -  cuándo corre
  pull_request:
    branches: [main, develop]

jobs:                             # uno o más jobs (corren en paralelo por defecto)
  build-and-test:                 # nombre del job
    runs-on: ubuntu-latest        # runner  -  máquina donde corre

    steps:                        # pasos secuenciales dentro del job
      - uses: actions/checkout@v4 # action externa
      - name: Build               # step con nombre
        run: dotnet build         # comando shell
```

---

## Glosario esencial

| Término | Definición |
|---------|-----------|
| **workflow** | Archivo YAML en `.github/workflows/`  -  define el pipeline completo |
| **job** | Unidad de trabajo que corre en un runner; jobs paralelos por default |
| **step** | Comando o action dentro de un job; secuenciales, comparten filesystem |
| **runner** | Máquina virtual (GitHub-hosted o self-hosted) donde corre el job |
| **trigger (`on:`)** | Evento que dispara el workflow: `push`, `pull_request`, `schedule`, `workflow_dispatch` |
| **artifact** | Archivo generado en el pipeline (ej. binarios, reportes de cobertura) que se puede descargar o pasar entre jobs |

---

## Triggers más usados

```yaml
on:
  pull_request:             # corre en cada PR hacia las ramas indicadas
    branches: [main, develop]

  push:                     # corre en push directo a main o tags v*.*.*
    branches: [main]
    tags: ['v*.*.*']

  workflow_dispatch:        # permite ejecutar el workflow manualmente desde la UI
```

> `pull_request` es el trigger de CI  -  bloquea merge si falla.
> `push` a `main` es el trigger de CD  -  arranca el deploy.

---

<!-- _class: lead -->

# Parte 3

## Workflow de CI
### checkout  -  setup-dotnet  -  restore  -  build  -  test

---

## ci.yml  -  Validación en cada PR

```yaml
# .github/workflows/ci.yml
name: CI

on:
  pull_request:
    branches: [main, develop]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test
        run: dotnet test --no-build --configuration Release --verbosity normal
```

---

## ¿Qué hace cada step?

| Step | Por qué es necesario |
|------|---------------------|
| `actions/checkout@v4` | Descarga el código del repo al runner (sin esto, el filesystem está vacío) |
| `actions/setup-dotnet@v4` | Instala el SDK de .NET indicado; los runners tienen versiones preinstaladas pero conviene fijar |
| `dotnet restore` | Descarga NuGet packages; separado del build para cachear en futuros runs |
| `dotnet build --no-restore` | Compila en Release  -  si hay error de compilación, el pipeline falla aquí |
| `dotnet test --no-build` | Corre todos los proyectos `*.Tests.csproj`; `--no-build` evita recompilar |

> Si cualquier step devuelve exit code distinto de 0, el job falla y el PR queda bloqueado.

---

## Artifacts de cobertura

```yaml
      - name: Test con cobertura
        run: |
          dotnet test --no-build --configuration Release \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage

      - name: Upload coverage report
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: ./coverage
          retention-days: 7
```

`actions/upload-artifact` guarda el reporte en GitHub para descargarlo o usarlo en jobs posteriores (ej. SonarCloud).

---

<!-- _class: lead -->

# Parte 4

## Workflow de CD
### Build imagen Docker  -  Push a ECR  -  Deploy a ECS

---

## cd.yml  -  Deploy a AWS ECS (con IAM keys)

```yaml
# .github/workflows/cd.yml
name: CD

on:
  push:
    branches: [main]
    tags: ['v*.*.*']

env:
  AWS_REGION:     ${{ secrets.AWS_REGION }}
  ECR_REGISTRY:   ${{ secrets.ECR_REGISTRY }}
  ECR_REPOSITORY: ${{ secrets.ECR_REPOSITORY }}

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id:     ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region:            ${{ env.AWS_REGION }}
```

---

## cd.yml  -  Build, push y deploy

```yaml
      - name: Login to Amazon ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v2

      - name: Set image tag
        id: tag
        run: |
          if [[ "${{ github.ref }}" == refs/tags/* ]]; then
            echo "IMAGE_TAG=${GITHUB_REF#refs/tags/}" >> $GITHUB_OUTPUT
          else
            echo "IMAGE_TAG=${{ github.sha }}" >> $GITHUB_OUTPUT
          fi

      - name: Build and push image to ECR
        id: build-image
        run: |
          IMAGE=$ECR_REGISTRY/$ECR_REPOSITORY:${{ steps.tag.outputs.IMAGE_TAG }}
          docker build -t $IMAGE .
          docker push $IMAGE
          echo "image=$IMAGE" >> $GITHUB_OUTPUT
```

> Nota: en tags usa la versión semántica (`v1.2.0`); en push a main usa el SHA del commit.

---

## cd.yml  -  Actualizar ECS

```yaml
      - name: Update ECS task definition
        id: task-def
        uses: aws-actions/amazon-ecs-render-task-definition@v1
        with:
          task-definition: ${{ secrets.ECS_TASK_DEFINITION }}
          container-name:  ${{ secrets.CONTAINER_NAME }}
          image:           ${{ steps.build-image.outputs.image }}

      - name: Deploy to ECS
        uses: aws-actions/amazon-ecs-deploy-task-definition@v1
        with:
          task-definition: ${{ steps.task-def.outputs.task-definition }}
          service:         ${{ secrets.ECS_SERVICE }}
          cluster:         ${{ secrets.ECS_CLUSTER }}
          wait-for-service-stability: true
```

`wait-for-service-stability: true` hace que el pipeline espere a que ECS confirme que los contenedores nuevos están healthy. Si fallan los health checks, el pipeline falla.

---

## Secrets requeridos en GitHub

Configurar en: **Settings -> Secrets and variables -> Actions**

| Secret | Ejemplo de valor |
|--------|-----------------|
| `AWS_REGION` | `us-east-1` |
| `ECR_REGISTRY` | `123456789.dkr.ecr.us-east-1.amazonaws.com` |
| `ECR_REPOSITORY` | `mensajeria-api` |
| `ECS_CLUSTER` | `mensajeria-cluster` |
| `ECS_SERVICE` | `mensajeria-service` |
| `ECS_TASK_DEFINITION` | `mensajeria-task` |
| `CONTAINER_NAME` | `mensajeria-api` |

> Las variables de entorno de la app (connection strings, JWT secret) NO van en el Dockerfile  -  se inyectan en la task definition de ECS o desde AWS Secrets Manager.

---

<!-- _class: lead -->

# Parte 5

## OIDC con AWS
### Por qué OIDC es mejor que IAM Access Keys

---

## El problema con IAM keys en GitHub Secrets

Con el enfoque anterior (Parte 4), se guardan credenciales **de larga duración** en GitHub:

```
AWS_ACCESS_KEY_ID     = AKIA...
AWS_SECRET_ACCESS_KEY = wJalrXUtnFEMI...
```

Riesgos:
- Si el repo se filtra (fork malicioso, breach de GitHub), las keys son válidas para siempre
- Hay que rotarlas manualmente cada 90 días  -  fácil olvidar
- Aplican a todo lo que el IAM user puede hacer, no solo al pipeline

**OIDC resuelve todo esto con tokens temporales.**

---

## Cómo funciona OIDC

```
GitHub Actions  ->  presenta token OIDC firmado por GitHub
     ↓
AWS STS         ->  valida el token, verifica la trust policy
     ↓
Credenciales temporales (por omisión 1 hora)  ->  llegan al runner
```

El runner nunca tiene `AWS_ACCESS_KEY_ID`. Solo tiene un token de sesión que expira solo.

**Ventajas:**
- No hay credenciales de larga duración que rotar o filtrar
- Las credenciales temporales expiran solas (por omisión a la hora; la duración es configurable)

Fuente: AWS, *AssumeRoleWithWebIdentity* (API Reference de STS): `DurationSeconds` vale 3600 s (1 hora) por omisión y es configurable, docs.aws.amazon.com/STS/latest/APIReference/API_AssumeRoleWithWebIdentity.html.

-
- La trust policy limita el acceso al repo/rama exacto

---

## Trust policy del IAM Role

```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Principal": {
      "Federated":
        "arn:aws:iam::ACCOUNT_ID:oidc-provider/token.actions.githubusercontent.com"
    },
    "Action": "sts:AssumeRoleWithWebIdentity",
    "Condition": {
      "StringEquals": {
        "token.actions.githubusercontent.com:aud": "sts.amazonaws.com"
      },
      "StringLike": {
        "token.actions.githubusercontent.com:sub": "repo:acme/mensajeria:*"
      }
    }
  }]
}
```

El claim `sub` limita el acceso **solo al repo `acme/mensajeria`**. Un repo diferente no puede asumir este rol aunque tenga el workflow.

---

## cd-oidc.yml  -  Sin credenciales en Secrets

```yaml
# .github/workflows/cd-oidc.yml
name: CD con OIDC

on:
  push:
    branches: [main]
    tags: ['v*.*.*']

permissions:
  id-token: write   # necesario para solicitar el token OIDC
  contents: read

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Configure AWS credentials (OIDC)
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::ACCOUNT_ID:role/github-actions-deploy-role
          aws-region: us-east-1

      # El resto del pipeline es idéntico al cd.yml de la Parte 4
```

> `permissions: id-token: write` es obligatorio. Sin eso GitHub no emite el token OIDC.

---

<!-- _class: lead -->

# Parte 6

## Secrets en GitHub Actions
### secrets.NOMBRE  -  environments  -  protection rules

---

## Niveles de secrets en GitHub

| Nivel | Dónde se configura | Cuándo usar |
|-------|--------------------|-------------|
| **Repository** | Settings -> Secrets -> Actions | Valores que comparten todos los workflows del repo |
| **Environment** | Settings -> Environments -> `staging` / `production` | Valores distintos por ambiente (ej. ECR diferente) |
| **Organization** | Org Settings -> Secrets | Valores compartidos entre múltiples repos |

---

## Cómo acceder a secrets en un step

```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production          # activa los secrets del environment

    steps:
      - name: Configure AWS
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: ${{ secrets.AWS_DEPLOY_ROLE_ARN }}
          aws-region:     ${{ secrets.AWS_REGION }}

      - name: Conectar a base de datos
        env:
          DB_CONNECTION: ${{ secrets.DATABASE_URL }}  # inyectar en env del step
        run: ./scripts/migrate.sh
```

> Los secrets nunca se imprimen en los logs  -  GitHub los enmascara con `***`.

---

## Environments: staging vs production

```yaml
jobs:
  deploy-staging:
    runs-on: ubuntu-latest
    environment: staging             # usa secrets de staging, sin aprobación

  deploy-production:
    runs-on: ubuntu-latest
    needs: deploy-staging
    environment: production          # puede requerir aprobación manual
```

**Protection rules en `production`:**
- **Required reviewers:** un humano aprueba antes de que corra el job
- **Wait timer:** espera N minutos después de staging (ej. 30 min de soak)
- **Deployment branches:** solo `main` o tags `v*.*.*` pueden deployar a production

---

<!-- _class: lead -->

# Parte 7

## Checklists de Deploy
### pre-deploy  -  deploy  -  post-deploy  -  rollback

---

## Checklist pre-deploy

Antes de hacer merge a main o pushear un tag:

- ¿Hay secretos, API keys o passwords en el diff?
- ¿Pasan todos los checks de CI (build + tests)?
- ¿El PR fue revisado por al menos una persona?
- ¿Las migraciones de base de datos son reversibles?
- ¿Las variables de entorno nuevas están en producción (Secrets Manager)?
- ¿Se probó en staging con datos reales (no producción)?
- ¿Hay un plan de rollback documentado?
- ¿La ventana de deploy es la correcta?  -  **no deployar viernes a las 5 PM**
- ¿Está alguien on-call por si algo falla?

---

## Checklist deploy y post-deploy

**Durante el deploy:**
- El pipeline de CD corre y pasa todos los steps
- `wait-for-service-stability: true` confirma que ECS levantó los containers
- Los health checks de ECS responden 200

**Después del deploy:**
- `/health` responde 200 en el endpoint público
- Los logs en CloudWatch no muestran errores nuevos en los primeros 15 min
- Las métricas clave (latencia p95, tasa de error) están dentro del baseline
- Se valida un flujo crítico manualmente (ej. login -> crear conversación -> enviar mensaje)

---

## Diagnóstico rápido si algo falla en producción

1. **Health checks:** ¿`/health` responde 200 en cada servicio?
2. **Logs:** buscar errores en CloudWatch / Seq en los últimos 15 minutos
3. **Métricas:** latencia p95, tasa de error, uso de CPU/memoria
4. **Conexiones BD:** ¿pool agotado? ¿queries lentas?
5. **Servicios externos:** ¿WhatsApp API, Instagram, SMTP responden?
6. **Última versión:** ¿este bug existía antes? ¿coincide con el deploy reciente?

Si el problema es del deploy: **hacer rollback inmediato**, luego investigar.

---

## Rollback manual en ECS / EC2 con Docker

**ECS:** re-deployar la task definition anterior desde la consola de AWS o CLI:

```bash
# Listar revisiones anteriores de la task definition
aws ecs list-task-definitions --family-prefix mensajeria-task

# Forzar nuevo deploy con la revisión anterior (ej. :12)
aws ecs update-service \
  --cluster mensajeria-cluster \
  --service mensajeria-service \
  --task-definition mensajeria-task:12 \
  --force-new-deployment
```

**EC2 con Docker:** la imagen anterior sigue en ECR con su tag de versión:

```bash
docker pull 123456789.dkr.ecr.us-east-1.amazonaws.com/mensajeria-api:v1.1.0
docker stop mensajeria && docker rm mensajeria
docker run -d --name mensajeria ... mensajeria-api:v1.1.0
```

> Por eso no usar `:latest` en producción  -  imposible hacer rollback a una versión específica.

---

<!-- _class: lead -->

# Parte 8

## Pipeline completo del sistema de mensajería
### ci.yml + cd-oidc.yml  -  archivos reales

---

## Estructura de archivos del repo

```
mensajeria/
|-- .github/
|   `-- workflows/
|       |-- ci.yml          <- valida cada PR a main/develop
|       `-- cd-oidc.yml     <- deploya en push a main o tag v*.*.*
|-- src/
|   |-- Mensajeria.Api/
|   |-- Mensajeria.Application/
|   |-- Mensajeria.Domain/
|   `-- Mensajeria.Infrastructure/
|-- tests/
|   `-- Mensajeria.Tests/
`-- Dockerfile              <- multi-stage build (ya incluido en back-template)
```

---

## ci.yml  -  Sistema de mensajería (completo)

```yaml
# .github/workflows/ci.yml
name: CI

on:
  pull_request:
    branches: [main, develop]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test
        run: dotnet test --no-build --configuration Release --verbosity normal
```

---

## cd-oidc.yml  -  Sistema de mensajería (parte 1/2)

```yaml
# .github/workflows/cd-oidc.yml
name: CD

on:
  push:
    branches: [main]
    tags: ['v*.*.*']

permissions:
  id-token: write
  contents: read

env:
  AWS_REGION:     us-east-1
  ECR_REGISTRY:   ${{ secrets.ECR_REGISTRY }}
  ECR_REPOSITORY: mensajeria-api

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@v4

      - name: Configure AWS credentials (OIDC)
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: ${{ secrets.AWS_DEPLOY_ROLE_ARN }}
          aws-region: ${{ env.AWS_REGION }}
```

---

## cd-oidc.yml  -  Sistema de mensajería (parte 2/2)

```yaml
      - name: Login to Amazon ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v2

      - name: Set image tag
        id: tag
        run: |
          if [[ "${{ github.ref }}" == refs/tags/* ]]; then
            echo "IMAGE_TAG=${GITHUB_REF#refs/tags/}" >> $GITHUB_OUTPUT
          else
            echo "IMAGE_TAG=${{ github.sha }}" >> $GITHUB_OUTPUT
          fi

      - name: Build and push image to ECR
        run: |
          IMAGE=$ECR_REGISTRY/$ECR_REPOSITORY:${{ steps.tag.outputs.IMAGE_TAG }}
          docker build -t $IMAGE .
          docker push $IMAGE

      - name: Deploy to ECS
        uses: aws-actions/amazon-ecs-deploy-task-definition@v1
        with:
          task-definition: ${{ secrets.ECS_TASK_DEFINITION }}
          service:         ${{ secrets.ECS_SERVICE }}
          cluster:         ${{ secrets.ECS_CLUSTER }}
          wait-for-service-stability: true
```

---

## Permisos IAM mínimos para el deploy role

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "ecr:GetAuthorizationToken",
        "ecr:BatchCheckLayerAvailability",
        "ecr:InitiateLayerUpload",
        "ecr:UploadLayerPart",
        "ecr:CompleteLayerUpload",
        "ecr:PutImage"
      ],
      "Resource": "arn:aws:ecr:us-east-1:ACCOUNT_ID:repository/mensajeria-api"
    },
    {
      "Effect": "Allow",
      "Action": [
        "ecs:RegisterTaskDefinition",
        "ecs:UpdateService",
        "ecs:DescribeServices",
        "iam:PassRole"
      ],
      "Resource": "*"
    }
  ]
}
```

> En producción sustituir `"Resource": "*"` por los ARNs exactos del cluster y servicio.

---

## Resumen: decisiones de diseño del pipeline

| Decisión | Por qué |
|----------|---------|
| OIDC en lugar de IAM keys | No hay credenciales de larga duración en GitHub |
| `wait-for-service-stability: true` | El pipeline falla si ECS no levanta  -  detección temprana |
| Tag de versión semántica en imagen | Hace rollback posible  -  `:latest` no permite volver atrás |
| Environment `production` con protection rules | Requiere aprobación humana antes de deployar a prod |
| CI separado de CD (`ci.yml` / `cd-oidc.yml`) | CI corre en PRs, CD solo en push a main o tags |
| `needs: ci` en el job de deploy | CD no arranca si CI falla  -  main siempre deployable |
