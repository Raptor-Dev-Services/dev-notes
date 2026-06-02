# 07 — Métodos en C#

Los métodos definen el comportamiento de un objeto — qué puede hacer y cómo.

> Fuente: *C# 13 and .NET 9: Modern Cross-Platform Development* (Mark J. Price) — Ch.4 Writing, Debugging, and Testing Functions

---

## Anatomía de un método

```
[modificador acceso] [modificadores] [tipo retorno] NombreMetodo([parámetros])
{
    // cuerpo
    return valor;
}
```

Ejemplo completo con todos los elementos:

```csharp
//  acceso   async    retorno                               nombre           parámetros
public       async    Task<GetExampleUserResponse>          Handle(          GetExampleUserRequest request,
                                                                             CancellationToken     cancellationToken)
{
    var user = await _repo.GetByPublicIdAsync(request.PublicId, cancellationToken);
    
    if (user is null)
        return new GetExampleUserNotFoundFailure("Usuario no encontrado.");
    
    return new GetExampleUserSuccess(new ExampleUserDto(
        user.PublicId, user.FullName, user.Email,
        user.IsActive, user.CreatedAtUtc, user.UpdatedAtUtc));
}
```

---

## Tipos de retorno

### `void` — sin retorno

```csharp
public void LogError(Exception ex, string message)
{
    _logger.LogError(ex, message);
    // No hay return
}
```

### Tipo concreto

```csharp
public int Sumar(int a, int b) => a + b;
public string FormatearNombre(string nombre, string apellido) => $"{nombre} {apellido}";
public bool EsActivo(ExampleUser user) => user.IsActive && user.UpdatedAtUtc > DateTime.UtcNow.AddDays(-30);
```

### `Task` — void asíncrono

```csharp
// "Hace algo asíncrono pero no retorna valor"
public async Task EnviarEmailAsync(string destinatario, CancellationToken ct)
{
    await _smtpClient.SendAsync(/* ... */, ct);
    // No hay return de valor — el Task representa "terminó"
}
```

### `Task<T>` — valor asíncrono

```csharp
public async Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default)
{
    return await _db.QuerySingleAsync<ExampleUser>(/* ... */);
}
```

---

## Parámetros

### Parámetros obligatorios

```csharp
// Ambos son obligatorios — el compilador exige que se pasen
public Task<ExampleUser> InsertAsync(string fullName, string email, CancellationToken ct)
{
    return _sql.InsertAsync(fullName, email, ct);
}
```

### Parámetros opcionales (con valor por defecto)

```csharp
// ct es opcional — si no se pasa, usa default (= CancellationToken.None)
public Task<ExampleUser?> GetByPublicIdAsync(
    Guid publicId,
    CancellationToken ct = default)
{
    return _db.QuerySingleAsync<ExampleUser>(/* ... */);
}

// Llamar con y sin el parámetro opcional:
await repo.GetByPublicIdAsync(id);             // ct = default
await repo.GetByPublicIdAsync(id, ct);         // ct = el pasado
await repo.GetByPublicIdAsync(id, default);    // equivalente al primero
```

**Regla:** los parámetros opcionales van siempre al FINAL.

### Parámetros con nombre (named parameters)

```csharp
// Útil cuando hay muchos parámetros o para claridad
var dto = new ExampleUserDto(
    UserId:      user.PublicId,
    FullName:    user.FullName,
    Email:       user.Email,
    IsActive:    user.IsActive,
    CreatedAtUtc: user.CreatedAtUtc,
    UpdatedAtUtc: user.UpdatedAtUtc);
```

### `params` — número variable de argumentos

```csharp
public void LogMultiple(string template, params object[] values)
{
    _logger.LogInformation(template, values);
}

// Llamar con cualquier número de argumentos:
LogMultiple("Procesando {Count} items en {Duration}ms", 10, 500);
LogMultiple("Usuario {Name} autenticado", "Ana");
LogMultiple("Error genérico");  // sin valores
```

### `ref` — pasar por referencia (mutable)

```csharp
public void Incrementar(ref int contador)
{
    contador++;  // modifica el original
}

int n = 5;
Incrementar(ref n);
Console.WriteLine(n);  // 6 — el original cambió
```

### `out` — parámetro de salida

```csharp
// El método DEBE asignar el parámetro out antes de retornar
public bool TryParseGuid(string input, out Guid result)
{
    return Guid.TryParse(input, out result);
}

// Uso:
if (TryParseGuid(idStr, out Guid id))
{
    // id tiene valor aquí
    var user = await repo.GetByPublicIdAsync(id, ct);
}
```

### `in` — pasar por referencia (solo lectura)

```csharp
// Eficiente para structs grandes — no copia, no modifica
public double CalcularDistancia(in Coordenada origen, in Coordenada destino)
{
    // origen y destino no se pueden modificar
    return Math.Sqrt(
        Math.Pow(destino.X - origen.X, 2) +
        Math.Pow(destino.Y - origen.Y, 2));
}
```

---

## Sobrecarga de métodos (Overloading)

Múltiples métodos con el mismo nombre pero distintos parámetros:

```csharp
public sealed class ExampleUsersSql
{
    // Buscar por GUID
    public Task<ExampleUser?> GetAsync(Guid publicId, CancellationToken ct = default)
        => _db.QuerySingleAsync<ExampleUser>(
            "SELECT * FROM dbo.ExampleUsers WHERE PublicId = @publicId",
            new { publicId }, cancellationToken: ct);
    
    // Buscar por email
    public Task<ExampleUser?> GetAsync(string email, CancellationToken ct = default)
        => _db.QuerySingleAsync<ExampleUser>(
            "SELECT * FROM dbo.ExampleUsers WHERE Email = @email",
            new { email }, cancellationToken: ct);
    
    // Buscar por ID interno
    public Task<ExampleUser?> GetAsync(int id, CancellationToken ct = default)
        => _db.QuerySingleAsync<ExampleUser>(
            "SELECT * FROM dbo.ExampleUsers WHERE Id = @id",
            new { id }, cancellationToken: ct);
}

// El compilador elige el correcto según el tipo del argumento:
await sql.GetAsync(Guid.NewGuid());    // llama el de Guid
await sql.GetAsync("ana@ejemplo.com"); // llama el de string
await sql.GetAsync(42);               // llama el de int
```

---

## Métodos de extensión

Agregan métodos a tipos existentes sin modificarlos. Deben estar en clases estáticas y el primer parámetro es `this TipoExtendido`.

```csharp
public static class ServiceCollectionExtensions
{
    // Extiende IServiceCollection — agrega un método "AddApplicationServices"
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediator(typeof(GetExampleUserHandler).Assembly);
        return services;
    }
    
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<MainDbConnectionFactory>();
        services.AddScoped<MainDapperDbConnection>();
        services.AddScoped<ExampleUsersSql>();
        services.AddScoped<IExampleUserRepository, ExampleUserRepository>();
        return services;
    }
}

// Uso — parece un método nativo de IServiceCollection:
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddWebApiServices();
```

### Métodos de extensión en tipos primitivos

```csharp
public static class StringExtensions
{
    public static bool IsValidEmail(this string email)
        => email.Contains('@') && email.Contains('.');
    
    public static string ToCamelCase(this string s)
        => string.IsNullOrEmpty(s) ? s : char.ToLower(s[0]) + s[1..];
}

// Uso:
"ana@ejemplo.com".IsValidEmail()  // true
"NombreCompleto".ToCamelCase()    // "nombreCompleto"
```

---

## Expression-bodied members — forma concisa

Cuando el cuerpo del método es una sola expresión:

```csharp
// Método con cuerpo completo
public string GetDisplayName()
{
    return $"{FullName} ({Email})";
}

// Expression-bodied — equivalente
public string GetDisplayName() => $"{FullName} ({Email})";

// Método async expression-bodied
public Task<ExampleUser?> GetByPublicIdAsync(Guid id, CancellationToken ct = default)
    => _db.QuerySingleAsync<ExampleUser>(/* ... */);

// Propiedad expression-bodied
public bool IsAdmin => Role == "Admin";
public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
```

### Constructor expression-bodied

```csharp
// Constructor que solo asigna un campo
public GetExampleUserPresenter(ResultViewModel<ExampleUsersController> viewModel)
    => _viewModel = viewModel;

// Equivalente:
// {
//     _viewModel = viewModel;
// }
```

---

## Métodos locales (local functions)

Métodos definidos dentro de otros métodos. Solo son accesibles en el scope donde se definen.

```csharp
public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
{
    try
    {
        _ = await Mediator.Send(new GetExampleUserRequest(id), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error en GetById id={UserId}", id);
        return StatusCode(500, _viewModel.Fail(ObtenerMensajeRaiz(ex)));
    }
    
    // Método local — accesible solo dentro de GetById
    static string ObtenerMensajeRaiz(Exception ex)
    {
        var inner = ex;
        while (inner.InnerException != null) inner = inner.InnerException!;
        return inner.Message;
    }
}
```

**Ventajas de métodos locales:**
- No contaminan el API público de la clase
- Pueden capturar variables del scope externo (closures)
- `static` local function = no captura nada del scope externo (más eficiente)

---

## Lambdas — métodos anónimos

```csharp
// Lambda de una línea
Func<int, int, int> sumar = (a, b) => a + b;
Console.WriteLine(sumar(3, 4));  // 7

// Lambda multi-línea
Func<string, bool> esEmailValido = email =>
{
    if (string.IsNullOrEmpty(email)) return false;
    return email.Contains('@') && email.Contains('.');
};

// Lambdas en LINQ
var activos = users
    .Where(u => u.IsActive)
    .OrderBy(u => u.FullName)
    .Select(u => new ExampleUserDto(u.PublicId, u.FullName, /* ... */));

// Lambda como parámetro de método
services.AddHealthChecks()
    .AddCheck("custom-check", () => HealthCheckResult.Healthy("Todo bien"));
```

---

## Métodos de interfaz vs implementación

```csharp
// La interfaz define el CONTRATO (qué)
public interface IExampleUserRepository
{
    Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default);
}

// La implementación define el CÓMO
public sealed class ExampleUserRepository : IExampleUserRepository
{
    private readonly ExampleUsersSql _sql;
    
    public ExampleUserRepository(ExampleUsersSql sql) => _sql = sql;
    
    // Implementación del contrato
    public Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default)
        => _sql.GetByPublicIdAsync(publicId, ct);
    
    // Métodos adicionales NO en la interfaz — solo accesibles con el tipo concreto
    public async Task<int> ContarActivosAsync(CancellationToken ct = default)
        => await _sql.ContarActivosAsync(ct);
}
```

---

## Convenciones de nombres en métodos

| Situación | Convención | Ejemplo |
|-----------|-----------|---------|
| Método síncrono | PascalCase verbo | `GetById`, `Insert`, `Disable` |
| Método asíncrono | PascalCase + Async | `GetByIdAsync`, `InsertAsync` |
| Método de extensión | PascalCase verbo | `AddApplicationServices` |
| Método local | camelCase | `obtenerMensajeRaiz` |
| Lambda | camelCase param | `u => u.IsActive` |
| Método de test | `{Sujeto}_{Condición}_{Resultado}` | `Handle_MissingUser_ReturnsNotFound` |


---

## Glosario

| Término | Definición |
|---------|-----------|
| Método | Bloque de código con nombre que define el comportamiento de un objeto; tiene firma (nombre + parámetros + retorno) y cuerpo |
| Sobrecarga (overloading) | Múltiples métodos con el mismo nombre pero distintos parámetros; el compilador elige cuál llamar según los argumentos |
| Método de extensión | Método estático que se puede llamar como si fuera parte de un tipo existente; primer parámetro es `this TipoExtendido` |
| Expression-bodied member | Sintaxis concisa con `=>` para métodos o propiedades de una sola expresión |
| Método local | Función definida dentro de otro método; solo accesible en ese scope; puede ser `static` para evitar capturas |
| Lambda | Función anónima expresada con `=>`: `(parámetros) => cuerpo`; usada en LINQ y como parámetros de alto orden |
| `params` | Modificador que permite pasar un número variable de argumentos del mismo tipo a un método |
| `ref` | Modificador que pasa un argumento por referencia mutable — el método puede cambiar el valor del original |
| `out` | Modificador de parámetro de salida que el método debe asignar antes de retornar |
| `CancellationToken` | Parámetro estándar en métodos asíncronos del proyecto; permite cancelar la operación desde el llamador |
| `Task<T>` | Tipo de retorno de métodos asíncronos que devuelven un valor; se consume con `await` |
| Named parameters | Forma de llamar un método especificando el nombre del parámetro: `new Dto(UserId: id, FullName: nombre)` |

---

*Rogelio Arriaga Gonzalez*
