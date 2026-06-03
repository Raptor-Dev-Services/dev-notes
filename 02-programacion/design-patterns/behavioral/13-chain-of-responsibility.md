# 13 — Chain of Responsibility

**Categoría:** Conductual

**Intención:** Permite pasar peticiones a lo largo de una cadena de handlers. Al recibir una petición, cada handler decide procesarla o pasarla al siguiente handler de la cadena.

> Fuente: *Dive Into Design Patterns* (Alexander Shvets) — Ch.5 Behavioral Patterns: Chain of Responsibility

---

## El problema

Tienes un sistema de soporte técnico con tres niveles: básico, técnico, y manager. Una petición de soporte debe pasar por el nivel básico primero; si no puede resolverla, la escala al técnico; si tampoco puede, al manager. Sin el patrón, el código de enrutamiento quedaría en un único lugar con if/else encadenados.

```csharp
// ❌ Sin Chain of Responsibility — if/else monolítico
public string Handle(SupportRequest request)
{
    if (request.Type == "basic")
        return BasicSupportTeam.Handle(request);
    else if (request.Type == "technical")
        return TechTeam.Handle(request);
    else if (request.Type == "billing")
        return ManagerTeam.Handle(request);
    else
        return "No handler found.";
}
// Agregar un nivel nuevo = modificar este método
```

---

## Analogía

Un proceso de aprobación de gastos corporativos. Una factura de $50 la aprueba el supervisor. Una de $500 va al director. Una de $5,000 requiere al CEO. Cada nivel decide: "puedo manejarlo" o "lo escalo al siguiente". La factura viaja por la cadena hasta que alguien la maneja o llega al final sin ser procesada.

---

## Estructura

```
IHandler
├── SetNext(IHandler): IHandler   ← encadena al siguiente
└── Handle(object): object        ← procesa o pasa adelante

AbstractHandler : IHandler
├── _nextHandler: IHandler
├── SetNext(handler) → return handler (permite encadenar fluent)
└── Handle(request) → if _nextHandler != null → _nextHandler.Handle(request)

MonkeyHandler : AbstractHandler
└── Handle(request) → si es "Banana": procesar; sino: base.Handle(request)

SquirrelHandler : AbstractHandler
└── Handle(request) → si es "Nut": procesar; sino: base.Handle(request)
```

---

## Código del ejemplo conceptual

```csharp
public interface IHandler
{
    IHandler SetNext(IHandler handler);
    object Handle(object request);
}

abstract class AbstractHandler : IHandler
{
    private IHandler _nextHandler;

    // Retorna el handler para permitir encadenamiento fluent
    public IHandler SetNext(IHandler handler)
    {
        _nextHandler = handler;
        return handler;  // ← retorna el SIGUIENTE para que el chain sea fluent
    }

    public virtual object Handle(object request)
    {
        // Si hay siguiente, pasa la petición; si no, null (no procesada)
        return _nextHandler?.Handle(request);
    }
}

class MonkeyHandler : AbstractHandler
{
    public override object Handle(object request)
    {
        if ((string)request == "Banana")
            return $"Monkey: I'll eat the {request}.";
        return base.Handle(request);  // pasar al siguiente
    }
}

class SquirrelHandler : AbstractHandler
{
    public override object Handle(object request)
    {
        if ((string)request == "Nut")
            return $"Squirrel: I'll eat the {request}.";
        return base.Handle(request);
    }
}

class DogHandler : AbstractHandler
{
    public override object Handle(object request)
    {
        if ((string)request == "MeatBall")
            return $"Dog: I'll eat the {request}.";
        return base.Handle(request);
    }
}

// Construcción de la cadena — fluent:
var monkey   = new MonkeyHandler();
var squirrel = new SquirrelHandler();
var dog      = new DogHandler();

monkey.SetNext(squirrel).SetNext(dog);
// monkey → squirrel → dog

// Enviar peticiones por la cadena:
foreach (var food in new[] { "Nut", "Banana", "Cup of coffee" })
{
    var result = monkey.Handle(food);
    Console.WriteLine(result != null
        ? $"   {result}"
        : $"   {food} was left untouched.");
}
// "   Squirrel: I'll eat the Nut."      ← llegó a squirrel
// "   Monkey: I'll eat the Banana."     ← paró en monkey
// "   Cup of coffee was left untouched." ← nadie lo manejó
```

---

## Ejemplo real: validación de petición HTTP

```csharp
// Cada handler valida un aspecto — si falla, cortocircuita; si pasa, llama al siguiente
public abstract class RequestValidator
{
    private RequestValidator? _next;

    public RequestValidator SetNext(RequestValidator next)
    {
        _next = next;
        return next;
    }

    public abstract Task<ValidationResult> Validate(HttpRequest request);

    protected async Task<ValidationResult> ValidateNext(HttpRequest request)
    {
        if (_next is null) return ValidationResult.Success;
        return await _next.Validate(request);
    }
}

public class AuthHeaderValidator : RequestValidator
{
    public override async Task<ValidationResult> Validate(HttpRequest request)
    {
        if (!request.Headers.ContainsKey("Authorization"))
            return ValidationResult.Fail("Missing Authorization header");

        return await ValidateNext(request);  // pasar al siguiente
    }
}

public class ContentTypeValidator : RequestValidator
{
    public override async Task<ValidationResult> Validate(HttpRequest request)
    {
        if (request.ContentType != "application/json")
            return ValidationResult.Fail("Content-Type must be application/json");

        return await ValidateNext(request);
    }
}

public class RateLimitValidator : RequestValidator
{
    private readonly IRateLimiter _limiter;

    public RateLimitValidator(IRateLimiter limiter) { _limiter = limiter; }

    public override async Task<ValidationResult> Validate(HttpRequest request)
    {
        var clientId = request.Headers["X-Client-Id"].ToString();
        if (!await _limiter.AllowAsync(clientId))
            return ValidationResult.Fail("Rate limit exceeded");

        return await ValidateNext(request);
    }
}

// Construcción de la cadena:
var authValidator        = new AuthHeaderValidator();
var contentTypeValidator = new ContentTypeValidator();
var rateLimitValidator   = new RateLimitValidator(limiter);

authValidator
    .SetNext(contentTypeValidator)
    .SetNext(rateLimitValidator);

// Validar una petición — pasa por todos hasta que uno falle o todos pasen:
var result = await authValidator.Validate(httpRequest);
```

---

## Implementación ASP.NET Core (Ferreira)

Ferreira (*Architecting ASP.NET Core Applications*, Ch.12) implementa un intérprete de mensajes de alarma y muestra cómo combinar Chain of Responsibility con Template Method para eliminar código duplicado.

### Cadena de handlers sin clase base

```csharp
// Interfaz del handler
public interface IMessageHandler
{
    void Handle(Message message);
}

public record class Message(string Name, string Payload);

// Cada handler inyecta el siguiente en su constructor
public class AlarmTriggeredHandler : IMessageHandler
{
    private readonly IMessageHandler? _next;
    public AlarmTriggeredHandler(IMessageHandler? next = null) { _next = next; }

    public void Handle(Message message)
    {
        if (message.Name == "AlarmTriggered")
        {
            // Procesar — lógica específica de esta alarma
        }
        else
        {
            _next?.Handle(message);  // delegar al siguiente
        }
    }
}

// Handler terminal — notifica que ningún handler pudo procesar el mensaje
public class DefaultHandler : IMessageHandler
{
    public void Handle(Message message)
    {
        throw new NotSupportedException(
            $"Messages named '{message.Name}' are not supported.");
    }
}
```

### Registrar la cadena en el contenedor de DI

```csharp
// Opción 1 — construcción manual explícita
builder.Services.AddSingleton<IMessageHandler>(
    new AlarmTriggeredHandler(
        new AlarmPausedHandler(
            new AlarmStoppedHandler(
                new DefaultHandler()))));

// Opción 2 — con Scrutor (más legible, misma semántica)
// Nota: el orden es inverso porque cada uno decora al anterior
builder.Services
    .AddSingleton<IMessageHandler, DefaultHandler>()
    .Decorate<IMessageHandler, AlarmStoppedHandler>()
    .Decorate<IMessageHandler, AlarmPausedHandler>()
    .Decorate<IMessageHandler, AlarmTriggeredHandler>();
```

### Mezclar Template Method + Chain of Responsibility (diseño mejorado)

Ferreira extrae el código duplicado (inyección del siguiente, lógica de delegación) a una clase base usando Template Method, dejando cada handler solo con su responsabilidad:

```csharp
// Clase base — Template Method para el plumbing de la cadena
public abstract class MessageHandlerBase : IMessageHandler
{
    private readonly IMessageHandler? _next;

    public MessageHandlerBase(IMessageHandler? next = null)
    {
        _next = next;
    }

    // Template Method — define el flujo: ¿puedo manejarlo? → proceso; si no → delego
    public void Handle(Message message)
    {
        if (CanHandle(message))
            Process(message);
        else if (HasNext())
            _next!.Handle(message);
    }

    [MemberNotNullWhen(true, nameof(_next))]
    private bool HasNext() => _next != null;

    // Hook ABSTRACTO — cada subclase decide si puede manejar el mensaje
    protected abstract bool CanHandle(Message message);

    // Hook ABSTRACTO — cada subclase implementa su lógica específica
    protected abstract void Process(Message message);
}

// Para handlers que manejan UN solo tipo de mensaje:
public abstract class SingleMessageHandlerBase : MessageHandlerBase
{
    public SingleMessageHandlerBase(IMessageHandler? next = null)
        : base(next) { }

    protected override bool CanHandle(Message message)
        => message.Name == HandledMessageName;

    // Hook ABSTRACTO — nombre del mensaje que este handler maneja
    protected abstract string HandledMessageName { get; }
}

// Para handlers que manejan MÚLTIPLES tipos de mensaje:
public abstract class MultipleMessageHandlerBase : MessageHandlerBase
{
    public MultipleMessageHandlerBase(IMessageHandler? next = null)
        : base(next) { }

    protected override bool CanHandle(Message message)
        => HandledMessagesName.Contains(message.Name);

    protected abstract string[] HandledMessagesName { get; }
}
```

### Handlers concretos simplificados

Con la base abstracta, cada handler solo define su nombre y su lógica:

```csharp
public class AlarmTriggeredHandler : SingleMessageHandlerBase
{
    protected override string HandledMessageName => "AlarmTriggered";

    public AlarmTriggeredHandler(IMessageHandler? next = null)
        : base(next) { }

    protected override void Process(Message message)
    {
        // Lógica específica de AlarmTriggered
    }
}

public class AlarmPausedHandler : SingleMessageHandlerBase
{
    protected override string HandledMessageName => "AlarmPaused";

    public AlarmPausedHandler(IMessageHandler? next = null)
        : base(next) { }

    protected override void Process(Message message)
    {
        // Lógica específica de AlarmPaused
    }
}

// Handler que maneja múltiples mensajes
public class SomeMultiHandler : MultipleMessageHandlerBase
{
    public SomeMultiHandler(IMessageHandler? next = null)
        : base(next) { }

    protected override string[] HandledMessagesName
        => new[] { "Foo", "Bar", "Baz" };

    protected override void Process(Message message)
    {
        // Lógica para cualquiera de los tres mensajes
    }
}
```

### Endpoint que usa la cadena

```csharp
app.MapPost("/handle", (Message message, IMessageHandler messageHandler) =>
{
    try
    {
        messageHandler.Handle(message);
        return $"Message '{message.Name}' handled successfully.";
    }
    catch (NotSupportedException ex)
    {
        return ex.Message;  // "Messages named 'X' are not supported."
    }
});
```

### El patrón como pipeline (múltiples handlers procesan el mismo mensaje)

La cadena no requiere cortocircuitar en el primer handler que procesa. Si todos los handlers llaman al siguiente después de procesar, se convierte en un pipeline:

```csharp
// En el handler base: Process siempre llama al siguiente
public void Handle(Message message)
{
    Process(message);      // siempre procesa
    _next?.Handle(message); // Y siempre pasa al siguiente
}
// Útil para transformaciones en cadena, logs, decoradores de comportamiento
```

## En este proyecto

```csharp
// 1. El middleware pipeline de ASP.NET Core ES una Chain of Responsibility:
app.UseHttpsRedirection();   // handler 1: redirige si no es HTTPS
app.UseCors(PolicyName);     // handler 2: verifica CORS
app.UseAuthentication();     // handler 3: autentica el JWT
app.UseAuthorization();      // handler 4: verifica permisos
app.MapControllers();        // handler final: ejecuta el controller

// Cada middleware llama await next() para pasar al siguiente.
// Si un middleware no llama next(), cortocircuita la cadena.
// Ejemplo: UseAuthentication detecta token inválido → retorna 401 sin llamar a MapControllers.

// 2. El InteractorPipeline del mediador es un chain:
// Controller
//   → Mediator.Send(request)     ← envía al pipeline
//   → [pipeline behaviors]       ← cadena de handlers
//   → Handler.Handle(request)    ← handler final
//   → InteractorPipeline.Publish ← cadena de notification handlers
//   → Presenter.Handle(response)
```

---

## Cuándo usar

- Cuando más de un objeto puede manejar una petición y el handler no se conoce de antemano.
- Cuando quieres que el conjunto de handlers sea configurable en runtime.
- Cuando el orden de procesamiento importa y puede variar.
- Pipelines de procesamiento: middleware, validación, transformación, filtros.

## Cuándo NO usar

- Cuando tienes garantía de que la petición siempre será manejada. Si puede "caer al vacío", considerar si eso es correcto.
- Cuando el encadenamiento genera demasiada indirección y hace el debug difícil.
- Cuando solo tienes 2-3 handlers fijos. Un if/else simple es más claro.


---

## Glosario

| Término | Definición |
|---------|-----------|
| Chain of Responsibility | Patrón conductual que pasa una petición por una cadena de handlers; cada uno decide procesarla o pasarla al siguiente |
| Handler | Objeto que procesa una petición o la delega al siguiente handler de la cadena |
| AbstractHandler | Clase base que implementa el encadenamiento (`SetNext`) y la delegación por defecto al siguiente handler |
| Cortocircuito | Comportamiento en que un handler detiene la cadena sin llamar al siguiente, típicamente al detectar un error |
| `SetNext()` | Método que retorna el siguiente handler para permitir construcción fluent de la cadena: `a.SetNext(b).SetNext(c)` |
| Middleware pipeline | Implementación del patrón en ASP.NET Core: cada middleware es un handler que puede pasar al siguiente con `await next()` |
| `await next()` | Llamada en un middleware de ASP.NET Core que pasa la petición al siguiente handler de la cadena |
| InteractorPipeline | Cadena de behaviors del mediador del proyecto que orquesta el flujo Handler → Publish → Presenter |
| Petición no manejada | Resultado cuando ningún handler procesa la petición y llega al final de la cadena sin ser atendida |
| Configuración en runtime | Capacidad de ensamblar y reordenar la cadena dinámicamente sin modificar los handlers existentes |
| `MessageHandlerBase` | Clase base de Ferreira que usa Template Method para encapsular el plumbing de la cadena — los handlers concretos solo implementan `CanHandle` y `Process` |
| `SingleMessageHandlerBase` | Subclase de `MessageHandlerBase` para handlers que procesan un solo tipo de mensaje — implementa `CanHandle` comparando `message.Name == HandledMessageName` |
| `MultipleMessageHandlerBase` | Subclase de `MessageHandlerBase` para handlers que procesan varios tipos de mensaje — `CanHandle` verifica si el nombre está en un array |
| Handler terminal | Handler al final de la cadena que no delega al siguiente — notifica cuando ningún handler pudo procesar el mensaje (ej. lanza `NotSupportedException`) |
| Scrutor | Librería de DI para .NET que permite registrar decoradores con `.Decorate<TInterface, TDecorator>()` — simplifica el ensamble de cadenas de responsabilidad |
| `.Decorate<T, D>()` | Método de Scrutor que registra `D` como decorador de `T` — usado para construir la cadena en orden inverso al de ejecución |
| Pipeline vs cadena | Variante donde todos los handlers procesan el mismo mensaje (cada uno llama al siguiente después de procesar) — útil para transformaciones y logging |
| `[MemberNotNullWhen]` | Atributo de C# que le indica al compilador que un campo no es null cuando el método retorna `true` — evita warnings de nullable en la delegación al `_next` |

---

> Fuente adicional: *Architecting ASP.NET Core Applications, 3a ed.* (Carl-Hugo Marcotte) — Ch.12 Behavioral Patterns

*Rogelio Arriaga Gonzalez*
