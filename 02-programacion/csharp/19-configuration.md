# 19 — Configuración: IConfiguration e IOptions\<T\>

ASP.NET Core tiene un sistema de configuración unificado que lee valores de múltiples fuentes (archivos JSON, variables de entorno, secrets) y los expone a través de `IConfiguration` e `IOptions<T>`.

> Fuente: *Apps and Services with .NET 8* (Mark J. Price) — Ch.2 Managing Configuration and Secrets

---

## El sistema de configuración por capas

Las fuentes se cargan en orden y las posteriores sobreescriben a las anteriores:

```
1. appsettings.json                    ← base, siempre presente
2. appsettings.{Environment}.json      ← sobreescribe según ASPNETCORE_ENVIRONMENT
3. appsettings.Local.json              ← solo existe en desarrollo local (en .gitignore)
4. Variables de entorno                ← sobreescriben TODO lo anterior
5. Argumentos de línea de comandos     ← la mayor prioridad
```

```json
// appsettings.json — valores base para todos los entornos
{
  "ConnectionStrings": {
    "MainDbConnection": "Host=localhost;Port=5432;Database=back_template_dev;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "",
    "Issuer": "back-template",
    "Audience": "back-template-clients",
    "ExpiresInMinutes": 60
  },
  "CustomLogging": {
    "SeqUri": "http://localhost:5341",
    "IncludeSqlText": false
  }
}
```

```json
// appsettings.Local.json — solo para desarrollo local
{
  "CustomLogging": {
    "IncludeSqlText": true    // sobreescribe el valor de appsettings.json
  }
}
```

```
// Variable de entorno — sobreescribe appsettings.*
// __ (doble guión bajo) es el separador de sección
Jwt__Key=mi-clave-super-secreta-que-tiene-mas-de-32-caracteres
ConnectionStrings__MainDbConnection=Host=prod-server;...
```

---

## IConfiguration — acceso directo

Inyecta `IConfiguration` en constructores para leer valores directamente:

```csharp
public class JwtTokenService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiresInMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        // ?? throw = fail fast: si la config falta, la app no arranca
        _key      = configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("Jwt:Key no configurado.");

        _issuer   = configuration["Jwt:Issuer"]
                    ?? throw new InvalidOperationException("Jwt:Issuer no configurado.");

        _audience = configuration["Jwt:Audience"]
                    ?? throw new InvalidOperationException("Jwt:Audience no configurado.");

        // GetValue con tipo y default:
        _expiresInMinutes = configuration.GetValue<int>("Jwt:ExpiresInMinutes", defaultValue: 60);
    }
}
```

### Métodos de IConfiguration

```csharp
// Leer un valor simple — retorna string? (null si no existe)
string? value = configuration["Jwt:Key"];

// Leer con tipo — retorna el valor convertido o el default
int timeout  = configuration.GetValue<int>("Database:TimeoutSeconds", 30);
bool enabled = configuration.GetValue<bool>("Feature:NewDashboard", false);

// Leer una sección completa
IConfigurationSection jwtSection = configuration.GetSection("Jwt");
string? key = jwtSection["Key"];    // equivalente a configuration["Jwt:Key"]

// Obtener un string de conexión (atajo)
string? connStr = configuration.GetConnectionString("MainDbConnection");
// equivalente a configuration["ConnectionStrings:MainDbConnection"]

// Leer y mapear a un tipo directamente
var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>();
```

---

## IOptions\<T\> — el patrón recomendado

En lugar de leer con strings y perder el tipado, define una clase de settings y usa `IOptions<T>`:

### 1. Define la clase de configuración

```csharp
// Clase que mapea a la sección en appsettings.json
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";  // nombre de la sección en appsettings

    public string Key              { get; init; } = string.Empty;
    public string Issuer           { get; init; } = string.Empty;
    public string Audience         { get; init; } = string.Empty;
    public int    ExpiresInMinutes { get; init; } = 60;
}
```

### 2. Registra en DI

```csharp
// En Program.cs o en Extensions/JwtAuthExtensions.cs:
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));
```

### 3. Inyecta y usa

```csharp
public class JwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public string GenerateToken(Guid userId, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:   _settings.Issuer,
            audience: _settings.Audience,
            expires:  DateTime.UtcNow.AddMinutes(_settings.ExpiresInMinutes),
            claims:   new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) },
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

## IOptions vs IOptionsSnapshot vs IOptionsMonitor

| Interfaz | Cuándo se re-lee | Lifetime recomendado |
|---------|-----------------|---------------------|
| `IOptions<T>` | Una vez al inicio de la app | Singleton, Scoped, Transient |
| `IOptionsSnapshot<T>` | Una vez por request | Solo Scoped |
| `IOptionsMonitor<T>` | Cuando el archivo cambia (hot-reload) | Singleton |

```csharp
// IOptionsSnapshot — re-evalúa por cada request
// Útil para feature flags que cambian sin reiniciar la app
public class FeatureFlagService
{
    private readonly FeatureSettings _features;

    public FeatureFlagService(IOptionsSnapshot<FeatureSettings> options)
    {
        _features = options.Value;  // se re-lee en cada request
    }
}

// IOptionsMonitor — hot-reload en Singleton
public class CacheService  // registrado como Singleton
{
    private CacheSettings _settings;

    public CacheService(IOptionsMonitor<CacheSettings> monitor)
    {
        _settings = monitor.CurrentValue;

        // Suscribirse a cambios:
        monitor.OnChange(newSettings =>
        {
            _settings = newSettings;
            Console.WriteLine("Cache settings updated.");
        });
    }
}
```

---

## Validación de configuración al iniciar

```csharp
// Validar que la configuración es correcta ANTES de arrancar la app:
builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateDataAnnotations()   // valida atributos [Required], [MinLength], etc.
    .ValidateOnStart();          // falla al inicio si la config es inválida

// La clase de settings con validaciones:
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "Jwt:Key es requerido.")]
    [MinLength(32, ErrorMessage = "Jwt:Key debe tener al menos 32 caracteres.")]
    public string Key { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Range(1, 10080)]  // 1 minuto a 7 días
    public int ExpiresInMinutes { get; init; } = 60;
}
```

---

## Variables de entorno — formato ASP.NET Core

ASP.NET Core convierte `__` (doble guión bajo) en `:` para mapear variables de entorno a secciones:

```bash
# Variable de entorno → clave de configuración
Jwt__Key=clave-secreta         → Jwt:Key
Jwt__Issuer=mi-app             → Jwt:Issuer
ConnectionStrings__MainDbConnection=Host=... → ConnectionStrings:MainDbConnection
CustomLogging__SeqUri=http://seq:5341 → CustomLogging:SeqUri

# En Docker Compose:
environment:
  - Jwt__Key=clave-secreta-muy-larga-de-al-menos-32-caracteres
  - Jwt__Issuer=back-template
  - ConnectionStrings__MainDbConnection=Host=postgres;Port=5432;...
  - ASPNETCORE_ENVIRONMENT=Production

# Con archivo .env:
env_file:
  - .env
```

---

## User Secrets — desarrollo local sin exponer secretos

`dotnet user-secrets` guarda secretos en la máquina local fuera del repositorio:

```bash
# Inicializar User Secrets en el proyecto Host
dotnet user-secrets init --project back-template/Host

# Agregar un secreto
dotnet user-secrets set "Jwt:Key" "mi-clave-dev-super-secreta-de-32-chars!!" \
  --project back-template/Host

# Listar todos los secretos
dotnet user-secrets list --project back-template/Host

# Los secretos se guardan en:
# Windows: %APPDATA%\Microsoft\UserSecrets\{user-secrets-id}\secrets.json
# Linux/Mac: ~/.microsoft/usersecrets/{user-secrets-id}/secrets.json
# NUNCA en el repositorio
```

Los User Secrets se cargan automáticamente en entorno Development y tienen mayor prioridad que `appsettings.json`.

---

## Prioridades de configuración (de menor a mayor)

```
appsettings.json
    ↑ sobreescrito por
appsettings.{Environment}.json
    ↑ sobreescrito por
appsettings.Local.json
    ↑ sobreescrito por
User Secrets (solo Development)
    ↑ sobreescrito por
Variables de entorno
    ↑ sobreescrito por
Argumentos de línea de comandos
```

---

## En este proyecto

```csharp
// Host/Extensions/JwtAuthExtensions.cs
public static IServiceCollection AddJwtAuthentication(
    this IServiceCollection services, IConfiguration configuration)
{
    var key    = configuration["Jwt:Key"]
                 ?? throw new InvalidOperationException("Jwt:Key no configurado.");
    var issuer = configuration["Jwt:Issuer"]
                 ?? throw new InvalidOperationException("Jwt:Issuer no configurado.");

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = issuer,
                ValidAudience            = configuration["Jwt:Audience"],
                IssuerSigningKey         = new SymmetricSecurityKey(
                                               Encoding.UTF8.GetBytes(key))
            };
        });

    return services;
}

// Infrastructure/ServiceCollectionEx.cs
public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<MainDbConnectionFactory>();
    // MainDbConnectionFactory recibe IConfiguration en su constructor
    // y lee ConnectionStrings:MainDbConnection ahí
    // ...
    return services;
}
```

---

## Errores comunes

### Error 1 — Guardar secretos en appsettings.json

```json
// ❌ NUNCA en appsettings.json — se sube al repositorio
{
  "Jwt": {
    "Key": "mi-clave-secreta-real"   // ← exposición de credenciales
  }
}

// ✓ Usar variables de entorno o User Secrets
// En appsettings.json dejar vacío o comentario:
{
  "Jwt": {
    "Key": ""    // ← configurar via env var o user secrets
  }
}
```

### Error 2 — No validar configuración al inicio

```csharp
// ❌ La app arranca aunque Jwt:Key esté vacío — falla en la primera request
public JwtTokenService(IConfiguration config)
{
    _key = config["Jwt:Key"] ?? "";  // string vacío silenciado
}

// ✓ Fail fast al inicio — la app no arranca si falta configuración crítica
public JwtTokenService(IConfiguration config)
{
    _key = config["Jwt:Key"]
           ?? throw new InvalidOperationException("Jwt:Key no configurado.");
}
// O usar ValidateOnStart() con IOptions<T>
```

### Error 3 — Inyectar IConfiguration en capas internas

```csharp
// ❌ Application o Domain no deberían conocer IConfiguration
// Viola la dirección de dependencias (Application no depende de Infrastructure)
public class GetUserHandler : IRequestHandler<...>
{
    public GetUserHandler(IConfiguration config) // ← no correcto en Application
    {
        _timeout = config.GetValue<int>("Query:Timeout");
    }
}

// ✓ La configuración se inyecta en la capa de composición (Host/Extensions)
// o en Infrastructure donde tiene sentido
// Application solo recibe lo que necesita — no IConfiguration
```


---

## Glosario

| Término | Definición |
|---------|-----------|
| `IConfiguration` | Interfaz central del sistema de configuración de .NET que provee acceso a todos los valores de configuración |
| `IOptions<T>` | Interfaz que expone un objeto de configuración fuertemente tipado en lugar de strings planos |
| `IOptionsSnapshot<T>` | Variante de `IOptions<T>` que recarga la configuración en cada petición HTTP cuando el archivo cambia |
| `IOptionsMonitor<T>` | Variante de `IOptions<T>` que notifica cambios en tiempo real sin reiniciar la aplicación |
| Configuración por capas | Sistema donde múltiples fuentes (JSON, env vars, secrets) se cargan en orden y las posteriores sobreescriben a las anteriores |
| `appsettings.json` | Archivo de configuración base que se incluye en todos los entornos |
| `appsettings.Local.json` | Archivo de configuración solo para desarrollo local; se agrega al `.gitignore` para no versionarlo |
| Variables de entorno | Pares clave-valor del sistema operativo que sobreescriben la configuración JSON; convención con `__` para jerarquía |
| User Secrets | Mecanismo de desarrollo de .NET para almacenar secretos fuera del repositorio: `dotnet user-secrets set "Clave" "Valor"` |
| Strongly-typed config | Clase POCO que mapea una sección de configuración; se registra con `services.Configure<T>(section)` |
| `GetConnectionString()` | Método de `IConfiguration` que accede directamente a la sección `ConnectionStrings:NombreConexion` |

---

*Rogelio Arriaga Gonzalez*
