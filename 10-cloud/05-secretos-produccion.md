# 05 · Secretos en producción

> Fuente: *AWS Certified Solutions Architect – Associate Guide* Ch.14 (KMS, encryption) + Ch.15 (Systems Manager Parameter Store) — *Architecting ASP.NET Core Applications* (Packt) Ch.9 (Azure Key Vault, Managed Identities)

## Problema que resuelve

Los secretos (connection strings, API keys, JWT secrets) no pueden vivir en código fuente ni en variables de entorno del sistema operativo sin cifrar. Se necesita un vault centralizado con acceso controlado, rotación y auditoría.

## Reglas fundamentales

- Nunca commitear secretos en Git (`.env`, `appsettings.Production.json` con valores reales)
- Nunca pasar secretos como argumentos de línea de comandos (quedan en historial de shell)
- Nunca almacenar secretos en variables de entorno del pipeline en texto plano
- Siempre usar el servicio de secretos del cloud proveedor

## AWS Secrets Manager

Vault completamente gestionado con rotación automática de secretos.

### Crear un secreto

```bash
# crear secreto con JSON (connection string + JWT)
aws secretsmanager create-secret \
  --name "gtm-suite/production" \
  --description "Secrets for GTM Suite API in production" \
  --secret-string '{
    "ConnectionStrings__Default": "Host=gtm-db.xyz.rds.amazonaws.com;...",
    "Jwt__Secret": "super-secret-key-256-bits",
    "Redis__ConnectionString": "redis.xyz.cache.amazonaws.com:6380,ssl=true"
  }'
```

### Leer el secreto en .NET

```bash
dotnet add package AWSSDK.SecretsManager
dotnet add package Amazon.SecretsManager.AWSSDK.Extensions.NETCore.Setup
```

```csharp
// Program.cs
builder.Configuration.AddSecretsManager(region: RegionEndpoint.USEast1, configurator: opts =>
{
    opts.SecretFilter = entry => entry.Name.StartsWith("gtm-suite/");
    opts.KeyGenerator = (entry, key) => key
        .Replace("gtm-suite/production:", "")
        .Replace("__", ":");
});
```

Con esto, `Jwt__Secret` del secreto de AWS mapea a `Jwt:Secret` en la configuración de .NET automáticamente.

### Política IAM para leer el secreto

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
      "Resource": "arn:aws:secretsmanager:us-east-1:123456789:secret:gtm-suite/*"
    }
  ]
}
```

Esta política se adjunta al IAM Role de la task definition de ECS. La app nunca tiene credenciales propias — asume el rol del contenedor.

### Rotación automática

```bash
# rotar el secreto cada 30 días con una Lambda
aws secretsmanager rotate-secret \
  --secret-id "gtm-suite/production" \
  --rotation-lambda-arn arn:aws:lambda:us-east-1:123456789:function:SecretsRotation \
  --rotation-rules AutomaticallyAfterDays=30
```

## AWS Systems Manager Parameter Store

Alternativa más simple (y más barata) para secretos sin rotación automática.

```bash
# crear parámetro cifrado con KMS
aws ssm put-parameter \
  --name "/gtm-suite/production/jwt-secret" \
  --type SecureString \
  --value "super-secret-key" \
  --description "JWT signing key"

# leer el valor
aws ssm get-parameter \
  --name "/gtm-suite/production/jwt-secret" \
  --with-decryption \
  --query Parameter.Value \
  --output text
```

### Cuándo usar Parameter Store vs Secrets Manager

| Criterio | Parameter Store | Secrets Manager |
|----------|----------------|-----------------|
| Costo | gratuito (Standard) | $0.40/secreto/mes |
| Rotación automática | no | sí (con Lambda) |
| Tamaño máximo | 4 KB (Standard) | 65 KB |
| Caso de uso | config no sensible, flags | credenciales de DB, API keys |

## Azure Key Vault

Equivalente en Azure.

```bash
# crear Key Vault
az keyvault create \
  --name gtm-keyvault \
  --resource-group gtm-rg \
  --location eastus

# agregar secreto
az keyvault secret set \
  --vault-name gtm-keyvault \
  --name "ConnectionStrings--Default" \
  --value "Host=gtm-db.postgres.database.azure.com;..."
```

### Leer desde .NET con Managed Identity

```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

```csharp
// Program.cs
var keyVaultUri = new Uri($"https://gtm-keyvault.vault.azure.net/");
builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
```

`DefaultAzureCredential` usa automáticamente la Managed Identity del App Service en producción y las credenciales del desarrollador (`az login`) en local. No se necesita ningún secret en código.

### Política de acceso

```bash
# dar permiso de lectura al App Service (usando su Managed Identity)
az keyvault set-policy \
  --name gtm-keyvault \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

## Secretos en el pipeline CI/CD

Los pipelines nunca deben tener secretos hardcodeados. Se usan los secrets del sistema de CI:

**GitHub Actions:**
```yaml
# usar secret almacenado en GitHub Settings → Secrets
- name: Configure AWS credentials
  uses: aws-actions/configure-aws-credentials@v4
  with:
    aws-access-key-id:     ${{ secrets.AWS_ACCESS_KEY_ID }}
    aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
```

**Azure DevOps:**
```yaml
# usar variable group vinculado a Key Vault
variables:
  - group: prod-secrets   # synced from Key Vault
```

## Secretos en desarrollo local

```csharp
// en desarrollo usar dotnet user-secrets (nunca commitear)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;..."
dotnet user-secrets set "Jwt:Secret" "dev-secret-key"

// Program.cs — user-secrets se carga automáticamente en Development
// No se necesita código adicional si se usa CreateBuilder(args)
```

Los user-secrets se almacenan en `%APPDATA%\Microsoft\UserSecrets\<guid>\secrets.json` y nunca en el repositorio.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| AWS Secrets Manager para credenciales de DB en ECS | variables de entorno sin cifrar en task definitions |
| Parameter Store para configuración que no es crítica | hardcodear secretos en `appsettings.Production.json` |
| Azure Key Vault + Managed Identity para cualquier servicio en Azure | service principals con secretos de larga vida |
| user-secrets en desarrollo local | `.env` con secretos reales commiteado al repo |

---

## Detección de secretos expuestos
> Fuente: *AWS Certified Security Specialty* — Ch.6 Data Protection in AWS

### Pre-commit con git-secrets o detect-secrets

```bash
# Instalar detect-secrets
pip install detect-secrets

# Escanear el repositorio completo
detect-secrets scan > .secrets.baseline

# Agregar al pre-commit hook
cat >> .git/hooks/pre-commit << 'EOF'
detect-secrets-hook --baseline .secrets.baseline
EOF
chmod +x .git/hooks/pre-commit

# Actualizar baseline cuando se agrega un falso positivo intencionalmente
detect-secrets scan --update .secrets.baseline
```

```yaml
# .pre-commit-config.yaml — usando pre-commit framework
repos:
  - repo: https://github.com/Yelp/detect-secrets
    rev: v1.4.0
    hooks:
      - id: detect-secrets
        args: ['--baseline', '.secrets.baseline']
```

### AWS Macie — detectar secretos en S3

```bash
# Habilitar Macie para escanear buckets S3 en busca de datos sensibles
aws macie2 enable-macie

# Crear job de análisis
aws macie2 create-classification-job \
  --job-type ONE_TIME \
  --name "scan-for-secrets-$(date +%Y%m%d)" \
  --s3-job-definition '{
    "bucketDefinitions": [
      {
        "accountId": "123456789",
        "buckets": ["gtm-suite-uploads", "gtm-suite-backups"]
      }
    ]
  }'
```

### GitHub Secret Scanning

GitHub escanea automáticamente los repositorios en busca de patrones de secretos conocidos (AWS keys, GitHub tokens, Stripe keys, etc.). Habilitar en: Settings → Security → Secret scanning.

```bash
# Si GitHub detecta un secreto expuesto:
# 1. Rotar INMEDIATAMENTE — asumir que ya fue comprometido
aws iam create-access-key --user-name github-actions-user   # nueva key
aws iam delete-access-key --access-key-id AKIA...           # eliminar la expuesta

# 2. Verificar si fue usado
aws cloudtrail lookup-events \
  --lookup-attributes AttributeKey=AccessKeyId,AttributeValue=AKIA...
```


---

## Glosario

| Término | Definición |
|---------|-----------|
| Secreto | Valor sensible (contraseña, API key, connection string, JWT secret) que no debe exponerse en código ni logs |
| AWS Secrets Manager | Servicio gestionado de AWS para almacenar, rotar y auditar secretos con integración nativa en ECS |
| Parameter Store | Servicio de AWS Systems Manager para almacenar configuración y secretos cifrados con KMS; más económico que Secrets Manager |
| Azure Key Vault | Servicio de Azure para almacenar secretos, claves criptográficas y certificados con acceso vía Managed Identity |
| KMS | AWS Key Management Service; servicio para crear y gestionar las claves criptográficas que cifran los secretos |
| Managed Identity | Identidad asignada por Azure a un servicio que permite acceder a Key Vault sin credenciales en el código |
| DefaultAzureCredential | Clase del SDK de Azure que resuelve automáticamente la credencial correcta según el entorno de ejecución |
| user-secrets | Herramienta de .NET para almacenar secretos de desarrollo local fuera del repositorio |
| detect-secrets | Herramienta de Yelp que escanea repositorios en busca de patrones de secretos y genera un baseline |
| Secret scanning | Función de GitHub que detecta automáticamente secretos conocidos (AWS keys, tokens) en el código enviado |
| Rotación automática | Proceso programado que reemplaza un secreto por uno nuevo sin intervención manual, reduciendo la ventana de exposición |
| IAM Role | Rol de AWS que define permisos; se asigna a una tarea ECS para que la app acceda a Secrets Manager sin claves propias |

---

*Rogelio Arriaga Gonzalez*
