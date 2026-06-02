# 09 — Configuración en .NET

.NET tiene un sistema de configuración por capas que combina múltiples fuentes. La configuración se lee como un árbol jerárquico — cualquier fuente puede sobreescribir a la anterior.

> Fuente: *ASP.NET Core 9 Essentials* (Packt) — Ch.9 Managing Application Settings  
> Fuente: *Apps and Services with .NET 8* (Mark J. Price) — Ch.2 Managing Configuration and Secrets

---

## Orden de precedencia (de menor a mayor)

```
appsettings.json                   ← base, va al repo, valores neutros
appsettings.{Environment}.json     ← overrides por ambiente
User Secrets (solo Development)    ← secretos locales del dev, NO van al repo
Variables de entorno               ← sobreescriben todo lo anterior
Argumentos de línea de comandos    ← la última palabra
```

---

## Leer configuración — acceso directo vs. Options

### Acceso directo con `IConfiguration` (no recomendado para producción)

```csharp
// Propenso a errores de typo en las keys — solo se detectan en runtime
var connectionString = builder.Configuration.GetConnectionString("Default");
var jwtKey = builder.Configuration["Jwt:SigningKey"];
var timeout = builder.Configuration.GetValue<int>("Jwt:AccessTokenLifetimeMinutes");
```

### Options Pattern — el enfoque correcto

El Options Pattern mapea secciones del JSON a clases POCO fuertemente tipadas. Los errores de key se detectan en compilación y en startup con validación.

```csharp
// 1. Define la clase POCO que mapea la sección
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer   { get; init; } = string.Empty;
    [Required] public string Audience { get; init; } = string.Empty;
    [Range(1, 60)]
    public int AccessTokenLifetimeMinutes  { get; init; } = 15;
    [Range(1, 365)]
    public int RefreshTokenLifetimeDays    { get; init; } = 30;
    [MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;
}

// 2. Registra en Program.cs con validación
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()   // valida [Required], [Range], [MinLength], etc.
    .ValidateOnStart();          // falla en startup si la config está incompleta

// 3. Inyecta donde se necesite
public class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _jwt = options.Value;

    public string CreateToken(Guid userId)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenLifetimeMinutes);
        // ...
    }
}
```

---

## IOptions vs. IOptionsSnapshot vs. IOptionsMonitor

| Interfaz | Lifetime | Se recarga | Cuándo usar |
|----------|----------|-----------|------------|
| `IOptions<T>` | Singleton | No — solo al arrancar | Config fija (JWT, Stripe keys) |
| `IOptionsSnapshot<T>` | Scoped (por request) | Sí — en cada request | Config que puede cambiar sin reiniciar |
| `IOptionsMonitor<T>` | Singleton | Sí — reacciona a cambios en tiempo real | Jobs en background, servicios long-running |

```csharp
// IOptionsMonitor — para servicios singleton que necesitan leer config actualizada
public class FeatureFlagService(IOptionsMonitor<FeatureFlags> monitor)
{
    public bool IsEnabled(string flag)
    {
        // .CurrentValue siempre retorna la versión más reciente del archivo de config
        return monitor.CurrentValue.EnabledFlags.Contains(flag);
    }
}
```

---

## Variables de entorno — sobreescribir configuración en Docker

En .NET, una variable de entorno con doble underscore (`__`) navega secciones del JSON. Es la forma estándar en Linux/Docker (`:` no es válido en nombres de variables en Linux).

```env
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default="Host=db.prod;Port=5432;Database=mydb;..."
Jwt__SigningKey="super-secret-key-min-32-chars"
Stripe__SecretKey="sk_live_..."
Cors__AllowedOrigins__0="https://app.midominio.com"
Logging__LogLevel__Default="Warning"
```

```yaml
# docker-compose.yml
services:
  api:
    image: registry/mi-api:${APP_VERSION:-latest}
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Production}
      ConnectionStrings__Default: ${DB_CONNECTION}
      Jwt__SigningKey: ${JWT_SIGNING_KEY}
      Stripe__SecretKey: ${STRIPE_SECRET_KEY}
    env_file:
      - .env.docker       # archivo local, en .gitignore
    ports:
      - "${API_PORT:-8080}:8080"
    depends_on:
      - postgres

  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: ${DB_NAME}
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

---

## User Secrets en desarrollo local

```bash
dotnet user-secrets init --project Host/Host.csproj

dotnet user-secrets set "Jwt:SigningKey" "dev-only-key-min-32-characters-long"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;..."

dotnet user-secrets list --project Host/
dotnet user-secrets remove "Stripe:SecretKey" --project Host/
```

Los secretos se almacenan en `%APPDATA%\Microsoft\UserSecrets\{guid}\secrets.json` — fuera del repositorio.

---

## Convención de secciones en appsettings.json

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=mydb;Username=app_user;Password=dev_pass"
  },
  "Jwt": {
    "Issuer": "https://api.midominio.com",
    "Audience": "https://app.midominio.com",
    "AccessTokenLifetimeMinutes": 15,
    "RefreshTokenLifetimeDays": 30,
    "SigningKey": ""
  },
  "Cors": {
    "AllowedOrigins": [
      "https://app.midominio.com",
      "https://admin.midominio.com"
    ]
  },
  "Stripe": {
    "PublishableKey": "pk_test_...",
    "SecretKey": "",
    "WebhookSecret": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

---

## Múltiples opciones con nombre (`AddOptions` named)

Útil cuando hay varias instancias de la misma config (múltiples proveedores de email, múltiples bases de datos):

```csharp
// Registro con nombre
builder.Services
    .AddOptions<SmtpOptions>("SendGrid")
    .Bind(builder.Configuration.GetSection("Smtp:SendGrid"));

builder.Services
    .AddOptions<SmtpOptions>("Mailgun")
    .Bind(builder.Configuration.GetSection("Smtp:Mailgun"));

// Uso con IOptionsMonitor — accede por nombre
public class EmailRouter(IOptionsMonitor<SmtpOptions> monitor)
{
    public SmtpOptions GetProvider(string name) => monitor.Get(name);
}
```

---

## Validación personalizada con `IValidateOptions`

Para validaciones más complejas que Data Annotations no cubre:

```csharp
public class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        if (options.AccessTokenLifetimeMinutes >= options.RefreshTokenLifetimeDays * 24 * 60)
            return ValidateOptionsResult.Fail(
                "AccessToken debe expirar antes que RefreshToken");

        return ValidateOptionsResult.Success;
    }
}

// Registro
builder.Services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
```

---

## Recargar configuración en caliente

`appsettings.json` se monitorea automáticamente por .NET. Para forzar recarga desde código:

```csharp
// El IConfiguration nativo recarga el JSON automáticamente cuando cambia el archivo.
// Solo IOptions<T> NO se recarga — usa IOptionsSnapshot o IOptionsMonitor.
```

Ver `04-backend/10-secretos.md` para gestión de secretos en producción (Azure Key Vault, Docker BuildKit).

---

## Glosario

| Término | Definición |
|---------|-----------|
| Options Pattern | Patrón de .NET que mapea secciones de configuración a clases POCO fuertemente tipadas con validación en startup |
| IOptions\<T\> | Singleton de configuración que se lee una sola vez al iniciar la aplicación — no se recarga |
| IOptionsSnapshot\<T\> | Configuración Scoped que se recarga en cada request HTTP cuando el archivo de config cambia |
| IOptionsMonitor\<T\> | Singleton de configuración que notifica cambios en tiempo real — para servicios long-running |
| ValidateDataAnnotations | Método que activa la validación de atributos ([Required], [Range]) en las clases Options al iniciar |
| ValidateOnStart | Método que fuerza la validación de configuración en startup en lugar de esperar al primer uso |
| IValidateOptions\<T\> | Interfaz para validaciones personalizadas de configuración más complejas que Data Annotations |
| appsettings.json | Archivo base de configuración de .NET — va al repositorio con valores neutros o de ejemplo |
| User Secrets | Mecanismo de desarrollo local para almacenar secretos fuera del repositorio en %APPDATA% |
| IConfiguration | Interfaz de bajo nivel para acceder a valores de configuración por clave string — propenso a typos |
| Double Underscore | Separador de secciones en variables de entorno Linux/Docker (`Jwt__SigningKey`) equivalente a `:` |
| Named Options | Instancias múltiples de la misma clase Options identificadas por nombre — para múltiples proveedores |

---

*Rogelio Arriaga Gonzalez*
