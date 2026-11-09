<!-- _class: lead -->

# S04  -  Patrones Creacionales + Estructurales

### Dev Notes

**Rogelio Arriaga  -  Raptor Dev Services**

---

## Agenda del día

- Revisión homework S03  -  PR review en vivo
- **Teoría:** patrones creacionales (5 patrones)
- **Teoría:** patrones estructurales (7 patrones)
- **Teoría:** patrones en el back-template  -  mapa completo
- **Teoría:** proceso de desarrollo  -  estimación y commits
- Code review  -  retrospectiva  -  homework

---

## La premisa del día

Los patrones de diseño no son recetas de libro.
Son **soluciones a problemas recurrentes** que ya están en tu código  -  a veces sin que lo sepas.

**Objetivo de hoy:** reconocer patrones en el back-template antes de aprenderlos en abstracto.

Entidades que usaremos hoy: `UserProfile`, `Contact`, `Conversation`, `Agent`, `Channel`.

---

<!-- _class: lead -->

# Parte 1
## Patrones Creacionales

*Cómo se crean los objetos*

---

## Singleton  -  un solo objeto, siempre el mismo

**Problema:** necesitas garantizar que solo exista una instancia de una clase.

```csharp
//  Sin patrón  -  múltiples instancias, estado inconsistente
var ctx1 = new TenantContextAccessor();
var ctx2 = new TenantContextAccessor();  // objeto distinto, estado diferente

//  DI como Singleton  -  el contenedor garantiza una sola instancia
// Host.Api/Program.cs
builder.Services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
// Cada componente que inyecte ITenantContextAccessor recibe el MISMO objeto
```

**En el back-template:**
`TenantContextAccessor` se registra Singleton porque el contexto de tenant
es compartido transversalmente  -  logs, EF Core, OpenTelemetry lo leen.

---

## Singleton  -  cuándo NO usar

```csharp
//  NUNCA Singleton con estado mutable por request
builder.Services.AddSingleton<AppDbContext>();   // memory leak  -  DbContext no es thread-safe

//  DbContext siempre Scoped  -  un objeto por request HTTP
// Host.Api/Program.cs
builder.Services.AddMainDatabase(builder.Configuration);
// Internamente: services.AddDbContext<AppDbContext>(...) -> Scoped
```

| Lifetime | Cuándo | Ejemplo |
|----------|--------|---------|
| Singleton | Estado global, sin mutación por request | `ITenantContextAccessor` |
| Scoped | Estado ligado a un request HTTP | `AppDbContext` |
| Transient | Sin estado, creación barata | `IValidator<T>` |

---

## Singleton  -  la señal

**Cuándo usarlo:**
- El objeto tiene estado global que NO cambia por petición: configuración, clave JWT, pool de conexiones
- Crearlo es costoso: conexión TCP, lectura de archivo, compilación de expresión regex
- Todos los consumidores necesitan el **mismo** objeto, no copias independientes

**Señal en el código:**

```csharp
// Señal: variable static con lazy init manual  -  frágil y no thread-safe
private static TenantContextAccessor? _instance;
public static TenantContextAccessor GetInstance()
    => _instance ??= new TenantContextAccessor();

// Solución: AddSingleton en DI  -  el contenedor garantiza una instancia, thread-safe
builder.Services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
```

**Nunca Singleton si:** el objeto lee `HttpContext`, tiene estado de usuario o tenant,
usa `DbContext`, o depende de datos de la petición HTTP actual.

---

## Factory Method  -  el creador decide la subclase

**Problema:** un método necesita crear un objeto, pero el tipo exacto depende de una condición.

```csharp
// Users.Application/UseCases/GetUserProfile/GetUserProfileHandler.cs
using Common.Messaging;
using Users.Application.UseCases.GetUserProfile.Responses;
using Users.Contracts.Dtos;
using Users.Domain.Repositories;

namespace Users.Application.UseCases.GetUserProfile;

public sealed class GetUserProfileHandler
    : IRequestHandler<GetUserProfileRequest, GetUserProfileResponse>
{
    private readonly IUserProfileRepository _profiles;
    public GetUserProfileHandler(IUserProfileRepository profiles) => _profiles = profiles;

    public async Task<GetUserProfileResponse> Handle(
        GetUserProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByPublicIdAsync(
            request.PublicId, request.TenantId, cancellationToken);

        // El Handler es el "factory"  -  decide qué subclase crear
        if (profile is null)
            return new GetUserProfileNotFoundFailure("Perfil de usuario no encontrado.");

        return new GetUserProfileSuccess(new UserProfileDto(
            profile.PublicId, profile.FullName, profile.IsActive,
            profile.CreatedAtUtc, profile.UpdatedAtUtc));
    }
}
```

---

## Factory Method  -  por qué es un patrón

```csharp
// El caller (InteractorPipeline) solo sabe que recibe GetUserProfileResponse
// No sabe si es Success o NotFoundFailure hasta que el Presenter lo inspecciona

// Users.Application/UseCases/GetUserProfile/Responses/GetUserProfileResponse.cs
public abstract record GetUserProfileResponse : IResponse;

// Users.Application/UseCases/GetUserProfile/Responses/GetUserProfileSuccess.cs
public sealed record GetUserProfileSuccess(UserProfileDto Data)
    : GetUserProfileResponse, ISuccess<UserProfileDto>;

// Users.Application/UseCases/GetUserProfile/Responses/GetUserProfileNotFoundFailure.cs
public sealed record GetUserProfileNotFoundFailure(string Message)
    : GetUserProfileResponse, INotFoundFailure;
```

El mismo patrón aplica a todos los use cases del CRM:
`GetContactSuccess` / `GetContactNotFoundFailure`,
`CreateConversationSuccess` / `CreateConversationValidationFailure`, etc.

---

## Factory Method  -  la señal

**Cuándo usarlo:**
- Un método crea objetos y el tipo exacto depende de una condición de negocio
- El caller solo debe conocer la clase base, no los tipos concretos posibles
- Necesitas agregar nuevos tipos de respuesta sin modificar el caller

**Señal en el código:**

```csharp
// Señal: if/switch que retorna distintos tipos del mismo ancestro
if (contact is null)           return new GetContactNotFoundFailure("No encontrado.");
if (!agent.CanReadContact)     return new GetContactForbiddenFailure("Sin acceso.");
if (!contact.BelongsToTenant)  return new GetContactForbiddenFailure("Tenant incorrecto.");
return new GetContactSuccess(new ContactDto(...));
// Cada rama crea un tipo diferente que hereda GetContactResponse
// El Presenter recibe GetContactResponse y no necesita saber cuál tipo llegó
```

**No usar si:** siempre creates el mismo tipo de objeto.
El valor del patrón está en el polimorfismo: distintos tipos, un contrato de retorno.

---

## Abstract Factory  -  familia de objetos relacionados

**Problema:** necesitas crear familias de objetos que trabajan juntos, sin acoplar al tipo concreto.

```csharp
// Abstract Factory en DI  -  la familia "repositorios para tests"
// Reemplaza toda la familia de una vez sin tocar el código de producción

public interface IRepositoryFactory
{
    IUserProfileRepository CreateUserProfileRepository();
    IContactRepository     CreateContactRepository();
    IConversationRepository CreateConversationRepository();
}
```

```csharp
// Producción  -  usa EF Core
public class EfCoreRepositoryFactory : IRepositoryFactory
{
    public IUserProfileRepository  CreateUserProfileRepository()  => new UserProfileRepository(_db);
    public IContactRepository      CreateContactRepository()      => new ContactRepository(_db);
    public IConversationRepository CreateConversationRepository() => new ConversationRepository(_db);
}
```

---

## Abstract Factory  -  implementación de tests

```csharp
// Tests  -  usa InMemory, sin tocar producción
public class InMemoryRepositoryFactory : IRepositoryFactory
{
    public IUserProfileRepository  CreateUserProfileRepository()
        => new InMemoryUserProfileRepository();
    public IContactRepository      CreateContactRepository()
        => new InMemoryContactRepository();
    public IConversationRepository CreateConversationRepository()
        => new InMemoryConversationRepository();
}

// En tests: cambiar TODA la familia en un solo lugar
services.AddSingleton<IRepositoryFactory, InMemoryRepositoryFactory>();
// Todos los handlers de Users, Contacts, Conversations ahora usan InMemory
//  -  sin modificar ningún Handler
```

---

## Abstract Factory  -  la señal

**Cuándo usarlo:**
- Necesitas familias de objetos que deben ser compatibles entre sí
- Quieres intercambiar toda la familia de una vez: producción, staging, tests
- El código cliente no debe saber con qué familia concreta trabaja

**Comparación con Factory Method:**

| | Factory Method | Abstract Factory |
|-|---------------|-----------------|
| Crea | 1 tipo de objeto | Una familia de tipos relacionados |
| Se cambia | Una implementación | Toda la familia a la vez |
| Señal | `if` que decide el tipo | N productos que deben ser compatibles entre sí |

```csharp
// Señal: cambias un repositorio y rompes la consistencia transaccional del resto
services.AddScoped<IUserProfileRepository, DapperUserProfileRepository>();
// Si los demás repositorios siguen en EF Core, no comparten la misma transacción

// Abstract Factory: cambias TODOS con una línea, garantizando consistencia
services.AddSingleton<IRepositoryFactory, DapperRepositoryFactory>();
```

---

## Builder  -  construcción paso a paso

**Problema:** crear un objeto complejo requiere muchos pasos opcionales.

```csharp
// Builder REAL en el back-template  -  Options Pattern con fluent API
// Host.Api/Program.cs
builder.Services
    .AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)   // paso 1: bindear sección
    .ValidateDataAnnotations()                      // paso 2: validar anotaciones
    .ValidateOnStart();                             // paso 3: validar al arrancar

// Cada paso es opcional y retorna el mismo builder  -  fluent interface
// Al final, el contenedor tiene un JwtSettings validado y configurado
```

---

## Builder  -  entidades de dominio en el CRM

```csharp
// Builder explícito para entidades de dominio
// Users.Infrastructure/Repositories/UserProfileRepository.cs
public async Task InsertAsync(
    Guid publicId, long tenantId, string fullName,
    CancellationToken cancellationToken = default)
{
    var profile = new UserProfile     // <- inicializador de objetos como Builder
    {
        PublicId     = publicId,
        TenantId     = tenantId,
        FullName     = fullName,
    };
    // IsActive, CreatedAtUtc, UpdatedAtUtc: valores por defecto en la entidad
    _db.UserProfiles.Add(profile);
    await _db.SaveChangesAsync(cancellationToken);
}
```

El mismo patrón aplica a `Contact`, `Conversation`, `Agent`, `Channel`:
cada `InsertAsync` construye la entidad paso a paso antes de persistirla.

---

## Builder  -  la señal

**Cuándo usarlo:**
- El constructor requiere 5+ parámetros, muchos opcionales
- La inicialización tiene pasos que deben ejecutarse en orden
- Quieres un API que deje claro qué configura cada paso

**Señal en el código:**

```csharp
// Señal: constructor telescópico  -  no queda claro qué significa cada argumento
var contact = new Contact(null, "Juan López", null, "la empresa",
                          null, null, "MX", true, null, null);
// El séptimo argumento, ¿es el país? ¿el idioma? -> Builder lo resuelve

// Con fluent Builder:
var contact = new ContactBuilder()
    .WithName("Juan López")
    .ForCompany("la empresa")
    .InCountry("MX")
    .SetActive(true)
    .Build();
```

**No usar si:** el objeto tiene 1-3 parámetros siempre requeridos.
Un constructor directo es más legible y más fácil de navegar en el IDE.

---

## Prototype  -  copiar en lugar de crear desde cero

**Problema:** crear un objeto nuevo es costoso; es más barato clonar uno existente y modificar lo que cambia.

```csharp
// Prototype con C# records  -  el operador 'with' crea una copia modificada

// Users.Application/UseCases/GetUserProfile/GetUserProfileRequest.cs
public sealed record GetUserProfileRequest(Guid PublicId, long TenantId)
    : IRequest<GetUserProfileResponse>;
```

```csharp
// En tests  -  clonar el request y cambiar solo un campo
var tenantId    = 1L;
var baseRequest = new GetUserProfileRequest(Guid.NewGuid(), tenantId);

var otroUsuario  = baseRequest with { PublicId = Guid.NewGuid() };   // Prototype
var otroTenant   = baseRequest with { TenantId = 2L };               // Prototype

// Mucho más limpio que new GetUserProfileRequest(...) repitiendo todos los campos
// El mismo patrón aplica a GetContactRequest, CreateConversationRequest, etc.
```

---

## Prototype  -  la señal

**Cuándo usarlo:**
- Necesitas muchas variaciones de un objeto base con diferencias pequeñas
- Crear el objeto desde cero es costoso: acceso a DB, cálculos complejos, I/O
- El código no debe depender de los tipos concretos de los objetos que copia

**Señal en el código:**

```csharp
// Señal: repites new con los mismos parámetros cambiando solo uno o dos campos
var page1 = new GetConversationsRequest(tenantId, AgentId: "ana", Status: null, Page: 1);
var page2 = new GetConversationsRequest(tenantId, AgentId: "ana", Status: null, Page: 2);
var page3 = new GetConversationsRequest(tenantId, AgentId: "ana", Status: null, Page: 3);
// Cuatro argumentos idénticos, solo Page cambia -> Prototype con 'with'

var page1 = new GetConversationsRequest(tenantId, AgentId: "ana", Status: null, Page: 1);
var page2 = page1 with { Page = 2 };
var page3 = page1 with { Page = 3 };
```

**No usar si:** el objeto es simple y casi todos los campos varían entre instancias.
Si todo cambia entre copias, clonar no ahorra nada respecto a `new` directo.

---

<!-- _class: lead -->

# Parte 2
## Patrones Estructurales

*Cómo se componen los objetos*

---

## Adapter  -  conectar interfaces incompatibles

**Problema:** tienes una clase existente con una interfaz que no coincide con lo que tu código espera.

```csharp
// Users.Domain/Repositories/IUserProfileRepository.cs
// El Domain define el contrato que Application necesita
namespace Users.Domain.Repositories;

public interface IUserProfileRepository   // <- lo que Application espera
{
    Task<UserProfile?> GetByPublicIdAsync(
        Guid publicId, long tenantId, CancellationToken cancellationToken = default);
    Task InsertAsync(
        Guid publicId, long tenantId, string fullName,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(
        Guid publicId, long tenantId, string fullName,
        CancellationToken cancellationToken = default);
    Task<bool> DisableAsync(
        Guid publicId, long tenantId, CancellationToken cancellationToken = default);
}
// EF Core tiene su propia API (DbSet<T>, LINQ)  -  incompatible directamente
```

---

## Adapter  -  la implementación concreta

```csharp
// Users.Infrastructure/Repositories/UserProfileRepository.cs
// UserProfileRepository ADAPTA AppDbContext a IUserProfileRepository
using Microsoft.EntityFrameworkCore;
using Shared.Database;
using Users.Domain.Entities;
using Users.Domain.Repositories;

namespace Users.Infrastructure.Repositories;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _db;
    public UserProfileRepository(AppDbContext db) => _db = db;

    public Task<UserProfile?> GetByPublicIdAsync(
        Guid publicId, long tenantId, CancellationToken cancellationToken = default) =>
        _db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.PublicId == publicId && e.IsActive, cancellationToken);
}
// AppDbContext (adaptee) -> UserProfileRepository (adapter) -> IUserProfileRepository (target)
```

---

## Adapter  -  la señal

**Cuándo usarlo:**
- Quieres integrar una librería externa sin que tu dominio dependa de ella directamente
- Tienes código legado que funciona bien pero con una interfaz incompatible
- Principio DIP: Application depende de abstracciones en Domain;
  Infrastructure adapta herramientas externas a esas abstracciones

**Señal en el código:**

```csharp
// Señal: el Handler referencia directamente tipos de una librería externa
public class GetContactHandler(AppDbContext _db)   // <- EF Core en Application
{
    // Si cambias a Dapper mañana, debes modificar este Handler
}

// Con Adapter:
public class GetContactHandler(IContactRepository _contacts)  // <- abstracción en Domain
{
    // AppDbContext queda oculto dentro de ContactRepository
    // Cambiar EF Core por Dapper = cambiar ContactRepository, no GetContactHandler
}
```

---

## Adapter vs Bridge  -  la diferencia que importa

Ambos separan una interfaz de su implementación. La diferencia está en el momento:

| | Adapter | Bridge |
|-|---------|--------|
| **Cuándo** | Después: arregla incompatibilidad existente | Antes: se diseña para extensibilidad |
| **Propósito** | Hacer funcionar dos cosas incompatibles | Permitir que abstracción e implementación crezcan por separado |
| **Quién inicia** | Incompatibilidad ya existe | Anticipas que ambos lados van a cambiar |

**La regla del Bridge: M x N clases -> M + N clases**

```
Sin Bridge: 3 tipos de Channel x 3 implementaciones de repo = 9 clases
  WhatsAppEfRepo, WhatsAppDapperRepo, WhatsAppInMemoryRepo,
  InstagramEfRepo, InstagramDapperRepo, ...

Con Bridge: 3 abstracciones de Channel + 3 implementaciones de repo = 6 clases
  Agregar un Channel nuevo = 1 clase. Agregar impl nueva = 1 clase.
```

---

## Bridge  -  separar abstracción de implementación

**Problema:** quieres que la abstracción y su implementación evolucionen independientemente.

```csharp
// Bridge en la capa de repositorios del CRM

// Abstracción (en Domain)  -  no cambia aunque cambie la base de datos:
// Users.Domain/Repositories/IUserProfileRepository.cs
public interface IUserProfileRepository
{
    Task<UserProfile?> GetByPublicIdAsync(
        Guid publicId, long tenantId, CancellationToken cancellationToken = default);
}
```

```csharp
// Implementación A  -  EF Core (producción):
// Users.Infrastructure/Repositories/UserProfileRepository.cs
public sealed class UserProfileRepository : IUserProfileRepository { /* AppDbContext */ }

// Implementación B  -  Dapper (lectura de alta performance):
public sealed class DapperUserProfileRepository : IUserProfileRepository { /* SQL explícito */ }

// Implementación C  -  InMemory (tests):
public sealed class InMemoryUserProfileRepository : IUserProfileRepository { /* Dictionary<Guid, UserProfile> */ }
```

---

## Bridge  -  impacto real en el Handler

```csharp
// Users.Application/UseCases/GetUserProfile/GetUserProfileHandler.cs
// El Handler solo conoce IUserProfileRepository  -  NUNCA la implementación concreta

public sealed class GetUserProfileHandler
    : IRequestHandler<GetUserProfileRequest, GetUserProfileResponse>
{
    private readonly IUserProfileRepository _profiles;  // <- abstracción

    public GetUserProfileHandler(IUserProfileRepository profiles) => _profiles = profiles;

    public async Task<GetUserProfileResponse> Handle(
        GetUserProfileRequest request, CancellationToken cancellationToken)
    {
        // Este código NO cambia si mañana se reemplaza EF Core por Dapper
        var profile = await _profiles.GetByPublicIdAsync(
            request.PublicId, request.TenantId, cancellationToken);
        // ...
    }
}
// Cambiar implementación = cambiar un línea en DI, cero cambios en el Handler
```

---

## Bridge  -  la señal

**Cuándo usarlo:**
- Necesitas que la abstracción y la implementación puedan cambiar de forma independiente
- Hay múltiples dimensiones de variación: tipo de canal x tipo de persistencia
- Quieres evitar una explosión de subclases por cada combinación posible

**Señal en el código:**

```csharp
// Señal: jerarquía que crece en dos dimensiones al mismo tiempo
class WhatsAppEfCoreRepository  : IChannelRepository { }
class WhatsAppDapperRepository  : IChannelRepository { }
class InstagramEfCoreRepository : IChannelRepository { }
class InstagramDapperRepository : IChannelRepository { }
// Agregar un canal nuevo = N clases nuevas. Agregar impl nueva = M clases nuevas.

// Con Bridge: Channel inyecta IRepository, los dos ejes son independientes
public class WhatsAppChannel(IChannelRepository _repo) { }
public class InstagramChannel(IChannelRepository _repo) { }
// Agregar canal nuevo = 1 clase. Agregar impl nueva = 1 clase.
```

---

## Composite  -  tratar objetos individuales y colecciones igual

**Problema:** necesitas trabajar con objetos individuales y grupos del mismo tipo uniformemente.

```csharp
// Composite en la jerarquía de Responses del back-template
// Todos los resultados comparten la misma base  -  el caller los trata igual

// Users.Application/UseCases/GetUserProfile/Responses/GetUserProfileResponse.cs
public abstract record GetUserProfileResponse : IResponse;   // Component (raíz)

// Users.Application/UseCases/GetUserProfile/Responses/GetUserProfileSuccess.cs
public sealed record GetUserProfileSuccess(UserProfileDto Data)
    : GetUserProfileResponse, ISuccess<UserProfileDto>;      // Leaf

// Users.Application/UseCases/GetUserProfile/Responses/GetUserProfileNotFoundFailure.cs
public sealed record GetUserProfileNotFoundFailure(string Message)
    : GetUserProfileResponse, INotFoundFailure;              // Leaf
```

---

## Composite  -  el Presenter lo aprovecha

```csharp
// Users.Presentation/Presenters/GetUserProfilePresenter.cs
// El Presenter maneja cualquier variante con el mismo Handle()
using Common.Messaging;
using Common.Results;
using Common.ViewModels;
using Users.Application.UseCases.GetUserProfile.Responses;
using Users.Contracts.Dtos;

namespace Users.Presentation.Presenters;

public sealed class GetUserProfilePresenter : INotificationHandler<GetUserProfileResponse>
{
    private readonly ResultViewModel<UsersController> _viewModel;
    public GetUserProfilePresenter(ResultViewModel<UsersController> viewModel)
        => _viewModel = viewModel;

    public Task Handle(GetUserProfileResponse notification, CancellationToken cancellationToken)
    {
        if (notification is IFailure failure)                    // <- IFailure PRIMERO
            _viewModel.Fail(failure.Message);
        else if (notification is ISuccess<UserProfileDto> success)
            _viewModel.Set(success);
        return Task.CompletedTask;
    }
}
// El Presenter no sabe cuál variante es  -  solo sabe que es GetUserProfileResponse
```

---

## Composite  -  orden del pattern matching importa

```csharp
//  INCORRECTO  -  ISuccess primero puede romper si hay ISuccess que también es IFailure
if (notification is ISuccess<UserProfileDto> success)
    _viewModel.Set(success);
else if (notification is IFailure failure)
    _viewModel.Fail(failure.Message);

//  CORRECTO  -  IFailure SIEMPRE primero
// INotFoundFailure, IValidationFailure, IConflictFailure todos heredan IFailure
// Si el Composite tiene más Leafs en el futuro, el orden garantiza que los
// errores se manejen antes que los casos de éxito
if (notification is IFailure failure)
    _viewModel.Fail(failure.Message);
else if (notification is ISuccess<UserProfileDto> success)
    _viewModel.Set(success);
```

**Regla:** en todos los Presenters del CRM, `IFailure` va primero.

---

## Composite  -  la señal

**Cuándo usarlo:**
- El modelo tiene estructura jerárquica: permisos anidados, categorías, árboles de flujos de bot
- Quieres que el código cliente trate un nodo hoja y un subárbol exactamente igual
- Agregar nuevos tipos de "hoja" no debe cambiar el código que los procesa

**Señal en el código:**

```csharp
// Señal: código duplicado para individuos y colecciones
if (response is GetContactSuccess s)
    HandleSuccess(s);
if (responses is List<GetContactSuccess> list)
    foreach (var r in list) HandleSuccess(r);

// Con Composite: el Presenter no distingue
public Task Handle(GetContactResponse n, CancellationToken ct)
{
    if (n is IFailure f)                    _viewModel.Fail(f.Message);
    else if (n is ISuccess<ContactDto> ok)  _viewModel.Set(ok);
    return Task.CompletedTask;
    // Agregar GetContactConflictFailure no requiere cambiar el Presenter
}
```

---

## Decorator  -  agregar comportamiento sin modificar la clase

**Problema:** quieres agregar responsabilidades a un objeto dinámicamente, sin heredar.

```csharp
// El MEJOR ejemplo de Decorator en el back-template: InteractorPipeline
// Decora IRequestHandler<TRequest, TResponse> con logging + publishing

public sealed class InteractorPipeline<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest  : IRequest<TResponse>
    where TResponse : IResponse
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        _logger.LogInformation("{@Request}", request);   // comportamiento agregado ANTES
        var response = await next();                      // el handler real (decorado)
        _logger.LogInformation("{@Response}", response); // comportamiento agregado DESPUÉS
        await _mediator.Publish(response, ct);            // comportamiento agregado DESPUÉS
        return response;
    }
}
// GetUserProfileHandler no sabe que está siendo decorado  -  SRP respetado
```

---

## Decorator  -  la señal

**Cuándo usarlo:**
- Quieres agregar logging, caching o autorización sin modificar la clase original
- El comportamiento adicional es ortogonal a la responsabilidad principal del objeto
- Necesitas combinar múltiples comportamientos adicionales de forma flexible

**Diferencia con herencia:**

| Herencia | Decorator |
|---------|-----------|
| Estática: definida en compilación | Dinámica: compuesta en runtime |
| 3 behaviors x 4 handlers = 12 subclases | 3 Decorators + 4 handlers = 7 clases |
| El comportamiento se decide en compile-time | El comportamiento se apila en DI |

**Señal en el código:**

```csharp
// Señal: copias el mismo bloque de logging en cada Handler
public async Task<GetContactResponse> Handle(GetContactRequest req, CancellationToken ct)
{
    _logger.LogInformation("{@Request}", req);   // copiado en TODOS los handlers
    var result = ...;
    _logger.LogInformation("{@Response}", result);
    return result;
}
// Decorator (InteractorPipeline): log una vez, aplica a todos sin tocarlos
```

---

## Facade  -  interfaz simple sobre un sistema complejo

**Problema:** un subsistema tiene muchas clases y la coordinación es compleja.

```csharp
// Facade en BaseApiController
// Sistema real: Request -> InteractorPipeline -> Handler -> Publish -> Presenter -> ViewModel
// La Facade: una sola llamada a Mediator.Send()

// Users.Presentation/Controllers/UsersController.cs
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
{
    try
    {
        var result = await Mediator.Send(
            new GetUserProfileRequest(id, CurrentTenantId), ct);
        if (_viewModel.IsSuccess) return Ok(_viewModel);
        return result is GetUserProfileNotFoundFailure
            ? NotFound(_viewModel)
            : StatusCode(500, _viewModel);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error en GetById User");
        return StatusCode(500, _viewModel.Fail(ex.Message));
    }
}
// 4 líneas ocultan: validación, pipeline, handler, repositorio, presenter, viewmodel
```

---

## Facade  -  la señal

**Cuándo usarlo:**
- Para hacer "algo simple" necesitas coordinar 5+ objetos en el orden correcto
- El mismo flujo de orquestación se repite en múltiples callers
- Quieres aislar a los callers de los cambios internos del subsistema

**Señal en el código:**

```csharp
// Señal: el Controller coordina directamente los objetos internos del subsistema
var handler   = new GetUserProfileHandler(_profiles);
var pipeline  = new InteractorPipeline<...>(_logger, _mediator);
var presenter = new GetUserProfilePresenter(_viewModel);
var result    = await pipeline.Handle(request, () => handler.Handle(request, ct), ct);
await presenter.Handle(result, ct);
return _viewModel.IsSuccess ? Ok(_viewModel) : NotFound(_viewModel);
// Esto se repetiría en cada Controller -> Facade lo oculta

// Con Facade: el subsistema desaparece para el Controller
var result = await Mediator.Send(new GetUserProfileRequest(id, tenantId), ct);
return _viewModel.IsSuccess ? Ok(_viewModel) : NotFound(_viewModel);
```

**Costo:** la Facade puede crecer hasta ser un "God Object".
Debe delegar trabajo, no hacer trabajo.

---

## Flyweight  -  compartir estado para reducir memoria

**Problema:** tienes muchos objetos similares que consumen mucha memoria por duplicar estado compartido.

```csharp
// Flyweight en .NET  -  casos reales que el back-template aprovecha

// 1. string interning  -  .NET deduplica strings literales automáticamente
var role1 = "admin";
var role2 = "admin";
// object.ReferenceEquals(role1, role2) == true  -  mismo objeto en memoria

// 2. Enum values  -  un solo objeto por valor, compartido en toda la app
public enum AgentRole { Admin, Agent, Viewer }
// AgentRole.Admin siempre es el mismo valor en memoria

// 3. IOptions<T> Singleton  -  configuración cargada una vez, compartida por toda la app
// Host.Api/Program.cs
builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateOnStart();
// Un solo JwtSettings en memoria  -  todas las clases que lo inyecten comparten el mismo
```

---

## Flyweight  -  la señal

**Cuándo usarlo:**
- Tienes miles o millones de objetos similares y la memoria es el cuello de botella
- Los objetos comparten la mayor parte de su estado; solo una fracción varía por instancia
- Puedes separar el estado compartible (intrínseco) del estado variable (extrínseco)

**Estado intrínseco vs extrínseco en el CRM:**

| Objeto | Intrínseco (compartido) | Extrínseco (por instancia) |
|--------|------------------------|---------------------------|
| Configuración JWT | Issuer, Audience, clave secreta | El token de cada usuario |
| Plantilla de email | HTML, CSS, layout | Nombre del destinatario, link |
| `AgentRole` enum | Nombre del rol, permisos | Qué agente tiene ese rol |

**Señal en el código:**

```csharp
// Señal: creas el mismo "template" miles de veces con una pequeña variación
var n1 = new PushNotification("Tienes un mensaje nuevo", "es-MX", "push", userId1);
var n2 = new PushNotification("Tienes un mensaje nuevo", "es-MX", "push", userId2);
// Los primeros 3 campos son idénticos -> Flyweight los comparte, solo userId varía
```

**No usar si:** el número de objetos es manejable. La complejidad adicional no se justifica.

---

## Proxy  -  controlar el acceso a un objeto

**Problema:** quieres controlar, decorar o diferir el acceso a un objeto costoso.

```csharp
// ResultViewModel<T> como Proxy del Response en el back-template
// El Controller NUNCA accede al Response directamente  -  pasa por el ViewModel

public class ResultViewModel<T>
{
    public object? Data      { get; private set; }
    public bool    IsSuccess { get; private set; }
    public string? Message   { get; private set; }

    // El Presenter popula el proxy
    public void Set<TData>(ISuccess<TData> success)
        { Data = success.Data; IsSuccess = true; }
    public ResultViewModel<T> Fail(string msg)
        { Message = msg; IsSuccess = false; return this; }
}
```

```csharp
// El Controller lee el proxy  -  nunca la Response original
if (_viewModel.IsSuccess) return Ok(_viewModel);
return result is GetUserProfileNotFoundFailure ? NotFound(_viewModel) : StatusCode(500, _viewModel);
// El Proxy desacopla el tipo de Response (dominio) del código de status HTTP
```

---

## Proxy  -  tres tipos que ya usas

**Cuándo usarlo:**
- Quieres controlar, diferir o enriquecer el acceso a un objeto sin que el caller lo note
- El objeto real es costoso de crear, está en red, o requiere permisos especiales

**Los tres tipos de Proxy en el CRM:**

| Tipo | Propósito | Ejemplo real |
|------|-----------|-------------|
| **Virtual** | Difiere la creación hasta que se necesita | `Lazy<T>`, lazy loading de EF Core |
| **Protection** | Controla quién puede acceder y qué puede ver | `ResultViewModel<T>`, `[Authorize]` |
| **Remote** | Representa un objeto en otro proceso o red | `HttpClient`, cliente de WhatsApp API |

**Diferencia con Decorator:**

| Decorator | Proxy |
|-----------|-------|
| Agrega comportamiento al objeto | Controla el acceso al objeto |
| El cliente construye el Decorator | El Proxy decide si crear el objeto real |
| "Hace mas" | "Decide si dejas pasar" |

`ResultViewModel<T>` es un **Protection Proxy**: el Controller nunca accede
al `Response` de dominio directamente, solo a lo que el Proxy expone.

---

## Los 10 patrones en el back-template  -  mapa

| Patrón | Dónde vive en el back-template |
|--------|-------------------------------|
| Singleton | `AddSingleton<ITenantContextAccessor, TenantContextAccessor>()` |
| Factory Method | Handler: `return new GetUserProfileNotFoundFailure(...)` |
| Abstract Factory | Familia de repos (EF Core / InMemory / Dapper) |
| Builder | `AddOptions<JwtSettings>().BindConfiguration().ValidateOnStart()` |
| Prototype | `record with { }` en tests  -  copiar Request modificando un campo |
| Adapter | `UserProfileRepository` adapta `AppDbContext` a `IUserProfileRepository` |
| Bridge | `IUserProfileRepository` (Domain) vs `UserProfileRepository` (Infrastructure) |
| Composite | `GetUserProfileResponse` -> `Success` / `NotFoundFailure` |
| Decorator | `InteractorPipeline` envuelve el Handler con log + publish |
| Facade | `Mediator.Send(new GetUserProfileRequest(...))` oculta todo el pipeline |
| Flyweight | String interning, `AgentRole` enum, `IOptions<JwtSettings>` singleton |
| Proxy | `ResultViewModel<T>`  -  buffer entre Response de dominio y HTTP output |

---

## Estructura de un módulo CRM  -  dónde vive cada patrón

```
Modules/Contacts/
|-- Contacts.Domain/
|   |-- Entities/Contact.cs                         <- entidad (Builder target)
|   `-- Repositories/IContactRepository.cs          <- Bridge abstracción
|
|-- Contacts.Application/UseCases/GetContact/
|   |-- GetContactRequest.cs                        <- Prototype (record with {})
|   |-- GetContactHandler.cs                        <- Factory Method
|   `-- Responses/
|       |-- GetContactResponse.cs                   <- Composite raíz
|       |-- GetContactSuccess.cs                    <- Composite Leaf
|       `-- GetContactNotFoundFailure.cs            <- Composite Leaf
|
|-- Contacts.Contracts/Dtos/ContactDto.cs
|-- Contacts.Infrastructure/
|   `-- Repositories/ContactRepository.cs           <- Adapter + Bridge impl.
`-- Contacts.Presentation/
    |-- Controllers/ContactsController.cs            <- Facade
    `-- Presenters/GetContactPresenter.cs           <- Proxy consumer
```

---

## Regla práctica: ¿cuándo usar cada familia?

| Necesitas... | Familia | Patrón |
|-------------|---------|--------|
| Controlar cómo se crea un objeto | Creacional | Singleton / Factory / Builder |
| Una sola instancia global | Creacional | Singleton |
| Copiar un objeto con variaciones | Creacional | Prototype |
| Conectar interfaces incompatibles | Estructural | Adapter |
| Agregar comportamiento sin modificar | Estructural | Decorator |
| Simplificar un subsistema complejo | Estructural | Facade |
| Compartir estado entre muchos objetos | Estructural | Flyweight |
| Controlar el acceso a un objeto | Estructural | Proxy |

---

<!-- _class: lead -->

# Parte 2B  -  DI Lifetimes

## Singleton  -  Scoped  -  Transient  -  Captive Dependency

> Esta sección cierra ese gap de una vez.

---

## Los tres lifetimes del contenedor de DI

| Lifetime | Se crea | Se destruye | Instancias |
|----------|---------|------------|-----------|
| **Singleton** | Una vez al arrancar la app | Al apagar la app | 1 en toda la app |
| **Scoped** | Una vez por petición HTTP | Al terminar la petición | 1 por petición |
| **Transient** | Cada vez que se pide | Al salir del scope del consumidor | N por petición |

```csharp
// Host.Api/Program.cs  -  los tres lifetimes en uso real
builder.Services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddTransient<IPasswordHasher, BcryptPasswordHasher>();
```

---

## Singleton  -  exactamente una instancia

```csharp
//  Qué va como Singleton
builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect(config["Redis"]!));
// -> La conexión TCP a Redis es costosa. Una sola para toda la app.

builder.Services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
// -> Lee del HttpContext  -  es thread-safe por diseño.
```

**¿Qué NUNCA va como Singleton?**

```csharp
//  MAL  -  DbContext como Singleton
builder.Services.AddSingleton<AppDbContext>(...);
// DbContext NO es thread-safe. Si dos peticiones lo usan simultáneamente -> excepción.
// O peor: comportamiento silenciosamente incorrecto (datos de otra petición filtrados).
```

> Regla: si el objeto guarda estado interno que varía por petición
> (usuario actual, tenant, transacción de DB) -> NUNCA Singleton.

---

## Scoped  -  una instancia por petición HTTP

```csharp
//  Qué va como Scoped
builder.Services.AddScoped<AppDbContext>(...);
// -> Una instancia por petición. La transacción de EF Core vive exactamente una petición.

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
// -> Lee claims del JWT de esta petición. Distinta para cada usuario.

// En el back-template  -  todos los Handlers son Scoped:
builder.Services.AddScoped<GetUserProfileHandler>();
// -> El Handler comparte el DbContext de la petición.
```

**¿Qué NUNCA va como Scoped?**

```csharp
//  Inyectar un Scoped dentro de un Singleton -> Captive Dependency
// El Singleton "captura" la instancia Scoped del primer request
// y la reutiliza en TODOS los requests siguientes.
```

---

## Transient  -  nueva instancia cada vez

```csharp
//  Qué va como Transient
builder.Services.AddTransient<IPasswordHasher, BcryptPasswordHasher>();
// -> Sin estado. Crear una instancia por uso es barato.

builder.Services.AddTransient<IEmailRenderer, RazorEmailRenderer>();
// -> Renderiza un template de email. Sin estado compartido.
```

**Cuándo preferir Transient sobre Scoped:**
- El objeto es stateless y crearlo es barato
- No necesitas que la misma instancia se comparta dentro de la petición
- Cada consumidor necesita su propia instancia (para evitar side effects)

**Cuándo Transient puede ser problema:**
- Si el objeto hace algo costoso al crearse (conexión TCP, archivo grande)
- Si tiene `IDisposable` -> cada instancia se disposa al salir de su scope

---

## El bug de Captive Dependency  -  el más silencioso

**Definición:** inyectar un servicio de lifetime corto en un servicio de lifetime largo.
El contenedor de .NET lanza excepción en desarrollo  -  pero puede ser silencioso en producción si no activas la validación.

```csharp
//  Captive Dependency  -  Singleton captura un Scoped
public class NotificationService(ICurrentUserService _userService) // Scoped
{
    // _userService fue creado para la petición 1
    // Esta instancia de NotificationService es Singleton
    // La petición 2 llega -> _userService sigue siendo la de la petición 1
    // -> El usuario de la petición 1 recibe notificaciones del usuario 2
}

builder.Services.AddSingleton<NotificationService>(); // <- el error
```

**El runtime de ASP.NET Core lo detecta si activas la validación:**

```csharp
// Program.cs  -  SIEMPRE activar en desarrollo
builder.Services.Configure<ServiceProviderOptions>(options =>
{
    options.ValidateScopes = builder.Environment.IsDevelopment();
    options.ValidateOnBuild = builder.Environment.IsDevelopment();
});
// InvalidOperationException al arrancar: "Cannot consume scoped service from singleton"
```

---

## Captive Dependency  -  diagrama del error

```
Petición 1 (usuario Ana):
  -> Singleton "NotificationService" se crea
  -> Captura Scoped "CurrentUserService" { userId: "ana@example.com" }

Petición 2 (usuario Beto):
  -> Singleton "NotificationService" ya existe  -  se reutiliza
  -> CurrentUserService capturado sigue siendo el de Ana
  -> Resultado: Beto recibe emails como si fuera Ana
```

**Cómo detectarlo antes de que pase:**
1. `ValidateScopes = true` en desarrollo -> excepción al arrancar
2. Test unitario que construye el ServiceProvider y llama `BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true })`

---

## Reglas prácticas de lifetime en el sistema de mensajería

| Servicio | Lifetime | Razón |
|---------|---------|-------|
| `AppDbContext` | Scoped | Una transacción por petición |
| `IUserProfileRepository` | Scoped | Usa AppDbContext |
| `IConversationRepository` | Scoped | Usa AppDbContext |
| `ICurrentUserService` | Scoped | Depende del JWT de la petición |
| `ICurrentTenantService` | Scoped | Depende del JWT de la petición |
| `IConnectionMultiplexer` (Redis) | Singleton | Conexión TCP reutilizable |
| `IPasswordHasher` | Transient | Stateless, barato de crear |
| `IEmailRenderer` | Transient | Stateless, sin estado compartido |
| `GetUserProfileHandler` | Scoped | Usa repositorios Scoped |
| `ITenantContextAccessor` | Singleton | Thread-safe, sin estado de petición |

---

## Background Services  -  el caso especial

Los `IHostedService` (BackgroundService) son Singleton por naturaleza.
Si necesitan usar un DbContext Scoped, no pueden inyectarlo directamente.

```csharp
//  MAL  -  DbContext inyectado en BackgroundService
public class CampaignWorker(AppDbContext _db) : BackgroundService
// -> AppDbContext es Scoped. El BackgroundService es Singleton.
// -> Captive Dependency -> excepción o comportamiento incorrecto.

//  BIEN  -  usar IServiceScopeFactory
public class CampaignWorker(IServiceScopeFactory _scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // db es un Scoped "fresco" creado para este ciclo del worker
            await ProcessCampaignAsync(db, ct);
        }
    }
}
```

---

## DI Lifetimes  -  resumen visual

```
Lifetime    | Petición 1        | Petición 2        | Petición 3
------------+-------------------+-------------------+-------------------
Singleton   |  instancia A     |  instancia A     |  instancia A
            |   (la misma)      |   (la misma)      |   (la misma)
------------+-------------------+-------------------+-------------------
Scoped      |  instancia B     |  instancia C     |  instancia D
            |   (nueva por req) |   (nueva por req) |   (nueva por req)
------------+-------------------+-------------------+-------------------
Transient   |  B, C, D       |  E, F, G       |  H, I, J
            |   (nueva por uso) |   (nueva por uso) |   (nueva por uso)
```

> Singleton = proceso  -  Scoped = petición  -  Transient = inyección

---

<!-- _class: lead -->

# Parte 3
## Proceso de Desarrollo
### Estimación, Commits y Stakeholders

---

## Estimación con Story Points

Story points miden **complejidad relativa**, no horas absolutas.

| Puntos | Significado | Ejemplo (Sistema de mensajería) |
|--------|-------------|------------------------|
| 1-2 | Trivial/Simple | Cambiar un texto, agregar campo a perfil |
| 3-5 | Pequeño/Medio | Filtro por estado, asignación automática |
| 8 | Complejo | Integración con API de WhatsApp |
| 13 | Muy complejo | Flujo de billing completo |
| 21 | Demasiado grande | **Dividir en historias más pequeñas** |

**Planning Poker:** votar simultáneamente  -  las divergencias revelan riesgos ocultos, no diferencias de habilidad.

---

## Errores comunes de estimación

```
 "Esta historia toma 8 horas"
   Las horas son exactas para trabajo repetitivo,
   no para trabajo creativo o con incertidumbre.

 El tech lead dice "es fácil" y todos asienten
   Se pierde el riesgo que el dev junior habría detectado.

 PO: "¿No puede ser 3 en lugar de 8?"
   Los story points no se negocian. El scope sí.
```

**Triangulación:** antes de estimar una historia nueva, compararla con historias ya completadas del mismo equipo  -  eso convierte el "adivinar" en "comparar con evidencia real".

---

## Conventional Commits  -  Historial legible

Formato estándar para mensajes de commit:

```
type(scope): descripción breve en imperativo

[cuerpo opcional  -  el por qué, no el qué]
[footer: BREAKING CHANGE: ..., Closes #123]
```

**Tipos principales:**

| Tipo | Cuándo usarlo |
|------|--------------|
| `feat` | Nueva funcionalidad visible al usuario |
| `fix` | Corrección de bug |
| `refactor` | Cambio interno sin cambio de comportamiento |
| `test` | Añadir o corregir tests |
| `chore` | Actualización de dependencias, configuración |
| `docs` | Solo documentación |

---

## Por qué commits atómicos

Un commit = un concepto. No mezclar features, fixes y refactors en el mismo commit.

```
 Commit atómico  -  fácil de entender y revertir:
   feat(conversations): agregar filtro por estado de conversación

 Commit mezclado  -  difícil de auditar:
   varios cambios y también fix del login y refactor del repo
```

**Beneficios concretos:**
- `git bisect` funciona  -  encontrar cuándo se introdujo un bug en segundos
- `git revert` limpio  -  deshacer un cambio sin afectar otros
- Code review más rápido  -  el reviewer entiende el propósito de cada commit
- `CHANGELOG.md` automático con `conventional-changelog`

---

## Stakeholders en el desarrollo

No todos los stakeholders quieren lo mismo  -  ajustar la comunicación:

| Stakeholder | Le importa | Cómo comunicar |
|-------------|------------|---------------|
| **Product Owner** | Features, fechas, ROI | Impacto en el usuario, velocidad del equipo |
| **CTO / Arquitecto** | Decisiones técnicas, deuda, escalabilidad | ADRs, métricas de calidad, riesgos |
| **Usuario final** | Que funcione rápido y sin bugs | UX, uptime, tiempo de respuesta |
| **Management** | Fechas, costos, riesgos de negocio | Milestone dates, burn rate, riesgos con mitigaciones |
| **Dev Team** | DX, autonomía, código limpio | Deuda técnica, herramientas, retrospectivas |

**Regla de oro:** habla el idioma de tu audiencia. Lo que para un dev es "complejidad ciclomática", para el PO es "cada nueva feature tarda el doble de lo que debería".

---

## Homework S04

### Lecturas (1-2 horas)
- `dev-notes/02-programacion/design-patterns/creational/01-05`
- `dev-notes/02-programacion/design-patterns/structural/06-12`

### Ejercicio (2 horas)
Implementar **dos patrones** usando entidades del sistema de mensajería:
1. `AbstractFactory`  -  `IRepositoryFactory` con implementaciones `EfCoreRepositoryFactory` e `InMemoryRepositoryFactory` para `Contact` y `Conversation`
2. `Prototype`  -  en los tests de `GetUserProfileHandler`, clonar el request base con `with { }` para probar distintos `PublicId` y `TenantId`

Ver `S04/homework.md` para el detalle completo.

---

## S05  -  Preview

**Patrones de comportamiento: el flujo del back-template explicado**

- Command, Mediator, Observer  -  ya los usas, ahora sabes por qué se llaman así
- Strategy  -  asignación intercambiable de conversaciones a agentes sin tocar el Handler
- Chain of Responsibility  -  el pipeline de behaviors en detalle
- Clean Code  -  naming, funciones, comentarios en el contexto del back-template

---

<!-- _class: lead -->

# ¡Patrones en acción!
## Mañana identificarás estos 10 patrones en el back-template
### Guía en S04/hands-on.md
