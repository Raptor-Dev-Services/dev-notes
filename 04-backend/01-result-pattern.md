# 02 — Result Pattern

El Result Pattern modela el resultado de una operación como un objeto con dos estados posibles: éxito o fallo. Es la alternativa a usar excepciones para controlar el flujo de negocio.

> Fuente: *Clean Code with C# 2nd Ed* (Jason Alls) — Ch.3 Classes, Objects, and Error Handling

---

## El problema que resuelve

```csharp
// ❌ Con excepciones para flujo de negocio — varias razones para fallar,
// el caller tiene que saber qué excepción capturar y cuál dejar subir
public async Task<User> GetUserAsync(Guid id)
{
    var user = await _repo.GetByIdAsync(id);
    if (user is null) throw new NotFoundException($"Usuario {id} no encontrado.");
    if (!user.IsActive) throw new BusinessException("Usuario inactivo.");
    return user;
}

// El caller no sabe qué puede volar sin leer la implementación
try
{
    var user = await GetUserAsync(id);
}
catch (NotFoundException)    { return NotFound(); }
catch (BusinessException ex) { return BadRequest(ex.Message); }
catch (Exception)            { return StatusCode(500); }
// las excepciones son costosas y no aparecen en la firma del método
```

Con el Result Pattern, la firma del método **documenta** todos los resultados posibles:

```csharp
// ✓ La firma dice exactamente qué puede pasar
public async Task<GetUserResponse> Handle(GetUserRequest req, CancellationToken ct)
    // puede retornar: GetUserSuccess | GetUserNotFoundFailure | GetUserInactiveFailure
```

---

## Implementación en este proyecto

El proyecto usa `Common.Results` — interfaces que tipan el resultado:

```csharp
// Common.Results — las interfaces base
public interface IResponse { }
public interface ISuccess : IResponse { }
public interface ISuccess<T> : ISuccess { T Data { get; } }
public interface IFailure : IResponse   { string Message { get; } }

// Tipos de fallo específicos (mapean a HTTP status codes):
public interface INotFoundFailure   : IFailure { }   // 404
public interface IConflictFailure   : IFailure { }   // 409
public interface IValidationFailure : IFailure { }   // 400
public interface IUnauthorizedFailure : IFailure { } // 401
```

### Definir un resultado de caso de uso

```csharp
// 1. Base — abstract record
public abstract record GetExampleUserResponse : IResponse;

// 2. Éxito — implementa ISuccess<T>, tiene propiedad Data
public sealed record GetExampleUserSuccess(ExampleUserDto Data)
    : GetExampleUserResponse, ISuccess<ExampleUserDto>;

// 3. Fallos — implementan el tipo de fallo correcto
public sealed record GetExampleUserNotFoundFailure(string Message)
    : GetExampleUserResponse, INotFoundFailure;
```

### Retornar desde el Handler

```csharp
public async Task<GetExampleUserResponse> Handle(
    GetExampleUserRequest request, CancellationToken ct)
{
    var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);

    if (user is null)
        return new GetExampleUserNotFoundFailure("Usuario no encontrado.");

    return new GetExampleUserSuccess(new ExampleUserDto(user));
}
// sin try/catch, sin excepciones de flujo — el fallo es un valor
```

### Consumir en el Presenter

```csharp
public Task Handle(GetExampleUserResponse notification, CancellationToken ct)
{
    if (notification is IFailure failure)
        _viewModel.Fail(failure.Message);
    else if (notification is ISuccess<ExampleUserDto> success)
        _viewModel.Set(success);

    return Task.CompletedTask;
}
// pattern matching sobre la interfaz — cada tipo de fallo se maneja diferente si hace falta
```

---

## Múltiples tipos de fallo

```csharp
public abstract record LoginResponse : IResponse;

public sealed record LoginSuccess(string Token, string RefreshToken)
    : LoginResponse, ISuccess;

public sealed record LoginNotFoundFailure(string Message)
    : LoginResponse, INotFoundFailure;      // usuario no existe → 404

public sealed record LoginUnauthorizedFailure(string Message)
    : LoginResponse, IUnauthorizedFailure;  // contraseña incorrecta → 401

public sealed record LoginLockedFailure(string Message)
    : LoginResponse, IValidationFailure;    // cuenta bloqueada → 400
```

```csharp
// Presenter con múltiples fallos:
public Task Handle(LoginResponse notification, CancellationToken ct)
{
    switch (notification)
    {
        case LoginSuccess s:
            _viewModel.OK(s);
            break;
        case LoginNotFoundFailure f:
            _viewModel.Fail(f.Message);     // _viewModel lleva el tipo al controller
            break;
        case LoginUnauthorizedFailure f:
            _viewModel.Fail(f.Message);
            break;
        case LoginLockedFailure f:
            _viewModel.Fail(f.Message);
            break;
    }
    return Task.CompletedTask;
}
```

---

## Éxito sin DTO (ISuccess sin genérico)

Para operaciones que no retornan datos (insert, update, delete):

```csharp
public sealed record InsertExampleUserSuccess
    : InsertExampleUserResponse, ISuccess;  // sin <T>

// Presenter:
else if (notification is ISuccess)
    _viewModel.OK(new { message = "Usuario creado." });
```

---

## Éxito con datos de colección (no ISuccess<T>)

Cuando el éxito tiene múltiples campos (paginación):

```csharp
// ❌ NO hacer esto — referencia circular al serializar
public sealed record GetUsersSuccess(IReadOnlyCollection<ExampleUserDto> Users, int Total)
    : GetUsersResponse, ISuccess<GetUsersSuccess>   // ← Data => this → recursión infinita en JSON
{
    public GetUsersSuccess Data => this;
}

// ✓ Implementar ISuccess (sin genérico) y pasar el objeto completo al ViewModel
public sealed record GetUsersSuccess(
    IReadOnlyCollection<ExampleUserDto> Users, int Total, int Page, int PageSize)
    : GetUsersResponse, ISuccess;

// Presenter:
else if (notification is GetUsersSuccess success)
    _viewModel.OK(success);   // Data = el record completo
```

---

## Result Pattern vs Excepciones

| Aspecto | Result Pattern | Excepciones |
|---------|---------------|-------------|
| Flujo de negocio esperado | ✓ Ideal — fallo es un valor | ✗ Costoso, semántica incorrecta |
| Error inesperado (bug, null ref) | ✗ No aplica — deja que burbujee | ✓ Correcto — capturar en el handler global |
| Visibilidad en la firma | ✓ La firma documenta los resultados | ✗ Invisible — hay que leer la implementación |
| Rendimiento | ✓ Sin overhead de stack trace | ✗ Crear un stack trace es costoso |
| Composabilidad | ✓ Se encadena bien con pattern matching | ✗ El anidado de try/catch es verboso |

**Regla práctica:** si el fallo es algo que el código del llamador *espera y maneja* (usuario no encontrado, validación falló, conflicto de unicidad) → Result Pattern. Si es un error que nadie espera (NullReferenceException, OutOfMemoryException, bug) → deja que la excepción suba y el global handler la capture.

```csharp
// ✓ Result — el caller espera que el usuario pueda no existir
return new GetExampleUserNotFoundFailure("No encontrado.");

// ✓ Excepción — nadie espera que la DB esté caída; el global handler la registra
var conn = _factory.OpenConnection();  // lanza si no puede conectar → 500 automático
```

---

## Railway-Oriented Programming

El Result Pattern es la versión OOP del concepto funcional de **Railway-Oriented Programming (ROP)**: el flujo tiene dos rieles, el del éxito y el del fallo. Una vez que entra al riel del fallo, se mantiene ahí y se propaga.

```
Riel éxito:  ─── Handler ──→ Success ──→ Presenter ──→ Ok(200)
                                 ↘ falla
Riel fallo:                      ──→ Failure ──→ Presenter ──→ StatusCode(4xx/5xx)
```

```csharp
// Encadenamiento funcional (alternativa sin Presenter, para operaciones simples):
public async Task<IActionResult> DoSomething(Guid id, CancellationToken ct)
{
    var response = await _mediator.Send(new GetExampleUserRequest(id), ct);

    return response switch
    {
        ISuccess<ExampleUserDto> s => Ok(s.Data),
        INotFoundFailure f         => NotFound(f.Message),
        IFailure f                 => StatusCode(500, f.Message),
        _                          => StatusCode(500, "Error desconocido.")
    };
    // nota: en este proyecto el Presenter hace esto en lugar del Controller
}
```

---

## `ResultViewModel<T>` — el puente al HTTP

```csharp
// Common — estructura que serializa como JSON
public class ResultViewModel<T>
{
    public object?  Data        { get; private set; }
    public bool     IsSuccess   { get; private set; }
    public string   Message     { get; private set; } = string.Empty;
    public DateTime UtcTimeStamp { get; } = DateTime.UtcNow;

    // Éxito con ISuccess<TDto> — Data = success.Data
    public ResultViewModel<T> Set(ISuccess<TDto> success) { ... }

    // Éxito con datos custom — Data = el objeto
    public ResultViewModel<T> OK(object data) { ... }

    // Fallo — IsSuccess = false
    public ResultViewModel<T> Fail(string message) { ... }
}
```

```csharp
// Controller — la lógica HTTP es trivial porque el ViewModel ya tiene el estado
return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);

// Respuesta JSON:
// {
//   "data": { "publicId": "...", "fullName": "..." },
//   "isSuccess": true,
//   "message": "",
//   "utcTimeStamp": "2026-05-11T..."
// }
```


---

*Rogelio Arriaga Gonzalez*
