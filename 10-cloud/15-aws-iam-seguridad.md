# 15 · AWS IAM — Identidad y Acceso

## Problema que resuelve

Sin IAM correctamente configurado, un equipo termina compartiendo credenciales de root, usando access keys estáticas en el código, dando permisos de administrador a todos los servicios, y sin registro de quién hizo qué. El resultado es una cuenta imposible de auditar, con credenciales comprometidas en git y sin forma de revocar accesos granularmente. IAM es el sistema de control de acceso de AWS y la primera línea de defensa de toda la infraestructura.

---

## Regla de oro: nunca usar la cuenta root

La cuenta root tiene acceso irrestricto a TODOS los recursos y NUNCA puede ser limitada por políticas. Si se compromete, el atacante tiene control total de la cuenta.

```
Cuenta root — solo usarla para:
  ✓ Habilitar MFA en la cuenta root (primera vez)
  ✓ Cambiar el plan de soporte
  ✓ Cerrar la cuenta
  ✓ Restaurar acceso si todos los admins IAM están bloqueados

Para todo lo demás:
  → Crear usuario IAM con permisos administrativos limitados
  → Usar ese usuario con MFA activado
  → Nunca compartir las credenciales root
```

---

## Usuarios IAM (Users)

Son identidades permanentes para personas reales o aplicaciones que requieren acceso a la consola web o CLI.

```bash
# Crear usuario IAM
aws iam create-user --user-name rogelio-arriaga

# Crear access keys para CLI (evitar — usar roles cuando sea posible)
aws iam create-access-key --user-name rogelio-arriaga

# Habilitar MFA virtual (TOTP) — requiere app de autenticacion
aws iam enable-mfa-device \
  --user-name rogelio-arriaga \
  --serial-number arn:aws:iam::123456789:mfa/rogelio-arriaga \
  --authentication-code1 123456 \
  --authentication-code2 789012

# Crear login profile (acceso a consola web)
aws iam create-login-profile \
  --user-name rogelio-arriaga \
  --password "TempPass123!" \
  --password-reset-required
```

**Buenas prácticas para usuarios:**
- Un usuario por persona (nunca compartidos)
- MFA obligatorio para todos los usuarios con acceso a consola
- Rotar access keys cada 90 días (o no usarlas — preferir roles)
- Password policy fuerte: mínimo 12 caracteres, letras + números + símbolos

---

## Grupos IAM (Groups)

Los grupos son colecciones de usuarios que comparten las mismas políticas.

```bash
# Crear grupo de administradores de la app
aws iam create-group --group-name BackendDevelopers

# Adjuntar politica al grupo (no a usuarios individuales)
aws iam attach-group-policy \
  --group-name BackendDevelopers \
  --policy-arn arn:aws:iam::aws:policy/ReadOnlyAccess

# Agregar usuario al grupo
aws iam add-user-to-group \
  --group-name BackendDevelopers \
  --user-name rogelio-arriaga
```

**Restricciones de grupos:**
- Los grupos NO pueden contener otros grupos (no hay anidamiento)
- Un usuario puede pertenecer a múltiples grupos
- Los grupos no pueden asumir roles (solo usuarios pueden)

---

## Roles IAM (Roles)

Los roles son identidades temporales sin credenciales estáticas. Son asumidos por servicios (EC2, Lambda, ECS), pipelines (GitHub Actions) o cuentas externas.

```
Diferencia fundamental:
  User: tiene credenciales permanentes (password + access keys)
  Role: no tiene credenciales propias — genera credenciales temporales al asumir el rol
  
  Las credenciales de un rol son:
    AccessKeyId + SecretAccessKey + SessionToken
    Duran 15 min a 12 horas (configurable)
    Rotan automáticamente
```

### Tipos de confianza (quién puede asumir el rol)

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Service": "ec2.amazonaws.com"
      },
      "Action": "sts:AssumeRole"
    }
  ]
}
```

| Principal | Caso de uso |
|-----------|-------------|
| `ec2.amazonaws.com` | EC2 accede a S3/Secrets Manager |
| `ecs-tasks.amazonaws.com` | Contenedor ECS accede a servicios AWS |
| `lambda.amazonaws.com` | Lambda accede a DynamoDB/S3 |
| `arn:aws:iam::CUENTA:root` | Cross-account: otra cuenta AWS asume este rol |
| OIDC provider (GitHub) | GitHub Actions asume rol sin access keys |

---

## Políticas IAM (Policies)

Las políticas son documentos JSON que definen qué acciones están permitidas o denegadas sobre qué recursos.

### Estructura de una política

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "LeerSecretos",
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue",
        "secretsmanager:DescribeSecret"
      ],
      "Resource": "arn:aws:secretsmanager:us-east-1:123456789:secret:prod/*",
      "Condition": {
        "StringEquals": {
          "aws:RequestedRegion": "us-east-1"
        }
      }
    },
    {
      "Sid": "DenegarEliminacion",
      "Effect": "Deny",
      "Action": "secretsmanager:DeleteSecret",
      "Resource": "*"
    }
  ]
}
```

| Campo | Descripción |
|-------|-------------|
| `Version` | Siempre `"2012-10-17"` (versión del lenguaje de políticas) |
| `Statement` | Array de declaraciones. Todas se evalúan. |
| `Sid` | Identificador opcional para la declaración |
| `Effect` | `Allow` o `Deny` |
| `Principal` | Quién (solo en resource-based policies y trust policies) |
| `Action` | Qué acciones. `s3:GetObject`, `rds:*`, `"*"` |
| `Resource` | ARN del recurso. `arn:aws:s3:::bucket/*`, `"*"` |
| `Condition` | Condiciones opcionales (región, IP, MFA, etc.) |

### Tipos de política

| Tipo | Descripción | Caso de uso |
|------|-------------|-------------|
| AWS Managed | Creada y mantenida por AWS | `AmazonS3ReadOnlyAccess`, `PowerUserAccess` |
| Customer Managed | Creada por tu cuenta, reutilizable | Política específica de tu app |
| Inline | Adjunta directamente a un user/role/group | Permisos únicos que no se deben compartir |

**Preferir Customer Managed sobre Inline:** las inline no se pueden reutilizar ni auditar fácilmente.

---

## Evaluación de políticas — quién gana

El motor de evaluación de IAM sigue este orden:

```
1. ¿Hay un SCP (Service Control Policy) que lo DENIEGUE?
   → Si: DENEGADO (no importa nada más)

2. ¿Hay un Deny explícito en alguna policy aplicable?
   → Si: DENEGADO

3. ¿Hay un Permission Boundary que no incluya esta acción?
   → Si: DENEGADO

4. ¿Hay un Allow explícito?
   → Si: PERMITIDO

5. Sin Allow explícito → DENEGADO por defecto (implicit deny)
```

```
Regla de oro:
  DENY explícito SIEMPRE gana, sin excepciones.
  "Allow *" en una policy no te ayuda si hay un Deny en otra policy.
```

---

## Principio de mínimo privilegio

Empezar con cero permisos y agregar solo lo estrictamente necesario.

```json
{
  "❌ MAL — acceso completo a todos los servicios": {
    "Effect": "Allow",
    "Action": "*",
    "Resource": "*"
  },

  "✓ BIEN — solo lo necesario": {
    "Effect": "Allow",
    "Action": [
      "secretsmanager:GetSecretValue",
      "s3:PutObject",
      "s3:GetObject"
    ],
    "Resource": [
      "arn:aws:secretsmanager:us-east-1:123456789:secret:prod/backtemplate/*",
      "arn:aws:s3:::gtm-suite-prod-files/uploads/*"
    ]
  }
}
```

**Proceso recomendado:**
1. Empezar con la política `ReadOnlyAccess` de AWS
2. Agregar solo los `Allow` específicos que necesita el servicio
3. Usar IAM Access Analyzer para detectar permisos no usados
4. Revisar periódicamente el Credential Report

---

## IAM Instance Profiles — EC2 sin access keys

```
Problema:
  ❌ Guardar access keys en el servidor
     AWS_ACCESS_KEY_ID=AKIA...  → en /etc/environment
     AWS_SECRET_ACCESS_KEY=...  → en el código
     
     Riesgo: si el servidor es comprometido, las keys son robadas.
     Las keys estáticas nunca expiran.

Solución:
  ✓ IAM Instance Profile
  
  1. Crear IAM Role con trust policy para ec2.amazonaws.com
  2. Adjuntar las políticas necesarias al role
  3. Crear Instance Profile (contenedor del role para EC2)
  4. Asignar el Instance Profile al lanzar la instancia
  
  El IMDS (169.254.169.254) provee credenciales temporales que rotan
  automáticamente cada 60 minutos. El SDK de AWS las obtiene solo.
```

```bash
# Crear el role
aws iam create-role \
  --role-name BackTemplateEC2Role \
  --assume-role-policy-document '{
    "Version": "2012-10-17",
    "Statement": [{
      "Effect": "Allow",
      "Principal": {"Service": "ec2.amazonaws.com"},
      "Action": "sts:AssumeRole"
    }]
  }'

# Adjuntar politicas
aws iam attach-role-policy \
  --role-name BackTemplateEC2Role \
  --policy-arn arn:aws:iam::123456789:policy/BackTemplateSecretsPolicy

# Crear Instance Profile
aws iam create-instance-profile \
  --instance-profile-name BackTemplateEC2Profile

# Agregar el role al Instance Profile
aws iam add-role-to-instance-profile \
  --instance-profile-name BackTemplateEC2Profile \
  --role-name BackTemplateEC2Role

# El SDK .NET obtiene las credenciales automaticamente
# No necesitas configurar nada en el código
```

```csharp
// El SDK busca credenciales en este orden:
// 1. Variables de entorno (AWS_ACCESS_KEY_ID)
// 2. AWS credentials file (~/.aws/credentials)
// 3. Instance Profile (si corre en EC2/ECS/Lambda)
// → En produccion siempre llega al paso 3

var secretsClient = new AmazonSecretsManagerClient();
// Sin credenciales explicitas — usa el Instance Profile automaticamente
```

---

## OIDC y Federación — GitHub Actions sin access keys

OIDC (OpenID Connect) permite que GitHub Actions asuma un rol IAM usando un token JWT, sin necesidad de guardar access keys como secrets en GitHub.

```
Flujo OIDC:
  1. GitHub Actions genera un JWT firmado por GitHub
  2. AWS verifica la firma del JWT con el OIDC provider de GitHub
  3. Si el JWT es válido y el claim coincide con la condición del role → AssumeRole
  4. AWS devuelve credenciales temporales (15 min a 1 hora)
  5. GitHub Actions usa esas credenciales para el deployment
```

```bash
# Crear OIDC provider en la cuenta AWS
aws iam create-open-id-connect-provider \
  --url https://token.actions.githubusercontent.com \
  --client-id-list sts.amazonaws.com \
  --thumbprint-list 6938fd4d98bab03faadb97b34396831e3780aea1

# Crear role que GitHub Actions puede asumir
aws iam create-role \
  --role-name GitHubActionsDeployRole \
  --assume-role-policy-document '{
    "Version": "2012-10-17",
    "Statement": [{
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::123456789:oidc-provider/token.actions.githubusercontent.com"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "token.actions.githubusercontent.com:aud": "sts.amazonaws.com"
        },
        "StringLike": {
          "token.actions.githubusercontent.com:sub": "repo:tu-org/back-template:*"
        }
      }
    }]
  }'
```

```yaml
# .github/workflows/deploy.yml
jobs:
  deploy:
    permissions:
      id-token: write    # necesario para OIDC
      contents: read
    steps:
      - name: Asumir rol AWS via OIDC
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::123456789:role/GitHubActionsDeployRole
          aws-region: us-east-1
          # Sin access-key-id ni secret-access-key — usa OIDC
      
      - name: Subir imagen a ECR
        run: |
          aws ecr get-login-password | docker login --username AWS --password-stdin $ECR_REGISTRY
          docker push $ECR_REGISTRY/back-template:$GITHUB_SHA
```

---

## Tipos de credenciales

| Tipo | Duración | Rotación | Caso de uso |
|------|----------|----------|-------------|
| Access Keys (long-term) | No expiran | Manual | CLI de desarrolladores (con MFA) |
| Credenciales de Instance Profile | 60 min | Automática | EC2, ECS, Lambda |
| STS Session Token | 15 min - 12 horas | Automática | AssumeRole, OIDC federation |
| Contraseña de consola | No expira | Configurar política | Acceso a consola web |

---

## Permission Boundaries

Límites máximos de permisos que un usuario o role puede tener, independientemente de las políticas adjuntas.

```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Action": [
      "s3:*",
      "cloudwatch:*",
      "logs:*"
    ],
    "Resource": "*"
  }]
}
```

```bash
# Adjuntar un Permission Boundary a un role
aws iam put-role-permissions-boundary \
  --role-name DeveloperRole \
  --permissions-boundary arn:aws:iam::123456789:policy/DeveloperBoundary

# El role no puede exceder los permisos definidos en el boundary,
# aunque tenga adjuntas políticas más permisivas.
```

**Caso de uso:** delegar creación de roles a desarrolladores sin que puedan crear roles con más permisos que ellos mismos.

---

## Service Control Policies (SCPs) — Organizations

Los SCPs aplican límites a nivel de cuenta completa dentro de AWS Organizations.

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Deny",
      "Action": "*",
      "Resource": "*",
      "Condition": {
        "StringNotEquals": {
          "aws:RequestedRegion": ["us-east-1", "us-west-2"]
        }
      }
    }
  ]
}
```

Este SCP impide crear recursos en cualquier región excepto `us-east-1` y `us-west-2`, independientemente de los permisos IAM de los usuarios.

---

## Auditoría con CloudTrail e IAM Access Analyzer

### CloudTrail

Registra cada llamada a la API de AWS (quién, qué, cuándo, desde dónde).

```bash
# Buscar quién elimino un secret en Secrets Manager
aws cloudtrail lookup-events \
  --lookup-attributes AttributeKey=EventName,AttributeValue=DeleteSecret \
  --start-time "2026-06-01T00:00:00Z" \
  --end-time "2026-06-02T00:00:00Z" \
  --query 'Events[*].[EventTime,Username,Resources[0].ResourceName]' \
  --output table
```

### IAM Access Analyzer

Detecta recursos que están accesibles desde fuera de tu cuenta (políticas demasiado permisivas).

```bash
# Crear analyzer para toda la cuenta
aws accessanalyzer create-analyzer \
  --analyzer-name prod-account-analyzer \
  --type ACCOUNT

# Ver hallazgos (recursos con acceso externo)
aws accessanalyzer list-findings \
  --analyzer-name prod-account-analyzer \
  --filter '{"status": {"eq": ["ACTIVE"]}}'
```

### Credential Report

Reporte de todos los usuarios IAM con el estado de sus credenciales.

```bash
# Generar el reporte
aws iam generate-credential-report

# Descargar el reporte
aws iam get-credential-report \
  --query 'Content' \
  --output text | base64 --decode > credential-report.csv

# Revisar: usuarios sin MFA, access keys viejas (>90 dias), password sin usar
```

---

## Política IAM completa para el back-template

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "SecretsManager",
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue",
        "secretsmanager:DescribeSecret"
      ],
      "Resource": "arn:aws:secretsmanager:us-east-1:123456789:secret:prod/backtemplate/*"
    },
    {
      "Sid": "S3Archivos",
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:GetObjectAttributes"
      ],
      "Resource": "arn:aws:s3:::gtm-suite-prod-files/*"
    },
    {
      "Sid": "S3ListBucket",
      "Effect": "Allow",
      "Action": "s3:ListBucket",
      "Resource": "arn:aws:s3:::gtm-suite-prod-files"
    },
    {
      "Sid": "CloudWatch",
      "Effect": "Allow",
      "Action": [
        "cloudwatch:PutMetricData",
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents",
        "logs:DescribeLogStreams"
      ],
      "Resource": "*"
    },
    {
      "Sid": "SSMSessionManager",
      "Effect": "Allow",
      "Action": [
        "ssm:UpdateInstanceInformation",
        "ssmmessages:CreateControlChannel",
        "ssmmessages:CreateDataChannel",
        "ssmmessages:OpenControlChannel",
        "ssmmessages:OpenDataChannel"
      ],
      "Resource": "*"
    }
  ]
}
```

---

## Relación con back-template

```
GitHub Actions (OIDC) → AssumeRole GitHubActionsDeployRole
  → Push imagen a ECR
  → Deploy en ECS/EC2

EC2 Instance Profile (BackTemplateEC2Role):
  → Obtiene credenciales de Secrets Manager (connection string, JWT secret)
  → Sube archivos a S3
  → Envía métricas a CloudWatch
  → Acceso via Session Manager (sin puerto 22)
```

El back-template NUNCA debe tener access keys en:
- Archivos de configuración
- Variables de entorno hardcodeadas
- Código fuente
- Repositorio git

---

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| IAM Roles para servicios (EC2, Lambda, ECS) | Access keys estáticas en servidores |
| OIDC para GitHub Actions | Secrets de GitHub con `AWS_ACCESS_KEY_ID` |
| Grupos para asignar políticas a usuarios | Políticas inline directamente en usuarios |
| Customer Managed Policies para reutilización | AWS Managed Policies con más permisos de los necesarios |
| Permission Boundaries para delegar creación de roles | Dar `iam:*` sin restricciones |
| MFA obligatorio para todos los usuarios de consola | Acceso a consola sin MFA |
| CloudTrail activo en todas las regiones | Confiar en que nadie accede indebidamente |
| Credential Report mensual para auditoría | Dejar access keys sin rotar por más de 90 días |

---

## Glosario

| Término | Definición |
|---------|-----------|
| IAM | Identity and Access Management. Sistema de control de acceso de AWS. |
| Usuario IAM | Identidad permanente para una persona. Tiene password y/o access keys. |
| Grupo IAM | Colección de usuarios que comparten políticas. No anidable. |
| Rol IAM | Identidad temporal asumible por servicios, usuarios o cuentas externas. |
| Policy | Documento JSON con permisos (Effect, Action, Resource). |
| AWS Managed Policy | Política creada por AWS. No personalizable. |
| Customer Managed Policy | Política creada por tu cuenta. Reutilizable. |
| Inline Policy | Política adjunta directamente a un user/role/group. No reutilizable. |
| Instance Profile | Contenedor de un IAM Role que puede asignarse a una instancia EC2. |
| IMDS | Instance Metadata Service. Servidor en 169.254.169.254 que provee credenciales temporales. |
| AssumeRole | Acción de STS que genera credenciales temporales para un rol. |
| STS | Security Token Service. Genera credenciales temporales para roles. |
| OIDC | OpenID Connect. Protocolo de identidad federada. Permite que GitHub Actions asuma roles. |
| Permission Boundary | Límite máximo de permisos para un user/role, independiente de las políticas adjuntas. |
| SCP | Service Control Policy. Límites de permisos a nivel de cuenta en AWS Organizations. |
| CloudTrail | Servicio que registra cada llamada a la API de AWS. Auditoría. |
| IAM Access Analyzer | Detecta recursos con acceso externo no intencional. |
| Credential Report | Reporte de estado de credenciales de todos los usuarios IAM. |
| MFA | Multi-Factor Authentication. Segundo factor de autenticación (TOTP). |
| Least Privilege | Principio de mínimo privilegio: dar solo los permisos necesarios, nada más. |

> Fuente: *AWS Certified Solutions Architect – Associate Guide* (Gabriel Ramirez, Packt) — Ch.1 IAM, Ch.13 Security Best Practices; *AWS Certified Security Specialty SCS-C02 Exam Guide* — Ch.1 Identity and Access Management

---

*Rogelio Arriaga Gonzalez*
