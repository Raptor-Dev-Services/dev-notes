# 07 — Pipeline Behaviors

Los Pipeline Behaviors son middlewares del mediador — interceptan cada Request antes y después de que llegue al Handler. Permiten agregar comportamiento cross-cutting (que aplica a todos los casos de uso) sin modificar cada Handler individualmente.

> Fuente: *Architecting ASP.NET Core Applications* (Carl-Hugo Marcotte) — Ch.14 Mediator and CQRS Design Patterns

---

## El problema que resuelven

```csharp
// ❌ Sin pipeline — cada Handler repite el mismo código
public class GetExampleUserHandler : IRequestHandler<...>
{
    public async Task<GetExampleUserResponse> Handle(GetExampleUserRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Handling {Request}", req);        // ← repetido en cada handler
        var sw = Stopwatch.StartNew();
        try
        {
            var user = await _repo.GetByPublicIdAsync(req.PublicId, ct);
            _logger.LogInformation("Handled in {Ms}ms", sw.ElapsedMilliseconds);
            return /* ... */;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {Request}", req);
            throw;
        }
    }
}
// Lo mismo en InsertHandler, UpdateHandler, DeleteHandler...
```

Con Pipeline Behaviors, el comportamiento cross-cutting se escribe **una sola vez** y aplica automáticamente a todos los Handlers.

---

## Cómo funciona el Pipeline

```
Request
    ↓
LoggingBehavior.Handle()
    ↓ → await next()
    ValidationBehavior.Handle()
        ↓ → await next()
        PerformanceBehavior.Handle()
            ↓ → await next()
            Handler.Handle()   ← lógica de negocio real
            ↑ retorna Response
        ↑ mide tiempo, loguea si lento
    ↑ valida, retorna error si inválido
↑ loguea entrada/salida
Response
```

Es el mismo patrón que el middleware de ASP.NET Core, pero para el mediador interno.

---

## Interfaz base

El mediador de `Common.Messaging` expone la interfaz de pipeline behavior:

```csharp
// Common.Messaging — interfaz que implementan los behaviors
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest  : IRequest<TResponse>
    where TResponse : IResponse
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

// RequestHandlerDelegate — función que llama al siguiente en el pipeline
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
```

---

## Behavior 1: Logging

```csharp
// Application/Behaviors/LoggingBehavior.cs
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest  : IRequest<TResponse>
    where TResponse : IResponse
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("→ {Request}: {@Payload}", requestName, request);

        var response = await next();

        _logger.LogInformation("← {Request}: {ResponseType}", requestName, typeof(TResponse).Name);

        return response;
    }
}
```

---

## Behavior 2: Performance (slow query detection)

```csharp
// Application/Behaviors/PerformanceBehavior.cs
public sealed class PerformanceBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest  : IRequest<TResponse>
    where TResponse : IResponse
{
    private const int SlowRequestThresholdMs = 500;

    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var sw       = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            _logger.LogWarning(
                "Slow request: {Request} took {ElapsedMs}ms — payload: {@Payload}",
                typeof(TRequest).Name, sw.ElapsedMilliseconds, request);
        }

        return response;
    }
}
```

---

## Behavior 3: Validación (con FluentValidation)

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
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any())
            return await next();  // sin validators para este request → continuar

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (failures.Count > 0)
        {
            // Retornar un fallo de validación sin llegar al Handler
            // Necesita que TResponse implemente IResponse y exista un constructor de fallo
            throw new ValidationException(failures);
            // alternativa: retornar un IValidationFailure si TResponse lo soporta
        }

        return await next();
    }
}

// Validator de ejemplo (FluentValidation):
public sealed class GetExampleUserRequestValidator : AbstractValidator<GetExampleUserRequest>
{
    public GetExampleUserRequestValidator()
    {
        RuleFor(x => x.PublicId)
            .NotEmpty().WithMessage("El ID no puede estar vacío.");
    }
}
```

---

## Behavior 4: Exception handling

```csharp
// Application/Behaviors/ExceptionHandlingBehavior.cs
public sealed class ExceptionHandlingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest  : IRequest<TResponse>
    where TResponse : IResponse
{
    private readonly ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> _logger;

    public ExceptionHandlingBehavior(
        ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        try
        {
            return await next();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Request {Request} cancelled.", typeof(TRequest).Name);
            throw;  // dejar subir — el cliente canceló la request
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in {Request}: {@Payload}",
                typeof(TRequest).Name, request);
            throw;  // dejar subir al global exception handler del middleware
        }
    }
}
```

---

## Registro en DI

```csharp
// Application/ServiceCollectionEx.cs
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    // Handlers y mediator (ya configurado)
    services.AddMediator(/* ... */);

    // Pipeline behaviors — el orden de registro = orden de ejecución
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
    // ExceptionHandlingBehavior puede ir al final (más externo):
    // services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));

    // Validators de FluentValidation (si se usa)
    services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

    return services;
}
```

**Orden de ejecución** (el primero registrado es el más externo):

```
Request → LoggingBehavior → ValidationBehavior → PerformanceBehavior → Handler → Response
```

---

## El InteractorPipeline de Common.Messaging

El mediador de este proyecto tiene un `InteractorPipeline` propio que ejecuta los behaviors y luego hace el Publish de la response al Presenter:

```
Request
    ↓
[Pipeline Behaviors] (si están registrados)
    ↓
Handler.Handle()  → retorna Response
    ↓
InteractorPipeline: await Mediator.Publish(response, ct)
    ↓
Presenter.Handle(response, ct)  → llena _viewModel
    ↓
Controller: _viewModel.IsSuccess ? Ok() : StatusCode(500)
```

Los behaviors se ejecutan antes del Handler. El Publish al Presenter ocurre automáticamente después del Handler — no hay que llamarlo manualmente.

---

## Cuándo agregar un behavior vs modificar un Handler

| Comportamiento | Dónde va |
|---------------|----------|
| Aplica a TODOS los requests | Pipeline Behavior |
| Aplica a ALGUNOS requests (por tipo) | Pipeline Behavior con `where TRequest : ISpecificMarker` |
| Es específico de un caso de uso | En el Handler directamente |
| Es lógica de infraestructura | En la clase `...Sql` o repositorio |

```csharp
// Behavior selectivo — solo aplica a Commands (no a Queries)
public interface ICommand<TResponse> : IRequest<TResponse> { }  // marker interface

public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest  : ICommand<TResponse>   // ← solo para Commands
    where TResponse : IResponse
{
    // abre transacción, hace commit si el handler no falla
}
```


---

*Rogelio Arriaga Gonzalez*
