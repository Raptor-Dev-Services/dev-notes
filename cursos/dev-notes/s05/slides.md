<!-- _class: lead -->

# S05  -  Patrones de Comportamiento
## Command - Mediator - Observer - Chain of Responsibility - Strategy - State - Template Method

**Dev Notes**
Stack: .NET 10 - back-template - EF Core 10 - Dapper

---

## Agenda

- Revisión homework S04 + dudas de la semana
- **Bloque teórico**  -  10 patrones de comportamiento (parte I)
- **Bloque teórico**  -  10 patrones de comportamiento (parte II)
- **Bloque teórico**  -  Patrones en el back-template: recorrido completo
- Code review + retrospectiva + homework

---

<!-- _class: lead -->

## Patrones de Comportamiento
### Cómo colaboran los objetos y se distribuyen las responsabilidades

---

## Los 10 patrones de comportamiento

| # | Patrón | Intención en una frase |
|---|--------|----------------------|
| 13 | **Command** | Encapsula una solicitud como objeto  -  permite deshacer, encolar, loguear |
| 14 | **Iterator** | Acceso secuencial a una colección sin exponer su representación interna |
| 15 | **Template Method** | Define el esqueleto de un algoritmo; subclases rellenan los pasos |
| 16 | **Mediator** | Centraliza la comunicación entre objetos para evitar acoplamientos directos |
| 17 | **Observer** | Notifica a múltiples suscriptores cuando el estado de un objeto cambia |
| 18 | **Chain of Responsibility** | Pasa la solicitud a lo largo de una cadena hasta que alguien la maneja |
| 19 | **State** | El objeto cambia de comportamiento cuando cambia su estado interno |
| 20 | **Strategy** | Intercambia algoritmos en tiempo de ejecución sin cambiar el cliente |
| 21 | **Memento** | Captura y externaliza el estado de un objeto para restaurarlo después |
| 22 | **Visitor** | Agrega operaciones a una jerarquía de clases sin modificar las clases |

---

## Patrones en el back-template  -  resumen anticipado

| Patrón | Dónde vive en el back-template |
|--------|-------------------------------|
| **Command** | `IRequest<TResponse>` + `IRequestHandler`  -  cada Command es un objeto Request |
| **Mediator** | `IMediator.Send()` / `IMediator.Publish()`  -  coordina Handler y Presenters |
| **Observer** | `INotificationHandler<TResponse>`  -  los Presenters observan el Response |
| **Chain of Responsibility** | `IPipelineBehavior` + `InteractorPipeline`  -  cadena antes del Handler real |
| **Template Method** | (sin ejemplo estricto en el back-template; `BaseApiController` es herencia para reutilizar) |
| **Strategy** | `IConversationAssignmentStrategy`  -  intercambiable por DI |
| **State** | `ConversationStatus` + métodos de transición en la entidad |
| **Iterator** | `IAsyncEnumerable<T>` en EF Core  -  streaming de resultados |

---

<!-- _class: lead -->

## Command
### Encapsular una solicitud como objeto

---

## Command  -  Concepto

**Problema:** el emisor no sabe cómo ejecutar la operación  -  solo sabe que quiere ejecutarla.

**Solución:** encapsular la solicitud (parámetros + intención) como un objeto independiente.

**Beneficios:**
- El emisor y el receptor no se conocen
- Permite encolar, loguear, deshacer o reintentar solicitudes
- El objeto Command es serializable (puede viajar por red o persistirse)

```
Emisor -> [Command: {Parámetros}] -> Receiver (Handler)
```

**En el back-template:** cada `IRequest<TResponse>` ES un Command.

---

## Command  -  Request en el back-template (CRM)

```csharp
//  Conversations.Application/UseCases/AssignConversation/AssignConversationRequest.cs
using Common.Messaging;

namespace Conversations.Application.UseCases.AssignConversation;

// Command: encapsula la intención "asignar conversación" con sus parámetros
public sealed record AssignConversationRequest(
    Guid ConversationId,
    long TenantId
) : IRequest<AssignConversationResponse>;
```

```csharp
//  Antipatrón  -  parámetros sueltos en el Controller
[HttpPost("{id:guid}/assign")]
public async Task<IActionResult> Assign(
    Guid id, long tenantId, Guid agentId) // Controller acoplado a los parámetros
```

El Controller no sabe cómo se asigna la conversación  -  solo emite el Command.

---

## Command  -  Handler en el back-template (CRM)

```csharp
//  Conversations.Application/UseCases/AssignConversation/AssignConversationHandler.cs
using Common.Messaging;
using Conversations.Application.UseCases.AssignConversation.Responses;
using Conversations.Domain.Repositories;

namespace Conversations.Application.UseCases.AssignConversation;

public sealed class AssignConversationHandler
    : IRequestHandler<AssignConversationRequest, AssignConversationResponse>
{
    private readonly IConversationRepository _conversations;

    public AssignConversationHandler(IConversationRepository conversations)
        => _conversations = conversations;

    public async Task<AssignConversationResponse> Handle(
        AssignConversationRequest request, CancellationToken cancellationToken)
    {
        var conversation = await _conversations
            .GetByPublicIdAsync(request.ConversationId, request.TenantId, cancellationToken);
        if (conversation is null)
            return new AssignConversationNotFoundFailure("Conversación no encontrada.");
        // ...
        return new AssignConversationSuccess(conversation.PublicId);
    }
}
```

---

## Command  -  Comparación EF Core vs Dapper en el Handler

```csharp
//  EF Core  -  el Handler delega persistencia al repositorio
// _conversations.GetByPublicIdAsync() usa AppDbContext internamente
var conversation = await _conversations
    .GetByPublicIdAsync(request.ConversationId, request.TenantId, cancellationToken);
```

```csharp
//  Dapper  -  mismo Handler, mismo Command; solo cambia la implementación del repo
// Conversations.Infrastructure/Persistence/SQLDB/ConversationRepository.cs
// _conversations.GetByPublicIdAsync() usa Dapper internamente:
//   await _conn.QueryFirstOrDefaultAsync<Conversation>(
//       "SELECT * FROM conversations WHERE public_id = @PublicId AND tenant_id = @TenantId",
//       new { request.ConversationId, request.TenantId });
```

El Command no cambia. El Handler no cambia. Solo la implementación del repositorio difiere.

---

## Command  -  Alternativa externa

```csharp
//  Back-template  -  IRequest<TResponse> propio (Common.Messaging)
public sealed record AssignConversationRequest(Guid ConversationId, long TenantId)
    : IRequest<AssignConversationResponse>;
```

```csharp
// Alternativa  -  MediatR NuGet (muy común en OSS y tutoriales)
// using MediatR;
public sealed record AssignConversationCommand(Guid ConversationId, long TenantId)
    : IRequest<AssignConversationResult>;
// Diferencia: el nombre lleva "Command" explícito; el back-template usa "Request"
// La mecánica es idéntica  -  IRequest<TResponse> + IRequestHandler<TRequest, TResponse>
```

> Al leer código en GitHub o libros verán `IRequest<T>` de MediatR  -  la idea es la misma; el tipo es de un NuGet diferente.

---

## Command  -  la señal

**Cuándo usarlo:**
- Quieres desacoplar al emisor de la lógica de ejecución: el Controller no sabe cómo se asigna una conversación
- Necesitas que la solicitud sea serializable: para colas de trabajo, reintentos o auditoría
- Quieres habilitar deshacer, encolar o reintentar operaciones sin cambiar el código emisor

**Señal en el código:**

```csharp
// Señal: el Controller conoce los detalles internos de la operación
[HttpPost("{id:guid}/assign")]
public async Task<IActionResult> Assign(Guid id, Guid agentId, CancellationToken ct)
{
    var conv = await _db.Conversations.FindAsync(id, ct);
    conv.AgentId = agentId;                        // conoce la entidad
    await _db.SaveChangesAsync(ct);                // conoce el DbContext
    return Ok(conv);                               // acoplamiento total
}

// Con Command: el Controller solo emite la intención
_ = await Mediator.Send(new AssignConversationRequest(id, CurrentTenantId), ct);
// El Controller no sabe cómo ni quién ejecuta la asignación
```

**No usar si:** la operación es trivialmente simple y no necesita deshacer, encolar ni auditar. Una llamada directa es más legible.

---

<!-- _class: lead -->

## Mediator
### Centralizar la comunicación entre objetos

---

## Mediator  -  Concepto

**Problema:** múltiples objetos se comunican directamente -> acoplamiento en malla.

**Solución:** un objeto central (Mediator) gestiona todas las comunicaciones.

```
Sin Mediator:  A ↔ B ↔ C ↔ D  (O(n²) conexiones)
Con Mediator:  A -> M <- B     (O(n) conexiones)
               C -> M <- D
```

**En el back-template:** `IMediator` propio (`Common.Messaging`) es el Mediator.

- `Send(request)` -> entrega el Command al único Handler registrado
- `Publish(response)` -> entrega el Response a todos los Presenters registrados

El Controller solo habla con el Mediator  -  nunca con Handlers ni Presenters.

---

## Mediator  -  IMediator.Send() en el Controller

```csharp
//  Conversations.Presentation/ConversationsController.cs
using Common.Messaging;
using Common.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Shared.Web;
using Conversations.Application.UseCases.AssignConversation;
using Conversations.Application.UseCases.AssignConversation.Responses;

namespace Conversations.Presentation;

[Route("api/conversations")]
public sealed class ConversationsController : BaseApiController
{
    private readonly ResultViewModel<ConversationsController> _viewModel;

    public ConversationsController(
        IMediator mediator,
        ResultViewModel<ConversationsController> viewModel) : base(mediator)
        => _viewModel = viewModel;

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, CancellationToken ct = default)
    {
        _ = await Mediator.Send(new AssignConversationRequest(id, CurrentTenantId), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : NotFound(_viewModel);
    }
}
```

---

## Mediator  -  Implementación real de Mediator.cs

```csharp
//  Common/Common/Messaging/Mediator.cs  -  Send despacha al único Handler
public Task<TResponse> Send<TResponse>(
    IRequest<TResponse> request, CancellationToken cancellationToken = default)
{
    var requestType  = request.GetType();
    var handlerType  = typeof(IRequestHandler<,>)
                           .MakeGenericType(requestType, typeof(TResponse));
    var handler      = _serviceProvider.GetService(handlerType)
                       ?? throw new InvalidOperationException(...);

    // Envuelve el Handler con los IPipelineBehavior registrados (cadena)
    RequestHandlerDelegate<TResponse> handlerDelegate = () =>
        InvokeRequestHandler<TResponse>(handleMethod, handler, request, ct);
    var behaviors = _serviceProvider.GetServices(behaviorType).Reverse();
    foreach (var behavior in behaviors)
    {
        var next = handlerDelegate;
        handlerDelegate = () => InvokePipelineBehavior<TResponse>(..., next, ct);
    }
    return handlerDelegate();
}
```

---

## Mediator  -  Publish despacha a todos los Presenters

```csharp
//  Common/Common/Messaging/Mediator.cs  -  Publish notifica a N handlers en paralelo
public Task Publish<TNotification>(
    TNotification notification, CancellationToken cancellationToken = default)
    where TNotification : INotification
{
    var handlers = _serviceProvider
        .GetServices<INotificationHandler<TNotification>>();

    var tasks = handlers.Select(h => h.Handle(notification, cancellationToken));
    return Task.WhenAll(tasks);
}
```

`Send` es **Command** (uno a uno). `Publish` es **Observer** (uno a muchos).

> **Alternativa externa:** MediatR NuGet tiene `ISender.Send()` y `IPublisher.Publish()`  -  misma idea, interfaces separadas. El back-template los unifica en `IMediator`.

---

## Mediator  -  Send vs Publish resumido

```csharp
// IMediator.Send  -  Command Pattern + Mediator
// Un emisor -> un receptor (el único IRequestHandler registrado)
_ = await Mediator.Send(new AssignConversationRequest(id, CurrentTenantId), ct);

// IMediator.Publish  -  Observer Pattern + Mediator
// Un emisor -> N receptores (todos los INotificationHandler<TResponse>)
// Lo llama InteractorPipeline automáticamente tras ejecutar el Handler:
await Mediator.Publish(response, cancellationToken);
```

El Controller nunca llama a `Publish` directamente  -  eso lo gestiona `InteractorPipeline`.

---

## Mediator  -  la señal

**Cuándo usarlo:**
- Varios componentes necesitan comunicarse y las referencias directas crearían acoplamiento O(n²)
- Quieres que los componentes no se conozcan entre sí: el Controller no conoce al Handler ni al Presenter
- Necesitas un punto central donde agregar comportamiento transversal: logging, validación, caché

**Señal en el código:**

```csharp
// Señal: Controller con referencias a N dependencias para coordinar un flujo
public class ConversationsController(
    AssignConversationHandler   _assignHandler,    // <- conoce el Handler
    AssignConversationPresenter _assignPresenter,  // <- conoce el Presenter
    ValidationPipeline          _validation)       // <- conoce el pipeline
{
    // Cada nuevo use case agrega más dependencias al Controller
}

// Con Mediator: el Controller solo conoce IMediator
public class ConversationsController(IMediator mediator) : BaseApiController(mediator) { }
// Agregar AssignConversation, ResolveConversation, ArchiveConversation
// NO cambia el constructor del Controller
```

**No usar si:** la comunicación es estrictamente uno-a-uno con un receptor que nunca cambia. Una llamada directa es más clara.

---

<!-- _class: lead -->

## Observer
### Notificar a múltiples suscriptores

---

## Observer  -  Concepto

**Problema:** cuando el estado de un objeto cambia, otros objetos deben actualizarse  -  pero el sujeto no debería conocer a los observadores.

**Solución:** el sujeto mantiene una lista de observadores y los notifica automáticamente.

**En el back-template:**
- **Sujeto (publisher):** `InteractorPipeline` llama a `IMediator.Publish(response)`
- **Observadores:** los Presenters que implementan `INotificationHandler<TResponse>`
- **Canal:** `IMediator.Publish()` distribuye a todos en paralelo (`Task.WhenAll`)

El Handler no sabe cuántos Presenters existen. Los Presenters no se conocen entre sí.

---

## Observer  -  Jerarquía de Response (CRM)

```csharp
//  Conversations.Application/UseCases/AssignConversation/Responses/
//    AssignConversationResponse.cs
using Common.Messaging;

namespace Conversations.Application.UseCases.AssignConversation.Responses;

public abstract record AssignConversationResponse : IResponse;
```

```csharp
// AssignConversationSuccess.cs
using Common.Results;

public sealed record AssignConversationSuccess(Guid ConversationId)
    : AssignConversationResponse, ISuccess<Guid>;
```

```csharp
// AssignConversationNotFoundFailure.cs
using Common.Results;

public sealed record AssignConversationNotFoundFailure(string Message)
    : AssignConversationResponse, INotFoundFailure;
```

---

## Observer  -  Presenter HTTP (Observador principal)

```csharp
//  Conversations.Presentation/Presenters/AssignConversationPresenter.cs
using Common.Messaging;
using Common.Results;
using Common.ViewModels;
using Conversations.Application.UseCases.AssignConversation.Responses;

namespace Conversations.Presentation.Presenters;

public sealed class AssignConversationPresenter
    : INotificationHandler<AssignConversationResponse>
{
    private readonly ResultViewModel<ConversationsController> _viewModel;

    public AssignConversationPresenter(
        ResultViewModel<ConversationsController> viewModel)
        => _viewModel = viewModel;

    public Task Handle(
        AssignConversationResponse notification, CancellationToken ct)
    {
        if (notification is IFailure failure)
            _viewModel.Fail(failure.Message);
        else if (notification is ISuccess<Guid> success)
            _viewModel.Set(success);
        return Task.CompletedTask;
    }
}
```

---

## Observer  -  Patrón IFailure primero (por qué importa el orden)

```csharp
//  IFailure PRIMERO  -  correcto
public Task Handle(AssignConversationResponse notification, CancellationToken ct)
{
    if (notification is IFailure failure)          // atrapa CUALQUIER fallo
        _viewModel.Fail(failure.Message);
    else if (notification is ISuccess<Guid> success)
        _viewModel.Set(success);
    return Task.CompletedTask;
}
```

```csharp
//  ISuccess PRIMERO  -  peligroso: si se agrega un nuevo tipo de fallo
// que también implementa ISuccess, el else if nunca se evalúa
public Task Handle(AssignConversationResponse notification, CancellationToken ct)
{
    if (notification is ISuccess<Guid> success)    //  orden incorrecto
        _viewModel.Set(success);
    else if (notification is IFailure failure)
        _viewModel.Fail(failure.Message);
    return Task.CompletedTask;
}
```

> `INotFoundFailure`, `IValidationFailure` y `IConflictFailure` todos heredan de `IFailure`  -  el `is IFailure` los captura a todos con un solo branch.

---

## Observer  -  Segundo observador: AuditLogger

```csharp
//  Segundo INotificationHandler  -  cero cambios en Handler ni en Presenter principal
// Conversations.Presentation/Presenters/AssignConversationAuditPresenter.cs
using Common.Messaging;
using Conversations.Application.UseCases.AssignConversation.Responses;
using Microsoft.Extensions.Logging;

namespace Conversations.Presentation.Presenters;

public sealed class AssignConversationAuditPresenter
    : INotificationHandler<AssignConversationResponse>
{
    private readonly ILogger<AssignConversationAuditPresenter> _log;

    public AssignConversationAuditPresenter(
        ILogger<AssignConversationAuditPresenter> log) => _log = log;

    public Task Handle(
        AssignConversationResponse notification, CancellationToken ct)
    {
        if (notification is AssignConversationSuccess s)
            _log.LogInformation("Conversation {Id} assigned", s.ConversationId);
        return Task.CompletedTask;
    }
}
```

---

## Observer  -  Registro DI de los Presenters

```csharp
//  Conversations.Presentation/ServiceCollectionEx.cs
// Patrón de registro del back-template: instancia concreta + handler por jerarquía
services.AddScoped<AssignConversationPresenter>();
services.AddScoped<INotificationHandler<AssignConversationResponse>>(
    sp => sp.GetRequiredService<AssignConversationPresenter>());

services.AddScoped<AssignConversationAuditPresenter>();
services.AddScoped<INotificationHandler<AssignConversationResponse>>(
    sp => sp.GetRequiredService<AssignConversationAuditPresenter>());
```

```csharp
//  Alternativa  -  dos handlers por tipo concreto (no es el patrón del back-template)
services.AddScoped<INotificationHandler<AssignConversationSuccess>, MyPresenter>();
services.AddScoped<INotificationHandler<AssignConversationNotFoundFailure>, MyPresenter>();
// Problema: el Presenter debe registrarse dos veces y manejar dos métodos Handle
```

> **Alternativa externa (MediatR):** `INotificationHandler<TNotification>` por tipo concreto. Válido pero el back-template despacha por jerarquía  -  el Presenter siempre recibe cualquier resultado.

---

## Observer  -  la señal

**Cuándo usarlo:**
- Múltiples componentes reaccionan al mismo evento sin que el emisor los conozca
- Quieres agregar nuevos suscriptores sin modificar al emisor
- El emisor no debe saber cuántos ni qué suscriptores existen

**Señal en el código:**

```csharp
// Señal: Handler que llama directamente a cada componente que reacciona al evento
public async Task<AssignConversationResponse> Handle(...)
{
    // ... lógica ...
    await _presenter.NotifySuccess(convId);       // <- conoce Presenter HTTP
    await _auditLogger.LogAssignment(convId);     // <- conoce AuditLogger
    await _signalRHub.BroadcastUpdate(convId);   // <- conoce SignalR hub
    // Agregar un nuevo observador = modificar este Handler
}

// Con Observer (Publish): el Handler desconoce a sus suscriptores
await _mediator.Publish(new AssignConversationSuccess(convId), ct);
// AssignConversationPresenter, AuditPresenter, SignalRPresenter
// todos reciben el evento  -  sin tocar el Handler
```

**No usar si:** solo un componente reacciona al evento. Una llamada directa es más legible y más fácil de seguir.

---

<!-- _class: lead -->

## Chain of Responsibility
### Pasar la solicitud a lo largo de una cadena

---

## Chain of Responsibility  -  Concepto

**Problema:** múltiples objetos pueden procesar una solicitud, pero el emisor no sabe cuál  -  y el orden importa.

**Solución:** encadenar los manejadores  -  cada uno procesa lo suyo y delega al siguiente.

```
Request -> [Behavior1] -> [Behavior2] -> [Handler real] -> Response
             ↓               ↓
          (logging)     (validación)
```

**En el back-template:** `IPipelineBehavior<TRequest, TResponse>` + `InteractorPipeline`
forman la cadena. Cada comportamiento recibe un `next` delegate  -  llama a lo siguiente.

---

## Chain of Responsibility  -  IPipelineBehavior

```csharp
//  Common/Common/Messaging/InteractorPipeline.cs  -  nodo real del back-template
// record posicional con primary constructor
public record InteractorPipeline<TRequest, TResponse>(
    IMediator Mediator,
    ILogger<InteractorPipeline<TRequest, TResponse>> Logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest  : IRequest<TResponse>
    where TResponse : IResponse
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,   // <- siguiente en la cadena
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("{@Request}", request);     // antes
        var response = await next().ConfigureAwait(false); // delega
        await Mediator.Publish(response).ConfigureAwait(false); // después
        return response;
    }
}
```

---

## Chain of Responsibility  -  Flujo completo

```
HTTP Request
     |

IMediator.Send(AssignConversationRequest)
     |

InteractorPipeline.Handle(request, next, ct)       <- Behavior registrado
  Logger.LogInformation("{@Request}", request)      -> log antes
  response = await next()                           -> delega al Handler
  await Mediator.Publish(response)                  -> notifica Presenters
     |
      (next delegate)
AssignConversationHandler.Handle(request, ct)       <- Handler real
  // consulta repos, aplica lógica, retorna Success/Failure
```

Se pueden apilar múltiples `IPipelineBehavior`: Logging -> Validation -> Authorization -> Handler.

---

## Chain of Responsibility  -  Registro en Program.cs

```csharp
//  Host.Api/Program.cs  -  AddMediator registra todos los assemblies de Application
builder.Services.AddMediator(
    typeof(Tenancy.Application.ServiceCollectionEx).Assembly,
    typeof(Users.Application.ServiceCollectionEx).Assembly,
    typeof(Authentication.Application.ServiceCollectionEx).Assembly
);
// AddMediator registra automáticamente:
// - IRequestHandler por cada Handler encontrado en los assemblies
// - InteractorPipeline como IPipelineBehavior global
// - INotificationHandler por cada Presenter registrado en Presentation
```

```csharp
//  MediatR NuGet  -  similar pero con AddMediatR()
// services.AddMediatR(cfg =>
//     cfg.RegisterServicesFromAssemblies(
//         typeof(AssignConversationHandler).Assembly));
// Diferencia: MediatR NuGet; back-template usa su propio Common.Messaging
```

---

## Chain of Responsibility  -  la señal

**Cuándo usarlo:**
- Múltiples pasos deben ejecutarse antes o después del Handler real: logging, validación, autorización
- Quieres agregar o quitar pasos de la cadena sin modificar los Handlers
- El orden de los pasos importa y necesitas poder reconfigurarlo

**Señal en el código:**

```csharp
// Señal: el mismo bloque transversal copiado en TODOS los Handlers
public async Task<GetContactResponse> Handle(GetContactRequest req, CancellationToken ct)
{
    _logger.LogInformation("{@Request}", req);       // copiado en cada Handler
    if (!_validator.IsValid(req))                    // copiado en cada Handler
        return new GetContactValidationFailure(...);
    // lógica real del handler...
    _logger.LogInformation("{@Response}", response); // copiado en cada Handler
    return response;
}

// Con Chain of Responsibility (IPipelineBehavior):
// InteractorPipeline hace el log. Un ValidationBehavior puede hacer la validación.
// Ningún Handler repite esas líneas. Se agregan behaviors en DI, no en Handlers.
```

**No usar si:** solo hay un Handler y no hay lógica transversal repetida. La cadena agrega indirección sin beneficio.

---

<!-- _class: lead -->

## Template Method
### El esqueleto del algoritmo en la clase base

---

## Template Method  -  Concepto

**Problema:** varias clases implementan el mismo algoritmo con pequeñas variaciones en cada paso.

**Solución:** la clase base define el esqueleto (orden de pasos); las subclases sobreescriben los pasos variables.

```csharp
abstract class Report {
    // Template Method  -  el esqueleto invariante
    public void Generate() {
        LoadData();   // paso 1  -  siempre igual
        Render();     // paso 2  -  variable (implementar en subclase)
        Export();     // paso 3  -  variable (implementar en subclase)
    }
    protected abstract void Render();
    protected abstract void Export();
    private void LoadData() { /* siempre igual */ }
}
```

**En el back-template:** `BaseApiController` define el flujo HTTP; cada Controller concreto añade sus endpoints.

---

## BaseApiController  -  herencia para reutilizar (no es Template Method)

```csharp
//  Shared/Web/BaseApiController.cs  -  clase base: cómo acceder al Mediator
using Common.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace Shared.Web;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator Mediator;
    protected BaseApiController(IMediator mediator) => Mediator = mediator;
}
```

La clase base provee `Mediator` a todos los Controllers (reutilización por herencia).
No es Template Method: no define un algoritmo con pasos abstractos que las subclases rellenen; solo expone una dependencia.

---

## BaseApiController  -  Controller concreto (CRM)

```csharp
//  Conversations.Presentation/ConversationsController.cs
// Subclase  -  añade sus endpoints
[Route("api/conversations")]
public sealed class ConversationsController : BaseApiController
{
    private readonly ResultViewModel<ConversationsController> _viewModel;

    public ConversationsController(
        IMediator mediator,
        ResultViewModel<ConversationsController> viewModel) : base(mediator)
        => _viewModel = viewModel;

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, CancellationToken ct = default)
    {
        _ = await Mediator.Send(
            new AssignConversationRequest(id, CurrentTenantId), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : NotFound(_viewModel);
    }
}
```

---

## BaseApiController  -  alternativa externa

```csharp
//  Back-template  -  BaseApiController con IMediator inyectado vía constructor
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator Mediator;
    protected BaseApiController(IMediator mediator) => Mediator = mediator;
}
```

```csharp
// Alternativa  -  BaseController con lazy resolution del servicio (ASP.NET antiguo)
public abstract class BaseApiController : ControllerBase
{
    private IMediator? _mediator;
    protected IMediator Mediator =>
        _mediator ??= HttpContext.RequestServices
                          .GetRequiredService<IMediator>();
}
// Diferencia: el back-template inyecta explícitamente en el constructor (más testeable)
// La versión lazy evita modificar los constructores de subclases, pero oculta la dependencia
```

---

## Template Method  -  la señal

**Cuándo usarlo:**
- Varias subclases implementan el mismo algoritmo con el mismo esqueleto pero pasos distintos
- Hay código duplicado entre subclases que difiere solo en 2-3 pasos específicos
- Quieres garantizar que el flujo invariante no se pueda modificar en las subclases

**Señal en el código:**

```csharp
// Señal: copia del mismo setup en cada Controller, cambiando solo el endpoint
class ContactsController : ControllerBase
{
    private IMediator? _mediator;
    protected IMediator Mediator =>          // copiado en cada Controller
        _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();
    // 3 líneas idénticas en cada Controller del proyecto
}

// Con una clase base (BaseApiController): el setup vive una sola vez
// Cada Controller solo define sus endpoints (herencia para reutilizar; no es Template Method)
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator Mediator;
    protected BaseApiController(IMediator mediator) => Mediator = mediator;
}
```

**No usar si:** las subclases son tan distintas que no comparten estructura. En ese caso, composición sobre herencia.

---

<!-- _class: lead -->

## Strategy
### Intercambiar algoritmos en tiempo de ejecución

---

## Strategy  -  Concepto

**Problema:** un objeto necesita realizar una operación que puede implementarse de múltiples maneras intercambiables.

**Solución:** definir una familia de algoritmos, encapsular cada uno y hacerlos intercambiables.

```
Context -> IStrategy.Execute()
              ↑ (inyectada por DI)
    +---------+----------+
StrategyA  StrategyB  StrategyC
```

**Caso del producto:** asignación de conversaciones a agentes.
¿Round-robin? ¿Menor carga? ¿Por habilidad del agente?  -  mismo contrato, distinta lógica.

---

## Strategy  -  IConversationAssignmentStrategy

```csharp
//  Conversations.Domain/Services/IConversationAssignmentStrategy.cs
namespace Conversations.Domain.Services;

// Contrato  -  todos los algoritmos implementan esta interfaz
public interface IConversationAssignmentStrategy
{
    Task<Guid?> AssignAsync(
        Guid conversationId,
        long tenantId,
        CancellationToken cancellationToken = default);
}
```

```csharp
//  Estrategia A  -  Round Robin
// Conversations.Infrastructure/Services/RoundRobinAssignmentStrategy.cs
using Conversations.Domain.Services;

namespace Conversations.Infrastructure.Services;

public sealed class RoundRobinAssignmentStrategy : IConversationAssignmentStrategy
{
    // selecciona el siguiente agente disponible en orden circular
    public async Task<Guid?> AssignAsync(
        Guid conversationId, long tenantId, CancellationToken ct = default)
    { /* EF Core: ORDER BY LastAssignedAt, LIMIT 1 */ }
}
```

---

## Strategy  -  Estrategia B: Menor Carga

```csharp
//  Estrategia B  -  Menor carga (agente con menos conversaciones abiertas)
// Conversations.Infrastructure/Services/LeastLoadAssignmentStrategy.cs
using Conversations.Domain.Services;

namespace Conversations.Infrastructure.Services;

public sealed class LeastLoadAssignmentStrategy : IConversationAssignmentStrategy
{
    public async Task<Guid?> AssignAsync(
        Guid conversationId, long tenantId, CancellationToken ct = default)
    {
        // EF Core:
        // SELECT agentId FROM agents
        // WHERE tenant_id = @tenantId AND is_active = true
        // ORDER BY (
        //   SELECT COUNT(*) FROM conversations
        //   WHERE agent_id = agents.id AND status = 'Open'
        // ) ASC
        // LIMIT 1
    }
}
```

---

## Strategy  -  Registro DI e intercambio

```csharp
//  Conversations.Infrastructure/ServiceCollectionEx.cs
// Solo cambia una línea para cambiar el algoritmo  -  sin tocar Handlers ni Controllers
services.AddScoped<IConversationAssignmentStrategy, RoundRobinAssignmentStrategy>();
// o:
// services.AddScoped<IConversationAssignmentStrategy, LeastLoadAssignmentStrategy>();
```

```csharp
//  El Handler usa la Strategy  -  no sabe qué algoritmo está detrás
public sealed class AssignConversationHandler
    : IRequestHandler<AssignConversationRequest, AssignConversationResponse>
{
    private readonly IConversationAssignmentStrategy _strategy;
    private readonly IConversationRepository         _conversations;

    public AssignConversationHandler(
        IConversationAssignmentStrategy strategy,
        IConversationRepository conversations)
    {
        _strategy      = strategy;
        _conversations = conversations;
    }
    // ...
}
```

---

## Strategy  -  Handle usando la Strategy

```csharp
//  Conversations.Application/UseCases/AssignConversation/AssignConversationHandler.cs
public async Task<AssignConversationResponse> Handle(
    AssignConversationRequest request, CancellationToken cancellationToken)
{
    var conversation = await _conversations
        .GetByPublicIdAsync(
            request.ConversationId, request.TenantId, cancellationToken);
    if (conversation is null)
        return new AssignConversationNotFoundFailure("Conversación no encontrada.");

    var agentId = await _strategy.AssignAsync(
        request.ConversationId, request.TenantId, cancellationToken);
    if (agentId is null)
        return new AssignConversationNoAgentAvailableFailure(
            "No hay agentes disponibles.");

    // actualizar la conversación con el agente asignado...
    return new AssignConversationSuccess(conversation.PublicId);
}
```

---

## Strategy  -  Alternativa externa

```csharp
//  Back-template  -  IConversationAssignmentStrategy inyectada vía DI
services.AddScoped<IConversationAssignmentStrategy, RoundRobinAssignmentStrategy>();
```

```csharp
// Alternativa  -  Strategy seleccionada por factory en runtime
// (útil cuando la estrategia se elige según datos del workspace)
public interface IAssignmentStrategyFactory
{
    IConversationAssignmentStrategy GetStrategy(string workspaceMode);
}
// services.AddScoped<IAssignmentStrategyFactory, AssignmentStrategyFactory>();

// En el Handler:
var strategy = _strategyFactory.GetStrategy(workspace.AssignmentMode);
var agentId  = await strategy.AssignAsync(request.ConversationId, ct);
```

> La factory es útil cuando distintos workspaces del mismo tenant necesitan algoritmos diferentes  -  lo veremos al construir la lógica multi-tenant más adelante.

---

## Strategy  -  la señal

**Cuándo usarlo:**
- Un objeto realiza una operación que puede implementarse de múltiples maneras intercambiables
- La implementación debe seleccionarse en tiempo de ejecución o por configuración de workspace
- Quieres agregar nuevas variantes del algoritmo sin modificar el código que lo usa

**Señal en el código:**

```csharp
// Señal: if/switch que selecciona el algoritmo según configuración o contexto
public async Task<Guid?> AssignAsync(Guid convId, long tenantId, CancellationToken ct)
{
    var workspace = await _workspaceRepo.GetAsync(tenantId, ct);
    if (workspace.AssignmentMode == "round-robin")   return await RoundRobinAsync(convId, ct);
    if (workspace.AssignmentMode == "least-load")    return await LeastLoadAsync(convId, ct);
    if (workspace.AssignmentMode == "skill-based")   return await SkillBasedAsync(convId, ct);
    // Agregar un nuevo modo = modificar este método y agregar otro if
}

// Con Strategy: una línea de DI cambia el algoritmo, el Handler no se toca
services.AddScoped<IConversationAssignmentStrategy, RoundRobinAssignmentStrategy>();
// Cambiar a LeastLoad = cambiar esa línea en DI, cero cambios en AssignConversationHandler
```

**No usar si:** solo usas un algoritmo y no hay variantes previstas. La interfaz agrega indirección sin valor real.

---

<!-- _class: lead -->

## State
### El comportamiento cambia con el estado

---

## State  -  Concepto

**Problema:** un objeto se comporta diferente según su estado interno  -  código lleno de `switch/if` por estado esparcidos en múltiples Handlers.

**Solución:** encapsular cada estado en su propia clase o centralizar las transiciones en la entidad.

```csharp
//  Antipatrón  -  estado disperso en Handlers y Controllers
public void Handle(Conversation c) {
    switch (c.Status) {
        case "Open":     /* lógica A en Handler1 */
        case "Assigned": /* lógica B en Handler2 */
        case "Resolved": /* lógica C en Handler3 */
    }
}
// El mismo switch se repite en 4 Handlers distintos -> bug cuando agregas un estado nuevo
```

```csharp
//  Con State Pattern  -  la entidad conoce sus transiciones válidas
c.Resolve(); // la conversación sabe si puede resolverse desde su estado actual
```

---

## State  -  Enum de estados (CRM)

```csharp
//  Conversations.Domain/Enums/ConversationStatus.cs
namespace Conversations.Domain.Enums;

public enum ConversationStatus
{
    Open,       // recién creada, sin agente
    Assigned,   // agente asignado, en atención
    Resolved,   // marcada como resuelta por el agente
    Archived    // archivada  -  estado terminal
}
```

Las transiciones válidas son:
```
Open -> Assigned -> Resolved -> Archived
Open -> Archived  (cierre directo sin asignar)
```

Todo lo que no esté aquí es una transición inválida  -  la entidad lo hace cumplir.

---

## State  -  Entidad con transiciones (parte 1)

```csharp
//  Conversations.Domain/Entities/Conversation.cs
using Common.Results;
using Conversations.Domain.Enums;

namespace Conversations.Domain.Entities;

public sealed class Conversation
{
    public long               Id        { get; init; }
    public Guid               PublicId  { get; init; }
    public ConversationStatus Status    { get; private set; }
                                          = ConversationStatus.Open;
    public Guid?              AgentId   { get; private set; }
    public DateTime           CreatedAtUtc { get; init; }
    public DateTime           UpdatedAtUtc { get; private set; }
```

---

## State  -  Entidad con transiciones (parte 2)

```csharp
    public Result Assign(Guid agentId)
    {
        if (Status is not ConversationStatus.Open)
            return Result.Fail("Solo se puede asignar una conversación abierta.");
        Status    = ConversationStatus.Assigned;
        AgentId   = agentId;
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.OK();
    }

    public Result Resolve()
    {
        if (Status is not ConversationStatus.Assigned)
            return Result.Fail("Solo se puede resolver una conversación asignada.");
        Status = ConversationStatus.Resolved;
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.OK();
    }

    public Result Archive()
    {
        if (Status is ConversationStatus.Archived)
            return Result.Fail("La conversación ya está archivada.");
        Status = ConversationStatus.Archived;
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.OK();
    }
}
```

---

## State  -  Uso en el Handler

```csharp
//  Conversations.Application/UseCases/ResolveConversation/ResolveConversationHandler.cs
public async Task<ResolveConversationResponse> Handle(
    ResolveConversationRequest request, CancellationToken cancellationToken)
{
    var conversation = await _conversations
        .GetByPublicIdAsync(
            request.ConversationId, request.TenantId, cancellationToken);
    if (conversation is null)
        return new ResolveConversationNotFoundFailure("Conversación no encontrada.");

    var result = conversation.Resolve(); // la entidad valida la transición
    if (result.IsFailure)
        return new ResolveConversationInvalidStateFailure(result.Error);

    await _conversations.UpdateAsync(conversation, cancellationToken);
    return new ResolveConversationSuccess(conversation.PublicId);
}
```

La lógica de transición vive en la entidad  -  no en el Handler.

---

## State  -  Alternativa externa

```csharp
//  Back-template  -  enum + métodos de transición en la entidad (State "ligero")
public sealed class Conversation
{
    public ConversationStatus Status { get; private set; }
    public Result Resolve() { /* valida y transiciona */ }
}
```

```csharp
// Alternativa  -  State Pattern "pesado" (cada estado es una clase)
// Válido cuando cada estado tiene comportamiento complejo diferente
public abstract class ConversationState
{
    public abstract Result Resolve(Conversation context);
    public abstract Result Archive(Conversation context);
}
public sealed class OpenState     : ConversationState { ... }
public sealed class AssignedState : ConversationState { ... }
public sealed class ResolvedState : ConversationState { ... }
// Conversation delega: _state.Resolve(this)
```

> Para el sistema de mensajería, el enum + métodos es suficiente. El State "pesado" aplica cuando la lógica de cada estado tiene >10 líneas y crece con el tiempo.

---

## State  -  la señal

**Cuándo usarlo:**
- El comportamiento de un objeto cambia significativamente según su estado interno
- El mismo `switch/if` sobre el estado aparece en múltiples Handlers o clases
- Agregar un nuevo estado requiere modificar múltiples archivos

**Señal en el código:**

```csharp
// Señal: la misma validación de estado dispersa en N Handlers distintos
// En AssignConversationHandler:
if (conversation.Status != ConversationStatus.Open)
    return new AssignConversationInvalidStateFailure("Solo se puede asignar si está abierta.");

// En ResolveConversationHandler:
if (conversation.Status != ConversationStatus.Assigned)
    return new ResolveConversationInvalidStateFailure("Solo se puede resolver si está asignada.");

// En ArchiveConversationHandler:
if (conversation.Status == ConversationStatus.Archived)
    return new ArchiveConversationInvalidStateFailure("Ya está archivada.");
// Agregar estado "Snoozed" = tocar los 3 Handlers, potencialmente más

// Con State: la entidad centraliza todas las reglas de transición
conversation.Assign(agentId);  // la entidad valida y transiciona  -  un solo lugar
conversation.Resolve();
conversation.Archive();
```

**No usar si:** el objeto tiene 2 estados y la diferencia de comportamiento es trivial. Un booleano con un `if` es suficiente.

---

<!-- _class: lead -->

## Iterator y los demás
### Patrones restantes en contexto

---

## Iterator  -  IAsyncEnumerable en EF Core

**Concepto:** acceso secuencial a una colección sin exponer su representación interna.

```csharp
//  Contacts.Infrastructure/Repositories/ContactRepository.cs
// Streaming de contactos  -  el caller itera sin cargar todo en memoria
public async IAsyncEnumerable<Contact> StreamByWorkspaceAsync(
    long tenantId,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    await foreach (var contact in _db.Contacts
        .AsNoTracking()
        .Where(c => c.TenantId == tenantId && c.IsActive)
        .AsAsyncEnumerable()
        .WithCancellation(ct))
    {
        yield return contact;
    }
}
// Uso: await foreach (var c in _contacts.StreamByWorkspaceAsync(tenantId, ct))
```

El caller itera con `await foreach`  -  no sabe si viene de EF Core, Dapper o un archivo CSV.

---

## Memento, Visitor  -  cuándo aplican

### Memento
Captura el estado de un objeto para restaurarlo.

```csharp
// Caso CRM: guardar el estado de un BotFlow antes de que el usuario lo edite
var snapshot = botFlow.CreateSnapshot();   // Memento  -  guarda estado actual
// ... el usuario arrastra nodos, cambia conexiones
botFlow.RestoreFrom(snapshot);             // si cancela, vuelve al estado previo
```

### Visitor
Agrega operaciones a una jerarquía de clases sin modificarlas.

```csharp
// Caso CRM: exportar contactos a distintos formatos sin agregar Export() a la entidad
contact.Accept(new JsonExportVisitor());
contact.Accept(new CsvExportVisitor());
```

En el back-template actual, Memento y Visitor aparecerán más adelante (BotFlow builder) y una sesión posterior (exportación de contactos).

---

## Resumen  -  Los 10 patrones en el back-template

| Patrón | Implementación concreta | Tecnología |
|--------|------------------------|--------------------|
| Command | `IRequest<TResponse>` sealed record | Common.Messaging |
| Mediator | `IMediator.Send()` + `Publish()` | Common.Messaging |
| Observer | `INotificationHandler<TResponse>`  -  Presenters | Common.Messaging |
| Chain of Responsibility | `IPipelineBehavior` + `InteractorPipeline` | Common.Messaging |
| Template Method | (sin ejemplo estricto; `BaseApiController` solo reutiliza por herencia) | - |
| Strategy | `IConversationAssignmentStrategy` | DI intercambiable |
| State | `ConversationStatus` enum + métodos en la entidad | Domain |
| Iterator | `IAsyncEnumerable<Contact>` en repos | EF Core / Dapper |
| Memento | `BotFlow.CreateSnapshot()` |  -  |
| Visitor | Exportadores de contactos |  -  |

---

## Cómo Command + Mediator + Observer + CoR trabajan juntos

```
HTTP Request
     |

ConversationsController
  Mediator.Send(AssignConversationRequest)     <- Command emitido
     |

InteractorPipeline (IPipelineBehavior)         <- Chain of Responsibility
  LogInformation -> next() -> Publish(response)
     |                           |

AssignConversationHandler    INotificationHandler<AssignConversationResponse> × N
  (Command Receiver)             |-- AssignConversationPresenter  -> ResultViewModel
                                 `-- AssignConversationAuditPresenter -> log
```

Cinco patrones en una sola llamada HTTP. Todos desacoplados entre sí.

---

## Anti-patrones de comportamiento comunes

| Anti-patrón | Problema | Solución |
|-------------|----------|----------|
| Handler llama a otro Handler directamente | Acoplamiento  -  viola Mediator | Emitir nuevo `IRequest` via `IMediator.Send()` |
| `switch (status)` en cada Handler | Estado esparcido  -  viola State | Transiciones en métodos de la entidad |
| Estrategia hardcoded con `if (algo == "roundrobin")` | No intercambiable  -  viola Strategy | Inyectar `IConversationAssignmentStrategy` |
| Presenter que conoce al Controller | Viola Observer  -  acoplamiento directo | Presenter solo escribe en `ResultViewModel<T>` |
| `ISuccess` antes de `IFailure` en pattern matching | Puede no atrapar todos los fallos | `IFailure` siempre va primero |

---

## Próxima clase  -  S06

**Tema:** DDD - CQRS - Clean Architecture - Vertical Slice - Monolito Modular

- Bounded Contexts y Aggregates en el back-template
- CQRS: separar lectura de escritura con `IRequest` de Query vs Command
- Vertical Slice vs Layered Architecture  -  cuándo usar cada uno
- Monolito Modular como punto de partida correcto para el proyecto

**Preparación:** leer `dev-notes/03-arquitectura/01-ddd.md` y `02-cqrs.md` antes de S06.

---

<!-- _class: lead -->

# Parte V  -  Clean Code Aplicado

## Nombres - Funciones - Guard Clauses - Code Smells - Refactoring

> "Cualquier tonto puede escribir código que un ordenador entiende.
> Los buenos programadores escriben código que los humanos entienden."
>  -  Martin Fowler

---

## ¿Qué es Clean Code?

Clean Code no es sobre herramientas, frameworks ni patrones.
Es sobre **legibilidad** y **mantenibilidad** del código que escribes hoy
para la persona que lo leerá en seis meses  -  que probablemente serás tú.

**El costo del código sucio:**

```
Semana 1:  cambio tarda 2 horas
Semana 12: el mismo cambio tarda 3 días (nadie entiende el contexto)
Semana 24: "hay que reescribirlo"  -  el código ya no se puede extender
```

**Los tres autores que definen el canon:**
- **Robert C. Martin (Uncle Bob)**  -  *Clean Code* (2008)
- **Martin Fowler**  -  *Refactoring* (2018)
- **Kent Beck**  -  *Implementation Patterns* (2008)

---

## Nombres que revelan intención

El nombre es la primera documentación del código. Si necesitas un comentario para explicar el nombre, el nombre está mal.

```csharp
//  Nombres que ocultan la intención
int d;                              // ¿elapsed time? ¿days? ¿distance?
bool flag;                          // ¿qué representa este flag?
List<int> list1;                    // ¿lista de qué?
void Process(object o) { ... }      // ¿procesa qué? ¿hace qué?
string GetData(int id) { ... }      // ¿qué datos? ¿de dónde?

//  Nombres que revelan intención
int elapsedDaysInSprint;
bool isConversationAssigned;
List<long> unassignedConversationIds;
void AssignConversationToAgent(long convId, long agentId) { ... }
ConversationDto GetConversationById(long tenantId, long convId) { ... }
```

**Reglas de nombrado en el sistema de mensajería:**
- Métodos: verbo + sustantivo  -  `AssignTo`, `GetByEmail`, `CreateContact`
- Booleanos: `is/has/can/should`  -  `isActive`, `hasUnreadMessages`
- Colecciones: plural  -  `conversations`, `agents`, `channels`
- Evitar prefijos de tipo  -  `strName` -> `name`, `lstItems` -> `items`

---

## Funciones: haz una sola cosa

**La regla del nivel de abstracción único:**
Una función solo debe operar en un nivel de abstracción. No mezcles lógica de negocio con detalles de implementación en la misma función.

```csharp
//  Función que hace muchas cosas (nivel de abstracción mezclado)
public async Task<IActionResult> CreateContact(CreateContactRequest request)
{
    if (string.IsNullOrEmpty(request.Email)) return BadRequest("Email requerido");
    if (!request.Email.Contains('@')) return BadRequest("Email inválido");
    var existing = await _db.Contacts
        .Where(c => c.Email == request.Email && c.TenantId == tenantId)
        .FirstOrDefaultAsync();
    if (existing != null) return Conflict("Email ya registrado");
    var contact = new Contact { Email = request.Email, FullName = request.FullName,
        TenantId = tenantId, CreatedAt = DateTime.UtcNow };
    _db.Contacts.Add(contact);
    await _db.SaveChangesAsync();
    return Created($"/contacts/{contact.Id}", contact);
}

//  Cada función en su nivel de abstracción
public async Task<IActionResult> CreateContact(CreateContactRequest request)
{
    var response = await _handler.Handle(
        new CreateContactCommand(request.Email, request.FullName, TenantId), ct);

    return response switch
    {
        CreateContactSuccess s   => Created($"/contacts/{s.Data.Id}", s.Data),
        CreateContactConflict f  => Conflict(f.Message),
        _                        => StatusCode(500)
    };
}
```

---

## Guard Clauses  -  falla rápido y sale pronto

Las Guard Clauses eliminan el anidamiento profundo.

```csharp
//  Arrow Code  -  profundidad de anidamiento hace el código difícil de seguir
public async Task<AssignConversationResponse> Handle(AssignConversationRequest req, CancellationToken ct)
{
    if (req.AgentId > 0)
    {
        var agent = await _agentRepo.GetByIdAsync(req.TenantId, req.AgentId, ct);
        if (agent is not null)
        {
            if (agent.IsActive)
            {
                var conv = await _convRepo.GetByIdAsync(req.TenantId, req.ConvId, ct);
                if (conv is not null)
                {
                    // ... lógica real 5 niveles adentro
                }
            }
        }
    }
    return new AssignConversationFailure("Error");
}

//  Guard Clauses  -  validaciones al inicio, lógica al final (plana)
public async Task<AssignConversationResponse> Handle(AssignConversationRequest req, CancellationToken ct)
{
    var agent = await _agentRepo.GetByIdAsync(req.TenantId, req.AgentId, ct);
    if (agent is null)       return new AssignConversationAgentNotFoundFailure("Agente no encontrado.");
    if (!agent.IsActive)     return new AssignConversationAgentInactiveFailure("Agente inactivo.");

    var conv = await _convRepo.GetByIdAsync(req.TenantId, req.ConvId, ct);
    if (conv is null)        return new AssignConversationNotFoundFailure("Conversación no encontrada.");

    // Lógica real en el nivel más superficial  -  fácil de leer
    await _convRepo.AssignAsync(req.TenantId, req.ConvId, req.AgentId, ct);
    return new AssignConversationSuccess(/* dto */);
}
```

---

## Comentarios: solo el porqué, nunca el qué

```csharp
//  Comentario que explica QUÉ hace el código (redundante  -  el código ya lo dice)
// Incrementa el contador de mensajes no leídos
conversation.UnreadCount++;

//  Comentario de código comentado (¿por qué está aquí? ¿se puede borrar?)
// var oldAssignment = await _repo.GetAssignmentAsync(tenantId, convId, ct);

//  Comentario de sección (síntoma de función demasiado larga)
// --- Validar inputs ---
// --- Obtener datos de BD ---
// --- Calcular resultado ---

//  Comentario que explica POR QUÉ  -  no obvio desde el código
// WhatsApp API requiere que el número incluya el código de país sin '+'.
// Referencia: https://developers.facebook.com/docs/whatsapp/cloud-api/phone-numbers
var formattedNumber = phoneNumber.TrimStart('+');

//  Workaround documentado
// EF Core no soporta DateOnly en PostgreSQL antes de la versión 8.x.
// Mapeamos manualmente a DateTime hasta migrar la versión del ORM.
```

> Si sientes la necesidad de escribir un comentario para explicar el código,
> primero intenta renombrar la variable o extraer el método.
> El comentario es el último recurso.

---

## Magic Numbers y constantes

```csharp
//  Magic Numbers  -  ¿qué significan estos números?
if (messages.Count > 100)
    await _archiver.ArchiveAsync(conversation, ct);

if (responseTime > 30)
    await _sla.RecordViolationAsync(tenantId, convId, ct);

await Task.Delay(5000, ct);

//  Constantes con nombre  -  el porqué queda claro
private const int MaxMessagesBeforeArchiving  = 100;
private const int SlaResponseTimeThresholdMin = 30;
private const int RetryDelayMs               = 5_000;

if (messages.Count > MaxMessagesBeforeArchiving)
    await _archiver.ArchiveAsync(conversation, ct);

if (responseTimeMin > SlaResponseTimeThresholdMin)
    await _sla.RecordViolationAsync(tenantId, convId, ct);

await Task.Delay(RetryDelayMs, ct);
```

---

## Code Smells  -  señales de código que pide refactorización

| Code Smell | Síntoma | Refactoring |
|-----------|---------|------------|
| **Long Method** | Función de >20 líneas | Extraer métodos privados |
| **God Class** | Clase con >500 líneas y muchas responsabilidades | Extraer clases por responsabilidad |
| **Feature Envy** | Método que usa más datos de otra clase que de la propia | Mover el método a esa clase |
| **Primitive Obsession** | `string phoneNumber` en vez de `PhoneNumber` value object | Introducir tipos ricos |
| **Long Parameter List** | Método con >3-4 parámetros | Introducir Parameter Object / record |
| **Duplicated Code** | El mismo bloque en 3 lugares distintos | Extraer método / clase base |
| **Dead Code** | Código comentado o nunca llamado | Eliminarlo  -  el git history lo conserva |
| **Data Clumps** | Los mismos 3 parámetros siempre juntos | Agrupar en un record |

---

## Refactoring aplicado al CRM  -  antes y después

**Code Smell: Long Parameter List (antes)**

```csharp
//  7 parámetros  -  difícil de llamar y de testear
public async Task<Contact> CreateContactAsync(
    string fullName, string email, string phone,
    long tenantId, long createdByUserId,
    bool isMobileVerified, string? notes)
```

**Después  -  Parameter Object con record**

```csharp
//  Un parámetro rico con todas las propiedades
public record CreateContactParameters(
    string FullName,
    string Email,
    string Phone,
    long TenantId,
    long CreatedByUserId,
    bool IsMobileVerified,
    string? Notes);

public async Task<Contact> CreateContactAsync(CreateContactParameters p, CancellationToken ct)
```

---

## Clean Code en el back-template  -  mapa

| Principio | Dónde lo ves en el back-template |
|-----------|--------------------------------|
| **Nombres que revelan intención** | `GetUserProfileHandler`, `UserProfileNotFoundFailure`  -  el nombre dice todo |
| **Función hace una sola cosa** | `Handle()` solo coordina  -  no valida, no mapea, no logea |
| **Guard Clauses** | `if (profile is null) return new NotFoundFailure(...)` al inicio del Handle |
| **Sin magic numbers** | `IOptions<JwtSettings>`  -  ningún número hardcodeado |
| **Sin comentarios de qué** | El código en el back-template no tiene comentarios de qué hace |
| **Parameter Object** | `GetUserProfileRequest` encapsula todos los parámetros del caso de uso |
| **Sin dead code** | No hay código comentado ni métodos sin usar |

> El back-template fue diseñado siguiendo Clean Code.
> Úsalo como referencia cada vez que escribas código nuevo para el CRM.

---

## Resumen  -  Clean Code

| Área | Regla central |
|------|--------------|
| **Nombres** | Revelan intención - sin abreviaciones - verbos para métodos |
| **Funciones** | Una sola responsabilidad - un nivel de abstracción - ≤20 líneas |
| **Guard Clauses** | Validar y salir pronto - sin anidamiento profundo |
| **Comentarios** | Solo el **porqué** - nunca el qué - eliminar código comentado |
| **Magic numbers** | Siempre constantes con nombre |
| **Code Smells** | Long Method - God Class - Feature Envy - Primitive Obsession |
| **Refactoring** | Pequeños pasos - con tests si es posible - siempre en verde |

---

<!-- _class: lead -->

# Parte VI  -  Trabajo en Equipo y Proceso

## Git Flow - Feature Branches - Stakeholders - Code Review

---

## Git Flow  -  Vista previa

**Cinco tipos de ramas con responsabilidades claras:**

| Rama | Tipo | Propósito |
|------|------|-----------|
| `main` | Permanente | Código en producción  -  solo merges de `release/*` y `hotfix/*` |
| `develop` | Permanente | Integración de features  -  base para desarrollo diario |
| `feature/*` | Temporal | Una funcionalidad, un ticket |
| `release/*` | Temporal | Preparación de versión: solo fixes menores y bump |
| `hotfix/*` | Temporal | Corrección urgente que sale directamente de `main` |

```
main     ------------------------------------ (producción)
            ↑ merge release/1.0.0    ↑ merge hotfix/1.0.1
develop  ----------------------------------
              ↑ merge feature/s05   ↑ merge feature/s05b
feature  ----------------------
```

> Lo veremos a fondo en **una sesión posterior  -  Git, Docker, CI/CD**. Hoy: convención de nombres.

---

## Feature branches en la práctica

**Un feature = una rama = un contexto claro.**

```bash
# Crear la rama para el trabajo de hoy
git checkout develop
git pull origin develop
git checkout -b feature/s05-behavioral-patterns
```

**Convención de nombres:**

```
feature/s05-behavioral-patterns     <- contexto de sesión + tema
feature/crm-assign-conversation     <- módulo del producto + funcionalidad
bugfix/crm-fix-contact-import       <- tipo de cambio explícito
```

**Por qué importa el nombre de la rama:**
- El equipo sabe de un vistazo en qué trabaja cada persona
- El PR hereda el contexto  -  no hay que explicar de qué trata
- Git Flow funciona con nombres  -  `release/*` y `hotfix/*` no son decoración

---

## Gestión de Stakeholders

**Stakeholder:** cualquier persona con interés o influencia sobre el producto.

**Cuadrante poder/interés  -  qué estrategia usar con cada uno:**

| | Interés bajo | Interés alto |
|--|---|---|
| **Poder alto** | Mantener satisfecho (actualizaciones breves) | Gestionar de cerca (colaboración activa) |
| **Poder bajo** | Monitorear (mínimo esfuerzo) | Informar (demos, métricas) |

**Qué le importa a cada rol:**

| Rol | Su pregunta clave |
|-----|------------------|
| **PO / Cliente** | ¿Cuándo estará listo? ¿Qué valor genera? |
| **CTO / Tech Lead** | ¿Escala? ¿Genera deuda? ¿Está documentado? |
| **Usuario final** | ¿Es fácil de usar? ¿Responde rápido? ¿No pierde mis datos? |
| **Ops / DevOps** | ¿Puedo desplegarlo sin riesgo? ¿Tengo visibilidad si falla? |

---

## Comunicar decisiones técnicas

**Estructura que funciona con cualquier stakeholder no técnico:**

```
1. Contexto        -  ¿Cuál es el problema o la situación actual?
2. Opción elegida  -  ¿Qué se decidió hacer?
3. Trade-offs      -  ¿Qué se gana? ¿Qué se sacrifica?
4. Próximos pasos  -  ¿Qué se espera del stakeholder (si algo)?
```

**Ejemplo  -  Strategy para asignación de conversaciones:**

```
Contexto:       Hoy la lógica de asignación está hardcoded  -  cada cliente
                que quiera una regla diferente requiere un cambio de código.

Opción elegida: Extraer la lógica a una interfaz intercambiable (Strategy)
                configurable por workspace sin cambiar el código base.

Trade-offs:     +  Nuevas reglas sin redeploy del servicio
                +  Cada workspace puede tener su algoritmo propio
                −  Un archivo más por estrategia (complejidad marginal)

Próximos pasos: Confirmar qué reglas necesita la empresa para la demo.
```

> Sin jerga. Sin mencionar interfaces, DI containers, ni nombres de patrones.

---

## Code Review como práctica de equipo

**Qué revisar en un PR  -  en orden de prioridad:**

| Prioridad | Qué revisar | Señal de problema |
|-----------|------------|-------------------|
| 1 | **Lógica y correctitud** | El código hace lo que el ticket pide |
| 2 | **Seguridad** | Sin SQL injection, credenciales hardcoded, roles validados |
| 3 | **Legibilidad** | Otro desarrollador lo entiende sin comentarios |
| 4 | **Performance** | Sin N+1, sin locks innecesarios, paginación donde corresponde |
| 5 | **Convenciones** | Nombres, estructura de capas, formato |

**Tono constructivo  -  las tres reglas:**

- Preguntar en lugar de afirmar: *"¿Consideraste usar X aquí?"* en vez de *"Esto está mal"*
- Distinguir bloqueante de sugerencia: `[bloqueante]` vs `[nit]`
- Aprobar aunque haya nits  -  no retener un PR por preferencias de estilo

**Velocidad:** revisar en menos de 24h hábiles  -  el costo de un PR en espera es real.
