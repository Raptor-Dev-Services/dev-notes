# 10 — Gestión de Secretos en Producción

En desarrollo local, `dotnet user-secrets` resuelve. En producción, los secretos viven en gestores especializados. Nunca en archivos de configuración, repositorios, ni variables de entorno expuestas en logs.

> Fuente: *Building Secure and Reliable Systems* (Heather Adkins et al.) — Ch.8 Design for Security  
> Fuente: *ASP.NET Core 9 Essentials* (Packt) — Ch.6 Enhancing Security and Quality

---

## Reglas absolutas

```
✗ Cero secretos en el repositorio. Nunca, bajo ninguna circunstancia.
✗ Cero secretos en el Dockerfile. Cada capa es inspeccionable con docker history.
✗ Cero secretos en logs. Enmascarar strings sensibles antes de loggear.
✓ Rotación periódica. Cada secreto tiene fecha de caducidad (mínimo cada 90 días).
✓ Acceso mínimo. Cada servicio usa un secreto distinto con permisos mínimos.
✓ Auditoría. Cada acceso a un secreto queda registrado y es auditable.
```

---

## Gestores de secretos por plataforma

| Plataforma | Servicio | Cuándo usarlo |
|------------|----------|--------------|
| **Azure** | Azure Key Vault | Stack Microsoft — integración nativa con .NET (`DefaultAzureCredential`) |
| **AWS** | Secrets Manager | Stack AWS — soporta rotación automática para RDS |
| **AWS** | SSM Parameter Store | Alternativa más económica para configuración menos sensible |
| **GCP** | Secret Manager | Stack Google Cloud |
| **Multi-cloud** | HashiCorp Vault | Independiente del cloud provider |
| **Self-hosted** | Bitwarden Secrets / Infisical | SaaS pequeños o on-premise |

---

## Desarrollo local — User Secrets

```bash
# Inicializar User Secrets en el proyecto
dotnet user-secrets init --project Host/Host.csproj

# Agregar un secreto
dotnet user-secrets set "ConnectionStrings:MainDb" "Host=localhost;..." --project Host/

# Ver todos los secretos
dotnet user-secrets list --project Host/
```

User Secrets se almacena en `%APPDATA%\Microsoft\UserSecrets\{guid}\secrets.json`, fuera del repo.

---

## Producción — Azure Key Vault con Managed Identity

```csharp
// Program.cs — integrar Key Vault como fuente de configuración
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    var keyVaultUri = builder.Configuration["KeyVault:Uri"]
        ?? throw new InvalidOperationException("KeyVault:Uri no configurado");

    // DefaultAzureCredential prueba automáticamente:
    // 1. Variables de entorno AZURE_CLIENT_ID, etc.
    // 2. Managed Identity (en Azure App Service, AKS, etc.)
    // 3. Azure CLI (para dev en máquina del dev)
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}
```

Los secretos de Key Vault se acceden igual que cualquier configuración:
```csharp
var connectionString = builder.Configuration["ConnectionStrings:MainDb"];
```

---

## Secretos en Docker build — BuildKit mount

```dockerfile
# Pasar secreto durante el build SIN dejarlo en ninguna capa de la imagen
# syntax=docker/dockerfile:1

RUN --mount=type=secret,id=nuget_token \
    NUGET_TOKEN=$(cat /run/secrets/nuget_token) \
    dotnet restore --source "https://nuget.pkg.github.com/mi-org/index.json"
```

```bash
# En el comando docker build
docker build \
    --secret id=nuget_token,src=~/.github_token \
    -t mi-api:latest .
```

---

## Detectar filtraciones en logs

Después de cada cambio en logging, buscar en Seq o en stdout:

```
Palabras clave a buscar: password, secret, token, apikey, pk_test, sk_test, eyJ
"eyJ" — inicio típico de un JWT en base64
```

Si aparecen en logs, hay un bug de seguridad que debe corregirse antes de desplegar.

---

## Glosario

| Término | Definición |
|---------|-----------|
| User Secrets | Mecanismo de .NET para almacenar secretos en desarrollo fuera del repositorio, en %APPDATA% |
| Azure Key Vault | Servicio de Azure para almacenar y gestionar secretos, claves y certificados en producción |
| DefaultAzureCredential | Clase del SDK de Azure que detecta automáticamente el mecanismo de autenticación disponible |
| Managed Identity | Identidad administrada de Azure que permite a una VM o App Service autenticarse sin credenciales explícitas |
| BuildKit Mount Secret | Mecanismo de Docker BuildKit para montar secretos durante el build sin que queden en capas de imagen |
| Rotación de secretos | Proceso de reemplazar periódicamente las credenciales para reducir el impacto de una exposición |
| AWS Secrets Manager | Servicio de AWS para gestionar secretos con rotación automática y auditoría |
| Secret Scanning | Proceso de detectar secretos (tokens, contraseñas) accidentalmente incluidos en el código fuente |
| .gitignore | Archivo que lista los patrones de archivos que Git no debe rastrear — esencial para excluir .env |
| Principio de mínimo privilegio | Práctica de otorgar solo los permisos estrictamente necesarios a cada identidad o servicio |

---

*Rogelio Arriaga Gonzalez*
