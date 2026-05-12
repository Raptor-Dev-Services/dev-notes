# 02 — Interfaces en C#

Una interfaz es un **contrato**. Define qué métodos y propiedades debe tener quien la implemente, sin decir cómo.

> Fuente: *C# 13 and .NET 9: Modern Cross-Platform Development* (Mark J. Price) — Ch.6 Implementing Interfaces and Inheriting Classes

---

## ¿Qué es una interfaz?

```csharp
// El contrato: "cualquier repositorio de usuarios DEBE poder hacer esto"
public interface IExampleUserRepository
{
    Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default);
    Task<IEnumerable<ExampleUser>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<ExampleUser> InsertAsync(string fullName, string email, CancellationToken ct = default);
    Task<int> DisableAsync(Guid publicId, CancellationToken ct = default);
}
```

La interfaz no tiene cuerpo — solo firmas de métodos. La implementación viene en una clase:

```csharp
public sealed class ExampleUserRepository : IExampleUserRepository
{
    private readonly ExampleUsersSql _sql;
    
    public ExampleUserRepository(ExampleUsersSql sql) => _sql = sql;
    
    // Debe implementar TODOS los métodos del contrato:
    public Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default)
        => _sql.GetByPublicIdAsync(publicId, ct);
    
    public Task<IEnumerable<ExampleUser>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        => _sql.GetPagedAsync(page, pageSize, ct);
    
    public Task<ExampleUser> InsertAsync(string fullName, string email, CancellationToken ct = default)
        => _sql.InsertAsync(fullName, email, ct);
    
    public Task<int> DisableAsync(Guid publicId, CancellationToken ct = default)
        => _sql.DisableAsync(publicId, ct);
}
```

---

## Por qué usar interfaces en vez de la clase directa

Este es el concepto más importante para entender la arquitectura del proyecto.

### Sin interfaz — acoplamiento directo

```csharp
// ❌ El handler depende de la clase concreta
public sealed class GetExampleUserHandler
{
    private readonly ExampleUserRepository _repo;
    //               ↑ clase concreta de Infrastructure
    
    public GetExampleUserHandler(ExampleUserRepository repo) => _repo = repo;
    
    // Consecuencias:
    // 1. Application importa Infrastructure — viola Clean Architecture
    // 2. Para testear, necesitas PostgreSQL real
    // 3. No puedes cambiar la implementación sin cambiar el handler
}
```

### Con interfaz — desacoplamiento

```csharp
// ✓ El handler depende del contrato
public sealed class GetExampleUserHandler
{
    private readonly IExampleUserRepository _repo;
    //               ↑ interfaz de Domain
    
    public GetExampleUserHandler(IExampleUserRepository repo) => _repo = repo;
    
    // Consecuencias:
    // 1. Application solo importa Domain — correcto
    // 2. Para testear, pasas un mock que implementa IExampleUserRepository
    // 3. Puedes cambiar la implementación (Redis, otro ORM) sin tocar el handler
}
```

### El flujo de dependencias con interfaces

```
Domain:         IExampleUserRepository (interfaz — el contrato)
                       ↑
Application:    GetExampleUserHandler (usa la interfaz)
                       ↑ (no conoce Infrastructure)
Infrastructure: ExampleUserRepository (implementa la interfaz)
                       ↑
Host:           services.AddScoped<IExampleUserRepository, ExampleUserRepository>();
                (conecta contrato con implementación)
```

---

## Cómo declarar una interfaz

```csharp
// Convención: nombre empieza con "I"
public interface INombreInterfaz
{
    // Solo firmas — sin cuerpo, sin implementación
    ReturnType MetodoUno(Parametro param);
    Task<ReturnType> MetodoAsync(Parametro param, CancellationToken ct = default);
    
    // Propiedades — sin backing field, sin implementación
    string Propiedad { get; }      // solo lectura
    string OtraPropiedad { get; set; }  // lectura y escritura
}
```

---

## Implementar múltiples interfaces

Una clase puede implementar muchas interfaces. Los records también.

```csharp
// Una respuesta implementa la base y el marcador de resultado
public sealed record GetExampleUserSuccess(ExampleUserDto Data)
    : GetExampleUserResponse,      // hereda del abstract record base
      ISuccess<ExampleUserDto>;    // marca como "éxito con un DTO"

// Un handler implementa la interfaz del mediator
public sealed class GetExampleUserHandler
    : IRequestHandler<GetExampleUserRequest, GetExampleUserResponse>
```

---

## Interfaces del proyecto — catálogo completo

### De `Common.Messaging`

```csharp
// Marca un objeto como "mensaje que se puede enviar al mediator"
public interface IRequest<TResponse> { }

// Implementado por handlers — recibe y procesa el request
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

// Todas las respuestas implementan esto (base del sistema de resultados)
public interface IResponse { }

// El mediator — punto central de comunicación
public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
    Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : IResponse;
}
```

### De `Common.Results`

```csharp
// Resultado exitoso sin datos
public interface ISuccess : IResponse { }

// Resultado exitoso con datos
public interface ISuccess<T> : ISuccess
{
    T Data { get; }
}

// Resultado fallido genérico (500)
public interface IFailure : IResponse
{
    string Message { get; }
}

// Resultado no encontrado (404)
public interface INotFoundFailure : IFailure { }

// Conflicto (409) — ej. email duplicado
public interface IConflictFailure : IFailure { }

// Validación fallida (400)
public interface IValidationFailure : IFailure { }

// No autorizado (401)
public interface IUnauthorizedFailure : IFailure { }
```

### De Domain del proyecto

```csharp
// Contrato del repositorio de usuarios de ejemplo
public interface IExampleUserRepository
{
    Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default);
    Task<IEnumerable<ExampleUser>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<ExampleUser> InsertAsync(string fullName, string email, CancellationToken ct = default);
    Task<int> DisableAsync(Guid publicId, CancellationToken ct = default);
}
```

---

## Interfaces de .NET que debes conocer

### `IDisposable` — liberar recursos

Implementada por clases que usan recursos no administrados (conexiones de BD, archivos, sockets).

```csharp
public sealed class DatabaseConnection : IDisposable
{
    private NpgsqlConnection _connection;
    
    public DatabaseConnection(string connectionString)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
    }
    
    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
    }
}

// Uso con using — garantiza que Dispose() se llama aunque haya excepción
using var db = new DatabaseConnection("...");
// ... usar db
// Al salir del bloque: db.Dispose() automático
```

### `IEnumerable<T>` — iterable

Permite usar `foreach`. Ver [Colecciones](12-collections.md) para detalle completo.

### `IAsyncEnumerable<T>` — iterable asíncrono

Para secuencias que se obtienen de forma asíncrona (ej. streaming de BD).

```csharp
public interface IProductRepository
{
    IAsyncEnumerable<Product> StreamAllAsync(CancellationToken ct = default);
}

// Uso:
await foreach (var product in repo.StreamAllAsync(ct))
{
    await ProcessProduct(product);
}
```

### `IComparable<T>` — comparación para ordenamiento

```csharp
public sealed class Precio : IComparable<Precio>
{
    public decimal Valor { get; init; }
    
    public int CompareTo(Precio? other)
    {
        if (other is null) return 1;
        return Valor.CompareTo(other.Valor);
    }
}
```

### `IEquatable<T>` — comparación de igualdad

Los `record` lo implementan automáticamente. Para clases, puedes hacerlo manual:

```csharp
public sealed class UserId : IEquatable<UserId>
{
    public Guid Value { get; init; }
    
    public bool Equals(UserId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as UserId);
    public override int GetHashCode() => Value.GetHashCode();
}
```

---

## Default interface methods — C# 8+

Las interfaces pueden tener implementaciones por defecto. Se usan para agregar métodos sin romper implementaciones existentes.

```csharp
public interface IExampleUserRepository
{
    Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default);
    
    // Método con implementación por defecto
    // Las clases existentes no están obligadas a implementarlo
    async Task<bool> ExistsAsync(Guid publicId, CancellationToken ct = default)
    {
        var user = await GetByPublicIdAsync(publicId, ct);
        return user is not null;
    }
}
```

**Cuándo usar:** versionado de APIs — agregar funcionalidad sin romper implementaciones existentes.

**Cuándo NO usar:** como sustituto de clases base o para lógica de negocio compleja.

---

## Implementación explícita de interfaz

Cuando una clase implementa dos interfaces con un método del mismo nombre, puedes implementarlos por separado:

```csharp
public interface IPrintable
{
    void Print();
}

public interface ILoggable
{
    void Print();
}

public sealed class Documento : IPrintable, ILoggable
{
    // Implementación explícita — se llama solo a través de la interfaz
    void IPrintable.Print() => Console.WriteLine("Imprimiendo documento...");
    void ILoggable.Print()  => Console.WriteLine("[LOG] Documento procesado");
    
    // Puedes tener también la implementación pública normal
    public void Print() => Console.WriteLine("Print genérico");
}

var doc = new Documento();
doc.Print();                          // "Print genérico"
((IPrintable)doc).Print();            // "Imprimiendo documento..."
((ILoggable)doc).Print();             // "[LOG] Documento procesado"
```

---

## Interface vs Clase Abstracta — cuándo usar cada una

| Criterio | Interface | Clase Abstracta |
|----------|-----------|-----------------|
| ¿Cuántas puede implementar/heredar? | Múltiples | Solo una |
| ¿Tiene implementación? | Solo default methods (C# 8+) | Sí, métodos concretos |
| ¿Tiene constructor? | No | Sí |
| ¿Tiene estado (campos)? | No | Sí |
| ¿Para qué sirve? | Contrato de capacidades | Base compartida con lógica |
| Ejemplo en el proyecto | `IExampleUserRepository` | `BaseApiController` |

**Regla de oro:**
- Usa **interface** cuando defines un contrato que múltiples clases no relacionadas pueden implementar.
- Usa **clase abstracta** cuando tienes lógica compartida que las subclases necesitan heredar.

```csharp
// Interface — "puede hacer login" aplica a User y a ServiceAccount (no relacionados)
public interface IAuthenticatable
{
    bool Authenticate(string password);
}

// Clase abstracta — lógica común para todos los controllers
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator Mediator;
    protected BaseApiController(IMediator mediator) => Mediator = mediator;
}
```

---

## Marker interfaces — interfaces vacías

Sin métodos. Solo marcan que una clase tiene cierta característica.

```csharp
// IResponse es una marker interface en Common.Messaging
public interface IResponse { }
// Solo indica "esto es una respuesta del sistema"

// ISuccess también es marker — indica resultado exitoso
public interface ISuccess : IResponse { }

// El presenter usa pattern matching para detectar el tipo:
if (response is ISuccess<ExampleUserDto> success)
    _viewModel.Set(success);
else if (response is INotFoundFailure notFound)
    _viewModel.Fail(notFound.Message);
```

---

## Errores comunes con interfaces

### Error 1 — Interfaz demasiado grande (Interface Segregation)

```csharp
// ❌ Una interfaz que hace todo — viola el principio de segregación
public interface IUserService
{
    Task<User> GetByIdAsync(Guid id, CancellationToken ct);
    Task SendEmailAsync(string to, string subject, CancellationToken ct);
    Task<string> GenerateJwtAsync(User user, CancellationToken ct);
    Task LogActionAsync(string action, CancellationToken ct);
}

// ✓ Interfaces pequeñas y focalizadas
public interface IUserRepository  { Task<User?> GetByIdAsync(Guid id, CancellationToken ct); }
public interface IEmailService    { Task SendAsync(string to, string subject, CancellationToken ct); }
public interface IJwtTokenService { string Generate(Guid userId, string email); }
```

### Error 2 — Implementar interfaz sin registrarla en DI

```csharp
// Defines e implementas la interfaz...
public interface IProductRepository { Task<Product?> GetByIdAsync(Guid id, CancellationToken ct); }
public sealed class ProductRepository : IProductRepository { /* ... */ }

// ...pero olvidas registrarla en DI — el container no sabe que existe
// Infrastructure/ServiceCollectionEx.cs:
services.AddScoped<ProductsSql>();
// ❌ Falta: services.AddScoped<IProductRepository, ProductRepository>();

// Resultado: InvalidOperationException al arrancar la app
```

### Error 3 — Inyectar la implementación concreta, no la interfaz

```csharp
// ❌ Inyecta la clase concreta — acoplamiento directo
public sealed class GetProductHandler
{
    private readonly ProductRepository _repo;  // concreto
    public GetProductHandler(ProductRepository repo) => _repo = repo;
}

// ✓ Inyecta la interfaz — desacoplado, testeable
public sealed class GetProductHandler
{
    private readonly IProductRepository _repo;  // contrato
    public GetProductHandler(IProductRepository repo) => _repo = repo;
}
```


---

*Rogelio Arriaga Gonzalez*
