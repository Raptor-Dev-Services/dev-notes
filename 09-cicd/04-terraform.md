# 04 — Terraform: Infraestructura como código

Terraform permite definir infraestructura cloud (AWS, Azure, GCP) en archivos de configuración versionables y reproducibles.

---

## Conceptos fundamentales
> Fuente: *Terraform Up and Running* (Brikman) — Ch.1 Why Terraform

```
Provider   = plugin que conecta Terraform con un cloud (aws, azurerm, google)
Resource   = un componente de infraestructura (instancia EC2, base de datos, etc.)
Data Source = información de recursos ya existentes (no crea, solo lee)
Variable   = parámetros de entrada configurables
Output     = valores que se exportan para otros módulos
State      = el estado actual de la infraestructura (terraform.tfstate)
```

---

## Estructura de un proyecto Terraform

```
infra/
├── main.tf          — recursos principales
├── variables.tf     — definición de variables
├── outputs.tf       — valores de salida
├── providers.tf     — configuración de providers
├── terraform.tfvars — valores de las variables (no commitear con secretos)
└── modules/
    ├── ecs/         — módulo reutilizable para ECS
    │   ├── main.tf
    │   ├── variables.tf
    │   └── outputs.tf
    └── rds/
        └── ...
```

---

## Provider AWS

```hcl
# providers.tf
terraform {
  required_version = ">= 1.6"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  # Backend remoto — el estado se guarda en S3 (no en local)
  backend "s3" {
    bucket         = "gtm-suite-terraform-state"
    key            = "production/terraform.tfstate"
    region         = "us-east-1"
    encrypt        = true
    dynamodb_table = "gtm-suite-terraform-locks"   # para locking
  }
}

provider "aws" {
  region = var.aws_region
}
```

---

## Recursos principales para el stack GTM Suite

```hcl
# variables.tf
variable "aws_region"    { default = "us-east-1" }
variable "environment"   { default = "production" }
variable "app_name"      { default = "gtm-suite" }
variable "db_password"   {
  type      = string
  sensitive = true   # no se muestra en logs ni output
}

# main.tf — ECS Fargate
resource "aws_ecs_cluster" "main" {
  name = "${var.app_name}-${var.environment}"

  setting {
    name  = "containerInsights"
    value = "enabled"
  }
}

resource "aws_ecs_task_definition" "api" {
  family                   = "${var.app_name}-api"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = 512
  memory                   = 1024
  execution_role_arn       = aws_iam_role.ecs_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  container_definitions = jsonencode([{
    name  = "api"
    image = "${aws_ecr_repository.api.repository_url}:latest"

    portMappings = [{ containerPort = 8080, protocol = "tcp" }]

    environment = [
      { name = "ASPNETCORE_ENVIRONMENT", value = var.environment }
    ]

    secrets = [
      {
        name      = "ConnectionStrings__MainDb"
        valueFrom = aws_secretsmanager_secret.db_connection.arn
      }
    ]

    logConfiguration = {
      logDriver = "awslogs"
      options = {
        "awslogs-group"         = aws_cloudwatch_log_group.api.name
        "awslogs-region"        = var.aws_region
        "awslogs-stream-prefix" = "ecs"
      }
    }

    healthCheck = {
      command     = ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
      interval    = 30
      timeout     = 5
      retries     = 3
      startPeriod = 30
    }
  }])
}

resource "aws_ecs_service" "api" {
  name            = "${var.app_name}-api"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.api.arn
  desired_count   = 2
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = aws_subnet.private[*].id
    security_groups  = [aws_security_group.api.id]
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.api.arn
    container_name   = "api"
    container_port   = 8080
  }

  deployment_circuit_breaker {
    enable   = true
    rollback = true   # rollback automático si el deploy falla
  }
}
```

```hcl
# RDS PostgreSQL
resource "aws_db_instance" "postgres" {
  identifier = "${var.app_name}-${var.environment}"

  engine         = "postgres"
  engine_version = "17"
  instance_class = "db.t3.medium"

  allocated_storage     = 20
  max_allocated_storage = 100   # auto-scaling de storage

  db_name  = "gtmsuite"
  username = "gtmadmin"
  password = var.db_password

  vpc_security_group_ids = [aws_security_group.rds.id]
  db_subnet_group_name   = aws_db_subnet_group.main.name

  backup_retention_period = 7   # 7 días de backups automáticos
  backup_window           = "03:00-04:00"
  maintenance_window      = "sun:04:00-sun:05:00"

  deletion_protection = true   # protección contra `terraform destroy` accidental
  skip_final_snapshot = false
  final_snapshot_identifier = "${var.app_name}-final-snapshot"

  tags = {
    Environment = var.environment
    Project     = var.app_name
  }
}
```

---

## Comandos del flujo de trabajo

```bash
# Inicializar — descargar providers, configurar backend
terraform init

# Planificar — ver qué va a cambiar SIN aplicar
terraform plan

# Planificar y guardar el plan en archivo (para CI/CD)
terraform plan -out=tfplan

# Aplicar cambios (pide confirmación)
terraform apply

# Aplicar el plan guardado (sin confirmación — para CI/CD)
terraform apply tfplan

# Ver el estado actual
terraform state list
terraform state show aws_ecs_service.api

# Destruir infraestructura (peligroso — pide confirmación)
terraform destroy

# Importar un recurso existente al estado
terraform import aws_s3_bucket.logs gtm-suite-logs-bucket

# Formatear archivos .tf
terraform fmt

# Validar la sintaxis
terraform validate
```

---

## Outputs — exponer valores para otros módulos

```hcl
# outputs.tf
output "ecr_repository_url" {
  description = "URL del repositorio ECR para el pipeline de CI/CD"
  value       = aws_ecr_repository.api.repository_url
}

output "rds_endpoint" {
  description = "Endpoint de la base de datos (sin credenciales)"
  value       = aws_db_instance.postgres.endpoint
}

output "load_balancer_dns" {
  description = "DNS del Application Load Balancer"
  value       = aws_lb.main.dns_name
}

# Usar el output de otro módulo
module "network" {
  source = "./modules/network"
  # ...
}

module "ecs" {
  source     = "./modules/ecs"
  vpc_id     = module.network.vpc_id        # output del módulo network
  subnet_ids = module.network.private_subnet_ids
}
```

---

## Terraform en CI/CD

```yaml
# .github/workflows/terraform.yml
name: Terraform

on:
  pull_request:
    paths: ["infra/**"]
  push:
    branches: [main]
    paths: ["infra/**"]

jobs:
  terraform:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: infra/

    steps:
      - uses: actions/checkout@v4

      - uses: hashicorp/setup-terraform@v3
        with:
          terraform_version: "1.7.x"

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: ${{ secrets.TERRAFORM_ROLE_ARN }}
          aws-region: us-east-1

      - name: Terraform init
        run: terraform init

      - name: Terraform validate
        run: terraform validate

      - name: Terraform plan
        run: terraform plan -out=tfplan
        env:
          TF_VAR_db_password: ${{ secrets.DB_PASSWORD }}

      # Solo en push a main — no en PR
      - name: Terraform apply
        if: github.event_name == 'push'
        run: terraform apply tfplan
```

---

## Módulos reutilizables
> Fuente: *Terraform: Up and Running* (Brikman) — Ch.4 How to Create Reusable Infrastructure with Terraform Modules

Un módulo es cualquier conjunto de archivos `.tf` en una carpeta. Los módulos reutilizables permiten definir un componente de infraestructura una vez y usarlo en múltiples entornos (staging, producción).

```
infra/
├── modules/                      ← módulos reutilizables (blueprints)
│   ├── ecs-service/
│   │   ├── main.tf
│   │   ├── variables.tf          ← inputs del módulo
│   │   └── outputs.tf            ← valores exportados
│   └── rds-postgres/
│       ├── main.tf
│       ├── variables.tf
│       └── outputs.tf
└── live/                         ← infraestructura real por ambiente
    ├── staging/
    │   └── main.tf               ← usa los módulos con config de staging
    └── production/
        └── main.tf               ← usa los mismos módulos con config de prod
```

```hcl
# modules/ecs-service/variables.tf — inputs del módulo
variable "service_name" {
  description = "Nombre del servicio ECS"
  type        = string
}

variable "docker_image" {
  description = "URL de la imagen Docker (ECR)"
  type        = string
}

variable "desired_count" {
  description = "Número de tareas a correr"
  type        = number
  default     = 2
}

variable "container_port" {
  description = "Puerto que expone el contenedor"
  type        = number
  default     = 8080
}

# modules/ecs-service/outputs.tf — valores que el módulo exporta
output "service_name" {
  value       = aws_ecs_service.this.name
  description = "Nombre del servicio ECS creado"
}

output "service_arn" {
  value       = aws_ecs_service.this.id
}
```

```hcl
# live/staging/main.tf — consumir el módulo en staging
module "api_service" {
  source = "../../modules/ecs-service"

  service_name  = "gtm-api-staging"
  docker_image  = "${var.ecr_url}:staging-latest"
  desired_count = 1   # menos réplicas en staging
  container_port = 8080
}

# live/production/main.tf — mismo módulo, config de producción
module "api_service" {
  source = "../../modules/ecs-service"

  service_name  = "gtm-api-production"
  docker_image  = "${var.ecr_url}:v1.2.0"
  desired_count = 3   # más réplicas en producción
  container_port = 8080
}
```

### Módulos versionados desde Git

```hcl
# Apuntar a un tag específico del repo de módulos (no a un path local)
# Esto permite usar v0.0.1 en prod y v0.0.2 en staging sin que afecten entre sí
module "api_service" {
  source = "github.com/mi-org/infra-modules//ecs-service?ref=v0.0.2"

  service_name  = "gtm-api-staging"
  docker_image  = "${var.ecr_url}:staging"
  desired_count = 1
}

# En producción — versión estable anterior
module "api_service" {
  source = "github.com/mi-org/infra-modules//ecs-service?ref=v0.0.1"

  service_name  = "gtm-api-production"
  docker_image  = "${var.ecr_url}:v1.2.0"
  desired_count = 3
}
```

```bash
# Siempre correr init después de cambiar source o agregar un módulo
terraform init

# Etiquetar el repo de módulos con semver
git tag -a "v0.0.2" -m "Add health check to ECS service module"
git push --follow-tags
```

---

## Aislamiento de ambientes — file layout vs workspaces
> Fuente: *Terraform: Up and Running* (Brikman) — Ch.3 How to Manage Terraform State

### Por qué NO usar workspaces para aislar ambientes

Los workspaces de Terraform parecen una solución obvia para staging/producción, pero tienen problemas:
- Todos los workspaces comparten el mismo backend (mismo bucket S3, mismas credenciales IAM)
- No son visibles en el código — es fácil ejecutar `terraform destroy` en el workspace equivocado
- No proveen verdadero aislamiento de permisos entre ambientes

### Estructura recomendada: carpetas separadas por ambiente

```
infra/
├── global/                      ← recursos compartidos entre ambientes (IAM, S3)
│   ├── s3/
│   └── iam/
├── staging/
│   ├── vpc/
│   ├── services/
│   │   └── api/
│   │       ├── main.tf          ← backend apunta a staging state bucket
│   │       ├── variables.tf
│   │       └── terraform.tfvars
│   └── data-storage/
│       └── rds/
└── production/
    ├── vpc/
    ├── services/
    │   └── api/
    │       ├── main.tf          ← backend apunta a production state bucket
    │       ├── variables.tf
    │       └── terraform.tfvars
    └── data-storage/
        └── rds/
```

```hcl
# staging/services/api/main.tf — backend propio por ambiente
terraform {
  backend "s3" {
    bucket         = "gtm-suite-staging-tfstate"
    key            = "services/api/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "gtm-suite-staging-tf-locks"
    encrypt        = true
  }
}

# production/services/api/main.tf — bucket y tabla DynamoDB distintos
terraform {
  backend "s3" {
    bucket         = "gtm-suite-prod-tfstate"    # bucket diferente → cuenta AWS diferente
    key            = "services/api/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "gtm-suite-prod-tf-locks"
    encrypt        = true
  }
}
```

**Ventaja clave:** cada ambiente puede tener su propia cuenta AWS con permisos separados. Un desarrollador puede tener acceso total a staging pero solo lectura en producción.

---

## Cuándo usar Terraform

| Usar | No usar |
|------|---------|
| Infraestructura que cambia con regularidad | Infraestructura creada una sola vez y nunca modificada |
| Equipos con más de 1 persona manejando infraestructura | Recursos efímeros de desarrollo local |
| Necesitas reproducir entornos (staging = producción) | Configuración de software dentro de instancias (usar Ansible) |
| Auditoría de cambios de infraestructura en git | Cuando la consola de AWS es suficiente para el tamaño del proyecto |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Provider | plugin de Terraform que conecta con un cloud (aws, azurerm, google) — gestiona autenticación y API calls |
| Resource | bloque que define un componente de infraestructura real (`aws_ecs_service`, `aws_db_instance`) |
| Data Source | bloque que lee recursos existentes sin crearlos — útil para referenciar VPCs o AMIs existentes |
| Variable | parámetro de entrada del módulo o del root module — permite reutilizar configuración |
| Output | valor que expone un módulo para que otros módulos o el usuario lo consuman |
| State | archivo que mapea los recursos de Terraform con los recursos reales del cloud (`terraform.tfstate`) |
| Remote State | estado guardado en un backend externo (S3) en lugar de localmente — necesario para equipos |
| Backend | sistema que almacena el state remotamente (S3, Azure Blob, Terraform Cloud) |
| State Locking | mecanismo para evitar ejecuciones simultáneas que corrompan el state (DynamoDB en AWS) |
| Módulo | carpeta con archivos `.tf` que encapsula un conjunto de recursos reutilizables |
| Root Module | el directorio donde se ejecuta `terraform apply` — punto de entrada de la ejecución |
| Plan | operación de solo lectura que calcula qué recursos se crearán, modificarán o destruirán |
| Apply | operación que ejecuta el plan y materializa los cambios en el cloud |
| `terraform.tfvars` | archivo que provee valores para las variables — nunca commitear si contiene secretos |
| `terraform init` | descarga providers, configura el backend y los módulos — siempre el primer paso |
| `terraform fmt` | formatea los archivos `.tf` con el estilo estándar de HCL |
| `terraform validate` | valida la sintaxis y coherencia de la configuración sin contactar el cloud |
| HCL | HashiCorp Configuration Language — el lenguaje declarativo de Terraform |
| Workspace | contexto de ejecución alternativo (no recomendado para aislar ambientes) |
| Sensitive | marca una variable u output como secreto — no se muestra en logs ni en plan output |
| `depends_on` | declaración explícita de dependencia entre recursos cuando Terraform no la infiere |
| Lifecycle | bloque de control que define `create_before_destroy`, `prevent_destroy` o `ignore_changes` |
| `prevent_destroy` | lifecycle rule que impide que `terraform destroy` elimine un recurso crítico |
| `terraform import` | importa un recurso existente (creado fuera de Terraform) al state actual |
| `terraform taint` | (pre-v1.0) marca un recurso para ser recreado en el próximo apply; ahora `terraform apply -replace` |
| `for_each` | meta-argumento para crear múltiples recursos desde un mapa — alternativa a `count` |
| `count` | meta-argumento para crear N copias de un recurso usando un número entero |
| `locals` | bloque para definir valores intermedios reutilizables dentro de un módulo |
| `terraform_remote_state` | data source para leer outputs del state de otro módulo remoto |

---

*Rogelio Arriaga Gonzalez*
