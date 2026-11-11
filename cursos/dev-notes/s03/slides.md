<!-- _class: lead -->

# S03 - C# Avanzado + Tour del Back-Template

### Dev Notes

**Rogelio Arriaga - Raptor Dev Services**

---

## Agenda del día

- **Teoría:** C# avanzado - Classes - Records - Generics - Pattern Matching - Async - Configuration
- **Teoría:** Tour del back-template - estructura Modular Monolith - flujo Request->Handler->Response->Presenter
- **Teoría:** Controller real - Contracts layer - multi-tenancy - Program.cs - SDLC - SemVer - Story Points
- Quiz - homework: hacer quiz del módulo extra asignado

---

## El mapa del back-template - Monolito Modular

```
back-template/
├── Modules/                          ← un proyecto por módulo de negocio
│   ├── Users/
│   │   ├── Users.Domain/             ← entidades + interfaces de repositorio
│   │   │   ├── Entities/UserProfile.cs
│   │   │   └── Repositories/IUserProfileRepository.cs
│   │   ├── Users.Application/        ← casos de uso (Handlers)
│   │   │   └── UseCases/GetUserProfile/
│   │   │       ├── GetUserProfileRequest.cs
│   │   │       ├── GetUserProfileHandler.cs
│   │   │       └── Responses/  (Response · Success · NotFoundFailure)
│   │   ├── Users.Contracts/          ← DTOs y eventos de integración (API pública del módulo)
│   │   │   ├── Dtos/UserProfileDto.cs
│   │   │   └── Events/UserRegisteredIntegrationEvent.cs
│   │   ├── Users.Infrastructure/     ← implementaciones (EF Core / Dapper)
│   │   ├── Users.Presentation/       ← Controller + Presenters + RequestBodies
│   │   └── Users.Tests/
│   └── Tenancy/  (mismo patrón — Domain · Application · Presentation)
│
├── Shared/Authentication/            ← funcionalidad transversal (no es un módulo de negocio)
│   ├── Authentication.Domain/        ← RefreshToken, IRefreshTokenRepository
│   ├── Authentication.Application/   ← Login · Register · RefreshToken
│   ├── Authentication.Infrastructure/← PasswordHasher
│   └── Authentication.Presentation/  ← LoginBody, RefreshTokenBody
│
├── Common/Common/                    ← librería compartida (NuGet interno eventual)
│   ├── Messaging/    (IMediator, IRequestHandler, INotificationHandler)
│   ├── Results/      (ISuccess<T>, IFailure, Result, Result<T>)
│   ├── ViewModels/   (ResultViewModel<T>)
│   ├── MultiTenancy/ (ITenantContextAccessor, TenantContext)
│   └── Abstractions/ (IInteractor, IPresenter, IResultInteractor)
│
├── Tests/                            ← pruebas de arquitectura (NetArchTest)
└── Host.Api/                         ← punto de entrada; DI root; middleware pipeline
    ├── Program.cs
    ├── Extensions/  (JwtAuthExtensions · CorsExtensions · SwaggerExtensions)
    └── Middleware/TenantClaimsMiddleware.cs
```

---

<!-- _class: lead -->

# Parte 1
## Clases

---

## Tipos de clases - cuál usar en cada capa

| Modificador | Qué significa | Dónde se usa |
|-------------|--------------|--------------|
| `sealed class` | No se puede heredar - implementación final | Handlers, Repos, Sql, Controllers, Presenters |
| `abstract class` | No se puede instanciar - define estructura base | `BaseApiController` |
| `static class` | Sin instancia - solo miembros estáticos | Extension methods (`ServiceCollectionEx`) |
| `class` (normal) | Instanciable, heredable | Entidades de dominio (mapeadas por Dapper) |

---

## Por qué `sealed` en Handlers y Repositorios

```csharp
// Bien: sealed — comunicación clara + optimización del compilador
public sealed class GetUserHandler
    : IRequestHandler<GetUserRequest, GetUserResponse>
{
    // Sealed dice: "esta clase NO está diseñada para ser base de otras"
    // El compilador puede hacer devirtualización (más rápido)
}

// Mal: Sin sealed — deja la puerta abierta a herencia no deseada
public class GetUserHandler { }
// Alguien podría heredar: public class BuggyHandler : GetUserHandler { }
```

**Regla del proyecto:** Handlers - Repositorios - Presenters - Controllers -> siempre `sealed`.

---

## `static class` - extensiones por módulo

```csharp
// Bien: Cada módulo expone sus propias extensiones de DI
// Users.Application/ServiceCollectionEx.cs
public static class ServiceCollectionEx
{
    public static IServiceCollection AddUsersApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped(typeof(ResultViewModel<>));
        // handlers, validators, presenters del módulo Users
        return services;
    }
}
```

```csharp
// Host.Api/Program.cs — cada módulo se registra de forma independiente
builder.Services.AddMediator(
    typeof(Users.Application.ServiceCollectionEx).Assembly,
    typeof(Authentication.Application.ServiceCollectionEx).Assembly
);

builder.Services.AddUsersApplicationServices();
builder.Services.AddUsersInfrastructureServices();
builder.Services.AddUsersWebApiServices();
// Ventaja: agregar un módulo nuevo = agregar 3 líneas aquí
```

---

## Modificadores de acceso - regla práctica

```
private          → todo lo que es detalle de implementación (_repo, _sql, métodos auxiliares)
protected        → lo que los controllers hijos necesitan (Mediator en BaseApiController)
internal         → detalles de Infrastructure que no son API pública del proyecto
public           → el contrato: interfaces, métodos del Handler, endpoints del Controller
```

```csharp
public sealed class GetUserHandler
{
    private readonly IUserRepository _repo;    // ← private: nadie fuera necesita esto
    private readonly ILogger _logger;          // ← private: ídem

    public GetUserHandler(IUserRepository repo, ILogger<GetUserHandler> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task<GetUserResponse> Handle(   // ← public: es el contrato de IRequestHandler
        GetUserRequest request, CancellationToken ct) { ... }
}
```

---

## DIP - Domain define el contrato; Infrastructure lo implementa

```csharp
// Users.Domain/Repositories/IUserProfileRepository.cs
// La interfaz vive en Domain — Domain no depende de nadie
public interface IUserProfileRepository
{
    Task<UserProfile?> GetByPublicIdAsync(
        Guid publicId, long tenantId, CancellationToken ct = default);
    Task<IReadOnlyCollection<UserProfile>> GetPagedAsync(
        long tenantId, int page, int pageSize, CancellationToken ct = default);
    Task InsertAsync(
        Guid publicId, long tenantId, string fullName, CancellationToken ct = default);
    Task<bool> UpdateAsync(
        Guid publicId, long tenantId, string fullName, CancellationToken ct = default);
    Task<bool> DisableAsync(
        Guid publicId, long tenantId, CancellationToken ct = default);
}
```

**Nota:** todos los métodos reciben `tenantId` - el aislamiento multi-tenant se aplica desde el repositorio.

**Regla:** `Users.Application` y `Users.Domain` nunca importan `Users.Infrastructure`.

---

## DIP - Application no sabe que Infrastructure existe

```csharp
// Users.Application/UseCases/GetUserProfile/GetUserProfileHandler.cs
public sealed class GetUserProfileHandler
    : IRequestHandler<GetUserProfileRequest, GetUserProfileResponse>
{
    private readonly IUserProfileRepository _profiles; // ← interfaz de Domain

    public async Task<GetUserProfileResponse> Handle(
        GetUserProfileRequest request, CancellationToken ct)
    {
        var profile = await _profiles.GetByPublicIdAsync(
            request.PublicId, request.TenantId, ct);  // ← TenantId siempre presente

        if (profile is null)
            return new GetUserProfileNotFoundFailure("Perfil no encontrado.");

        return new GetUserProfileSuccess(
            new UserProfileDto(profile.PublicId, profile.FullName,
                               profile.IsActive, profile.CreatedAtUtc,
                               profile.UpdatedAtUtc));
    }
}
```

```
Users.Application ──→ IUserProfileRepository ←── Users.Infrastructure
     (usa)              (en Users.Domain)           (implementa)
```

Si mañana cambias EF Core -> Dapper: el Handler no se modifica.

---

## ¿YAGNI dice "no crees `IUserRepository` con una sola implementación"?

**No.** YAGNI tiene una excepción: cuando el costo de agregar después es muy alto.

| Escenario | Sin `IUserRepository` | Con `IUserRepository` |
|-----------|-----------------------|----------------------|
| Cambiar EF Core -> Dapper | Modificar todos los Handlers | Solo cambiar `UserRepository` |
| Testear un Handler en aislamiento | Mock casi imposible | `FakeUserRepository : IUserRepository` |
| Agregar cache (Decorator) | Rompe la cadena de inyección | `CachedUserRepository : IUserRepository` |

Crear la interfaz hoy: **~5 minutos**.
Retrofittarla después: **1-2 horas + riesgo de regresiones** en todos los Handlers.

**YAGNI** = no implementes lo que ningún requisito exige.
El cruce de capa Application->Infrastructure **siempre** exige abstracción (DIP).
Cuando DIP aplica, el costo de no hacerlo hoy siempre supera el costo de hacerlo.

---

## Complejidad ciclomática - los tres umbrales

| CC | Señal | Acción |
|----|-------|--------|
| **≤ 10** | Aceptable | No refactorizar por refactorizar |
| **11 - 15** | Monitorear | ¿Existe una abstracción natural aquí? |
| **> 15** | Refactorizar | Demasiados caminos para testear o mantener |

```csharp
// CC = 7 — dentro del umbral aceptable (≤ 10) — NO refactorizar
public string GetPostLabel(Post post) {
    if (post.Score > 1000 && post.AgeHours < 24) return "Hot";   // +2
    else if (post.Score > 500)  return "Popular";                  // +1
    else if (post.AgeHours < 1) return "New";                      // +1
    else if (post.IsSticky)     return "Pinned";                   // +1
    else if (post.IsNSFW)       return "NSFW";                     // +1
    else if (post.IsSpoiler)    return "Spoiler";                  // +1
    else return "Normal";
}
// Extraer cada condición a su propia clase violaría KISS — CC ≤ 10 está bien.
```

---

<!-- _class: lead -->

# Parte 2
## Records

---

## ¿Por qué records? El problema de igualdad

```csharp
// Con clase — comparación por referencia (identidad)
public class PuntoClass { public int X; public int Y; }

var a = new PuntoClass { X = 1, Y = 2 };
var b = new PuntoClass { X = 1, Y = 2 };
Console.WriteLine(a == b);  // false — objetos distintos en memoria

// Con record — comparación por valor (contenido)
public record PuntoRecord(int X, int Y);

var c = new PuntoRecord(1, 2);
var d = new PuntoRecord(1, 2);
Console.WriteLine(c == d);     // true — mismos valores → iguales
Console.WriteLine(c.ToString()); // "PuntoRecord { X = 1, Y = 2 }" — descriptivo
```

**El compilador genera automáticamente:** `Equals` - `GetHashCode` - `==` - `ToString` - `Deconstruct`

---

## Record posicional - el más usado en el proyecto

```csharp
// Declaración: los parámetros son las propiedades (init-only)
public sealed record GetUserRequest(Guid PublicId)
    : IRequest<GetUserResponse>;

public sealed record UserDto(
    Guid     UserId,
    string   FullName,
    string   Email,
    bool     IsActive,
    DateTime CreatedAtUtc);

// Lo que genera el compilador (equivalente expandido):
// public Guid UserId { get; init; }   ← init: se asigna solo en construcción
// public UserDto(Guid userId, ...) { UserId = userId; ... }
// public void Deconstruct(out Guid userId, ...) { ... }
```

---

## `with` expression - copiar con modificaciones

Records son inmutables. Para "cambiar" un valor, creas una copia:

```csharp
var original = new UserDto(
    UserId:      Guid.NewGuid(),
    FullName:    "Ana García",
    Email:       "ana@example.com",
    IsActive:    true,
    CreatedAtUtc: DateTime.UtcNow);

// Copia con Email diferente — el resto igual
var actualizado = original with { Email = "ana.nueva@example.com" };

Console.WriteLine(original.Email);    // "ana@example.com"   — sin cambios
Console.WriteLine(actualizado.Email); // "ana.nueva@example.com"

// Útil en tests: crear variantes de un objeto base
var inactivo = original with { IsActive = false };
```

---

## Records en cada capa del proyecto

| Capa | Tipo | Por qué |
|------|------|---------|
| Domain - Entidades | `class` | Dapper mapea propiedades mutables (`set`) |
| Domain - Value Objects | `sealed record` | Inmutable, igualdad por valor |
| Application - Request | `sealed record` | Mensaje inmutable al Handler |
| Application - Response base | `abstract record` | Tipo base polimórfico |
| Application - Response variantes | `sealed record` | Éxito y fallos específicos |
| Application - DTO | `sealed record` | Transferencia de datos inmutable |
| Infrastructure - Nada | `sealed class` | Tienen estado (conexiones, repos) |

---

## La trampa de `ISuccess<TSelf>` - referencia circular

```csharp
// NO hacer — data.Data.Data.Data... → bucle de serialización JSON
public sealed record GetUsersSuccess(
    IReadOnlyCollection<UserDto> Users, int Total)
    : GetUsersResponse, ISuccess<GetUsersSuccess>  // T = sí mismo
{
    public GetUsersSuccess Data => this;  // CIRCULAR
}

// Bien: Usar ISuccess (sin genérico) cuando el éxito tiene múltiples campos
public sealed record GetUsersSuccess(
    IReadOnlyCollection<UserDto> Users, int Total, int Page)
    : GetUsersResponse, ISuccess;   // ← sin T

// En el Presenter:
if (notification is GetUsersSuccess success)
    _viewModel.OK(success);  // pasa el record completo — no .Set()
```

---

<!-- _class: lead -->

# Parte 3
## Generics

---

## Por qué `ResultViewModel<T>` es genérico

```csharp
// ¿Por qué no un ViewModel sin T?
// Sin genérico, el DI container no puede distinguir el ViewModel de cada controller
// — todos serían el mismo tipo y el Scoped se compartiría entre controllers

// Con genérico — cada controller tiene SU ViewModel en el DI
public class ResultViewModel<T>   // T = el Controller que lo usa
{
    public object? Data { get; private set; }
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = string.Empty;
}

// En DI — genérico abierto: typeof(ResultViewModel<>) sin especificar T
services.AddScoped(typeof(ResultViewModel<>));

// El container crea la versión correcta automáticamente:
// GetUsersController → ResultViewModel<GetUsersController>
// AuthController    → ResultViewModel<AuthController>
// Son tipos DISTINTOS → instancias DISTINTAS en el mismo request
```

---

## Genéricos en el proyecto - los más importantes

```csharp
// IRequestHandler<TRequest, TResponse> — dos parámetros de tipo
// TRequest debe implementar IRequest<TResponse> (constraint)
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}

// Implementación — el compilador verifica que los tipos sean compatibles:
public sealed class GetUserHandler
    : IRequestHandler<GetUserRequest, GetUserResponse>
//                    ↑ TRequest      ↑ TResponse

// ISuccess<T> — tipo del dato exitoso
public sealed record GetUserSuccess(UserDto Data)
    : GetUserResponse, ISuccess<UserDto>;
//                    ↑ T = UserDto
```

---

## QuerySingleAsync\<T\> - inferencia de tipo en Dapper

```csharp
// El método genérico — T = tipo que Dapper mapeará
public Task<T?> QuerySingleAsync<T>(string sql, object? param = null,
    CancellationToken ct = default)
{
    return connection.QuerySingleOrDefaultAsync<T>(sql, param);
}

// Uso — T se especifica explícitamente (Dapper no puede inferirlo solo)
var user  = await db.QuerySingleAsync<User>("SELECT ...", new { id });
var count = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) ...");
var posts = await db.QueryAsync<PostDto>("SELECT ...", new { communityId });

// Constraint útil — where T : class
// garantiza que T no es int, bool, struct — evita ambigüedad con nullable
public Task<T?> QuerySingleAsync<T>(string sql, ...) where T : class
```

---

<!-- _class: lead -->

# Parte 4
## Pattern Matching

---

## `is` con asignación - en Presenters

```csharp
// Users.Presentation/Presenters/GetUserProfilePresenter.cs
public sealed class GetUserProfilePresenter
    : INotificationHandler<GetUserProfileResponse>
{
    private readonly ResultViewModel<UsersController> _viewModel;

    public GetUserProfilePresenter(ResultViewModel<UsersController> viewModel)
        => _viewModel = viewModel;

    public Task Handle(GetUserProfileResponse notification, CancellationToken ct)
    {
        if (notification is IFailure failure)           // ← IFailure PRIMERO
            _viewModel.Fail(failure.Message);
        else if (notification is ISuccess<UserProfileDto> success)
            _viewModel.Set(success);

        return Task.CompletedTask;
    }
}
```

**Jerarquía real en `Common/Common/Results/IFailure.cs`:**
```csharp
public interface IFailure          { string Message { get; } }
public interface INotFoundFailure  : IFailure { }   // ← SÍ existe
public interface IValidationFailure: IFailure { }
public interface IConflictFailure  : IFailure { }
```

**Regla del Presenter:** `IFailure` va primero porque captura toda la jerarquía.
El Controller distingue tipos específicos para decidir el status code HTTP:
```csharp
return result is GetUserProfileNotFoundFailure ? NotFound(_viewModel) : StatusCode(500, _viewModel);
```

---

## `switch` expression - en Controllers y Handlers

```csharp
// Mapear canal a tipo de procesamiento — switch por valor (string)
public async Task<GetConversationsResponse> Handle(
    GetConversationsRequest req, CancellationToken ct)
{
    var conversations = req.Channel switch
    {
        "whatsapp"  => await _repo.GetByChannelAsync(req.TenantId, "whatsapp", ct),
        "instagram" => await _repo.GetByChannelAsync(req.TenantId, "instagram", ct),
        _           => await _repo.GetAllAsync(req.TenantId, ct)
    };
    return new GetConversationsSuccess(conversations);
}

// En el Controller — switch por tipo para decidir el status code
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
{
    var result = await Mediator.Send(
        new GetUserProfileRequest(id, CurrentTenantId), ct);

    if (_viewModel.IsSuccess) return Ok(_viewModel);

    return result is GetUserProfileNotFoundFailure
        ? NotFound(_viewModel)
        : StatusCode(500, _viewModel);
}
```

---

## El Controller real - patrón completo

```csharp
[Route("api/users")]
[Authorize]
public sealed class UsersController : BaseApiController
{
    private readonly ILogger<UsersController>         _logger;
    private readonly ResultViewModel<UsersController> _viewModel;

    public UsersController(IMediator mediator,
        ILogger<UsersController> logger,
        ResultViewModel<UsersController> viewModel) : base(mediator)
    { _logger = logger; _viewModel = viewModel; }

    private long CurrentTenantId =>        // ← lee el TenantId del JWT
        long.TryParse(User.FindFirstValue("tenant_id"), out var id) ? id : 0;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        try
        {
            var result = await Mediator.Send(
                new GetUserProfileRequest(id, CurrentTenantId), ct); // ← siempre pasa TenantId
            if (_viewModel.IsSuccess) return Ok(_viewModel);
            return result is GetUserProfileNotFoundFailure
                ? NotFound(_viewModel) : StatusCode(500, _viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetById User");
            return StatusCode(500, _viewModel.Fail(ex.Message));
        }
    }
}
```

---

## La capa Contracts - la API pública de cada módulo

```
Users.Contracts/
├── Dtos/
│   └── UserProfileDto.cs          ← el DTO que otros módulos pueden usar
└── Events/
    └── UserRegisteredIntegrationEvent.cs  ← evento para comunicación entre módulos
```

```csharp
// Users.Contracts/Dtos/UserProfileDto.cs
// Este record lo usa Users.Application en el Success, y podría usarlo
// Authentication si necesita datos del perfil
public sealed record UserProfileDto(
    Guid     PublicId,
    string   FullName,
    bool     IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
```

**Regla de dependencia entre módulos:**
`Tenancy` puede referenciar `Users.Contracts` - nunca `Users.Application` ni `Users.Domain`.
La capa `Contracts` es el único punto de contacto entre módulos.

---

## Multi-tenancy - baked in desde el día 1

```csharp
// TenantClaimsMiddleware extrae el tenantId del JWT
// y lo pone disponible en el ITenantContextAccessor

// En el Controller — siempre inyectado en la Request
private long CurrentTenantId =>
    long.TryParse(User.FindFirstValue("tenant_id"), out var id) ? id : 0;

// En el Handler — viene en la Request
var profile = await _profiles.GetByPublicIdAsync(
    request.PublicId,
    request.TenantId,   // ← cada query filtra por tenant
    ct);

// En el Repository — cada método lo recibe como parámetro
Task<UserProfile?> GetByPublicIdAsync(
    Guid publicId,
    long tenantId,       // ← nunca se puede acceder a datos de otro tenant
    CancellationToken ct = default);
```

**El contrato es claro:** si olvidas pasar `tenantId` -> no compila.
No hay magia ni variables globales - el aislamiento es explícito.

---

<!-- _class: lead -->

# Parte 5
## Async / Await

---

## El pool de hilos - por qué async importa

```
Pool de hilos (8 hilos disponibles):
[H1] [H2] [H3] [H4] [H5] [H6] [H7] [H8]

Sin async — H1 se bloquea esperando PostgreSQL:
[H1 BLOQUEADO···200ms] [H2] [H3] [H4] [H5] [H6] [H7] [H8]
↑ Si llegan 8 requests simultáneas que consultan BD → pool agotado

Con async — H1 libera el hilo mientras espera:
[H1→BD]                    [H1 libre para otra request]
         (PostgreSQL trabaja solo)
                            [resultado llega, cualquier hilo libre lo toma]
↑ Con 8 hilos puedes atender cientos de requests simultáneas
```

**Resultado:** el mismo hardware atiende más usuarios.

---

## La cadena async - de punta a punta

```
HTTP GET /users/{id}
  ↓
Controller.GetById(id, ct)           → async Task<IActionResult>
  ↓ await
Mediator.Send(request, ct)           → async Task<Response>
  ↓ await
GetUserHandler.Handle(request, ct)   → async Task<GetUserResponse>
  ↓ await
IUserRepository.GetByPublicIdAsync() → Task<User?>   ← sin async (solo delega)
  ↓ (delega directamente)
UsersSql.GetByPublicIdAsync()        → Task<User?>   ← sin async (Dapper devuelve Task)
  ↓ await (Dapper)
PostgreSQL ejecuta la query
  ↓ responde
```

**Nota:** los métodos que solo **delegan** no necesitan `async`/`await` - retornan el Task directamente.

---

## Los 5 errores de async - memorizarlos

```csharp
// Error 1 — async void: las excepciones no se pueden capturar
public async void CargarDatos() { ... }          // NUNCA

// Error 2 — .Result bloquea el hilo → posible deadlock en ASP.NET
var user = repo.GetByIdAsync(id, ct).Result;     // NUNCA en ASP.NET

// Error 3 — olvidar await: task es un Task<User?>, no un User?
var task = repo.GetByIdAsync(id, ct);
if (task is null)  // LÓGICO ERROR — task nunca es null

// Error 4 — no propagar CancellationToken
await db.QuerySingleAsync<User>(sql, new { id });  // falta: cancellationToken: ct

// Error 5 — async sin await: el compilador avisa, pero es sutil
public async Task<string> GetName() { return "Ana"; }  // async inútil
```

---

## `CancellationToken` - la señal de cancelación

```csharp
// ASP.NET lo inyecta automáticamente en los métodos de Controller
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
//                                                 ↑ ASP.NET lo llena automáticamente

// El token viaja hacia abajo en toda la cadena
_ = await Mediator.Send(new GetUserRequest(id), ct);
//                                              ↑ pasa al Handler

// El Handler lo pasa al repo
var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);
//                                                           ↑ pasa al Sql

// El Sql lo pasa a Dapper/PostgreSQL
=> db.QuerySingleOrDefaultAsync<User>(sql, param, cancellationToken: ct);
//                                               ↑ si el cliente cancela → PostgreSQL también cancela
```

---

<!-- _class: lead -->

# Parte 6
## Configuration

---

## IConfiguration vs IOptions\<T\>

```csharp
// Mal: IConfiguration directo — acceso por string, sin tipado, difícil de testear
public JwtService(IConfiguration config)
{
    _key = config["Jwt:Key"] ?? throw new InvalidOperationException("...");
}

// Bien: IOptions<T> — tipado, validable, testeable
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    [Required, MinLength(32)]
    public string Key { get; init; } = string.Empty;
    [Required]
    public string Issuer { get; init; } = string.Empty;
    [Range(1, 10080)]
    public int ExpiresInMinutes { get; init; } = 60;
}

// En Program.cs — valida al iniciar, no en la primera request
builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();          // ← fail fast: la app no arranca si falta config
```

---

## Capas de configuración - prioridad

```
appsettings.json               ← base, siempre presente, en el repo
appsettings.{Env}.json         ← sobreescribe por ambiente
appsettings.Local.json         ← solo local, en .gitignore
User Secrets                   ← solo Development, fuera del repo
Variables de entorno           ← sobreescriben todo
Argumentos CLI                 ← máxima prioridad
```

```bash
# Variable de entorno → clave de configuración
# __ (doble guión bajo) = separador de sección
Jwt__Key=mi-clave-de-32-chars     →  Jwt:Key
Jwt__ExpiresInMinutes=120          →  Jwt:ExpiresInMinutes
ConnectionStrings__Default=Host=.. →  ConnectionStrings:Default
```

---

## Secretos - nunca en el repo

```json
// NUNCA en appsettings.json — se sube a GitHub
{
  "Jwt": { "Key": "mi-clave-secreta-real-de-produccion" }
}
```

```bash
# Bien: Development local — User Secrets (fuera del repo, en tu máquina)
dotnet user-secrets init --project Reddit.WebApi
dotnet user-secrets set "Jwt:Key" "clave-local-dev-32-chars!!!!!" \
  --project Reddit.WebApi

# Bien: Staging/Producción — variables de entorno o AWS Secrets Manager
# En docker-compose.yml:
environment:
  - Jwt__Key=${JWT_KEY}        # viene del .env (en .gitignore) o del CI/CD
  - ASPNETCORE_ENVIRONMENT=Production
```

---

## Hands-on - ### Tour del back-template (Módulo Users)

1. Navegar la estructura de carpetas - entender el patrón Modular Monolith
2. Seguir `GetUserProfile` de Controller -> Handler -> Repository -> Response -> Presenter:
   - `UsersController.GetById` -> `Mediator.Send(new GetUserProfileRequest(id, TenantId))`
   - `GetUserProfileHandler` -> `_profiles.GetByPublicIdAsync(publicId, tenantId)`
   - `GetUserProfileSuccess` / `GetUserProfileNotFoundFailure`
   - `GetUserProfilePresenter` -> `_viewModel.Fail` / `_viewModel.Set`
3. Identificar en el código:
   - Dónde aplica `sealed` vs `abstract` vs `static`
   - La capa `Users.Contracts` y su rol
   - El `CurrentTenantId` en el Controller y cómo viaja hasta el Repository
   - Pattern matching en el Presenter (IFailure primero)
   - La cadena async completa con CancellationToken
   - `IOptions<JwtSettings>` en `Host.Api/Extensions/JwtAuthExtensions.cs`

**Instrucciones detalladas -> S03/hands-on.md**

---

## Desarrollo - ### Agregar módulo `Contacts` al sistema de mensajería

Siguiendo exactamente el patrón del módulo `Users`:

```
Modules/Contacts/
├── Contacts.Domain/
│   ├── Entities/Contact.cs
│   └── Repositories/IContactRepository.cs
├── Contacts.Application/
│   └── UseCases/GetContact/
│       ├── GetContactRequest.cs          ← sealed record con PublicId + TenantId
│       ├── GetContactHandler.cs          ← IRequestHandler<...>
│       └── Responses/
│           ├── GetContactResponse.cs     ← abstract record (base)
│           ├── GetContactSuccess.cs      ← sealed record : ISuccess<ContactDto>
│           └── GetContactNotFoundFailure.cs
├── Contacts.Contracts/
│   └── Dtos/ContactDto.cs
├── Contacts.Infrastructure/
│   └── Repositories/ContactRepository.cs
└── Contacts.Presentation/
    ├── Controllers/ContactsController.cs
    ├── Presenters/GetContactPresenter.cs
    └── ServiceCollectionEx.cs
```

Registrar en `Host.Api/Program.cs`: `AddContactsApplicationServices()` - `AddContactsInfrastructureServices()` - `AddContactsWebApiServices()`

---

## Homework S03

### Lecturas (1-2 horas)
- `dev-notes/02-programacion/csharp/08-async.md` - sección de errores comunes (async void, .Result, CancellationToken)
- `dev-notes/02-programacion/csharp/04-records.md` - herencia de records y cuándo NO usar record
- `dev-notes/02-programacion/csharp/19-configuration.md` - IOptions\<T\> y ValidateOnStart

### Ejercicio (2 horas)
Completar el módulo `Contacts` del Hands-on - agregar el caso de uso `CreateContact`:

- `CreateContactRequest(string FullName, string Email, long TenantId)`
- Handler valida que no exista un contacto con el mismo Email en el mismo Tenant
- Responses: `CreateContactSuccess(ContactDto)` y `CreateContactEmailConflictFailure`
- Presenter: `IFailure` primero, luego `ISuccess<ContactDto>`
- Controller: `POST /api/contacts` -> 201 Created en éxito, 409 Conflict en email duplicado
- Registrado en `Program.cs`

**Criterio de aceptación:** el endpoint responde correctamente con Postman/Scalar - se revisa en S04.

---

## S04 - Preview

**Patrones creacionales y estructurales (10 patrones GoF)**

- Singleton - Factory Method - Abstract Factory - Builder - Prototype
- Adapter - Bridge - Composite - Decorator - Facade - Flyweight - Proxy
- Cada patrón anclado a código real del sistema de mensajería

**Prerrequisito:** el módulo `Contacts` con `GetContact` + `CreateContact` debe estar completo y el repo compilando.

---

<!-- _class: lead -->

# ¡A desensamblar el template!
## Tour
### Guía en S03/hands-on.md

---

<!-- _class: lead -->

# Parte 6B - Serilog - Structured Logging

## Niveles - ILogger\<T\> - Sinks - Anti-patrones

> "El primer síntoma de un sistema en producción con problemas
> es que nadie puede leer los logs."

---

## ¿Por qué no Console.WriteLine?

**Los problemas del logging naïve:**

```csharp
// Mal: Lo que NO debes hacer nunca
Console.WriteLine("Usuario logueado: " + userId);
// → Solo visible en la consola local
// → Se pierde en producción (contenedores no tienen consola permanente)
// → Sin timestamp, sin nivel, sin contexto de tenant
// → No se puede consultar: ¿cuántos errores hubo hoy?
```

**Lo que da Structured Logging:**
- Cada log es un evento con propiedades (`timestamp`, `level`, `UserId`, `TenantId`, `message`)
- Se puede filtrar, agregar, alertar en Seq / CloudWatch / Grafana Loki
- El mismo evento se envía a consola en dev y a un sink cloud en producción

---

## Log Levels - cuál usar cuándo

| Nivel | Cuándo usarlo | En producción |
|-------|--------------|--------------|
| `Trace` | Flujo interno muy granular | Nunca - satura los logs |
| `Debug` | Diagnóstico en desarrollo | Solo en dev |
| `Information` | Eventos de negocio normales | Nivel mínimo recomendado |
| `Warning` | Algo inusual que no rompe el flujo | Siempre |
| `Error` | Fallo recuperable con contexto | Siempre + alertas |
| `Critical` | El sistema no puede continuar | Siempre + PagerDuty |

**Configuración por entorno:**

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

---

## String interpolation vs Structured Logging

Esta es la regla más importante - y la más fácil de romper sin querer.

```csharp
// MAL — string interpolation en Serilog
_logger.LogInformation($"Conversación {convId} asignada al agente {agentId}");
// Resultado en el sink: "Conversación 42 asignada al agente 7"
// → Solo texto plano. No se puede filtrar por ConvId = 42.
// → El analizador de C# muestra warning CA2254 — no ignorarlo.

// BIEN — Structured Logging con message templates
_logger.LogInformation(
    "Conversación {ConvId} asignada al agente {AgentId}",
    convId, agentId);
// Resultado en el sink:
// { "message": "Conversación 42 asignada al agente 7",
//   "ConvId": 42, "AgentId": 7, "@t": "2026-05-30T...", "@l": "Information" }
// → Se puede filtrar: WHERE ConvId = 42
// → Se puede agregar: ¿cuántas conversaciones asignó el agente 7 hoy?
```

> **Esta regla está en el CLAUDE.md del proyecto.**
> No usar string interpolation en Serilog. Nunca.

---

## ILogger\<T\> - cómo llega al servicio

ASP.NET Core inyecta `ILogger<T>` automáticamente - no necesitas registrarlo.

```csharp
public sealed class CreateConversationHandler(
    IConversationRepository _repo,
    ILogger<CreateConversationHandler> _logger)      // ← inyectado por DI
    : IRequestHandler<CreateConversationRequest, CreateConversationResponse>
{
    public async Task<CreateConversationResponse> Handle(
        CreateConversationRequest request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Creando conversación para tenant {TenantId} canal {ChannelId}",
            request.TenantId, request.ChannelId);

        var result = await _repo.InsertAsync(request.TenantId, request.ChannelId, ct);

        if (result is null)
        {
            _logger.LogWarning(
                "Canal {ChannelId} no existe para tenant {TenantId}",
                request.ChannelId, request.TenantId);
            return new CreateConversationChannelNotFoundFailure("Canal no encontrado.");
        }

        _logger.LogInformation(
            "Conversación {ConvId} creada exitosamente para tenant {TenantId}",
            result.Id, request.TenantId);

        return new CreateConversationSuccess(/* dto */);
    }
}
```

---

## Serilog en Program.cs

```bash
# Paquetes necesarios
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq          # para desarrollo
```

```csharp
// Program.cs
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()           // para LogContext.PushProperty(...)
        .Enrich.WithMachineName()          // hostname de la instancia (útil en k8s)
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
        .WriteTo.Seq(context.Configuration["Seq:Url"] ?? "http://localhost:5341");
});

// Reemplaza el logging nativo — no agregar ambos
app.UseSerilogRequestLogging(); // loguea cada HTTP request automáticamente
```

---

## Seq - el Grafana de los logs en desarrollo

Seq es un servidor local para visualizar y consultar logs estructurados.

```yaml
# docker-compose.yml
services:
  seq:
    image: datalust/seq:latest
    ports:
      - "5341:5341"     # ingest (al que apunta Serilog)
      - "8080:80"       # UI → http://localhost:8080
    environment:
      ACCEPT_EULA: Y
```

**Queries de Seq que usarás todos los días:**

```sql
-- Todos los logs de error de hoy
@Level = 'Error'

-- Logs del tenant 1 en el módulo de mensajes
TenantId = 1 and SourceContext like 'Startup.Messages%'

-- Peticiones que tardaron más de 500ms
ElapsedMilliseconds > 500

-- Excepciones de un tipo específico
@Exception like '%DbUpdateException%'
```

---

## LogContext - enriquecer logs automáticamente

Con `LogContext` puedes agregar propiedades a TODOS los logs de una petición HTTP
sin pasarlas manualmente a cada método.

```csharp
// Middleware — ejecuta antes de cada petición
app.Use(async (context, next) =>
{
    var tenantId = context.Items["TenantId"]?.ToString() ?? "unknown";
    var userId   = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anon";

    using (LogContext.PushProperty("TenantId", tenantId))
    using (LogContext.PushProperty("UserId", userId))
    {
        await next();
    }
    // Al salir del using, las propiedades se eliminan automáticamente
});

// Ahora en cualquier Handler, Controller o Service:
_logger.LogInformation("Conversación creada");
// Genera: { "message": "Conversación creada", "TenantId": "1", "UserId": "99", ... }
// SIN necesitar pasar tenantId explícitamente al logger
```

---

## Logging en el Handler de la Request - patrón completo

```csharp
public async Task<AssignConversationResponse> Handle(
    AssignConversationRequest request, CancellationToken ct)
{
    // Log de entrada con contexto relevante
    _logger.LogInformation(
        "Asignando conversación {ConvId} al agente {AgentId}",
        request.ConvId, request.AgentId);

    var conversation = await _repo.GetByIdAsync(request.TenantId, request.ConvId, ct);
    if (conversation is null)
    {
        _logger.LogWarning(
            "Conversación {ConvId} no encontrada para tenant {TenantId}",
            request.ConvId, request.TenantId);
        return new AssignConversationNotFoundFailure("Conversación no encontrada.");
    }

    try
    {
        await _repo.AssignAsync(request.TenantId, request.ConvId, request.AgentId, ct);
    }
    catch (Exception ex)
    {
        // Siempre pasar la excepción como primer argumento — incluye el stack trace
        _logger.LogError(ex,
            "Error al asignar conversación {ConvId} al agente {AgentId}",
            request.ConvId, request.AgentId);
        throw;
    }

    _logger.LogInformation(
        "Conversación {ConvId} asignada exitosamente al agente {AgentId}",
        request.ConvId, request.AgentId);

    return new AssignConversationSuccess(/* dto */);
}
```

---

## Anti-patrones de logging

```
No: Loguear datos sensibles
   _logger.LogDebug("JWT: {Token}", jwtToken);
   _logger.LogInformation("Contraseña: {Password}", request.Password);
   → Datos sensibles en logs = vulnerabilidad de seguridad real.
   → Nunca loguear credentials, tokens completos, ni PII.

No: Nivel de log incorrecto
   _logger.LogError("Entrando al método GetConversations");
   → Error es para fallos. Los sistemas de alertas se disparan en Error+.
   → Usar Information para flujo normal.

No: Exception sin contexto completo
   catch (Exception ex) { _logger.LogError(ex.Message); }
   → Solo el mensaje, sin stack trace. Imposible de debuggear.
   → Correcto: _logger.LogError(ex, "Contexto: {ConvId}", convId);

No: String interpolation
   _logger.LogInformation($"Tenant: {tenantId}");
   → Pierde la estructura. Warning CA2254 de Roslyn lo advierte.
   → Siempre usar message templates con {PlaceholderName}.

No: Log masivo en loops
   foreach (var msg in messages)
       _logger.LogInformation("Procesando {MsgId}", msg.Id);
   → 50,000 mensajes = 50,000 logs. Usar LogDebug + nivel mínimo Warning en prod.
```

---

## Resumen - Logging con Serilog

| Regla | Correcto | Incorrecto |
|-------|-----------|-------------|
| Nivel | `LogInformation` para flujo normal | `LogError` para todo |
| Template | `"Msg {ConvId}", convId` | `$"Msg {convId}"` |
| Exception | `LogError(ex, "msg {id}", id)` | `LogError(ex.Message)` |
| Datos sensibles | No loguear tokens/passwords | Loguear JWT o hashes |
| Contexto | `LogContext.PushProperty` en middleware | Pasar manualmente cada propiedad |
| Dev | Seq en Docker + nivel Debug | Console.WriteLine |
| Producción | CloudWatch / Datadog / Grafana Loki | Seq expuesto públicamente |

---

<!-- _class: lead -->

# Parte 7
## Herramientas - Proceso - Documentación de Repositorios

---

## ¿Qué cubre la Parte 7?

| Sub-sección | Temas |
|-------------|-------|
| **7A - SDLC y Sprints** | Ceremonias - DoD - User Stories - INVEST - PR workflow - Code Review |
| **7B - SemVer** | Versiones - Pre-releases - Conventional Commits - Git tags - historial real |
| **7C - Story Points** | Planning Poker - Velocity - Burndown - Calibración - deuda técnica |
| **7D - Documentación del repositorio** | README - CHANGELOG - CONTRIBUTING - /docs - ADRs - AGENTS.md - .editorconfig |

---

<!-- _class: lead -->

# 7A - SDLC y Sprints

---

## SDLC en un sprint de 2 semanas

| Fase SDLC | Actividad concreta en el sprint |
|-----------|--------------------------------|
| Planificación | Sprint Planning: qué stories entran, estimación en puntos |
| Análisis | Refinement: criterios de aceptación claros en cada issue |
| Diseño | ADR si hay decisión de arquitectura; ERD si cambia el esquema |
| Implementación | Feature branch -> commits atómicos -> PR |
| Testing | Unit + integration tests en el PR; pipeline verde = requisito |
| Despliegue | Merge a `main` -> CI/CD despliega a staging automáticamente |
| Revisión | Sprint Review: demo al cliente; Retro: qué mejorar |

**Regla:** ningún PR se mergea sin review + pipeline verde.

---

## Calendario de ceremonias - 2 semanas

```
Lunes    S1 │ Sprint Planning (2h) — quién hace qué, estimación, sprint goal
Martes   S1 │ Daily (15 min) — ¿qué hice? ¿qué haré? ¿hay un blocker?
   ...   …  │ Daily diario — máx 15 min, de pie, sin profundizar
Jueves   S1 │ Refinement (1h) — preparar backlog del siguiente sprint
Viernes  S2 │ Sprint Review (1h) — demo de lo que se terminó al cliente
Viernes  S2 │ Retrospectiva (45 min) — qué mejorar en el proceso
```

**Regla de las dailies:** si el blocker no se resuelve en 30 segundos, se agenda una reunión aparte.
Las dailies no son reuniones de status para el manager - son sincronización entre pares.

---

## Sprint Planning - cómo funciona paso a paso

```
1. Product Owner presenta el Sprint Goal (objetivo del sprint en 1 oración)
2. El equipo toma stories del backlog (ordenado por valor de negocio)
3. Cada story se descompone en tareas técnicas concretas
4. Se estima cada story en Story Points (Planning Poker)
5. Se acuerda el Sprint Backlog: qué entra, en qué orden
```

**Ejemplo - Sistema de mensajería Sprint 1:**

| Story | Puntos | Responsable |
|-------|--------|-------------|
| US-01: Login con JWT + refresh token | 5 | Dev B |
| US-02: Registro de workspace | 3 | Dev A |
| US-03: CRUD de Contacts (GET + POST) | 8 | Dev C |
| US-04: Listar conversaciones paginadas | 5 | Dev D |
| **Total** | **21 pts** | |

---

## Definition of Done (DoD)

El DoD es el acuerdo del equipo sobre qué significa que una story esté **realmente** terminada.

**DoD del sistema de mensajería:**
- [ ] El código compila sin warnings
- [ ] Los tests pasan (unit + integration)
- [ ] El pipeline de CI está en verde
- [ ] El PR fue revisado por al menos 1 persona
- [ ] Los criterios de aceptación son verificables con Scalar/Postman
- [ ] No hay código muerto ni `TODO:` sin ticket asociado
- [ ] El CHANGELOG.md fue actualizado si hay cambio público
- [ ] No hay secretos hardcodeados en el código

**¿Por qué es crítico el DoD?**
Sin DoD, "terminado" significa cosas distintas para cada miembro del equipo.
Dev D dice "terminé" y Dev A descubre que falta el test de integración.

---

## User Stories - el formato

```
Como [tipo de usuario]
quiero [funcionalidad]
para [beneficio / valor de negocio]
```

**Ejemplos Sistema de mensajería:**

```
Como agente de soporte
quiero ver todas las conversaciones sin atender en la bandeja
para priorizarlas y no perder clientes.

Como administrador del workspace
quiero invitar a nuevos agentes por email
para que accedan al sistema sin contactarme directamente.

Como bot de WhatsApp
quiero recibir mensajes entrantes vía webhook de Meta
para procesarlos automáticamente según las reglas del flujo.

Como supervisor
quiero ver el tiempo promedio de respuesta por agente
para identificar cuellos de botella y mejorar la atención.
```

---

## INVEST - 6 criterios para una buena story

| Letra | Criterio | Pregunta que debes hacerte |
|-------|----------|---------------------------|
| **I** | Independent | ¿Puede desarrollarse sin depender de otra story no terminada? |
| **N** | Negotiable | ¿El cómo es negociable? ¿Solo el qué está fijo? |
| **V** | Valuable | ¿Aporta valor real al usuario o al negocio? |
| **E** | Estimable | ¿El equipo puede estimarla con los datos que tiene? |
| **S** | Small | ¿Cabe en un sprint? (regla: ≤ 8 puntos o dividirla) |
| **T** | Testable | ¿Tiene criterios de aceptación claros y verificables? |

**Story que falla INVEST:**
> "Mejorar el sistema para que sea más rápido y confiable"
> <- no estimable - no testeable - demasiado amplia - no hay valor específico

**Story que pasa INVEST:**
> "Como agente, quiero buscar contactos por nombre o email para encontrarlos en < 1 segundo"
> <- testeable - acotada - valiosa - estimable (5 pts) - independiente

---

## Acceptance Criteria - formato Gherkin

Gherkin es un lenguaje legible por negocio que define el comportamiento esperado:

```gherkin
Feature: Autenticación de agentes

  Scenario: Login exitoso
    Given el agente "dev-a@example.com" existe con contraseña válida
    When envía POST /api/auth/login con esas credenciales
    Then recibe 200 OK con access_token y refresh_token no vacíos

  Scenario: Contraseña incorrecta
    Given el agente "dev-a@example.com" existe
    When envía POST /api/auth/login con contraseña incorrecta
    Then recibe 401 Unauthorized con message "Credenciales inválidas"

  Scenario: Email no registrado
    When envía POST /api/auth/login con email "noexiste@example.com"
    Then recibe 401 Unauthorized
```

---

## Acceptance Criteria - Sistema de mensajería: CreateContact

```gherkin
Feature: Crear contacto

  Scenario: Contacto creado exitosamente
    Given estoy autenticado en el workspace "acme-corp"
    When envío POST /api/contacts con {"fullName":"Ana","email":"ana@example.com"}
    Then recibo 201 Created con el ContactDto del nuevo contacto
    And el contacto aparece en GET /api/contacts del mismo workspace

  Scenario: Email duplicado en el mismo workspace
    Given ya existe un contacto con email "ana@example.com" en el workspace "acme-corp"
    When envío POST /api/contacts con {"fullName":"Otra Ana","email":"ana@example.com"}
    Then recibo 409 Conflict con message "El email ya está registrado en este workspace"

  Scenario: Email pertenece a otro workspace (no es conflicto)
    Given existe "ana@example.com" en el workspace "otro-cliente"
    When envío POST /api/contacts desde workspace "acme-corp"
    Then recibo 201 Created — el aislamiento multi-tenant funciona
```

---

## Feature branches - convenciones de nombre

```bash
# Formato: tipo/numero-de-issue/descripcion-corta
feature/42/add-contact-endpoint
bugfix/87/fix-null-ref-in-contact-handler
hotfix/91/patch-jwt-expiry-calculation
chore/15/update-ef-core-to-10-1
refactor/23/extract-contact-validation-to-fluentvalidation
docs/55/add-adr-multitenancy-decision
test/61/add-integration-tests-for-auth

# Reglas:
# - Solo minúsculas y guiones (nunca guiones bajos ni espacios)
# - Incluir el número de issue — linkea automáticamente el PR al issue
# - Descripción corta (3-5 palabras) — qué hace, no quién lo hace
# - Nunca trabajar directamente sobre main
```

**¿Por qué el número de issue?**
GitHub cierra el issue automáticamente con "Closes #42" en el PR body.

---

## PR - Pull Request: el proceso completo

```
1. Crear branch:   git checkout -b feature/42/add-contact-endpoint
2. Desarrollar:    commits pequeños y atómicos (1 propósito por commit)
3. Push:           git push -u origin feature/42/add-contact-endpoint
4. Abrir PR:       contra main; llenar el PR template completamente
   - ¿Qué hace este PR? (2-3 líneas)
   - ¿Cómo testearlo? (pasos con curl / Postman / Scalar)
   - Link al issue: "Closes #42"
5. Revisor:        recibe notificación automática (CODEOWNERS)
6. Review:         comentarios constructivos con sugerencias concretas
7. Fixes:          nuevos commits (NO force push en PRs abiertos)
8. Aprobar:        reviewer aprueba + pipeline en verde
9. Merge:          Squash & Merge → 1 commit limpio en main
10. Cleanup:       la branch se elimina automáticamente
```

---

## Code Review - qué buscar

```
Sí: Corrección:
   - ¿El código hace lo que el issue pide?
   - ¿Cubre los Acceptance Criteria del Gherkin?
   - ¿Los tests son útiles (no triviales, no solo "compila")?

Sí: Seguridad:
   - ¿El tenantId se valida en cada query?
   - ¿Hay posibilidad de SQL injection (aunque usamos Dapper parametrizado)?
   - ¿Se loguean datos sensibles (emails, tokens)?

Sí: Arquitectura:
   - ¿El código está en la capa correcta? (lógica de negocio en Handler, no Controller)
   - ¿La interfaz está en Domain, la implementación en Infrastructure?
   - ¿Se viola la regla de dependencia entre capas?

Sí: Legibilidad:
   - ¿Los nombres son claros sin necesitar comentarios?
   - ¿El método es demasiado largo (> 30 líneas)?
   - ¿Hay comentarios que explican el QUÉ en vez del POR QUÉ?
```

---

## Code Review - antipatrones a evitar

```
No: "LGTM" sin leer el código
   → Si apruebas sin entender, eres responsable del bug que no viste

No: Nitpicking de estilo (tabs vs espacios, naming subjetivo)
   → .editorconfig + CI formatter automatiza el formato; no lo debatas en review

No: Review tardío: el PR tiene 48h sin reviewer
   → Acuerdo de equipo: review en < 4h laborales durante el sprint

No: PRs enormes: 800 líneas cambiadas en un solo PR
   → PR ≤ 400 líneas — si es más, dividir en PRs secuenciales

No: Aprobar por presión social: "Dev A lo propuso, debe estar bien"
   → Cada reviewer es responsable de lo que aprueba, independientemente de quién lo escribió

No: Comentar sin propuesta: "esto no me gusta"
   → Comentario útil: "considera usar ExecuteUpdateAsync en lugar de cargar la entidad — evitas un SELECT"
```

---

## Pipeline verde como requisito - qué corre en CI

```yaml
# .github/workflows/ci.yml — se ejecuta en cada PR
jobs:
  build:
    steps:
      - dotnet restore
      - dotnet build --no-restore --warnaserror   # warnings = errores en CI

  test:
    steps:
      - dotnet test --no-build --collect:"XPlat Code Coverage"
      # PostgreSQL real en GitHub Actions (no mocks de DB)
      # Si un test falla: el PR NO puede mergearse

  analyze:
    steps:
      - dotnet format --verify-no-changes         # .editorconfig aplicado
      # Si hay diferencias de formato: el PR NO puede mergearse
```

**Regla absoluta:** si el pipeline está en rojo, el PR no se mergea.
Ni aunque el código "se vea bien" o "sea solo un test flaky".

---

<!-- _class: lead -->

# 7B - SemVer

---

## SemVer - MAJOR.MINOR.PATCH

```
v  2  .  4  .  1
   ↑     ↑     ↑
MAJOR  MINOR  PATCH
```

| Segmento | Cuándo incrementa | Ejemplo de cambio |
|----------|------------------|-------------------|
| **PATCH** | Corrección de bug compatible | Fix en validación de email |
| **MINOR** | Feature nueva compatible | Agregar endpoint de búsqueda |
| **MAJOR** | Breaking change | Renombrar campo en la respuesta de API |

**Regla de reset:** al subir MINOR -> PATCH vuelve a 0. Al subir MAJOR -> MINOR y PATCH vuelven a 0.

```
v2.3.5  +  bug fix       →  v2.3.6
v2.3.5  +  nueva feature →  v2.4.0   ← MINOR sube, PATCH resetea
v2.3.5  +  breaking change → v3.0.0  ← MAJOR sube, todo resetea
```

---

## Breaking change - ejemplos concretos del CRM

```json
// v1.2.0 — respuesta de GET /api/contacts/:id
{ "publicId": "abc-123", "fullName": "Ana García", "email": "ana@example.com" }
```

| Cambio | ¿Breaking? | Versión | Razón |
|--------|-----------|---------|-------|
| Agregar campo `"phone": null` | No | v1.3.0 | Clientes ignoran campos nuevos |
| Renombrar `fullName` -> `name` | **Sí** | **v2.0.0** | Clientes buscan `fullName` y fallan |
| Eliminar campo `email` | **Sí** | **v2.0.0** | Clientes que usan `email` fallan |
| Cambiar `publicId` de string a int | **Sí** | **v2.0.0** | Rompe deserialización |
| Cambiar endpoint a `/api/v2/contacts` | **Sí** | **v2.0.0** | URL anterior falla |
| Fix en validación server-side | No | v1.2.1 | No afecta el contrato de la API |
| Cambiar orden de campos en JSON | No | v1.2.1 | JSON no es sensible al orden |

---

## SemVer 0.x.y - La fase inestable

Mientras `MAJOR = 0`, la API pública se considera **inestable**.
Cualquier versión puede romper compatibilidad sin que sea necesariamente MAJOR.

```
0.1.0   → primera entrega funcional (no productiva aún)
0.2.0   → iteración — puede romper cosas de 0.1.0
0.9.0   → feature-complete, en revisión final antes del MVP
1.0.0   → primera versión estable y pública
```

**¿Cuándo pasar a v1.0.0?**
- El equipo considera la API estable y no planea cambios breaking en el corto plazo
- Hay usuarios reales (clientes) que dependen de la API
- Existe documentación pública y un proceso de deprecación definido

**En el sistema de mensajería:** empezamos en `0.1.0`. Cuando el MVP esté en staging con el primer cliente real, evaluamos mover a `1.0.0`.

---

## Pre-release tags - alpha - beta - rc

```
v1.0.0-alpha.1   ← prueba interna — puede cambiar todo sin aviso
v1.0.0-alpha.2
v1.0.0-beta.1    ← prueba con usuarios seleccionados — API casi fija
v1.0.0-beta.2
v1.0.0-rc.1      ← Release Candidate — candidato final, solo fixes críticos
v1.0.0-rc.2
v1.0.0            ← Release estable — listo para producción
```

**Pre-releases no se instalan automáticamente** por NuGet/npm sin pedirlo:

```bash
# NuGet — instalar pre-release explícitamente
dotnet add package MiPaquete --version 1.0.0-beta.1

# npm — instalar tag beta
npm install mi-paquete@beta

# Docker — tag de pre-release
docker pull crm-api:1.0.0-rc.1
```

**¿Cuándo usar en el sistema de mensajería?**
Antes de cada release a staging de un cliente nuevo -> `v0.X.0-rc.1` para prueba interna de 48h.

---

## Build metadata - +build.123

```
v1.0.0+build.2026.05.29.001
        ↑ metadata — informativa, NO afecta la precedencia de versiones
        → dos builds del mismo código fuente pueden tener metadata diferente

v1.0.0-rc.1+sha.abc1234
             ↑ SHA del commit — rastrear exactamente qué código se desplegó
```

**Uso práctico en CI/CD:**

```yaml
# GitHub Actions — generar versión con metadata de build
- name: Set version
  run: echo "VERSION=1.0.0+build.${{ github.run_number }}" >> $GITHUB_ENV

# La metadata se ignora para comparar versiones:
# v1.0.0+build.001 == v1.0.0+build.002 (mismo MAJOR.MINOR.PATCH)
```

**Docker:** usar solo `1.0.0` o `1.0.0-rc.1` - la metadata en tags de Docker genera confusión.

---

## Conventional Commits - la relación con SemVer

Conventional Commits es una convención para mensajes de commit que permite **automatizar SemVer** y generar el CHANGELOG automáticamente.

```
<tipo>[scope opcional]: <descripción corta en imperativo>

[cuerpo opcional — contexto, motivación, decisión]

[footer opcional — BREAKING CHANGE: descripción; Closes #issue]
```

| Tipo de commit | Impacto en SemVer | Descripción |
|---------------|------------------|-------------|
| `fix:` | -> PATCH | Corrección de bug |
| `feat:` | -> MINOR | Feature nueva |
| `feat!:` o `BREAKING CHANGE:` | -> MAJOR | Rompe compatibilidad |
| `refactor:`, `test:`, `docs:`, `ci:`, `chore:` | ninguno | Mantenimiento |
| `perf:` | -> PATCH | Mejora de rendimiento sin cambio de API |

---

## Conventional Commits - ejemplos reales del CRM

```bash
# PATCH — fix de bug
git commit -m "fix(auth): validate refresh token expiry before issuing new access token

The handler was not checking if the refresh token had expired,
allowing tokens past their validity window to generate new access tokens.

Closes #87"

# MINOR — feature nueva
git commit -m "feat(contacts): add POST /api/contacts endpoint with email uniqueness

Implements CreateContact use case. Returns 201 with ContactDto on success,
409 Conflict if email already exists in the same tenant.

Closes #42"

# MAJOR — breaking change
git commit -m "feat(api)!: rename fullName to displayName in ContactDto

BREAKING CHANGE: The 'fullName' field in all contact responses has been
renamed to 'displayName' to align with the design system. Clients must
update their code to use 'displayName'."
```

---

## Conventional Commits - tipos completos

| Tipo | Cuándo usarlo | Ejemplo real |
|------|--------------|-------------|
| `feat` | Feature nueva | `feat(contacts): add search by phone` |
| `fix` | Bug fix | `fix(auth): handle expired JWT gracefully` |
| `refactor` | Refactor sin cambio funcional | `refactor(handler): extract validation logic` |
| `perf` | Mejora de rendimiento | `perf(repo): add composite index on tenant_id` |
| `test` | Agregar o corregir tests | `test(contacts): add integration test for create` |
| `docs` | Solo documentación | `docs(readme): add local setup instructions` |
| `ci` | Cambios al pipeline | `ci: add sonar scan step to PR workflow` |
| `chore` | Mantenimiento sin lógica | `chore: update EF Core from 10.0 to 10.1` |
| `build` | Sistema de build o deps | `build: configure multi-stage Dockerfile` |
| `style` | Formato puro (no lógica) | `style: apply editorconfig formatting` |

---

## Git tags - marcar versiones en el historial

```bash
# Crear tag anotado (recomendado — tiene autor, fecha y mensaje)
git tag -a v0.1.0 -m "Primera entrega funcional — módulos Auth y Users"

# Ver todos los tags
git tag --list

# Ver tags con mensajes
git tag -n

# Empujar un tag específico al remoto
git push origin v0.1.0

# Empujar todos los tags pendientes
git push origin --tags

# Ver el historial de versiones en el log
git log --oneline --decorate

# Checkout de una versión específica (auditoría o hotfix base)
git checkout v0.1.0
git checkout -b hotfix/91/patch-jwt-from-v0.1.0   ← base para hotfix
```

**Regla:** el tag siempre apunta a un commit en `main`. Nunca a un branch de feature.

---

## Historial SemVer - caso real Sistema de mensajería

```
v0.1.0  (Sprint 1)  Login · Registro · Users CRUD — MVP interno
v0.1.1  (Sprint 1)  fix: dominios con guión eran rechazados en validación de email
v0.2.0  (Sprint 2)  feat: módulo Contacts con CRUD y paginación cursor-based
v0.2.1  (Sprint 2)  fix: GET /contacts no aplicaba filtro de tenant correctamente
v0.3.0  (Sprint 3)  feat: módulo Conversations + webhook WhatsApp recepción
v0.3.1  (Sprint 3)  fix: mensajes duplicados cuando WhatsApp reintentaba el webhook
v0.4.0  (Sprint 4)  feat: módulo Messages + upload de adjuntos a S3 pre-signed URLs
v0.5.0  (Sprint 5)  feat: módulo Bots — bot flow builder con React Flow
v1.0.0  (Sprint 8)  MVP en staging con primer cliente real
v1.0.1  (Sprint 8)  fix: race condition en asignación concurrente de conversaciones
v1.1.0  (Sprint 9)  feat: campañas masivas programadas con BackgroundService
v2.0.0  (Sprint 12) feat!: renombrar workspace → organization (breaking en API pública)
```

---

<!-- _class: lead -->

# 7C - Story Points

---

## Story Points vs Horas - por qué las horas mienten

```
Tarea: "agregar login con Google OAuth"

Dev A (nunca lo hizo):    estima 8h → realmente: 3 días
Dev B (lo hizo 3 veces):  estima 2h → realmente: 2 horas

La misma tarea — estimaciones radicalmente distintas.
Las horas miden velocidad individual, no complejidad de la tarea.
```

**Story Points miden tres dimensiones combinadas:**
1. **Esfuerzo** - cuánto trabajo requiere hacer la tarea
2. **Complejidad** - cuántas partes móviles e interdependencias tiene
3. **Incertidumbre** - qué tanto no sabemos sobre cómo implementarlo

Un 8 puede completarse en 4h si el equipo ya lo hizo antes,
o en 2 días si requiere investigación. Los puntos son **consistentes entre sprints**; las horas estimadas individualmente no lo son.

---

## Escala de Fibonacci - por qué no 1 al 10

```
Fibonacci:   1 · 2 · 3 · 5 · 8 · 13 · 21
```

La escala Fibonacci refleja cómo los humanos estimamos incertidumbre:
a mayor tamaño, mayor imprecisión - los saltos crecen exponencialmente.

| Puntos | Significado | Ejemplo: sistema de mensajería |
|--------|-------------|----------------------|
| 1 | Trivial - completamente conocido | Cambiar mensaje de error en validación |
| 2 | Muy pequeño - sin sorpresas | Agregar campo `phone` a ContactDto |
| 3 | Pequeño - patrón ya establecido | GET /contacts/:id (patrón Users ya existe) |
| 5 | Medio - algo de complejidad | CreateContact con validación email único |
| 8 | Grande - múltiples capas o integración nueva | Webhook WhatsApp + procesamiento |
| 13 | Muy grande - dividir si es posible | Módulo Bots completo (demasiado para 1 sprint) |
| 21 | Épica - dividir obligatoriamente | "El CRM completo" no cabe en 1 sprint |

---

## Planning Poker - el proceso anti-sesgo

```
1. Product Owner lee la story en voz alta (no más de 2 minutos)
2. Cada dev elige su carta en SECRETO (app: PlanITPoker, Jira Poker, cartas físicas)
3. A la señal, todos revelan SIMULTÁNEAMENTE:  [ 3 ] [ 5 ] [ 8 ] [ 3 ]
4. Si hay consenso → se registra el punto, siguiente story
5. Si hay divergencia (3 vs 8):
   a. El que dio el MAYOR explica: "vi que hay 3 integraciones externas"
   b. El que dio el MENOR explica: "pensé que el patrón ya está hecho"
   c. Segunda votación — con nueva información
   d. Si hay empate persiste → tomar el mayor (conservadurismo)
```

**¿Por qué la revelación es simultánea?**
El **sesgo de anclaje**: si Dev A dice "5" primero, los demás inconscientemente van hacia ese número.
La simultaneidad fuerza estimaciones independientes.

---

## Planning Poker - caso práctico Sistema de mensajería

```
Story: "Agregar búsqueda de contactos por nombre, email o teléfono"

Ronda 1 de votación:
  Dev A:   5  ("el patrón LIKE en Dapper es sencillo")
  Dev C:   8  ("hay que manejarlo en el frontend también")
  Dev B:   3  ("solo es una query WHERE con LIKE")
  Dev D:   8  ("la búsqueda en tiempo real necesita debounce en React")

Divergencia: 3 vs 8

Discusión: la story incluye el frontend (debounce + UI)
  → Dev B actualiza: "ah, no había visto el frontend, cambio a 8"
  → Dev A actualiza: "coincido en 8"

Ronda 2: [ 8 ] [ 8 ] [ 8 ] [ 8 ] → CONSENSO: 8 puntos
```

---

## Reference Stories - calibrar el equipo al inicio

Antes del primer sprint, el equipo acuerda historias de referencia como ancla:

```
1 punto  → Cambiar el texto del botón "Guardar" a "Crear contacto" en el frontend
3 puntos → Endpoint GET /contacts/:id — patrón Users ya establecido, sin lógica nueva
5 puntos → Endpoint POST /contacts — validación email único, Result Pattern completo
8 puntos → Webhook WhatsApp — integración externa, autenticación con Meta, parsing
13 puntos → Módulo completo de Bots (frontend React Flow + backend) — dividir si es posible
```

**Beneficio:** cuando el equipo dice "es un 5", todos piensan en CreateContact.
Las reference stories son el idioma compartido de estimación.

**Revisar las referencias cada 3-4 sprints** - a medida que el equipo madura, el 5 de hoy puede sentirse como un 3.

---

## Velocity - predecir el futuro con datos reales

```
Sprint 1: 21 puntos completados  ← base inicial
Sprint 2: 18 puntos              ← bug crítico interrumpió 2 días
Sprint 3: 24 puntos              ← equipo en ritmo
Sprint 4: 22 puntos

Velocity promedio (últimos 3 sprints): (18 + 24 + 22) / 3 = 21.3 ≈ 21 pts/sprint
```

**Usos de la velocity:**

```
Backlog restante: 126 puntos
Sprints estimados: 126 / 21 = 6 sprints → ~12 semanas

"¿Cuándo estará el módulo de campañas?" → 21 pts / velocity 21 = 1 sprint
"¿Cabe todo esto en el sprint?" → sprint commitment ≤ velocity histórica
"¿La velocity cayó?" → detectar blocker: deuda técnica, ausencias, scope creep
```

**La velocity es un indicador, no un objetivo.**
Presionar para "subir la velocity" lleva a inflar estimaciones o a dejar DoD incompleto.

---

## Sprint Burndown Chart - cómo leerlo

```
Puntos
restantes
   40 │*
      │ *
   30 │  *      ← velocidad normal
      │   *
   20 │    * *  ← dos días planos: hay un blocker
      │        *
   10 │         *
      │          * *
    0 └─────────────────── Días del sprint
      1  2  3  4  5  6  7  8  9  10

*  = puntos restantes reales
── = línea ideal (bajada lineal perfecta)
```

**Señales de alerta que debes saber leer:**
- Curva plana varios días seguidos -> blocker no resuelto (escalar en Retro)
- Caída rápida al inicio + lenta al final -> se trabaja en lo fácil primero, lo difícil queda al final
- Curva sube en vez de bajar -> se agregaron stories al sprint sin remover otras (scope creep)

---

## Deuda técnica - darle visibilidad con tickets

La deuda técnica es trabajo acumulado que hace más difícil hacer trabajo nuevo.
Si no se trackea, se vuelve invisible y crece indefinidamente.

```markdown
## Tech Debt Backlog — Sistema de mensajería

TD-01 [3 pts] — Agregar índice compuesto en (tenant_id, created_at) en conversations
               Sin índice: el listado de conversaciones ya tarda ~800ms con datos de prueba
               Medido: 2026-05-20 con 500 registros de prueba

TD-02 [5 pts] — Extraer validación inline del ContactHandler a FluentValidation
               La validación está hardcodeada en el Handler — difícil de testear y reutilizar

TD-03 [8 pts] — Agregar tests de integración al módulo Authentication
               Coverage actual: 0%. Un cambio reciente rompió el login sin que los tests lo detectaran

TD-04 [2 pts] — Reemplazar Console.WriteLine en DatabaseInitializationService por ILogger
               Está loguando con Console — no aparece en el sistema de observabilidad
```

**Regla:** dedicar ~20% de la velocity de cada sprint a deuda técnica. (21 pts/sprint -> ~4 pts para TD)

---

<!-- _class: lead -->

# 7D - Documentación del Repositorio

---

## ¿Por qué documentar el repositorio?

```
Escenario real: un nuevo dev entra al equipo el lunes.

Sin documentación:
  Día 1: "¿cómo levanto el proyecto?" → le preguntan a Dev A (30 min perdidos)
  Día 2: "¿cuál es la convención de branches?" → le preguntan a Dev A
  Día 3: "¿por qué usamos Modular Monolith?" → le preguntan a Dev A
  Día 10: Dev A está bloqueado haciendo onboarding en vez de desarrollar

Con documentación:
  README.md → el nuevo sigue los pasos y levanta el proyecto solo en 30 min
  CONTRIBUTING.md → entiende las convenciones sin preguntarle a nadie
  docs/adr/ → entiende POR QUÉ se tomaron las decisiones — y no las revierte sin contexto
```

**La documentación del repo = el onboarding automatizado.**
Cada minuto que tardas en documentar ahorra 10 minutos de interrupción futura.

---

## Anatomía de un repositorio profesional

```
mensajeria/
├── README.md              ← puerta de entrada — lo primero que ven todos
├── CHANGELOG.md           ← historial de cambios por versión
├── CONTRIBUTING.md        ← cómo contribuir al proyecto
├── SECURITY.md            ← cómo reportar vulnerabilidades
├── LICENSE                ← términos de uso del código
├── .editorconfig          ← formato de código consistente y automático
├── .gitignore
├── .github/
│   ├── CODEOWNERS               ← quién revisa automáticamente cada área
│   ├── PULL_REQUEST_TEMPLATE.md ← checklist obligatorio en cada PR
│   └── ISSUE_TEMPLATE/
│       ├── bug_report.md
│       └── feature_request.md
├── docs/
│   ├── adr/               ← Architecture Decision Records
│   ├── architecture.md    ← diagramas C4
│   ├── database.md        ← ER diagram + decisiones de schema
│   ├── deployment.md      ← runbook de despliegue
│   └── api.md             ← endpoints, contratos, ejemplos curl
└── AGENTS.md              ← contexto para AI agents (Claude, Copilot, Cursor)
```

---

## README.md - estructura mínima

```markdown
# Sistema de mensajería

> Sistema CRM conversacional multi-canal.
> WhatsApp · Instagram · Email · Real-time con SignalR.

[![CI](badge_url)](workflow_url)
[![Version](badge_url)]()
[![License: MIT](badge_url)](LICENSE)

## Stack
.NET 10 · EF Core 10 · PostgreSQL 17 · React 19 · Docker · GitHub Actions

## Prerrequisitos
- .NET 10 SDK
- Docker Desktop 4.x+
- Node.js 22+

## Setup local          ← sección más importante
## Variables de entorno ← qué configurar
## Cómo correr tests    ← comando exacto
## Arquitectura         ← enlace a docs/architecture.md
## Contribuir           ← enlace a CONTRIBUTING.md
## Licencia
```

---

## README.md - sección Setup local (ejemplo completo)

```markdown
## Setup local

### 1. Clonar el repositorio
git clone https://github.com/acme/mensajeria.git
cd mensajeria

### 2. Copiar variables de entorno
cp .env.example .env
# Editar .env con tus valores (ver tabla en la sección siguiente)

### 3. Levantar infraestructura (PostgreSQL + Redis)
docker compose up -d db redis

### 4. Aplicar migraciones de base de datos
dotnet ef database update --project src/Host.Api

### 5. Levantar el backend
dotnet run --project src/Host.Api
# API disponible en: http://localhost:5000
# Swagger UI en:     http://localhost:5000/swagger

### 6. Levantar el frontend
cd frontend && npm install && npm run dev
# UI disponible en:  http://localhost:5173

### Verificar que todo funciona
curl http://localhost:5000/health
# → { "status": "Healthy", "version": "0.3.0" }
```

---

## README.md - sección Variables de entorno

```markdown
## Variables de entorno

Copia `.env.example` a `.env` y completa los valores:

| Variable | Descripción | Ejemplo (dev local) |
|----------|-------------|---------------------|
| `ConnectionStrings__Default` | PostgreSQL | `Host=localhost;Database=crm;Username=postgres;Password=postgres` |
| `Jwt__Key` | Clave JWT (mín 32 chars) | `dev-secret-32-chars-minimum!!` |
| `Jwt__Issuer` | Issuer del token | `mensajeria` |
| `Jwt__ExpiresInMinutes` | Vida del access token | `60` |
| `Redis__ConnectionString` | Redis | `localhost:6379` |
| `WhatsApp__WebhookVerifyToken` | Token de verificación Meta | `mi-token-secreto-local` |
| `WhatsApp__AppSecret` | App Secret de Meta | `(de Meta for Developers)` |

> Nunca subas `.env` ni ningún valor real al repositorio.
> `.env` está en `.gitignore`. Para staging/prod: AWS Secrets Manager.
```

---

## README.md - badges de estado del proyecto

```markdown
<!-- CI — verde o rojo visible desde GitHub sin entrar al repo -->
[![CI](https://github.com/acme/mensajeria/workflows/CI/badge.svg)](
  https://github.com/acme/mensajeria/actions)

<!-- Versión actual (se actualiza con cada tag) -->
[![Version](https://img.shields.io/github/v/tag/acme/mensajeria?label=version)](
  https://github.com/acme/mensajeria/releases)

<!-- Cobertura de tests — integración con Codecov -->
[![Coverage](https://codecov.io/gh/acme/mensajeria/branch/main/graph/badge.svg)](
  https://codecov.io/gh/acme/mensajeria)

<!-- Licencia -->
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
```

**¿Para qué sirven los badges?**
Un badge de CI en verde en el README dice "este repo compila y los tests pasan" antes de leer una sola línea de código. Genera confianza instantánea en nuevos colaboradores y clientes.

---

## README.md - antipatrones a evitar

```markdown
No: README vacío:
# Sistema de mensajería
[nada más — GitHub lo genera automáticamente al crear el repo]

No: README desactualizado:
## Setup
dotnet run --project Reddit.WebApi   ← proyecto renombrado hace 3 meses

No: README que asume demasiado:
## Cómo correr
Simplemente ejecutar el proyecto.   ← ¿Con qué IDE? ¿Con qué config?

No: README que documenta el código (para eso existe el código):
## Cómo funciona GetUserProfileHandler
El handler recibe un GetUserProfileRequest y llama a IUserProfileRepository...
```

**Regla de oro:** el README documenta **CÓMO USAR** el proyecto, no cómo está implementado.
Si alguien necesita leer el README para entender el código, el código tiene malos nombres.

---

## CHANGELOG.md - Keep a Changelog

Formato estándar del ecosistema open source: [keepachangelog.com](https://keepachangelog.com/)

```markdown
# Changelog

Todos los cambios notables se documentan aquí.
Formato: [Keep a Changelog](https://keepachangelog.com/es/)
Versionado: [Semantic Versioning](https://semver.org/lang/es/)

## [Sin publicar]
### Agregado
- Módulo de campañas masivas programadas (en desarrollo)

## [0.3.0] — 2026-05-29
### Agregado
- Módulo Conversations: bandeja de entrada, asignación, estados
- Webhook WhatsApp: recepción y procesamiento de mensajes entrantes
### Corregido
- GET /contacts no aplicaba filtro de tenant en queries de búsqueda

## [0.2.0] — 2026-05-15
### Agregado
- Módulo Contacts: CRUD completo con paginación cursor-based
- CreateContact valida unicidad de email por workspace (409 Conflict)

## [0.1.0] — 2026-05-01
### Agregado
- Login con JWT HS256 + refresh token rotativo
- Registro y configuración de workspace
- CRUD de agentes (Users)
```

---

## CHANGELOG.md - secciones y a quién va dirigido

| Sección | Qué va aquí | Ejemplo |
|---------|------------|---------|
| **Agregado** | Features nuevas | "Búsqueda de contactos por nombre, email o teléfono" |
| **Cambiado** | Cambios en funcionalidad existente | "La paginación ahora usa cursor-based en lugar de offset" |
| **Deprecado** | Features que se eliminarán en próxima versión | "El campo `workspace_id` será reemplazado por `tenant_id` en v2.0" |
| **Eliminado** | Features removidas (breaking) | "Eliminado endpoint GET /api/v1/users (migrar a /api/users)" |
| **Corregido** | Bug fixes | "Corregido: mensajes duplicados al reintentar webhook" |
| **Seguridad** | Vulnerabilidades corregidas | "Corregida validación de firma en webhook de WhatsApp" |

**El CHANGELOG es para usuarios y clientes - no para developers.**

```
No: "Refactorizar UserProfileRepository para usar ExecuteUpdateAsync"
Sí: "Mejorado: actualizar perfil de usuario es ahora 3x más rápido"
```

---

## CONTRIBUTING.md - el contrato del equipo

```markdown
# Cómo contribuir a Sistema de mensajería

## Antes de empezar
- Lee el README completo
- Revisa los issues abiertos — puede que alguien ya esté trabajando en lo mismo
- Si es una feature nueva, abre un issue para discutirla antes de codificar

## Branches
Formato: `tipo/numero-issue/descripcion-corta-en-kebab`
  feature/42/add-contact-search
  bugfix/87/fix-tenant-filter-in-conversations
  hotfix/91/patch-jwt-expiry-validation
  chore/15/update-ef-core-to-10-1

## Conventional Commits (obligatorio)
  feat(contacts): add search by phone number
  fix(auth): handle expired refresh token gracefully
  chore: update EF Core from 10.0 to 10.1

## Pull Requests
1. Abrir PR contra main con el template COMPLETAMENTE llenado
2. Agregar "Closes #42" para link automático al issue
3. Esperar review de al menos 1 miembro del equipo (< 4h en horario laboral)
4. El pipeline debe estar verde antes de mergear
```

---

## SECURITY.md - reportar vulnerabilidades

```markdown
# Política de Seguridad

## Versiones con soporte activo

| Versión | Soporte |
|---------|---------|
| 1.x.x   | Sí — recibe parches de seguridad |
| 0.x.x   | No — actualizar a v1.0.0 |

## Cómo reportar una vulnerabilidad

NO abras un issue público para vulnerabilidades de seguridad.
Un issue público da ventaja a actores maliciosos antes de que se libere el parche.

**Proceso:**
1. Enviar email a: security@example.com
   - Asunto: [SECURITY] Descripción breve del problema
   - Incluir: pasos para reproducir · impacto potencial · versión afectada
2. Recibirás respuesta en < 48 horas hábiles
3. Si se confirma: publicaremos un GitHub Security Advisory y liberaremos un parche en < 7 días
4. Reconocimiento público en el CHANGELOG (si el reportante lo desea)
```

---

## .github/PULL_REQUEST_TEMPLATE.md

```markdown
## ¿Qué hace este PR?

<!-- Describe los cambios en 2-3 líneas -->

## Tipo de cambio
- [ ] Feature nueva (`feat`)
- [ ] Bug fix (`fix`)
- [ ] Refactor
- [ ] Documentación
- [ ] Infraestructura / CI

## Issue relacionado
Closes #___

## Cómo testear

<!-- Pasos concretos — copiar/pegar debe funcionar -->
1. `POST /api/contacts` con body `{"fullName": "Ana", "email": "ana@example.com", "tenantId": 1}`
2. Verificar respuesta 201 con ContactDto
3. Repetir el POST → verificar 409 Conflict

## Checklist
- [ ] Los tests pasan localmente (`dotnet test`)
- [ ] Usé Conventional Commits en todos los commits
- [ ] Actualicé CHANGELOG.md si aplica
- [ ] No hay secrets, passwords ni API keys en el código
- [ ] El tenantId se valida en el Handler y en el Repository
```

---

## .github/ISSUE_TEMPLATE - bug_report.md

```markdown
---
name: Bug Report
about: Reportar un comportamiento incorrecto
labels: bug
assignees: ''
---

## Descripción del bug
<!-- Descripción clara y concisa del comportamiento incorrecto -->

## Pasos para reproducir
1. Llamar `POST /api/contacts` con body `{"email": "test@example.com"}`
2. Llamar el mismo endpoint con el mismo email desde el mismo workspace
3. Observar que devuelve 500 en lugar de 409

## Comportamiento esperado
409 Conflict con mensaje "El email ya está registrado en este workspace"

## Comportamiento actual
500 Internal Server Error — stack trace incluido a continuación

## Ambiente
- Versión del proyecto: v0.3.0
- Ambiente: staging / local
- OS del servidor: Ubuntu 22.04 (Docker)

## Logs / Stack trace
```

---

## .github/ISSUE_TEMPLATE - feature_request.md

```markdown
---
name: Feature Request
about: Proponer una nueva funcionalidad
labels: enhancement
---

## Problema que esta feature resolvería
<!-- ¿Qué no puede hacer hoy el usuario que necesita hacer? -->
Como agente, cuando busco un contacto por su nombre tengo que scrollear
manualmente por toda la lista de contactos, lo que toma mucho tiempo
cuando hay más de 200 contactos en el workspace.

## Solución propuesta
Agregar un campo de búsqueda en la vista de Contactos que filtre en
tiempo real (con debounce de 300ms) por nombre, email o teléfono.

## Alternativas consideradas
- Filtrar en el frontend con los datos ya cargados:
  no escala cuando hay miles de contactos.
- Usar ElasticSearch:
  demasiado costoso para el MVP — PostgreSQL FTS es suficiente.

## Contexto adicional
Mockup de la UI: [enlace a Figma]
Issue relacionado: #35 (búsqueda full-text en conversaciones)
```

---

## .github/CODEOWNERS - quién revisa qué automáticamente

```
# CODEOWNERS — GitHub asigna reviewers automáticamente según la ruta del archivo
# Formato: glob_pattern   @usuario

# Regla base: Dev A revisa todo lo que no tenga regla específica
*                                    @dev-a

# Módulo Authentication — Dev B como especialista
/src/Shared/Authentication/          @dev-b

# Frontend — Dev C y Dev D
/frontend/                           @dev-c @dev-d

# Infrastructure (repositorios, queries SQL) — Dev A como arquitecto
/src/Modules/*/Infrastructure/       @dev-a

# CI/CD y Docker — Dev A
/.github/                            @dev-a
/docker/                             @dev-a

# Documentación — cualquier miembro puede modificar sin review forzado
/docs/                               @acme/team
README.md                            @acme/team
```

**Efecto:** un PR que toca `/frontend/` tiene a `@dev-c` y `@dev-d` asignados automáticamente como reviewers - sin tener que acordarse de mencionarlos.

---

## /docs/ - la carpeta de conocimiento del equipo

```
docs/
├── adr/
│   ├── ADR-001-stack-y-arquitectura.md         ← por qué .NET 10 y no Go
│   ├── ADR-002-multitenancy-pool-model.md      ← DB compartida con tenant_id
│   ├── ADR-003-whatsapp-webhook-vs-polling.md  ← webhook es más eficiente
│   └── ADR-004-signalr-vs-sse-para-realtime.md ← SignalR elegido por bidireccional
├── architecture.md   ← diagramas C4 Nivel 1 (System) y Nivel 2 (Container)
├── database.md       ← ER diagram + decisiones de schema (tipos, índices, soft delete)
├── deployment.md     ← runbook de despliegue a staging y producción
└── api.md            ← endpoints, request/response, ejemplos curl completos
```

**Regla crítica:** los docs viven en el repo junto al código - no en Notion, no en Confluence, no en Google Docs.
Cuando el código cambia, el doc se actualiza en el mismo PR.
Si el doc vive en otra herramienta, se desactualiza en 2 semanas.

---

## ADR - Architecture Decision Record

Un ADR documenta **UNA** decisión de arquitectura: qué se decidió, por qué, y las consecuencias.

```markdown
# ADR-001 — Arquitectura Monolito Modular

**Fecha:** 2026-05-01  ·  **Estado:** Aceptado

## Contexto
La empresa tiene un equipo de 4 developers. El sistema de mensajería no tiene
usuarios reales todavía. La velocidad de iteración es la prioridad.

## Opciones consideradas
1. Monolito Modular con Clean Architecture ← elección
2. Microservicios
3. Monolito tradicional (sin límites de módulo)

## Decisión
Monolito Modular: cada módulo tiene sus propias capas Domain/Application/
Infrastructure/Presentation, pero se despliegan juntas en un solo proceso.

## Consecuencias
Sí: Un solo deployment · un solo DB · sin latencia de red entre módulos
Sí: Los módulos pueden extraerse como microservicios cuando el equipo crezca
No: Los límites de módulo deben mantenerse con disciplina — no hay enforcement técnico
```

---

## ADR - ciclo de vida de una decisión

```markdown
**Estado: Propuesto**
   ↓ el equipo discute pros/contras
**Estado: Aceptado**
   ↓ se implementa · el tiempo pasa · el contexto cambia
**Estado: Deprecado**        ← la decisión ya no aplica pero no fue reemplazada
   o
**Estado: Supersedido por ADR-007**  ← se tomó una mejor decisión documentada
```

**Regla de oro:** los ADRs **nunca se borran** - son historia permanente del proyecto.
Si la decisión cambió, crea un nuevo ADR que diga "supersede al ADR-001" y explica por qué cambió el contexto.

**¿Cuándo crear un ADR?**
- Decisión de tecnología o framework
- Decisión de arquitectura (patrón, modelo de datos, multi-tenancy)
- Decisión de proceso con impacto técnico (SemVer, branching strategy)
- Decisión que alguien podría revertir sin el contexto de por qué se tomó

---

## ADR-002 - ejemplo: Multi-tenancy Pool Model

```markdown
# ADR-002 — Modelo de Multi-tenancy: Pool (DB compartida con tenant_id)

**Fecha:** 2026-05-08  ·  **Estado:** Aceptado

## Contexto
El CRM sirve a múltiples empresas (workspaces/tenants). Se necesita
aislar los datos entre clientes sin multiplicar costos operativos.

## Opciones
| Modelo | Descripción | Costo | Aislamiento |
|--------|-------------|-------|-------------|
| Silo | Una DB por tenant | Alto (1 DB/cliente) | Máximo |
| Pool | DB compartida, filtro por tenant_id | Bajo | Bueno (con disciplina) |
| Bridge | Schema por tenant en la misma DB | Medio | Bueno |

## Decisión: Pool
Todos los tenants comparten la DB. Cada tabla tiene `tenant_id bigint NOT NULL`.
EF Core Global Query Filter garantiza que ninguna query devuelva datos de otro tenant.

## Consecuencias
Sí: Bajo costo operativo · una sola migración aplica a todos los tenants
No: Un bug en el filtro de tenant podría exponer datos → tests de leakage son obligatorios
```

---

## ADR-003 - ejemplo: WhatsApp Webhook vs Polling

```markdown
# ADR-003 — WhatsApp: Webhook vs Polling para mensajes entrantes

**Fecha:** 2026-05-15  ·  **Estado:** Aceptado

## Contexto
El CRM necesita recibir mensajes de WhatsApp en tiempo real para mostrarlos
en la bandeja de conversaciones.

## Opciones

**Polling:** el CRM consulta la API de Meta cada N segundos para ver si hay mensajes
- Sí: Simple de implementar
- No: Latencia de hasta N segundos · Meta limita el polling agresivo

**Webhook:** Meta envía un HTTP POST al CRM cuando llega un mensaje
- Sí: Latencia < 1 segundo · no hay rate limiting de Meta
- No: Requiere URL pública · requiere verificación de firma HMAC

## Decisión: Webhook
La latencia < 1 segundo es un requisito del producto (chat en tiempo real).
La verificación de firma HMAC se implementa en el middleware de webhook.

## Consecuencias
Sí: Mensajes en tiempo real
No: Se necesita URL pública en desarrollo — usar ngrok localmente para pruebas
```

---

## docs/architecture.md - C4 Nivel 1 (System Context)

```
## Sistema de mensajería — Vista de contexto (Nivel 1)

┌─────────────────────────────────────────────────────────────┐
│                         EMPRESA                             │
│                                                             │
│  ┌──────────┐         ┌─────────────────────┐              │
│  │  Agente  │─usa────→│    Sistema de mensajería     │              │
│  └──────────┘         │                     │              │
│  ┌──────────┐         │  .NET 10 API        │              │
│  │  Admin   │─usa────→│  React 19 UI        │              │
│  └──────────┘         │  PostgreSQL 17       │              │
│                       │  Redis 7             │              │
│                       └─────────────────────┘              │
│                                  │                          │
│              ┌───────────────────┼──────────────────┐      │
│              ↓                   ↓                   ↓      │
│       WhatsApp             Instagram              Email     │
│       Business API         Graph API              SMTP      │
│       (Meta)               (Meta)                 (SendGrid)│
└─────────────────────────────────────────────────────────────┘
```

---

## docs/architecture.md - C4 Nivel 2 (Containers)

```
## Vista de Contenedores (Nivel 2)

┌─ React 19 SPA ─────┐  HTTPS  ┌─ .NET 10 Web API ────────────────────────┐
│ Vite · Tailwind v4  │────────→│ Modular Monolith                          │
│ TanStack Query      │  WS     │ ├── Auth (Login·Register·Refresh)         │
│ SignalR client      │────────→│ ├── Users (Agentes del workspace)         │
└────────────────────┘         │ ├── Contacts (CRUD·búsqueda·etiquetas)    │
                                │ ├── Conversations (bandeja·asignación)    │
                                │ ├── Messages (envío·recepción·adjuntos)   │
                                │ ├── Channels (WhatsApp·Instagram·Email)   │
                                │ └── Bots (flujos·triggers·acciones)       │
                                └──────────────────────────────────────────┘
                                        │                    │
                                        ↓                    ↓
                                  PostgreSQL 17          Redis 7
                                  (datos primarios)      (cache + SignalR backplane)
```

---

## docs/database.md - ER Diagram y decisiones de schema

```markdown
## Relaciones principales

tenants (1) ──────── (N) users
tenants (1) ──────── (N) contacts
tenants (1) ──────── (N) channels
tenants (1) ──────── (N) conversations

conversations (1) ── (N) messages
conversations (N) ── (1) contacts   (el contacto que inició la conv.)
conversations (N) ── (1) channels   (por qué canal llegó)
conversations (N) ── (1) users      (agente asignado, nullable)

## Decisiones de schema
- IDs primarios: bigint con GENERATED ALWAYS AS IDENTITY (no UUID — más rápido en índices)
- IDs públicos en la API: uuid (expuesto en URLs, no revela secuencia)
- Soft delete: columna is_active boolean NOT NULL DEFAULT true (no DELETE físico)
- Timestamps: created_at y updated_at con DEFAULT NOW() en PostgreSQL
- Índices: (tenant_id, created_at DESC) en todas las tablas de alto volumen
- Strings: varchar con límite explícito (no text sin límite en columnas de usuario)
```

---

## docs/deployment.md - Runbook de despliegue

```markdown
# Runbook de Despliegue

## Staging — automático
Todo merge a `main` dispara el pipeline CI/CD.
Despliegue automático a staging sin intervención manual.
URL: https://staging.app.example.com

## Producción — semi-manual (requiere tag)
1. Verificar que staging esté verde (pruebas manuales + pipeline)
2. Crear y empujar el tag de versión:
   git tag -a v1.2.0 -m "Sprint 5 — módulo Bots"
   git push origin v1.2.0
3. El pipeline de prod se dispara al detectar el tag
4. Verificar en producción:
   curl https://app.example.com/health → { "status": "Healthy", "version": "1.2.0" }

## Rollback de emergencia
1. Identificar la última versión estable: git tag --list | sort -V | tail -5
2. Crear nuevo tag sobre esa versión: git tag -a v1.1.4 v1.1.3^{} -m "Rollback de v1.2.0"
3. Push del tag → el pipeline despliega la versión anterior automáticamente
4. Investigar el bug en un branch de hotfix
```

---

## development-rules.md - naming y convenciones

```markdown
# Reglas de Desarrollo — Sistema de mensajería

## Naming
- Clases:          PascalCase           (ContactHandler, UserProfileDto)
- Variables:       camelCase            (tenantId, fullName, contactList)
- Constantes:      PascalCase en const  (MaxPageSize = 100)
- Tablas DB:       snake_case           (contact_emails, conversation_messages)
- Columnas DB:     snake_case           (tenant_id, created_at, is_active)
- Endpoints REST:  kebab-case           (/api/contact-groups, /api/bot-flows)
- Branches:        kebab-case           (feature/42/add-contact-search)

## Arquitectura — reglas no negociables
- Interfaces en Domain — NUNCA en Application ni Infrastructure
- Handlers no conocen Infrastructure — solo consumen interfaces de Domain
- El tenantId siempre viaja explícito en el Request — no en variables globales
- Nunca usar IRepository<T> genérico — interfaces específicas por entidad
- Nunca `:latest` como tag de Docker en staging ni producción
```

---

## development-rules.md - qué NO va aquí

```markdown
## Prohibido en este proyecto
- async void        → usar async Task
- .Result / .Wait() → usar await en código ASP.NET (riesgo de deadlock)
- string.Format     → usar interpolación $""
- Console.WriteLine → usar ILogger (el log no aparece en observabilidad)
- Commit directo a main → siempre feature branch + PR
- IRepository<T> genérico → ver ADR-005

## ¿Qué NO va en development-rules.md?
No: Decisiones de arquitectura (eso va en /docs/adr/)
   "Usamos Modular Monolith porque..." → ADR-001

No: Lo que el código ya expresa con nombres claros
   "El método GetByPublicIdAsync recibe un Guid" → obvia redundancia

No: Lo que .editorconfig ya enforcea automáticamente
   "Usar 4 espacios en C#" → .editorconfig lo aplica en el editor

Sí: Restricciones no obvias específicas del proyecto
   "No cargar el tenant desde la DB en cada request — usar el claim del JWT"

Sí: Antipatrones que el equipo acordó explícitamente evitar
   "No usar IRepository<T> genérico — ver ADR-005 para el contexto"
```

---

## .editorconfig - formato automático sin peleas

```ini
# .editorconfig — se aplica automáticamente en VS, Rider y VS Code
root = true

[*]
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
charset = utf-8

[*.cs]
indent_style = space
indent_size = 4
dotnet_sort_system_directives_first = true
csharp_new_line_before_open_brace = all
csharp_prefer_simple_using_statement = true:suggestion
dotnet_naming_rule.private_fields_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_underscore.style = underscore_prefix

[*.{json,yaml,yml}]
indent_style = space
indent_size = 2

[*.md]
trim_trailing_whitespace = false   # espacios al final son saltos de línea en Markdown
```

**Ventaja:** el CI puede verificar formato con `dotnet format --verify-no-changes`.
Si alguien sube código mal formateado, el pipeline falla automáticamente - sin debates en el PR.

---

## AGENTS.md - contexto para AI agents

```markdown
# AGENTS.md — Sistema de mensajería

## Qué es este proyecto
CRM conversacional multi-canal (WhatsApp, Instagram, Email).
Stack: .NET 10 · EF Core 10 · React 19 · PostgreSQL 17 · Docker · GitHub Actions · AWS

## Arquitectura
Modular Monolith con Clean Architecture por módulo.
Módulos: Auth · Users · Contacts · Conversations · Messages · Bots · Channels

## Patrones obligatorios
- Result Pattern: usar Result.OK<T>() / Fail<T>() — no lanzar excepciones en Handlers
- Repository: interfaces en Domain/{Módulo}/Repositories/, implementación en Infrastructure/
- Multi-tenancy: tenantId siempre en el Request — viene del JWT via CurrentTenantId
- Conventional Commits: feat/fix/chore/refactor/test/docs/ci/perf

## Prohibido en este proyecto
- IRepository<T> genérico
- async void
- .Result / .Wait() en código ASP.NET
- Tag :latest de Docker en staging/producción
- Modificar el back-template directamente (es solo referencia)

## Dónde encontrar las cosas
- Módulos: src/Modules/{NombreModulo}/
- Common library: src/Common/Common/
- Punto de entrada: src/Host.Api/Program.cs
- Tests: src/Modules/{NombreModulo}/{NombreModulo}.Tests/
```

---

## TODO.md - deuda técnica con visibilidad

```markdown
# TODO — Sistema de mensajería

## En progreso (Sprint 6)
- [ ] Módulo de campañas masivas — Dev C (feature/55/bulk-campaigns)
- [ ] Tests de integración para módulo Auth — Dev B (feature/61/auth-integration-tests)

## Deuda técnica pendiente (backlog)
- [ ] TD-01 [3 pts] — Índice compuesto en (tenant_id, created_at) en conversations
      Sin índice: listado tarda ~800ms con 500 registros de prueba (medido 2026-05-20)
- [ ] TD-02 [5 pts] — Migrar validación inline del ContactHandler a FluentValidation
      La validación está hardcodeada — difícil de testear independientemente
- [ ] TD-03 [8 pts] — Tests de integración para módulo Authentication
      Coverage: 0%. Un cambio reciente rompió login sin que los tests lo detectaran
- [ ] TD-04 [2 pts] — Reemplazar Console.WriteLine en DatabaseInitializationService

## Ideas futuras (no comprometidas, sin sprint asignado)
- Exportar historial de conversaciones a PDF
- Integración con Slack para notificaciones internas al equipo
- Soporte multi-idioma (i18n) en el frontend
- Modo oscuro en la UI
```

---

## Checklist - ¿qué documentos necesita tu repo?

| Documento | Cuándo es obligatorio |
|-----------|----------------------|
| `README.md` | Siempre - desde el día 1 |
| `CHANGELOG.md` | Proyectos con versiones/releases (siempre en el CRM) |
| `CONTRIBUTING.md` | Equipos de > 1 persona (siempre en el CRM) |
| `SECURITY.md` | APIs públicas o con datos de clientes (siempre en el CRM) |
| `.github/PR_TEMPLATE` | Equipos de > 1 persona (siempre en el CRM) |
| `.github/ISSUE_TEMPLATE` | Proyectos que reciben reportes de bugs externos |
| `.github/CODEOWNERS` | Equipos con áreas de especialidad definidas |
| `/docs/adr/` | Siempre que se toman decisiones arquitectónicas importantes |
| `development-rules.md` | Equipos de > 1 persona con convenciones acordadas |
| `AGENTS.md` | Siempre que el equipo use AI agents (Claude, Copilot, Cursor) |
| `.editorconfig` | Siempre - desde el primer commit |

---

## Resumen - Parte 7

| Sub-sección | El concepto más importante |
|-------------|---------------------------|
| **SDLC - Sprints** | El sprint es un ciclo completo: Plan -> Build -> Review -> Retro |
| **Ceremonies** | DoD acordado por el equipo; Planning Poker simultáneo para evitar sesgo |
| **User Stories** | INVEST - Gherkin - criterios de aceptación testeable con curl/Postman |
| **Branches - PRs** | `tipo/issue/descripcion` - template lleno - pipeline verde es requisito |
| **SemVer** | MAJOR.MINOR.PATCH - Conventional Commits automatiza el bump |
| **Pre-releases** | alpha -> beta -> rc -> release - no se instalan automáticamente |
| **Story Points** | Fibonacci - mide esfuerzo+complejidad+incertidumbre - velocity predice |
| **Deuda técnica** | Hacer visible como tickets - 20% de cada sprint para pagarla |
| **README** | Setup local completo - badges - documenta el cómo usar, no el cómo funciona |
| **CHANGELOG** | Keep a Changelog - para usuarios - Aggregado/Cambiado/Corregido |
| **CONTRIBUTING** | Branches - commits - PR process - review SLA de 4h |
| **ADRs** | Una decisión por doc - nunca borrar - estado explícito |
| **AGENTS.md** | Contexto del proyecto para que el AI no cometa errores del equipo |
| **.editorconfig** | Formato automático - el CI lo verifica - sin peleas de estilo |

---

<!-- _class: lead -->

# Parte 8 - Escalabilidad

## Vertical - Horizontal - Microservicios - Regiones - Rate Limiting

> ¿Cuándo deja de funcionar un sistema que nadie diseñó para crecer?
> **Hoy.** - el día en que el primer cliente real llega.

---

## ¿Qué es escalabilidad?

**Escalabilidad** = la capacidad de un sistema de manejar más carga sin degradar la experiencia del usuario.

| Dimensión | Pregunta que responde |
|-----------|----------------------|
| **Throughput** | ¿Cuántas peticiones por segundo puede procesar? |
| **Latencia** | ¿Cuánto tiempo tarda en responder bajo carga? |
| **Disponibilidad** | ¿Qué porcentaje del tiempo está funcionando? |
| **Durabilidad** | ¿Se pierden datos si falla un servidor? |

> Un sistema que responde 200 req/s con 50ms de latencia pero
> que cae a 2 000 ms bajo 2 000 req/s tiene un problema de escalabilidad - no de código.

---

## El mito de "lo arreglamos después"

```
Semana 1:  10 usuarios   → todo bien
Semana 8:  500 usuarios  → lento a veces
Semana 12: 2000 usuarios → caídas diarias
Semana 14: "hay que refactorizar todo"
```

**El costo de escalar tarde:**

| Momento | Costo relativo |
|---------|---------------|
| Durante diseño inicial | 1× |
| Durante desarrollo | 6× |
| En producción con usuarios | 60× |

> No necesitas escalar a Google desde el día 1.
> Necesitas tomar decisiones que no te encierren.

---

<!-- _class: lead -->

## 8A - Escalabilidad Vertical vs Horizontal

---

## Escalabilidad Vertical (Scale Up)

**Definición:** añadir más recursos al mismo servidor.

```
Antes:  1 servidor · 4 vCPU · 8 GB RAM
Después: 1 servidor · 32 vCPU · 128 GB RAM
```

**Ventajas:**
- Sin cambios en el código
- Sin problemas de estado compartido
- Simple de operar

**Desventajas:**
- Límite físico: no hay servidor infinito
- Single point of failure - si cae el servidor, cae todo
- El downtime para upgradear puede ser inevitable
- Costo no lineal: el hardware top-of-line es desproporcionadamente caro

> La base de datos suele escalar verticalmente primero.
> La API debería poder escalar horizontalmente desde el día 1.

---

## Escalabilidad Horizontal (Scale Out)

**Definición:** añadir más instancias del mismo servidor.

```
Antes:  1 instancia  · 4 vCPU · 8 GB RAM
Después: 10 instancias · 4 vCPU · 8 GB RAM · balanceador al frente
```

**Ventajas:**
- Sin límite teórico - agrega instancias según demanda
- Alta disponibilidad: si una falla, las otras siguen
- Escala automática (auto-scaling en AWS/Azure)
- Instancias baratas y reemplazables

**Desventajas:**
- Requiere que la aplicación sea **stateless**
- El balanceador de carga es un nuevo componente a operar
- Los recursos compartidos (DB, cache, sesiones) se vuelven cuellos de botella

---

## Comparativa: ¿cuándo usar cada una?

| Criterio | Vertical | Horizontal |
|----------|----------|------------|
| Código existente no stateless | sin cambios | refactorizar primero |
| Alta disponibilidad requerida | single point of failure | N+1 siempre disponible |
| Base de datos (OLTP) | primero aquí | sharding es complejo |
| API sin estado (REST/gRPC) | funciona pero desperdicia | ideal |
| Costo en picos cortos | pagas 24/7 | auto-scaling |
| Tiempo para implementar | minutos | días (infra + código) |

**Estrategia típica para el sistema de mensajería:**
- Base de datos -> vertical primero, réplicas de lectura después
- API (.NET) -> horizontal desde el despliegue inicial
- Redis -> cluster horizontal cuando sea necesario

---

## Requisito número 1 para escalar horizontalmente: Stateless

Una aplicación **stateless** no guarda estado en memoria entre peticiones.

```csharp
// Mal: Con estado en memoria — NO escala horizontalmente
public class ConversationController : ControllerBase
{
    private static readonly List<Conversation> _cache = new(); // estado en RAM

    [HttpGet]
    public IActionResult GetAll() => Ok(_cache); // solo funciona en ESTA instancia
}
```

```csharp
// Bien: Stateless — escala horizontalmente
public class ConversationController : ControllerBase
{
    private readonly IConversationRepository _repo;
    private readonly IMemoryCache _cache; // ← Redis en producción, no in-process

    public ConversationController(IConversationRepository repo, IMemoryCache cache)
    {
        _repo = repo;
        _cache = cache;
    }
}
```

> Todo estado que necesita vivir entre peticiones va a la DB o a Redis.
> La instancia de la API es desechable y reemplazable.

---

## Load Balancer - el distribuidor de tráfico

El **Load Balancer** recibe todas las peticiones y las distribuye entre las instancias disponibles.

```
Internet
    ↓
Load Balancer (NGINX · AWS ALB · Azure Load Balancer)
    ├── Instancia 1 (192.0.2.10:5000)
    ├── Instancia 2 (192.0.2.11:5000)
    └── Instancia 3 (192.0.2.12:5000)
```

**Algoritmos de distribución:**

| Algoritmo | Funcionamiento | Cuándo usarlo |
|-----------|---------------|--------------|
| **Round Robin** | Turno rotativo | Peticiones del mismo peso |
| **Least Connections** | A la instancia menos ocupada | Peticiones de duración variable |
| **IP Hash** | Mismo cliente -> misma instancia | Cuando necesitas "sticky sessions" |
| **Weighted** | % de tráfico por instancia | Blue/Green deployments |

> Para el sistema de mensajería: Round Robin + health checks activos.
> Si una instancia falla el health check, el LB la retira automáticamente.

---

## El problema del estado: sesiones HTTP

**Problema:** el usuario inicia sesión en la Instancia 1.
La siguiente petición llega a la Instancia 2. ¿Está logueado?

```
No: Solución naive: Sticky Sessions (IP Hash)
   - El usuario siempre va a la misma instancia
   - Si esa instancia falla, pierde la sesión
   - No distribuyes carga de forma pareja

Sí: Solución correcta: JWT + stateless auth
   - El token JWT se valida en cualquier instancia (la clave es la misma)
   - No hay sesión que guardar — la firma del token es la prueba
   - Cualquier instancia puede procesar cualquier petición

Sí: Alternativa: Redis Session Store
   - La sesión se guarda en Redis (compartido)
   - Todas las instancias leen del mismo Redis
   - Útil si el cliente no puede usar JWT (aplicaciones legacy)
```

> El back-template usa JWT HS256. La clave está en IOptions<JwtSettings>.
> Cualquier instancia puede validar el token sin llamar a un servicio central.

---

## Réplicas de lectura - escalar la base de datos

**El problema:** la DB es el cuello de botella más común.

```
100% del tráfico (lecturas + escrituras)
    ↓
1 servidor PostgreSQL
    ↓
CPU: 95% · I/O: saturado · latencia: 800ms
```

**Solución: réplicas de lectura (Read Replicas)**

```
Escrituras (INSERT/UPDATE/DELETE)
    → Primary PostgreSQL

Lecturas (SELECT)
    → Replica 1 · Replica 2 · Replica 3 (replicación asíncrona ~50ms lag)
```

**En el sistema de mensajería:**
- Consultas del historial de mensajes -> réplica de lectura
- Escritura de un nuevo mensaje -> primary
- El connection string cambia según el tipo de operación

---

## Auto-scaling - escalar según demanda real

**Auto-scaling:** el proveedor cloud agrega/retira instancias automáticamente según métricas.

```yaml
# AWS Auto Scaling Group (concepto)
min_instances: 2      # mínimo para alta disponibilidad
max_instances: 20     # límite de costo
scale_out_rule:       # agregar instancia cuando...
  metric: CPU         # CPU promedio del grupo
  threshold: 70%      # supera 70% por 3 minutos
scale_in_rule:        # retirar instancia cuando...
  metric: CPU
  threshold: 30%      # baja de 30% por 10 minutos
```

**Métricas comunes de auto-scaling:**

| Métrica | Cuándo usarla |
|---------|--------------|
| CPU promedio | Carga de cómputo (transformaciones, validaciones) |
| Request count | Tráfico HTTP puro |
| Queue length | Procesamiento de trabajos en segundo plano |
| Memory usage | Aplicaciones con mucho caching en RAM |

> Con auto-scaling, pagas solo por lo que usas.
> El reto: el arranque de una instancia tarda 30-90 segundos -> warm-up.

---

<!-- _class: lead -->

## 8B - Microservicios

---

## ¿Qué es un microservicio?

Un **microservicio** es una unidad de despliegue independiente que:

- Tiene un único dominio de responsabilidad (bounded context)
- Se despliega, escala y versiona de forma independiente
- Se comunica con otros servicios vía red (HTTP, gRPC, mensajes)
- Tiene su propia base de datos (database per service)

```
Monolito Modular (back-template)    vs    Microservicios
───────────────────────────────────────────────────────
1 proceso · 1 base de datos              N procesos · N bases de datos
Módulos en el mismo binario              Cada módulo es un servicio separado
Comunicación en memoria (Mediator)       Comunicación por red (latencia + fallas)
1 repositorio · 1 deploy                 N repositorios · N pipelines · N deploys
```

> El 90% de los equipos que eligen microservicios
> lo hacen porque suena moderno - y terminan con distributed monolith.

---

## Microservicios: las ventajas reales

**1. Escala independiente por dominio**

```
Módulo Mensajes    → 20 instancias (mucho tráfico en tiempo real)
Módulo Contactos   → 2 instancias (acceso esporádico)
Módulo Bots        → 5 instancias (picos al ejecutar campañas)
```

**2. Despliegue independiente**

```
Equipo A publica Módulo Mensajes v2.3.0
    → sin afectar Módulo Contactos
    → sin downtime del sistema completo
```

**3. Libertad tecnológica por servicio**

```
Mensajes → Go (alta concurrencia, baja latencia)
Bots     → Python (librerías de ML disponibles)
API Core → .NET 10
```

**4. Aislamiento de fallos**

```
Módulo Reportes cae → Módulo Mensajes sigue funcionando
```

---

## Microservicios: las desventajas reales

**El problema que nadie te cuenta:**

```
Monolito: A llama a B en memoria → 0ms · 0 fallos de red · 1 transacción

Microservicios: A llama a B por HTTP → 5-50ms + puede fallar + sin transacción distribuida
```

| Complejidad añadida | Impacto real |
|--------------------|-------------|
| Service discovery | ¿Cómo sabe A dónde está B? |
| Distributed tracing | ¿Cómo debuggeas un error que cruza 5 servicios? |
| Consistencia eventual | No hay ACID entre servicios - hay Sagas |
| CI/CD por servicio | 10 pipelines en lugar de 1 |
| Contratos de API | Versionado entre servicios es un problema real |
| Tests de integración | Cada servicio necesita mocks de los demás |
| Network partitions | ¿Qué pasa si el servicio B está caído cuando A lo llama? |

> Distributes systems are hard. - Werner Vogels (CTO de Amazon)
> El equipo que construyó los microservicios de Amazon tenía cientos de ingenieros.

---

## Monolito Modular vs Microservicios - tabla honesta

| Criterio | Monolito Modular | Microservicios |
|----------|-----------------|---------------|
| Tamaño del equipo | 1-20 personas | 50+ personas (un equipo por servicio) |
| Velocidad inicial | alta | lenta (infra + contracts primero) |
| Escala por dominio | todo o nada | granular |
| Debugging | un stack trace | distributed tracing necesario |
| Despliegue | 1 pipeline | N pipelines |
| Consistencia de datos | ACID en 1 DB | Eventual consistency |
| Costo infra | bajo | alto (N clusters, N DBs) |
| Skill requerido | Intermedio | Senior + DevOps fuerte |

**Para la empresa hoy:**

> Monolito Modular con Clean Architecture. Un equipo pequeño.
> Si algún módulo necesita escalar de forma muy diferente a los demás
> en el futuro, se extrae como microservicio - **cuando el problema existe**.

---

## El Distributed Monolith - el peor de los dos mundos

**Distributed Monolith:** servicios que parecen microservicios pero están fuertemente acoplados.

```
No: Señales de que tienes un distributed monolith:
   1. Desplegar el Servicio A requiere desplegar también B y C
   2. Un cambio de schema en la DB de A rompe B y C
   3. Los servicios comparten la misma base de datos
   4. Hay llamadas síncronas en cadena: A → B → C → D → A
   5. Las pruebas de integración requieren levantar todos los servicios

Sí: Microservicio verdaderamente independiente:
   1. Puede desplegarse sin que los demás sepan
   2. Su DB no la toca nadie más
   3. Expone un contrato versionado (API contract)
   4. Puede funcionar en modo degradado si sus dependencias fallan
```

> Si migras de monolito a microservicios copiando el código módulo por módulo,
> acabas con un distributed monolith. La arquitectura interna debe cambiar también.

---

## Comunicación entre microservicios

### Síncrona - esperas la respuesta

```
REST / HTTP/1.1
    A → POST /orders → B → 200 OK → A (continúa)
    Ventaja: simple, familiar
    Desventaja: A se bloquea si B está lento

gRPC / HTTP/2
    A → rpc CreateOrder(req) → B → response → A
    Ventaja: streaming bidireccional, contrato fuerte (protobuf)
    Desventaja: tooling más complejo
```

### Asíncrona - envías y olvidas

```
Message Queue (RabbitMQ · AWS SQS · Azure Service Bus)
    A publica OrderCreated
    B consume cuando tiene capacidad
    Ventaja: A no espera, A no falla si B está caído
    Desventaja: consistencia eventual, debugging más difícil
```

> Para el CRM: mensajes en tiempo real vía SignalR (síncrono),
> tareas en segundo plano (campañas masivas) vía cola asíncrona.

---

## API Gateway - la puerta de entrada única

El **API Gateway** es el único punto de entrada desde el exterior.

```
Cliente (React)
    ↓
API Gateway (NGINX · Kong · AWS API Gateway · YARP)
    ├── /api/auth/*        → Servicio de Autenticación
    ├── /api/contacts/*    → Servicio de Contactos
    ├── /api/messages/*    → Servicio de Mensajes
    └── /api/bots/*        → Servicio de Bots
```

**Responsabilidades del API Gateway:**

| Función | Descripción |
|---------|------------|
| **Routing** | Redirige cada ruta al servicio correcto |
| **Rate limiting** | Limita peticiones por cliente/tenant |
| **Auth** | Valida el JWT antes de pasar al servicio |
| **SSL termination** | Descifra TLS en el gateway, tráfico interno HTTP |
| **Logging** | Centraliza logs de entrada |
| **Caching** | Caché de respuestas frecuentes |

> En el back-template el módulo `Host.Api` actúa como gateway rudimentario.
> En producción a escala se reemplaza o antepone por NGINX o YARP.

---

## Service Discovery - ¿cómo sabe A dónde está B?

**El problema:**

```
La IP de la instancia del Servicio B cambia cada vez que se reinicia.
Auto-scaling crea y destruye instancias. ¿Cómo mantiene A una dirección actualizada?
```

**Soluciones:**

```
1. DNS (simple)
   b-service.internal → DNS resuelve a IP actual
   Kubernetes hace esto automáticamente por servicio

2. Service Registry (Consul · Eureka)
   Cada instancia se registra al arrancar → se da de baja al apagarse
   A consulta el registry: "¿dónde está B?" → obtiene IP:puerto

3. Kubernetes Service
   Kubernetes asigna una IP virtual estable al servicio
   El kube-proxy hace el balanceo internamente
```

> Para la empresa: un solo cluster ECS en AWS o App Service en Azure.
> Service discovery automático vía ALB (Application Load Balancer).

---

<!-- _class: lead -->

## 8C - Regiones y Disponibilidad

---

## ¿Qué es una región?

Una **región** es un conjunto de centros de datos en la misma zona geográfica.

```
AWS Regions:
    us-east-1       → Norte de Virginia, EE.UU.
    us-west-2       → Oregon, EE.UU.
    eu-west-1       → Irlanda
    sa-east-1       → São Paulo, Brasil ← más cercana a México
    ap-southeast-1  → Singapur

Azure Regions:
    eastus          → Virginia
    brazilsouth     → São Paulo
    mexicocentral   → Querétaro ← disponible desde 2023
```

**Cada región tiene múltiples Availability Zones (AZs):**

```
us-east-1a · us-east-1b · us-east-1c
    → Datacenters físicamente separados (distancia: km)
    → Conectados con fibra de baja latencia (<2ms entre AZs)
    → Si una AZ se inunda/incendia, las otras siguen funcionando
```

---

## Arquitectura Multi-AZ (Alta Disponibilidad)

**Despliegue mínimo recomendado: 2 AZs**

```
Region: us-east-1
    ┌─────────────────┬─────────────────┐
    │   us-east-1a    │   us-east-1b    │
    │                 │                 │
    │  API instancia  │  API instancia  │
    │       ↕         │       ↕         │
    │  DB Primary     │  DB Standby     │
    │  (escrituras)   │  (failover)     │
    └────────────────────────────────── ┘
              ↕ Load Balancer (multi-AZ)
```

**RDS Multi-AZ (PostgreSQL administrado):**
- Primary en us-east-1a
- Standby sincrónico en us-east-1b
- Failover automático en ~60 segundos si el primary falla
- La URL de conexión no cambia (CNAME del endpoint)

> Para el sistema de mensajería: 1 región (sa-east-1 o mexicocentral) con 2 AZs.
> Multi-región se agrega cuando el negocio esté en varios países.

---

## SLA, RPO y RTO - los números que importan

| Métrica | Definición | Ejemplo |
|---------|-----------|---------|
| **SLA** (Service Level Agreement) | Disponibilidad comprometida | 99.9% = 8.7 horas de downtime al año |
| **RTO** (Recovery Time Objective) | Tiempo máximo para recuperarse de un fallo | 1 hora |
| **RPO** (Recovery Point Objective) | Cuánto tiempo de datos se pueden perder | 5 minutos |

**Tabla de nines:**

| SLA | Downtime al año | Downtime al mes |
|-----|----------------|----------------|
| 99% | 87.6 horas | 7.3 horas |
| 99.9% | 8.76 horas | 43.8 minutos |
| 99.95% | 4.38 horas | 21.9 minutos |
| 99.99% | 52.6 minutos | 4.4 minutos |
| 99.999% | 5.26 minutos | 26.3 segundos |

> Amazon S3 promete 99.99% de disponibilidad (durabilidad de datos: 99.999999999%).
> Para el CRM: 99.9% es razonable al inicio -> 99.95% cuando el negocio depende de él.

---

## CDN - Content Delivery Network

Una **CDN** sirve contenido estático desde el servidor más cercano al usuario.

```
Sin CDN:
    Usuario en CDMX → request → Servidor en Virginia → 150ms

Con CDN (CloudFront · Cloudflare · Azure CDN):
    Usuario en CDMX → request → Edge node en México → 8ms
```

**¿Qué va en la CDN?**

```
Sí: Assets estáticos:
   images/logo.png   videos/*.mp4   fonts/*.woff2
   *.css   *.js (bundles de React/Vite)

Sí: API responses cacheables:
   GET /api/tenants/{id}/settings (cambia poco)
   GET /api/plans                 (casi nunca cambia)

Nunca en CDN:
   POST /api/messages   (escrituras)
   GET /api/conversations (datos en tiempo real)
   Datos sensibles de cada usuario
```

> El front-template usa Vite -> build genera assets con hash en el nombre.
> Eso permite cache-busting automático: `/assets/main.a1b2c3.js`

---

## Multi-Región - cuándo y cómo

**Multi-región** = el sistema corre simultáneamente en 2+ regiones geográficas.

**¿Cuándo tiene sentido?**

```
Latencia: usuarios en México Y en España → regiones sa-east-1 + eu-west-1
Compliance: el RGPD restringe transferir datos fuera del EEE sin garantías (arts. 44 a 49) → una región europea es la vía más sencilla
Disaster recovery: si toda una región de AWS cae → la otra sigue
```

**El mayor desafío: sincronización de datos**

```
Usuario escribe mensaje → ¿en qué región se guarda?
Si Region A cae → ¿cómo tiene Region B los datos más recientes?

Estrategia activo-pasivo:
    Región A (activa): recibe todo el tráfico
    Región B (pasiva): réplica de lectura, failover en 5-10 min

Estrategia activo-activo:
    Ambas regiones reciben tráfico
    Conflictos de escritura concurrente → CRDT o última-escritura-gana
    (muy complejo — evitar hasta que sea necesario)
```

> La empresa: 1 región activa + backups a S3 cross-region.
> Multi-región activo-activo cuando haya clientes en 2+ continentes.

---

<!-- _class: lead -->

## 8D - Rate Limiting: proteger el sistema de sus propios usuarios

---

## ¿Qué es Rate Limiting?

**Rate Limiting** = limitar cuántas peticiones puede hacer un cliente en un período de tiempo.

**Sin rate limiting:**

```
Bot malicioso → 10,000 req/min → tu API → DB saturada → todos los usuarios afectados
```

**Con rate limiting:**

```
Bot malicioso → 10,000 req/min →
    API Gateway: "solo permito 100 req/min por IP" →
    429 Too Many Requests después de req #101 →
    Tu API procesa 100 req/min · los demás usuarios no se ven afectados
```

**Casos reales donde rate limiting salva el día:**

| Caso | Sin límite | Con límite |
|------|-----------|-----------|
| Credenciales comprometidas | Attacker enumera 1M usuarios | Bloqueado en req #10 |
| Bug en el frontend | Loop infinito -> 50,000 req/min | Bloqueado - error visible |
| Cliente mal configurado | Retry sin backoff -> satura la DB | Limitado - fácil de diagnosticar |
| Scraping de datos | 100% de tus contactos en 1 hora | Tomará meses -> no rentable |

---

## Algoritmos de Rate Limiting

### Token Bucket (el más común)

```
Cubo de tokens con capacidad N.
Cada segundo se agregan K tokens (si el cubo no está lleno).
Cada petición consume 1 token.
Si no hay tokens → 429.

Ventaja: permite ráfagas cortas (burst) hasta la capacidad del cubo.
Usado por: AWS API Gateway, Stripe, Twitter API.
```

### Fixed Window Counter

```
Ventana de 1 minuto. Contador se reinicia en :00 cada minuto.
Permite hasta N peticiones por ventana.

Problema: burst al cambio de ventana.
Si el límite es 100/min y haces 100 req en el segundo 59
    + 100 req en el segundo 01 del siguiente minuto → 200 req en 2 segundos.
```

### Sliding Window Log

```
Guarda el timestamp de cada petición.
Al llegar una nueva petición, descarta las más viejas que N segundos.
Si quedan ≥ límite → 429.

Ventaja: más preciso que Fixed Window.
Desventaja: usa más memoria (un registro por petición).
```

---

## Sliding Window Counter - el mejor balance

**Sliding Window Counter** combina precisión y eficiencia de memoria.

```
Límite: 100 req/min

Momento actual: 01:30 (1 minuto 30 segundos)
    Ventana anterior (): 80 peticiones
    Ventana actual   (): 20 peticiones (hasta ahora)

Peso de la ventana anterior:
    (60 - 30) / 60 = 0.5 → 80 × 0.5 = 40

Estimación actual = 40 + 20 = 60 req → por debajo del límite → OK

En Redis: solo 2 contadores en lugar de N timestamps
```

> Nginx, Cloudflare y la mayoría de los servicios modernos usan este algoritmo.
> ASP.NET Core `System.Threading.RateLimiting` implementa Fixed Window y Sliding Window.

---

## Leaky Bucket - para flujo uniforme

**Leaky Bucket** procesa peticiones a una tasa constante, sin importar los picos.

```
Peticiones entrantes (ráfaga):  ████████████████
                                    ↓ cubo (cola FIFO)
Peticiones procesadas (uniforme): █ █ █ █ █ █ █ █

Si el cubo se llena → nuevas peticiones se descartan (429)
```

**Diferencia con Token Bucket:**

| | Token Bucket | Leaky Bucket |
|-|-------------|-------------|
| Ráfagas | permitidas hasta capacidad | siempre tasa constante |
| Uniformidad de salida | Variable | siempre uniforme |
| Uso típico | Rate limiting de API | Shaping de tráfico de red |

> Para una API REST: Token Bucket o Sliding Window.
> Para WebSockets o streaming: Leaky Bucket para proteger la DB.

---

## Rate Limiting en ASP.NET Core

Disponible desde .NET 7 en `System.Threading.RateLimiting`:

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("global", config =>
    {
        config.PermitLimit = 100;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 10;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

app.UseRateLimiter();
```

```csharp
// Aplicar a un endpoint específico
app.MapPost("/api/messages", SendMessage)
   .RequireRateLimiting("global");

// O a todos los endpoints del grupo
app.MapGroup("/api/auth")
   .RequireRateLimiting("strict")
   .MapPost("/login", Login)
   .MapPost("/register", Register);
```

---

## Rate Limiting por usuario

**Problema con rate limiting por IP:**

```
NAT / Red corporativa → 500 usuarios comparten la misma IP
Rate limit: 100 req/min por IP
Resultado: 500 usuarios comparten 100 req/min → sistema inutilizable
```

**Solución: rate limiting por usuario autenticado (user ID / tenant ID)**

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("per-user", config =>
    {
        config.PermitLimit = 200;           // 200 req por usuario
        config.Window = TimeSpan.FromMinutes(1);
        config.SegmentsPerWindow = 4;       // segmentos de 15s para sliding window
    });

    // Extraer la clave de rate limit del JWT
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? httpContext.Connection.RemoteIpAddress?.ToString()
                         ?? "anonymous";

            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: userId,
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 4
                });
        });
});
```

---

## Rate Limiting por tenant (multi-tenancy)

En el sistema de mensajería cada workspace (tenant) tiene su propio límite.

```csharp
// Planes con diferentes límites de API
// Plan Starter:      500 req/min por tenant
// Plan Professional: 2000 req/min por tenant
// Plan Enterprise:   ilimitado (fair use policy)

options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
    httpContext =>
    {
        var tenantId = httpContext.Items["TenantId"]?.ToString() ?? "unknown";
        var plan = httpContext.Items["TenantPlan"]?.ToString() ?? "starter";

        var limit = plan switch
        {
            "professional" => 2000,
            "enterprise"   => int.MaxValue,
            _              => 500           // starter
        };

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"tenant:{tenantId}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit,
                Window = TimeSpan.FromMinutes(1)
            });
    });
```

---

## Headers de Rate Limiting - comunicar el estado al cliente

El RFC 6585 y el draft IETF `RateLimit-*` headers:

```http
HTTP/1.1 200 OK
X-RateLimit-Limit: 200
X-RateLimit-Remaining: 143
X-RateLimit-Reset: 1717027200
Retry-After: 60
```

```http
HTTP/1.1 429 Too Many Requests
X-RateLimit-Limit: 200
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1717027200
Retry-After: 47
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc6585#section-4",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Has superado el límite de 200 peticiones por minuto. Reintenta en 47 segundos.",
  "retryAfterSeconds": 47
}
```

> El cliente React debe leer `Retry-After` y esperar antes de reintentar.
> Sin este header, el cliente puede reintentar inmediatamente -> loop de 429s.

---

## Throttling vs Rate Limiting

A menudo se confunden. Son distintos:

| | Rate Limiting | Throttling |
|-|--------------|-----------|
| **Qué hace** | Bloquea peticiones que superan el límite | Ralentiza peticiones que superan el límite |
| **Respuesta al cliente** | 429 Too Many Requests (inmediato) | 200 OK pero más lento (artificial delay) |
| **Experiencia de usuario** | "Tu solicitud fue rechazada" | "Tu solicitud tardó más de lo normal" |
| **Uso típico** | APIs públicas - protección contra abuso | Background jobs - migraciones - scraping interno |

```csharp
// Throttling en un BackgroundService de campañas masivas
// Enviar máximo 10 mensajes/segundo para no saturar WhatsApp API
await foreach (var recipient in campaign.Recipients)
{
    await _whatsAppClient.SendAsync(recipient, message, ct);
    await Task.Delay(TimeSpan.FromMilliseconds(100), ct); // 10/seg
}
```

---

## Circuit Breaker - proteger cuando el servicio externo falla

**Sin Circuit Breaker:**

```
WhatsApp API cae 30 minutos
    → Tu API sigue intentando enviar mensajes
    → 1000 threads esperando timeout de 30s
    → Thread pool agotado
    → Tu API completa deja de responder
    → Todos los módulos afectados
```

**Con Circuit Breaker (Polly):**

```
Estado Closed (normal):
    → Peticiones fluyen normalmente
    → Se cuentan los fallos consecutivos

Estado Open (disparado):
    → Peticiones fallan inmediatamente (fast fail) sin llamar a WhatsApp API
    → Espera 30s antes de intentar de nuevo
    → Tu thread pool queda libre para otras peticiones

Estado Half-Open (prueba):
    → Deja pasar 1 petición de prueba
    → Si tiene éxito → vuelve a Closed
    → Si falla → vuelve a Open
```

---

## Circuit Breaker en .NET con Polly

```csharp
// Configuración del circuit breaker para el cliente de WhatsApp
builder.Services.AddHttpClient<IWhatsAppClient, WhatsAppClient>(client =>
{
    client.BaseAddress = new Uri(whatsAppApiUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddResilienceHandler("whatsapp-circuit-breaker", pipeline =>
{
    pipeline.AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        // Abrir el circuito si 5 de 10 peticiones fallan
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(10),
        MinimumThroughput = 10,

        // Tiempo en estado Open antes de probar de nuevo
        BreakDuration = TimeSpan.FromSeconds(30),

        OnOpened = args =>
        {
            _logger.LogWarning("Circuit abierto: WhatsApp API no disponible");
            return ValueTask.CompletedTask;
        }
    });

    pipeline.AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(2),
        BackoffType = DelayBackoffType.Exponential
    });
});
```

---

## Queue-based Load Leveling - absorber los picos

**El problema de los picos de tráfico:**

```
09:00 → campaña masiva se activa → 50,000 mensajes en cola
Enviar todos a WhatsApp API simultáneamente → rate limit de WhatsApp → 429

Sin queue: 50,000 peticiones concurrentes → tu API cae
```

**Solución: Message Queue**

```
Campaña activada
    → Encola 50,000 mensajes en RabbitMQ / AWS SQS

BackgroundService (worker)
    → Consume la cola a 10 mensajes/segundo
    → Nunca supera el rate limit de WhatsApp
    → La cola actúa como buffer entre el pico de entrada y la tasa de salida

Tiempo total: 50,000 / 10 = 5,000 segundos ≈ 83 minutos
    → Todos los mensajes llegan · el sistema no se satura
```

---

## Back Pressure - cuando el productor supera al consumidor

**Back Pressure** = señal del consumidor al productor para que frene.

```
Sin back pressure:
    Productor → 1,000 req/seg
    Consumidor → procesa 100 req/seg
    Cola → crece 900 req/seg → en 1 hora: 3.2M mensajes acumulados → OOM

Con back pressure:
    Cola llena → productor recibe señal → frena la generación
    Alternativa: productor descarta mensajes no críticos (shedding)
    Alternativa: productor responde 503 Service Unavailable temporalmente
```

**En ASP.NET Core:**

```csharp
// El servidor tiene un límite máximo de conexiones pendientes
// Si se supera, devuelve 503 antes de aceptar la conexión
// Kestrel: MaxConcurrentConnections, MaxConcurrentUpgradedConnections

// En el middleware:
app.Use(async (context, next) =>
{
    if (_semaphore.CurrentCount == 0)
    {
        context.Response.StatusCode = 503;
        await context.Response.WriteAsync("Servidor temporalmente ocupado. Reintenta en 5 segundos.");
        return;
    }
    await _semaphore.WaitAsync();
    try { await next(); }
    finally { _semaphore.Release(); }
});
```

---

## Métricas de escalabilidad - medir para decidir

No escales lo que no has medido. Las métricas que importan:

| Métrica | Herramienta | Qué te dice |
|---------|------------|------------|
| **p50 / p95 / p99 latencia** | Prometheus + Grafana | La mayoría ve X ms, el 1% más lento ve Y ms |
| **Throughput (req/seg)** | Prometheus | Cuántas peticiones procesas en realidad |
| **Error rate** | Serilog + Seq | % de peticiones que terminan en 5xx |
| **CPU / Memory** | CloudWatch - Azure Monitor | Cuándo necesitas más recursos |
| **DB connections** | pg_stat_activity | Si el pool de conexiones está agotado |
| **Queue depth** | RabbitMQ Management | Si los mensajes se acumulan |
| **Cache hit rate** | Redis INFO | Si el caching está funcionando |

**Herramienta para cargas de prueba:**

```bash
# k6 — load testing tool (open source)
k6 run --vus 100 --duration 30s script.js
# 100 usuarios virtuales durante 30 segundos
# Te da p95, p99, throughput, error rate
```

---

## Escalabilidad en el sistema de mensajería - hoja de ruta

**Fase 1 - MVP (hoy, S03 en adelante):**

```
1 servidor (EC2 t3.medium o App Service B2)
PostgreSQL administrado (RDS · 2 AZs · Multi-AZ standby)
Rate limiting: 200 req/min por usuario (ASP.NET Core built-in)
Sin Redis aún (IMemoryCache para sesiones)
```

**Fase 2 - Primeros 100 tenants:**

```
2 instancias de la API detrás de un ALB
Redis para cache y sesiones distribuidas
Réplica de lectura en PostgreSQL
Rate limiting por tenant según plan
CDN para assets del frontend (CloudFront)
```

**Fase 3 - Escala real:**

```
Auto-scaling 2-10 instancias
Redis Cluster
Read replicas con pgBouncer (pool de conexiones)
Circuit breakers en todas las llamadas externas
Observabilidad completa (OpenTelemetry + Grafana)
```

---

## Resumen - Parte 8

| Sub-sección | Concepto clave |
|-------------|---------------|
| **Escalabilidad vertical** | Más recursos al mismo servidor - límite físico - single point of failure |
| **Escalabilidad horizontal** | Más instancias - requiere stateless - necesita load balancer |
| **Stateless API** | Todo estado va a DB o Redis - la instancia es desechable |
| **Load Balancer** | Round Robin para APIs - health checks activos - multi-AZ |
| **Réplicas de lectura** | Lecturas a replica - escrituras a primary - lag ~50ms aceptable |
| **Auto-scaling** | Escala en picos - ahorra costo - cold start de 30-90s |
| **Microservicios** | Un equipo por servicio - escala independiente - complejidad alta |
| **Monolito Modular** | Correcto para equipos pequeños - migrar módulo por módulo si es necesario |
| **Regiones** | AZs para alta disponibilidad - multi-región cuando el negocio lo justifica |
| **SLA / RTO / RPO** | Números que tienes que conocer antes de el primer cliente de producción |
| **Rate Limiting** | Token Bucket o Sliding Window - 429 con Retry-After - por usuario/tenant |
| **Circuit Breaker** | Fast fail cuando el dependiente falla - protege el thread pool |
| **Back Pressure** | El consumidor frena al productor - 503 temporal es mejor que caída total |
| **Métricas** | p95 latencia - throughput - error rate - medir antes de escalar |

---

<!-- _class: lead -->

# Parte 9 - Redis

## La base de datos que vive en RAM

> Redis = **Re**mote **Di**ctionary **S**erver
> No lo agregaremos todavía - lo instalamos más adelante.
> Hoy entiendes qué es, cómo piensa y para qué lo usaremos.

---

## ¿Qué problema resuelve Redis?

**Sin Redis:**

```
Petición → API → PostgreSQL (disco) → respuesta
                     ↑
              10–50ms por consulta
              Si hay 1000 peticiones/seg → DB saturada
```

**Con Redis:**

```
Petición → API → Redis (RAM) → respuesta         ← hit (< 1ms)
Petición → API → Redis (miss) → PostgreSQL → Redis → respuesta ← miss (50ms, pero lo guarda)
```

**Redis es una base de datos en memoria (in-memory):**
- Los datos viven en RAM -> lectura en microsegundos
- Soporta persistencia opcional en disco (RDB / AOF)
- Velocidad: 100,000-1,000,000 operaciones/segundo en un solo nodo

> La diferencia entre 1ms (Redis) y 50ms (PostgreSQL)
> no parece mucho. A 1,000 req/seg se vuelve la diferencia
> entre escalar o comprar más hardware.

---

## Redis vs PostgreSQL - no son competidores

| | Redis | PostgreSQL |
|--|-------|-----------|
| **Dónde vive** | RAM (opcional disco) | Disco (opcional RAM via shared_buffers) |
| **Velocidad** | < 1ms | 1-100ms |
| **Durabilidad** | Opcional (configurable) | ACID full |
| **Queries complejas** | no SQL | JOINs, CTEs, window functions |
| **Estructura** | Clave-valor (estructuras ricas) | Tablas relacionales |
| **Tamaño de datos** | Limitado por RAM | Limitado por disco |
| **Uso típico** | Cache - sesiones - colas - pub/sub | Fuente de verdad |

**Regla de oro:**

> PostgreSQL es la **fuente de verdad**.
> Redis es el **acelerador**.
> Un dato que no está en PostgreSQL no existe.
> Un dato que no está en Redis simplemente tarda más en llegar.

---

## Los tipos de datos de Redis

Redis no es un simple diccionario `string → string`. Tiene estructuras ricas:

| Tipo | Operaciones clave | Caso de uso |
|------|------------------|------------|
| **String** | GET/SET/INCR/EXPIRE | Cache de cualquier valor - contadores |
| **Hash** | HGET/HSET/HGETALL | Objeto con múltiples campos (perfil de usuario) |
| **List** | LPUSH/RPUSH/LRANGE | Cola FIFO - historial de últimas N entradas |
| **Set** | SADD/SMEMBERS/SINTERSTORE | Tags únicos - miembros de un grupo |
| **Sorted Set** | ZADD/ZRANGE/ZRANGEBYSCORE | Leaderboards - colas con prioridad |
| **JSON** (RedisJSON) | JSON.SET/JSON.GET | Documentos anidados sin serializar |
| **Stream** | XADD/XREAD/XREADGROUP | Event log - message queue persistente |
| **Pub/Sub** | PUBLISH/SUBSCRIBE | Eventos en tiempo real entre procesos |

---

## String - el tipo más simple (y más versátil)

```redis
# Guardar un valor con TTL de 5 minutos
SET conversation:tenant:1:conv:42 "{...json...}" EX 300

# Recuperar
GET conversation:tenant:1:conv:42
# → "{...json...}" o nil si expiró

# Contador atómico (para rate limiting)
INCR ratelimit:user:99:minute:2026053014  → 1
INCR ratelimit:user:99:minute:2026053014  → 2
EXPIRE ratelimit:user:99:minute:2026053014 60

# ¿Cuántos quedan?
GET ratelimit:user:99:minute:2026053014   → "2"
```

**INCR es atómico:**
- No hay race condition entre dos instancias incrementando simultáneamente
- Redis es single-threaded para operaciones -> garantiza orden

---

## Hash - objetos con múltiples campos

```redis
# Guardar perfil de agente (más eficiente que serializar JSON completo)
HSET agent:tenant:1:agent:7
    fullName "Ana García"
    email    "ana@example.com"
    role     "agent"
    isActive "true"

# Leer un campo específico
HGET agent:tenant:1:agent:7 fullName   → "Ana García"

# Leer todos los campos
HGETALL agent:tenant:1:agent:7
→ ["fullName", "Ana García", "email", "ana@example.com", ...]

# Actualizar solo un campo (sin reescribir todo el objeto)
HSET agent:tenant:1:agent:7 role "admin"

# ¿Existe?
HEXISTS agent:tenant:1:agent:7 email   → 1
```

> Ventaja sobre String+JSON: puedes actualizar un campo sin leer/escribir todo el objeto.

---

## List - cola FIFO y buffer circular

```redis
# Cola de mensajes pendientes de envío (FIFO)
RPUSH queue:tenant:1:outbound "{msg1}"
RPUSH queue:tenant:1:outbound "{msg2}"
RPUSH queue:tenant:1:outbound "{msg3}"

# Worker consume de la izquierda (FIFO)
BLPOP queue:tenant:1:outbound 0   → "{msg1}"  (blocking pop)

# Historial de últimos 50 mensajes de una conversación
LPUSH history:tenant:1:conv:42 "{msg_nuevo}"
LTRIM history:tenant:1:conv:42 0 49    # mantener solo los últimos 50

# Leer el historial
LRANGE history:tenant:1:conv:42 0 -1
```

**BLPOP** - blocking pop:
- El worker espera bloqueado hasta que llegue un elemento
- No hace polling -> 0 CPU cuando la cola está vacía

---

## Sorted Set - colas con prioridad y leaderboards

```redis
# Cola con prioridad para mensajes (score = timestamp)
ZADD queue:priority:tenant:1
    1717027200 "{msg:urgente}"      ← score más bajo = primero
    1717027260 "{msg:normal}"

# El worker toma el de menor score (más antiguo / más urgente)
ZPOPMIN queue:priority:tenant:1    → "{msg:urgente}"

# Leaderboard de agentes por conversaciones resueltas (mes)
ZADD leaderboard:tenant:1:2026-05 15 "agent:7"   ← 15 resueltas
ZADD leaderboard:tenant:1:2026-05 23 "agent:3"
ZADD leaderboard:tenant:1:2026-05  8 "agent:9"

# Top 3 agentes del mes
ZREVRANGE leaderboard:tenant:1:2026-05 0 2 WITHSCORES
→ ["agent:3", "23", "agent:7", "15", "agent:9", "8"]
```

---

## Pub/Sub - mensajería en tiempo real entre procesos

```redis
# Proceso A — Suscriptor (SignalR Hub)
SUBSCRIBE channel:tenant:1:conversations

# Proceso B — Publicador (cuando llega un mensaje nuevo de WhatsApp)
PUBLISH channel:tenant:1:conversations "{tipo: 'new-message', convId: 42}"

# Proceso A recibe:
# Message type: message
# Channel: channel:tenant:1:conversations
# Payload: "{tipo: 'new-message', convId: 42}"
```

**En el sistema de mensajería:**

```
WhatsApp webhook → API (instancia 1) → PUBLISH en Redis
                                            ↓
                                 Todas las instancias suscritas reciben el evento
                                            ↓
                                 SignalR Hub → push al cliente React del agente
```

> Sin Redis Pub/Sub, si hay 3 instancias de la API,
> el evento solo llega al cliente conectado a la instancia 1.
> Con Redis como backplane: llega a todos.

---

## Streams - event log persistente

```redis
# Agregar evento al stream (como append-only log)
XADD events:tenant:1 * type message_received convId 42 fromNumber "+521234567890"
#                    ↑ ID auto-generado (timestamp + secuencia)

# Leer todos los eventos desde el inicio
XREAD COUNT 10 STREAMS events:tenant:1 0-0

# Consumer group — múltiples workers comparten el stream (cada mensaje a 1 worker)
XGROUP CREATE events:tenant:1 workers $ MKSTREAM
XREADGROUP GROUP workers worker-1 COUNT 5 STREAMS events:tenant:1 >
# ">" significa: dame mensajes no entregados aún a este grupo

# Confirmar procesamiento
XACK events:tenant:1 workers 1717027200-0
```

> Streams = Kafka lite.
> Para el CRM: audit trail de eventos del sistema,
> procesamiento de webhooks entrantes con garantía de entrega.

---

## TTL - Time To Live (expiración automática)

```redis
# Establecer TTL al crear
SET session:abc123 "{userId: 99}" EX 3600    # expira en 1 hora

# Agregar TTL a una clave existente
EXPIRE agent:tenant:1:agent:7 300             # expira en 5 minutos

# Ver cuánto tiempo queda
TTL session:abc123   → 3547 (segundos restantes)
TTL session:abc123   → -2   (ya expiró / no existe)
TTL session:abc123   → -1   (existe pero sin TTL)

# Persistir (quitar el TTL)
PERSIST session:abc123
```

**Casos de uso del TTL en el CRM:**

| Clave | TTL | Por qué |
|-------|-----|---------|
| Cache de conversación | 5 min | Datos cambian frecuente |
| Sesión de usuario | 1 hora | Seguridad |
| Rate limit counter | 1 min | Ventana deslizante |
| Config de tenant | 10 min | Cambia raramente |
| OTP de invitación | 15 min | Seguridad |

---

## Nombrado de claves - convención crítica

Redis no tiene tablas ni namespaces - las claves son strings planos.
La convención de nombres es tu única estructura.

**Patrón recomendado:** `{módulo}:{tenant}:{entidad}:{id}:{campo}`

```redis
# Bien nombradas — legibles, multi-tenant, filtrables
session:tenant:1:user:99
conv:tenant:1:id:42
ratelimit:tenant:1:user:99:2026053014

# Mal nombradas — ambiguas, sin tenant, colisiones
user99
conv42
cache1
data:42
```

**En código C#:**

```csharp
public static class RedisKeys
{
    public static string Session(long tenantId, long userId)
        => $"session:tenant:{tenantId}:user:{userId}";

    public static string Conversation(long tenantId, long convId)
        => $"conv:tenant:{tenantId}:id:{convId}";

    public static string RateLimit(long tenantId, long userId, string window)
        => $"ratelimit:tenant:{tenantId}:user:{userId}:{window}";
}
```

---

## Redis en ASP.NET Core - Stack Exchange y IDistributedCache

**Dos niveles de abstracción:**

```
IDistributedCache (interfaz de .NET)
    ↓ implementación
StackExchange.Redis (librería de bajo nivel)
    ↓ conecta a
Redis Server (proceso separado)
```

```csharp
// Program.cs — registrar Redis como IDistributedCache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    // Redis: "localhost:6379" en dev
    //        "redis-cluster.internal:6379" en producción
});
```

```csharp
// Inyectar en un servicio
public class ConversationCache(IDistributedCache cache)
{
    public async Task<Conversation?> GetAsync(long tenantId, long convId, CancellationToken ct)
    {
        var key = RedisKeys.Conversation(tenantId, convId);
        var bytes = await cache.GetAsync(key, ct);
        return bytes is null ? null : JsonSerializer.Deserialize<Conversation>(bytes);
    }
}
```

---

## IConnectionMultiplexer - acceso de bajo nivel

Para operaciones avanzadas (Pub/Sub, Streams, comandos directos):

```csharp
// Program.cs
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
```

```csharp
// En un servicio
public class MessagePublisher(IConnectionMultiplexer redis)
{
    public async Task PublishNewMessageAsync(long tenantId, long convId, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var subscriber = redis.GetSubscriber();

        // Publicar evento para SignalR Hub
        await subscriber.PublishAsync(
            RedisChannel.Literal($"channel:tenant:{tenantId}:conversations"),
            JsonSerializer.Serialize(new { Type = "new-message", ConvId = convId }));
    }
}
```

> `IDistributedCache` para cache estándar (GET/SET/DELETE + TTL).
> `IConnectionMultiplexer` para Pub/Sub, Streams y comandos específicos de Redis.

---

## Patrón Cache-Aside (el más común)

El código decide cuándo leer de Redis y cuándo ir a la DB.

```csharp
public async Task<ConversationDto?> GetConversationAsync(
    long tenantId, long convId, CancellationToken ct)
{
    var key = RedisKeys.Conversation(tenantId, convId);

    // 1. Intenta desde Redis
    var cached = await _cache.GetAsync(key, ct);
    if (cached is not null)
        return JsonSerializer.Deserialize<ConversationDto>(cached);

    // 2. Cache miss → va a la base de datos
    var conversation = await _repo.GetByIdAsync(tenantId, convId, ct);
    if (conversation is null)
        return null;

    // 3. Guarda en Redis con TTL
    var dto = _mapper.Map<ConversationDto>(conversation);
    await _cache.SetAsync(
        key,
        JsonSerializer.SerializeToUtf8Bytes(dto),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
        ct);

    return dto;
}
```

---

## Patrón Write-Through - actualizar cache al escribir

Cuando se actualiza la DB, también se actualiza Redis en el mismo flujo:

```csharp
public async Task UpdateConversationStatusAsync(
    long tenantId, long convId, ConversationStatus status, CancellationToken ct)
{
    // 1. Actualizar en la DB (fuente de verdad)
    await _repo.UpdateStatusAsync(tenantId, convId, status, ct);

    // 2. Actualizar en Redis inmediatamente (Write-Through)
    var key = RedisKeys.Conversation(tenantId, convId);
    var updated = await _repo.GetByIdAsync(tenantId, convId, ct);
    await _cache.SetAsync(
        key,
        JsonSerializer.SerializeToUtf8Bytes(updated),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
        ct);
}
```

**vs Invalidar el cache (más simple):**

```csharp
// Alternativa: simplemente borrar la clave
await _cache.RemoveAsync(key, ct);
// La próxima lectura hará cache miss → reconstruye desde DB
```

> Invalidar es más simple y más seguro. Write-Through es más rápido pero
> requiere mantener la lógica de escritura sincronizada.

---

## Cache Stampede - el problema que nadie menciona

**Cache Stampede:** el caché expira -> 1000 peticiones simultáneas van a la DB al mismo tiempo.

```
TTL expira a las 14:00:00.000
→ 1000 requests llegan entre 14:00:00.001 y 14:00:00.010
→ Todas hacen cache miss
→ Todas van a PostgreSQL simultáneamente
→ DB se satura
→ Respuesta: el caché resuelve el problema de DB · el stampede lo vuelve a crear
```

**Soluciones:**

```csharp
// 1. Jitter en el TTL (aleatorio dentro de un rango)
var ttl = TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(Random.Shared.Next(0, 60));

// 2. Mutex en Redis (solo 1 request reconstruye el cache)
var lockKey = $"lock:{key}";
var acquired = await db.StringSetAsync(lockKey, "1", TimeSpan.FromSeconds(10),
    When.NotExists);

if (acquired)
{
    // Este worker reconstruye el cache
    var data = await _repo.GetAsync(...);
    await _cache.SetAsync(key, data, options, ct);
    await db.KeyDeleteAsync(lockKey);
}
else
{
    // Otros workers esperan o devuelven dato stale
    await Task.Delay(50, ct);
    return await _cache.GetAsync(key, ct);
}
```

---

## Política de evicción - qué pasa cuando Redis se llena

Si Redis llega al límite de memoria, necesita expulsar claves. Políticas disponibles:

| Política | Comportamiento | Cuándo usarla |
|----------|---------------|--------------|
| `noeviction` | Error si se intenta escribir | Cuando Redis es cola (no cache) |
| `allkeys-lru` | Expulsa la clave menos usada recientemente | Cache general |
| `allkeys-lfu` | Expulsa la clave menos frecuentemente usada | Cache con patrones de acceso irregulares |
| `volatile-lru` | LRU solo en claves con TTL | Mix de cache y datos permanentes |
| `volatile-ttl` | Expulsa la que expira más pronto | Cache con TTLs variados |
| `allkeys-random` | Expulsa al azar | raramente útil |

**Configuración en redis.conf:**

```
maxmemory 512mb
maxmemory-policy allkeys-lru
```

> Para el CRM: `volatile-lru` si Redis tiene tanto cache (con TTL) como colas (sin TTL).
> `allkeys-lru` si Redis es dedicado solo a cache.

---

## Persistencia en Redis - RDB y AOF

Por defecto Redis puede perder datos si se reinicia. Dos mecanismos de persistencia:

### RDB - Snapshot periódico

```
# redis.conf
save 900 1      # snapshot si hay ≥1 cambio en 15 minutos
save 300 10     # snapshot si hay ≥10 cambios en 5 minutos
save 60 10000   # snapshot si hay ≥10000 cambios en 1 minuto
```

- Guarda una foto completa del estado en disco
- Rápido de restaurar al arrancar
- Puede perder datos entre snapshots (RPO = tiempo entre snapshots)

### AOF - Append Only File

```
# redis.conf
appendonly yes
appendfsync everysec    # fsync cada segundo (balance velocidad/durabilidad)
```

- Registra cada operación de escritura en un log
- RPO ≤ 1 segundo (con `everysec`)
- Más lento de restaurar que RDB

> Para el CRM: cache -> sin persistencia (se reconstruye desde DB).
> Para colas de mensajes -> AOF con `everysec`.

---

## Redis en Docker Compose

```yaml
# docker-compose.yml
services:
  redis:
    image: redis:7.4-alpine
    ports:
      - "6379:6379"
    command: >
      redis-server
      --maxmemory 256mb
      --maxmemory-policy volatile-lru
      --appendonly yes
      --appendfsync everysec
    volumes:
      - redis-data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 3

  api:
    build: .
    depends_on:
      redis:
        condition: service_healthy
    environment:
      - ConnectionStrings__Redis=redis:6379

volumes:
  redis-data:
```

```bash
# Conectar al CLI de Redis dentro del contenedor
docker compose exec redis redis-cli

# Comandos básicos de diagnóstico
redis-cli PING              → PONG
redis-cli INFO server       → versión, uptime, etc.
redis-cli INFO memory       → uso de RAM
redis-cli KEYS "conv:*"     → todas las claves del patrón (¡cuidado en producción!)
redis-cli SCAN 0 MATCH "conv:tenant:1:*" COUNT 100   → versión segura de KEYS
redis-cli MONITOR           → ver comandos en tiempo real (debug)
```

---

## Redis Sentinel vs Redis Cluster

### Redis Standalone (dev y proyectos pequeños)

```
1 instancia · 1 servidor · sin redundancia
```

### Redis Sentinel (alta disponibilidad, 1 shard)

```
Primary Redis ← escribe aquí
    ↓ replicación asíncrona
Replica Redis 1 · Replica Redis 2 ← lee aquí

3 Sentinels monitoran al primary
Si el primary cae → Sentinels eligen nueva replica como primary (failover < 30s)
```

### Redis Cluster (alta disponibilidad + sharding horizontal)

```
16384 slots distribuidos entre N masters
Cada master tiene réplicas
Escala horizontalmente en datos Y en throughput
```

| | Standalone | Sentinel | Cluster |
|--|-----------|---------|--------|
| Alta disponibilidad | No | Sí | Sí |
| Sharding | No | No | Sí |
| Complejidad | Baja | Media | Alta |
| Cuándo usarlo | Dev | Producción inicial | > 100GB datos |

> Para el CRM: Standalone en desarrollo, Sentinel en producción inicial.

---

## Transacciones en Redis - MULTI / EXEC

Redis puede agrupar comandos para que se ejecuten atómicamente:

```redis
MULTI
INCR ratelimit:user:99
EXPIRE ratelimit:user:99 60
EXEC
```

**En C# con StackExchange.Redis:**

```csharp
var db = redis.GetDatabase();
var key = RedisKeys.RateLimit(tenantId, userId, window);

// Transacción: incrementar y poner TTL en una sola operación atómica
var transaction = db.CreateTransaction();
var incrTask = transaction.StringIncrementAsync(key);
var expireTask = transaction.KeyExpireAsync(key, TimeSpan.FromMinutes(1));
await transaction.ExecuteAsync();

var count = await incrTask;
// count = nuevo valor del contador
```

**Lua Scripts - transacciones con lógica:**

```csharp
// Script Lua para rate limiting atómico
var script = LuaScript.Prepare(@"
    local current = redis.call('INCR', @key)
    if current == 1 then
        redis.call('EXPIRE', @key, @ttl)
    end
    return current");

var result = (long)await db.ScriptEvaluateAsync(script,
    new { key = (RedisKey)key, ttl = 60 });
```

---

## Multi-tenancy con Redis - aislamiento por clave

**El prefijo de tenant en la clave es la única separación entre tenants:**

```csharp
// Bien: Siempre incluir tenantId en la clave
await _cache.SetAsync($"conv:tenant:{tenantId}:id:{convId}", data, opts, ct);

// Mal: Sin tenant — un tenant puede leer datos de otro
await _cache.SetAsync($"conv:{convId}", data, opts, ct);
```

**Redis no tiene namespaces nativos en la versión open-source.**
Para aislamiento fuerte entre tenants en producción:

```
Opción A: prefijo en clave (lo que usamos) — simple, efectivo
Opción B: DB number separado (Redis tiene 16 DBs: 0-15) — limitado a 16 tenants
Opción C: instancia Redis separada por tenant — costo alto, solo enterprise
```

**Gestión del prefijo con `IDistributedCache`:**

```csharp
// Wrapper que inyecta el tenant automáticamente
public class TenantAwareCache(IDistributedCache cache, ICurrentTenantService tenant)
{
    private string Key(string key) => $"{key}:tenant:{tenant.Id}";

    public Task<byte[]?> GetAsync(string key, CancellationToken ct)
        => cache.GetAsync(Key(key), ct);

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions opts, CancellationToken ct)
        => cache.SetAsync(Key(key), value, opts, ct);
}
```

---

## Redis para sesiones distribuidas en .NET

```csharp
// Program.cs — sesiones guardadas en Redis (en lugar de en memoria)
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration.GetConnectionString("Redis"));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Nota: el CRM usa JWT, no sesiones de servidor.
// Las sesiones de Redis son útiles para flujos de onboarding multi-paso
// donde el estado temportal no cabe cómodamente en el JWT.
```

---

## Redis como SignalR Backplane

Sin Redis, los mensajes WebSocket solo llegan a los clientes de la misma instancia:

```
Instancia 1: tiene 300 conexiones WebSocket
Instancia 2: tiene 300 conexiones WebSocket

Evento: nuevo mensaje llega a Instancia 1
→ Solo los 300 clientes de Instancia 1 lo reciben
→ Los 300 clientes de Instancia 2 no saben nada
```

**Con Redis como backplane:**

```csharp
// Program.cs
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis")!,
        options => options.Configuration.ChannelPrefix =
            RedisChannel.Literal("mensajeria"));
```

```
Evento: nuevo mensaje llega a Instancia 1
→ Instancia 1 hace PUBLISH en Redis
→ Instancia 2 recibe (está suscrita al mismo canal)
→ Ambas instancias notifican a sus 300 clientes cada una
→ Los 600 clientes reciben el evento en tiempo real
```

---

## Redis para Rate Limiting (implementación real)

Usando el algoritmo Sliding Window Counter en Redis:

```csharp
public class RedisRateLimiter(IConnectionMultiplexer redis)
{
    public async Task<RateLimitResult> CheckAsync(
        string key, int limit, TimeSpan window, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowMs = (long)window.TotalMilliseconds;

        // Script Lua atómico: add + count en la misma operación
        var script = @"
            local now = tonumber(ARGV[1])
            local window = tonumber(ARGV[2])
            local limit = tonumber(ARGV[3])
            local key = KEYS[1]

            -- Remover entradas fuera de la ventana
            redis.call('ZREMRANGEBYSCORE', key, 0, now - window)

            -- Contar las que quedan
            local count = redis.call('ZCARD', key)

            if count < limit then
                -- Agregar esta petición
                redis.call('ZADD', key, now, now)
                redis.call('PEXPIRE', key, window)
                return {1, limit - count - 1}   -- permitido, restante
            else
                return {0, 0}                   -- denegado
            end";

        var result = (RedisResult[])await db.ScriptEvaluateAsync(
            script,
            new RedisKey[] { key },
            new RedisValue[] { now, windowMs, limit });

        return new RateLimitResult(
            Allowed: (int)result[0] == 1,
            Remaining: (int)result[1]);
    }
}
```

---

## Redis en el sistema de mensajería - mapa de uso

| Módulo | Qué se guarda en Redis | TTL | Tipo |
|--------|----------------------|-----|------|
| **Auth** | Sesión de onboarding multi-paso | 15 min | Hash |
| **Auth** | Blacklist de JWT revocados | Hasta expiración del JWT | Set |
| **Rate Limiting** | Contadores por usuario | 1 min | Sorted Set (Lua) |
| **Conversaciones** | Cache de bandeja de entrada | 5 min | String (JSON) |
| **Contactos** | Cache de perfil de contacto | 10 min | Hash |
| **Config** | Settings del tenant | 10 min | Hash |
| **SignalR** | Backplane para tiempo real | N/A (Pub/Sub) | Pub/Sub |
| **Campañas** | Cola de mensajes pendientes | Sin TTL | List |
| **Invitaciones** | Token OTP de invitación | 15 min | String |
| **Canales** | Credenciales de WhatsApp/Instagram | 30 min | Hash |

---

## Comandos Redis más usados - cheat sheet

```redis
# Clave/valor básico
SET key value [EX segundos]    # guardar con TTL opcional
GET key                        # leer
DEL key                        # borrar
EXISTS key                     # 1 si existe, 0 si no
TTL key                        # segundos restantes (-1=sin TTL, -2=no existe)
EXPIRE key segundos            # poner/actualizar TTL

# Debug y exploración
KEYS pattern          # NO usar en producción (bloquea Redis)
SCAN 0 MATCH "conv:*" COUNT 100   # Bien: alternativa segura
TYPE key              # string|hash|list|set|zset|stream
OBJECT ENCODING key   # cómo Redis almacena internamente

# Diagnóstico
INFO                  # info completa del servidor
INFO memory           # uso de RAM
INFO clients          # clientes conectados
INFO stats            # operaciones por segundo
DBSIZE                # número de claves en la DB actual
MONITOR               # stream de todos los comandos (solo en dev)

# Hash
HSET key campo valor [campo valor ...]
HGET key campo
HGETALL key
HDEL key campo
HMGET key campo1 campo2

# Listas (colas)
RPUSH key valor       # encolar al final
BLPOP key timeout     # desencolar del inicio (blocking)
LLEN key              # tamaño de la lista
```

---

## Anti-patrones comunes con Redis

```
No: Usar KEYS * en producción
   Bloquea Redis (single-threaded) mientras escanea millones de claves
   → Usa SCAN con COUNT en su lugar

No: Guardar objetos enormes como un solo String
   SET tenant:1:all-conversations "[...100MB de JSON...]"
   → Divide en claves individuales o usa Hash

No: Sin TTL en claves de cache
   El cache crece hasta llenar la RAM y empieza la evicción aleatoria
   → Siempre pon TTL en datos cacheables

No: Claves sin prefijo de tenant
   conv:42 accesible desde cualquier tenant
   → Siempre: conv:tenant:{id}:id:42

No: Conectar con new ConnectionMultiplexer() en cada petición
   Crear una conexión TCP cuesta 50-200ms — se nota
   → IConnectionMultiplexer como Singleton (una conexión, múltiples canales)

No: Usar SELECT para separar tenants
   Redis tiene 16 DBs (SELECT 0-15) — no escala para multi-tenancy real
   → Prefijos en la clave
```

---

## Resumen - Parte 9 - Redis

| Concepto | Lo esencial |
|----------|------------|
| **¿Qué es** | Base de datos en RAM - 100K-1M ops/seg - no reemplaza PostgreSQL |
| **Tipos de datos** | String - Hash - List - Set - Sorted Set - Stream - Pub/Sub |
| **TTL** | Toda clave de cache debe tener expiración - evita acumulación infinita |
| **Nombrado** | `módulo:tenant:{id}:entidad:{id}` - el prefijo de tenant es obligatorio |
| **IDistributedCache** | API de .NET para cache estándar (GET/SET/REMOVE + TTL) |
| **IConnectionMultiplexer** | Acceso de bajo nivel - Pub/Sub - Lua scripts - Singleton |
| **Cache-Aside** | Patrón más común: lee de Redis primero - miss -> va a DB -> guarda en Redis |
| **Write-Through** | Escribe en DB y Redis al mismo tiempo - consistencia inmediata |
| **Cache Stampede** | Jitter en TTL + Mutex para evitar thundering herd al expirar |
| **Pub/Sub** | SignalR backplane - eventos tiempo real entre instancias |
| **Evicción** | `volatile-lru` para mix cache+colas - `allkeys-lru` para cache puro |
| **Persistencia** | AOF para colas - sin persistencia para cache puro |
| **Docker** | `redis:7.4-alpine` en compose - `redis-cli MONITOR` para debug |
| **Sentinel** | Failover automático en producción - 3 sentinels mínimo |
| **Cuándo llega** | una sesión posterior - Caching, Background Services, OpenAPI |
