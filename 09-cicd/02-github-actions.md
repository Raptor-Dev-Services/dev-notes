# 02 · GitHub Actions — CI/CD con AWS

## Problema que resuelve

Sin automatización, el proceso de build, pruebas y deploy es manual, propenso a errores y no reproducible. GitHub Actions orquesta el pipeline completo: valida cada PR con CI, y en merge a `main` construye la imagen Docker, la publica en Amazon ECR y actualiza el servicio en ECS Fargate.

## Arquitectura del pipeline

```
PR abierta        → CI: build + tests
Merge a main      → CD: build imagen → push ECR → deploy ECS
Push de tag v*.*.*→ CD: igual que main, con tag de versión
```

## Secrets requeridos en GitHub

Configurar en: Settings → Secrets and variables → Actions

| Secret | Valor |
|--------|-------|
| `AWS_ACCESS_KEY_ID` | IAM user con permisos ECR + ECS |
| `AWS_SECRET_ACCESS_KEY` | clave del IAM user |
| `AWS_REGION` | ej. `us-east-1` |
| `ECR_REGISTRY` | ej. `123456789.dkr.ecr.us-east-1.amazonaws.com` |
| `ECR_REPOSITORY` | nombre del repo en ECR, ej. `gtm-suite-api` |
| `ECS_CLUSTER` | nombre del cluster ECS |
| `ECS_SERVICE` | nombre del servicio ECS |
| `ECS_TASK_DEFINITION` | nombre de la task definition |
| `CONTAINER_NAME` | nombre del contenedor en la task definition |

## Workflow CI — validación en PR

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

## Workflow CD — deploy a AWS ECS

```yaml
# .github/workflows/cd.yml
name: CD

on:
  push:
    branches: [main]
  push:
    tags: ['v*.*.*']

env:
  AWS_REGION:      ${{ secrets.AWS_REGION }}
  ECR_REGISTRY:    ${{ secrets.ECR_REGISTRY }}
  ECR_REPOSITORY:  ${{ secrets.ECR_REPOSITORY }}

jobs:
  deploy:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      # 1. Autenticar en AWS
      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id:     ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region:            ${{ env.AWS_REGION }}

      # 2. Login al ECR
      - name: Login to Amazon ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v2

      # 3. Determinar el tag de imagen
      - name: Set image tag
        id: tag
        run: |
          if [[ "${{ github.ref }}" == refs/tags/* ]]; then
            echo "IMAGE_TAG=${GITHUB_REF#refs/tags/}" >> $GITHUB_OUTPUT
          else
            echo "IMAGE_TAG=latest" >> $GITHUB_OUTPUT
          fi

      # 4. Build y push de imagen Docker
      - name: Build and push image to ECR
        id: build-image
        run: |
          IMAGE=$ECR_REGISTRY/$ECR_REPOSITORY:${{ steps.tag.outputs.IMAGE_TAG }}
          docker build -t $IMAGE .
          docker push $IMAGE
          echo "image=$IMAGE" >> $GITHUB_OUTPUT

      # 5. Actualizar task definition con la nueva imagen
      - name: Update ECS task definition
        id: task-def
        uses: aws-actions/amazon-ecs-render-task-definition@v1
        with:
          task-definition: ${{ secrets.ECS_TASK_DEFINITION }}
          container-name:  ${{ secrets.CONTAINER_NAME }}
          image:           ${{ steps.build-image.outputs.image }}

      # 6. Deploy al servicio ECS
      - name: Deploy to ECS
        uses: aws-actions/amazon-ecs-deploy-task-definition@v1
        with:
          task-definition: ${{ steps.task-def.outputs.task-definition }}
          service:         ${{ secrets.ECS_SERVICE }}
          cluster:         ${{ secrets.ECS_CLUSTER }}
          wait-for-service-stability: true
```

## Workflow combinado CI + CD en un solo archivo

Alternativa cuando el equipo es pequeño y se quiere un solo archivo:

```yaml
# .github/workflows/pipeline.yml
name: Pipeline

on:
  pull_request:
    branches: [main, develop]
  push:
    branches: [main]
    tags: ['v*.*.*']

jobs:
  ci:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.x' }
      - run: dotnet restore
      - run: dotnet build --no-restore --configuration Release
      - run: dotnet test --no-build --configuration Release

  deploy:
    needs: ci
    if: github.event_name == 'push'   # solo en push, no en PR
    runs-on: ubuntu-latest
    steps:
      # ... mismos pasos del workflow CD de arriba
```

`needs: ci` garantiza que el deploy solo corre si CI pasa. `if: github.event_name == 'push'` evita que intente deployar en PRs.

## Permisos IAM mínimos para el pipeline

El IAM user del pipeline necesita solo estos permisos:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "ecr:GetAuthorizationToken",
        "ecr:BatchCheckLayerAvailability",
        "ecr:GetDownloadUrlForLayer",
        "ecr:BatchGetImage",
        "ecr:InitiateLayerUpload",
        "ecr:UploadLayerPart",
        "ecr:CompleteLayerUpload",
        "ecr:PutImage"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "ecs:DescribeTaskDefinition",
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

En producción, reemplazar `"Resource": "*"` con los ARNs específicos del cluster, servicio y repositorio ECR.

## Relación con back-template

El `Dockerfile` del back-template usa multi-stage build. El step de `docker build` en el pipeline usa ese archivo sin modificación. Las variables de entorno de producción (connection strings, secrets) no van en la imagen — se inyectan como variables de entorno en la task definition de ECS o desde AWS Secrets Manager (ver `10-cloud/05-secretos-produccion.md`).

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| proyectos con repositorio en GitHub y deploy en AWS ECS | proyectos en Azure DevOps (usar pipelines YAML de ADO) |
| tags `v*.*.*` para controlar qué versiones llegan a producción | push directo a main sin PR cuando el equipo tiene más de 1 persona |
| `wait-for-service-stability: true` para detectar fallos de deploy en el pipeline | en repos de solo frontend estático (usar S3 + CloudFront deploy action en su lugar) |

---

## OIDC — autenticación sin credenciales de larga duración
> Fuente: *Learning GitHub Actions* (Weimer): Ch.8 Security Best Practices

En lugar de guardar `AWS_ACCESS_KEY_ID` y `AWS_SECRET_ACCESS_KEY` como Secrets (credenciales de larga duración), se puede usar OIDC para que GitHub Actions pida tokens temporales directamente a AWS.

```
GitHub Actions → presenta token OIDC → AWS STS → genera credenciales temporales (15 min)
```

```json
// IAM Role Trust Policy — permite que GitHub Actions asuma el rol
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::ACCOUNT_ID:oidc-provider/token.actions.githubusercontent.com"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "token.actions.githubusercontent.com:aud": "sts.amazonaws.com"
        },
        "StringLike": {
          "token.actions.githubusercontent.com:sub": "repo:org/repo:*"
        }
      }
    }
  ]
}
```

```yaml
# .github/workflows/cd-oidc.yml
name: CD con OIDC

on:
  push:
    branches: [main]

permissions:
  id-token: write   # necesario para OIDC
  contents: read

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      # OIDC — sin credenciales de larga duración en Secrets
      - name: Configure AWS credentials (OIDC)
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::ACCOUNT_ID:role/github-actions-deploy-role
          aws-region: us-east-1

      # El resto del pipeline es igual
      - name: Login to Amazon ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v2

      - name: Build and push to ECR
        run: |
          IMAGE=${{ steps.login-ecr.outputs.registry }}/gtm-suite-api:${{ github.sha }}
          docker build -t $IMAGE .
          docker push $IMAGE
```

**Ventajas de OIDC vs credenciales estáticas:**
- No hay credenciales de larga duración que rotar o que puedan filtrarse
- Los tokens expiran en 15 minutos automáticamente
- El rol tiene scope exacto (solo el repositorio/rama especificados en la trust policy)

---

## Reusable Workflows y composite actions

```yaml
# .github/workflows/reusable-build.yml — workflow reutilizable
name: Build y Test Reutilizable

on:
  workflow_call:
    inputs:
      dotnet-version:
        type: string
        default: '10.x'
    secrets:
      SONAR_TOKEN:
        required: false

jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ inputs.dotnet-version }}
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-build -c Release
```

```yaml
# .github/workflows/ci.yml — usa el workflow reutilizable
name: CI

on:
  pull_request:
    branches: [main, develop]

jobs:
  build:
    uses: ./.github/workflows/reusable-build.yml
    with:
      dotnet-version: '10.x'
    secrets:
      SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
```


---

## Matrix builds — probar en múltiples versiones/OS

Una matrix build ejecuta el mismo job en múltiples combinaciones de variables (versión de .NET, sistema operativo, región, etc.) en paralelo:

```yaml
# .github/workflows/ci-matrix.yml
name: CI Matrix

on:
  pull_request:
    branches: [main]

jobs:
  test:
    name: Test (.NET ${{ matrix.dotnet }} / ${{ matrix.os }})
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        dotnet: ['9.0.x', '10.0.x']
        os: [ubuntu-latest, windows-latest]
      fail-fast: false   # continuar con otras combinaciones aunque una falle
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ matrix.dotnet }}

      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-build -c Release
```

Resultado: 4 jobs en paralelo (2 versiones × 2 OS).

### Incluir y excluir combinaciones específicas

```yaml
strategy:
  matrix:
    dotnet: ['9.0.x', '10.0.x']
    os: [ubuntu-latest, windows-latest, macos-latest]
    include:
      # Agregar una combinación extra que no existe en el producto cartesiano
      - dotnet: '10.0.x'
        os: ubuntu-latest
        experimental: true
    exclude:
      # Excluir combinaciones sin sentido
      - dotnet: '9.0.x'
        os: macos-latest
```

### Matrix para despliegue multi-región

```yaml
jobs:
  deploy:
    name: Deploy to ${{ matrix.region }}
    strategy:
      matrix:
        region: [us-east-1, eu-west-1, ap-southeast-1]
      max-parallel: 1   # desplegar una región a la vez
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Deploy to ${{ matrix.region }}
        run: |
          aws ecs update-service \
            --region ${{ matrix.region }} \
            --cluster my-cluster \
            --service my-service \
            --force-new-deployment
```

---

## Glosario

| Término | Definición |
|---------|-----------|
| Workflow | archivo YAML en `.github/workflows/` que define el pipeline |
| Job | unidad de trabajo que corre en un runner; los jobs se pueden paralizar con `needs` |
| Step | comando o action dentro de un job |
| Action | step reutilizable publicado en el Marketplace o en el propio repo |
| Runner | máquina virtual donde corre el job (ubuntu-latest, windows-latest, etc.) |
| OIDC | OpenID Connect — permite que GitHub Actions obtenga credenciales temporales de AWS/Azure sin secrets de larga duración |
| Reusable workflow | workflow con `workflow_call` que puede ser invocado por otros workflows |
| Matrix build | estrategia que ejecuta el mismo job con múltiples combinaciones de variables en paralelo |
| `needs` | declara dependencia entre jobs — un job no empieza hasta que su dependencia termine |
| `fail-fast` | en matrix builds: si es `true` (default), cancela todas las combinaciones si una falla |
| Artifact | archivo generado por el workflow (binarios, reportes) que se puede pasar entre jobs o descargar |
| GITHUB_TOKEN | token temporal generado automáticamente por GitHub Actions para acceder al repositorio |
| ECR | Amazon Elastic Container Registry — registro de imágenes Docker en AWS |
| ECS | Amazon Elastic Container Service — orquestador de contenedores de AWS |

---

*Rogelio Arriaga Gonzalez*
