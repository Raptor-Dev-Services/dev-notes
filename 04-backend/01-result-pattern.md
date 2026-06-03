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

El proyecto usa `Common.Results`. Las interfaces que tipan el resultado:

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

## Variantes del Operation Result (Ferreira)

Ferreira (*Architecting ASP.NET Core Applications*, Ch.13) documenta una progresión de implementaciones desde la más simple hasta la más robusta. Cada forma es válida; la elección depende de cuánta información necesita el caller.

### Forma 1 — Indicador booleano simple

La más básica: solo comunica éxito o falla. Útil como base extensible (puede agregar propiedades después sin romper consumers).

```csharp
// La diferencia con retornar bool directo: el record es extensible sin romper consumers
public record class OperationResult(bool Succeeded);
```

### Forma 2 — Un solo mensaje de error

El éxito se infiere de la ausencia de error:

```csharp
public record class OperationResult
{
    public bool Succeeded => string.IsNullOrWhiteSpace(ErrorMessage);
    public string? ErrorMessage { get; init; }
}

// Retornar desde el ejecutor:
return success
    ? new()
    : new() { ErrorMessage = "Algo salió mal." };
```

### Forma 3 — Agregar un valor de retorno

Para operaciones que producen datos en el camino exitoso:

```csharp
public record class OperationResult
{
    public bool Succeeded => string.IsNullOrWhiteSpace(ErrorMessage);
    public string? ErrorMessage { get; init; }
    public int? Value { get; init; }  // nullable: puede ser null en caso de error
}
```

### Forma 4 — Múltiples errores (validación)

Cuando se necesitan reportar varios errores a la vez (ej. validación de formularios):

```csharp
using System.Collections.Immutable;

public record class OperationResult
{
    public OperationResult()
    {
        Errors = ImmutableList<string>.Empty;
    }
    public OperationResult(params string[] errors)
    {
        Errors = errors.ToImmutableList();
    }
    public bool Succeeded => !HasErrors();
    public int? Value { get; init; }
    public IReadOnlyCollection<string> Errors { get; init; }
    public bool HasErrors() => Errors?.Count > 0;
}

// Retornar desde el ejecutor:
return success
    ? new() { Value = randomNumber }
    : new("Error A.", "Error B.");
```

### Forma 5 — Mensajes con nivel de severidad

Cuando la respuesta no es solo "error/no error" sino también advertencias e información:

```csharp
public enum OperationResultSeverity { Information, Warning, Error }

public record class OperationResultMessage(
    string Message,
    OperationResultSeverity Severity);

public record class OperationResult
{
    public OperationResult(params OperationResultMessage[] messages)
    {
        Messages = messages.ToImmutableList();
    }
    public bool Succeeded => !HasErrors();
    public int? Value { get; init; }
    public IReadOnlyCollection<OperationResultMessage> Messages { get; init; }
    public bool HasErrors()
    {
        return FindErrors().Any();
    }
    private IEnumerable<OperationResultMessage> FindErrors()
        => Messages.Where(x => x.Severity == OperationResultSeverity.Error);
}

// Salida JSON cuando el resultado tiene advertencias:
// {
//   "succeeded": true,
//   "value": 56,
//   "messages": [
//     { "message": "Informativo!", "severity": "Information" },
//     { "message": "Algo puede fallar después.", "severity": "Warning" }
//   ]
// }
```

### Forma 6 — Subclases con static factory methods

La más robusta: el tipo mismo distingue éxito de fallo. El contrato de la API de la clase base es mínimo (solo `Succeeded`) y cada subclase expone solo lo que necesita:

```csharp
public abstract record class OperationResult
{
    private OperationResult() { }  // constructor privado — solo subclases internas

    public abstract bool Succeeded { get; }

    // Static factories — única forma de instanciar
    public static OperationResult Success(int? value = null)
        => new SuccessfulOperationResult { Value = value };

    public static OperationResult Failure(params OperationResultMessage[] errors)
        => new FailedOperationResult(errors);

    // Subclases privadas — inaccesibles desde fuera
    private record class SuccessfulOperationResult : OperationResult
    {
        public override bool Succeeded { get; } = true;
        public virtual int? Value { get; init; }
    }

    private record class FailedOperationResult : OperationResult
    {
        public FailedOperationResult(params OperationResultMessage[] errors)
        {
            Messages = errors.ToImmutableList();
        }
        public override bool Succeeded { get; } = false;
        public ImmutableList<OperationResultMessage> Messages { get; }
    }
}

// Uso — lecturas claras de intención:
return OperationResult.Success(randomNumber);
return OperationResult.Failure(new OperationResultMessage("Error.", OperationResultSeverity.Error));
```

---

## Ejemplo de dominio real — registro a concierto (Ferreira)

Ilustra cómo el pattern se aplica a un caso de negocio real con factory methods y `MemberNotNullWhen`:

```csharp
using System.Diagnostics.CodeAnalysis;

public record class ConcertRegistrationResult
{
    // Atributos que le dicen al compilador qué propiedades son non-null
    // según el valor de RegistrationSucceeded
    [MemberNotNullWhen(false, nameof(ErrorMessage))]
    [MemberNotNullWhen(true, nameof(ConfirmationNumber))]
    public bool RegistrationSucceeded { get; init; }

    public User User { get; init; } = null!;
    public Concert Concert { get; init; } = null!;
    public string? ConfirmationNumber { get; init; }
    public string? ErrorMessage { get; init; }

    // Constructor privado — fuerza uso de factory methods
    private ConcertRegistrationResult() { }

    public static ConcertRegistrationResult CreateSuccess(
        User user, Concert concert, string confirmationNumber)
        => new() { RegistrationSucceeded = true, User = user,
                   Concert = concert, ConfirmationNumber = confirmationNumber };

    public static ConcertRegistrationResult CreateFailure(
        User user, Concert concert, string errorMessage)
        => new() { RegistrationSucceeded = false, User = user,
                   Concert = concert, ErrorMessage = errorMessage };
}

// El servicio (ejecutor):
public class ConcertRegistrationService
{
    public async Task<ConcertRegistrationResult> RegisterAsync(User user, Concert concert)
    {
        var (success, confirmationNumber) = await SimulatedRegistrationProcessAsync(user, concert);

        if (!success)
            return ConcertRegistrationResult.CreateFailure(
                user, concert, "El registro al concierto falló.");

        return ConcertRegistrationResult.CreateSuccess(user, concert, confirmationNumber);
    }
}

// Consumer (endpoint Minimal API):
app.MapPost("/concerts/{concertId}/register",
    async Task<Results<Ok<ConcertRegistrationResult>, BadRequest<ConcertRegistrationResult>>>
    (int concertId, ConcertRegistrationService service) =>
    {
        var user    = GetCurrentUser();
        var concert = GetConcert(concertId);
        var result  = await service.RegisterAsync(user, concert);

        if (result.RegistrationSucceeded)
            return TypedResults.Ok(result);

        // MemberNotNullWhen garantiza que ErrorMessage no es null aquí
        await LogErrorMessageAsync(result.ErrorMessage);
        return TypedResults.BadRequest(result);
    });
```

---

## Estado parcial (`OperationStatus`)

Ferreira sugiere agregar un tercer estado para operaciones con resultados mixtos:

```csharp
public enum OperationStatus { Success, Failure, PartialSuccess }

// En el OperationResult:
public OperationStatus Status => HasErrors()
    ? OperationStatus.Failure
    : (HasWarnings() ? OperationStatus.PartialSuccess : OperationStatus.Success);

// Consumer puede manejar los tres estados sin inspeccionar mensajes individuales:
switch (result.Status)
{
    case OperationStatus.Success:        HandleSuccess(result); break;
    case OperationStatus.PartialSuccess: HandlePartial(result); break;
    case OperationStatus.Failure:        HandleFailure(result); break;
}
```

---

## Ventajas y desventajas del Operation Result (Ferreira)

### Ventajas
- **Explicitud:** el tipo de retorno documenta todos los estados posibles. Más claro que saber qué excepciones pueden volar.
- **Rendimiento:** retornar un objeto es marginalmente más rápido que crear un stack trace de excepción.
- **Flexibilidad de diseño:** permite transportar mensajes de advertencia e información, no solo errores.

### Desventajas
- **Propagación manual:** el resultado debe pasarse hacia arriba en la pila de llamadas de forma explícita. Si debe recorrer muchos niveles, se vuelve tedioso.
- **Superficie de API grande:** es fácil exponer propiedades que no aplican a todos los escenarios (ej. `Value` puede ser null en error). El trade-off es siempre legibilidad vs perfección de diseño.

> **Regla de Ferreira:** "Cuando las ventajas superan los impactos menores de las violaciones de SOLID, es aceptable dejarlas pasar. Los principios son ideales, no leyes."

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

## Glosario

| Término | Definición |
|---------|-----------|
| Result Pattern | Patrón que modela el resultado de una operación como un objeto con estados éxito o fallo, sin usar excepciones para el flujo de negocio |
| IResponse | Interfaz base de todos los resultados posibles de un caso de uso |
| ISuccess\<T\> | Interfaz que representa un resultado exitoso con datos de tipo T |
| IFailure | Interfaz que representa un resultado fallido con un mensaje de error |
| INotFoundFailure | Tipo de fallo que mapea a HTTP 404 — recurso no encontrado |
| IConflictFailure | Tipo de fallo que mapea a HTTP 409 — conflicto de unicidad o estado |
| IValidationFailure | Tipo de fallo que mapea a HTTP 400 — datos de entrada inválidos |
| Presenter | Clase que recibe la respuesta del Handler vía Publish y construye el ViewModel |
| ResultViewModel\<T\> | Clase que serializa como JSON el resultado final enviado al cliente HTTP |
| Railway-Oriented Programming | Concepto funcional de dos rieles (éxito/fallo) que inspira el Result Pattern |
| Pattern Matching | Técnica de C# usada en el Presenter para discriminar entre tipos de IResponse |
| DTO | Data Transfer Object — objeto plano con solo los campos que necesita el cliente |
| INotFoundFailure | Respuesta semántica de negocio que el Presenter traduce a HTTP 404 sin lanzar excepción |
| OperationResultSeverity | Enumeración con tres niveles: `Information`, `Warning`, `Error` — permite diferenciar tipos de mensajes en el resultado |
| Static Factory Method | Método estático que crea instancias del resultado (`Success(...)`, `Failure(...)`) — encapsula la lógica de construcción y fuerza el uso del contrato correcto |
| `MemberNotNullWhen` | Atributo de C# que le indica al compilador qué propiedad es non-null según el valor de un booleano — elimina warnings de nullable en el consumer |
| Constructor privado | Técnica para forzar el uso de static factory methods — nadie puede instanciar la clase directamente |
| OperationStatus | Enumeración con tres estados: `Success`, `PartialSuccess`, `Failure` — útil cuando una operación puede tener resultados mixtos (algunos errores son tolerables) |
| ImmutableList\<T\> | Colección inmutable de .NET usada en la lista de errores/mensajes — previene que actores externos muten los resultados |
| Subclase privada anidada | Clase definida dentro de otra con visibilidad `private` — la única forma de heredar de un padre con constructor privado; inaccesible desde fuera |

---

> Fuente adicional: *Architecting ASP.NET Core Applications* (Carl-Hugo Marcotte / Ferreira) — Ch.13 Operation Result Pattern

*Rogelio Arriaga Gonzalez*
