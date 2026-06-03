# 09 — Configuración en .NET

.NET tiene un sistema de configuración por capas que combina múltiples fuentes. La configuración se lee como un árbol jerárquico. Cualquier fuente puede sobreescribir a la anterior.

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

Los secretos se almacenan en `%APPDATA%\Microsoft\UserSecrets\{guid}\secrets.json`, fuera del repositorio.

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

## Inyectar la clase Options directamente (sin IOptions)
> Fuente: *Architecting ASP.NET Core Applications* (Ferreira) — Ch.9 Injecting Options Objects Directly

El problema con `IOptions<T>`, `IOptionsSnapshot<T>`, etc. es que el consumidor controla el lifetime. Eso rompe Inversion of Control. La solución es inyectar la clase POCO directamente desde el composition root:

```csharp
// ❌ El consumidor controla el lifetime al elegir IOptionsSnapshot vs IOptions vs IOptionsMonitor
public class ExampleUserService(IOptionsSnapshot<JwtOptions> options)   // ← acoplamiento a IOptionsSnapshot
{
    private readonly JwtOptions _jwt = options.Value;
}

// ✓ El consumer no sabe nada de lifetimes — solo recibe JwtOptions
public class ExampleUserService(JwtOptions jwt)   // ← solo depende del POCO
{
    private readonly JwtOptions _jwt = jwt;
}

// Composition root — controla el lifetime aquí, no en el consumidor
builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Factory que crea JwtOptions con Scoped lifetime (se recarga en cada request)
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IOptionsSnapshot<JwtOptions>>().Value);
```

Con este patrón:
- Los tests inyectan `new JwtOptions { ... }` directamente sin mockear `IOptions<T>`
- El composition root decide el lifetime (Scoped, Singleton, etc.)
- El código queda desacoplado de `Microsoft.Extensions.Options`

---

## FluentValidation para validar opciones
> Fuente: *Architecting ASP.NET Core Applications* (Ferreira) — Ch.9 Validating Options Using FluentValidation

Puente entre FluentValidation y el sistema de validación de Options de .NET:

```csharp
// 1. Validador FluentValidation — reglas declarativas
public class JwtOptionsValidator : AbstractValidator<JwtOptions>
{
    public JwtOptionsValidator()
    {
        RuleFor(x => x.SigningKey).NotEmpty().MinimumLength(32);
        RuleFor(x => x.Issuer).NotEmpty();
        RuleFor(x => x.AccessTokenLifetimeMinutes).InclusiveBetween(1, 60);
        RuleFor(x => x.AccessTokenLifetimeMinutes)
            .LessThan(x => x.RefreshTokenLifetimeDays * 24 * 60)
            .WithMessage("AccessToken debe expirar antes que RefreshToken.");
    }
}

// 2. Adaptador genérico — reutilizable para cualquier tipo de Options
public sealed class FluentValidateOptions<TOptions> : IValidateOptions<TOptions>
    where TOptions : class
{
    private readonly IValidator<TOptions> _validator;

    public FluentValidateOptions(IValidator<TOptions> validator)
        => _validator = validator;

    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        var result = _validator.Validate(options);
        if (result.IsValid) return ValidateOptionsResult.Success;

        var errors = result.Errors.Select(e => e.ErrorMessage);
        return ValidateOptionsResult.Fail(errors);
    }
}

// 3. Registro en Program.cs
builder.Services
    .AddSingleton<IValidator<JwtOptions>, JwtOptionsValidator>()
    .AddSingleton<IValidateOptions<JwtOptions>, FluentValidateOptions<JwtOptions>>();

builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateOnStart();
```

La ventaja sobre `ValidateDataAnnotations()`: las reglas de validación viven en una clase separada, son más expresivas y más fáciles de testear.

---

## `[OptionsValidator]` — source generator (.NET 8)
> Fuente: *Architecting ASP.NET Core Applications* (Ferreira) — Ch.9 Using the Options Validation Source Generator

Para proyectos con AOT (Ahead-of-Time compilation) o trimming, el generador de código crea el validador en tiempo de compilación, sin reflection:

```csharp
// Opciones con Data Annotations
public class JwtOptions
{
    [Required] public string Issuer    { get; init; } = string.Empty;
    [Required, MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;
    [Range(1, 60)]
    public int AccessTokenLifetimeMinutes { get; init; } = 15;
}

// Validador generado automáticamente — solo la declaración, el código lo genera el compilador
[OptionsValidator]
public partial class JwtOptionsValidator : IValidateOptions<JwtOptions> { }

// Registro
builder.Services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateOnStart();
```

```xml
<!-- .csproj — activar el source generator (activo por defecto en AOT/trimming) -->
<PropertyGroup>
    <EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>
</PropertyGroup>
```

Diferencia clave vs `ValidateDataAnnotations()`: el código de validación se genera en tiempo de compilación, no usa reflection en runtime. Compatible con publicación AOT.

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
| IConfigureOptions\<T\> | Interfaz para encapsular lógica de configuración en una clase separada — se ejecuta en la fase de configuración |
| IPostConfigureOptions\<T\> | Como IConfigureOptions pero se ejecuta después — útil para sobreescribir valores en tests de integración |
| FluentValidateOptions\<T\> | Adaptador genérico que conecta FluentValidation con IValidateOptions — bridge entre los dos sistemas |
| [OptionsValidator] | Atributo de .NET 8 para generar código de validación en tiempo de compilación — compatible con AOT |
| AOT | Ahead-of-Time compilation — compila .NET a código nativo antes de ejecutar; requiere evitar reflection |

---

*Rogelio Arriaga Gonzalez*
