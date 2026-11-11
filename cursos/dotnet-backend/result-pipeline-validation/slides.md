<!-- _class: lead -->

# Result Pattern  -  Pipeline Behaviors  -  Validación

## Material extra  -  Sistema de mensajería  -  .NET 10

Rogelio Arriaga González  -  Raptor Dev Services

---

<!-- _class: lead -->

# Parte 1

## Result Pattern
### Por qué evitar excepciones en flujos de negocio

---

## El problema: excepciones como control de flujo

```csharp
//  Excepción para un caso esperado de negocio
public async Task<ContactDto> GetContactAsync(Guid id, CancellationToken ct)
{
    var contact = await _repo.GetByIdAsync(id, ct);
    if (contact is null)
        throw new NotFoundException($"Contacto {id} no encontrado.");
    return new ContactDto(contact);
}
```

```csharp
//  El llamador captura para manejar el flujo normal
try
{
    var contact = await _service.GetContactAsync(id, ct);
    return Ok(contact);
}
catch (NotFoundException ex)
{
    return NotFound(ex.Message);
}
```

Las excepciones son caras, ocultan el contrato y mezclan errores esperados con bugs.

---

## La solución: Result Pattern

El **Result Pattern** hace que los posibles resultados sean explícitos en la firma del método.

```
  Task<ContactDto>           -  solo el camino feliz; errores = excepciones ocultas
  Task<GetContactResponse>   -  éxito o fallo son tipos concretos y manejables
```

| Criterio | Excepción | Result Pattern |
|----------|-----------|----------------|
| Contrato visible | No | Sí |
| Rendimiento | Caro (stack unwind) | Barato (objeto normal) |
| Casos esperados | Mezclados con bugs | Separados |
| Legibilidad del Handler | Verboso (try/catch) | Limpio (return type) |

---

<!-- _class: lead -->

# Parte 2

## Result Pattern en el back-template
### Result  -  ISuccess  -  IFailure  -  SuccessResult  -  FailureResult

---

## Result  -  clase abstracta base (Common/Results/Result.cs)

```csharp
using Common.Messaging;

namespace Common.Results
{
    public abstract class Result : IResponse
    {
        public static Result OK()               => new SuccessResult();
        public static Result Fail(string message) => new FailureResult(message);
        public static Result Fail(Exception ex)   => new FailureResult(ex);

        public static Result<T> OK<T>(T data)         => new SuccessResult<T>(data);
        public static Result<T> Fail<T>(string message) => new FailureResult<T>(message);
        public static Result<T> Fail<T>(Exception ex)   => new FailureResult<T>(ex);
    }

    public abstract class Result<T> : IResponse
    { }
}
```

> `Result` implementa `IResponse`  -  los Handlers retornan subtipos de `IResponse`, no de `Result` directamente.

---

## ISuccess e IFailure (Common/Results/)

```csharp
// Common/Results/ISuccess.cs
namespace Common.Results
{
    public interface ISuccess
    { }

    public interface ISuccess<T> : ISuccess
    {
        T Data { get; }
    }
}
```

```csharp
// Common/Results/IFailure.cs
namespace Common.Results
{
    public interface IFailure        { string Message { get; } }
    public interface IValidationFailure : IFailure { }
    public interface INotFoundFailure   : IFailure { }
    public interface IConflictFailure   : IFailure { }
}
```

Las interfaces son **marcadores semánticos**  -  el Presenter las usa para decidir el HTTP status code.

---

## SuccessResult (Common/Results/SuccessResult.cs)

```csharp
namespace Common.Results
{
    public sealed class SuccessResult : Result, ISuccess
    { }

    public sealed class SuccessResult<T> : Result<T>, ISuccess<T>
    {
        public SuccessResult(T data) => Data = data;

        public T Data { get; }

        public static implicit operator SuccessResult<T>(T data) => new(data);
    }
}
```

El operador implícito permite asignar `T` directamente donde se espera `SuccessResult<T>`.

---

## FailureResult (Common/Results/FailureResult.cs)

```csharp
namespace Common.Results
{
    public sealed class FailureResult : Result, IFailure
    {
        public FailureResult(string message) => Exception = new Exception(message);
        public FailureResult(Exception ex)   => Exception = ex;

        public Exception Exception { get; }
        public string Message => Exception.Message;
    }

    public sealed class FailureResult<T> : Result<T>, IFailure
    {
        public FailureResult(string message) => Exception = new Exception(message);
        public FailureResult(Exception ex)   => Exception = ex;

        public Exception Exception { get; }
        public string Message => Exception.Message;
    }
}
```

---

<!-- _class: lead -->

# Parte 3

## Responses por caso de uso
### abstract record  -  sealed records  -  jerarquía específica

---

## abstract record Response  -  base de cada caso de uso

Cada caso de uso tiene **su propia jerarquía de responses**, nunca un tipo genérico compartido.

```csharp
// Users.Application/UseCases/GetUserProfile/Responses/GetUserProfileResponse.cs
using Common.Messaging;

namespace Users.Application.UseCases.GetUserProfile.Responses;

public abstract record GetUserProfileResponse : IResponse;
```

`IResponse` viene de `Common.Messaging`  -  es el contrato que el pipeline necesita para publicar.

---

## sealed records de éxito y fallo

```csharp
// Users.Application/UseCases/GetUserProfile/Responses/GetUserProfileSuccess.cs
using Common.Results;
using Users.Contracts.Dtos;

namespace Users.Application.UseCases.GetUserProfile.Responses;

public sealed record GetUserProfileSuccess(UserProfileDto Data)
    : GetUserProfileResponse, ISuccess<UserProfileDto>;
```

```csharp
// Users.Application/UseCases/GetUserProfile/Responses/GetUserProfileNotFoundFailure.cs
using Common.Results;

namespace Users.Application.UseCases.GetUserProfile.Responses;

public sealed record GetUserProfileNotFoundFailure(string Message)
    : GetUserProfileResponse, INotFoundFailure;
```

**Patron:** una `Response` abstracta por caso de uso, múltiples `sealed records` como variantes.

---

## Jerarquía de responses para un caso de uso de CRM

```
GetContactResponse           (abstract record : IResponse)
|-- GetContactSuccess         (sealed record : ISuccess<ContactDto>)
`-- GetContactNotFoundFailure (sealed record : INotFoundFailure)

RegisterAgentResponse        (abstract record : IResponse)
|-- RegisterAgentSuccess      (sealed record : ISuccess<AgentDto>)
|-- RegisterAgentEmailConflictFailure (sealed record : IConflictFailure)
`-- RegisterAgentValidationFailure    (sealed record : IValidationFailure)

GetConversationResponse      (abstract record : IResponse)
|-- GetConversationSuccess    (sealed record : ISuccess<ConversationDto>)
`-- GetConversationNotFoundFailure (sealed record : INotFoundFailure)
```

Cada `sealed record` implementa la interfaz de marcador que el Presenter necesita para mapear al HTTP status code correcto.

---

## Comparación: excepción vs Response tipada

```csharp
//  Basado en excepciones  -  el contrato no es visible en la firma
public Task<GetContactResponse> Handle(
    GetContactRequest req, CancellationToken ct)
{
    throw new NotFoundException("...");   // oculto al llamador
}
```

```csharp
//  Result Pattern  -  el compilador conoce todos los casos posibles
public async Task<GetContactResponse> Handle(
    GetContactRequest req, CancellationToken ct)
{
    var contact = await _repo.GetByIdAsync(req.Id, ct);

    if (contact is null)
        return new GetContactNotFoundFailure("Contacto no encontrado.");

    return new GetContactSuccess(new ContactDto(contact));
}
```

El Handler no lanza excepciones de negocio  -  retorna tipos que modelan cada resultado.

---

## Handler real del back-template  -  GetUserProfileHandler

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

    public GetUserProfileHandler(IUserProfileRepository profiles)
        => _profiles = profiles;

    public async Task<GetUserProfileResponse> Handle(
        GetUserProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByPublicIdAsync(
            request.PublicId, request.TenantId, cancellationToken);

        if (profile is null)
            return new GetUserProfileNotFoundFailure("Perfil de usuario no encontrado.");

        return new GetUserProfileSuccess(new UserProfileDto(
            profile.PublicId, profile.FullName, profile.IsActive,
            profile.CreatedAtUtc, profile.UpdatedAtUtc));
    }
}
```

---

<!-- _class: lead -->

# Parte 4

## Presenters
### INotificationHandler  -  Pattern matching  -  Mapeo a HTTP

---

## ¿Qué es un Presenter?

El **Presenter** traduce una `Response` del dominio a algo que el Controller puede devolver como HTTP.

```
Handler              ->  retorna GetContactResponse
                                ↓
InteractorPipeline   ->  Mediator.Publish(response)
                                        ↓
Presenter            ->  INotificationHandler<GetContactResponse>
                                ↓  pattern matching
ResultViewModel      ->  IsSuccess / Data / Message
                                ↓
Controller           ->  Ok(viewModel) / NotFound(viewModel) / ...
```

El Presenter implementa `INotificationHandler<TResponse>`  -  NO es un servicio inyectado directamente en el Controller.

---

## Presenter real  -  GetUserProfilePresenter

```csharp
// Users.Presentation/Presenters/GetUserProfilePresenter.cs
using Common.Messaging;
using Common.Results;
using Common.ViewModels;
using Users.Application.UseCases.GetUserProfile.Responses;
using Users.Contracts.Dtos;
using Users.Presentation;

namespace Users.Presentation.Presenters;

public sealed class GetUserProfilePresenter
    : INotificationHandler<GetUserProfileResponse>
{
    private readonly ResultViewModel<UsersController> _viewModel;

    public GetUserProfilePresenter(ResultViewModel<UsersController> viewModel)
        => _viewModel = viewModel;

    public Task Handle(GetUserProfileResponse notification, CancellationToken cancellationToken)
    {
        if (notification is IFailure failure)
            _viewModel.Fail(failure.Message);
        else if (notification is ISuccess<UserProfileDto> success)
            _viewModel.Set(success);

        return Task.CompletedTask;
    }
}
```

---

## Orden en el pattern matching  -  IFailure primero

```csharp
//  CORRECTO  -  IFailure primero; las subinterfaces se capturan igualmente
if (notification is IFailure failure)
    _viewModel.Fail(failure.Message);          // -> aplica a INotFoundFailure, IConflictFailure, etc.
else if (notification is ISuccess<ContactDto> success)
    _viewModel.Set(success);                   // -> 200
```

```csharp
//  TAMBIÉN CORRECTO  -  más específico primero cuando el Presenter necesita
//    distinguir el HTTP status code por tipo de fallo
if (notification is INotFoundFailure notFound)
    _viewModel.Fail(notFound.Message);         // -> 404
else if (notification is IConflictFailure conflict)
    _viewModel.Fail(conflict.Message);         // -> 409
else if (notification is IFailure failure)
    _viewModel.Fail(failure.Message);          // -> 400 genérico
else if (notification is ISuccess<ContactDto> success)
    _viewModel.Set(success);                   // -> 200
```

Cuando distingues HTTP status codes por tipo de fallo, pon los tipos más específicos antes de `IFailure`.

---

## ResultViewModel  -  estructura real (Common/ViewModels/)

```csharp
// Common/ViewModels/ResultViewModel.cs
using Common.Results;

namespace Common.ViewModels
{
    public class ResultViewModel<T>
    {
        public object?   Data         { get; private set; }
        public bool      IsSuccess    { get; private set; }
        public string?   Message      { get; private set; }
        public DateTime  UtcTimeStamp { get; private set; }

        public ResultViewModel<T> Fail(string message, int statusCode = 400)
        {
            Data      = null;
            Message   = message;
            IsSuccess = false;
            UtcTimeStamp = DateTime.UtcNow;
            return this;
        }

        public void Set<TData>(ISuccess<TData> success, Func<TData, object>? callback = null)
        {
            Data      = callback?.Invoke(success.Data) ?? success.Data;
            IsSuccess = true;
            Message   = null;
            UtcTimeStamp = DateTime.UtcNow;
        }
    }
}
```

---

<!-- _class: lead -->

# Parte 5

## InteractorPipeline
### IPipelineBehavior  -  Publish  -  Logging  -  BusinessRuleException

---

## MediatorAbstractions  -  las interfaces base (Common/Messaging/)

```csharp
// Common/Messaging/MediatorAbstractions.cs
namespace Common.Messaging
{
    public interface IResponse : INotification { }

    public interface IRequest<out TResponse> { }

    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

    public interface IPipelineBehavior<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(
            TRequest                          request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken                 cancellationToken);
    }

    public interface INotificationHandler<in TNotification>
        where TNotification : INotification
    {
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }
}
```

---

## InteractorPipeline  -  el behavior central del back-template

```csharp
// Common/Messaging/InteractorPipeline.cs
using Common.Results;
using Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace Common.Messaging
{
    public record InteractorPipeline<TRequest, TResponse>(
        IMediator Mediator,
        ILogger<InteractorPipeline<TRequest, TResponse>> Logger)
        : IPipelineBehavior<TRequest, TResponse>
            where TRequest  : IRequest<TResponse>
            where TResponse : IResponse
    {
        public async Task<TResponse> Handle(
            TRequest request, RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            Logger.LogInformation("{@Request}", request);
            TResponse response;
            try
            {
                response = await next().ConfigureAwait(false);
                // continúa en la siguiente diapositiva...
```

---

## InteractorPipeline  -  logging y Publish

```csharp
                if (response is IFailure failure)
                {
                    var typeOfResponse = typeof(TResponse);
                    var genericArgs    = typeOfResponse.GetGenericArguments();
                    var responseTypeName = genericArgs.Length > 0
                        ? genericArgs[0].Name
                        : typeOfResponse.Name;
                    Logger.LogWarning("{ResponseType}: {FailureMessage}",
                        responseTypeName, failure.Message);
                }
                else
                {
                    Logger.LogInformation("{@Response}", response);
                }
                await Mediator.Publish(response).ConfigureAwait(false);  // <- activa el Presenter
            }
            catch (BusinessRuleException ex)
            {
                Logger.LogError(ex, "Error: {ErrorMessage}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                var innerEx = ex;
                while (innerEx.InnerException != null) innerEx = innerEx.InnerException!;
                Logger.LogCritical(ex, "Error crítico: {ErrorMessage}", innerEx.Message);
                throw;
            }
            return response;
        }
    }
}
```

---

## Qué hace InteractorPipeline en cada paso

```
Controller: await _mediator.Send(request, ct)
                    ↓
InteractorPipeline.Handle()
    1. Logger.LogInformation("{@Request}", request)
                    ↓
    2. response = await next()      <- ejecuta el Handler real
                    ↓
    3. Si response is IFailure  -> LogWarning con tipo y mensaje
       Si no                   -> LogInformation con response completa
                    ↓
    4. await Mediator.Publish(response) <- activa todos los INotificationHandler<TResponse>
       (aquí se ejecuta el Presenter)
                    ↓
    5. return response
                    ↓
Controller: _viewModel.IsSuccess ? Ok(_viewModel) : BadRequest(_viewModel)
```

El `InteractorPipeline` combina: **logging + publish al Presenter** en un solo behavior.

---

## IInteractor  -  interfaz que usan los Handlers

```csharp
// Common/Abstractions/IInteractor.cs
using Common.Messaging;

namespace Common.Abstractions
{
    public interface IInteractor<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
        where TRequest  : IRequest<TResponse>
        where TResponse : IResponse
    { }
}
```

Los Handlers implementan `IInteractor<TRequest, TResponse>` en lugar de `IRequestHandler` directamente  -  es la convención del back-template.

```csharp
// Ejemplo de declaración de un Handler
public sealed class GetContactHandler
    : IInteractor<GetContactRequest, GetContactResponse>
{ ... }
```

---

<!-- _class: lead -->

# Parte 6

## Pipeline Behaviors adicionales
### ValidationBehavior  -  PerformanceBehavior  -  Orden de registro

---

## El problema sin Pipeline

```csharp
//  Sin pipeline  -  cada Handler repite el mismo logging y validación
public async Task<RegisterAgentResponse> Handle(
    RegisterAgentRequest req, CancellationToken ct)
{
    _logger.LogInformation("-> RegisterAgentRequest: {@Req}", req);  // repetido
    var sw = Stopwatch.StartNew();
    // ... lógica real ...
    _logger.LogInformation("<- en {Ms}ms", sw.ElapsedMilliseconds); // repetido
    return new RegisterAgentSuccess(dto);
}
// Lo mismo en GetContactHandler, UpdateConversationHandler, SendMessageHandler...
```

Con **Pipeline Behaviors**, el comportamiento cross-cutting se escribe una sola vez y aplica a todos los Handlers.

---

## Cómo fluye el pipeline completo

```
Request
    ↓
ValidationBehavior.Handle()
    si falla -> throw ValidationException
               -> GlobalExceptionHandler -> 400 ValidationProblemDetails
    si pasa  ↓
InteractorPipeline.Handle()
    LogInformation(request)
        ↓  <- await next()
        Handler.Handle()         <- lógica de negocio -> retorna Response
        ↑  retorna Response
    LogWarning (si IFailure) / LogInformation (si éxito)
    Mediator.Publish(response)   <- activa el Presenter
        ↓
Presenter.Handle(response)       <- llena ResultViewModel
    ↑
Response
    ↓
Controller devuelve HTTP response
```

---

## Registro en DI  -  el orden importa

```csharp
// Application/ServiceCollectionEx.cs
public static IServiceCollection AddApplicationServices(
    this IServiceCollection services)
{
    // ValidationBehavior va ANTES de InteractorPipeline
    // (el primero registrado es el más externo)
    services.AddScoped(
        typeof(IPipelineBehavior<,>),
        typeof(ValidationBehavior<,>));

    services.AddScoped(
        typeof(IPipelineBehavior<,>),
        typeof(InteractorPipeline<,>));

    // FluentValidation  -  detecta todos los AbstractValidator<T> del assembly
    services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

    return services;
}
```

> El primer behavior registrado es el más externo  -  envuelve a todos los demás.

---

## Casos de uso por behavior

| Behavior | Qué hace | Cuándo registrar |
|----------|----------|------------------|
| `ValidationBehavior` | Corre FluentValidation antes del Handler | Siempre que haya validators |
| `InteractorPipeline` | Logging + Publish al Presenter | Siempre  -  es el behavior central |
| `PerformanceBehavior` | Alerta si el Handler tarda > 500ms | Siempre en producción |
| `TransactionBehavior` | Abre y commitea una transacción de DB | Solo para Commands (escrituras) |
| `CachingBehavior` | Lee/escribe caché antes del Handler | Solo para Queries cacheables |

```csharp
// Behavior selectivo  -  solo aplica a Commands mediante marker interface
public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest  : ICommand<TResponse>   // marker interface
    where TResponse : IResponse
{ /* abre tx, commit, rollback */ }
```

---

<!-- _class: lead -->

# Parte 7

## ValidationBehavior
### FluentValidation  -  AbstractValidator  -  RuleFor

---

## El problema sin validación centralizada

```csharp
//  Validación mezclada con lógica de negocio en el Handler
public async Task<RegisterAgentResponse> Handle(
    RegisterAgentRequest req, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(req.FullName))
        return new RegisterAgentValidationFailure("El nombre es requerido.");
    if (req.FullName.Length > 100)
        return new RegisterAgentValidationFailure("Máximo 100 caracteres.");
    if (!req.Email.Contains('@'))
        return new RegisterAgentValidationFailure("Email inválido.");
    if (string.IsNullOrWhiteSpace(req.Password))
        return new RegisterAgentValidationFailure("La contraseña es requerida.");
    // ... y recién empieza la lógica real ...
}
```

---

## AbstractValidator  -  validator del Request

```csharp
// Application/UseCases/Agents/Register/RegisterAgentRequestValidator.cs
using FluentValidation;

namespace Users.Application.UseCases.Agents.Register;

public sealed class RegisterAgentRequestValidator
    : AbstractValidator<RegisterAgentRequest>
{
    public RegisterAgentRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(100).WithMessage("Máximo 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido.")
            .EmailAddress().WithMessage("Email con formato inválido.")
            .MaximumLength(200);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Mínimo 8 caracteres.")
            .Matches("[A-Z]").WithMessage("Debe contener al menos una mayúscula.")
            .Matches("[0-9]").WithMessage("Debe contener al menos un número.");
    }
}
```

---

## ValidationBehavior  -  ejecución antes del Handler

```csharp
// Application/Behaviors/ValidationBehavior.cs
using FluentValidation;
using Common.Messaging;

namespace Users.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest  : IRequest<TResponse>
    where TResponse : IResponse
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any())
            return await next();     // sin validator -> continuar

        var context  = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);  // -> GlobalExceptionHandler -> 400
        return await next();
    }
}
```

---

## Reglas avanzadas de FluentValidation

```csharp
// Condicional  -  solo valida si HasDiscount == true
RuleFor(x => x.DiscountCode)
    .NotEmpty().WithMessage("El código es requerido.")
    .When(x => x.HasDiscount);
```

```csharp
// Colecciones  -  valida cada elemento de la lista
RuleForEach(x => x.Tags)
    .NotEmpty()
    .MaximumLength(50);
```

```csharp
// Regla custom con Must
RuleFor(x => x.ScheduledAt)
    .Must(date => date > DateTime.UtcNow)
    .WithMessage("La fecha debe ser futura.");
```

```csharp
// Async  -  consulta la base de datos desde el validator
RuleFor(x => x.Email)
    .MustAsync(async (email, ct) =>
        !await _repo.EmailExistsAsync(email, ct))
    .WithMessage("El email ya está registrado.");
```

---

## Handler limpio después de separar la validación

```csharp
//  Handler sin código de validación  -  solo lógica de negocio
using Common.Abstractions;
using Users.Application.UseCases.Agents.Register.Responses;
using Users.Domain.Repositories;

namespace Users.Application.UseCases.Agents.Register;

public sealed class RegisterAgentHandler
    : IInteractor<RegisterAgentRequest, RegisterAgentResponse>
{
    private readonly IAgentRepository _repo;

    public RegisterAgentHandler(IAgentRepository repo) => _repo = repo;

    public async Task<RegisterAgentResponse> Handle(
        RegisterAgentRequest request, CancellationToken ct)
    {
        // Llega aquí SOLO si pasó la validación de FluentValidation
        if (await _repo.EmailExistsAsync(request.Email, ct))
            return new RegisterAgentEmailConflictFailure("El email ya está registrado.");

        var agent = new Agent
        {
            PublicId = Guid.NewGuid(),
            FullName = request.FullName,
            Email    = request.Email,
            IsActive = true
        };
        await _repo.InsertAsync(agent, ct);
        return new RegisterAgentSuccess(new AgentDto(agent));
    }
}
```

---

<!-- _class: lead -->

# Parte 8

## Problem Details
### RFC 9457 (antes RFC 7807)  -  ProblemDetails en ASP.NET Core  -  Errores consistentes

Nota: RFC 9457 (julio de 2023, rfc-editor.org/rfc/rfc9457) obsoleta al RFC 7807; el formato `application/problem+json` y sus campos se mantienen.

---

## El problema: formatos de error ad-hoc

```json
//  Formato A  -  información mínima
{ "error": "Not found" }
```

```json
//  Formato B  -  estructura inconsistente entre endpoints
{ "success": false, "message": "Agente no encontrado", "code": 404 }
```

```json
//  Formato C  -  expone detalles técnicos al cliente
{
  "exception": "System.NullReferenceException",
  "stackTrace": "at App.Controllers...",
  "innerException": "..."
}
```

Los clientes no pueden procesar errores de forma uniforme. RFC 9457 (que reemplazó al 7807) resuelve esto.

---

## RFC 7807  -  estructura estándar

```json
{
  "type":     "https://httpstatuses.io/404",
  "title":    "Not Found",
  "status":   404,
  "detail":   "Contacto con ID abc-123 no encontrado.",
  "instance": "/api/contacts/abc-123",
  "traceId":  "00-4bf92f3577b34da6a3ce929d0e0e4736-01"
}
```

| Campo | Rol |
|-------|-----|
| `type` | URI que identifica el tipo de problema (constante por tipo de error) |
| `title` | Descripción corta  -  constante para el mismo `type` |
| `status` | HTTP status code |
| `detail` | Explicación legible de esta instancia específica |
| `instance` | URI de la request que causó el error |
| `traceId` | Extensión para correlación en logs |

---

## GlobalExceptionHandler  -  mapeo de ValidationException

```csharp
// WebApi/Exceptions/GlobalExceptionHandler.cs
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Users.WebApi.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
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

            var problem = new ValidationProblemDetails(errors)
            {
                Status   = 400,
                Title    = "Validation Error",
                Detail   = "Uno o más campos no son válidos.",
                Instance = httpContext.Request.Path,
                Type     = "https://httpstatuses.io/400"
            };
            problem.Extensions["traceId"] =
                Activity.Current?.Id ?? httpContext.TraceIdentifier;
            httpContext.Response.StatusCode  = 400;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(problem, ct);
            return true;
        }
        return false;
    }
}
```

---

## Respuesta 400 con errores por campo

```json
{
  "type":     "https://httpstatuses.io/400",
  "title":    "Validation Error",
  "status":   400,
  "detail":   "Uno o más campos no son válidos.",
  "instance": "/api/agents/register",
  "errors": {
    "Email":    ["Email con formato inválido."],
    "Password": [
      "Mínimo 8 caracteres.",
      "Debe contener al menos una mayúscula."
    ]
  },
  "traceId": "00-abc123..."
}
```

> `title` es constante para todos los errores 400. `detail` describe esta instancia específica.
> En **producción** nunca exponer stack trace ni mensajes de excepción internos.

---

## Configuración en Program.cs

```csharp
// Host/Program.cs
using System.Diagnostics;

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;

        if (!ctx.HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            ctx.ProblemDetails.Detail = null;  // limpiar en producción
        }
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

app.UseExceptionHandler();   // convierte excepciones no manejadas en ProblemDetails
app.UseStatusCodePages();    // convierte 404/405 bare en ProblemDetails
```

---

<!-- _class: lead -->

# Parte 9

## Flujo Completo
### Request -> ValidationBehavior -> InteractorPipeline -> Handler -> Presenter -> HTTP

---

## Diagrama del flujo completo

```
HTTP POST /api/agents/register
           ↓
Controller: await _mediator.Send(request, ct)
           ↓
[Pipeline  -  orden de ejecución]
ValidationBehavior    -> corre RegisterAgentRequestValidator
  si falla -> throw ValidationException
           -> GlobalExceptionHandler -> 400 ValidationProblemDetails
  si pasa  ↓
InteractorPipeline    -> LogInformation(request)
           ↓
RegisterAgentHandler  -> lógica de negocio -> retorna RegisterAgentResponse
           ↑
InteractorPipeline    -> LogWarning/LogInformation según IFailure o éxito
                     -> Mediator.Publish(response)
           ↓
RegisterAgentPresenter -> INotificationHandler<RegisterAgentResponse>
                       -> pattern matching -> _viewModel.Fail() o _viewModel.Set()
           ↓
Controller: _viewModel.IsSuccess ? Ok(_viewModel) : BadRequest(_viewModel)
```

---

## Código del Controller  -  minimal y declarativo

```csharp
// Users.Presentation/Controllers/AgentsController.cs
using Common.Messaging;
using Common.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Users.Presentation;

[ApiController]
[Route("api/agents")]
public sealed class AgentsController(
    IMediator mediator,
    ResultViewModel<AgentsController> viewModel) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterAgentBody body,
        CancellationToken ct)
    {
        var request = new RegisterAgentRequest(
            body.FullName, body.Email, body.Password);

        await mediator.Send(request, ct);

        return viewModel.IsSuccess
            ? Ok(viewModel)
            : BadRequest(viewModel);
    }
}
```

El Controller no contiene lógica de negocio ni pattern matching de responses. El Presenter llenó el `viewModel` cuando el `InteractorPipeline` hizo `Publish`.

---

## Presenter del caso Register con status codes distintos

```csharp
// Users.Presentation/Presenters/RegisterAgentPresenter.cs
using Common.Messaging;
using Common.Results;
using Common.ViewModels;
using Users.Application.UseCases.Agents.Register.Responses;
using Users.Contracts.Dtos;
using Users.Presentation;

namespace Users.Presentation.Presenters;

public sealed class RegisterAgentPresenter
    : INotificationHandler<RegisterAgentResponse>
{
    private readonly ResultViewModel<AgentsController> _viewModel;

    public RegisterAgentPresenter(ResultViewModel<AgentsController> viewModel)
        => _viewModel = viewModel;

    public Task Handle(RegisterAgentResponse notification, CancellationToken ct)
    {
        if (notification is IConflictFailure conflict)
            _viewModel.Fail(conflict.Message);          // -> 409
        else if (notification is IFailure failure)
            _viewModel.Fail(failure.Message);           // -> 400 genérico
        else if (notification is ISuccess<AgentDto> success)
            _viewModel.Set(success);                    // -> 200
        return Task.CompletedTask;
    }
}
```

---

## Tabla resumen  -  responsabilidades

| Componente | Responsabilidad |
|------------|----------------|
| `Request` | Datos de entrada del caso de uso |
| `AbstractValidator<Request>` | Validar formato de la entrada (campos, longitudes, regex) |
| `ValidationBehavior` | Ejecutar validators antes del Handler; throw si falla |
| `InteractorPipeline` | Logging + `Mediator.Publish(response)` -> activa el Presenter |
| `IInteractor<TReq, TRes>` | Interfaz base de los Handlers en el back-template |
| `Handler` | Lógica de negocio pura  -  retorna una `Response` tipada |
| `abstract record Response` | Contrato de salida del caso de uso (implementa `IResponse`) |
| `sealed record Success/Failure` | Variantes concretas  -  implementan `ISuccess<T>` o `IXxxFailure` |
| `Presenter` | `INotificationHandler<TResponse>`  -  pattern matching -> `ResultViewModel` |
| `ResultViewModel<T>` | DTO HTTP-ready  -  `IsSuccess`, `Data`, `Message` |
| `GlobalExceptionHandler` | Captura `ValidationException` -> `ValidationProblemDetails` 400 |
| `Controller` | Envía la request, devuelve el ViewModel como HTTP response |

---

## Cuándo usar cada herramienta

| Escenario | Herramienta |
|-----------|-------------|
| Validar campos de entrada (formato, longitud, regex) | `AbstractValidator` en FluentValidation |
| Verificar que una entidad existe en DB | Validator async o en el Handler |
| Reglas de negocio con múltiples entidades | En el Handler  -  no en el validator |
| Invariantes de la entidad de dominio | En el constructor/método de la entidad |
| Resultado not found | `sealed record ...NotFoundFailure : INotFoundFailure` |
| Resultado conflicto (email duplicado) | `sealed record ...ConflictFailure : IConflictFailure` |
| Error inesperado / bug | Excepción  -  el `GlobalExceptionHandler` devuelve 500 |
| Comportamiento cross-cutting (logging, perf) | Pipeline Behavior |

> **Separación clave:** FluentValidation valida el *formato*. El Handler verifica la *semántica de negocio*.

---

## Referencias dev-notes

| Tema | Archivo |
|------|---------|
| Result Pattern | `04-backend/01-result-pattern.md` |
| Pipeline Behaviors | `04-backend/03-pipeline-behaviors.md` |
| Validación FluentValidation | `04-backend/06-validation.md` |
| Problem Details RFC 7807 | `04-backend/08-problem-details.md` |

*Rogelio Arriaga González  -  Raptor Dev Services*
