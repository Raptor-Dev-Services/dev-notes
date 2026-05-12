# 10 — Genéricos \<T\>

Los genéricos permiten escribir código que funciona con cualquier tipo, determinado en el momento de uso — sin perder type-safety.

---

## El problema sin genéricos

```csharp
// Sin genéricos — necesitarías una clase por cada tipo
public class ContenedorInt    { public int    Valor { get; set; } }
public class ContenedorString { public string Valor { get; set; } }
public class ContenedorUser   { public ExampleUser Valor { get; set; } }

// Con genéricos — una sola clase para todos los tipos
public class Contenedor<T>
{
    public T Valor { get; set; }
}

var ci = new Contenedor<int>          { Valor = 42 };
var cs = new Contenedor<string>       { Valor = "hola" };
var cu = new Contenedor<ExampleUser>  { Valor = new ExampleUser() };
```

`T` es un **parámetro de tipo** — es un placeholder que se reemplaza por el tipo real al instanciar.

---

## Genéricos en clases

### Con un parámetro de tipo

```csharp
// ResultViewModel<T> — ViewModel tipado por el Controller
public class ResultViewModel<T>
{
    public object? Data { get; private set; }
    public bool    IsSuccess { get; private set; }
    public string  Message  { get; private set; } = string.Empty;
    public DateTime UtcTimeStamp { get; } = DateTime.UtcNow;

    // T solo identifica para qué controller es — no se usa como dato
    public ResultViewModel<T> Set(ISuccess<object> success)
    {
        Data      = success.Data;
        IsSuccess = true;
        return this;
    }

    public ResultViewModel<T> OK(object data)
    {
        Data      = data;
        IsSuccess = true;
        return this;
    }

    public ResultViewModel<T> Fail(string message)
    {
        Message   = message;
        IsSuccess = false;
        return this;
    }
}

// Uso — T determina para qué controller:
ResultViewModel<ExampleUsersController> vmUsers    = new();
ResultViewModel<ProductsController>     vmProducts = new();
// Son tipos distintos en el DI container — no se mezclan
```

### Con múltiples parámetros de tipo

```csharp
// IRequestHandler<TRequest, TResponse> — dos parámetros
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

// Implementación:
public sealed class GetExampleUserHandler
    : IRequestHandler<GetExampleUserRequest, GetExampleUserResponse>
//                    ↑ TRequest              ↑ TResponse
{
    public async Task<GetExampleUserResponse> Handle(
        GetExampleUserRequest request,   // TRequest → GetExampleUserRequest
        CancellationToken ct)
    {
        // ...
    }
}
```

---

## Genéricos en métodos

```csharp
// Método genérico — T se determina en el sitio de llamada
public class MainDapperDbConnection
{
    // T = tipo que Dapper mapeará desde el resultado SQL
    public async Task<T?> QuerySingleAsync<T>(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = _factory.OpenConnection();
        return await connection.QuerySingleOrDefaultAsync<T>(sql, param);
    }
    
    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = _factory.OpenConnection();
        return await connection.QueryAsync<T>(sql, param);
    }
}

// Uso — el compilador infiere T del tipo esperado o lo especificas explícitamente:
var user  = await db.QuerySingleAsync<ExampleUser>("SELECT ...");
var users = await db.QueryAsync<ExampleUser>("SELECT ...");
var count = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ...");
```

---

## Constraints — restricciones de tipo

Los constraints limitan qué tipos se pueden usar con el genérico.

### `where T : class` — T debe ser tipo referencia

```csharp
public sealed class Repositorio<T> where T : class
{
    // Solo acepta clases (ExampleUser, Product, etc.) — no int, bool, struct
    public Task<T?> GetByIdAsync(int id) { }
}
```

### `where T : struct` — T debe ser tipo de valor

```csharp
public T? ParseValue<T>(string input) where T : struct
{
    // Solo acepta int, Guid, DateTime, etc. — no clases
}
```

### `where T : new()` — T debe tener constructor sin parámetros

```csharp
public T Crear<T>() where T : new()
{
    return new T();  // solo funciona si T tiene un constructor vacío
}
```

### `where T : IInterfaz` — T debe implementar una interfaz

```csharp
// El constraint del mediator — TRequest debe implementar IRequest<TResponse>
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}

// Solo acepta tipos que implementen IRequest<TResponse>:
// IRequestHandler<GetExampleUserRequest, GetExampleUserResponse> ✓
// IRequestHandler<string, GetExampleUserResponse>                ✗ string no implementa IRequest
```

### `where T : ClaseBase` — T debe heredar de una clase

```csharp
public void Procesar<T>(T item) where T : BaseApiController
{
    // Solo acepta subclases de BaseApiController
}
```

### Múltiples constraints

```csharp
public void ProcesarEntidad<T>(T entidad)
    where T : class, IDisposable, new()
{
    // T debe ser: clase + implementar IDisposable + tener constructor vacío
}
```

---

## Genérico abierto en DI

Un genérico "abierto" es sin los parámetros de tipo especificados. Permite registrar el tipo para cualquier `T`:

```csharp
// Genérico abierto — typeof(ResultViewModel<>) sin especificar T
services.AddScoped(typeof(ResultViewModel<>));

// El container crea la versión correcta cuando la piden:
// Alguien pide ResultViewModel<ExampleUsersController>
//     → container crea new ResultViewModel<ExampleUsersController>()
// Alguien pide ResultViewModel<ProductsController>
//     → container crea new ResultViewModel<ProductsController>()
// Son instancias distintas — cada controller tiene su propio ViewModel
```

Sin el genérico abierto, tendrías que registrar cada variante manualmente:

```csharp
// Sin genérico abierto — registro manual por cada controller (tedioso)
services.AddScoped<ResultViewModel<ExampleUsersController>>();
services.AddScoped<ResultViewModel<ProductsController>>();
services.AddScoped<ResultViewModel<AuthController>>();
// ... uno por cada controller que crees
```

---

## `ISuccess<T>` — genérico para respuestas exitosas

```csharp
// En Common.Results:
public interface ISuccess<T> : ISuccess
{
    T Data { get; }
}

// Uso en respuestas:
public sealed record GetExampleUserSuccess(ExampleUserDto Data)
    : GetExampleUserResponse, ISuccess<ExampleUserDto>;
//                            ↑ T = ExampleUserDto — Data es ExampleUserDto

// En el ViewModel:
public ResultViewModel<TController> Set(ISuccess<object> success)
{
    Data = success.Data;  // el dato tipado
    IsSuccess = true;
    return this;
}
```

---

## Inferencia de tipos genéricos

El compilador puede inferir el tipo T sin que lo especifiques:

```csharp
// Sin inferencia — explícito
var result = db.QuerySingleAsync<ExampleUser>(sql, param);

// Con inferencia — el compilador lo deduce del tipo de retorno esperado
ExampleUser? user = await db.QuerySingleAsync<ExampleUser>(sql, param);
// (en este caso no puede inferir sin el <ExampleUser> porque el método no sabe qué mapear)

// En un método genérico sí puede inferir:
public T GetOrDefault<T>(T value, T defaultValue) => value ?? defaultValue;

// El compilador infiere T = string:
var resultado = GetOrDefault("Ana", "Anónimo");
// No necesitas: GetOrDefault<string>("Ana", "Anónimo")
```

---

## Covarianza y contravarianza — avanzado

Para interfaces y delegates genéricos.

### Covarianza (`out T`) — puedes usar un tipo más derivado

```csharp
// IEnumerable<T> es covariante — puedes asignar IEnumerable<Perro> a IEnumerable<Animal>
IEnumerable<Perro> perros = new List<Perro> { new Perro() };
IEnumerable<Animal> animales = perros;  // ✓ funciona con "out T"
```

### Contravarianza (`in T`) — puedes usar un tipo menos derivado

```csharp
// Action<T> es contravariante — puedes asignar Action<Animal> a Action<Perro>
Action<Animal> procesarAnimal = a => Console.WriteLine(a.Nombre);
Action<Perro>  procesarPerro  = procesarAnimal;  // ✓ funciona con "in T"
```

---

## Errores comunes con genéricos

### Error 1 — Usar `object` en vez de genérico

```csharp
// ❌ Pierde type-safety
public class Repositorio
{
    public object GetById(int id) { /* ... */ }
}

var result = repo.GetById(1);
var user = (ExampleUser)result;  // cast en runtime — puede explotar

// ✓ Genérico — type-safe
public class Repositorio<T>
{
    public T GetById(int id) { /* ... */ }
}

var repo = new Repositorio<ExampleUser>();
ExampleUser user = repo.GetById(1);  // el compilador sabe el tipo
```

### Error 2 — Registrar genérico cerrado cuando debería ser abierto

```csharp
// ❌ Solo funciona para ProductsController
services.AddScoped<ResultViewModel<ProductsController>>();
// Los otros controllers no tienen su ViewModel

// ✓ Genérico abierto — funciona para cualquier T
services.AddScoped(typeof(ResultViewModel<>));
```

### Error 3 — Constraint demasiado restrictivo

```csharp
// ❌ where T : ExampleUser — solo acepta ExampleUser (entonces para qué el genérico?)
public Task<T> GetUserAsync<T>(Guid id) where T : ExampleUser
{
    return /* ... */;
}

// ✓ Si solo funciona con ExampleUser, no necesitas genérico
public Task<ExampleUser> GetUserAsync(Guid id)
{
    return /* ... */;
}
```


---

*Rogelio Arriaga Gonzalez*
