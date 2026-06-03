# 03: Constructores y Dependency Injection

El constructor y la inyección de dependencias son inseparables en este proyecto. Este documento los explica juntos, como se usan en la práctica.

> Fuente: *C# 13 and .NET 9: Modern Cross-Platform Development* (Mark J. Price). Ch.5 Constructors; Ch.14 Dependency Injection

---

## ¿Qué es un constructor?

El constructor es un método especial que se ejecuta automáticamente cuando creas un objeto con `new`. Su trabajo: inicializar el objeto y recibir las dependencias que necesita.

```csharp
public sealed class ExampleUsersSql
{
    // Campo privado — donde guardamos la dependencia
    private readonly MainDapperDbConnection _db;

    // Constructor — mismo nombre que la clase, sin tipo de retorno
    public ExampleUsersSql(MainDapperDbConnection db)
    {
        _db = db;  // asignar la dependencia recibida al campo
    }
    
    // Ahora _db está disponible en todos los métodos
    public Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default)
        => _db.QuerySingleAsync<ExampleUser>(/* ... */);
}
```

---

## Tipos de constructores

### Constructor por defecto (implícito)

Si no defines ningún constructor, C# genera uno vacío automáticamente:

```csharp
public class Configuracion
{
    public string Nombre { get; set; }
    public int Timeout { get; set; }
}

// C# genera implícitamente:
// public Configuracion() { }

var config = new Configuracion();  // funciona sin constructor explícito
config.Nombre = "API";
```

**Nota:** en cuanto defines cualquier constructor, el implícito desaparece.

### Constructor con parámetros

El más común en este proyecto:

```csharp
public sealed class GetExampleUserHandler
    : IRequestHandler<GetExampleUserRequest, GetExampleUserResponse>
{
    private readonly IExampleUserRepository _repo;
    private readonly ILogger<GetExampleUserHandler> _logger;

    public GetExampleUserHandler(
        IExampleUserRepository repo,
        ILogger<GetExampleUserHandler> logger)
    {
        _repo   = repo;
        _logger = logger;
    }
}
```

### Constructor con validación

```csharp
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly int    _expirationMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        // Validar en el constructor — falla rápido si la config está mal
        _key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key no configurado.");
        
        _issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer no configurado.");
        
        _expirationMinutes = configuration.GetValue<int>("Jwt:ExpirationMinutes", 60);
    }
}
```

### Constructor encadenado: `this()`

Un constructor llama a otro de la misma clase:

```csharp
public sealed class ConexionConfig
{
    public string Host { get; }
    public int Puerto { get; }
    public string BaseDeDatos { get; }

    // Constructor completo
    public ConexionConfig(string host, int puerto, string baseDeDatos)
    {
        Host = host;
        Puerto = puerto;
        BaseDeDatos = baseDeDatos;
    }

    // Constructor con defaults — llama al completo con this()
    public ConexionConfig(string host) 
        : this(host, 5432, "main_db")
    { }

    // Constructor mínimo
    public ConexionConfig() 
        : this("localhost")
    { }
}

var c1 = new ConexionConfig();                     // host=localhost, port=5432, db=main_db
var c2 = new ConexionConfig("db.ejemplo.com");     // host=db.ejemplo.com, port=5432, db=main_db
var c3 = new ConexionConfig("db.ejemplo.com", 5433, "prod_db");
```

### Constructor base: `base()`

Llama al constructor del padre:

```csharp
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator Mediator;
    
    // Constructor del padre
    protected BaseApiController(IMediator mediator)
    {
        Mediator = mediator;
    }
}

public sealed class ExampleUsersController : BaseApiController
{
    private readonly ILogger<ExampleUsersController> _logger;
    private readonly ResultViewModel<ExampleUsersController> _viewModel;

    public ExampleUsersController(
        IMediator mediator,                                    // para el padre
        ILogger<ExampleUsersController> logger,
        ResultViewModel<ExampleUsersController> viewModel)
        : base(mediator)   // ← llama al constructor de BaseApiController
    {
        _logger    = logger;
        _viewModel = viewModel;
    }
}
```

### Constructor estático

Se ejecuta una sola vez antes del primer uso de la clase. No tiene parámetros, no se puede llamar directamente.

```csharp
public sealed class ProductMetrics
{
    private static readonly Meter Meter;
    public static readonly Counter<long> ProductsCreated;
    
    // Constructor estático — inicializa miembros estáticos
    static ProductMetrics()
    {
        Meter = new Meter("back-template.products");
        ProductsCreated = Meter.CreateCounter<long>(
            "products_created_total", "count", "Total de productos creados");
    }
}
```

---

## Primary Constructor: C# 12+

La forma más concisa de declarar un constructor. Los parámetros van en la declaración de la clase.

### Sintaxis básica

```csharp
// Constructor tradicional
public sealed class ExampleUsersSql
{
    private readonly MainDapperDbConnection _db;
    
    public ExampleUsersSql(MainDapperDbConnection db)
    {
        _db = db;
    }
    
    public Task<ExampleUser?> GetByPublicIdAsync(...) => _db.QuerySingleAsync<ExampleUser>(...);
}

// Primary constructor — equivalente, mucho más corto
public sealed class ExampleUsersSql(MainDapperDbConnection db)
{
    // "db" está disponible en toda la clase como si fuera un campo
    public Task<ExampleUser?> GetByPublicIdAsync(...) => db.QuerySingleAsync<ExampleUser>(...);
}
```

### Con múltiples parámetros

```csharp
public sealed class GetExampleUserHandler(
    IExampleUserRepository repo,
    ILogger<GetExampleUserHandler> logger)
    : IRequestHandler<GetExampleUserRequest, GetExampleUserResponse>
{
    public async Task<GetExampleUserResponse> Handle(
        GetExampleUserRequest request, CancellationToken cancellationToken)
    {
        // Uso directo de "repo" y "logger" — sin campos privados explícitos
        logger.LogInformation("Buscando usuario {UserId}", request.PublicId);
        
        var user = await repo.GetByPublicIdAsync(request.PublicId, cancellationToken);
        
        if (user is null)
            return new GetExampleUserNotFoundFailure("Usuario no encontrado.");
        
        return new GetExampleUserSuccess(new ExampleUserDto(
            user.PublicId, user.FullName, user.Email,
            user.IsActive, user.CreatedAtUtc, user.UpdatedAtUtc));
    }
}
```

### Primary constructor + campo explícito

Cuando necesitas almacenar el parámetro con un nombre diferente o con lógica:

```csharp
public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    // Extraemos y guardamos explícitamente — el primary constructor recibe IConfiguration
    // pero almacenamos solo lo que necesitamos
    private readonly string _key = configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key no configurado.");
    
    private readonly string _issuer = configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("Jwt:Issuer no configurado.");
    
    private readonly int _expMinutes = configuration.GetValue<int>("Jwt:ExpirationMinutes", 60);
    
    public string Generate(Guid userId, string email)
    {
        // Usa los campos privados, no "configuration" directamente
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        // ...
    }
}
```

### Cuándo usar primary constructor vs tradicional

| Situación | Primary Constructor | Tradicional |
|-----------|--------------------|----|
| Asignar parámetros directamente | ✓ Ideal | Verboso |
| Necesitas nombres distintos para los campos | Más verboso | ✓ Natural |
| Necesitas validación en el constructor | Posible con campo inicializador | ✓ Más claro |
| Herencia con `base()` | ✓ Funciona | ✓ Funciona |
| Múltiples constructores (sobrecarga) | ✗ Solo uno | ✓ Posible |

---

## Dependency Injection: el problema del `new`

### Sin DI: código acoplado

Imagina que escribes un handler sin DI:

```csharp
// ❌ Handler que crea sus propias dependencias
public sealed class GetExampleUserHandler
{
    public async Task<GetExampleUserResponse> Handle(GetExampleUserRequest request, CancellationToken ct)
    {
        // Para crear el repositorio necesito saber toda la cadena:
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json").Build();
        
        var factory = new MainDbConnectionFactory(config);
        var logger = LoggerFactory.Create(b => b.AddConsole())
            .CreateLogger<MainDapperDbConnection>();
        var db = new MainDapperDbConnection(factory, logger, config);
        var sql = new ExampleUsersSql(db);
        var repo = new ExampleUserRepository(sql);
        
        // Finalmente:
        var user = await repo.GetByPublicIdAsync(request.PublicId, ct);
        // ...
    }
}
```

**Problemas:**
1. El handler sabe cómo construir toda la infraestructura
2. Si cambia la firma de `MainDapperDbConnection`, tienes que cambiar el handler
3. No puedes testear sin una base de datos real
4. La conexión a BD se crea en cada llamada. Sin reutilización.

### Con DI: código desacoplado

```csharp
// ✓ Handler que pide lo que necesita
public sealed class GetExampleUserHandler(IExampleUserRepository repo)
    : IRequestHandler<GetExampleUserRequest, GetExampleUserResponse>
{
    public async Task<GetExampleUserResponse> Handle(GetExampleUserRequest request, CancellationToken ct)
    {
        // Solo usa la interfaz — no sabe ni le importa cómo está implementada
        var user = await repo.GetByPublicIdAsync(request.PublicId, ct);
        
        if (user is null)
            return new GetExampleUserNotFoundFailure("Usuario no encontrado.");
        
        return new GetExampleUserSuccess(new ExampleUserDto(
            user.PublicId, user.FullName, user.Email,
            user.IsActive, user.CreatedAtUtc, user.UpdatedAtUtc));
    }
}
```

El DI container de ASP.NET Core resuelve toda la cadena automáticamente en el momento de la petición.

---

## Cómo funciona el DI container

### Paso 1: Registrar las dependencias

En `Program.cs` y los archivos `ServiceCollectionEx.cs`:

```csharp
// Application/ServiceCollectionEx.cs
services.AddMediator(typeof(GetExampleUserHandler).Assembly);
// AddMediator escanea el ensamblado y registra todos los IRequestHandler<,>

// Infrastructure/ServiceCollectionEx.cs
services.AddSingleton<MainDbConnectionFactory>();           // fábrica de conexiones
services.AddScoped<MainDapperDbConnection>();               // conexión por request
services.AddScoped<ExampleUsersSql>();                      // queries SQL
services.AddScoped<IExampleUserRepository, ExampleUserRepository>();  // repositorio

// WebApi/ServiceCollectionEx.cs
services.AddScoped(typeof(ResultViewModel<>));             // ViewModel genérico
services.AddScoped<INotificationHandler<GetExampleUserResponse>, GetExampleUserPresenter>();
```

### Paso 2: El container resuelve la cadena

Cuando llega una petición HTTP `GET /api/example/users/{id}`:

```
ASP.NET necesita ExampleUsersController
    ↓ lee su constructor: necesita IMediator, ILogger<>, ResultViewModel<>
    ↓ resuelve IMediator → ConcreteMediator (interno de Common)
    ↓ resuelve ILogger<ExampleUsersController> → Serilog logger
    ↓ resuelve ResultViewModel<ExampleUsersController>
        ↓ ResultViewModel no tiene dependencias → crea directamente
    ↓ ExampleUsersController creado ✓

Al llamar Mediator.Send(new GetExampleUserRequest(id)):
    ↓ InteractorPipeline busca GetExampleUserHandler
    ↓ GetExampleUserHandler necesita IExampleUserRepository
    ↓ IExampleUserRepository → ExampleUserRepository
    ↓ ExampleUserRepository necesita ExampleUsersSql
    ↓ ExampleUsersSql necesita MainDapperDbConnection
    ↓ MainDapperDbConnection necesita MainDbConnectionFactory + ILogger + IConfiguration
    ↓ resuelve todo en cascada
    ↓ GetExampleUserHandler creado y llamado ✓
```

Tú nunca escribes ningún `new`. El container lo hace todo.

---

## Registro en DI: patrones del proyecto

### Registrar interfaz → implementación

```csharp
// El container: cuando alguien pida IExampleUserRepository, dale ExampleUserRepository
services.AddScoped<IExampleUserRepository, ExampleUserRepository>();
```

### Registrar solo la clase

```csharp
// No hay interfaz — se registra la clase concreta
services.AddScoped<ExampleUsersSql>();
```

### Registrar genérico abierto

```csharp
// ResultViewModel<T> para cualquier T
// Cuando alguien pida ResultViewModel<ProductsController>, lo crea con T = ProductsController
services.AddScoped(typeof(ResultViewModel<>));
```

### Registrar con factory

```csharp
// Cuando necesitas lógica en la construcción
services.AddScoped<ICurrentUserService>(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    return new CurrentUserService(httpContextAccessor);
});
```

### Resolver manualmente (raramente necesario)

```csharp
// Desde un factory o middleware — obtener un servicio del container
public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var migrationService = scope.ServiceProvider.GetRequiredService<SchemaMigrationService>();
    migrationService.RunAsync().Wait();
}
```

---

## El flujo completo de DI en el proyecto

```
Program.cs
    builder.Services.AddApplicationServices()     → handlers via AddMediator
    builder.Services.AddInfrastructureServices()  → factory, db, sql, repos
    builder.Services.AddWebApiServices()          → viewmodels, presenters, controllers
    builder.Services.AddJwtAuthentication()       → JWT middleware
    builder.Services.AddSwaggerWithJwt()          → Swagger
    builder.Services.AddHealthServices()          → health checks
    builder.Services.AddLocalhostCors()           → CORS
```

Todo se registra una vez al arrancar. El container gestiona la vida de cada objeto según su lifetime (ver [DI Lifetimes](09-lifetimes.md)).

---

## Errores comunes con constructores y DI

### Error 1: Constructor sin parámetro obligatorio

```csharp
// ❌ El container no puede resolver GetExampleUserHandler
// porque no sabe qué IExampleUserRepository darle
public sealed class GetExampleUserHandler
{
    public async Task<GetExampleUserResponse> Handle(...) { }
    // Sin constructor → el container intenta el constructor vacío → no puede resolver
}

// ✓ Con constructor que declara la dependencia
public sealed class GetExampleUserHandler(IExampleUserRepository repo) { }
```

### Error 2: Guardar un Scoped en un campo Singleton

```csharp
// ❌ El Singleton vive toda la app — el Scoped (MainDapperDbConnection) fue creado
// para una request específica y ya no existe
public sealed class CacheSingleton
{
    // ERROR: MainDapperDbConnection es Scoped — no puede vivir en un Singleton
    private readonly MainDapperDbConnection _db;
    
    public CacheSingleton(MainDapperDbConnection db) => _db = db;
}
```

Ver [DI Lifetimes](09-lifetimes.md) para la explicación completa.

### Error 3: Circular dependency

```csharp
// ❌ A necesita B, B necesita A → el container lanza StackOverflowException
public sealed class ServiceA(ServiceB b) { }
public sealed class ServiceB(ServiceA a) { }

// ✓ Solución: redesignar para romper el ciclo, o usar Lazy<T>
public sealed class ServiceA(Lazy<ServiceB> b) { }
public sealed class ServiceB(ServiceA a) { }
// Registrar: services.AddScoped<ServiceA>(); services.AddScoped<ServiceB>();
// services.AddScoped(sp => new Lazy<ServiceB>(() => sp.GetRequiredService<ServiceB>()));
```

### Error 4: Olvidar registrar una dependencia

```csharp
// Infrastructure/ServiceCollectionEx.cs
services.AddScoped<ExampleUsersSql>();
// ❌ Falta el repositorio:
// services.AddScoped<IExampleUserRepository, ExampleUserRepository>();

// Resultado al arrancar:
// InvalidOperationException: Unable to resolve service for type
// 'Domain.Repositories.ExampleUsers.IExampleUserRepository'
// while attempting to activate 'Application.UseCases.ExampleUsers...'
```

**Debugging tip:** cuando ves `Unable to resolve service for type`, busca en los archivos `ServiceCollectionEx.cs`. Falta un `AddScoped`.


---

## Glosario

| Término | Definición |
|---------|-----------|
| Constructor | Método especial con el mismo nombre de la clase que se ejecuta al crear un objeto con `new`; inicializa el estado y recibe dependencias |
| Primary constructor | Sintaxis de C# 12+ que declara los parámetros del constructor directamente en la cabecera de la clase |
| Constructor encadenado (`this()`) | Constructor que llama a otro constructor de la misma clase para reutilizar lógica de inicialización |
| Constructor base (`base()`) | Llamada desde un constructor hijo al constructor del padre para inicializar la parte heredada |
| Dependency Injection (DI) | Patrón en el que las dependencias de un objeto se pasan desde el exterior (constructor, property) en lugar de crearlas internamente |
| DI container | Sistema que gestiona la creación y el ciclo de vida de los objetos registrados; resuelve automáticamente la cadena de dependencias |
| `services.AddScoped<>()` | Registro de un servicio cuya instancia vive mientras dure la petición HTTP |
| `services.AddSingleton<>()` | Registro de un servicio cuya instancia vive toda la vida de la aplicación |
| `AddMediator()` | Método que escanea un ensamblado y registra automáticamente todos los `IRequestHandler<,>` |
| Circular dependency | Error de diseño donde A depende de B y B depende de A; el container no puede resolver la cadena |
| `InvalidOperationException` | Excepción lanzada por el container cuando no encuentra un registro para un tipo requerido |
| `Lazy<T>` | Mecanismo para romper dependencias circulares: la dependencia se resuelve la primera vez que se usa, no al construir el objeto |

---

*Rogelio Arriaga Gonzalez*
