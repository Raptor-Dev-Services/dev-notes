# 06 — Connection Strings y Manejo por Ambiente

La cadena de conexión es uno de los secretos más sensibles del sistema. Un leak permite acceso completo a la base de datos — merece la misma disciplina que cualquier secreto.

> Fuente: *Apps and Services with .NET 8* (Mark J. Price) — Ch.3 Building Data Access Layers

---

## Componentes de una connection string

| Componente | Descripción |
|------------|-------------|
| `Host` / `Server` | Hostname o IP del servidor de BD |
| `Port` | 5432 PostgreSQL · 1433 SQL Server · 3306 MySQL |
| `Database` | Nombre de la base de datos |
| `Username` / `User Id` | Usuario de la conexión |
| `Password` | Contraseña del usuario |
| `Maximum Pool Size` | Conexiones simultáneas máximas (default 100) |
| `Connection Idle Lifetime` | Segundos antes de cerrar conexiones inactivas |
| `Connect Timeout` | Segundos antes de fallar al conectar (default 15) |
| `Ssl Mode` / `Encrypt` | Cifrado TLS en tránsito |
| `Application Name` | Identifica la app en logs del servidor de BD — ponlo siempre |

---

## PostgreSQL (Npgsql)

```
Host=db.miapp.internal;
Port=5432;
Database=miapp_prod;
Username=app_user;
Password=***;
Maximum Pool Size=100;
Connection Idle Lifetime=300;
Application Name=MiApp.Api;
Ssl Mode=Require;
Trust Server Certificate=false
```

---

## SQL Server

```
Server=db.miapp.internal,1433;
Database=miapp_prod;
User Id=app_user;
Password=***;
Max Pool Size=100;
Connection Timeout=15;
Application Name=MiApp.Api;
TrustServerCertificate=False;
Encrypt=True
```

---

## Connection pooling

EF Core y Npgsql/SqlClient mantienen un pool de conexiones TCP reutilizables. El pool evita el overhead de abrir y cerrar conexiones en cada request.

| Variable | Recomendación |
|----------|---------------|
| `Maximum Pool Size` | 100 por instancia de API. Con 4 instancias = 400 conexiones a la BD |
| Límite del servidor | PostgreSQL default acepta 100 conexiones totales — ajustar `max_connections` si se escala |
| Síntoma de pool agotado | Errores `timeout obtaining connection` — subir pool size o investigar conexiones no liberadas |
| Conexiones zombi | Si no se usa `using`/`await using` al abrir `NpgsqlConnection`, las conexiones quedan tomadas — bug clásico con Dapper manual |

---

## Manejo por ambiente

### Desarrollo local — User Secrets

```bash
dotnet user-secrets set "ConnectionStrings:Default" \
  "Host=localhost;Port=5432;Database=miapp_dev;Username=dev_user;Password=dev_pass;Application Name=MiApp.Api.Dev"
```

### Docker Compose — variable de entorno

```yaml
services:
  api:
    environment:
      ConnectionStrings__Default: "${DB_CONNECTION}"
```

```env
# .env.docker (en .gitignore — nunca al repo)
DB_CONNECTION=Host=postgres;Port=5432;Database=miapp;Username=app;Password=secret;Application Name=MiApp.Api
```

### Producción — Azure Key Vault o Secrets Manager

La connection string vive en el gestor de secretos. La app la lee a través de `IConfiguration` con el Key Vault como provider:

```csharp
// appsettings.json — solo el nombre del secreto, no el valor
// "ConnectionStrings:Default" → el valor real viene de Key Vault en producción
```

Ver `04-backend/10-secretos.md` para la integración completa con Azure Key Vault.

---

## Registrar en EF Core

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        npgsql => npgsql.CommandTimeout(30)));
```

---

## Checklist de seguridad

```
✗ Nunca incluir la connection string real en appsettings.json del repo
✗ Nunca loggear la connection string — enmascarar en Serilog con destructuring
✓ Usar Application Name para identificar la app en slow query logs
✓ SSL/TLS habilitado en producción (Ssl Mode=Require / Encrypt=True)
✓ Usuario de BD con permisos mínimos — solo SELECT/INSERT/UPDATE/DELETE en su schema
✓ Rotación de contraseña de BD con AWS Secrets Manager RDS rotation o Azure Managed Identity
```

---

## Glosario

| Término | Definición |
|---------|-----------|
| Connection string | cadena de texto que contiene todos los parámetros necesarios para que una aplicación se conecte a una base de datos |
| Connection pooling | técnica que reutiliza conexiones TCP existentes en lugar de abrir y cerrar una nueva en cada request |
| Pool size | número máximo de conexiones simultáneas mantenidas en el pool por instancia de la aplicación |
| Npgsql | driver oficial de .NET para conectarse a PostgreSQL, compatible con EF Core y Dapper |
| Application Name | parámetro de la connection string que identifica la aplicación en los logs del servidor de base de datos |
| SSL Mode | parámetro que controla el cifrado TLS en la conexión entre la aplicación y el servidor de base de datos |
| User Secrets | mecanismo de .NET para almacenar secretos de desarrollo en el directorio del usuario, fuera del repositorio |
| Managed Identity | identidad de Azure AD asignada a un recurso de nube (App Service, AKS) que permite conectarse sin contraseña |
| Conexión zombi | conexión de base de datos que no se liberó al pool porque no se usó `using` o `await using` correctamente |
| Principio de mínimo privilegio | práctica de asignar al usuario de BD solo los permisos estrictamente necesarios: SELECT, INSERT, UPDATE, DELETE en su schema |

---

*Rogelio Arriaga Gonzalez*
