# 01: Clases en C#

Una clase es el bloque fundamental de C#. Todo objeto que existe en tiempo de ejecución viene de una clase.

> Fuente: *C# 13 and .NET 9: Modern Cross-Platform Development* (Mark J. Price). Ch.5 Building Your Own Types with OOP

---

## ¿Qué es una clase?

Una clase es un **molde** o **plano** que describe:
- Qué **datos** tiene (campos y propiedades)
- Qué **puede hacer** (métodos)
- Cómo se **crea** (constructor)

```csharp
// Definición del molde
public class Persona
{
    // Datos
    public string Nombre { get; set; }
    public int Edad { get; set; }

    // Comportamiento
    public string Saludar() => $"Hola, soy {Nombre} y tengo {Edad} años.";
}

// Crear objetos a partir del molde (instanciar)
var p1 = new Persona { Nombre = "Ana", Edad = 28 };
var p2 = new Persona { Nombre = "Luis", Edad = 35 };

Console.WriteLine(p1.Saludar()); // "Hola, soy Ana y tengo 28 años."
Console.WriteLine(p2.Saludar()); // "Hola, soy Luis y tengo 35 años."
```

`p1` y `p2` son **instancias** (objetos concretos) de la clase `Persona`. Comparten la misma estructura pero tienen datos distintos.

---

## Modificadores de acceso

Controlan quién puede ver y usar los miembros de una clase. Son la primera palabra de cada declaración.

### `public`: visible para todos

```csharp
public class ExampleUsersSql          // cualquier proyecto puede usarla
{
    public Task<ExampleUser?> GetByPublicIdAsync(...)  // cualquier código puede llamarlo
    { }
}
```

Úsalo para: clases de interfaces públicas, métodos que forman parte del contrato externo.

### `private`: solo dentro de la clase

```csharp
public sealed class GetExampleUserHandler
{
    private readonly IExampleUserRepository _repo;  // nadie fuera puede tocarlo
    
    private ExampleUserDto MapToDto(ExampleUser user)  // método auxiliar interno
    {
        return new ExampleUserDto(user.PublicId, user.FullName, user.Email,
            user.IsActive, user.CreatedAtUtc, user.UpdatedAtUtc);
    }
}
```

Úsalo para: campos, métodos auxiliares que no son parte del API pública.

**Convención del proyecto:** campos privados se nombran con guión bajo: `_camelCase`.

### `protected`: la clase y sus subclases

```csharp
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator Mediator;  // los controllers hijos lo pueden usar
    
    protected BaseApiController(IMediator mediator)
    {
        Mediator = mediator;
    }
}

public sealed class ExampleUsersController : BaseApiController
{
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        // Mediator está disponible aquí — viene del padre (protected)
        _ = await Mediator.Send(new GetExampleUserRequest(id), ct);
        return Ok(_viewModel);
    }
}
```

Úsalo para: miembros que las subclases necesitan pero el código externo no.

### `internal`: solo dentro del mismo proyecto (.csproj)

```csharp
// Solo código dentro del proyecto "Infrastructure" puede ver esto
internal sealed class MainDbConnectionFactory
{
    internal NpgsqlConnection CreateConnection(string connectionString) { }
}
```

Úsalo para: detalles de implementación que no deben ser API pública del proyecto, pero sí son accesibles dentro del ensamblado.

### `private protected`: la clase y subclases del mismo proyecto

Combinación restrictiva: heredable, pero solo dentro del mismo ensamblado.

```csharp
private protected void MetodoInterno() { }
// Accesible desde subclases, pero solo si están en el mismo proyecto
```

### `protected internal`: la clase, subclases, y todo el proyecto

```csharp
protected internal void MetodoCompartido() { }
// Accesible: subclases de cualquier proyecto + todo el proyecto actual
```

### Tabla resumen

| Modificador | Misma clase | Subclases (mismo proyecto) | Subclases (otro proyecto) | Mismo proyecto | Otro proyecto |
|-------------|-------------|---------------------------|--------------------------|---------------|--------------|
| `private` | ✓ | ✗ | ✗ | ✗ | ✗ |
| `private protected` | ✓ | ✓ | ✗ | ✗ | ✗ |
| `protected` | ✓ | ✓ | ✓ | ✗ | ✗ |
| `internal` | ✓ | ✓ | ✗ | ✓ | ✗ |
| `protected internal` | ✓ | ✓ | ✓ | ✓ | ✗ |
| `public` | ✓ | ✓ | ✓ | ✓ | ✓ |

**Regla práctica:** usa el modificador más restrictivo que permita que el código funcione. Si algo puede ser `private`, hazlo `private`.

---

## Tipos de clases

### Clase normal (por defecto)

Se puede instanciar y heredar libremente. El tipo más común.

```csharp
public class ProductRepository : IProductRepository
{
    private readonly ProductsSql _sql;
    
    public ProductRepository(ProductsSql sql) => _sql = sql;
    
    public Task<Product?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default)
        => _sql.GetByPublicIdAsync(publicId, ct);
}
```

### `sealed`: clase sellada, no se puede heredar

```csharp
public sealed class GetExampleUserHandler : IRequestHandler<GetExampleUserRequest, GetExampleUserResponse>
{
    // El compilador sabe que nadie puede heredar de esto
    // Optimización: el compilador puede hacer devirtualización
    // Comunicación: "esta clase no está diseñada para ser base de otras"
}

// ERROR — no compila:
// public class MiHandler : GetExampleUserHandler { }
```

**Cuándo usar `sealed`:**
- Handlers, repositorios, presenters: son implementaciones concretas finales
- Mejora el rendimiento (el compilador puede optimizar las llamadas a métodos)
- Comunica que la clase no forma parte de una jerarquía

**Cuándo NO usar `sealed`:**
- Cuando la clase está diseñada como base para extensión (`abstract class`)
- Cuando quieres que los tests puedan hacer subclases para override de comportamiento

### `abstract`: clase abstracta, no se puede instanciar

Una clase que está incompleta a propósito. Define la estructura que las subclases deben completar.

```csharp
public abstract class BaseApiController : ControllerBase
{
    // Parte completa — todos los controllers tienen IMediator
    protected readonly IMediator Mediator;
    
    protected BaseApiController(IMediator mediator)
    {
        Mediator = mediator;
    }
    
    // No define métodos abstractos aquí — pero podría:
    // protected abstract string GetModuleName();
}

// Subclase concreta — puede instanciarse
public sealed class ExampleUsersController : BaseApiController
{
    public ExampleUsersController(IMediator mediator, ...) : base(mediator) { }
}

// ERROR — no compila:
// var ctrl = new BaseApiController(...);
```

**Métodos abstractos**: deben implementarse en subclases:

```csharp
public abstract class Animal
{
    public string Nombre { get; set; }
    
    // Método abstracto — sin implementación, DEBE implementarse
    public abstract string HacerSonido();
    
    // Método normal — tiene implementación, puede sobreescribirse con override
    public virtual void Dormir() => Console.WriteLine($"{Nombre} duerme...");
    
    // Método normal sellado — no se puede sobreescribir
    public void Respirar() => Console.WriteLine("Inhala... Exhala...");
}

public sealed class Perro : Animal
{
    // OBLIGATORIO implementar el abstract
    public override string HacerSonido() => "Guau";
    
    // OPCIONAL sobreescribir el virtual
    public override void Dormir() => Console.WriteLine($"{Nombre} ronca.");
}

// ERROR — Gato no implementó HacerSonido():
// public sealed class Gato : Animal { }
```

**En este proyecto**, los `abstract record` son las respuestas base:

```csharp
public abstract record GetExampleUserResponse : IResponse;
// No tiene implementación — es solo la firma del tipo de resultado
// Permite que el presenter reciba cualquier subtipo de respuesta
```

### `static`: clase estática

No se puede instanciar. Todos sus miembros son estáticos. Vive durante toda la aplicación.

```csharp
public static class ServiceCollectionExtensions
{
    // No puedes hacer: new ServiceCollectionExtensions()
    
    // Método de extensión (this = sobre quién se llama)
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediator(typeof(GetExampleUserHandler).Assembly);
        return services;
    }
    
    public static IServiceCollection AddWebApiServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(ResultViewModel<>));
        // registrar presenters...
        return services;
    }
}

// Uso — parece un método de IServiceCollection:
builder.Services.AddApplicationServices();
builder.Services.AddWebApiServices();
```

**Cuándo usar `static`:**
- Métodos de utilidad sin estado: helpers, extensiones
- Constantes y configuraciones globales
- Fábricas simples

**Cuándo NO usar `static`:**
- Cuando necesitas DI (las clases estáticas no se inyectan)
- Cuando el estado varía entre peticiones

### `partial`: clase dividida en múltiples archivos

Permite dividir una clase en varios archivos. El compilador los une.

```csharp
// ExampleUser.cs
public partial class ExampleUser
{
    public Guid PublicId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
}

// ExampleUser.Validation.cs
public partial class ExampleUser
{
    public bool IsValidEmail() => Email.Contains('@');
}

// ExampleUser.Domain.cs
public partial class ExampleUser
{
    public bool CanLogin() => IsActive && IsValidEmail();
}
```

**Cuándo usar:** generación de código (source generators), separación lógica de responsabilidades dentro de una clase grande. No muy común en este proyecto.

---

## Object Initializers: inicializar sin constructor explícito

```csharp
// Forma larga
var user = new ExampleUser();
user.PublicId = Guid.NewGuid();
user.FullName = "Ana García";
user.Email = "ana@ejemplo.com";

// Object initializer — más conciso
var user = new ExampleUser
{
    PublicId = Guid.NewGuid(),
    FullName = "Ana García",
    Email = "ana@ejemplo.com",
    IsActive = true
};
```

**Importante:** el object initializer requiere que las propiedades sean `set` o `init`. No funciona con `readonly`.

---

## Nested classes: clases anidadas

Una clase dentro de otra. Útil cuando la clase anidada solo tiene sentido en el contexto de la exterior.

```csharp
public sealed class GetExampleUsersHandler
{
    // Clase auxiliar privada — solo existe dentro del handler
    private sealed class PaginationState
    {
        public int Skip { get; init; }
        public int Take { get; init; }
        
        public static PaginationState From(int page, int pageSize)
            => new() { Skip = (page - 1) * pageSize, Take = pageSize };
    }
    
    public async Task<GetExampleUsersResponse> Handle(GetExampleUsersRequest request, CancellationToken ct)
    {
        var pagination = PaginationState.From(request.Page, request.PageSize);
        var users = await _repo.GetPagedAsync(pagination.Skip, pagination.Take, ct);
        // ...
    }
}
```

---

## Tipos de clases en cada capa del proyecto

| Capa | Tipo de clase | Por qué |
|------|--------------|---------|
| `Domain/Entities` | `class` (normal) | Entidades mutables — estado cambia |
| `Application/UseCases/.../Request` | `sealed record` | Inmutable, se compara por valor |
| `Application/UseCases/.../Responses` | `abstract record` + `sealed record` | Jerarquía de tipos de resultado |
| `Application/UseCases/.../Handler` | `sealed class` | Implementación final, sin herencia |
| `Application/Dto` | `sealed record` | Inmutable, transferencia de datos |
| `Infrastructure/.../Sql` | `sealed class` | Implementación final de queries |
| `Infrastructure/Repositories` | `sealed class` | Implementación final de repositorio |
| `WebApi/EndPoints/.../Controller` | `sealed class` | Implementación final, hereda BaseApiController |
| `WebApi/EndPoints/.../Presenter` | `sealed class` | Implementación final |
| `Host/Extensions` | `static class` | Solo métodos de extensión, sin estado |
| `*/ServiceCollectionEx` | `static class` | Solo métodos de extensión |

---

## Errores comunes

### Error 1: Olvidar `sealed` en implementaciones

```csharp
// ❌ sin sealed — deja la puerta abierta a herencia no deseada
public class GetExampleUserHandler { }

// ✓ con sealed — comunica la intención, mejor rendimiento
public sealed class GetExampleUserHandler { }
```

### Error 2: Hacer público lo que debería ser privado

```csharp
// ❌ _db es un detalle de implementación — no debería ser público
public sealed class ExampleUsersSql
{
    public MainDapperDbConnection _db;  // cualquiera puede acceder y modificar
}

// ✓ privado — encapsulado correctamente
public sealed class ExampleUsersSql
{
    private readonly MainDapperDbConnection _db;
}
```

### Error 3: Usar `static` cuando se necesita DI

```csharp
// ❌ sin DI — no puedes testear, no puedes cambiar la implementación
public static class UserService
{
    public static async Task<User> GetUserAsync(Guid id)
    {
        var db = new NpgsqlConnection("...");  // hardcodeado
        return await db.QuerySingleAsync<User>(...);
    }
}

// ✓ con DI — testeable, desacoplado
public sealed class UserService : IUserService
{
    private readonly IUserRepository _repo;
    public UserService(IUserRepository repo) => _repo = repo;
    
    public Task<User?> GetUserAsync(Guid id, CancellationToken ct)
        => _repo.GetByPublicIdAsync(id, ct);
}
```

### Error 4: Clase con demasiadas responsabilidades

```csharp
// ❌ una clase que hace todo — difícil de mantener y testear
public class UserManager
{
    public User GetUser(Guid id) { /* SQL */ }
    public void SendWelcomeEmail(User user) { /* SMTP */ }
    public string GenerateJwt(User user) { /* JWT */ }
    public void LogAction(string action) { /* Serilog */ }
}

// ✓ separación de responsabilidades — cada clase hace una cosa
public sealed class UserRepository : IUserRepository { /* Solo SQL */ }
public sealed class EmailService : IEmailService { /* Solo emails */ }
public sealed class JwtTokenService : IJwtTokenService { /* Solo JWT */ }
```


---

## Glosario

| Término | Definición |
|---------|-----------|
| Clase | Plantilla que define la estructura (campos, propiedades) y el comportamiento (métodos) de los objetos que se crean a partir de ella |
| Instancia | Objeto concreto creado en memoria a partir de una clase mediante `new` |
| `sealed` | Modificador que impide que una clase sea heredada; comunica que es una implementación final y permite optimizaciones del compilador |
| `abstract` | Modificador que impide instanciar la clase directamente; define la estructura que las subclases deben completar |
| `static` | Modificador que impide instanciar la clase; todos sus miembros pertenecen al tipo y no a objetos individuales |
| `partial` | Modificador que permite dividir la definición de una clase en múltiples archivos del mismo proyecto |
| Modificador de acceso | Palabra clave que controla la visibilidad de un miembro: `public`, `private`, `protected`, `internal` |
| `sealed class` | Tipo concreto final del proyecto — handlers, repositorios, presenters no están diseñados para ser heredados |
| `abstract record` | Tipo base de respuestas en el proyecto (ej: `GetExampleUserResponse`) que no se puede instanciar directamente |
| `static class` | Usado en el proyecto para los métodos de extensión de registro de DI (`ServiceCollectionExtensions`) |
| Object initializer | Sintaxis `new Clase { Prop1 = val1 }` para asignar propiedades después de la creación sin un constructor explícito |
| Principio de mínimo privilegio | Regla de usar el modificador de acceso más restrictivo posible para cada miembro |

---

*Rogelio Arriaga Gonzalez*
