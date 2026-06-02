# 12 — Validación: FluentValidation + Pipeline Behavior

Validar los datos de entrada antes de que lleguen al Handler, de forma centralizada y componible.

> Fuente: *Web API Development with ASP.NET Core 8* (Xiaodi Yan) — Ch.5 Model Validation and Data Annotations

---

## El problema que resuelve

Sin validación centralizada, cada Handler tiene que verificar manualmente sus precondiciones:

```csharp
// ❌ Validación dispersa — lógica de validación mezclada con lógica de negocio
public async Task<InsertExampleUserResponse> Handle(
    InsertExampleUserRequest request, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(request.FullName))
        return new InsertExampleUserValidationFailure("El nombre es requerido.");
    if (request.FullName.Length > 100)
        return new InsertExampleUserValidationFailure("Máximo 100 caracteres.");
    if (!request.Email.Contains('@'))
        return new InsertExampleUserValidationFailure("Email inválido.");
    if (string.IsNullOrWhiteSpace(request.Password))
        return new InsertExampleUserValidationFailure("La contraseña es requerida.");
    if (request.Password.Length < 8)
        return new InsertExampleUserValidationFailure("Mínimo 8 caracteres.");

    // ... lógica real del caso de uso ...
}
```

FluentValidation + un Pipeline Behavior extrae esa validación del Handler y la ejecuta automáticamente antes de que llegue a él.

---

## FluentValidation — validator del Request

```xml
<!-- Application/Application.csproj -->
<PackageReference Include="FluentValidation"                                    Version="11.*" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions"     Version="11.*" />
```

```csharp
// Application/UseCases/ExampleUsers/Insert/InsertExampleUserValidator.cs
public sealed class InsertExampleUserRequestValidator
    : AbstractValidator<InsertExampleUserRequest>
{
    public InsertExampleUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(100).WithMessage("Máximo 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido.")
            .EmailAddress().WithMessage("Email con formato inválido.")
            .MaximumLength(200).WithMessage("Máximo 200 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(8).WithMessage("Mínimo 8 caracteres.")
            .Matches("[A-Z]").WithMessage("Debe contener al menos una mayúscula.")
            .Matches("[0-9]").WithMessage("Debe contener al menos un número.");
    }
}
```

### Reglas avanzadas

```csharp
// Condicional
RuleFor(x => x.DiscountCode)
    .NotEmpty().WithMessage("El código es requerido.")
    .When(x => x.HasDiscount);   // solo valida si HasDiscount == true

// Colecciones
RuleForEach(x => x.Tags)
    .NotEmpty().WithMessage("Un tag no puede estar vacío.")
    .MaximumLength(50);

// Regla custom
RuleFor(x => x.StartDate)
    .Must(date => date > DateTime.UtcNow)
    .WithMessage("La fecha de inicio debe ser futura.");

// Validador anidado
RuleFor(x => x.Address)
    .SetValidator(new AddressValidator());

// Validación async (hit a DB)
RuleFor(x => x.Email)
    .MustAsync(async (email, ct) =>
        !await _repo.EmailExistsAsync(email, ct))
    .WithMessage("El email ya está registrado.");
```

---

## Pipeline Behavior — ejecución automática antes del Handler

El Pipeline Behavior intercepta el `IMediator.Send()` y corre los validators antes de llamar al Handler.

```csharp
// Application/Behaviors/ValidationBehavior.cs
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest  : IRequest<TResponse>
    where TResponse : IResponse
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest                          request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken                 ct)
    {
        // Si no hay validators registrados para este Request, pasar directo
        if (!_validators.Any())
            return await next();

        var context  = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        // Sin errores → continuar al Handler
        if (failures.Count == 0)
            return await next();

        // Con errores → lanzar excepción (el GlobalExceptionHandler devuelve 400)
        throw new ValidationException(failures);
    }
}
```

### Alternativa: retornar IValidationFailure en lugar de lanzar excepción

Si el diseño prefiere el Result Pattern puro (sin excepciones para validación):

```csharp
// Requiere que TResponse tenga un constructor de fallo conocido
// Solo funciona si IResponse tiene una forma de construir un fallo genérico.
// La opción más pragmática es lanzar ValidationException y capturarla en el GlobalHandler.
```

---

## Registro en DI

```csharp
// Application/ServiceCollectionEx.cs
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    // Registra automáticamente todos los AbstractValidator<T> del assembly
    services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

    // Registrar el behavior — se ejecuta para todos los IRequest del mediator
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    // ... AddMediator, handlers ...
    return services;
}
```

**Orden de registro importa:** el ValidationBehavior debe registrarse antes del Handler en el pipeline. Con el mediator de `Common.Messaging`, el orden es el orden de registro.

---

## Manejo del ValidationException en el Global Handler

```csharp
// WebApi/Exceptions/GlobalExceptionHandler.cs
public async ValueTask<bool> TryHandleAsync(
    HttpContext httpContext, Exception exception, CancellationToken ct)
{
    if (exception is ValidationException validationEx)
    {
        var errors = validationEx.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status   = StatusCodes.Status400BadRequest,
            Title    = "Validation Error",
            Detail   = "Uno o más campos no son válidos.",
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode  = 400;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);
        return true;
    }

    // ... resto de tipos de excepción ...
    return false;
}
```

**Respuesta JSON resultante:**

```json
{
  "type":   "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title":  "Validation Error",
  "status": 400,
  "detail": "Uno o más campos no son válidos.",
  "errors": {
    "Email":    ["Email con formato inválido."],
    "Password": ["Mínimo 8 caracteres.", "Debe contener al menos una mayúscula."]
  },
  "traceId": "00-abc123..."
}
```

---

## El Handler limpio después de extraer la validación

```csharp
// ✓ Handler sin validación — solo lógica de negocio
public async Task<InsertExampleUserResponse> Handle(
    InsertExampleUserRequest request, CancellationToken ct)
{
    // Llega aquí solo si pasó la validación
    var emailExists = await _repo.EmailExistsAsync(request.Email, ct);
    if (emailExists)
        return new InsertExampleUserConflictFailure("El email ya está registrado.");

    var user = new ExampleUser
    {
        PublicId  = Guid.NewGuid(),
        FullName  = request.FullName,
        Email     = request.Email,
        IsActive  = true,
        CreatedBy = _currentUser.UserId
    };

    await _repo.InsertAsync(user, ct);
    return new InsertExampleUserSuccess(new ExampleUserDto(user));
}
```

---

## Relación con back-template

`back-template/docs/Security.md` documenta FluentValidation y el Pipeline Behavior en el contexto del proyecto. En este repo, el foco es entender el patrón desde cero.

Para implementarlo en el proyecto:
1. Agregar paquetes FluentValidation a `Application.csproj`.
2. Crear `Application/Behaviors/ValidationBehavior.cs`.
3. Crear validators junto a cada Request que los necesite.
4. Registrar en `Application/ServiceCollectionEx.cs`.
5. Agregar manejo de `ValidationException` en `GlobalExceptionHandler`.

---

## Cuándo usar / Cuándo no usar

| Escenario | Decisión |
|-----------|----------|
| Validar campos de entrada de usuario (HTTP body) | ✓ FluentValidation |
| Verificar que una entidad existe en DB | ✓ Validator async o dentro del Handler |
| Reglas de negocio complejas con múltiples entidades | ✗ No en validator — en el Handler |
| Invariantes de la entidad de dominio | ✗ No en validator — en el constructor/método de la entidad |
| Requests internos entre servicios conocidos | Depende — si el origen es confiable, validación mínima |

**Separación clave:** FluentValidation valida el *formato* de la entrada (campos requeridos, longitudes, regex). La *lógica de negocio* ("el email ya existe", "el usuario puede editar este recurso") va en el Handler.


---

## Glosario

| Término | Definición |
|---------|-----------|
| FluentValidation | Librería de .NET para definir reglas de validación con API fluida sobre clases de validación separadas |
| AbstractValidator\<T\> | Clase base de FluentValidation que se hereda para definir las reglas de validación del tipo T |
| RuleFor | Método de FluentValidation para configurar reglas sobre una propiedad específica del request |
| ValidationBehavior | Pipeline behavior que ejecuta todos los IValidator registrados antes de pasar al handler |
| IValidator\<T\> | Interfaz de FluentValidation inyectada en el behavior para validar el request de tipo T |
| ValidationException | Excepción lanzada por FluentValidation cuando una o más reglas fallan |
| ValidationProblemDetails | Respuesta HTTP estándar con lista de errores de validación, compatible con RFC 7807 |
| GlobalExceptionHandler | Middleware que captura ValidationException y la convierte en HTTP 400 con detalles |
| MustAsync | Método de FluentValidation para reglas de validación asíncronas que consultan base de datos |
| IValidationFailure | Tipo de resultado del Result Pattern que representa errores de validación de negocio |
| WithMessage | Método de FluentValidation para personalizar el mensaje de error de una regla |
| Separación de responsabilidades | Principio que reserva FluentValidation para formato/estructura y el Handler para lógica de negocio |

---

*Rogelio Arriaga Gonzalez*
